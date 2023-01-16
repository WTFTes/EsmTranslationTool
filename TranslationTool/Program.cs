using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using TranslationLibrary.Enums;
using TranslationLibrary.Storage;
using TranslationLibrary.Translation;
using TranslationTool.Commands;

namespace TranslationTool
{
    internal static class Program
    {
        private static List<Tuple<Command, Action<InvocationContext>?>> _commandDefinitions = new()
        {
            new(new RootCommand("TranslationTool: dumps and loads translatable strings from/to TES3 .esm/.esp files."),
                null),
            new(
                new(
                    name: "extract",
                    description: "Extract texts from .esm/.esp's."
                )
                {
                    ExtractArgumentsTemplate.InputEsms,
                    ExtractArgumentsTemplate.Encoding,
                    ExtractArgumentsTemplate.GlossaryDirectories,
                    ExtractArgumentsTemplate.OutputPath,
                    ExtractArgumentsTemplate.NoMerge,
                    ExtractArgumentsTemplate.TypesInclude,
                    ExtractArgumentsTemplate.TypesSkip,
                    ExtractArgumentsTemplate.TextTypes,
                    ExtractArgumentsTemplate.MarkTopics,
                    ExtractArgumentsTemplate.AllScripts,
                    ExtractArgumentsTemplate.AllToJson,
                    ExtractArgumentsTemplate.RegexMatch,
                    ExtractArgumentsTemplate.RegexExclude,
                    ExtractArgumentsTemplate.RecordsPerFile,
                },
                context =>
                {
                    var result = context.ParseResult;

                    ExtractParameters parameters = new();
                    parameters.InputEsms = result.GetValueForOption(ExtractArgumentsTemplate.InputEsms);
                    // context.Console.WriteLine("Esms: " + string.Join(", ", parameters.InputEsms));
                    parameters.Encoding = result.GetValueForOption(ExtractArgumentsTemplate.Encoding);
                    // context.Console.WriteLine($"Encoding: {parameters.Encoding}");
                    parameters.GlossaryDirectories =
                        result.GetValueForOption(ExtractArgumentsTemplate.GlossaryDirectories);
                    // context.Console.WriteLine($"Glossaries: "  + string.Join(", ", parameters.GlossaryDirectories));
                    parameters.OutputPath = result.GetValueForOption(ExtractArgumentsTemplate.OutputPath);
                    // context.Console.WriteLine($"Output: {parameters.OutputPath}");
                    parameters.NoMerge = result.GetValueForOption(ExtractArgumentsTemplate.NoMerge);
                    parameters.TypesInclude = result.GetValueForOption(ExtractArgumentsTemplate.TypesInclude);
                    parameters.TypesSkip = result.GetValueForOption(ExtractArgumentsTemplate.TypesSkip);
                    parameters.TextTypes = result.GetValueForOption(ExtractArgumentsTemplate.TextTypes);
                    parameters.MarkTopics = result.GetValueForOption(ExtractArgumentsTemplate.MarkTopics);
                    parameters.AllScripts = result.GetValueForOption(ExtractArgumentsTemplate.AllScripts);
                    parameters.AllToJson = result.GetValueForOption(ExtractArgumentsTemplate.AllToJson);
                    parameters.RegexMatch = result.GetValueForOption(ExtractArgumentsTemplate.RegexMatch);
                    parameters.RegexExclude = result.GetValueForOption(ExtractArgumentsTemplate.RegexExclude);
                    parameters.RecordsPerFile = result.GetValueForOption(ExtractArgumentsTemplate.RecordsPerFile);
                    // context.Console.WriteLine($"Split: {parameters.SplitByContext}");

                    Extract.Run(parameters, context.Console);
                }
            ),
            new(
                new(
                    name: "build-glossary",
                    description:
                    "Builds glossary from provided original and localized .esm/.esp's. Localization .cel and .mrk files will also be parsed."
                )
                {
                    BuildGlossaryArgumentsTemplate.InputOriginalEsm,
                    BuildGlossaryArgumentsTemplate.OriginalEncoding,
                    BuildGlossaryArgumentsTemplate.InputLocalizedEsm,
                    BuildGlossaryArgumentsTemplate.LocalizedEncoding,
                    BuildGlossaryArgumentsTemplate.OutPath,
                    BuildGlossaryArgumentsTemplate.MarkTopics,
                    BuildGlossaryArgumentsTemplate.Plain,
                },
                context =>
                {
                    var result = context.ParseResult;

                    BuildGlossaryParameters parameters = new();

                    parameters.OriginalEsms = result.GetValueForOption(BuildGlossaryArgumentsTemplate.InputOriginalEsm);
                    parameters.OriginalEncoding =
                        result.GetValueForOption(BuildGlossaryArgumentsTemplate.OriginalEncoding);
                    parameters.LocalizedEsms =
                        result.GetValueForOption(BuildGlossaryArgumentsTemplate.InputLocalizedEsm);
                    parameters.LocalizedEncoding =
                        result.GetValueForOption(BuildGlossaryArgumentsTemplate.LocalizedEncoding);
                    parameters.OutputPath = result.GetValueForOption(BuildGlossaryArgumentsTemplate.OutPath);
                    parameters.MarkTopics = result.GetValueForOption(BuildGlossaryArgumentsTemplate.MarkTopics);
                    parameters.Plain = result.GetValueForOption(BuildGlossaryArgumentsTemplate.Plain);

                    BuildGlossary.Run(parameters, context.Console);
                }
            ),
            new(
                new(
                    name: "dump-dialogues",
                    description: "Dumps dialogues from .esm/.esp's."
                )
                {
                    DumpDialoguesArgumentsTemplate.InputEsms,
                    DumpDialoguesArgumentsTemplate.Encoding,
                    DumpDialoguesArgumentsTemplate.OutputPath,
                    DumpDialoguesArgumentsTemplate.NoMerge,
                },
                context =>
                {
                    var result = context.ParseResult;

                    DumpDialoguesParameters parameters = new();
                    parameters.InputEsms = result.GetValueForOption(DumpDialoguesArgumentsTemplate.InputEsms);
                    parameters.Encoding = result.GetValueForOption(DumpDialoguesArgumentsTemplate.Encoding);
                    parameters.OutputPath = result.GetValueForOption(DumpDialoguesArgumentsTemplate.OutputPath);
                    parameters.NoMerge = result.GetValueForOption(DumpDialoguesArgumentsTemplate.NoMerge);

                    DumpDialogues.Run(parameters, context.Console);
                }
            ),
            new(
                new(
                    name: "dump-localizations",
                    description: "Dumps localizations from .esm/.esp's."
                )
                {
                    DumpLocalizationsArgumentsTemplate.InputEsms,
                    DumpLocalizationsArgumentsTemplate.Encoding,
                    DumpLocalizationsArgumentsTemplate.OutputPath,
                    DumpLocalizationsArgumentsTemplate.NoMerge,
                },
                context =>
                {
                    var result = context.ParseResult;

                    DumpLocalizationsParameters parameters = new();
                    parameters.InputEsms = result.GetValueForOption(DumpLocalizationsArgumentsTemplate.InputEsms);
                    parameters.Encoding = result.GetValueForOption(DumpLocalizationsArgumentsTemplate.Encoding);
                    parameters.OutputPath = result.GetValueForOption(DumpLocalizationsArgumentsTemplate.OutputPath);
                    parameters.NoMerge = result.GetValueForOption(DumpLocalizationsArgumentsTemplate.NoMerge);

                    DumpLocalizations.Run(parameters, context.Console);
                }
            ),
            new(
                new(
                    name: "save",
                    description: "Load translations and save .esm/.esp's."
                )
                {
                    SaveArgumentsTemplate.InputEsms,
                    SaveArgumentsTemplate.InputEncoding,
                    SaveArgumentsTemplate.TargetEncoding,
                    SaveArgumentsTemplate.GlossaryDirectories,
                    SaveArgumentsTemplate.LocalizationsDirectory,
                    SaveArgumentsTemplate.DialoguesDirectory,
                    SaveArgumentsTemplate.Phraseforms,
                    SaveArgumentsTemplate.DumpPhraseforms,
                    SaveArgumentsTemplate.TranslationsPath,
                    SaveArgumentsTemplate.ValidateOnly,
                    SaveArgumentsTemplate.OutputPath,
                    SaveArgumentsTemplate.TranslateRegions,
                },
                context =>
                {
                    var result = context.ParseResult;

                    SaveParameters parameters = new();

                    parameters.InputEsms = result.GetValueForOption(SaveArgumentsTemplate.InputEsms);
                    parameters.OriginalEncoding = result.GetValueForOption(SaveArgumentsTemplate.InputEncoding);
                    parameters.TargetEncoding = result.GetValueForOption(SaveArgumentsTemplate.TargetEncoding);
                    parameters.GlossaryDirectories =
                        result.GetValueForOption(SaveArgumentsTemplate.GlossaryDirectories);
                    parameters.LocalizationsDirectory =
                        result.GetValueForOption(SaveArgumentsTemplate.LocalizationsDirectory);
                    parameters.DialoguesDirectory = result.GetValueForOption(SaveArgumentsTemplate.DialoguesDirectory);
                    parameters.TranslationsPath = result.GetValueForOption(SaveArgumentsTemplate.TranslationsPath);
                    parameters.ValidateOnly = result.GetValueForOption(SaveArgumentsTemplate.ValidateOnly);
                    parameters.Phraseforms = result.GetValueForOption(SaveArgumentsTemplate.Phraseforms);
                    parameters.DumpPhraseforms = result.GetValueForOption(SaveArgumentsTemplate.DumpPhraseforms);
                    parameters.OutputPath = result.GetValueForOption(SaveArgumentsTemplate.OutputPath);
                    parameters.TranslateRegions = result.GetValueForOption(SaveArgumentsTemplate.TranslateRegions);

                    Save.Run(parameters, context.Console);
                }
            )
        };

