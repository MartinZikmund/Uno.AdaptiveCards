// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace AdaptiveCards.ObjectModel.Uno
{
    public enum TextSize { Small = 0, Default, Medium, Large, ExtraLarge }

    public enum TextWeight { Lighter = 0, Default, Bolder }

    public enum ForegroundColor { Default = 0, Dark, Light, Accent, Good, Warning, Attention }

    public enum HAlignment { Left = 0, Center, Right }

    public enum VAlignment { Top = 0, Center, Bottom }

    public enum BackgroundImageFillMode { Cover = 0, RepeatHorizontally, RepeatVertically, Repeat }

    public enum ElementType
    {
        Unsupported = 0,
        AdaptiveCard,
        TextBlock,
        Image,
        Carousel,
        CarouselPage,
        Container,
        Column,
        ColumnSet,
        FactSet,
        Fact,
        ImageSet,
        ChoiceInput,
        ChoiceSetInput,
        DateInput,
        NumberInput,
        TextInput,
        TimeInput,
        ToggleInput,
        Custom,
        Media,
        ActionSet,
        RichTextBlock,
        TableCell,
        TableRow,
        Table,
    }

    public enum ActionType { Unsupported = 0, ShowCard, Submit, OpenUrl, ToggleVisibility, Execute, Custom }

    public enum ImageStyle { Default = 0, Person }

    public enum ImageSize { None = 0, Auto, Stretch, Small, Medium, Large }

    public enum SeparatorThickness { Default = 0, Thick }

    public enum Spacing { Default = 0, None, Small, Medium, Large, ExtraLarge, Padding }

    public enum ChoiceSetStyle { Compact = 0, Expanded, Filtered }

    public enum TextInputStyle { Text = 0, Tel, Url, Email, Password }

    public enum ContainerStyle { None = 0, Default, Emphasis, Good, Attention, Warning, Accent }

    public enum FontType { Default = 0, Monospace }

    public enum ErrorStatusCode
    {
        InvalidJson = 0,
        RenderFailed,
        RequiredPropertyMissing,
        InvalidPropertyValue,
        UnsupportedParserOverride,
        CustomError,
        // Values below are not part of the WinUI3 projection but are reported by the shared parser.
        IdCollision,
        ProhibitedType,
    }

    public enum WarningStatusCode
    {
        UnknownElementType = 0,
        UnknownActionElementType,
        UnknownPropertyOnElement,
        UnknownEnumValue,
        NoRendererForType,
        InteractivityNotSupported,
        MaxActionsExceeded,
        AssetLoadFailed,
        UnsupportedSchemaVersion,
        UnsupportedMediaType,
        InvalidMediaMix,
        InvalidValue,
        UnsupportedValue,
        PerformingFallback,
        CustomWarning,
        EmptyLabelInRequiredInput,
        MissingValidationErrorMessage,
        RequiredPropertyMissing,
        // Values below are not part of the WinUI3 projection but are reported by the shared parser.
        InvalidColorFormat,
        InvalidDimensionSpecified,
        InvalidLanguage,
        ProhibitedTypeDetected,
    }

    public enum VerticalContentAlignment { Top = 0, Center, Bottom }

    public enum HeightType { Auto = 0, Stretch }

    public enum IsVisible { IsVisibleToggle = 0, IsVisibleTrue, IsVisibleFalse }

    public enum FallbackType { None = 0, Drop, Content }

    [Flags]
    public enum BleedDirection
    {
        None = 0x0000,
        Left = 0x0001,
        Right = 0x0010,
        Up = 0x0100,
        Down = 0x1000,
        All = Left | Right | Up | Down,
    }

    public enum AssociatedInputs { Auto = 0, None }

    public enum TextStyle { Default = 0, Heading, ColumnHeader }

    public enum ActionMode { Primary = 0, Secondary }

    public enum ActionRole { Button = 0, Link, Tab, Menu, MenuItem }

    public enum CarouselOrientation { Horizontal = 0, Vertical }

    /// <summary>ImageSet presentation style (Adaptive Cards 1.6; not part of the WinUI3 projection).</summary>
    public enum ImageSetStyle { Default = 0, Stacked, Grid }
}
