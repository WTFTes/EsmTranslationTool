using System;

namespace TranslationLibrary.Enums;

[Flags]
public enum MergeMode
{
    None = 0,
    Full = 1,
    Text = 2,
    Missing = 4,
}