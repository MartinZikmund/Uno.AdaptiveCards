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

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("images");
            knownProperties.Add("imageSize");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (ImageSize != ImageSize.None)
            {
                root["imageSize"] = EnumStrings.ImageSize.ToString(ImageSize);
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
