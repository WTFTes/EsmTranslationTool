using System.CommandLine;
using TranslationLibrary;
using TranslationLibrary.Enums;
using TranslationLibrary.Glossary;
using TranslationLibrary.Translation;

namespace TranslationTool.Commands;

public class SaveParameters
{
    public string[] InputEsms { get; set; }
    public string OriginalEncoding { get; set; }
    public string TargetEncoding { get; set; }
    public string[] GlossaryDirectories { get; set; }
    public string TranslationsPath { get; set; }
    public string OutputPath { get; set; }
    public bool TranslateRegions { get; set; }
}

public class Save
{
    public static void Run(SaveParameters parameters, IConsole console)
    {
        try
        {
            GlossaryBuilder glossary = new();
            var translations = Dump.LoadKeyed<EntityRecord>(parameters.TranslationsPath);

            foreach (var glossaryPath in parameters.GlossaryDirectories)
            {
                var storage = Dump.LoadKeyed<GlossaryRecord>(glossaryPath);
                glossary.Storage.Merge(storage, MergeMode.Full);
                console.WriteLine($"Loaded {storage.Records.Count} entries from glossary at '{glossaryPath}'");
            }

            if (parameters.GlossaryDirectories.Length > 0)
                console.WriteLine($"Total glossary records: {glossary.Storage.Records.Count}");

            console.WriteLine($"Loaded {translations.Records.Count} translation texts");
            if (translations.Records.Count == 0)
            {
                Console.WriteLine();
                return;
            }

            var options = SaveOptions.None;
            if (parameters.TranslateRegions)
                options |= SaveOptions.TranslateRegions;
            
            foreach (var esm in parameters.InputEsms)
            {
                var state = new TranslationState();
                try
                {
                    console.WriteLine($"Loading '{Path.GetFileName(esm)}'...");
                    state.Load(esm, parameters.OriginalEncoding);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to load {Path.GetFileName(esm)}: {e.Message}");
                }

                Dictionary<string, RecordStorage<TranslationRecord>.MergedInfo> info;
                if (!glossary.Storage.Empty)
                {
                    console.WriteLine("Merging glossary...");
                    info = state.Storage.MergeGlossary(glossary.Storage);
                    foreach (var item in info)
                        console.WriteLine(
                            $"Merged {item.Value.Matched}/{item.Value.Total} glossary records for '{item.Key}' context, ({item.Value.NotMatched} not matched)");
                }

                console.WriteLine("Merging translations...");
                info = state.Storage.MergeTexts(translations);
                foreach (var item in info)
                    console.WriteLine($"Merged {item.Value.Matched}/{item.Value.Total} translations for '{item.Key}' context");

                try
                {
                    console.WriteLine($"Saving result file to {parameters.OutputPath}...");
                    state.Save(parameters.OutputPath, parameters.TargetEncoding, options);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to save '{Path.GetFileName(esm)}': {e.Message}");
                }
                console.WriteLine("Done");
            }
        }
        catch (Exception e)
        {
            console.WriteLine($"Error: {e.Message}");
        }
    }
}
