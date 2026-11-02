// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveColumnSet : AdaptiveStyledCollectionElement
    {
        public AdaptiveColumnSet() : base(ElementType.ColumnSet, "ColumnSet")
        {
        }

        public IList<AdaptiveColumn> Columns { get; } = new List<AdaptiveColumn>();

        public HAlignment? HorizontalAlignment { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("columns");
            knownProperties.Add("horizontalAlignment");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            var columns = new JsonArray();
            foreach (var column in Columns)
            {
                columns.Add((JsonNode)column.ToJson());
            }

            root["columns"] = columns;
            if (HorizontalAlignment.HasValue)
            {
                root["horizontalAlignment"] = EnumStrings.HAlignment.ToString(HorizontalAlignment.Value);
            }

            return root;
        }

        internal override void DeserializeChildren(ParseContext context, JsonObject json)
        {
            var columns = ParseUtil.GetElementCollection(
                false, context, json, "columns", false, "Column", (ctx, j) => ParseUtil.ParseCardElement(ctx, j) as AdaptiveColumn);

            int counts = 0;
            AdaptiveColumn current = null;
            foreach (var column in columns)
            {
                if (column == null)
                {
                    continue;
                }

                Columns.Add(column);
                var width = column.Width;
                if (string.IsNullOrEmpty(width))
                {
                    continue;
                }

                var ch = width[0];
                if (column.PixelWidth == 0 && ch != 'a' && ch != 's' && char.IsDigit(ch))
                {
                    counts++;
                    current = column;
                }
            }

            if (counts == 1 && current != null)
            {
                current.Width = "stretch";
            }
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            base.GetResourceInformation(resourceInformation);
            foreach (var column in Columns)
            {
                column.GetResourceInformation(resourceInformation);
            }
        }

        internal static AdaptiveColumnSet Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "ColumnSet");
            var columnSet = new AdaptiveColumnSet();
            columnSet.DeserializeStyledCollection(context, json);
            columnSet.HorizontalAlignment = ParseUtil.GetOptionalEnumValue(json, "horizontalAlignment", EnumStrings.HAlignment);
            return columnSet;
        }
    }

    public sealed class AdaptiveColumnSetParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveColumnSet.Deserialize(context, json);
    }
}
