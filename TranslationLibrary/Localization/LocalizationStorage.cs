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
    public class LocalizationStorage
    {
        private readonly LocalizationMappingStore _cells = new();
        private readonly LocalizationMappingStore _dialogueNames = new();
        private readonly LocalizationMappingStore _dialoguePhraseForms = new();

        public LocalizationMappingStore Cells => _cells;
        public LocalizationMappingStore DialogueNames => _dialogueNames;
        public LocalizationMappingStore DialoguePhraseForms => _dialoguePhraseForms;

        public int Size => _cells.Size + _dialogueNames.Size + _dialoguePhraseForms.Size;
        public bool IsEmpty => _cells.Empty && _dialogueNames.Empty && _dialoguePhraseForms.Empty;

        public void Clear()
        {
            _cells.Clear();
            _dialogueNames.Clear();
            _dialoguePhraseForms.Clear();
        }
        
        public void LoadNative(string path, string encoding, string contextName)
        {
            var sysEncoding = EsmEncoding.ToSysEncoding(encoding);

            LoadNativeLocalization(Path.Combine(path, contextName + ".cel"), MappingType.Cell, sysEncoding);
            LoadNativeLocalization(Path.Combine(path, contextName + ".mrk"), MappingType.Topic, sysEncoding);
            LoadNativeLocalization(Path.Combine(path, contextName + ".top"), MappingType.Phraseform, sysEncoding);
        }

        public void Load(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetFileNameWithoutExtension(file) != ".json")
                    continue;

                var entries = JsonSerializer.Deserialize<List<MappingRecord<string>>>(File.ReadAllText(file));
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
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                throw new Exception($"Directory '{Path.GetDirectoryName(path)}' does not exist");

            var tmp = _cells.Concat(_dialogueNames).Concat(_dialoguePhraseForms);

            var text = JsonSerializer.Serialize(tmp, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            });

            File.WriteAllText(path, text);
        }

        public void Merge(LocalizationStorage otherLocalization)
        {
            foreach (var cel in otherLocalization._cells)
                _cells.Add(cel);
            foreach (var dialogue in otherLocalization._dialogueNames)
                _dialogueNames.Add(dialogue);
            foreach (var phraseForm in otherLocalization._dialoguePhraseForms)
                _dialoguePhraseForms.Add(phraseForm);
        }

        public LocalizationStorage Diff(LocalizationStorage otherLocalization)
        {
            LocalizationStorage result = new();
            foreach (var cel in _cells)
                if (!otherLocalization.Cells.Contains(cel))
                    result.Cells.Add(cel);
            foreach (var dialogue in _dialogueNames)
                if (!otherLocalization.DialogueNames.Contains(dialogue))
                    result.DialogueNames.Add(dialogue);
            foreach (var phraseForm in _dialoguePhraseForms)
                if (!otherLocalization.DialoguePhraseForms.Contains(phraseForm))
                    result.DialoguePhraseForms.Add(phraseForm);

            return result;
        }

        public void SaveNative(string path, string contextName, string encoding)
        {
            if (!Directory.Exists(path))
                throw new Exception($"Directory '{path}' does not exist");

            var sysEncoding = EsmEncoding.ToSysEncoding(encoding);

            SaveNativeLocalization(Path.Combine(path, contextName + ".cel"), sysEncoding, _cells);
            SaveNativeLocalization(Path.Combine(path, contextName + ".mrk"), sysEncoding, _dialogueNames);
            SaveNativeLocalization(Path.Combine(path, contextName + ".top"), sysEncoding, _dialoguePhraseForms);
        }

        public void LoadNativeLocalization(string filePath, MappingType type, Encoding encoding)
        {
            if (!File.Exists(filePath))
                return;

            using var reader = new StreamReader(filePath, encoding);
            using var csv = new CsvReader(reader,
                new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false, Delimiter = "\t", Encoding = encoding,
                });

            while (csv.Read())
            {
                MappingRecord<string> record = new()
                {
                    Type = type,
                    Source = csv.GetField<string>(0),
                    Target = csv.GetField<string>(1),
                };

                switch (type)
                {
                    case MappingType.Cell:
                        _cells.Add(record);
                        break;
                    case MappingType.Topic:
                        _dialogueNames.Add(record);
                        break;
                    case MappingType.Phraseform:
                        _dialoguePhraseForms.Add(record);
                        break;
                    default:
                        throw new Exception($"Unsupported mapping type {type}");
                }
            }
        }

        private void SaveNativeLocalization(string path, Encoding encoding, LocalizationMappingStore store)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                throw new Exception("Directory does not exist");

            // overwrite with empty if file exists
            if (store.Empty && !File.Exists(path))
                return;

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream, encoding);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t", Encoding = encoding,
                HasHeaderRecord = false,
            });
            
            foreach (var item in store)
            {
                csv.WriteField(item.Source);
                csv.WriteField(item.Target);

                csv.NextRecord();
            }
        }
    }
}
