// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Windows.UI;
using static AdaptiveCards.Rendering.Uno.HostConfigParsing;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveSpacingConfig
    {
        public AdaptiveSpacingConfig()
        {
        }

        public uint Small { get; set; } = 3;

        public uint Default { get; set; } = 8;

        public uint Medium { get; set; } = 20;

        public uint Large { get; set; } = 30;

        public uint ExtraLarge { get; set; } = 40;

        public uint Padding { get; set; } = 20;

        internal void Merge(JsonObject json)
        {
            Small = ParseUtil.GetUInt(json, "small", Small);
            Default = ParseUtil.GetUInt(json, "default", Default);
            Medium = ParseUtil.GetUInt(json, "medium", Medium);
            Large = ParseUtil.GetUInt(json, "large", Large);
            ExtraLarge = ParseUtil.GetUInt(json, "extraLarge", ExtraLarge);
            Padding = ParseUtil.GetUInt(json, "padding", Padding);
        }
    }

    public sealed class AdaptiveSeparatorConfig
    {
        public AdaptiveSeparatorConfig()
        {
        }

        public uint LineThickness { get; set; } = 1;

        public Color LineColor { get; set; } = C(0xB2000000);

        internal void Merge(JsonObject json)
        {
            LineThickness = ParseUtil.GetUInt(json, "lineThickness", LineThickness);
            LineColor = GetColorOrDefault(json, "lineColor", LineColor);
        }
    }

    public sealed class AdaptiveImageSizesConfig
    {
        public AdaptiveImageSizesConfig()
        {
        }

        public uint Small { get; set; } = 80;

        public uint Medium { get; set; } = 120;

        public uint Large { get; set; } = 180;

        internal void Merge(JsonObject json)
        {
            Small = ParseUtil.GetUInt(json, "small", Small);
            Medium = ParseUtil.GetUInt(json, "medium", Medium);
            Large = ParseUtil.GetUInt(json, "large", Large);
        }
    }

    public sealed class AdaptiveImageSetConfig
    {
        public AdaptiveImageSetConfig()
        {
        }

        public ImageSize ImageSize { get; set; } = ImageSize.Auto;

        public uint MaxImageHeight { get; set; } = 100;

        internal void Merge(JsonObject json)
        {
            ImageSize = ParseUtil.GetEnumValue(json, "imageSize", ImageSize, EnumStrings.ImageSize);
            MaxImageHeight = ParseUtil.GetUInt(json, "maxImageHeight", MaxImageHeight);
        }
    }

    public sealed class AdaptiveImageConfig
    {
        public AdaptiveImageConfig()
        {
        }

        public ImageSize ImageSize { get; set; } = ImageSize.Auto;

        internal void Merge(JsonObject json) => ImageSize = ParseUtil.GetEnumValue(json, "size", ImageSize, EnumStrings.ImageSize);
    }

    public sealed class AdaptiveCardConfig
    {
        public AdaptiveCardConfig()
        {
        }

        public bool AllowCustomStyle { get; set; } = true;

        internal void Merge(JsonObject json) => AllowCustomStyle = ParseUtil.GetBool(json, "allowCustomStyle", AllowCustomStyle);
    }

    public sealed class AdaptiveShowCardActionConfig
    {
        public AdaptiveShowCardActionConfig()
        {
        }

        public ActionMode ActionMode { get; set; } = ActionMode.Inline;

        public ContainerStyle Style { get; set; } = ContainerStyle.Emphasis;

        public uint InlineTopMargin { get; set; } = 16;

        internal void Merge(JsonObject json)
        {
            ActionMode = ParseUtil.GetEnumValue(json, "actionMode", ActionMode, ActionModeMap);
            InlineTopMargin = ParseUtil.GetUInt(json, "inlineTopMargin", InlineTopMargin);
            Style = ParseUtil.GetEnumValue(json, "style", Style, EnumStrings.ContainerStyle);
        }
    }

    public sealed class AdaptiveActionsConfig
    {
        public AdaptiveActionsConfig()
        {
        }

        public AdaptiveShowCardActionConfig ShowCard { get; set; } = new AdaptiveShowCardActionConfig();

        public ActionsOrientation ActionsOrientation { get; set; } = ActionsOrientation.Horizontal;

        public ActionAlignment ActionAlignment { get; set; } = ActionAlignment.Stretch;

        public uint ButtonSpacing { get; set; } = 10;

        public uint MaxActions { get; set; } = 5;

        public Spacing Spacing { get; set; } = Spacing.Default;

        public IconPlacement IconPlacement { get; set; } = IconPlacement.AboveTitle;

        public uint IconSize { get; set; } = 16;

        internal void Merge(JsonObject json)
        {
            ActionsOrientation = ParseUtil.GetEnumValue(json, "actionsOrientation", ActionsOrientation, ActionsOrientationMap);
            ActionAlignment = ParseUtil.GetEnumValue(json, "actionAlignment", ActionAlignment, ActionAlignmentMap);
            ButtonSpacing = ParseUtil.GetUInt(json, "buttonSpacing", ButtonSpacing);
            MaxActions = ParseUtil.GetUInt(json, "maxActions", MaxActions);
            if (GetObject(json, "showCard") is { } showCard)
            {
                ShowCard.Merge(showCard);
            }

            Spacing = ParseUtil.GetEnumValue(json, "spacing", Spacing, EnumStrings.Spacing);
            IconPlacement = ParseUtil.GetEnumValue(json, "iconPlacement", IconPlacement, IconPlacementMap);
            IconSize = ParseUtil.GetUInt(json, "iconSize", IconSize);
        }
    }

    public sealed class AdaptiveMediaConfig
    {
        public AdaptiveMediaConfig()
        {
        }

        public string DefaultPoster { get; set; } = string.Empty;

        public string PlayButton { get; set; } = string.Empty;

        public bool AllowInlinePlayback { get; set; } = true;

        internal void Merge(JsonObject json)
        {
            DefaultPoster = GetStringOrDefault(json, "defaultPoster", DefaultPoster);
            PlayButton = GetStringOrDefault(json, "playButton", PlayButton);
            AllowInlinePlayback = ParseUtil.GetBool(json, "allowInlinePlayback", AllowInlinePlayback);
        }
    }

    public sealed class AdaptiveInputLabelConfig
    {
        public AdaptiveInputLabelConfig()
        {
        }

        public ForegroundColor Color { get; set; } = ForegroundColor.Default;

        public bool IsSubtle { get; set; }

        public TextSize Size { get; set; } = TextSize.Default;

        public string Suffix { get; set; } = string.Empty;

        public TextWeight Weight { get; set; } = TextWeight.Default;

        internal void Merge(JsonObject json)
        {
            Color = ParseUtil.GetEnumValue(json, "color", Color, EnumStrings.ForegroundColor);
            IsSubtle = ParseUtil.GetBool(json, "isSubtle", IsSubtle);
            Size = ParseUtil.GetEnumValue(json, "size", Size, EnumStrings.TextSize);
            Suffix = GetStringOrDefault(json, "suffix", Suffix);
            Weight = ParseUtil.GetEnumValue(json, "weight", Weight, EnumStrings.TextWeight);
        }
    }

    public sealed class AdaptiveLabelConfig
    {
        public AdaptiveLabelConfig()
        {
        }

        public Spacing InputSpacing { get; set; } = Spacing.Default;

        public AdaptiveInputLabelConfig RequiredInputs { get; set; } = new AdaptiveInputLabelConfig();

        public AdaptiveInputLabelConfig OptionalInputs { get; set; } = new AdaptiveInputLabelConfig();

        internal void Merge(JsonObject json)
        {
            InputSpacing = ParseUtil.GetEnumValue(json, "inputSpacing", InputSpacing, EnumStrings.Spacing);
            if (GetObject(json, "requiredInputs") is { } required)
            {
                RequiredInputs.Merge(required);
            }

            if (GetObject(json, "optionalInputs") is { } optional)
            {
                OptionalInputs.Merge(optional);
            }
        }
    }

    public sealed class AdaptiveErrorMessageConfig
    {
        public AdaptiveErrorMessageConfig()
        {
        }

        public TextSize Size { get; set; } = TextSize.Default;

        public Spacing Spacing { get; set; } = Spacing.Default;

        public TextWeight Weight { get; set; } = TextWeight.Default;

        internal void Merge(JsonObject json)
        {
            Size = ParseUtil.GetEnumValue(json, "size", Size, EnumStrings.TextSize);
            Spacing = ParseUtil.GetEnumValue(json, "spacing", Spacing, EnumStrings.Spacing);
            Weight = ParseUtil.GetEnumValue(json, "weight", Weight, EnumStrings.TextWeight);
        }
    }

    public sealed class AdaptiveInputsConfig
    {
        public AdaptiveInputsConfig()
        {
        }

        public AdaptiveErrorMessageConfig ErrorMessage { get; set; } = new AdaptiveErrorMessageConfig();

        public AdaptiveLabelConfig Label { get; set; } = new AdaptiveLabelConfig();

        internal void Merge(JsonObject json)
        {
            if (GetObject(json, "errorMessage") is { } errorMessage)
            {
                ErrorMessage.Merge(errorMessage);
            }

            if (GetObject(json, "label") is { } label)
            {
                Label.Merge(label);
            }
        }
    }

    public sealed class AdaptiveTableConfig
    {
        public AdaptiveTableConfig()
        {
        }

        public uint CellSpacing { get; set; } = 8;

        internal void Merge(JsonObject json) => CellSpacing = ParseUtil.GetUInt(json, "cellSpacing", CellSpacing);
    }
}
