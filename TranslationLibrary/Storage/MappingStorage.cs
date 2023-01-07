using TranslationLibrary.Enums;
using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary.Storage;

public class MappingStorage<TSource, TTarget, TRecord> : StorageById<TSource, TRecord>
    where TRecord : IRecordWithId<TSource>, IRecordWithSecondaryId<TTarget>
{
    private class SecondaryIdWrapper : IRecordWithId<TTarget>
    {
        public TRecord Content { get; }

        public SecondaryIdWrapper(TRecord content)
        {
            Content = content;
        }

        public TTarget GetId()
        {
            return Content.GetSecondaryId();
        }
    }
    
    private readonly StorageById<TTarget, SecondaryIdWrapper> _recordsBySecondaryId = new();

    public override void Add(TRecord record, MergeMode mode = MergeMode.Full)
    {
        _recordsBySecondaryId.Add(new SecondaryIdWrapper(record), mode);
        base.Add(record, mode);
    }

    public override void Clear()
    {
        base.Clear();
        _recordsBySecondaryId.Clear();
    }

    public TRecord? LookupRecordByTarget(TRecord record)
    {
        var result = _recordsBySecondaryId.LookupRecord(new SecondaryIdWrapper(record));

        return result != null ? result.Content : default;
    }

    public TRecord? LookupRecordByTarget(TTarget id)
    {
        var result = _recordsBySecondaryId.LookupRecord(id);

        return result != null ? result.Content : default;
    }
}