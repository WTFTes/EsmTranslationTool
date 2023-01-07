using System.Collections;
using System.Collections.Generic;
using TranslationLibrary.Enums;

namespace TranslationLibrary.Storage;

public abstract class AbstractStorage<TRecord> : IEnumerable<TRecord>
{
    public abstract IEnumerable<TRecord> Records { get; }

    public abstract void Add(TRecord record, MergeMode mode = MergeMode.Full);

    public abstract TRecord? LookupRecord(TRecord record);

    public abstract void Clear();

    public abstract void Merge(AbstractStorage<TRecord> other, MergeMode mode = MergeMode.Full);
    
    public abstract int Size { get; }

    public bool Empty => Size == 0;
    
    public abstract IEnumerator<TRecord> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
