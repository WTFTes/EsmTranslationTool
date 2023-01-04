using System;

namespace TranslationLibrary.Enums;

[Flags]
public enum DumpFlags : ulong
{
    None = 0,
    SkipTranslated = 1,
    SkipUntranslated = 2,
    SkipFileContextLevel = 4,
    IncludeImplicitTopics = 8,
    IncludeFullTopics = 16,
    AllScripts = 32,
    AllToJson = 64,
    TextOnly = 128,
    TranslatedText = 256,
}