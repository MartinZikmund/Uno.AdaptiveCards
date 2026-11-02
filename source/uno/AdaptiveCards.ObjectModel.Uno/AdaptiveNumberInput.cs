// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveNumberInput : AdaptiveInputElementBase
    {
        public AdaptiveNumberInput() : base(ElementType.NumberInput, "Input.Number")
        {
        }

        public double? Max { get; set; }

        public double? Min { get; set; }

        public string Placeholder { get; set; } = string.Empty;

        public double? Value { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("placeholder");
            knownProperties.Add("value");
            knownProperties.Add("max");
            knownProperties.Add("min");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (Min.HasValue)
            {
                root["min"] = Min.Value;
            }

            if (Max.HasValue)
            {
                root["max"] = Max.Value;
            }

            if (Value.HasValue)
            {
                root["value"] = Value.Value;
            }

            if (!string.IsNullOrEmpty(Placeholder))
            {
                root["placeholder"] = Placeholder;
            }

            return root;
        }

        internal static AdaptiveNumberInput Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Input.Number");
            var input = new AdaptiveNumberInput();
            input.DeserializeInputBaseProperties(context, json);
            input.Placeholder = ParseUtil.GetString(json, "placeholder");
            input.Value = ParseUtil.GetOptionalDouble(json, "value");
            input.Max = ParseUtil.GetOptionalDouble(json, "max");
            input.Min = ParseUtil.GetOptionalDouble(json, "min");
            return input;
        }
    }

    public sealed class AdaptiveNumberInputParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveNumberInput.Deserialize(context, json);
    }
}
