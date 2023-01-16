using System.CommandLine;
using System.Text;
using TranslationLibrary.Dialogue;
using TranslationLibrary.Enums;
using TranslationLibrary.Glossary;
using TranslationLibrary.Localization;
using TranslationLibrary.Localization.Records;
using TranslationLibrary.Storage;
using TranslationLibrary.Translation;

namespace TranslationTool.Commands;

public class SaveParameters
{
    public string[] InputEsms { get; set; }
    public string OriginalEncoding { get; set; }
    public string TargetEncoding { get; set; }
    public string[] GlossaryDirectories { get; set; }
    public string LocalizationsDirectory { get; set; }
    public string DialoguesDirectory { get; set; }
    public string TranslationsPath { get; set; }
    public bool ValidateOnly { get; set; }
    public string Phraseforms { get; set; }
    public bool DumpPhraseforms { get; set; }
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
            translations.Load(parameters.TranslationsPath);

            foreach (var glossaryPath in parameters.GlossaryDirectories)
            {
                glossary.Storage.Load(glossaryPath);
                console.WriteLine($"Loaded {glossary.Storage.Size} entries from glossary at '{glossaryPath}'");
            }

            if (parameters.GlossaryDirectories.Length > 0)
                console.WriteLine($"Total glossary records: {glossary.Storage.Size}");

            console.WriteLine($"Loaded {translations.Size} translation texts");
            
            LocalizationStorage phraseForms = new();
            if (!string.IsNullOrEmpty(parameters.Phraseforms))
            {
                phraseForms.LoadNativeLocalization(parameters.Phraseforms, MappingType.Phraseform,
                    Encoding.Default);
                console.WriteLine($"Loaded {phraseForms.Size} phraseforms for dialogues");
            }

            var options = SaveOptions.None;
            if (parameters.TranslateRegions)
                options |= SaveOptions.TranslateRegions;

            LocalizationStorage externalLocalizations = new();
            if (!string.IsNullOrEmpty(parameters.LocalizationsDirectory))
                externalLocalizations.Load(parameters.LocalizationsDirectory);
            
            DialogueHelper dialogueHelper = new();
            if (!string.IsNullOrEmpty(parameters.DialoguesDirectory))
                dialogueHelper.Load(parameters.DialoguesDirectory);
            
            foreach (var esm in parameters.InputEsms)
            {
                var state = new TranslationState();
                if (!dialogueHelper.Dialogues.Empty)
                    state.DialogueHelper = dialogueHelper;

                if (!externalLocalizations.IsEmpty)
                    state.BaseLocalizations = externalLocalizations;
                
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
                info = state.MergeTranslations(translations, phraseForms);
                foreach (var item in info)
                    console.WriteLine(
                        $"Merged {item.Value.Matched}/{item.Value.Total} translations for '{item.Key}' context");

                state.TranslationPostProcess();
                
                var analyzeResult = state.Analyze();
                var problemsStorage = analyzeResult.Problems;
                if (problemsStorage.IsEmpty)
                    console.WriteLine("No problems found");
                else
                {
                    var problems = problemsStorage.GetProblems().ToList();
                    console.WriteLine($"Problems found: {problems.Count}");
                    foreach (var problem in problems)
                        console.WriteLine(problem);
                }

                if (parameters.DumpPhraseforms && analyzeResult.PhraseformCandidates.Count > 0)
                {
                    var filePath = Path.Combine(parameters.OutputPath, "phraseform_candidates.txt");
                    
                    DumpPhraseforms(filePath, analyzeResult.PhraseformCandidates);
                    console.WriteLine($"Dumped {analyzeResult.PhraseformCandidates.Count} phraseform candidates to '{filePath}'");
                }

                if (parameters.ValidateOnly)
                    return;

                try
                {
                    console.WriteLine($"Saving result file to {parameters.OutputPath}...");
                    state.Save(parameters.OutputPath, parameters.TargetEncoding, options, true);
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

    private static void DumpPhraseforms(string filePath, Dictionary<string, List<string>> phraseformCandidates)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        List<string> lines = new List<string>();
        foreach (var (phraseform, candidates) in phraseformCandidates)
        {
            lines.Add(phraseform);
            foreach (var candidate in candidates)
                lines.Add($"\t{candidate}");
        }

        File.WriteAllLines(filePath, lines);
    }
}
