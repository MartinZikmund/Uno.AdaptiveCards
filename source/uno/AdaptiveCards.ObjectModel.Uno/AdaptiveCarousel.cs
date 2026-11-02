// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveCarouselPage : AdaptiveContainer
    {
        public AdaptiveCarouselPage() : base(ElementType.CarouselPage, "CarouselPage")
        {
        }

        internal static AdaptiveCarouselPage DeserializePage(ParseContext context, JsonObject json)
        {
            var page = new AdaptiveCarouselPage();
            page.DeserializeStyledCollection(context, json);
            page.Rtl = ParseUtil.GetOptionalBool(json, "rtl");
            return page;
        }
    }

    public sealed class AdaptiveCarouselPageParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveCarouselPage.DeserializePage(context, json);
    }

    public sealed class AdaptiveCarousel : AdaptiveStyledCollectionElement
    {
        internal static readonly string[] ProhibitedTypes = { "Carousel", "Action.ShowCard", "Action.ToggleVisibility" };

        private CarouselOrientation? _orientation;

        public AdaptiveCarousel() : base(ElementType.Carousel, "Carousel")
        {
        }

        public IList<AdaptiveCarouselPage> Pages { get; } = new List<AdaptiveCarouselPage>();

        public CarouselOrientation Orientation
        {
            get => _orientation ?? CarouselOrientation.Horizontal;
            set => _orientation = value;
        }

        public ulong? Timer { get; set; }

        public uint? InitialPage { get; set; }

        public bool? AutoLoop { get; set; }

        public bool? Rtl { get; set; }

        public uint HeightInPixels { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("heightInPixels");
            knownProperties.Add("initialPage");
            knownProperties.Add("loop");
            knownProperties.Add("orientation");
            knownProperties.Add("pages");
            knownProperties.Add("timer");
            knownProperties.Add("rtl");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (HeightInPixels != 0)
            {
                root["heightInPixels"] = HeightInPixels + "px";
            }

            if (InitialPage.HasValue)
            {
                root["initialPage"] = InitialPage.Value;
            }

            if (AutoLoop.HasValue)
            {
                root["loop"] = AutoLoop.Value;
            }

            if (Pages.Count > 0)
            {
                var pages = new JsonArray();
                foreach (var page in Pages)
                {
                    pages.Add((JsonNode)page.ToJson());
                }

                root["pages"] = pages;
            }

            if (_orientation.HasValue)
            {
                root["orientation"] = EnumStrings.CarouselOrientation.ToString(_orientation.Value);
            }

            if (Timer.HasValue)
            {
                root["timer"] = Timer.Value;
            }

            if (Rtl.HasValue)
            {
                root["rtl"] = Rtl.Value;
            }

            return root;
        }

        internal override void DeserializeChildren(ParseContext context, JsonObject json)
        {
            var pages = ParseUtil.GetElementCollection(
                true, context, json, "pages", false, null, (ctx, j) => ParseUtil.ParseCardElement(ctx, j) as AdaptiveCarouselPage);
            foreach (var page in pages)
            {
                if (page != null)
                {
                    Pages.Add(page);
                }
            }
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            base.GetResourceInformation(resourceInformation);
            foreach (var page in Pages)
            {
                page.GetResourceInformation(resourceInformation);
            }
        }

        internal static AdaptiveCarousel Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Carousel");
            context.AddProhibitedElementType(ProhibitedTypes);

            var carousel = new AdaptiveCarousel();
            carousel.DeserializeStyledCollection(context, json);

            var heightInPixels = ParseUtil.ParseSizeForPixelSize(ParseUtil.GetString(json, "heightInPixels"), context);
            if (heightInPixels.HasValue)
            {
                carousel.HeightInPixels = (uint)heightInPixels.Value;
            }

            carousel.InitialPage = ParseUtil.GetOptionalUnsignedInt(json, "initialPage");
            carousel.Timer = ParseUtil.GetOptionalUnsignedInt(json, "timer");
            carousel.AutoLoop = ParseUtil.GetOptionalBool(json, "loop");
            carousel._orientation = ParseUtil.GetOptionalEnumValue(json, "orientation", EnumStrings.CarouselOrientation);
            carousel.Rtl = ParseUtil.GetOptionalBool(json, "rtl");

            context.RemoveProhibitedElementType(ProhibitedTypes);
            return carousel;
        }
    }

    public sealed class AdaptiveCarouselParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveCarousel.Deserialize(context, json);
    }
}
