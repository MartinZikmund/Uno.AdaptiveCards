// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveTextInput : AdaptiveInputElementBase
    {
        public AdaptiveTextInput() : base(ElementType.TextInput, "Input.Text")
        {
        }

        public bool IsMultiline { get; set; }

        public uint MaxLength { get; set; }

        public string Placeholder { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public TextInputStyle TextInputStyle { get; set; } = TextInputStyle.Text;

        public IAdaptiveActionElement InlineAction { get; set; }

        public string Regex { get; set; } = string.Empty;

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("placeholder");
            knownProperties.Add("value");
            knownProperties.Add("isMultiline");
            knownProperties.Add("maxLength");
            knownProperties.Add("textInput");
            // Not in the shared model's known set, but parsed by it (avoids duplicating them in AdditionalProperties).
            knownProperties.Add("style");
            knownProperties.Add("inlineAction");
            knownProperties.Add("regex");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (IsMultiline)
            {
                root["isMultiline"] = true;
            }

            if (MaxLength != 0)
            {
                root["maxLength"] = MaxLength;
            }

            if (!string.IsNullOrEmpty(Placeholder))
            {
                root["placeholder"] = Placeholder;
            }

            if (!string.IsNullOrEmpty(Value))
            {
                root["value"] = Value;
            }

            if (TextInputStyle != TextInputStyle.Text)
            {
                root["style"] = EnumStrings.TextInputStyle.ToString(TextInputStyle);
            }

            if (InlineAction != null)
            {
                root["inlineAction"] = InlineAction.ToJson();
            }

            if (!string.IsNullOrEmpty(Regex))
            {
                root["regex"] = Regex;
            }

            return root;
        }

        internal static AdaptiveTextInput Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Input.Text");
            var input = new AdaptiveTextInput();
            input.DeserializeInputBaseProperties(context, json);
            input.Placeholder = ParseUtil.GetString(json, "placeholder");
            input.Value = ParseUtil.GetString(json, "value");
            input.MaxLength = ParseUtil.GetUInt(json, "maxLength", 0);
            input.IsMultiline = ParseUtil.GetBool(json, "isMultiline", false);
            input.TextInputStyle = ParseUtil.GetEnumValue(json, "style", TextInputStyle.Text, EnumStrings.TextInputStyle);

            if (input.IsMultiline && input.TextInputStyle == TextInputStyle.Password)
            {
                context.AddWarning(WarningStatusCode.InvalidValue, "Input.Text ignores isMultiline when using password style");
            }

            input.InlineAction = ParseUtil.GetAction(context, json, "inlineAction", false);
            input.Regex = ParseUtil.GetString(json, "regex");
            return input;
        }
    }

    public sealed class AdaptiveTextInputParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveTextInput.Deserialize(context, json);
    }
}
