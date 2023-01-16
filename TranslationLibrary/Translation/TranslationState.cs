using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using TranslationLibrary.Dialogue;
using TranslationLibrary.Enums;
using TranslationLibrary.Localization;
using TranslationLibrary.Localization.Records;
using TranslationLibrary.Storage;

namespace TranslationLibrary.Translation;

public class TranslationState : IDisposable
{
    private string _filePath;
    private string _encoding;

    private IntPtr _state;

    public LocalizationStorage BaseLocalizations { get; set; } = new();

    private LocalizationStorage _mergedLocalizations = new();
    private readonly LocalizationStorage _collectedLocalizations = new();
    
    public DialogueHelper DialogueHelper { get; set; } = new();

    public IEnumerable<TranslationRecord> Records => Storage.Records;

    public string FilePath => _filePath;
    public string Encoding => _encoding;
    public string FileContext => Path.GetFileNameWithoutExtension(FilePath);

    public TranslationStorage Storage { get; } = new();
    
    public ProblemStorage ProblemStorage { get; } = new();

    public void Merge(TranslationState other, MergeMode mode = MergeMode.Full)
    {
        _encoding = other._encoding;
        Storage.Merge(other.Storage, mode);
        BaseLocalizations.Merge(other.BaseLocalizations);
        _collectedLocalizations.Merge(other._collectedLocalizations);
        _mergedLocalizations.Merge(other._mergedLocalizations);
    }

    public void Load(string path, string encoding)
    {
        if (_state != IntPtr.Zero)
        {
            Dispose();
            Storage.Clear();
        }

        _filePath = path;
        _encoding = encoding;

        BaseLocalizations.LoadNative(Path.GetDirectoryName(_filePath), _encoding, FileContext);

        _state = Imports.Translation_GetTexts(path, encoding);
        if (_state == IntPtr.Zero)
            throw new Exception(Helpers.GetLastError($"Failed to get texts: {Imports.Translation_GetLastError()}"));

        for (;;)
        {
            var info = Imports.TranslationState_GetNextRecordInfo(_state);
            if (info.Pointer == IntPtr.Zero)
                break;

            TranslationRecord record = new()
            {
                Pointer = info.Pointer,
                Type = (TextType)info.Type,
                MaxLength = info.MaxLength,
                OriginalText = info.Source,
                UnprocessedOriginalText = info.Source,
                Text = info.Target,
                ContextName = info.ContextName,
                Meta = info.Meta.Length > 0 ? JsonObject.Parse(info.Meta) : null,
            };

            var contextId = info.ContextId;
            if (record.ContextName == "DIAL")
            {
                var localizationEntry = BaseLocalizations.DialogueNames.LookupRecord(record.OriginalText);
                if (localizationEntry != null)
                    contextId = localizationEntry.Target;
            }

            if (record.ContextName == "INFO")
            {
                var dialogue = record.Meta["dialogue"].ToString();
                var dialogueType = record.Meta["dialogue_type"].ToString();
                if (Enum.Parse<DialogueType>(dialogueType) == DialogueType.Topic)
                {
                    var localizationEntry = BaseLocalizations.DialogueNames.LookupRecord(dialogue);
                    if (localizationEntry != null)
                        dialogue = localizationEntry.Target;
                }

                contextId = $"{dialogue}_{contextId}";

                if (record.Type == TextType.Text)
                {
                    record.OriginalText =
                        record.UnprocessedOriginalText = Helpers.ReplacePseudoAsterisks(record.OriginalText);
                    record.Text = Helpers.ReplacePseudoAsterisks(record.Text);
                }
            }

            record.ContextId = !string.IsNullOrEmpty(contextId)
                ? $"{contextId}_{info.Index}"
                : $"{record.OriginalText}_{info.Index}";

            Storage.Add(record);
        }

        DialogueHelper.Load(this);
    }

