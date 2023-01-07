using System.Text.Json.Nodes;
using TranslationLibrary.Enums;

namespace TranslationLibrary.Storage.Interfaces;

public interface IDumpable
{
    public bool IsIgnoredForDump(DumpOptions options);
    
    public JsonObject FormatForDump(DumpFlags optionsFlags);

    public void FromDump(JsonObject obj);
    
    public bool IsValid { get; }
}
