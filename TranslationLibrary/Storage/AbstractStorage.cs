using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using TranslationLibrary.Enums;

namespace TranslationLibrary.Storage;

public abstract class AbstractStorage<TRecord> : IEnumerable<TRecord>
{
    public abstract IEnumerable<TRecord> Records { get; }

    public abstract void Add(TRecord record, MergeMode mode = MergeMode.Full);

    public abstract TRecord? LookupRecord(TRecord record);

    public bool HasRecord(TRecord record) => LookupRecord(record) != null;

    public abstract void Clear();

    public abstract void Merge(AbstractStorage<TRecord> other, MergeMode mode = MergeMode.Full);

    public abstract int Size { get; }

    public bool Empty => Size == 0;

    public abstract IEnumerator<TRecord> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void DumpPlain(string path)
    {
        Dump(path, this);
    }

    public void LoadPlain(string filePath)
    {
        var json = LoadJson(filePath);
        if (json == null)
            return;

        foreach (var obj in json.AsArray())
            Add(JsonSerializer.Deserialize<TRecord>(obj));
    }

    protected JsonNode? LoadJson(string filePath)
    {
        var text = File.ReadAllText(filePath);
        JsonNode json;
        try
        {
            json = JsonObject.Parse(text);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to parse json at '{filePath}': {e.Message}");
        }

        return json;
    }

    protected void Dump(string filePath, IEnumerable records)
    {
        var text = JsonSerializer.Serialize(records, new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
        });

        File.WriteAllText(filePath, text);
    }
}
