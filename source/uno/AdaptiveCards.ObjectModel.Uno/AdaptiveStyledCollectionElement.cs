// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>
    /// Port of the shared StyledCollectionElement (base for Container, Column, ColumnSet, TableCell, Carousel, CarouselPage).
    /// </summary>
    public abstract class AdaptiveStyledCollectionElement : AdaptiveCardElementBase, IAdaptiveContainerBase
    {
        protected AdaptiveStyledCollectionElement(ElementType elementType, string elementTypeString) : base(elementType, elementTypeString)
        {
        }

        public ContainerStyle Style { get; set; } = ContainerStyle.None;

        public IAdaptiveActionElement SelectAction { get; set; }

        public bool Bleed { get; set; }

        public BleedDirection BleedDirection { get; internal set; } = BleedDirection.All;

        public uint MinHeight { get; set; }

        public VerticalContentAlignment? VerticalContentAlignment { get; set; }

        public AdaptiveBackgroundImage BackgroundImage { get; set; }

        internal bool HasPadding { get; set; }

        internal uint ParentalId { get; set; }

        internal abstract void DeserializeChildren(ParseContext context, JsonObject json);

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("bleed");
            knownProperties.Add("style");
            knownProperties.Add("verticalContentAlignment");
            knownProperties.Add("selectAction");
            knownProperties.Add("backgroundImage");
            knownProperties.Add("minHeight");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            SerializeSelectAction(root, SelectAction);

            if (BackgroundImage != null && !string.IsNullOrEmpty(BackgroundImage.Url))
            {
                root["backgroundImage"] = BackgroundImage.ToJson();
            }

            if (Style != ContainerStyle.None)
            {
                root["style"] = EnumStrings.ContainerStyle.ToString(Style);
            }

            if (VerticalContentAlignment.HasValue)
            {
                root["verticalContentAlignment"] = EnumStrings.VerticalContentAlignment.ToString(VerticalContentAlignment.Value);
            }

            if (Bleed)
            {
                root["bleed"] = true;
            }

            if (MinHeight != 0)
            {
                root["minHeight"] = MinHeight + "px";
            }

            return root;
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            if (BackgroundImage != null)
            {
                resourceInformation.Add(new AdaptiveRemoteResourceInformation(BackgroundImage.Url, "image"));
            }
        }

        private void ConfigForContainerStyle(ParseContext context)
        {
            HasPadding = Style != ContainerStyle.None && context.GetParentalContainerStyle() != Style;

            var id = context.PaddingParentInternalId();
            bool canBleed = HasPadding && Bleed;
            if (canBleed && context.GetBleedDirection() != BleedDirection.None)
            {
                ParentalId = id;
                BleedDirection = context.GetBleedDirection();
            }
            else
            {
                BleedDirection = BleedDirection.None;
            }
        }

        /// <summary>Port of StyledCollectionElement::Deserialize&lt;T&gt;.</summary>
        internal void DeserializeStyledCollection(ParseContext context, JsonObject json)
        {
            DeserializeBaseProperties(context, json);

            BackgroundImage = AdaptiveBackgroundImage.Deserialize(ParseUtil.ExtractJsonValue(json, "backgroundImage"));

            bool canFallbackToAncestor = context.CanFallbackToAncestor;
            context.CanFallbackToAncestor = canFallbackToAncestor || FallbackType != FallbackType.None;
            CanFallbackToAncestor = canFallbackToAncestor;

            Style = ParseUtil.GetEnumValue(json, "style", ContainerStyle.None, EnumStrings.ContainerStyle);
            VerticalContentAlignment = ParseUtil.GetOptionalEnumValue(json, "verticalContentAlignment", EnumStrings.VerticalContentAlignment);
            Bleed = ParseUtil.GetBool(json, "bleed", false);

            var minHeight = ParseUtil.ParseSizeForPixelSize(ParseUtil.GetString(json, "minHeight"), context);
            if (minHeight.HasValue)
            {
                MinHeight = (uint)minHeight.Value;
            }

            ConfigForContainerStyle(context);
            context.SaveContextForStyledCollectionElement(this);
            DeserializeChildren(context, json);
            context.RestoreContextForStyledCollectionElement(this);
            context.CanFallbackToAncestor = canFallbackToAncestor;

            SelectAction = ParseUtil.GetAction(context, json, "selectAction", false);
        }
    }
}
