// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveBackgroundImage
    {
        public AdaptiveBackgroundImage()
        {
        }

        public AdaptiveBackgroundImage(string url)
        {
            Url = url;
        }

        public string Url { get; set; } = string.Empty;

        public BackgroundImageFillMode FillMode { get; set; } = BackgroundImageFillMode.Cover;

        public HAlignment HorizontalAlignment { get; set; } = HAlignment.Left;

        public VAlignment VerticalAlignment { get; set; } = VAlignment.Top;

        internal bool ShouldSerialize() => !string.IsNullOrEmpty(Url);

        internal JsonNode ToJson()
        {
            if (!string.IsNullOrEmpty(Url) && FillMode == BackgroundImageFillMode.Cover &&
                HorizontalAlignment == HAlignment.Left && VerticalAlignment == VAlignment.Top)
            {
                return JsonValue.Create(Url);
            }

            var root = new JsonObject();
            if (!string.IsNullOrEmpty(Url))
            {
                root["url"] = Url;
            }

            if (FillMode != BackgroundImageFillMode.Cover)
            {
                root["fillMode"] = EnumStrings.ImageFillMode.ToString(FillMode);
            }

            if (HorizontalAlignment != HAlignment.Left)
            {
                root["horizontalAlignment"] = EnumStrings.HAlignment.ToString(HorizontalAlignment);
            }

            if (VerticalAlignment != VAlignment.Top)
            {
                root["verticalAlignment"] = EnumStrings.VAlignment.ToString(VerticalAlignment);
            }

            return root;
        }

        internal static AdaptiveBackgroundImage Deserialize(JsonNode json)
        {
            if (ParseUtil.IsEmpty(json))
            {
                return null;
            }

            if (ParseUtil.IsString(json))
            {
                var url = json.GetValue<string>();
                return string.IsNullOrEmpty(url) ? null : new AdaptiveBackgroundImage(url);
            }

            var obj = ParseUtil.ThrowIfNotJsonObject(json);
            return new AdaptiveBackgroundImage
            {
                Url = ParseUtil.GetString(obj, "url", true),
                FillMode = ParseUtil.GetEnumValue(obj, "fillMode", BackgroundImageFillMode.Cover, EnumStrings.ImageFillMode),
                HorizontalAlignment = ParseUtil.GetEnumValue(obj, "horizontalAlignment", HAlignment.Left, EnumStrings.HAlignment),
                VerticalAlignment = ParseUtil.GetEnumValue(obj, "verticalAlignment", VAlignment.Top, EnumStrings.VAlignment),
            };
        }
    }
}
