using System.Collections.Generic;
using TranslationLibrary.Enums;

namespace TranslationLibrary.Storage.Interfaces;

public interface IStorage<RecordType> : IEnumerable<RecordType>
{
    public void Add(RecordType record, MergeMode mode = MergeMode.Full);
    
    public RecordType? LookupRecord(RecordType record);

    public void Clear();
}
