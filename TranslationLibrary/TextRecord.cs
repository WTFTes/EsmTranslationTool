using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TranslationLibrary.Enums;
using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary;

public abstract class TextRecord : IRecordWithContext<string>, IRecordWithId<string>, IDumpable
{
    public string ContextId { get; set; } = "";
    public string ContextName { get; set; } = "";
    public string Text { get; set; } = "";
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TextType Type { get; set; }

    public virtual string SubContext { get; set; } = "";
    
    public virtual bool IsIgnoredForDump(DumpOptions options)
    {
        var text = GetTextForDump(options);
        if (text.Trim() == "" || text.ToLowerInvariant().Trim() == "<deprecated>")
            return true;

        if (!options.NeedDumpText(text))
            return true;
        
        if (Type == TextType.Script && !options.HasFlag(DumpFlags.AllScripts) && !Helpers.ScriptCanBeTranslated(text))
            return true;
        
        if (!options.NeedDumpTextType(Type))
            return true;
        
        if (!options.NeedDumpContext(ContextName))
            return true;

        if (options.HasFlag(DumpFlags.TextOnly) && Type != TextType.Text)
            return true;

        return false;
    }

    public abstract JsonObject FormatForDump(DumpFlags optionsFlags);

    public abstract void FromDump(JsonObject obj);

    protected virtual string GetTextForDump(DumpOptions options) => Text;

    public bool IsValid => !string.IsNullOrEmpty(ContextId) && !string.IsNullOrEmpty(Text);

    public string GetContext()
    {
        return ContextName;
    }

    public string GetId()
    {
        return ContextId;
    }
}
