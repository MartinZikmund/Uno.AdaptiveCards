// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveColumn : AdaptiveStyledCollectionElement
    {
        private string _width = "auto";
        private uint _pixelWidth;

        public AdaptiveColumn() : base(ElementType.Column, "Column")
        {
        }

        /// <summary>"auto", "stretch", a relative weight (e.g. "2") or an explicit pixel width (e.g. "50px").</summary>
        public string Width
        {
            get => _width;
            set => SetWidth(value, null);
        }

        public uint PixelWidth
        {
            get => _pixelWidth;
            set
            {
                _pixelWidth = value;
                _width = value + "px";
            }
        }

        public IList<IAdaptiveCardElement> Items { get; } = new List<IAdaptiveCardElement>();

        public bool? Rtl { get; set; }

        internal void SetWidth(string value, ParseContext context)
        {
            _width = ParseUtil.ToLowercase(value);
            _pixelWidth = (uint)(ParseUtil.ParseSizeForPixelSize(_width, context) ?? 0);
        }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("items");
            knownProperties.Add("rtl");
            knownProperties.Add("width");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (!string.IsNullOrEmpty(_width))
            {
                root["width"] = _width;
            }

            var items = new JsonArray();
            foreach (var item in Items)
            {
                items.Add((JsonNode)item.ToJson());
            }

            root["items"] = items;
            if (Rtl.HasValue)
            {
                root["rtl"] = Rtl.Value;
            }

            return root;
        }

        internal override void DeserializeChildren(ParseContext context, JsonObject json)
        {
            foreach (var item in ParseUtil.GetElementCollection(true, context, json, "items", false))
            {
                Items.Add(item);
            }
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            base.GetResourceInformation(resourceInformation);
            GetResourceInformation(Items, resourceInformation);
        }

        internal static AdaptiveColumn Deserialize(ParseContext context, JsonObject json)
        {
            var column = new AdaptiveColumn();
            column.DeserializeStyledCollection(context, json);

            if (column.FallbackContent != null)
            {
                bool isFallbackColumn = column.FallbackContent.ElementTypeString == "Column";
                if (!isFallbackColumn)
                {
                    context.AddWarning(WarningStatusCode.UnknownElementType, "Column Fallback must be a Column. Fallback content dropped.");
                    column.FallbackContent = null;
                    column.FallbackType = FallbackType.None;
                }
            }

            var columnWidth = ParseUtil.GetValueAsString(json, "width");
            if (string.IsNullOrEmpty(columnWidth))
            {
                columnWidth = ParseUtil.GetValueAsString(json, "size");
            }

            column.SetWidth(ParseUtil.ToLowercase(columnWidth), context);
            column.Rtl = ParseUtil.GetOptionalBool(json, "rtl");
            return column;
        }
    }

    public sealed class AdaptiveColumnParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveColumn.Deserialize(context, json);
    }
}
