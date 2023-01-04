using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TranslationLibrary.Dialogue;

namespace TranslationLibrary;

public static class Helpers
{
    static Helpers()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static List<StringWithHash> FilterTopicsForText(string text,
        Dictionary<string, StringWithHash> topics)
    {
        var hashes = StringWithHash.GetStringPartHashes(text);

        return topics.Where(_ => _.Value.PartHashes.All(_2 => hashes.Contains(_2)))
            .Select(_ => _.Value).ToList();
    }

    public static string PrepareInfoText(string text, string script, DialogueHelper? helper = null)
    {
        List<StringWithHash> topicList;
        if (helper == null)
        {
            if (!script.Contains("addtopic", StringComparison.InvariantCultureIgnoreCase))
                return text;

            var matches = Regex.Matches(script, "addtopic\\s+\"?([\\w \\-']+)\"?", RegexOptions.IgnoreCase);
            topicList = matches.Select(_ => new StringWithHash() { Value = _.Groups[1].Value }).ToList();
        }
        else
            topicList = FilterTopicsForText(text, helper.Dialogues);

        // sort by length DESC to match longest first
        topicList = topicList.OrderByDescending(_ => _.Value.Length).ToList();

        text = " " + text + " ";
        List<string> matched = new();
        int matchIndex = 0;
        foreach (var topic in topicList)
        {
            text = Regex.Replace(text, "([^\\w\\-])(" + Regex.Escape(topic.Value) + ")([^\\w\\-])", match =>
            {
                matched.Add(match.Groups[2].Value);
                var rep = match.Groups[1].Value + "{" + matchIndex + "}" + match.Groups[3].Value;

                ++matchIndex;
                return rep;
            }, RegexOptions.IgnoreCase);
        }

        for (var i = 0; i < matched.Count; ++i)
            text = text.Replace("{" + i + "}", "@" + matched[i] + "#");

        return text.Trim();
    }

    public static string StripHyperlinks(string text)
    {
        return text.Replace("@", "").Replace("#", "");
    }

    private static readonly List<string> TranslatableScriptCommands = new()
    {
        // "GetPCRank",
        "MessageBox",
        "Say\\s+\"",
        "Choice"
    };
    
    // bad choices: ^\s*choice\s+(?<=\s)[^"]
    //Name	Line	Text	Path
    // Сталгрим_3213517364154376468_2282422603075131729_2677411163373231969_1.txt	1	Choice : "Да, вот один." 1 "Нет." 2
    // Исследовать вместе_27908313681285523350_103918463143143475_7939321882060124566_1.txt	1	Choice ,"Вперед.", 3, "Ты - ненормальный.", 4
    // Исследовать вместе_7939321882060124566_27908313681285523350_EMPTY_1.txt	1	Choice ,"Я сделаю это.", 3, "Я не могу помочь вам.", 4
    // суджамма_12186127903207729714_1304111561233932196_3190830075182711833_1.txt	1	Choice Yes 1 No 2	


    public static bool ScriptCanBeTranslated(string script)
    {
        var lines = script.Split("\n");
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(';'))
                continue;

            foreach (var token in TranslatableScriptCommands)
                if (Regex.IsMatch(line, token, RegexOptions.IgnoreCase))
                    return true;
        }

        return false;
    }

    public static Encoding EsmToSysEncoding(string encoding)
    {
        switch (encoding)
        {
            case "win1250":
                return Encoding.GetEncoding("windows-1250");
            case "win1251":
                return Encoding.GetEncoding("windows-1251");
            case "win1252":
                return Encoding.GetEncoding("windows-1252");
            default:
                throw new Exception($"Unsupported encoding '{encoding}'");
        }
    }

    public static string GetLastError(string context)
    {
        var lastError = Imports.Translation_GetLastError();
        if (string.IsNullOrEmpty(lastError))
            return context;

        return $"{context}: {lastError}";
    }

    /**
     * Replace aster
     */
    public static string ReplacePseudoAsterisks(string text, bool reverse = false)
    {
        var src = reverse ? "*" : "\u007F";
        var dst = reverse ? "\u007F" : "*";


        return Regex.Replace(text, "@[^#]+" + Regex.Escape(src) + "#",
            match => match.Groups[0].Value.Replace(src, dst));
    }
}
