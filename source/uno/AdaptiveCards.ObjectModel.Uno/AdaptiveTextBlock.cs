// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveTextBlock : AdaptiveCardElementBase, IAdaptiveTextElement
    {
        private string _text = string.Empty;

        public AdaptiveTextBlock() : base(ElementType.TextBlock, "TextBlock")
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

        public bool Wrap { get; set; }

        public HAlignment? HorizontalAlignment { get; set; }

        public uint MaxLines { get; set; }

        public TextStyle? Style { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            TextElementProperties.PopulateKnownProperties(knownProperties);
            knownProperties.Add("wrap");
            knownProperties.Add("style");
            knownProperties.Add("maxLines");
            knownProperties.Add("horizontalAlignment");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            TextElementProperties.Serialize(this, root);

            if (HorizontalAlignment.HasValue)
            {
                root["horizontalAlignment"] = EnumStrings.HAlignment.ToString(HorizontalAlignment.Value);
            }

            if (MaxLines != 0)
            {
                root["maxLines"] = MaxLines;
            }

            if (Wrap)
            {
                root["wrap"] = true;
            }

            if (Style.HasValue)
            {
                root["style"] = EnumStrings.TextStyle.ToString(Style.Value);
            }

            return root;
        }

        internal static AdaptiveTextBlock Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "TextBlock");
            var textBlock = new AdaptiveTextBlock();
            textBlock.DeserializeBaseProperties(context, json);
            TextElementProperties.Deserialize(textBlock, context, json);
            textBlock.Wrap = ParseUtil.GetBool(json, "wrap", false);
            textBlock.Style = ParseUtil.GetOptionalEnumValue(json, "style", EnumStrings.TextStyle);
            textBlock.MaxLines = ParseUtil.GetUInt(json, "maxLines", 0);
            textBlock.HorizontalAlignment = ParseUtil.GetOptionalEnumValue(json, "horizontalAlignment", EnumStrings.HAlignment);
            return textBlock;
        }
    }

    public sealed class AdaptiveTextBlockParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveTextBlock.Deserialize(context, json);
    }
}
