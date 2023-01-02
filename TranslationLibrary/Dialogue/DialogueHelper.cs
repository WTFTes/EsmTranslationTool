using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TranslationLibrary.Localization;
using TranslationLibrary.Translation;

namespace TranslationLibrary.Dialogue
{
    public class DialogueHelper
    {
        private Dictionary<string, StringWithHash> _dialogues = new();
    
        public static Dictionary<string, StringWithHash> CollectDialogueTopics(TranslationState state)
        {
            var topics = state.RecordsByContextAndId.GetValueOrDefault("DIAL");
            if (topics == null)
                return new();

            return topics.Select(_ => new StringWithHash() { Value = _.Key }).DistinctBy(_ => _.Value)
                .OrderByDescending(_ => _.Value.Length)
                .ToDictionary(_ => _.Value);
        }

        public void Load(string path)
        {
            if (!Directory.Exists(path))
                throw new Exception($"Directory '{path}' does not exist");

            IEnumerable<StringWithHash> colection = new List<StringWithHash>();

            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetExtension(file) != ".txt")
                    continue;

                colection = colection.Concat(LoadDialogues(file));
            }

            _dialogues = colection.DistinctBy(_ => _.Value).OrderByDescending(_ => _.Value).ToDictionary(_ => _.Value);
        }

        public void Load(TranslationState state, string additionalPath = "")
        {
            _dialogues = CollectDialogueTopics(state);

            if (!string.IsNullOrEmpty(additionalPath) && Directory.Exists(additionalPath))
                Load(additionalPath);
        }

        private IEnumerable<StringWithHash> LoadDialogues(string filePath)
        {
            return File.ReadAllText(filePath).Split("\n").Select(_ => _.Trim())
                .Where(_ => !string.IsNullOrEmpty(_)).Select(_ => new StringWithHash() { Value = _ });
        }

        public void Analyze<T>(RecordStorage<T> storage, LocalizationStore localizations) where T : EntityRecord
        {
            var hyperlinkTopics = CollectHyperlinks(storage);

            Dictionary<string, List<string>> topicProblems = new();
            List<string> phraseFormProblems = new();

            foreach (var (infoId, topics) in hyperlinkTopics)
            {
                foreach (var topic in topics)
                {
                    if (topic.Trim() != topic)
                        topicProblems.GetOrCreate(infoId)
                            .Add($"Hyperlink for topic '{topic.Trim()}' contains extra spaces");

                    var checkRes = CheckKnownTopicOrPhraseForm(topic.Trim(), localizations);
                    if (!string.IsNullOrEmpty(checkRes))
                        topicProblems.GetOrCreate(infoId).Add(checkRes);
                }
            }

            foreach (var (_, phraseForm) in localizations.DialoguePhraseForms.Records)
            {
                if (!_dialogues.ContainsKey(phraseForm.Source))
                    phraseFormProblems.Add(
                        $"Phraseform '{phraseForm.Target}' refers to unknown dialogue '{phraseForm.Source}'");
            }
        }

        private string CheckKnownTopicOrPhraseForm(string topic, LocalizationStore localizations)
        {
            if (_dialogues.ContainsKey(topic))
                return "";

            var phraseForm = localizations.DialoguePhraseForms.LookupByTarget(topic);
            if (phraseForm != null)
                return "";

            return $"'{topic}' is not a known dialogues or phraseform";
        }

        private Dictionary<string, List<string>> CollectHyperlinks<T>(RecordStorage<T> storage) where T : EntityRecord
        {
            var infos = storage.RecordsByContextAndId.GetValueOrDefault("INFO");
            if (infos == null)
                return new();

            Dictionary<string, List<string>> result = new();
            foreach (var (id, info) in infos)
            {
                var matches = Regex.Matches(info.Text, "@([^#])#", RegexOptions.IgnoreCase);

                result[id].AddRange(matches.Select(_ => _.Groups[1].Value));
            }

            return result;
        }
    }
}
