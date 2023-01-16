using System.Collections.Generic;
using System.Linq;
using TranslationLibrary.Enums;
using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary.Storage;

public class StorageById<TId, TRecord> : AbstractStorage<TRecord>
    where TRecord: IRecordWithId<TId>
{
    public Dictionary<TId, TRecord> RecordsById { get; } = new();

    public override IEnumerable<TRecord> Records => this.AsEnumerable();

    public override void Add(TRecord record, MergeMode mode = MergeMode.Full)
    {
        if (mode == MergeMode.None)
            mode = MergeMode.Full;

        if (mode.HasFlag(MergeMode.Full) || mode.HasFlag(MergeMode.Missing) && !RecordsById.ContainsKey(record.GetId()))
        {
            RecordsById[record.GetId()] = record;
        }
    }

    public override int Size => RecordsById.Count;

    public override void Clear()
    {
        RecordsById.Clear();
    }

    public override void Merge(AbstractStorage<TRecord> other, MergeMode mode = MergeMode.Full)
    {
        foreach (var record in other)
            Add(record, mode);
    }

    public override TRecord? LookupRecord(TRecord record)
    {
        return LookupRecord(record.GetId());
    }

    public TRecord? LookupRecord(TId id)
    {
        return RecordsById.GetValueOrDefault(id);
    }

    public bool HasRecord(TId id) => LookupRecord(id) != null;
    
    public override IEnumerator<TRecord> GetEnumerator()
    {
        foreach (var (_, record) in RecordsById)
            yield return record;
    }
}
