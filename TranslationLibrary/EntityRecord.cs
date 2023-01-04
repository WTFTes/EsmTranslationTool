using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TranslationLibrary.Enums;

namespace TranslationLibrary;

public class EntityRecord
{
    public string ContextId { get; set; } = "";
    public string ContextName { get; set; } = "";
    public virtual string Text { get; set; } = "";
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TextType Type { get; set; }

    public virtual string SubContext { get; set; } = "";
    
    public virtual bool IsIgnoredForDump(DumpOptions options)
    {
        if (Text.Trim() == "" || Text.ToLowerInvariant().Trim() == "<deprecated>")
            return true;

        if (!options.NeedDumpTextType(Type))
            return true;
        
        if (!options.NeedDumpContext(ContextName))
            return true;

        if (!options.NeedDumpText(Text))
            return true;
        
        if (options.HasFlag(DumpFlags.TextOnly) && Type != TextType.Text)
            return true;
        
        if (Type == TextType.Script && !options.HasFlag(DumpFlags.AllScripts) && !Helpers.ScriptCanBeTranslated(Text))
            return true;

        return false;
    }

    public bool IsValid => !string.IsNullOrEmpty(ContextId) && !string.IsNullOrEmpty(Text);

    public virtual string GetUniqId()
    {
        return !string.IsNullOrEmpty(ContextId) ? ContextId : Text;
    }

    public virtual JsonObject FormatForDump(DumpFlags optionsFlags)
    {
        return new JsonObject(new List<KeyValuePair<string, JsonNode?>>() { new(GetUniqId(), Text) });
    }

    public virtual void FromDump(JsonObject obj)
    {
        foreach (var p in obj)
        {
            ContextId = p.Key;
            Text = p.Value.ToString();
        }
    }
}
