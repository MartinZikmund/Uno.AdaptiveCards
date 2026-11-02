// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>Port of the shared Container (also the base for TableCell and CarouselPage).</summary>
    public class AdaptiveContainer : AdaptiveStyledCollectionElement, IAdaptiveContainer
    {
        public AdaptiveContainer() : base(ElementType.Container, "Container")
        {
        }

        protected AdaptiveContainer(ElementType elementType, string elementTypeString) : base(elementType, elementTypeString)
        {
        }

        public IList<IAdaptiveCardElement> Items { get; } = new List<IAdaptiveCardElement>();

        public bool? Rtl { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("rtl");
            knownProperties.Add("items");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
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

        internal static AdaptiveContainer Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Container");
            var container = new AdaptiveContainer();
            container.DeserializeStyledCollection(context, json);
            container.Rtl = ParseUtil.GetOptionalBool(json, "rtl");
            return container;
        }
    }

    public sealed class AdaptiveContainerParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveContainer.Deserialize(context, json);
    }
}
