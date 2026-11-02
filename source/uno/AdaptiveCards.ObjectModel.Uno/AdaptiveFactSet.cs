// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveFact
    {
        public AdaptiveFact()
        {
        }

        public ElementType ElementType => ElementType.Fact;

        public string Title { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string Language { get; set; } = string.Empty;

        internal JsonObject ToJson() => new() { ["title"] = Title ?? string.Empty, ["value"] = Value ?? string.Empty };

        internal static AdaptiveFact Deserialize(ParseContext context, JsonNode jsonNode)
        {
            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            var title = ParseUtil.GetString(json, "title", false);
            var value = ParseUtil.GetString(json, "value", false);
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(value))
            {
                context.AddWarning(
                    WarningStatusCode.RequiredPropertyMissing, "non-empty string has to be given for either title or value, none given");
            }

            return new AdaptiveFact { Title = title, Value = value, Language = context.Language };
        }
    }

    public sealed class AdaptiveFactSet : AdaptiveCardElementBase
    {
        public AdaptiveFactSet() : base(ElementType.FactSet, "FactSet")
        {
        }

        public IList<AdaptiveFact> Facts { get; } = new List<AdaptiveFact>();

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("facts");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            var facts = new JsonArray();
            foreach (var fact in Facts)
            {
                facts.Add((JsonNode)fact.ToJson());
            }

            root["facts"] = facts;
            return root;
        }

        internal static AdaptiveFactSet Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "FactSet");
            var factSet = new AdaptiveFactSet();
            factSet.DeserializeBaseProperties(context, json);
            var facts = ParseUtil.GetElementCollectionOfSingleType(context, json, "facts", AdaptiveFact.Deserialize, false);
            if (facts.Count == 0)
            {
                context.AddWarning(WarningStatusCode.RequiredPropertyMissing, "required property, \"fact\", is missing");
            }

            foreach (var fact in facts)
            {
                factSet.Facts.Add(fact);
            }

            return factSet;
        }
    }

    public sealed class AdaptiveFactSetParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveFactSet.Deserialize(context, json);
    }
}
