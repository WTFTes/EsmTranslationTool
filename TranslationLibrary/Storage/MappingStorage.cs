using TranslationLibrary.Enums;
using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary.Storage;

public class MappingStorage<IdType, IdGetter, SecondaryIdType, SecondaryIdGetter, RecordType> : StorageById<IdType, IdGetter, RecordType>
    where IdGetter : IIdGetter<IdType>, new()
    where SecondaryIdGetter : IIdGetter<SecondaryIdType>, new()
{
    public StorageById<SecondaryIdType, SecondaryIdGetter, RecordType> RecordsBySecondaryId { get; } = new();

    public override void Add(RecordType record, MergeMode mode = MergeMode.Full)
    {
        RecordsBySecondaryId.Add(record, mode);
        base.Add(record, mode);
    }

    public override void Clear()
    {
        RecordsById.Clear();
        RecordsBySecondaryId.Clear();
    }

    public RecordType? LookupRecordByTarget(RecordType record)
    {
        return RecordsBySecondaryId.LookupRecord(record);
    }

    public RecordType? LookupRecordByTarget(SecondaryIdType id)
    {
        return RecordsBySecondaryId.LookupRecord(id);
    }
}