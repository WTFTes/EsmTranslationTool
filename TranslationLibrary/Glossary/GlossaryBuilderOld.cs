namespace TranslationLibrary.Glossary;

// using TranslationStorage = Dictionary<string, Dictionary<string, GlossaryBuilder.CandidateRecord>>;
// using TranslationCandidatesStorage = Dictionary<string, List<string>>;

public class GlossaryBuilderOld
{
    // private readonly EntityRecord _sourceStore = new(); 
    // private readonly TranslationStorage _targetStore = new();
    //
    // public class CandidateRecord
    // {
    //     public TranslationRecord Record { get; }
    //     public string Text { get; private set; }
    //
    //     private TranslationState _state;
    //     private DumpOptions _lastInitOptions;
    //
    //     public CandidateRecord(TranslationRecord record, TranslationState state)
    //     {
    //         Record = record;
    //         _state = state;
    //     }
    // }
    //
    // private string GetGlossaryKey(TranslationRecord record)
    // {
    //     return $"{record.ContextId}_{record.Index}";
    // }
    //
    // public void AddSources(TranslationState state) => AddToStore(_sourceStore, state);
    // public void AddTargets(TranslationState state) => AddToStore(_targetStore, state);
    //
    // private void AddToStore(TranslationStorage storage, TranslationState state)
    // {
    //     foreach (var record in state.Records)
    //         storage.GetOrCreate(record.ContextName)[GetGlossaryKey(record)] = new(record, state);
    // }
    //
    // private CandidateRecord? GetCandidate(string context, string id)
    // {
    //     return _sourceStore?.GetValueOrDefault(context)?.GetValueOrDefault(id);
    // }
    //
    // private void AddNpcVariants(TranslationCandidatesStorage storage, string variant, string original)
    // {
    //     if (variant.Count(_ => _ == '\'') > 1 && original.Count(_ => _ == '\'') > 1 && variant.Contains(" ") && original.Contains(" "))
    //     {
    //         var parts1 = variant.Split("'", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    //             .ToArray();
    //         var parts2 = original.Split("'", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    //             .ToArray();
    //         if (parts1.Length == parts2.Length)
    //         {
    //             for (var i = 0; i < parts1.Length; ++i)
    //                 AddVariant(storage, parts1[i], parts2[i]);
    //
    //             return;
    //         }
    //     }
    //
    //     var parts3 = variant.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    //         .ToArray();
    //     var parts4 = original.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    //         .ToArray();
    //     if (parts3.Length == parts4.Length)
    //     {
    //         for (var i = 0; i < parts3.Length; ++i)
    //             AddVariant(storage, parts3[i], parts4[i]);
    //     }
    // }
    //
    // private void AddVariant(TranslationCandidatesStorage storage, string variant, string original)
    // {
    //     var storedCandidates = storage.GetOrCreate(variant);
    //     if (!storedCandidates.Contains(original))
    //         storedCandidates.Add(original);
    // }
    //
    // public TranslationCandidatesStorage Build(DumpOptions options, bool forceSplit = false)
    // {
    //     TranslationCandidatesStorage result = new();
    //     foreach (var typeDict in _targetStore)
    //     {
    //         foreach (var transItem in typeDict.Value)
    //         {
    //             transItem.Value.Init(options);
    //             
    //             if (!Regex.IsMatch(transItem.Value.Text, "[А-Яа-я]"))
    //                 continue;
    //             
    //             var candidate = GetCandidate(typeDict.Key, transItem.Key);
    //             if (candidate == null)
    //                 continue;
    //             
    //             candidate.Init(options);
    //
    //             if (candidate.Text.ToLower() == "<deprecated>")
    //                 continue;
    //
    //             if (candidate == transItem.Value)
    //                 continue;
    //
    //             if (typeDict.Key == "NPC" || forceSplit)
    //                 AddNpcVariants(result, candidate.Text, transItem.Value.Text);
    //
    //             AddVariant(result, candidate.Text, transItem.Value.Text);
    //         }
    //     }
    //
    //     return result;
    // }
    //
    // struct Tmp
    // {
    //     public string A { get; set; }
    //     public string B { get; set; }
    // }
    //
    // public void Dump(string outPath, DumpOptions options, bool forceSplit = false)
    // {
    //     var translationCandidatesStorage = Build(options, forceSplit);
    //     using (var writer = new StreamWriter(outPath))
    //     using (var wr = new CsvWriter(writer, CultureInfo.InvariantCulture))
    //     {
    //         foreach (var cand in translationCandidatesStorage)
    //         {
    //             if (cand.Value.Count > 1)
    //                 continue;
    //             
    //             wr.WriteRecord(new Tmp { A = cand.Key, B = cand.Value[0] });
    //             wr.NextRecord();
    //         }
    //     }
    //
    //     foreach (var cand in translationCandidatesStorage)
    //     {
    //         if (cand.Value.Count > 1)
    //         {
    //             Debug.WriteLine(cand.Key);
    //             foreach (var val in cand.Value)
    //                 Debug.WriteLine(val);
    //             Debug.WriteLine("");
    //         }
    //     }
    // }
}