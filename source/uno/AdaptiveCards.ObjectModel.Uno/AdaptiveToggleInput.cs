// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveToggleInput : AdaptiveInputElementBase
    {
        public AdaptiveToggleInput() : base(ElementType.ToggleInput, "Input.Toggle")
        {
        }

        public string Title { get; set; } = string.Empty;

        public bool Wrap { get; set; }

        public string Value { get; set; } = string.Empty;

        public string ValueOff { get; set; } = "false";

        public string ValueOn { get; set; } = "true";

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("title");
            knownProperties.Add("value");
            knownProperties.Add("valueOn");
            knownProperties.Add("valueOff");
            knownProperties.Add("wrap");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            root["title"] = Title ?? string.Empty;

            if (Wrap)
            {
                root["wrap"] = true;
            }

            if (!string.IsNullOrEmpty(Value))
            {
                root["value"] = Value;
            }

            if (ValueOff != "false")
            {
                root["valueOff"] = ValueOff;
            }

            if (ValueOn != "true")
            {
                root["valueOn"] = ValueOn;
            }

            return root;
        }

        internal static AdaptiveToggleInput Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Input.Toggle");
            var input = new AdaptiveToggleInput();
            input.DeserializeInputBaseProperties(context, json);
            input.Title = ParseUtil.GetString(json, "title", true);
            input.Value = ParseUtil.GetString(json, "value");
            input.Wrap = ParseUtil.GetBool(json, "wrap", false, false);
            input.ValueOff = ParseUtil.GetString(json, "valueOff", "false", false);
            input.ValueOn = ParseUtil.GetString(json, "valueOn", "true", false);
            return input;
        }
    }

    public sealed class AdaptiveToggleInputParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveToggleInput.Deserialize(context, json);
    }
}
