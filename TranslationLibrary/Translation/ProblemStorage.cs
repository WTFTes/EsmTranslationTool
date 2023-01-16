using System.Collections.Generic;
using System.Linq;
using TranslationLibrary.Storage;

namespace TranslationLibrary.Translation;

public class ProblemStorage
{
    private PlainStorage<string> _errors = new();
    private PlainStorage<string> _warnings = new();

    public bool IsEmpty => _errors.Empty && _warnings.Empty;

    public void AddWarning(string text) => _warnings.Add(text);
    public void AddError(string text) => _errors.Add(text);

    public IEnumerable<string> GetProblems() =>
        _errors.Select(_ => $"Error: {_}").Concat(_warnings.Select(_ => $"Warning: {_}"));
}
