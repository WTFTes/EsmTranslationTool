using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TranslationLibrary;

public class StringWithHash
{
    private string _value = "";

    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            PartHashes = GetStringPartHashes(_value);
        }
    }

    public int[] PartHashes { get; private set; } = Array.Empty<int>();

    public static int[] GetStringPartHashes(string text)
    {
        text = Regex.Replace(text, "[^\\w]", " ");

        return text.ToLowerInvariant().Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(_ => _.GetHashCode()).ToArray();
    }
}
