using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using TranslationLibrary.Enums;

namespace TranslationLibrary.Glossary
{
    public class GlossaryRecord : TextRecord
    {
        public MatchType MatchType { get; set; }
        
        public string OriginalText { get; set; }

        public override JsonObject FormatForDump(DumpFlags optionsFlags)
        {
            return new JsonObject(new List<KeyValuePair<string, JsonNode?>>()
            {
                new("id", ContextId),
                new("source", OriginalText),
                new("target", Text),
                new("type", Type.ToString()),
                new("match_type", MatchType.ToString()),
            });
        }

        public override void FromDump(JsonObject obj)
        {
            foreach (var p in obj)
            {
                switch (p.Key)
                {
                    case "id":
                        ContextId = p.Value.ToString();
                        break;
                    case "source":
                        OriginalText = p.Value.ToString();
                        break;
                    case "target":
                        Text = p.Value.ToString();
                        break;
                    case "match_type":
                        MatchType = Enum.Parse<MatchType>(p.Value.ToString());
                        break;
                    case "type":
                        Type = Enum.Parse<TextType>(p.Value.ToString());
                        break;
                }
            }
        }
    }
}
