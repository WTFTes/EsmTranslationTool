using System.Collections.Generic;
using System.Linq;
using TranslationLibrary.Enums;
using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary.Storage;

public class StorageByContext<TContext, TId, TRecord> :
    AbstractStorage<TRecord>
    where TRecord : IRecordWithContext<TContext>, IRecordWithId<TId>
{
    public class ContextIdStorageWrapper : StorageById<TId, TRecord>, IRecordWithId<TContext>
    {
        private readonly TContext _contextId;

        public ContextIdStorageWrapper(TContext contextId)
        {
            _contextId = contextId;
        }

        public TContext GetId()
        {
            return _contextId;
        }
    }
    
    private readonly StorageById<TContext, ContextIdStorageWrapper> _storage =
        new();

    public override IEnumerable<TRecord> Records
    {
        get { return _storage.SelectMany(byId => byId); }
    }

    public override int Size => _storage.Sum(_ => _.Size);

    public IEnumerable<KeyValuePair<TContext, ContextIdStorageWrapper>>
        RecordsByContext =>
        _storage.RecordsById;

    public override void Add(TRecord record, MergeMode mode = MergeMode.Full)
    {
        if (mode == MergeMode.None)
            mode = MergeMode.Full;

        var byId = _storage.LookupRecord(record.GetContext());
        if (byId != null)
        {
            byId.Add(record, mode);
            return;
        }

        _storage.Add(new ContextIdStorageWrapper(record.GetContext()) { { record, mode } });
    }

    public override TRecord? LookupRecord(TRecord record)
    {
        var byId = _storage.LookupRecord(record.GetContext());
        if (byId == null)
            return default;

        return byId.LookupRecord(record);
    }

    public StorageById<TId, TRecord>? LookupContext(TContext contextId)
    {
        return _storage.LookupRecord(contextId);
    }

    public override void Clear()
    {
        _storage.Clear();
    }

    public override void Merge(AbstractStorage<TRecord> other, MergeMode mode = MergeMode.Full)
    {
        foreach (var record in other.Records)
            Add(record, mode);
    }

    public TRecord? LookupRecord(TContext contextName, TId contextId)
    {
        var byId = _storage.LookupRecord(contextName);
        if (byId == null)
            return default;

        return byId.LookupRecord(contextId);
    }

    public override IEnumerator<TRecord> GetEnumerator()
    {
        foreach (var byContext in _storage)
        foreach (var record in byContext)
            yield return record;
    }
}
