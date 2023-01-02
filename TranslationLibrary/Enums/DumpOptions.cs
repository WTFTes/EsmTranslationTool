using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TranslationLibrary.Enums;

public class DumpOptions : ICloneable
{
    public DumpFlags Flags = DumpFlags.None;

    public bool HasFlag(DumpFlags flag) => Flags.HasFlag(flag);

    public DumpOptions SetFlag(DumpFlags flag)
    {
        Flags |= flag;
        return this;
    }

    public List<string> TypesInclude = new();
    public List<string> TypesSkip = new();
    public List<TextType> TextTypes = new();
    public Regex? TextSkipRegex;
    public Regex? TextMatchRegex;

    public bool NeedDumpText(string text)
    {
        return (TextSkipRegex == null || !TextSkipRegex.IsMatch(text)) &&
               (TextMatchRegex == null || TextMatchRegex.IsMatch(text));
    }

    public bool NeedDumpContext(string contextName)
    {
        return !TypesSkip.Contains(contextName) &&
               (TypesInclude.Count == 0 || TypesInclude.Contains(contextName));
    }

    public bool NeedDumpTextType(TextType type)
    {
        return TextTypes.Count == 0 || TextTypes.Contains(type);
    }

    public object Clone()
    {
        return MemberwiseClone();
    }

    public static DumpOptions Clone(DumpOptions obj)
    {
        return obj.Clone() as DumpOptions;
    }
}