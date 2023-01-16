using System.CommandLine;
using TranslationLibrary.Localization;

namespace TranslationTool.Commands;

public class DumpLocalizationsParameters
{
    public string[] InputEsms { get; set; }
    public string Encoding { get; set; }
    public string OutputPath { get; set; }
    public bool NoMerge { get; set; }
}

public static class DumpLocalizations
{
    public static void Run(DumpLocalizationsParameters parameters, IConsole console)
    {
        try
        {
            Dictionary <string, LocalizationStorage >  localizationStorages = new();

            var totalEntries = 0;
            foreach (var esm in parameters.InputEsms)
            {
                var localization = new LocalizationStorage();
                try
                {
                    console.WriteLine($"Loading localizations from '{Path.GetFileName(esm)}'...");
                    localization.LoadNative(Path.GetDirectoryName(esm), parameters.Encoding, Path.GetFileNameWithoutExtension(esm));
                    console.WriteLine($"Loaded {localization.Size} entries for '{Path.GetFileName(esm)}'");
                    totalEntries += localization.Size;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to load {esm}: {e.Message}");
                }

                if (!parameters.NoMerge)
                    localizationStorages[Path.GetFileNameWithoutExtension(esm)] = localization;
                else
                {
                    var dumpPath = parameters.OutputPath;
                    if (!Directory.Exists(dumpPath))
                        Directory.CreateDirectory(dumpPath);

                    console.WriteLine(
                        $"Dumping localizations from '{Path.GetFileName(esm)}' to '{parameters.OutputPath}'...");
                    try
                    {
                        localization.Save(Path.Combine(parameters.OutputPath, $"localizations_{Path.GetFileName(esm)}.json"));
                        console.WriteLine($"Dumped '{localization.Size}' entries for '{Path.GetFileName(esm)}'");
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to dump dialogues: {e.Message}");
                    }
                }
            }

            if (!parameters.NoMerge)
            {
                var merged = new LocalizationStorage();
                foreach (var (_,localization) in localizationStorages)
                    merged.Merge(localization);
                
                var dumpPath = parameters.OutputPath;
                if (!Directory.Exists(dumpPath))
                    Directory.CreateDirectory(dumpPath);

                if (parameters.InputEsms.Length > 1)
                    console.WriteLine($"{totalEntries} localizations merged to {merged.Size}");

                console.WriteLine($"Dumping localizations to '{dumpPath}'...");
                try
                {
                    merged.Save(Path.Combine(dumpPath, $"localizations.json"));
                    console.WriteLine($"Dumped {merged.Size} localizations");
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to dump localizations: {e.Message}");
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
