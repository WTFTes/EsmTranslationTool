using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TranslationLibrary.Enums;

namespace TranslationLibrary.Translation;

public class TranslationRecord : TextRecord
{
    public string UnprocessedOriginalText { get; set; } = "";
    
    public string OriginalText { get; set; } = "";
    
    public int MaxLength { get; set; }

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

    protected override string GetTextForDump(DumpOptions options) => options.HasFlag(DumpFlags.TranslatedText) ? Text : OriginalText;

    public override JsonObject FormatForDump(DumpFlags optionsFlags)
    {
        var text = optionsFlags.HasFlag(DumpFlags.TranslatedText) ? Text : OriginalText;

        return new JsonObject(new List<KeyValuePair<string, JsonNode?>>() { new(ContextId, text) });;
    }
    
    public override void FromDump(JsonObject obj)
    {
        foreach (var p in obj)
        {
            ContextId = p.Key;
            Text = p.Value.ToString();
        }
    }

    [JsonIgnore]
    public IntPtr Pointer { get; set; } = IntPtr.Zero;

    public bool IsTranslated => Helpers.StripHyperlinks(Text) != Helpers.StripHyperlinks(UnprocessedOriginalText);

    // public override string ContextId
    // {
    //     return !string.IsNullOrEmpty(ContextId) ? $"{ContextId}_{Index}" : $"{OriginalText}_{Index}";
    // }
}