    public void PrepareRecordsForDump(DumpOptions options)
    {
        var infoRecords = Storage.LookupContext("INFO");
        if (infoRecords == null)
            return;

        foreach (var record in infoRecords)
        {
            if (record.Type != TextType.Text)
                continue;

            if (!options.HasFlag(DumpFlags.IncludeFullTopics) && !options.HasFlag(DumpFlags.IncludeImplicitTopics))
            {
                record.OriginalText = record.UnprocessedOriginalText;
                continue;
            }

            var dialogueType = Enum.Parse<DialogueType>(record.Meta["dialogue_type"].ToString());
            if (!dialogueType.CanHaveHyperlinks())
                continue;

            var script = record.Meta["script"].ToString();
            if (options.HasFlag(DumpFlags.IncludeFullTopics))
            {
                record.OriginalText = Helpers.MarkupDialogues(record.UnprocessedOriginalText,
                    script, out _, DialogueHelper);
            }
            else if (options.HasFlag(DumpFlags.IncludeImplicitTopics))
                record.OriginalText = Helpers.MarkupDialogues(record.UnprocessedOriginalText,
                    script, out _);
        }
    }

    private static readonly List<string> NonTranslatedContextNames = new()
    {
        "CELL", "REGN"
    };

    private bool NeedSaveContext(string contextName, SaveOptions options)
    {
        if (contextName == "REGN" && options.HasFlag(SaveOptions.TranslateRegions))
            return true;

        return !NonTranslatedContextNames.Contains(contextName);
    }

    public void Save(string path, string encoding, SaveOptions options = SaveOptions.None, bool skipPostProcess = false)
    {
        if (!skipPostProcess)
            TranslationPostProcess();

        foreach (var record in Storage.Records)
        {
            if (!NeedSaveContext(record.ContextName, options))
                continue;

            if (record.IsTranslated)
            {
                var text = record.Text;
                if (record.ContextName == "INFO" && record.Type == TextType.Text)
                    text = Helpers.ReplacePseudoAsterisks(text, true);

                if (record.MaxLength > 0 && text.Length > record.MaxLength)
                {
                    throw new Exception(
                        $"Text exceeds max length ({record.MaxLength}) for id: '{record.ContextId}', context: '{record.ContextName}': '{text}'");
                }

                Imports.TranslationRecord_SetTarget(record.Pointer, text);
            }
        }

        var res = Imports.Translation_Save(_state, Path.Combine(path, Path.GetFileName(_filePath)), encoding);
        if (res != 0)
            throw new Exception(Helpers.GetLastError("Save failed"));

        try
        {
            _collectedLocalizations.Diff(BaseLocalizations).SaveNative(path, FileContext, encoding);
        }
        catch (Exception e)
        {
            throw new Exception($"Secondary translation files save failed: {e.Message}");
        }
    }

    private LocalizationStorage CollectLocalizations()
    {
        LocalizationStorage localizations = new();

        var dialogues = Storage.LookupContext("DIAL");
        if (dialogues != null)
        {
            foreach (var dialogue in dialogues)
                if (dialogue.IsTranslated)
                    localizations.DialogueNames.Add(new()
                    {
                        Type = MappingType.Topic, Source = dialogue.UnprocessedOriginalText, Target = dialogue.Text
                    });
        }

        var cells = Storage.LookupContext("CELL");
        if (cells != null)
        {
            foreach (var cell in cells)
                if (cell.IsTranslated)
                    localizations.Cells.Add(new()
                        { Type = MappingType.Cell, Source = cell.UnprocessedOriginalText, Target = cell.Text });
        }

        var regions = Storage.LookupContext("REGN");
        if (regions != null)
        {
            foreach (var region in regions)
                if (region.IsTranslated)
                    localizations.Cells.Add(new()
                        { Type = MappingType.Cell, Source = region.UnprocessedOriginalText, Target = region.Text });
        }
        

        return localizations;
    }

    public void TranslationPostProcess()
    {
        foreach (var record in Storage.Records)
        {
            if (record.Type == TextType.Script && record.IsTranslated)
                record.Text = PostProcessScriptText(record.Text, _mergedLocalizations);
        }
    }

