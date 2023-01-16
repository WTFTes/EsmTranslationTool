using System.CommandLine;
using TranslationLibrary.Dialogue;
using TranslationLibrary.Translation;

namespace TranslationTool.Commands;

public class DumpDialoguesParameters
{
    public string[] InputEsms { get; set; }
    public string Encoding { get; set; }
    public string OutputPath { get; set; }
    public bool NoMerge { get; set; }
}

public static class DumpDialogues
{
    public static void Run(DumpDialoguesParameters parameters, IConsole console)
    {
        try
        {
            var mergedState = new TranslationState();

            var totalDialogues = 0;
            foreach (var esm in parameters.InputEsms)
            {
                var state = new TranslationState();
                try
                {
                    console.WriteLine($"Loading dialogues from '{Path.GetFileName(esm)}'...");
                    state.Load(esm, parameters.Encoding);
                    var dialogues = state.Storage.LookupContext("DIAL");
                    int cnt = dialogues?.Size ?? 0;
                    console.WriteLine($"Loaded {cnt} dialogues from '{Path.GetFileName(esm)}'");
                    totalDialogues += cnt;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to load {esm}: {e.Message}");
                }

                if (!parameters.NoMerge)
                    mergedState.Merge(state);
                else
                {
                    var dumpPath = Path.Combine(parameters.OutputPath, state.FileContext);
                    if (!Directory.Exists(dumpPath))
                        Directory.CreateDirectory(dumpPath);

                    console.WriteLine($"Dumping dialogues from '{Path.GetFileName(esm)}' to '{parameters.OutputPath}'...");
                    try
                    {
                        var helper = DialogueHelper.Create(state);
                        helper.Save(Path.Combine(dumpPath, $"dialogues_{state.FileContext}.txt"));
                        console.WriteLine($"Dumped '{helper.Dialogues.Size}' dialogues for '{Path.GetFileName(esm)}'");
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to dump dialogues: {e.Message}");
                    }
                }
            }

            if (!parameters.NoMerge)
            {
                var dumpPath = parameters.OutputPath;
                if (!Directory.Exists(dumpPath))
                    Directory.CreateDirectory(dumpPath);

                var helper = DialogueHelper.Create(mergedState);
                
                if (parameters.InputEsms.Length > 1)
                    console.WriteLine($"{totalDialogues} dialogues merged to {helper.Dialogues.Size}");

                console.WriteLine($"Dumping dialogues to '{dumpPath}'...");
                try
                {
                    helper.Save(Path.Combine(dumpPath, $"dialogues.txt"));
                    console.WriteLine($"Dumped {helper.Dialogues.Size} dialogues");
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to dump dialogues: {e.Message}");
                }
            }

            console.WriteLine("Done");
        }
        catch (Exception e)
        {
            console.WriteLine($"Error: {e.Message}");
        }
    }
}