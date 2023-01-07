using System.CommandLine;
using System.Text.RegularExpressions;
using TranslationLibrary;
using TranslationLibrary.Enums;
using TranslationLibrary.Glossary;
using TranslationLibrary.Storage;
using TranslationLibrary.Translation;

namespace TranslationTool.Commands;

public class ExtractParameters
{
    public string[] InputEsms { get; set; }
    public string Encoding { get; set; }
    public string[] GlossaryDirectories { get; set; }
    public string[]? TypesInclude { get; set; }
    public string[]? TypesSkip { get; set; }
    public TextType[]? TextTypes { get; set; }
    public string OutputPath { get; set; }
    public bool NoMerge { get; set; }
    public bool MarkTopics { get; set; }
    public bool AllScripts { get; set; }
    public bool AllToJson { get; set; }
    public string? RegexMatch { get; set; }
    public string? RegexExclude { get; set; }
    public int RecordsPerFile { get; set; }
}

public static class Extract
{
    public static void Run(ExtractParameters parameters, IConsole console)
    {
        var mergedState = new TranslationState();
        try
        {
            GlossaryBuilder glossary = new();
            foreach (var glossaryPath in parameters.GlossaryDirectories)
            {
                GlossaryStorage storage = new();
                try
                {
                    storage.LoadKeyed(glossaryPath);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to load glossary at '{glossaryPath}': {e.Message}");
                }

                glossary.Storage.Merge(storage, MergeMode.Full);
                console.WriteLine($"Loaded {storage.Size} entries from glossary at '{glossaryPath}'");
            }

            if (parameters.GlossaryDirectories.Length > 0)
                console.WriteLine($"Total glossary records: {glossary.Storage.Size}");

            var flags = DumpFlags.SkipTranslated;
            if (parameters.MarkTopics)
                flags |= DumpFlags.IncludeFullTopics;
            if (parameters.AllScripts)
                flags |= DumpFlags.AllScripts;
            if (parameters.AllToJson)
                flags |= DumpFlags.AllToJson;

            DumpOptions options = new() { Flags = flags };
            if (parameters.TypesInclude?.Length > 0)
                options.TypesInclude.AddRange(parameters.TypesInclude);
            if (parameters.TypesSkip?.Length > 0)
                options.TypesSkip.AddRange(parameters.TypesSkip);
            if (parameters.TextTypes?.Length > 0)
                options.TextTypes.AddRange(parameters.TextTypes);

            try
            {
                if (!string.IsNullOrWhiteSpace(parameters.RegexMatch))
                    options.TextMatchRegex = new Regex(parameters.RegexMatch);
            }
            catch (Exception e)
            {
                throw new Exception($"Regexp '{parameters.RegexMatch}' parse failed: {e.Message}");
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(parameters.RegexExclude))
                    options.TextSkipRegex = new Regex(parameters.RegexExclude);
            }
            catch (Exception e)
            {
                throw new Exception($"Regexp '{parameters.RegexExclude}' parse failed: {e.Message}");
            }

            options.RecordsPerFile = parameters.RecordsPerFile;

            var totalRecords = 0;
            foreach (var esm in parameters.InputEsms)
            {
                var state = new TranslationState();
                try
                {
                    console.WriteLine($"Loading '{Path.GetFileName(esm)}'...");
                    state.Load(esm, parameters.Encoding);
                    console.WriteLine($"Loaded {state.Storage.Size} texts from '{Path.GetFileName(esm)}'");
                    totalRecords += state.Storage.Size;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to load {esm}: {e.Message}");
                }

                var info = state.Storage.MergeGlossary(glossary.Storage);
                foreach (var item in info)
                    console.WriteLine(
                        $"{item.Value.Matched}/{item.Value.Total} records matched to glossaries and will be excluded from extraction for '{item.Key}' context, ({item.Value.NotMatched} not matched)");

                if (parameters.GlossaryDirectories.Length > 0)
                    console.WriteLine(
                        $"Texts matched to glossaries: " + state.Records.Count(_ => _.IsTranslated));

                state.PrepareRecordsForDump(options);

                if (!parameters.NoMerge)
                    mergedState.Merge(state);
                else
                {
                    var dumpPath = Path.Combine(parameters.OutputPath);
                    if (!Directory.Exists(dumpPath))
                        Directory.CreateDirectory(dumpPath);

                    console.WriteLine($"Extracting texts from '{Path.GetFileName(esm)}' to '{parameters.OutputPath}'...");
                    try
                    {
                        var stats = state.Storage.DumpStore(dumpPath, Path.GetFileNameWithoutExtension(esm),
                            options);

                        console.WriteLine($"Extracted for '{Path.GetFileName(esm)}' - {StatsAsString(stats)}");
                    }
                    catch (Exception e)
                    {
                        console.WriteLine($"Failed to extract texts: {e.Message}");
                    }
                }
            }

            if (!parameters.NoMerge)
            {
                var dumpPath = Path.Combine(parameters.OutputPath);
                if (!Directory.Exists(dumpPath))
                    Directory.CreateDirectory(dumpPath);

                if (parameters.InputEsms.Length > 1)
                    console.WriteLine($"{totalRecords} texts merged to {mergedState.Storage.Size}");

                console.WriteLine($"Extracting texts to '{parameters.OutputPath}'...");
                try
                {
                    var stats = mergedState.Storage.DumpStore(dumpPath, "", options);
                    console.WriteLine($"Extracted - {StatsAsString(stats)}");
                }
                catch (Exception e)
                {
                    console.WriteLine($"Failed to extract texts: {e.Message}");
                }
            }

            console.WriteLine("Done");
        }
        catch (Exception e)
        {
            console.WriteLine($"Error: {e.Message}");
        }
        finally
        {
            mergedState.Dispose();
        }
    }

    private static string StatsAsString(Dictionary<string, TextStorage<TranslationRecord>.DumpInfo> stats)
    {
        var byContext = string.Join(", ", stats
            //.Select(_ => new KeyValuePair<string, Dump.DumpInfo>(_.Key, _.Value))
            .OrderBy(_ => _.Key)
            .Where(_ => _.Value.Skipped != _.Value.Total)
            .Select(_ =>
            {
                if (_.Value.Skipped == 0)
                    return $"'{_.Key}': {_.Value.Total}";

                return $"'{_.Key}': {_.Value.Total - _.Value.Skipped}/{_.Value.Total}";
            }));

        var skipped = stats.Sum(_ => _.Value.Skipped);
        var total = stats.Sum(_ => _.Value.Total);

        if (!string.IsNullOrEmpty(byContext))
            byContext = $", {byContext}";

        if (skipped == 0)
            return $"total {total}, skipped {skipped}{byContext}";

        return $"total {total - skipped}/{total}, skipped {skipped}{byContext}";
    }
}
