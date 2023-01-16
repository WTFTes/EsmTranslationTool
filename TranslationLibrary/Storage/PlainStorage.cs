using System.Collections.Generic;
using System.Linq;
using TranslationLibrary.Enums;

namespace TranslationLibrary.Storage;

public class PlainStorage<TRecord> : AbstractStorage<TRecord>
{
    private readonly List<TRecord> _records = new();
    
    public override IEnumerable<TRecord> Records => this.AsEnumerable();

    public override void Add(TRecord record, MergeMode mode = MergeMode.Full)
    {
        if (mode == MergeMode.None)
            mode = MergeMode.Full;

        if (mode.HasFlag(MergeMode.Full) || mode.HasFlag(MergeMode.Missing) && !_records.Contains(record))
            _records.Add(record);
    }

    public override int Size => _records.Count;

    public override void Clear()
    {
        _records.Clear();
    }

    public override void Merge(AbstractStorage<TRecord> other, MergeMode mode = MergeMode.Full)
    {
        foreach (var record in other)
            Add(record, mode);
    }

    public override TRecord? LookupRecord(TRecord record)
    {
        return _records.FirstOrDefault(record);
    }

    public override IEnumerator<TRecord> GetEnumerator() => _records.GetEnumerator();
}