        static int Main(string[] args)
        {
            Command commandTree = null;
            foreach (var command in _commandDefinitions)
            {
                if (command.Item2 != null)
                    command.Item1.SetHandler(command.Item2);
                if (commandTree == null)
                {
                    commandTree = command.Item1;
                    continue;
                }

                commandTree.AddCommand(command.Item1);
            }

            var parser = new CommandLineBuilder(commandTree).UseDefaults().Build();

            try
            {
                return parser.Invoke(args);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }

            return 1;
        }
    }

    public class ExtractArgumentsTemplate : ArgumentsTemplate
    {
        public static readonly Option<string[]> InputEsms = new(
            name: "--in",
            description: "List of .esm/.esp to extract.",
            parseArgument: ValidateInputs
        )
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true,
        };

        public static readonly Option<string> Encoding = new(
            name: "--encoding",
            description: "Encoding of input .esm/.esp's.",
            parseArgument: ValidateEncoding
        )
        {
            IsRequired = true,
        };

        public static readonly Option<string[]> GlossaryDirectories = new(
            name: "--glossary",
            description:
            "Path to glossaries. Entries contained in glossary with full text match will be skipped from extraction. Last directory glossary is preferred.",
            parseArgument: ValidateDirectories
        )
        {
            AllowMultipleArgumentsPerToken = true,
        };

