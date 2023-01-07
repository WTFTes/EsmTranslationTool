using System.Collections.Generic;
using TranslationLibrary.Glossary;
using TranslationLibrary.Storage;

namespace TranslationLibrary.Translation;

public class TranslationStorage : TextStorage<TranslationRecord>
{
    /// <summary>
    /// Merges texts from storage to source only of original text matches
    /// </summary>
    /// <param name="glossary"></param>
    /// <returns></returns>
    public Dictionary<string, MergedInfo> MergeGlossary(GlossaryStorage glossary)
    {
        Dictionary<string, MergedInfo> result = new();
        foreach (var record in glossary)
        {
            var origRecord = LookupRecord(record.ContextName, record.ContextId);
            if (origRecord == null)
                continue;
            
            ++result.GetOrCreate(record.ContextName).Total;

            if (origRecord.Text == record.OriginalText)
            {
                ++result.GetOrCreate(origRecord.ContextName).Matched;
                origRecord.Text = record.Text;
            }
            else if (origRecord.Text != record.Text)
                ++result.GetOrCreate(origRecord.ContextName).NotMatched;
        }

        return result;
    }
}
