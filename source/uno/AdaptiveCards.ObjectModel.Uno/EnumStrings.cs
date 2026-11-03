// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>
    /// Port of the shared model's DEFINE_ADAPTIVECARD_ENUM string tables (Enums.cpp). Parsing is case-insensitive;
    /// unknown values yield <c>null</c> (equivalent to std::out_of_range in the shared model).
    /// </summary>
    internal sealed class EnumMap<T> where T : struct, Enum
    {
        private readonly Dictionary<T, string> _toString = new();
        private readonly Dictionary<string, T> _fromString = new(StringComparer.OrdinalIgnoreCase);

        public EnumMap((T Value, string Name)[] values, (string Name, T Value)[] aliases = null)
        {
            foreach (var (value, name) in values)
            {
                _toString[value] = name;
                _fromString[name] = value;
            }

            if (aliases != null)
            {
                foreach (var (name, value) in aliases)
                {
                    _fromString[name] = value;
                }
            }
        }

        public T? FromString(string value) => value != null && _fromString.TryGetValue(value, out var result) ? result : null;

        public string ToString(T value) => _toString.TryGetValue(value, out var result) ? result : value.ToString();
    }

    internal static class EnumStrings
    {
        public static readonly EnumMap<HeightType> HeightType = new(new[]
        {
            (Uno.HeightType.Auto, "Auto"),
            (Uno.HeightType.Stretch, "Stretch"),
        });

        public static readonly EnumMap<Spacing> Spacing = new(new[]
        {
            (Uno.Spacing.Default, "default"),
            (Uno.Spacing.None, "none"),
            (Uno.Spacing.Small, "small"),
            (Uno.Spacing.Medium, "medium"),
            (Uno.Spacing.Large, "large"),
            (Uno.Spacing.ExtraLarge, "extraLarge"),
            (Uno.Spacing.Padding, "padding"),
        });

        public static readonly EnumMap<SeparatorThickness> SeparatorThickness = new(new[]
        {
            (Uno.SeparatorThickness.Default, "default"),
            (Uno.SeparatorThickness.Thick, "thick"),
        });

        public static readonly EnumMap<ImageStyle> ImageStyle = new(
            new[] { (Uno.ImageStyle.Default, "default"), (Uno.ImageStyle.Person, "person") },
            new[] { ("normal", Uno.ImageStyle.Default) });

        public static readonly EnumMap<VAlignment> VAlignment = new(new[]
        {
            (Uno.VAlignment.Top, "top"),
            (Uno.VAlignment.Center, "center"),
            (Uno.VAlignment.Bottom, "bottom"),
        });

        public static readonly EnumMap<BackgroundImageFillMode> ImageFillMode = new(new[]
        {
            (BackgroundImageFillMode.Cover, "cover"),
            (BackgroundImageFillMode.RepeatHorizontally, "repeatHorizontally"),
            (BackgroundImageFillMode.RepeatVertically, "repeatVertically"),
            (BackgroundImageFillMode.Repeat, "repeat"),
        });

        public static readonly EnumMap<ImageSize> ImageSize = new(new[]
        {
            (Uno.ImageSize.Auto, "Auto"),
            (Uno.ImageSize.Large, "Large"),
            (Uno.ImageSize.Medium, "Medium"),
            (Uno.ImageSize.Small, "Small"),
            (Uno.ImageSize.Stretch, "Stretch"),
        });

        public static readonly EnumMap<HAlignment> HAlignment = new(new[]
        {
            (Uno.HAlignment.Center, "center"),
            (Uno.HAlignment.Left, "left"),
            (Uno.HAlignment.Right, "right"),
        });

        public static readonly EnumMap<ForegroundColor> ForegroundColor = new(new[]
        {
            (Uno.ForegroundColor.Accent, "Accent"),
            (Uno.ForegroundColor.Attention, "Attention"),
            (Uno.ForegroundColor.Dark, "Dark"),
            (Uno.ForegroundColor.Default, "Default"),
            (Uno.ForegroundColor.Good, "Good"),
            (Uno.ForegroundColor.Light, "Light"),
            (Uno.ForegroundColor.Warning, "Warning"),
        });

        public static readonly EnumMap<TextStyle> TextStyle = new(new[]
        {
            (Uno.TextStyle.Default, "default"),
            (Uno.TextStyle.Heading, "heading"),
        });

        public static readonly EnumMap<TextWeight> TextWeight = new(
            new[] { (Uno.TextWeight.Bolder, "Bolder"), (Uno.TextWeight.Lighter, "Lighter"), (Uno.TextWeight.Default, "Default") },
            new[] { ("Normal", Uno.TextWeight.Default) });

        public static readonly EnumMap<TextSize> TextSize = new(
            new[]
            {
                (Uno.TextSize.ExtraLarge, "ExtraLarge"),
                (Uno.TextSize.Large, "Large"),
                (Uno.TextSize.Medium, "Medium"),
                (Uno.TextSize.Default, "Default"),
                (Uno.TextSize.Small, "Small"),
            },
            new[] { ("Normal", Uno.TextSize.Default) });

        public static readonly EnumMap<FontType> FontType = new(new[]
        {
            (Uno.FontType.Default, "Default"),
            (Uno.FontType.Monospace, "Monospace"),
        });

        public static readonly EnumMap<ActionRole> ActionRole = new(new[]
        {
            (Uno.ActionRole.Button, "Button"),
            (Uno.ActionRole.Link, "Link"),
            (Uno.ActionRole.Tab, "Tab"),
            (Uno.ActionRole.Menu, "Menu"),
            (Uno.ActionRole.MenuItem, "MenuItem"),
        });

        public static readonly EnumMap<AssociatedInputs> AssociatedInputs = new(new[]
        {
            (Uno.AssociatedInputs.Auto, "Auto"),
            (Uno.AssociatedInputs.None, "None"),
        });

        public static readonly EnumMap<ChoiceSetStyle> ChoiceSetStyle = new(new[]
        {
            (Uno.ChoiceSetStyle.Compact, "Compact"),
            (Uno.ChoiceSetStyle.Expanded, "Expanded"),
            (Uno.ChoiceSetStyle.Filtered, "Filtered"),
        });

        public static readonly EnumMap<TextInputStyle> TextInputStyle = new(new[]
        {
            (Uno.TextInputStyle.Email, "Email"),
            (Uno.TextInputStyle.Tel, "Tel"),
            (Uno.TextInputStyle.Text, "Text"),
            (Uno.TextInputStyle.Url, "Url"),
            (Uno.TextInputStyle.Password, "Password"),
        });

        public static readonly EnumMap<ContainerStyle> ContainerStyle = new(new[]
        {
            (Uno.ContainerStyle.Default, "Default"),
            (Uno.ContainerStyle.Emphasis, "Emphasis"),
            (Uno.ContainerStyle.Good, "Good"),
            (Uno.ContainerStyle.Attention, "Attention"),
            (Uno.ContainerStyle.Warning, "Warning"),
            (Uno.ContainerStyle.Accent, "Accent"),
        });

        public static readonly EnumMap<VerticalContentAlignment> VerticalContentAlignment = new(new[]
        {
            (Uno.VerticalContentAlignment.Top, "Top"),
            (Uno.VerticalContentAlignment.Center, "Center"),
            (Uno.VerticalContentAlignment.Bottom, "Bottom"),
        });

        public static readonly EnumMap<CarouselOrientation> CarouselOrientation = new(new[]
        {
            (Uno.CarouselOrientation.Horizontal, "horizontal"),
            (Uno.CarouselOrientation.Vertical, "vertical"),
        });

        public static readonly EnumMap<ImageSetStyle> ImageSetStyle = new(new[]
        {
            (Uno.ImageSetStyle.Default, "default"),
            (Uno.ImageSetStyle.Stacked, "stacked"),
            (Uno.ImageSetStyle.Grid, "grid"),
        });

        public static readonly EnumMap<InputLabelPosition> InputLabelPosition = new(new[]
        {
            (Uno.InputLabelPosition.Above, "above"),
            (Uno.InputLabelPosition.Inline, "inline"),
        });

        public static readonly EnumMap<InputStyle> InputStyle = new(new[]
        {
            (Uno.InputStyle.Default, "default"),
            (Uno.InputStyle.RevealOnHover, "revealOnHover"),
        });

        public static readonly EnumMap<ActionMode> Mode = new(new[]
        {
            (ActionMode.Primary, "primary"),
            (ActionMode.Secondary, "secondary"),
        });

        public static readonly EnumMap<ElementType> ElementType = new(new[]
        {
            (Uno.ElementType.ActionSet, "ActionSet"),
            (Uno.ElementType.AdaptiveCard, "AdaptiveCard"),
            (Uno.ElementType.Carousel, "Carousel"),
            (Uno.ElementType.CarouselPage, "CarouselPage"),
            (Uno.ElementType.ChoiceSetInput, "Input.ChoiceSet"),
            (Uno.ElementType.Column, "Column"),
            (Uno.ElementType.ColumnSet, "ColumnSet"),
            (Uno.ElementType.Container, "Container"),
            (Uno.ElementType.Custom, "Custom"),
            (Uno.ElementType.DateInput, "Input.Date"),
            (Uno.ElementType.Fact, "Fact"),
            (Uno.ElementType.FactSet, "FactSet"),
            (Uno.ElementType.Image, "Image"),
            (Uno.ElementType.ImageSet, "ImageSet"),
            (Uno.ElementType.Media, "Media"),
            (Uno.ElementType.NumberInput, "Input.Number"),
            (Uno.ElementType.RichTextBlock, "RichTextBlock"),
            (Uno.ElementType.Table, "Table"),
            (Uno.ElementType.TableCell, "TableCell"),
            (Uno.ElementType.TableRow, "TableRow"),
            (Uno.ElementType.TextBlock, "TextBlock"),
            (Uno.ElementType.TextInput, "Input.Text"),
            (Uno.ElementType.TimeInput, "Input.Time"),
            (Uno.ElementType.ToggleInput, "Input.Toggle"),
            (Uno.ElementType.ChoiceInput, "Input.Choice"),
            (Uno.ElementType.Unsupported, "Unknown"),
        });

        public static readonly EnumMap<ActionType> ActionType = new(new[]
        {
            (Uno.ActionType.Unsupported, "Unsupported"),
            (Uno.ActionType.Execute, "Action.Execute"),
            (Uno.ActionType.OpenUrl, "Action.OpenUrl"),
            (Uno.ActionType.ShowCard, "Action.ShowCard"),
            (Uno.ActionType.Submit, "Action.Submit"),
            (Uno.ActionType.ToggleVisibility, "Action.ToggleVisibility"),
            (Uno.ActionType.Custom, "Custom"),
        });
    }
}
