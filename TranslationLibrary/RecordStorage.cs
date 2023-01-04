using System.Collections.Generic;
using TranslationLibrary.Enums;
using TranslationLibrary.Glossary;

namespace TranslationLibrary;

public class RecordStorage<T> where T : EntityRecord
{
    private readonly List<T> _records = new();
    private readonly Dictionary<string, Dictionary<string, T>> _recordsByContextAndId = new();

    public List<T> Records => _records;
    public Dictionary<string, Dictionary<string, T>> RecordsByContextAndId => _recordsByContextAndId;

    public bool Empty => _records.Count == 0;

    public void Add(T record, MergeMode mode = MergeMode.Full)
    {
        if (mode == MergeMode.None)
            mode = MergeMode.Full;

        var values = _recordsByContextAndId.GetOrCreate(record.ContextName);
        if (mode.HasFlag(MergeMode.Full))
        {
            if (!values.ContainsKey(record.GetUniqId()))
                _records.Add(record);

            values[record.GetUniqId()] = record;
            return;
        }

        if (mode.HasFlag(MergeMode.Missing) && !values.ContainsKey(record.GetUniqId()))
        {
            values[record.GetUniqId()] = record;
            _records.Add(record);
            return;
        }

        if (mode.HasFlag(MergeMode.Text) && values.ContainsKey(record.GetUniqId()))
        {
            values[record.GetUniqId()].Text = record.Text;
            return;
        }
    }

    public void Clear()
    {
        _records.Clear();
        _recordsByContextAndId.Clear();
    }

    public void Merge(RecordStorage<T> other, MergeMode mode)
    {
        foreach (var record in other._records)
            Add(record, mode);
    }

    public class MergedInfo
    {
        public int Total = 0;
        public int Matched = 0;
        public int NotMatched = 0;
    }
    
    /// <summary>
    /// Merges all text from storage to source
    /// </summary>
    /// <param name="other"></param>
    /// <typeparam name="TV"></typeparam>
    /// <returns></returns>
    public Dictionary<string, MergedInfo> MergeTexts<TV>(RecordStorage<TV> other) where TV : EntityRecord
    {
        Dictionary<string, MergedInfo> result = new();
        foreach (var record in other._records)
        {
            ++result.GetOrCreate(record.ContextName).Total;
            
            var origRecord = LookupRecord(record.ContextName, record.GetUniqId());
            if (origRecord == null)
                continue;

            origRecord.Text = record.Text;
            ++result.GetOrCreate(record.ContextName).Matched;
        }

        return result;
    }

    /// <summary>
    /// Merges texts from storage to source only of original text matches
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public Dictionary<string, MergedInfo> MergeGlossary(RecordStorage<GlossaryRecord> other)
    {
        Dictionary<string, MergedInfo> result = new();
        foreach (var record in other._records)
        {
            var origRecord = LookupRecord(record.ContextName, record.GetUniqId());
            if (origRecord == null)
                continue;
            
            ++result.GetOrCreate(record.ContextName).Total;

            if (origRecord.Text == record.OriginalText)
            {
                ++result.GetOrCreate(origRecord.ContextName).Matched;
                origRecord.Text = record.Text;
            }
            else if (origRecord.Text != record.Text)
                ++result.GetOrCreate(origRecord.ContextName).NotMatched;
        }

        return result;
    }

    public T? LookupRecord(T record)
    {
        return _recordsByContextAndId.GetValueOrDefault(record.ContextName)
            ?.GetValueOrDefault(record.GetUniqId());
    }

    public T? LookupRecord(string contextName, string contextId)
    {
        return _recordsByContextAndId.GetValueOrDefault(contextName)
            ?.GetValueOrDefault(contextId);
    }
}