        public static readonly Option<string> OutputPath = new(
            name: "--out",
            description: "Path to working directory where texts will be extracted.",
            isDefault: true,
            parseArgument: result => ValidateDirectory(result, "texts")
        );

        public static readonly Option<bool> NoMerge = new(
            name: "--no-merge",
            description: "Input files will be not be merged, extracted contents will be put to separate subdirectories."
        );

        public static readonly Option<string[]> TypesInclude = new(
            name: "--types",
            description:
            "List of entity types that should be extracted."
        )
        {
            AllowMultipleArgumentsPerToken = true
        };

        public static readonly Option<string[]> TypesSkip = new(
            name: "--types-skip",
            description:
            "List of entity types that should not be extracted."
        )
        {
            AllowMultipleArgumentsPerToken = true
        };

        public static readonly Option<TextType[]> TextTypes = new(
            name: "--text-types",
            description:
            "List of text types to be extracted. " +
            string.Join(", ", Enum.GetValues<TextType>().Select(_ => $"'{_}'")) + " are supported."
        )
        {
            AllowMultipleArgumentsPerToken = true
        };

        public static readonly Option<bool> MarkTopics = new(
            name: "--mark-topics",
            description: "Topic names in dialogue texts will be guessed and marked with @...# tags."
        );

        public static readonly Option<bool> AllScripts = new(
            name: "--all-scripts",
            description:
            "All scripts will be extracted, not only translatable ones (those containing 'Say' and 'MessageBox' commands)."
        );

