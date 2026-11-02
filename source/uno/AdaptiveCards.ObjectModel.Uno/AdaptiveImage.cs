// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveImage : AdaptiveCardElementBase
    {
        public AdaptiveImage() : base(ElementType.Image, "Image")
        {
        }

        public string Url { get; set; } = string.Empty;

        public string BackgroundColor { get; set; } = string.Empty;

        public ImageStyle Style { get; set; } = ImageStyle.Default;

        public ImageSize Size { get; set; } = ImageSize.None;

        public uint PixelWidth { get; set; }

        public uint PixelHeight { get; set; }

        public HAlignment? HorizontalAlignment { get; set; }

        public string AltText { get; set; } = string.Empty;

        public IAdaptiveActionElement SelectAction { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("altText");
            knownProperties.Add("backgroundColor");
            knownProperties.Add("height");
            knownProperties.Add("horizontalAlignment");
            knownProperties.Add("selectAction");
            knownProperties.Add("size");
            knownProperties.Add("style");
            knownProperties.Add("url");
            knownProperties.Add("width");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (PixelWidth != 0 || PixelHeight != 0)
            {
                if (PixelWidth != 0)
                {
                    root["width"] = PixelWidth + "px";
                }

                if (PixelHeight != 0)
                {
                    root["height"] = PixelHeight + "px";
                }
            }
            else if (Size != ImageSize.None)
            {
                root["size"] = EnumStrings.ImageSize.ToString(Size);
            }

            if (Style != ImageStyle.Default)
            {
                root["style"] = EnumStrings.ImageStyle.ToString(Style);
            }

            if (!string.IsNullOrEmpty(Url))
            {
                root["url"] = Url;
            }

            if (!string.IsNullOrEmpty(BackgroundColor))
            {
                root["backgroundColor"] = BackgroundColor;
            }

            if (HorizontalAlignment.HasValue)
            {
                root["horizontalAlignment"] = EnumStrings.HAlignment.ToString(HorizontalAlignment.Value);
            }

            if (!string.IsNullOrEmpty(AltText))
            {
                root["altText"] = AltText;
            }

            SerializeSelectAction(root, SelectAction);
            return root;
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            resourceInformation.Add(new AdaptiveRemoteResourceInformation(Url, "image"));
        }

        internal static AdaptiveImage Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Image");
            return DeserializeWithoutCheckingType(context, json);
        }

        internal static AdaptiveImage DeserializeWithoutCheckingType(ParseContext context, JsonObject json)
        {
            var image = new AdaptiveImage();
            image.DeserializeBaseProperties(context, json);
            image.Url = ParseUtil.GetString(json, "url", true);
            image.BackgroundColor = ParseUtil.ValidateColor(ParseUtil.GetString(json, "backgroundColor"), context);
            image.Style = ParseUtil.GetEnumValue(json, "style", ImageStyle.Default, EnumStrings.ImageStyle);
            image.AltText = ParseUtil.GetString(json, "altText");
            image.HorizontalAlignment = ParseUtil.GetOptionalEnumValue(json, "horizontalAlignment", EnumStrings.HAlignment);

            var width = ParseUtil.ParseSizeForPixelSize(ParseUtil.GetString(json, "width"), context);
            var height = ParseUtil.ParseSizeForPixelSize(ParseUtil.GetString(json, "height"), context);
            if (width.HasValue || height.HasValue)
            {
                image.PixelWidth = (uint)(width ?? 0);
                image.PixelHeight = (uint)(height ?? 0);
            }
            else
            {
                image.Size = ParseUtil.GetEnumValue(json, "size", ImageSize.None, EnumStrings.ImageSize);
            }

            image.SelectAction = ParseUtil.GetAction(context, json, "selectAction", false);
            return image;
        }
    }

    public sealed class AdaptiveImageParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveImage.Deserialize(context, json);
    }
}
