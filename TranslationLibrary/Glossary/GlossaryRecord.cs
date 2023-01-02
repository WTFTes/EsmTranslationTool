using System;
using System.Text.Json.Nodes;
using TranslationLibrary.Enums;

namespace TranslationLibrary.Glossary
{
    public class GlossaryRecord : EntityRecord
    {
        public string OriginalText { get; set; } = "";

        public MatchType MatchType { get; set; }

        public override string SubContext { get; set; } = "";

        public override JsonObject FormatForDump()
        {
            var obj = new JsonObject();
            obj.Add(new("id", GetUniqId()));
            obj.Add(new("source", OriginalText));
            obj.Add(new("target", Text));
            obj.Add(new("type", Type.ToString()));
            obj.Add(new("match_type", MatchType.ToString()));

            return obj;
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
