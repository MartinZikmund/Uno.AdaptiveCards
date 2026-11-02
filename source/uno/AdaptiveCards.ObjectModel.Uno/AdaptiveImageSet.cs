// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveImageSet : AdaptiveCardElementBase
    {
        public AdaptiveImageSet() : base(ElementType.ImageSet, "ImageSet")
        {
        }

        public IList<AdaptiveImage> Images { get; } = new List<AdaptiveImage>();

        public ImageSize ImageSize { get; set; } = ImageSize.None;

        /// <summary>Presentation style ("style": default, stacked, grid). Adaptive Cards 1.6, not in the WinUI3 projection.</summary>
        public ImageSetStyle Style { get; set; } = ImageSetStyle.Default;

        /// <summary>Pixel offset between images in the stacked style ("offset"). Not in the WinUI3 projection.</summary>
        public double Offset { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("images");
            knownProperties.Add("imageSize");
            knownProperties.Add("style");
            knownProperties.Add("offset");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (ImageSize != ImageSize.None)
            {
                root["imageSize"] = EnumStrings.ImageSize.ToString(ImageSize);
            }

            if (Style != ImageSetStyle.Default)
            {
                root["style"] = EnumStrings.ImageSetStyle.ToString(Style);
            }

            if (Offset != 0)
            {
                root["offset"] = Offset;
            }

            var images = new JsonArray();
            foreach (var image in Images)
            {
                images.Add((JsonNode)image.ToJson());
            }

            root["images"] = images;
            return root;
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            foreach (var image in Images)
            {
                image.GetResourceInformation(resourceInformation);
            }
        }

        internal static AdaptiveImageSet Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "ImageSet");
            var imageSet = new AdaptiveImageSet();
            imageSet.DeserializeBaseProperties(context, json);
            imageSet.ImageSize = ParseUtil.GetEnumValue(json, "imageSize", ImageSize.None, EnumStrings.ImageSize);
            imageSet.Style = ParseUtil.GetEnumValue(json, "style", ImageSetStyle.Default, EnumStrings.ImageSetStyle);
            imageSet.Offset = ParseUtil.GetOptionalDouble(json, "offset") ?? 0;

            var images = ParseUtil.GetElementCollection(
                true, context, json, "images", true, "Image", (ctx, j) => ParseUtil.ParseCardElement(ctx, j) as AdaptiveImage);
            foreach (var image in images)
            {
                if (image != null)
                {
                    imageSet.Images.Add(image);
                }
            }

            return imageSet;
        }
    }

    public sealed class AdaptiveImageSetParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveImageSet.Deserialize(context, json);
    }
}
