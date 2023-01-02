namespace TranslationLibrary.Localization.Records;

public class MappingRecord
{
    public MappingType Type { get; set; }
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
}