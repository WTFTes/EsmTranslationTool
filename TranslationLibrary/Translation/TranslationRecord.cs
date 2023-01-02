using System;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TranslationLibrary.Enums;

namespace TranslationLibrary.Translation;

public class TranslationRecord : EntityRecord
{
    public string UnprocessedOriginalText { get; set; } = "";
    
    public string OriginalText { get; set; } = "";

    public JsonNode? Meta { get; set; }

    public override string SubContext
    {
        get
        {
            if (ContextName == "NPC_")
            {
                var race = "EMPTY";
                if (Meta != null)
                    race = Meta["race"]?.ToString();

                return race.Replace(" ", "_").ToUpperInvariant();
            }

            return "";
        }
    }

    public override bool IsIgnoredForDump(DumpOptions options)
    {
        if (options.HasFlag(DumpFlags.SkipTranslated) && IsTranslated)
            return true;

        if (options.HasFlag(DumpFlags.SkipUntranslated) && !IsTranslated)
            return true;

        return base.IsIgnoredForDump(options);
    }

    [JsonIgnore]
    public IntPtr Pointer { get; set; } = IntPtr.Zero;

    public bool IsTranslated => Helpers.StripHyperlinks(Text) != Helpers.StripHyperlinks(UnprocessedOriginalText);

    // public override string GetUniqId()
    // {
    //     return !string.IsNullOrEmpty(ContextId) ? $"{ContextId}_{Index}" : $"{OriginalText}_{Index}";
    // }
}