        public static readonly Option<bool> AllToJson = new(
            name: "--all-to-json",
            description:
            "By default scripts and books are extracted to separate txt and html files correspondingly. Use this option to extracted everything to single json file."
        );

        public static readonly Option<string> RegexMatch = new(
            name: "--regexp",
            description: "Only texts matching this regexp will be extracted."
        );

        public static readonly Option<string> RegexExclude = new(
            name: "--not-regexp",
            description: "Texts matching this regexp will not be extracted."
        );

        public static readonly Option<int> RecordsPerFile = new(
            name: "--records-per-file",
            description: "Maximum count of records per *.json file."
        );
    }

    public class BuildGlossaryArgumentsTemplate : ArgumentsTemplate
    {
        public static readonly Option<string[]> InputOriginalEsm = new(
            name: "--original",
            description: "List of original .esm/.esp to load.",
            parseArgument: ValidateInputs
        )
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true,
        };

        public static readonly Option<string> OriginalEncoding = new(
            name: "--original-encoding",
            description: "Encoding of original .esm/.esp's.",
            parseArgument: ValidateEncoding
        )
        {
            IsRequired = true,
        };

        public static readonly Option<string[]> InputLocalizedEsm = new(
            name: "--localized",
            description:
            "List of localized .esm/.esp to load. When building glossary from multiple files, these ones should be same order as in '--original', so e.g. your original Morrowind.esm corresponds to localized Morrowind.esm.",
            parseArgument: result =>
            {
                var res = ValidateInputs(result);
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    return res;

                var opts = result.GetValueForOption(InputOriginalEsm);
                if (opts.Length != result.Tokens.Count)
                {
                    result.ErrorMessage = FormatError("count does not match 'original' count.", result.Argument);
                    return null;
                }

                return res;
            }
        )
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true,
        };

        public static readonly Option<string> LocalizedEncoding = new(
            name: "--localized-encoding",
            description: "Encoding of localized .esm/.esp's.",
            parseArgument: ValidateEncoding
        )
        {
            IsRequired = true,
        };

        public static readonly Option<string> OutPath = new(
            name: "--out",
            description: "Path to directory where glossary files will be generated.",
            isDefault: true,
            parseArgument: result => ValidateDirectory(result, "glossary")
        );

        public static readonly Option<bool> MarkTopics = new(
            name: "--mark-topics",
            description:
            "Topic names in dialogue texts will be guessed and marked with @...# tags. Use this for locales that have declensions."
        );

        public static readonly Option<bool> Plain = new(
            name: "--plain",
            description: "Save as plain csv file. Will not contain book, script and dialogues matches."
        );
    }

    public class DumpDialoguesArgumentsTemplate : ArgumentsTemplate
    {
        public static readonly Option<string[]> InputEsms = new(
            name: "--in",
            description: "List of .esm/.esp.",
            parseArgument: ValidateInputs
        )
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true,
        };

        public static readonly Option<string> Encoding = new(
            name: "--encoding",
            description: "Encoding of input .esm/.esp's.",
            parseArgument: ValidateEncoding
        )
        {
            IsRequired = true,
        };

        public static readonly Option<string> OutputPath = new(
            name: "--out",
            description: "Path to working directory where dialogues will be dumped.",
            isDefault: true,
            parseArgument: result => ValidateDirectory(result, "dialogues")
        );

        public static readonly Option<bool> NoMerge = new(
            name: "--no-merge",
            description: "Input files will be not be merged, dumped dialogues will be put to separate files."
        );
    }

    public class DumpLocalizationsArgumentsTemplate : ArgumentsTemplate
    {
        public static readonly Option<string[]> InputEsms = new(
            name: "--in",
            description: "List of .esm/.esp's.",
            parseArgument: ValidateInputs
        )
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true,
        };

        public static readonly Option<string> Encoding = new(
            name: "--encoding",
            description: "Encoding of input .esm/.esp's.",
            parseArgument: ValidateEncoding
        )
        {
            IsRequired = true,
        };

        public static readonly Option<string> OutputPath = new(
            name: "--out",
            description: "Path to working directory where localizations will be dumped.",
            isDefault: true,
            parseArgument: result => ValidateDirectory(result, "localizations")
        );

        public static readonly Option<bool> NoMerge = new(
            name: "--no-merge",
            description: "Input files will be not be merged, dumped localizations will be put to separate files."
        );
    }

    public class SaveArgumentsTemplate : ArgumentsTemplate
    {
        public static readonly Option<string[]> InputEsms = new(
            name: "--in",
            description: "List of .esm/.esp to load.",
            parseArgument: ValidateInputs
        )
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true,
        };

        public static readonly Option<string> InputEncoding = new(
            name: "--input-encoding",
            description: "Encoding of input .esm/.esp's.",
            parseArgument: ValidateEncoding
        )
        {
            IsRequired = true,
        };

        public static readonly Option<string> TargetEncoding = new(
            name: "--target-encoding",
            description: "Encoding of result .esm/.esp's.",
            parseArgument: ValidateEncoding
        )
        {
            IsRequired = true,
        };

        public static readonly Option<string[]> GlossaryDirectories = new(
            name: "--glossary",
            description:
            "Path to glossaries. Fully matched glossary entries will be applied before applying translation data. Last directory glossary is preferred.",
            parseArgument: ValidateDirectories
        )
        {
            AllowMultipleArgumentsPerToken = true,
        };

        public static readonly Option<string> LocalizationsDirectory = new(
            name: "--localization",
            description:
            "Path to directory containing localization files. Used to properly validate translations and filter generated localization files.",
            parseArgument: result => ValidateDirectory(result, "")
        );

        public static readonly Option<string> DialoguesDirectory = new(
            name: "--dialogues",
            description:
            "Path to directory containing dialogues. Used to properly validate translations and append dialogue links properly.",
            parseArgument: result => ValidateDirectory(result, "")
        );
        
        public static readonly Option<bool> ValidateOnly = new(
            name: "--validate-only",
            description:
            "Only validate translations, do not generate result file."
        );
        
        public static readonly Option<string> Phraseforms = new(
            name: "--phraseforms",
            description: "Path to file containing dialogue phraseforms (.top) for current translation.",
            parseArgument: ValidateInput
        );
        
        public static readonly Option<bool> DumpPhraseforms = new(
            name: "--dump-phraseforms",
            description: "Dump unmatched dialogue phraseforms to file."
        );

        public static readonly Option<string> TranslationsPath = new(
            name: "--translations",
            description: "Path to translation files.",
            parseArgument: result => ValidateDirectory(result, "")
        )
        {
            IsRequired = true
        };

        public static readonly Option<string> OutputPath = new(
            name: "--out",
            description: "Path to directory where translated .esm/.esp will be created.",
            isDefault: true,
            parseArgument: result => ValidateDirectory(result, "out")
        );

        public static readonly Option<bool> TranslateRegions = new(
            name: "--translate-regions",
            description:
            "Apply region (REGN) translations. !NOTE: regions should not be translated, otherwise some script commands will be broken. Some locales (russian) have regions translated (and this can break scripts). Use this option to restore original region names."
        );
    }
}
