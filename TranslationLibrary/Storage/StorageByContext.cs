using System.Collections;
using System.Collections.Generic;
using TranslationLibrary.Enums;
using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary.Storage;

public class StorageByContext<ContextType, ContextGetter, IdType, IdGetter, RecordType> :
    IStorage<RecordType>
    where IdGetter: IIdGetter<IdType>, new()
    where ContextGetter: IIdGetter<ContextType>, new()
{
    private readonly ContextGetter _getterInstance;
    
    public StorageById<ContextType, ContextGetter, StorageById<IdType, IdGetter, RecordType>> RecordsByContext { get; } = new();

    public StorageByContext()
    {
        _getterInstance = new ContextGetter();
    }

    public void Add(RecordType record, MergeMode mode = MergeMode.Full)
    {
        if (mode == MergeMode.None)
            mode = MergeMode.Full;

        var byId = RecordsByContext.RecordsById.GetOrCreate(_getterInstance.Get(record));
        byId.Add(record, mode);
    }

    public void Clear()
    {
        foreach (var byId in RecordsByContext)
            byId.Clear();
        
        RecordsByContext.Clear();
    }

    public RecordType? LookupRecord(RecordType record)
    {
        var byId = RecordsByContext.LookupRecord(_getterInstance.Get(record));
        if (byId == null)
            return default;

        return byId.LookupRecord(record);
    }
    
    public RecordType? LookupRecord(ContextType contextName, IdType contextId)
    {
        var byId = RecordsByContext.LookupRecord(contextName);
        if (byId == null)
            return default;

        return byId.LookupRecord(contextId);
    }

    public IEnumerator<RecordType> GetEnumerator()
    {
        foreach (var byId in RecordsByContext)
        foreach (var record in byId)
            yield return record;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
