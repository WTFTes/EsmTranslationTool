using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using TranslationLibrary.Enums;

namespace TranslationLibrary;

public static class Dump
{
    public class DumpInfo
    {
        public int Skipped = 0;         
        public int Total = 0;
    }
    
    public static Dictionary<string, DumpInfo> DumpStore<T>(string path, RecordStorage<T> storage, string sourceContext,
        DumpOptions options) where T : EntityRecord
    {
        if (!Directory.Exists(path))
            throw new Exception($"Directory '{path}' does not exist");

        Dictionary<string, DumpInfo> stats = new();
        List<string> createdDirectoriesByType = new();
        Dictionary<string, Dictionary<string, List<JsonObject>>> textsByContextAndSubcontext = new();

        Dictionary<string, int> recordsPerSubcontext = new();

        foreach (var (contextName, byContext) in storage.RecordsByContextAndId)
        {
            stats.GetOrCreate(contextName).Total = byContext.Count;
            foreach (var (_, record) in byContext)
            {
                if (record.IsIgnoredForDump(options))
                {
                    ++stats.GetOrCreate(record.ContextName).Skipped;
                    continue;
                }

                var contextPart = !options.HasFlag(DumpFlags.SkipFileContextLevel) ? sourceContext : "";
                var recordDir = Path.Combine(path, contextPart, record.ContextName);

                if (!createdDirectoriesByType.Contains(record.ContextName))
                {
                    if (!Directory.Exists(recordDir))
                        Directory.CreateDirectory(recordDir);

                    createdDirectoriesByType.Add(record.ContextName);
                }

                var overrideType = record.Type;
                if (options.HasFlag(DumpFlags.AllToJson))
                    overrideType = TextType.Text;
                    
                switch (overrideType)
                {
                    case TextType.Script:
                        File.WriteAllText(Path.Combine(recordDir, record.GetUniqId()) + ".mwscript",
                            record.Text);
                        break;
                    case TextType.Html:
                        File.WriteAllText(Path.Combine(recordDir, record.GetUniqId()) + ".mwbook",
                            record.Text);
                        break;
                    case TextType.Text:
                        var subContext = record.SubContext;
                        if (options.RecordsPerFile > 0)
                        {
                            var counterContext = sourceContext + "_" + contextName + "_" + subContext;
                            if (!recordsPerSubcontext.ContainsKey(counterContext))
                                recordsPerSubcontext[counterContext] = 0;

                            if (!string.IsNullOrEmpty(subContext))
                                subContext += "_";

                            var offset = recordsPerSubcontext[counterContext] / options.RecordsPerFile; 
                            subContext +=
                                $"{offset * options.RecordsPerFile + 1}-{(offset + 1) * options.RecordsPerFile}";

                            ++recordsPerSubcontext[counterContext];
                        }

                        textsByContextAndSubcontext.GetOrCreate(record.ContextName).GetOrCreate(subContext).Add(record.FormatForDump(options.Flags));
                        break;
                    default:
                        throw new Exception($"Unknown record text type '{record.Type}'");
                }
            }
        }

        foreach (var (contextName, bySubcontext) in textsByContextAndSubcontext)
        {
            foreach (var (subContext, values) in bySubcontext)
            {
                var contextPart = !options.HasFlag(DumpFlags.SkipFileContextLevel) ? sourceContext : "";
                var recordDir = Path.Combine(path, contextPart, contextName);

                var text = JsonSerializer.Serialize(values, new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true,
                });

                var suffix = subContext;
                if (!string.IsNullOrEmpty(suffix))
                    suffix = "_" + suffix;

                File.WriteAllText(Path.Combine(recordDir, $"texts{suffix}.json"), text);
            }
        }

        return stats;
    }

    public static RecordStorage<T> LoadKeyed<T>(string path) where T : EntityRecord, new()
    {
        if (!Directory.Exists(path))
            throw new Exception($"Path '{path}' does not exist");

        var storage = new RecordStorage<T>();
        var contextDirectories = Directory.GetDirectories(path);
        foreach (var contextDirectory in contextDirectories)
        {
            var files = Directory.GetFiles(contextDirectory);
            foreach (var file in files)
            {
                var records = LoadFile<T>(file, Path.GetFileNameWithoutExtension(contextDirectory));
                foreach (var record in records)
                    storage.Add(record);
            }
        }

        return storage;
    }

    private static List<T> LoadFile<T>(string path, string contextName) where T : EntityRecord, new()
    {
        var extension = Path.GetExtension(path);

        List<T> records = new();
        switch (extension)
        {
            case ".mwscript":
            {
                var record = new T();
                record.ContextName = contextName;
                record.ContextId = Path.GetFileNameWithoutExtension(path);
                record.Text = File.ReadAllText(path);
                record.Type = TextType.Script;

                records.Add(record);
                break;
            }
            case ".mwbook":
            {
                var record = new T();
                record.ContextName = contextName;
                record.ContextId = Path.GetFileNameWithoutExtension(path);
                record.Text = File.ReadAllText(path);
                record.Type = TextType.Html;

                records.Add(record);
                break;
            }
            case ".json":
            {
                var text = File.ReadAllText(path);
                JsonNode? json;
                try
                {
                    json = JsonObject.Parse(text);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to parse json at '{path}': {e.Message}");
                }

                foreach (var obj in json.AsArray())
                {
                    var record = new T
                    {
                        ContextName = contextName,
                        Type = TextType.Text
                    };
                    
                    record.FromDump(obj.AsObject());
                    if (!record.IsValid)
                        throw new Exception($"Failed to parse record in json file '{path}': '{json.ToString()}'");
                    records.Add(record);
                }
                break;
            }
        }

        return records;
    }
}