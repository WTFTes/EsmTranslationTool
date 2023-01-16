using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TranslationLibrary.Enums;
using TranslationLibrary.Storage;

namespace TranslationLibrary.Glossary;

public class GlossaryStorage : TextStorage<GlossaryRecord>
{

    private readonly List<string> skippedContextsForPlainDump = new()
    {
        "INFO"
    };

    public DumpInfo DumpCsv(string filePath)
    {
        DumpInfo result = new();
        using (var writer = new StreamWriter(filePath))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
               {
                   Delimiter = "\t", Encoding = Encoding.UTF8,
                   HasHeaderRecord = false,
               }))
        {
            foreach (var (context, byId) in RecordsByContext)
            {
                if (skippedContextsForPlainDump.Contains(context))
                    continue;
                
                foreach (var record in byId)
                {
                    if (record.Type != TextType.Text)
                        continue;

                    csv.WriteField(record.OriginalText);
                    csv.WriteField(record.Text);

                    csv.NextRecord();

                    ++result.Total;
                }
            }
        }

        return result;
    }
}
