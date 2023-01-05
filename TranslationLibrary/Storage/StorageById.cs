using System.Collections;
using System.Collections.Generic;
using TranslationLibrary.Enums;
using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary.Storage;

public class StorageById<IdType, IdGetter, RecordType> : IStorage<RecordType> where IdGetter: IIdGetter<IdType>, new()
{
    protected readonly IdGetter _getterInstance;
    
    public Dictionary<IdType, RecordType> RecordsById { get; } = new();

    public StorageById()
    {
        _getterInstance = new IdGetter();
    }

    public virtual void Add(RecordType record, MergeMode mode = MergeMode.Full)
    {
        if (mode == MergeMode.None)
            mode = MergeMode.Full;

        var id = _getterInstance.Get(record);
        if (mode.HasFlag(MergeMode.Full) || mode.HasFlag(MergeMode.Missing) && !RecordsById.ContainsKey(id))
        {
            RecordsById[id] = record;
            return;
        }
    }

    public int Size => RecordsById.Count;
    public bool IsEmpty => RecordsById.Count == 0;

    public virtual void Clear()
    {
        RecordsById.Clear();
    }

    public RecordType? LookupRecord(RecordType record)
    {
        return LookupRecord(_getterInstance.Get(record));
    }

    public RecordType? LookupRecord(IdType id)
    {
        return RecordsById.GetValueOrDefault(id);
    }

    public IEnumerator<RecordType> GetEnumerator()
    {
        foreach (var (_, record) in RecordsById)
            yield return record;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}