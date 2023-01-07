using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TranslationLibrary.Enums;
using TranslationLibrary.Storage;
using TranslationLibrary.Translation;

namespace TranslationLibrary.Glossary
{
    public class GlossaryBuilder
    {
        public GlossaryStorage Storage { get; } = new();

        public void Build(TranslationState original, TranslationState localized, BuildMode mode = BuildMode.Merge)
        {
            BuildExactMatches(original, localized, mode);
            BuildOtherMatches(original, localized, mode);
        }

        private void BuildOtherMatches(TranslationState original, TranslationState localized,
            BuildMode mode = BuildMode.Merge)
        {
            BuildNpcMatches(original, localized, mode);
        }

        private void BuildExactMatches(TranslationState original, TranslationState localized,
            BuildMode mode = BuildMode.Merge)
        {
            Dictionary<string, Dictionary<string, TranslationRecord>> originalInfosByDialogue = new();
            Dictionary<string, Dictionary<string, TranslationRecord>> localizedInfosByDialogue = new();

            var originalInfos = original.Storage.LookupContext("INFO");
            var localizedInfos = localized.Storage.LookupContext("INFO");

            if (originalInfos != null)
            {
                foreach (var info in originalInfos)
                {
                    if (info.Type != TextType.Text)
                        continue;

                    originalInfosByDialogue.GetOrCreate(info.Meta["dialogue"].ToString())[info.ContextId] =
                        info;
                }
            }

            if (localizedInfos != null)
            {
                foreach (var info in localizedInfos)
                {
                    if (info.Type != TextType.Text)
                        continue;

                    localizedInfosByDialogue.GetOrCreate(info.Meta["dialogue"].ToString())[info.ContextId] =
                        info;
                }
            }

            foreach (var record in original.Storage)
            {
                var loc = localized.Storage.LookupRecord(record);
                if (loc == null)
                    continue;

                if (loc.Text == record.Text)
                    continue;

                Storage.Add(new GlossaryRecord()
                {
                    ContextId = loc.ContextId,
                    ContextName = loc.ContextName,
                    MatchType = MatchType.Full,
                    Text = loc.OriginalText,
                    OriginalText = record.OriginalText,
                    SubContext = record.SubContext,
                });
            }
        }

        private void BuildNpcMatches(TranslationState original, TranslationState localized,
            BuildMode mode = BuildMode.Merge)
        {
            var npcs = original.Storage.LookupContext("NPC_");
            if (npcs == null)
                return;

            foreach (var record in npcs)
            {
                var loc = localized.Storage.LookupRecord(record);
                if (loc == null)
                    continue;

                AddNpcVariants(record, loc);
            }
        }

        private void AddNpcVariants(TranslationRecord original, TranslationRecord localized)
        {
            var originalText = original.OriginalText;
            var localizedText = localized.OriginalText;

            if (localizedText.Count(_ => _ == '\'') > 1 && originalText.Count(_ => _ == '\'') > 1 &&
                localizedText.Contains(" ") &&
                originalText.Contains(" "))
            {
                var localizedParts1 = localizedText
                    .Split("'", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToArray();
                var originalParts1 = originalText
                    .Split("'", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToArray();
                if (localizedParts1.Length == originalParts1.Length && localizedParts1.Length > 1)
                {
                    for (var i = 0; i < localizedParts1.Length; ++i)
                    {
                        Storage.Add(new GlossaryRecord()
                        {
                            ContextId = original.ContextId,
                            ContextName = original.ContextName,
                            MatchType = MatchType.Partial,
                            Text = localizedParts1[i],
                            OriginalText = originalParts1[i],
                            SubContext = original.SubContext,
                        });
                    }

                    return;
                }
            }

            var localizedParts2 = localizedText
                .Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
            var originalParts2 = originalText
                .Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
            if (localizedParts2.Length == originalParts2.Length && localizedParts2.Length > 1)
            {
                for (var i = 0; i < localizedParts2.Length; ++i)
                    Storage.Add(new GlossaryRecord()
                    {
                        ContextId = $"{original.ContextId}_partial_{i}",
                        ContextName = original.ContextName,
                        MatchType = MatchType.Partial,
                        Text = localizedParts2[i],
                        OriginalText = originalParts2[i],
                        SubContext = original.SubContext,
                    });
            }
        }
    }
}
