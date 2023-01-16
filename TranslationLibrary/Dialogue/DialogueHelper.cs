using System;
using System.IO;
using System.Linq;
using TranslationLibrary.Storage;
using TranslationLibrary.Translation;

namespace TranslationLibrary.Dialogue
{
    public class DialogueHelper
    {
        public StorageById<string, StringWithHash> Dialogues { get; private set; } = new();

        public static DialogueHelper Create(TranslationState state)
        {
            DialogueHelper helper = new();

            helper.Load(state);

            return helper;
        }

        public static DialogueHelper Create(string path)
        {
            DialogueHelper helper = new();
            helper.Load(path);

            return helper;
        }

        private void CollectDialogueTopics(TranslationState state)
        {
            var topics = state.Storage.LookupContext("DIAL");
            if (topics == null)
                return;

            foreach (var topic in topics)
                Dialogues.Add(new StringWithHash() { Value = topic.Text });
        }

        public void Load(string path)
        {
            if (!Directory.Exists(path))
                throw new Exception($"Directory '{path}' does not exist");

            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetExtension(file) != ".txt")
                    continue;

                LoadDialogues(file);
            }
        }

        public void Save(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                throw new Exception($"Directory '{Path.GetDirectoryName(filePath)}' does not exist");

            File.WriteAllLines(filePath, Dialogues.Select(_ => _.Value));
        }

        public void Load(TranslationState state, string additionalPath = "")
        {
            CollectDialogueTopics(state);

            if (!string.IsNullOrEmpty(additionalPath) && Directory.Exists(additionalPath))
                Load(additionalPath);
        }

        private void LoadDialogues(string filePath)
        {
            var topics = File.ReadAllLines(filePath)
                .Where(_ => !string.IsNullOrWhiteSpace(_)).Select(_ => _.Trim())
                .Where(_ => !string.IsNullOrEmpty(_)).Select(_ => new StringWithHash() { Value = _ });
            foreach (var topic in topics)
                Dialogues.Add(topic);
        }
    }
}
