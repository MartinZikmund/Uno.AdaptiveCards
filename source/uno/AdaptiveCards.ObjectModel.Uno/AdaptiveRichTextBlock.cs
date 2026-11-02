// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveTextRun : IAdaptiveInline, IAdaptiveTextElement
    {
        private static readonly HashSet<string> s_knownProperties = new()
        {
            "type", "text", "size", "color", "weight", "fontType", "isSubtle", "italic", "strikethrough", "underline", "highlight", "selectAction",
        };

        private string _text = string.Empty;

        public AdaptiveTextRun()
        {
        }

        public TextSize? Size { get; set; }

        public TextWeight? Weight { get; set; }

        public ForegroundColor? Color { get; set; }

        public string Text
        {
            get => _text;
            set => _text = TextElementProperties.ProcessHtmlEntities(value);
        }

        public bool? IsSubtle { get; set; }

        public string Language { get; set; } = string.Empty;

        public FontType? FontType { get; set; }

        public bool Italic { get; set; }

        public bool Strikethrough { get; set; }

        public bool Highlight { get; set; }

        public bool Underline { get; set; }

        public IAdaptiveActionElement SelectAction { get; set; }

        /// <summary>Unknown properties found on the TextRun JSON (not part of the WinUI3 projection).</summary>
        public JsonObject AdditionalProperties { get; set; } = new JsonObject();

        public JsonObject ToJson()
        {
            var root = (JsonObject)(AdditionalProperties?.DeepClone() ?? new JsonObject());
            TextElementProperties.Serialize(this, root);
            if (Italic)
            {
                root["italic"] = true;
            }

            if (Strikethrough)
            {
                root["strikethrough"] = true;
            }

            if (Underline)
            {
                root["underline"] = true;
            }

            root["type"] = "TextRun";
            if (Highlight)
            {
                root["highlight"] = true;
            }

            if (SelectAction != null)
            {
                root["selectAction"] = SelectAction.ToJson();
            }

            return root;
        }

        internal static AdaptiveTextRun Deserialize(ParseContext context, JsonNode jsonNode)
        {
            var textRun = new AdaptiveTextRun();
            if (ParseUtil.IsString(jsonNode))
            {
                textRun.Text = jsonNode.GetValue<string>();
            }
            else
            {
                var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
                ParseUtil.ExpectTypeString(json, "TextRun");
                TextElementProperties.Deserialize(textRun, context, json);
                textRun.Italic = ParseUtil.GetBool(json, "italic", false);
                textRun.Strikethrough = ParseUtil.GetBool(json, "strikethrough", false);
                textRun.Underline = ParseUtil.GetBool(json, "underline", false);
                textRun.Highlight = ParseUtil.GetBool(json, "highlight", false);
                textRun.SelectAction = ParseUtil.GetAction(context, json, "selectAction", false);
                textRun.AdditionalProperties = ParseUtil.HandleUnknownProperties(json, s_knownProperties);
            }

            return textRun;
        }
    }

    public sealed class AdaptiveRichTextBlock : AdaptiveCardElementBase
    {
        public AdaptiveRichTextBlock() : base(ElementType.RichTextBlock, "RichTextBlock")
        {
        }

        public HAlignment? HorizontalAlignment { get; set; }

        public IList<IAdaptiveInline> Inlines { get; } = new List<IAdaptiveInline>();

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("horizontalAlignment");
            knownProperties.Add("inlines");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (HorizontalAlignment.HasValue)
            {
                root["horizontalAlignment"] = EnumStrings.HAlignment.ToString(HorizontalAlignment.Value);
            }

            var inlines = new JsonArray();
            foreach (var inline in Inlines)
            {
                if (inline is AdaptiveTextRun textRun)
                {
                    inlines.Add((JsonNode)textRun.ToJson());
                }
            }

            root["inlines"] = inlines;
            return root;
        }

        internal static AdaptiveRichTextBlock Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "RichTextBlock");
            var richTextBlock = new AdaptiveRichTextBlock();
            richTextBlock.DeserializeBaseProperties(context, json);
            richTextBlock.HorizontalAlignment = ParseUtil.GetOptionalEnumValue(json, "horizontalAlignment", EnumStrings.HAlignment);
            foreach (var inline in ParseUtil.GetElementCollectionOfSingleType(context, json, "inlines", AdaptiveTextRun.Deserialize, false))
            {
                richTextBlock.Inlines.Add(inline);
            }

            return richTextBlock;
        }
    }

    public sealed class AdaptiveRichTextBlockParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveRichTextBlock.Deserialize(context, json);
    }
}
