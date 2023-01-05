using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using TranslationLibrary.Dialogue;
using TranslationLibrary.Enums;
using TranslationLibrary.Localization;

namespace TranslationLibrary.Translation;

public class TranslationState : IDisposable
{
    private string _filePath;
    private string _encoding;

    private IntPtr _state;

    private readonly RecordStorage<TranslationRecord> _storage = new();
    private readonly LocalizationStore _localization = new();

    public List<TranslationRecord> Records => _storage.Records;

    public Dictionary<string, Dictionary<string, TranslationRecord>> RecordsByContextAndId =>
        _storage.RecordsByContextAndId;

    public string FilePath => _filePath;
    public string Encoding => _encoding;
    public string FileContext => Path.GetFileNameWithoutExtension(FilePath);

    public RecordStorage<TranslationRecord> Storage => _storage;

    public void Merge(TranslationState other, MergeMode mode = MergeMode.Full)
    {
        _encoding = other._encoding;
        _storage.Merge(other._storage, mode);
        _localization.Merge(other._localization);
    }

    public void Load(string path, string encoding)
    {
        if (_state != IntPtr.Zero)
        {
            Dispose();
            _storage.Clear();
        }

        _filePath = path;
        _encoding = encoding;

        _localization.LoadNative(Path.GetDirectoryName(_filePath), _encoding, FileContext);

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
                OriginalText = info.Source,
                UnprocessedOriginalText = info.Source,
                Text = info.Target,
                ContextName = info.ContextName,
                Meta = info.Meta.Length > 0 ? JsonObject.Parse(info.Meta) : null,
            };

            var contextId = info.ContextId;
            if (record.ContextName == "DIAL")
            {
                var localizationEntry = _localization.DialogueNames.LookupRecord(record.OriginalText);
                if (localizationEntry != null)
                    contextId = localizationEntry.Target;
            }

            if (record.ContextName == "INFO")
            {
                var dialogue = record.Meta["dialogue"].ToString();
                var dialogueType = record.Meta["dialogue_type"].ToString();
                if (Enum.Parse<DialogueType>(dialogueType) == DialogueType.Topic)
                {
                    var localizationEntry = _localization.DialogueNames.LookupRecord(dialogue);
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

            _storage.Add(record);
        }
    }

    // private bool HasTranslation(TranslationRecord record)
    // {
    //     if (Translated == null)
    //         return false;
    //
    //     var candidate = Translated.LookupRecord(record);
    //     if (candidate == null)
    //         return false;
    //
    //     var matches = Regex.Matches(candidate.OriginalText, "[А-Яа-я]");
    //     var isMatch = (float)matches.Count / candidate.OriginalText.Length > 0.2;
    //     if (candidate.ContextName == "BOOK" && candidate.Type == TextType.Html && isMatch)
    //         return isMatch;
    //
    //     return isMatch;
    // }

    public Dictionary<string, Dump.DumpInfo> DumpScripts(string path, string sourceContext, DumpOptions options)
    {
        if (!Directory.Exists(path))
            throw new Exception($"Directory '{path}' does not exist");

        var newOptions = DumpOptions.Clone(options);
        newOptions.TextTypes = new() { TextType.Script };

        PrepareRecordsForDump(options);

        return Dump.DumpStore(path, _storage, sourceContext, options);
    }

    public Dictionary<string, Dump.DumpInfo> DumpBooks(string path, string sourceContext, DumpOptions options)
    {
        if (!Directory.Exists(path))
            throw new Exception($"Directory '{path}' does not exist");

        var newOptions = DumpOptions.Clone(options);
        newOptions.TypesInclude = new() { "BOOK" };
        newOptions.TextTypes = new() { TextType.Html };

        PrepareRecordsForDump(options);

        return Dump.DumpStore(path, _storage, sourceContext, options);
    }

    public Dictionary<string, Dump.DumpInfo> DumpNPCs(string path, string sourceContext, DumpOptions options)
    {
        if (!Directory.Exists(path))
            throw new Exception($"Directory '{path}' does not exist");

        var newOptions = DumpOptions.Clone(options);
        newOptions.TypesInclude = new() { "NPC_" };

        PrepareRecordsForDump(options);

        return Dump.DumpStore(path, _storage, sourceContext, options);
    }

    public void PrepareRecordsForDump(DumpOptions options)
    {
        var infoRecords = _storage.RecordsByContextAndId.GetValueOrDefault("INFO");
        if (infoRecords == null)
            return;

        DialogueHelper? dialogueHelper = null;

        foreach (var (_, record) in infoRecords)
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
                if (dialogueHelper == null && options.HasFlag(DumpFlags.IncludeFullTopics))
                    dialogueHelper = DialogueHelper.Create(this);
                record.OriginalText = Helpers.MarkupDialogues(record.UnprocessedOriginalText,
                    script, out _, dialogueHelper);
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

    public void Save(string path, string encoding, SaveOptions options = SaveOptions.None)
    {
        foreach (var record in _storage.Records)
        {
            if (!NeedSaveContext(record.ContextName, options))
                continue;

            if (record.IsTranslated)
            {
                var text = record.Text;
                if (record.ContextName == "INFO" && record.Type == TextType.Text)
                    text = Helpers.ReplacePseudoAsterisks(text, true);

                Imports.TranslationRecord_SetTarget(record.Pointer, text);
            }
        }

        var res = Imports.Translation_Save(_state, Path.Combine(path, Path.GetFileName(_filePath)), encoding);
        if (res != 0)
            throw new Exception(Helpers.GetLastError("Save failed"));

        try
        {
            _localization.UpdateFromState(this);
            _localization.SaveNative(path, FileContext, encoding);
        }
        catch (Exception e)
        {
            throw new Exception($"Secondary translation files save failed: {e.Message}");
        }
    }

    public void Dispose()
    {
        if (_state != IntPtr.Zero)
            Imports.TranslationState_Dispose(_state);
    }
}
