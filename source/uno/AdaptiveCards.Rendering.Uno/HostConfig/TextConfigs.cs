// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using static AdaptiveCards.Rendering.Uno.HostConfigParsing;

namespace AdaptiveCards.Rendering.Uno
{
    public interface IAdaptiveTextStyleConfig
    {
        TextWeight Weight { get; set; }
        TextSize Size { get; set; }
        ForegroundColor Color { get; set; }
        bool IsSubtle { get; set; }
        FontType FontType { get; set; }
    }

    internal static class TextStyleConfigHelper
    {
        public static void Merge(IAdaptiveTextStyleConfig target, JsonObject json)
        {
            target.Color = ParseUtil.GetEnumValue(json, "color", target.Color, EnumStrings.ForegroundColor);
            target.FontType = ParseUtil.GetEnumValue(json, "fontType", target.FontType, EnumStrings.FontType);
            target.IsSubtle = ParseUtil.GetBool(json, "isSubtle", target.IsSubtle);
            target.Size = ParseUtil.GetEnumValue(json, "size", target.Size, EnumStrings.TextSize);
            target.Weight = ParseUtil.GetEnumValue(json, "weight", target.Weight, EnumStrings.TextWeight);
        }
    }

    public sealed class AdaptiveTextStyleConfig : IAdaptiveTextStyleConfig
    {
        public AdaptiveTextStyleConfig()
        {
        }

        internal AdaptiveTextStyleConfig(TextWeight weight, TextSize size)
        {
            Weight = weight;
            Size = size;
        }

        public TextWeight Weight { get; set; } = TextWeight.Default;

        public TextSize Size { get; set; } = TextSize.Default;

        public ForegroundColor Color { get; set; } = ForegroundColor.Default;

        public bool IsSubtle { get; set; }

        public FontType FontType { get; set; } = FontType.Default;

        internal void Merge(JsonObject json) => TextStyleConfigHelper.Merge(this, json);
    }

    public sealed class AdaptiveTextStylesConfig
    {
        public AdaptiveTextStylesConfig()
        {
        }

        public AdaptiveTextStyleConfig Heading { get; set; } = new AdaptiveTextStyleConfig(TextWeight.Bolder, TextSize.Large);

        public AdaptiveTextStyleConfig ColumnHeader { get; set; } = new AdaptiveTextStyleConfig(TextWeight.Bolder, TextSize.Default);

        internal void Merge(JsonObject json)
        {
            if (GetObject(json, "heading") is { } heading)
            {
                Heading.Merge(heading);
            }

            if (GetObject(json, "columnHeader") is { } columnHeader)
            {
                ColumnHeader.Merge(columnHeader);
            }
        }
    }

    public sealed class AdaptiveFactSetTextConfig : IAdaptiveTextStyleConfig
    {
        public AdaptiveFactSetTextConfig()
        {
        }

        internal AdaptiveFactSetTextConfig(TextWeight weight, uint maxWidth)
        {
            Weight = weight;
            MaxWidth = maxWidth;
        }

        public TextWeight Weight { get; set; } = TextWeight.Default;

        public TextSize Size { get; set; } = TextSize.Default;

        public ForegroundColor Color { get; set; } = ForegroundColor.Default;

        public bool IsSubtle { get; set; }

        public FontType FontType { get; set; } = FontType.Default;

        public bool Wrap { get; set; } = true;

        public uint MaxWidth { get; set; } = uint.MaxValue;

        internal void Merge(JsonObject json)
        {
            TextStyleConfigHelper.Merge(this, json);
            Wrap = ParseUtil.GetBool(json, "wrap", Wrap);
            MaxWidth = ParseUtil.GetUInt(json, "maxWidth", MaxWidth);
        }
    }

    public sealed class AdaptiveFactSetConfig
    {
        public AdaptiveFactSetConfig()
        {
        }

        public AdaptiveFactSetTextConfig Title { get; set; } = new AdaptiveFactSetTextConfig(TextWeight.Bolder, 150);

        public AdaptiveFactSetTextConfig Value { get; set; } = new AdaptiveFactSetTextConfig(TextWeight.Default, uint.MaxValue);

        public uint Spacing { get; set; } = 10;

        internal void Merge(JsonObject json)
        {
            Spacing = ParseUtil.GetUInt(json, "spacing", Spacing);
            if (GetObject(json, "title") is { } title)
            {
                Title.Merge(title);
            }

            var valueMaxWidth = Value.MaxWidth;
            if (GetObject(json, "value") is { } value)
            {
                Value.Merge(value);
            }

            // The shared model ignores maxWidth for fact values.
            Value.MaxWidth = valueMaxWidth;
        }
    }

    public sealed class AdaptiveTextBlockConfig
    {
        public AdaptiveTextBlockConfig()
        {
        }

        public uint HeadingLevel { get; set; } = 2;

        internal void Merge(JsonObject json) => HeadingLevel = unchecked((uint)ParseUtil.GetInt(json, "headingLevel", (int)HeadingLevel));
    }
}
