using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary.Localization.Records;

public class MappingRecord<T> : IRecordWithId<T>, IRecordWithSecondaryId<T>
{
    public MappingType Type { get; set; }
    public T Source { get; set; }
    public T Target { get; set; }
    
    public T GetId()
    {
        return Source;
    }

    public T GetSecondaryId()
    {
        return Target;
    }
}