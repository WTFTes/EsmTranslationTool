using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using TranslationLibrary.Localization.Records;
using TranslationLibrary.Translation;

namespace TranslationLibrary.Localization
{
    public class LocalizationStore
    {
        private readonly MappingStore<MappingRecord> _cells = new();
        private readonly MappingStore<MappingRecord> _dialogueNames = new();
        private readonly MappingStore<MappingRecord> _dialoguePhraseForms = new();

        public MappingStore<MappingRecord> Cells => _cells;
        public MappingStore<MappingRecord> DialogueNames => _dialogueNames;
        public MappingStore<MappingRecord> DialoguePhraseForms => _dialoguePhraseForms;

        public bool IsEmpty => _cells.IsEmpty && _dialogueNames.IsEmpty && _dialoguePhraseForms.IsEmpty;

        public void Clear()
        {
            _cells.Clear();
            _dialogueNames.Clear();
            _dialoguePhraseForms.Clear();
        }
        
        public void LoadNative(string path, string encoding, string contextName)
        {
            var sysEncoding = Helpers.EsmToSysEncoding(encoding);

            LoadNativeLocalization(Path.Combine(path, contextName + ".cel"), MappingType.Cell, sysEncoding, _cells);
            LoadNativeLocalization(Path.Combine(path, contextName + ".mrk"), MappingType.Topic, sysEncoding, _dialogueNames);
            LoadNativeLocalization(Path.Combine(path, contextName + ".top"), MappingType.Phraseform, sysEncoding, _dialoguePhraseForms);
        }

        public void Load(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetFileNameWithoutExtension(file) != ".json")
                    continue;

                var entries = JsonSerializer.Deserialize<List<MappingRecord>>(File.ReadAllText(file));
                foreach (var entry in entries ?? new())
                {
                    switch (entry.Type)
                    {
                        case MappingType.Cell:
                            _cells.Add(entry);
                            break;
                        case MappingType.Topic:
                            _dialogueNames.Add(entry);
                            break;
                        case MappingType.Phraseform:
                            _dialoguePhraseForms.Add(entry);
                            break;
                    }
                }
            }
        }

        public void Save(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                throw new Exception("Directory '{Path.GetDirectoryName(path)}' does not exist");

            var tmp = _cells.Records.Concat(_dialogueNames.Records).Concat(_dialoguePhraseForms.Records)
                .Select(_ => _.Value);

            var text = JsonSerializer.Serialize(tmp, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            });

            File.WriteAllText(path, text);
        }

        public void Merge(LocalizationStore otherLocalization)
        {
            foreach (var (_, cel) in otherLocalization._cells.Records)
                _cells.Add(cel);
            foreach (var (_, dialogue) in otherLocalization._dialogueNames.Records)
                _dialogueNames.Add(dialogue);
            foreach (var (_, phraseForm) in otherLocalization._dialoguePhraseForms.Records)
                _dialoguePhraseForms.Add(phraseForm);
        }

        public void SaveNative(string path, string contextName, string encoding)
        {
            if (!Directory.Exists(path))
                throw new Exception($"Directory '{path}' does not exist");

            var sysEncoding = Helpers.EsmToSysEncoding(encoding);

            SaveNativeLocalization(Path.Combine(path, contextName + ".cel"), sysEncoding, _cells);
            SaveNativeLocalization(Path.Combine(path, contextName + ".mrk"), sysEncoding, _dialogueNames);
            SaveNativeLocalization(Path.Combine(path, contextName + ".top"), sysEncoding, _dialoguePhraseForms);
        }

        private void LoadNativeLocalization(string filePath, MappingType type, Encoding encoding, MappingStore<MappingRecord> store)
        {
            if (!File.Exists(filePath))
                return;

            using (var reader = new StreamReader(filePath, encoding))
            using (var csv = new CsvReader(reader,
                       new CsvConfiguration(CultureInfo.InvariantCulture)
                       {
                           HasHeaderRecord = false, Delimiter = "\t", Encoding = encoding,
                       }))
            {
                while (csv.Read())
                {
                    store.Add(new MappingRecord()
                    {
                        Type = type,
                        Source = csv.GetField<string>(0),
                        Target = csv.GetField<string>(1),
                    });
                }
            }
        }

        private void SaveNativeLocalization(string path, Encoding encoding, MappingStore<MappingRecord> store)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                throw new Exception("Directory does not exist");

            // overwrite with empty if file exists
            if (store.IsEmpty && !File.Exists(path))
                return;
        
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream, encoding))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                   {
                       Delimiter = "\t", Encoding = encoding,
                       HasHeaderRecord = false,
                   }))
            {
                foreach (var item in store.Records)
                {
                    csv.WriteField(item.Value.Source);
                    csv.WriteField(item.Value.Target);

                    csv.NextRecord();
                }
            }
        }

        public void UpdateFromState(TranslationState state)
        {
            UpdateMrk(state);
            UpdateCel(state);
        }

        private void UpdateMrk(TranslationState state)
        {
            var dialogues = state.Storage.RecordsByContextAndId.GetValueOrDefault("DIAL");
            if (dialogues == null)
                return;

            foreach (var (_, dialogue) in dialogues)
                if (dialogue.IsTranslated)
                    _dialogueNames.Add(new MappingRecord()
                        { Type = MappingType.Topic, Source = dialogue.UnprocessedOriginalText, Target = dialogue.Text });
        }

        private void UpdateCel(TranslationState state)
        {
            var cells = state.Storage.RecordsByContextAndId.GetValueOrDefault("CELL");
            if (cells != null)
            {
                foreach (var (_, cell) in cells)
                    if (cell.IsTranslated)
                        _cells.Add(new MappingRecord()
                            { Type = MappingType.Cell, Source = cell.UnprocessedOriginalText, Target = cell.Text });
            }

            var regions = state.Storage.RecordsByContextAndId.GetValueOrDefault("REGN");
            if (regions != null)
            {
                foreach (var (_, region) in regions)
                    if (region.IsTranslated)
                        _cells.Add(new MappingRecord()
                            { Type = MappingType.Cell, Source = region.UnprocessedOriginalText, Target = region.Text });
            }
        }
    }
}