    private static string PostProcessScriptText(string scriptText, LocalizationStorage localizations)
    {
        return Regex.Replace(scriptText, "AddTopic[\\s,]*\"([^\"])+\"", match =>
            {
                var topic = match.Groups[1].Value;
                var mapping = localizations.DialogueNames.LookupRecordByTarget(topic);
                if (mapping != null)
                    topic = mapping.Source;

                return topic.Trim();
            },
            RegexOptions.IgnoreCase);
    }

    public void Dispose()
    {
        if (_state != IntPtr.Zero)
            Imports.TranslationState_Dispose(_state);
    }

    public Dictionary<string, TextStorage<TranslationRecord>.MergedInfo> MergeTranslations(
        TextStorage<TranslationRecord> translations, LocalizationStorage phraseForms)
    {
        var result = Storage.MergeTexts(translations);
        
        _collectedLocalizations.Merge(phraseForms);
        _collectedLocalizations.Merge(CollectLocalizations());
        
        _mergedLocalizations.Clear();
        _mergedLocalizations.Merge(BaseLocalizations);
        _mergedLocalizations.Merge(_collectedLocalizations);

        return result;
    }

    public class AnalyzeResult
    {
        public Dictionary<string, List<string>> PhraseformCandidates = new();
        public ProblemStorage Problems = new();
    }

    public AnalyzeResult Analyze()
    {
        var result = new AnalyzeResult();
        result.Problems = ProblemStorage;

        var grouped = Storage.LookupContext("DIAL").GroupBy(_ => _.Text);
        foreach (var grp in grouped)
            if (grp.Count() > 1)
                ProblemStorage.AddError($"Multiple topics with name '{grp.Key}', origins: {string.Join(", ", grp)}");

        var hyperlinkTopics = CollectHyperlinks();

        foreach (var (infoId, hyperlinkInfo) in hyperlinkTopics)
        {
            foreach (var topic in hyperlinkInfo.New)
            {
                if (topic.Trim() != topic)
                    ProblemStorage.AddWarning($"Hyperlink '{topic.Trim()}' contains extra spaces, INFO id: '{infoId}'");

                if (!CheckKnownTopicOrPhraseForm(topic.Trim(), _mergedLocalizations))
                {
                    ProblemStorage.AddWarning($"'{topic}' is not a known topic or phraseform, INFO id: '{infoId}'");
                    result.PhraseformCandidates[topic.Trim()] = hyperlinkInfo.Old.Select(_ =>
                    {
                        var mapping = _mergedLocalizations.DialogueNames.LookupRecordByTarget(_);
                        if (mapping != null)
                            return mapping.Source;

                        return _;
                    }).ToList();
                }
            }
        }

        foreach (var phraseForm in _mergedLocalizations.DialoguePhraseForms)
        {
            if (!_mergedLocalizations.DialogueNames.HasRecord(phraseForm.Target))
                ProblemStorage.AddWarning(
                    $"Phraseform '{phraseForm.Source}' refers to unknown dialogue '{phraseForm.Target}'");
        }

        return result;
    }

    private bool CheckKnownTopicOrPhraseForm(string topic, LocalizationStorage localizations)
    {
        if (DialogueHelper.Dialogues.HasRecord(topic))
            return true;

        var phraseForm = localizations.DialoguePhraseForms.LookupRecord(topic);
        if (phraseForm != null)
            return true;

        return false;
    }

    class HyperlinkInfo
    {
        public List<string> Old = new();
        public List<string> New = new();
    }

    private Dictionary<string, HyperlinkInfo> CollectHyperlinks()
    {
        var infos = Storage.LookupContext("INFO");
        if (infos == null)
            return new();

        Dictionary<string, HyperlinkInfo> result = new();
        foreach (var info in infos)
        {
            if (!info.IsTranslated)
                continue;

            Helpers.MarkupDialogues(info.UnprocessedOriginalText, "", out var dialogues, DialogueHelper);
            var newMatches = Regex.Matches(info.Text, "@([^#])#", RegexOptions.IgnoreCase);

            result[info.ContextId] = new()
            {
                Old = dialogues,
                New = newMatches.Select(_ => _.Groups[1].Value).ToList(),
            };
        }

        return result;
    }
}
