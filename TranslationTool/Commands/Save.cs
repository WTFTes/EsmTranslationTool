using System.CommandLine;
using TranslationLibrary.Enums;
using TranslationLibrary.Glossary;
using TranslationLibrary.Storage;
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
            TextStorage<TranslationRecord> translations = new();
            translations.LoadKeyed(parameters.TranslationsPath);

            foreach (var glossaryPath in parameters.GlossaryDirectories)
            {
                glossary.Storage.LoadKeyed(glossaryPath);
                console.WriteLine($"Loaded {glossary.Storage.Size} entries from glossary at '{glossaryPath}'");
            }

            if (parameters.GlossaryDirectories.Length > 0)
                console.WriteLine($"Total glossary records: {glossary.Storage.Size}");

            console.WriteLine($"Loaded {translations.Size} translation texts");
            if (translations.Size == 0)
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

                Dictionary<string, TextStorage<TranslationRecord>.MergedInfo> info;
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
