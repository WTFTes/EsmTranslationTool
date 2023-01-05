namespace TranslationLibrary.Localization.Records;

public class MappingRecord<T>
{
    public MappingType Type { get; set; }
    public T Source { get; set; }
    public T Target { get; set; }
}