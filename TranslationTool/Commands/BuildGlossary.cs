using System.CommandLine;
using TranslationLibrary.Enums;
using TranslationLibrary.Glossary;
using TranslationLibrary.Translation;

namespace TranslationTool.Commands;

public class BuildGlossaryParameters
{
    public string[] OriginalEsms { get; set; }
    public string OriginalEncoding { get; set; }
    public string[] LocalizedEsms { get; set; }
    public string LocalizedEncoding { get; set; }
    public string OutputPath { get; set; }
    public bool MarkTopics { get; set; }
    public bool Plain { get; set; }
}

public static class BuildGlossary
{
    public static void Run(BuildGlossaryParameters parameters, IConsole console)
    {
        try
        {
            var builder = new GlossaryBuilder();

            var dumpFlags = DumpFlags.AllToJson | DumpFlags.SkipFileContextLevel;
            if (parameters.MarkTopics)
                dumpFlags = DumpFlags.IncludeFullTopics;

            var recordsCount = 0;
            for (var i = 0; i < parameters.OriginalEsms.Length; ++i)
            {
                var origState = new TranslationState();
                var localizedState = new TranslationState();

                console.WriteLine($"Loading original '{Path.GetFileName(parameters.OriginalEsms[i])}'...");
                origState.Load(parameters.OriginalEsms[i], parameters.OriginalEncoding);
                origState.PrepareRecordsForDump(new() { Flags = dumpFlags });
                console.WriteLine($"Loading localized '{Path.GetFileName(parameters.LocalizedEsms[i])}'...");
                localizedState.Load(parameters.LocalizedEsms[i], parameters.LocalizedEncoding);

                builder.Build(origState, localizedState);
                console.WriteLine(
                    $"Generated {builder.Storage.Size - recordsCount} records from original {Path.GetFileName(parameters.OriginalEsms[i])} and localized {Path.GetFileName(parameters.LocalizedEsms[i])}");
                recordsCount = builder.Storage.Size;
            }

            if (!Directory.Exists(parameters.OutputPath))
                Directory.CreateDirectory(parameters.OutputPath);

            console.WriteLine($"Dumping total {builder.Storage.Size} records to {parameters.OutputPath}");
            if (parameters.Plain)
                builder.Storage.DumpCsv(Path.Combine(parameters.OutputPath, "glossary.csv"));
            else
                builder.Storage.DumpStore(parameters.OutputPath, "",
                    new() { Flags = dumpFlags });

            console.WriteLine("Done");
        }
        catch (Exception e)
        {
            console.WriteLine($"Error: {e.Message}");
        }
    }
}