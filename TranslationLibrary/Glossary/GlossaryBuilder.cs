using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TranslationLibrary.Enums;
using TranslationLibrary.Localization;
using TranslationLibrary.Translation;

namespace TranslationLibrary.Glossary
{
    public class GlossaryBuilder
    {
        private readonly LocalizationStore _localizations = new();
    
        private RecordStorage<GlossaryRecord> _storage = new();

        public RecordStorage<GlossaryRecord> Storage => _storage;

        public void Build(TranslationState original, TranslationState localized, BuildMode mode = BuildMode.Merge)
        {
            _localizations.LoadNative(Path.GetDirectoryName(original.FilePath), original.Encoding, original.FileContext);

            BuildExactMatches(original, localized, mode);
            BuildOtherMatches(original, localized, mode);
        }

        private void BuildOtherMatches(TranslationState original, TranslationState localized, BuildMode mode = BuildMode.Merge)
        {
        
        }

        private void BuildExactMatches(TranslationState original, TranslationState localized, BuildMode mode = BuildMode.Merge)
        {
            Dictionary<string, Dictionary<string, TranslationRecord>> originalInfosByDialogue = new(); 
            Dictionary<string, Dictionary<string, TranslationRecord>> localizedInfosByDialogue = new();

            var originalInfos = original.RecordsByContextAndId.GetValueOrDefault("INFO");
            var localizedInfos = localized.RecordsByContextAndId.GetValueOrDefault("INFO");

            if (originalInfos != null)
            {
                foreach (var info in originalInfos)
                {
                    if (info.Value.Type != TextType.Text)
                        continue;
                
                    originalInfosByDialogue.GetOrCreate(info.Value.Meta["dialogue"].ToString())[info.Value.ContextId] =
                        info.Value;
                }
            }
        
            if (localizedInfos != null)
            {
                foreach (var info in localizedInfos)
                {
                    if (info.Value.Type != TextType.Text)
                        continue;
                
                    localizedInfosByDialogue.GetOrCreate(info.Value.Meta["dialogue"].ToString())[info.Value.ContextId] =
                        info.Value;
                }
            }
        
            foreach (var byContext in original.RecordsByContextAndId)
            {
                foreach (var byId in byContext.Value)
                {
                    var record = byId.Value;
                    // if (record.Type != TextType.Text)
                    //     continue;

                    if (record.ContextName == "DIAL")
                    {
                        var dialogueName = record.OriginalText;
                        var mrkRecord = _localizations.DialogueNames.LookupBySource(dialogueName);
                        if (mrkRecord == null)
                            continue;

                        var localizedName = mrkRecord.Target;

                        var localizeds = localizedInfosByDialogue.GetValueOrDefault(localizedName) ?? new();
                        var origs = originalInfosByDialogue.GetValueOrDefault(dialogueName) ?? new();
                        foreach (var origInfo in origs)
                        {
                            var localizedInfo = localizeds.GetValueOrDefault(Regex.Replace(origInfo.Value.ContextId, "^" + Regex.Escape(dialogueName), localizedName));
                            if (localizedInfo == null)
                                continue;
                        
                            _storage.Add(new GlossaryRecord()
                            {
                                ContextId = origInfo.Value.GetUniqId(),
                                ContextName = origInfo.Value.ContextName,
                                MatchType = MatchType.Full,
                                Text = localizedInfo.Text,
                                OriginalText = origInfo.Value.Text,
                                SubContext = origInfo.Value.SubContext,
                            });
                        }
                    
                        _storage.Add(new GlossaryRecord()
                        {
                            ContextId = record.ContextId,
                            ContextName = record.ContextName,
                            MatchType = MatchType.Full,
                            Text = localizedName,
                            OriginalText = dialogueName,
                            SubContext = record.SubContext,
                        });
                    }
                    else if (record.ContextName == "CELL" || record.ContextName == "REGN")
                    {
                        var localizedRecord = localized.Storage.LookupRecord(record);
                        if (localizedRecord == null)
                            continue;

                        string? localizedText = "";
                        if (localizedRecord.Text != record.UnprocessedOriginalText)
                            localizedText = localizedRecord.Text;
                        else
                            localizedText = _localizations.Cells.LookupBySource(record.UnprocessedOriginalText)?.Target;

                        if (!string.IsNullOrEmpty(localizedText))
                        {
                            _storage.Add(new GlossaryRecord()
                            {
                                ContextId = record.GetUniqId(),
                                ContextName = record.ContextName,
                                MatchType = MatchType.Full,
                                Text = localizedText,
                                OriginalText = record.UnprocessedOriginalText,
                                SubContext = record.SubContext,
                            });
                        }
                    }
                    else if (record.ContextName != "INFO")
                    {
                        var loc = localized.Storage.LookupRecord(record);
                        if (loc == null)
                            continue;

                        if (loc.Text == record.Text)
                            continue;
                    
                        _storage.Add(new GlossaryRecord()
                        {
                            ContextId = loc.GetUniqId(),
                            ContextName = loc.ContextName,
                            MatchType = MatchType.Full,
                            Text = loc.Text,
                            OriginalText = record.Text,
                            SubContext = record.SubContext,
                        });
                    }
                }
            }
        }

        private List<TranslationRecord> GetRecordsByDialogue(string dialogue, TranslationState state)
        {
            var infos = state.RecordsByContextAndId.GetValueOrDefault("INFO");
            if (infos == null)
                return new();

            return infos.Where(_ => _.Value.Meta["dialogue"].ToString() == dialogue && _.Value.Type == TextType.Text).Select(_ => _.Value).ToList();
        }

        // private void AddNpcVariants(TranslationCandidatesStorage storage, string variant, string original)
        // {
        //     if (variant.Count(_ => _ == '\'') > 1 && original.Count(_ => _ == '\'') > 1 && variant.Contains(" ") && original.Contains(" "))
        //     {
        //         var parts1 = variant.Split("'", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        //             .ToArray();
        //         var parts2 = original.Split("'", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        //             .ToArray();
        //         if (parts1.Length == parts2.Length)
        //         {
        //             for (var i = 0; i < parts1.Length; ++i)
        //                 AddVariant(storage, parts1[i], parts2[i]);
        //
        //             return;
        //         }
        //     }
        //
        //     var parts3 = variant.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        //         .ToArray();
        //     var parts4 = original.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        //         .ToArray();
        //     if (parts3.Length == parts4.Length)
        //     {
        //         for (var i = 0; i < parts3.Length; ++i)
        //             AddVariant(storage, parts3[i], parts4[i]);
        //     }
        // }
    }
}
