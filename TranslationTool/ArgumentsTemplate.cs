using System.CommandLine;
using System.CommandLine.Parsing;
using TranslationLibrary;

namespace TranslationTool;

public class ArgumentsTemplate
{
    private static readonly string[] AllowedEncodings = { EsmEncoding.CentralOrWesternEuropean, EsmEncoding.Cyrillic, EsmEncoding.English };
    
    protected static string FormatError(string message, Argument arg)
    {
        return $"Error in '{arg.Name}': {message}";
    }

    protected static string ValidateEncoding(ArgumentResult result)
    {
        var encoding = result.Tokens.SingleOrDefault()?.ToString();
        if (encoding == null)
        {
            result.ErrorMessage = FormatError("value must be specified", result.Argument);
            return "";
        }

        if (!AllowedEncodings.Contains(encoding))
        {
            result.ErrorMessage = FormatError($"allowed encodings: " +
                                              string.Join(", ", AllowedEncodings.Select(_ => $"'{_}'")),
                result.Argument);
            return "";
        }

        return encoding;
    }

    protected static string ValidateDirectory(ArgumentResult result, string defaultName = "")
    {
        string path;
        if (result.Tokens.Count == 0)
        {
            if (!string.IsNullOrEmpty(defaultName))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), defaultName);

                return path;
            }

            result.ErrorMessage = FormatError("value must be specified", result.Argument);
            return "";
        }

        path = result.Tokens.Single().ToString();
        if (!Directory.Exists(path))
        {
            result.ErrorMessage = FormatError($"directory '{path}' does not exist", result.Argument);
            return "";
        }

        return path;
    }

    protected static string ValidateInput(ArgumentResult result)
    {
        string path;
        if (result.Tokens.Count == 0)
        {
            result.ErrorMessage = FormatError("value must be specified", result.Argument);
            return "";
        }

        path = result.Tokens.Single().ToString();
        if (!File.Exists(path))
        {
            result.ErrorMessage = FormatError($"file '{path}' does not exist", result.Argument);
            return "";
        }

        return path;
    }
    
    protected static string[] ValidateDirectories(ArgumentResult result)
    {
        var strTokens = result.Tokens.Select(_ => _.ToString()).ToArray();
        foreach (var token in strTokens)
        {
            if (!Directory.Exists(token))
            {
                result.ErrorMessage = FormatError($"directory '{token}' does not exist", result.Argument);
                return Array.Empty<string>();
            }
        }

        return strTokens;
    }

    protected static string[] ValidateInputs(ArgumentResult result)
    {
        var strTokens = result.Tokens.Select(_ => _.ToString()).ToArray();
        if (strTokens.Length == 0)
        {
            result.ErrorMessage = FormatError("files must be specified", result.Argument);
            return Array.Empty<string>();
        }

        foreach (var path in strTokens)
        {
            if (!File.Exists(path))
            {
                result.ErrorMessage = FormatError($"file '{path}' does not exist", result.Argument);
                return Array.Empty<string>();;
            }
        }

        return strTokens;
    }
}