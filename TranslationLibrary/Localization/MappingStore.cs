using System.Collections.Generic;
using TranslationLibrary.Localization.Records;

namespace TranslationLibrary.Localization;

public class MappingStore<T> where T : MappingRecord
{
    private readonly Dictionary<string, T> _bySource = new();
    private readonly Dictionary<string, T> _byTarget = new();

    public Dictionary<string, T> Records => _bySource;

    public bool IsEmpty => _bySource.Count == 0;

    public void Clear()
    {
        _bySource.Clear();
        _byTarget.Clear();
    }

    public void Add(T record)
    {
        _bySource[record.Source] = record;
        _byTarget[record.Target] = record;
    }

    public T? LookupBySource(string source)
    {
        return _bySource.GetValueOrDefault(source);
    }

    public T? LookupByTarget(string source)
    {
        return _byTarget.GetValueOrDefault(source);
    }
}