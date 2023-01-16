using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TranslationLibrary.Storage.Interfaces;

namespace TranslationLibrary;

public class StringWithHash : IRecordWithId<string>
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

    [JsonIgnore]
    public int[] PartHashes { get; private set; } = Array.Empty<int>();

    public static int[] GetStringPartHashes(string text)
    {
        text = Regex.Replace(text, "[^\\w]", " ");

        return text.ToLowerInvariant().Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(_ => _.GetHashCode()).ToArray();
    }

    public string GetId()
    {
        return _value;
    }
}
