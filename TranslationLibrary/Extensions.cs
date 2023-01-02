using System.Collections.Generic;
using TranslationLibrary.Enums;

namespace TranslationLibrary;

public static class Extensions
{
    public static V GetOrCreate<K, V>(this Dictionary<K, V> dict, K key) where V : new()
    {
        V val;
        if (!dict.TryGetValue(key, out val))
        {
            val = new();
            dict[key] = val;
        }

        return val;
    }

    public static bool CanHaveHyperlinks(this DialogueType dialogueType)
    {
        switch (dialogueType)
        {
            case DialogueType.Topic:
            case DialogueType.Greeting:
            case DialogueType.Journal:
                return true;
            default:
                return false;
        }
    }
}
