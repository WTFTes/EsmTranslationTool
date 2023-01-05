using TranslationLibrary.Localization.Records;
using TranslationLibrary.Storage;
using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary.Localization;

public class SourceIdGetter : IIdGetter<string>
{
    public string Get<TV>(TV record)
    {
        return (record as MappingRecord<string>).Source;
    }
}

public class TargetIdGetter : IIdGetter<string>
{
    public string Get<TV>(TV record)
    {
        return (record as MappingRecord<string>).Target;
    }
}

public class LocalizationMappingStore : MappingStorage<string, SourceIdGetter, string, TargetIdGetter, MappingRecord<string>>
{
}