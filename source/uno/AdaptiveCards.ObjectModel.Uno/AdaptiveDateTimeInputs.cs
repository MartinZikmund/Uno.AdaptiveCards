// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveDateInput : AdaptiveInputElementBase
    {
        public AdaptiveDateInput() : base(ElementType.DateInput, "Input.Date")
        {
        }

        public string Max { get; set; } = string.Empty;

        public string Min { get; set; } = string.Empty;

        public string Placeholder { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("max");
            knownProperties.Add("min");
            knownProperties.Add("value");
            knownProperties.Add("placeholder");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            DateTimeInputHelpers.Serialize(root, Max, Min, Placeholder, Value);
            return root;
        }

        internal static AdaptiveDateInput Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Input.Date");
            var input = new AdaptiveDateInput();
            input.DeserializeInputBaseProperties(context, json);
            input.Max = ParseUtil.GetString(json, "max");
            input.Min = ParseUtil.GetString(json, "min");
            input.Placeholder = ParseUtil.GetString(json, "placeholder");
            input.Value = ParseUtil.GetString(json, "value");
            return input;
        }
    }

    public sealed class AdaptiveDateInputParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveDateInput.Deserialize(context, json);
    }

    public sealed class AdaptiveTimeInput : AdaptiveInputElementBase
    {
        public AdaptiveTimeInput() : base(ElementType.TimeInput, "Input.Time")
        {
        }

        public string Max { get; set; } = string.Empty;

        public string Min { get; set; } = string.Empty;

        public string Placeholder { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("max");
            knownProperties.Add("min");
            knownProperties.Add("placeholder");
            knownProperties.Add("value");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            DateTimeInputHelpers.Serialize(root, Max, Min, Placeholder, Value);
            return root;
        }

        internal static AdaptiveTimeInput Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Input.Time");
            var input = new AdaptiveTimeInput();
            input.DeserializeInputBaseProperties(context, json);
            input.Max = ParseUtil.GetString(json, "max");
            input.Min = ParseUtil.GetString(json, "min");
            input.Placeholder = ParseUtil.GetString(json, "placeholder");
            input.Value = ParseUtil.GetString(json, "value");
            return input;
        }
    }

    public sealed class AdaptiveTimeInputParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveTimeInput.Deserialize(context, json);
    }

    internal static class DateTimeInputHelpers
    {
        public static void Serialize(JsonObject root, string max, string min, string placeholder, string value)
        {
            if (!string.IsNullOrEmpty(max))
            {
                root["max"] = max;
            }

            if (!string.IsNullOrEmpty(min))
            {
                root["min"] = min;
            }

            if (!string.IsNullOrEmpty(placeholder))
            {
                root["placeholder"] = placeholder;
            }

            if (!string.IsNullOrEmpty(value))
            {
                root["value"] = value;
            }
        }
    }
}
