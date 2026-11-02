// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Text.Json.Nodes;
using Windows.UI;
using static AdaptiveCards.Rendering.Uno.HostConfigParsing;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveHighlightColorConfig
    {
        public AdaptiveHighlightColorConfig()
        {
        }

        internal AdaptiveHighlightColorConfig(Color defaultColor, Color subtle)
        {
            Default = defaultColor;
            Subtle = subtle;
        }

        public Color Default { get; set; }

        public Color Subtle { get; set; }

        internal void Merge(JsonObject json)
        {
            Default = GetColorOrDefault(json, "default", Default);
            Subtle = GetColorOrDefault(json, "subtle", Subtle);
        }
    }

    public sealed class AdaptiveColorConfig
    {
        public AdaptiveColorConfig()
        {
        }

        internal AdaptiveColorConfig(uint defaultColor, uint subtle, bool withHighlights)
        {
            Default = C(defaultColor);
            Subtle = C(subtle);
            if (withHighlights)
            {
                HighlightColors = new AdaptiveHighlightColorConfig(C(0xFFFFFF00), C(0xFFFFFFE0));
            }
        }

        public Color Default { get; set; }

        public Color Subtle { get; set; }

        public AdaptiveHighlightColorConfig HighlightColors { get; set; } = new AdaptiveHighlightColorConfig();

        internal void Merge(JsonObject json)
        {
            Default = GetColorOrDefault(json, "default", Default);
            Subtle = GetColorOrDefault(json, "subtle", Subtle);
            if (GetObject(json, "highlightColors") is { } highlight)
            {
                HighlightColors.Merge(highlight);
            }
        }
    }

    public sealed class AdaptiveColorsConfig
    {
        public AdaptiveColorsConfig() : this(false)
        {
        }

        internal AdaptiveColorsConfig(bool withHighlights)
        {
            Default = new AdaptiveColorConfig(0xFF000000, 0xB2000000, withHighlights);
            Accent = new AdaptiveColorConfig(0xFF0000FF, 0xB20000FF, withHighlights);
            Dark = new AdaptiveColorConfig(0xFF101010, 0xB2101010, withHighlights);
            Light = new AdaptiveColorConfig(0xFFFFFFFF, 0xB2FFFFFF, withHighlights);
            Good = new AdaptiveColorConfig(0xFF008000, 0xB2008000, withHighlights);
            Warning = new AdaptiveColorConfig(0xFFFFD700, 0xB2FFD700, withHighlights);
            Attention = new AdaptiveColorConfig(0xFF8B0000, 0xB28B0000, withHighlights);
        }

        public AdaptiveColorConfig Default { get; set; }

        public AdaptiveColorConfig Accent { get; set; }

        public AdaptiveColorConfig Dark { get; set; }

        public AdaptiveColorConfig Light { get; set; }

        public AdaptiveColorConfig Good { get; set; }

        public AdaptiveColorConfig Warning { get; set; }

        public AdaptiveColorConfig Attention { get; set; }

        internal void Merge(JsonObject json)
        {
            MergeColor(json, "default", Default);
            MergeColor(json, "accent", Accent);
            MergeColor(json, "dark", Dark);
            MergeColor(json, "light", Light);
            MergeColor(json, "good", Good);
            MergeColor(json, "warning", Warning);
            MergeColor(json, "attention", Attention);
        }

        private static void MergeColor(JsonObject json, string key, AdaptiveColorConfig target)
        {
            if (GetObject(json, key) is { } obj)
            {
                target.Merge(obj);
            }
        }
    }

    public sealed class AdaptiveContainerStyleDefinition
    {
        public AdaptiveContainerStyleDefinition()
        {
            BackgroundColor = C(0xFFFFFFFF);
            BorderColor = C(0xFF7F7F7F);
            ForegroundColors = new AdaptiveColorsConfig();
        }

        internal AdaptiveContainerStyleDefinition(Color background, Color border, bool warningVariant)
        {
            BackgroundColor = background;
            BorderColor = border;
            ForegroundColors = new AdaptiveColorsConfig(true);
            if (warningVariant)
            {
                // The non-default palettes in the shared host config use a different warning color.
                ForegroundColors.Warning = new AdaptiveColorConfig(0xFFA60000, 0xB2FFA600, true);
            }
        }

        public Color BackgroundColor { get; set; }

        public Color BorderColor { get; set; }

        public AdaptiveColorsConfig ForegroundColors { get; set; }

        /// <summary>Experimental (iOS only in the shared model); parsed for completeness, not part of the WinUI3 projection.</summary>
        internal int BorderThickness { get; set; }

        internal void Merge(JsonObject json)
        {
            BackgroundColor = GetColorOrDefault(json, "backgroundColor", BackgroundColor);
            BorderColor = GetColorOrDefault(json, "borderColor", BorderColor);
            BorderThickness = AdaptiveCards.ObjectModel.Uno.ParseUtil.GetInt(json, "borderThickness", BorderThickness);
            if (GetObject(json, "foregroundColors") is { } fg)
            {
                ForegroundColors.Merge(fg);
            }
        }
    }

    public sealed class AdaptiveContainerStylesDefinition
    {
        public AdaptiveContainerStylesDefinition()
        {
            Default = new AdaptiveContainerStyleDefinition(C(0xFFFFFFFF), C(0xFF7F7F7F), false);
            Emphasis = new AdaptiveContainerStyleDefinition(C(0x08000000), C(0x08000000), false);
            Good = new AdaptiveContainerStyleDefinition(C(0xFFD5F0DD), C(0xFF7F7F7F), true);
            Attention = new AdaptiveContainerStyleDefinition(C(0xFFF7E9E9), C(0xFF7F7F7F), true);
            Warning = new AdaptiveContainerStyleDefinition(C(0xFFF7F7DF), C(0xFF7F7F7F), true);
            Accent = new AdaptiveContainerStyleDefinition(C(0xFFDCE5F7), C(0xFF7F7F7F), true);
        }

        public AdaptiveContainerStyleDefinition Default { get; set; }

        public AdaptiveContainerStyleDefinition Emphasis { get; set; }

        public AdaptiveContainerStyleDefinition Good { get; set; }

        public AdaptiveContainerStyleDefinition Attention { get; set; }

        public AdaptiveContainerStyleDefinition Warning { get; set; }

        public AdaptiveContainerStyleDefinition Accent { get; set; }

        internal void Merge(JsonObject json)
        {
            MergeStyle(json, "default", Default);
            MergeStyle(json, "emphasis", Emphasis);
            MergeStyle(json, "good", Good);
            MergeStyle(json, "attention", Attention);
            MergeStyle(json, "warning", Warning);
            MergeStyle(json, "accent", Accent);
        }

        private static void MergeStyle(JsonObject json, string key, AdaptiveContainerStyleDefinition target)
        {
            if (GetObject(json, key) is { } obj)
            {
                target.Merge(obj);
            }
        }
    }
}
