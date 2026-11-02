// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveChoiceInput
    {
        public AdaptiveChoiceInput()
        {
        }

        public ElementType ElementType => ElementType.ChoiceInput;

        public string Title { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public JsonObject ToJson() => new JsonObject { ["title"] = Title ?? string.Empty, ["value"] = Value ?? string.Empty };

        internal static AdaptiveChoiceInput Deserialize(ParseContext context, JsonNode jsonNode)
        {
            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            return new AdaptiveChoiceInput
            {
                Title = ParseUtil.GetString(json, "title", true),
                Value = ParseUtil.GetString(json, "value", true),
            };
        }
    }

    /// <summary>Port of the shared ChoicesData ("choices.data", Data.Query). Not part of the WinUI3 projection.</summary>
    public sealed class AdaptiveChoicesData
    {
        public const string DataQueryType = "Data.Query";

        public string ChoicesDataType { get; set; } = string.Empty;

        public string Dataset { get; set; } = string.Empty;

        /// <summary>Maximum number of choices to request (schema "count"; not parsed by the shared C++ model).</summary>
        public int? Count { get; set; }

        /// <summary>Number of choices to skip (schema "skip"; not parsed by the shared C++ model).</summary>
        public int? Skip { get; set; }

        internal bool ShouldSerialize() => ChoicesDataType == DataQueryType && !string.IsNullOrEmpty(Dataset);

        public JsonObject ToJson()
        {
            var root = new JsonObject();
            if (!string.IsNullOrEmpty(ChoicesDataType))
            {
                root["type"] = ChoicesDataType;
            }

            if (!string.IsNullOrEmpty(Dataset))
            {
                root["dataset"] = Dataset;
            }

            if (Count.HasValue)
            {
                root["count"] = Count.Value;
            }

            if (Skip.HasValue)
            {
                root["skip"] = Skip.Value;
            }

            return root;
        }

        internal static AdaptiveChoicesData Deserialize(ParseContext context, JsonNode jsonNode)
        {
            var choicesData = new AdaptiveChoicesData();
            if (jsonNode == null)
            {
                return choicesData;
            }

            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            var type = ParseUtil.GetString(json, "type", false);
            var dataset = ParseUtil.GetString(json, "dataset", false);
            bool isValid = true;

            if (string.IsNullOrEmpty(type))
            {
                isValid = false;
                context.AddWarning(WarningStatusCode.RequiredPropertyMissing, "non-empty string has to be given for type in choices.data, none given");
            }

            if (type != DataQueryType)
            {
                isValid = false;
                context.AddWarning(WarningStatusCode.InvalidValue, "Invalid type for Choices.data, only Data.Query is supported");
            }

            if (string.IsNullOrEmpty(dataset))
            {
                isValid = false;
                context.AddWarning(WarningStatusCode.RequiredPropertyMissing, "non-empty string has to be given for dataset in choices.data, none given");
            }

            if (isValid)
            {
                choicesData.ChoicesDataType = type;
                choicesData.Dataset = dataset;
                choicesData.Count = ParseUtil.GetOptionalInt(json, "count");
                choicesData.Skip = ParseUtil.GetOptionalInt(json, "skip");
            }

            return choicesData;
        }
    }

    public sealed class AdaptiveChoiceSetInput : AdaptiveInputElementBase
    {
        public AdaptiveChoiceSetInput() : base(ElementType.ChoiceSetInput, "Input.ChoiceSet")
        {
        }

        public string Value { get; set; } = string.Empty;

        public bool IsMultiSelect { get; set; }

        public string Placeholder { get; set; } = string.Empty;

        public bool Wrap { get; set; }

        public ChoiceSetStyle ChoiceSetStyle { get; set; } = ChoiceSetStyle.Compact;

        public IList<AdaptiveChoiceInput> Choices { get; } = new List<AdaptiveChoiceInput>();

        /// <summary>Dynamic typeahead data source ("choices.data"). Not part of the WinUI3 projection.</summary>
        public AdaptiveChoicesData ChoicesData { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("choices");
            knownProperties.Add("choices.data");
            knownProperties.Add("isMultiSelect");
            knownProperties.Add("placeholder");
            knownProperties.Add("style");
            knownProperties.Add("value");
            knownProperties.Add("wrap");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            root["style"] = EnumStrings.ChoiceSetStyle.ToString(ChoiceSetStyle);

            if (IsMultiSelect)
            {
                root["isMultiSelect"] = true;
            }

            if (!string.IsNullOrEmpty(Value))
            {
                root["value"] = Value;
            }

            if (Wrap)
            {
                root["wrap"] = true;
            }

            if (!string.IsNullOrEmpty(Placeholder))
            {
                root["placeholder"] = Placeholder;
            }

            if (Choices.Count > 0)
            {
                var choices = new JsonArray();
                foreach (var choice in Choices)
                {
                    choices.Add((JsonNode)choice.ToJson());
                }

                root["choices"] = choices;
            }

            if (ChoicesData != null && ChoicesData.ShouldSerialize())
            {
                root["choices.data"] = ChoicesData.ToJson();
            }

            return root;
        }

        internal static AdaptiveChoiceSetInput Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Input.ChoiceSet");
            var choiceSet = new AdaptiveChoiceSetInput();
            choiceSet.DeserializeInputBaseProperties(context, json);
            choiceSet.ChoiceSetStyle = ParseUtil.GetEnumValue(json, "style", ChoiceSetStyle.Compact, EnumStrings.ChoiceSetStyle);
            choiceSet.IsMultiSelect = ParseUtil.GetBool(json, "isMultiSelect", false);
            choiceSet.Value = ParseUtil.GetString(json, "value", false);
            choiceSet.Wrap = ParseUtil.GetBool(json, "wrap", false, false);
            choiceSet.Placeholder = ParseUtil.GetString(json, "placeholder");
            choiceSet.ChoicesData = AdaptiveChoicesData.Deserialize(context, ParseUtil.ExtractJsonValue(json, "choices.data"));

            foreach (var choice in ParseUtil.GetElementCollectionOfSingleType(context, json, "choices", AdaptiveChoiceInput.Deserialize, false))
            {
                choiceSet.Choices.Add(choice);
            }

            if (choiceSet.IsMultiSelect &&
                (choiceSet.ChoiceSetStyle == ChoiceSetStyle.Filtered || choiceSet.ChoicesData.ShouldSerialize()))
            {
                context.AddWarning(WarningStatusCode.InvalidValue, "Input.ChoiceSet does not support filtering with multiselect");
            }

            return choiceSet;
        }
    }

    public sealed class AdaptiveChoiceSetInputParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveChoiceSetInput.Deserialize(context, json);
    }
}
