// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using static AdaptiveCards.Rendering.Uno.HostConfigParsing;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Font sizes. Unset values are <see cref="uint.MaxValue"/> (as in the WinUI3 renderer) so the renderer can fall back
    /// to the default font type, the deprecated top-level font sizes and finally the built-in defaults.
    /// </summary>
    public sealed class AdaptiveFontSizesConfig
    {
        public const uint Unset = uint.MaxValue;

        public AdaptiveFontSizesConfig()
        {
        }

        public uint Small { get; set; } = Unset;

        public uint Default { get; set; } = Unset;

        public uint Medium { get; set; } = Unset;

        public uint Large { get; set; } = Unset;

        public uint ExtraLarge { get; set; } = Unset;

        internal uint Get(TextSize size) => size switch
        {
            TextSize.Small => Small,
            TextSize.Medium => Medium,
            TextSize.Large => Large,
            TextSize.ExtraLarge => ExtraLarge,
            _ => Default,
        };

        internal void Merge(JsonObject json)
        {
            Small = ParseUtil.GetUInt(json, "small", Small);
            Default = ParseUtil.GetUInt(json, "default", Default);
            Medium = ParseUtil.GetUInt(json, "medium", Medium);
            Large = ParseUtil.GetUInt(json, "large", Large);
            ExtraLarge = ParseUtil.GetUInt(json, "extraLarge", ExtraLarge);
        }
    }

    /// <summary>Font weights. Unset values are <see cref="ushort.MaxValue"/> (as in the WinUI3 renderer).</summary>
    public sealed class AdaptiveFontWeightsConfig
    {
        public const ushort Unset = ushort.MaxValue;

        public AdaptiveFontWeightsConfig()
        {
        }

        public ushort Lighter { get; set; } = Unset;

        public ushort Default { get; set; } = Unset;

        public ushort Bolder { get; set; } = Unset;

        internal ushort Get(TextWeight weight) => weight switch
        {
            TextWeight.Lighter => Lighter,
            TextWeight.Bolder => Bolder,
            _ => Default,
        };

        internal void Merge(JsonObject json)
        {
            Lighter = ToUShort(ParseUtil.GetUInt(json, "lighter", Lighter));
            Default = ToUShort(ParseUtil.GetUInt(json, "default", Default));
            Bolder = ToUShort(ParseUtil.GetUInt(json, "bolder", Bolder));
        }

        // The WinUI3 projection narrows the shared model's unsigned int to UInt16.
        private static ushort ToUShort(uint value) => unchecked((ushort)value);
    }

    public sealed class AdaptiveFontTypeDefinition
    {
        public AdaptiveFontTypeDefinition()
        {
        }

        public string FontFamily { get; set; } = string.Empty;

        public AdaptiveFontWeightsConfig FontWeights { get; set; } = new AdaptiveFontWeightsConfig();

        public AdaptiveFontSizesConfig FontSizes { get; set; } = new AdaptiveFontSizesConfig();

        internal void Merge(JsonObject json)
        {
            FontFamily = GetStringOrDefault(json, "fontFamily", FontFamily);
            if (GetObject(json, "fontSizes") is { } sizes)
            {
                FontSizes.Merge(sizes);
            }

            if (GetObject(json, "fontWeights") is { } weights)
            {
                FontWeights.Merge(weights);
            }
        }
    }

    public sealed class AdaptiveFontTypesDefinition
    {
        public AdaptiveFontTypesDefinition()
        {
        }

        public AdaptiveFontTypeDefinition Default { get; set; } = new AdaptiveFontTypeDefinition();

        public AdaptiveFontTypeDefinition Monospace { get; set; } = new AdaptiveFontTypeDefinition();

        internal void Merge(JsonObject json)
        {
            if (GetObject(json, "default") is { } d)
            {
                Default.Merge(d);
            }

            if (GetObject(json, "monospace") is { } m)
            {
                Monospace.Merge(m);
            }
        }
    }
}
