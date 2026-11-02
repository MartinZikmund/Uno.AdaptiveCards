// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Globalization;
using System.Xml;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Port of TextHelpers.h TextRunStyleParameters.</summary>
    internal readonly struct TextRunStyleParameters
    {
        public TextRunStyleParameters(bool isStrikethrough, bool isItalic, bool isUnderline, bool isInHyperlink)
        {
            IsStrikethrough = isStrikethrough;
            IsItalic = isItalic;
            IsUnderline = isUnderline;
            IsInHyperlink = isInHyperlink;
        }

        public bool IsStrikethrough { get; }

        public bool IsItalic { get; }

        public bool IsUnderline { get; }

        public bool IsInHyperlink { get; }
    }

    /// <summary>Port of TextHelpers.cpp/.h (markdown → XAML inlines, text styling).</summary>
    internal static class TextHelpers
    {
        public static void StyleXamlTextBlockProperties(
            AdaptiveTextBlock adaptiveTextBlock, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs, TextBlock xamlTextBlock)
        {
            SetWrapProperties(xamlTextBlock, adaptiveTextBlock.Wrap);

            uint maxLines = adaptiveTextBlock.MaxLines;
            if (maxLines != uint.MaxValue)
            {
                xamlTextBlock.MaxLines = (int)Math.Min(maxLines, int.MaxValue);
            }

            SetHorizontalAlignment(adaptiveTextBlock.HorizontalAlignment, renderContext, xamlTextBlock);
            StyleTextElement(adaptiveTextBlock, renderContext, renderArgs, new TextRunStyleParameters(), xamlTextBlock);
        }

        public static TextDecorations GetTextDecorations(TextRunStyleParameters styleProperties)
        {
            var textDecorations = TextDecorations.None;
            if (styleProperties.IsStrikethrough)
            {
                textDecorations |= TextDecorations.Strikethrough;
            }

            if (styleProperties.IsUnderline || styleProperties.IsInHyperlink)
            {
                textDecorations |= TextDecorations.Underline;
            }

            return textDecorations;
        }

        public static void SetStrikethroughAndUnderline(TextRunStyleParameters styleProperties, TextBlock textBlock) =>
            textBlock.TextDecorations = GetTextDecorations(styleProperties);

        public static void SetStrikethroughAndUnderline(TextRunStyleParameters styleProperties, TextElement textElement) =>
            textElement.TextDecorations = GetTextDecorations(styleProperties);

        public static void SetHorizontalAlignment(HAlignment? adaptiveHorizontalAlignment, AdaptiveRenderContext renderContext, TextBlock xamlTextBlock)
        {
            var (horizontal, text) = GetAlignment(adaptiveHorizontalAlignment, renderContext);
            xamlTextBlock.HorizontalAlignment = horizontal;
            xamlTextBlock.TextAlignment = text;
        }

        public static void SetHorizontalAlignment(HAlignment? adaptiveHorizontalAlignment, AdaptiveRenderContext renderContext, RichTextBlock xamlTextBlock)
        {
            var (horizontal, text) = GetAlignment(adaptiveHorizontalAlignment, renderContext);
            xamlTextBlock.HorizontalAlignment = horizontal;
            xamlTextBlock.TextAlignment = text;
        }

        private static (HorizontalAlignment, TextAlignment) GetAlignment(HAlignment? adaptiveHorizontalAlignment, AdaptiveRenderContext renderContext)
        {
            var alignment = adaptiveHorizontalAlignment ?? renderContext.HorizontalContentAlignment ?? HAlignment.Left;
            return alignment switch
            {
                HAlignment.Right => (HorizontalAlignment.Right, TextAlignment.Right),
                HAlignment.Center => (HorizontalAlignment.Center, TextAlignment.Center),
                _ => (HorizontalAlignment.Left, TextAlignment.Left),
            };
        }

        private readonly struct TextElementStyle
        {
            public TextElementStyle(Brush foreground, double fontSize, FontWeight fontWeight, FontFamily fontFamily)
            {
                Foreground = foreground;
                FontSize = fontSize;
                FontWeight = fontWeight;
                FontFamily = fontFamily;
            }

            public Brush Foreground { get; }

            public double FontSize { get; }

            public FontWeight FontWeight { get; }

            public FontFamily FontFamily { get; }
        }

        private static TextElementStyle ComputeTextElementStyle(
            IAdaptiveTextElement adaptiveTextElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs, TextRunStyleParameters styleProperties)
        {
            var hostConfig = renderContext.HostConfig;
            var adaptiveTextColor = adaptiveTextElement.Color ?? ForegroundColor.Default;
            var adaptiveTextSize = adaptiveTextElement.Size ?? TextSize.Default;
            var adaptiveTextWeight = adaptiveTextElement.Weight ?? TextWeight.Default;
            var fontType = adaptiveTextElement.FontType ?? FontType.Default;

            Brush foreground = null;
            if (adaptiveTextColor != ForegroundColor.Default || !styleProperties.IsInHyperlink)
            {
                bool isSubtle = adaptiveTextElement.IsSubtle ?? false;
                var containerStyle = renderArgs?.ContainerStyle ?? ContainerStyle.Default;
                var fontColor = Util.GetColorFromAdaptiveColor(hostConfig, adaptiveTextColor, containerStyle, isSubtle, false);
                foreground = new SolidColorBrush(fontColor);
            }

            uint fontSize = Util.GetFontSizeFromFontType(hostConfig, fontType, adaptiveTextSize);
            string fontFamilyName = Util.GetFontFamilyFromFontType(hostConfig, fontType);
            FontWeight xamlFontWeight = Util.GetFontWeightFromStyle(hostConfig, fontType, adaptiveTextWeight);
            return new TextElementStyle(foreground, fontSize, xamlFontWeight, Util.CreateFontFamily(fontFamilyName));
        }

        public static void StyleTextElement(
            IAdaptiveTextElement adaptiveTextElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            TextRunStyleParameters styleProperties,
            TextBlock xamlTextElement)
        {
            SetStrikethroughAndUnderline(styleProperties, xamlTextElement);
            if (styleProperties.IsItalic)
            {
                xamlTextElement.FontStyle = FontStyle.Italic;
            }

            var style = ComputeTextElementStyle(adaptiveTextElement, renderContext, renderArgs, styleProperties);
            if (style.Foreground != null)
            {
                xamlTextElement.Foreground = style.Foreground;
            }

            xamlTextElement.FontSize = style.FontSize;
            xamlTextElement.FontWeight = style.FontWeight;
            xamlTextElement.FontFamily = style.FontFamily;
        }

        public static void StyleTextElement(
            IAdaptiveTextElement adaptiveTextElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            TextRunStyleParameters styleProperties,
            TextElement xamlTextElement)
        {
            SetStrikethroughAndUnderline(styleProperties, xamlTextElement);
            if (styleProperties.IsItalic)
            {
                xamlTextElement.FontStyle = FontStyle.Italic;
            }

            var style = ComputeTextElementStyle(adaptiveTextElement, renderContext, renderArgs, styleProperties);
            if (style.Foreground != null)
            {
                xamlTextElement.Foreground = style.Foreground;
            }

            xamlTextElement.FontSize = style.FontSize;
            xamlTextElement.FontWeight = style.FontWeight;
            xamlTextElement.FontFamily = style.FontFamily;
        }

        public static void SetXamlInlinesWithTextStyleConfig(
            IAdaptiveTextElement textElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            IAdaptiveTextStyleConfig textStyleConfig,
            TextBlock textBlock) =>
            SetXamlInlinesWithTextStyleConfig(
                renderContext, renderArgs, textStyleConfig, textElement, textElement.Language, textElement.Text, textBlock);

        public static void SetXamlInlinesWithTextStyleConfig(
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            IAdaptiveTextStyleConfig textStyle,
            IAdaptiveTextElement textElement,
            string language,
            string text,
            TextBlock textBlock)
        {
            var textRun = new AdaptiveTextRun
            {
                Text = text,
                Language = language,
            };

            TextWeight? weightToSet = textStyle.Weight;
            ForegroundColor? colorToSet = textStyle.Color;
            TextSize? sizeToSet = textStyle.Size;
            FontType? fontTypeToSet = textStyle.FontType;
            bool? isSubtleToSet = textStyle.IsSubtle;

            if (textElement != null)
            {
                if (textElement.Weight.HasValue)
                {
                    weightToSet = textElement.Weight;
                }

                if (textElement.Color.HasValue)
                {
                    colorToSet = textElement.Color;
                }

                if (textElement.Size.HasValue)
                {
                    sizeToSet = textElement.Size;
                }

                if (textElement.FontType.HasValue)
                {
                    fontTypeToSet = textElement.FontType;
                }

                if (textElement.IsSubtle.HasValue)
                {
                    isSubtleToSet = textElement.IsSubtle;
                }
            }

            textRun.Weight = weightToSet;
            textRun.Color = colorToSet;
            textRun.Size = sizeToSet;
            textRun.FontType = fontTypeToSet;
            textRun.IsSubtle = isSubtleToSet;

            StyleTextElement(textRun, renderContext, renderArgs, new TextRunStyleParameters(), textBlock);
            SetXamlInlines(textRun, renderContext, renderArgs, false, textBlock.Inlines);
        }

        public static void SetXamlInlinesWithFactSetTextConfig(
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            AdaptiveFactSetTextConfig factSetTextConfig,
            string language,
            string text,
            TextBlock textBlock)
        {
            SetXamlInlinesWithTextStyleConfig(renderContext, renderArgs, factSetTextConfig, null, language, text, textBlock);
            SetWrapProperties(textBlock, factSetTextConfig.Wrap);
            textBlock.MaxWidth = factSetTextConfig.MaxWidth;
        }

        public static void SetWrapProperties(TextBlock xamlTextBlock, bool wrap)
        {
            xamlTextBlock.TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            xamlTextBlock.TextTrimming = TextTrimming.CharacterEllipsis;
        }

        public static uint SetXamlInlines(
            IAdaptiveTextElement adaptiveTextElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            bool isInHyperlink,
            InlineCollection inlines)
        {
            var text = adaptiveTextElement.Text ?? string.Empty;
            var markdownParser = new MarkDownParser(text);
            var htmlString = markdownParser.TransformToHtml();

            bool handledAsHtml = false;
            uint characterLength = 0;

            if (markdownParser.HasHtmlTags())
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml("<root>" + htmlString + "</root>");
                characterLength = AddHtmlInlines(adaptiveTextElement, renderContext, renderArgs, xmlDocument, isInHyperlink, inlines);
                handledAsHtml = true;
            }

            if (!handledAsHtml)
            {
                characterLength = AddSingleTextInline(
                    adaptiveTextElement, renderContext, renderArgs, text, new TextRunStyleParameters(false, false, false, isInHyperlink), inlines);
            }

            return characterLength;
        }

        private static string GetTextFromXmlNode(XmlNode node) => node.InnerText;

        private static uint AddListInlines(
            IAdaptiveTextElement adaptiveTextElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            XmlNode node,
            bool isListOrdered,
            bool isInHyperlink,
            InlineCollection inlines)
        {
            uint iteration = 1;
            var startNode = node.Attributes?.GetNamedItem("start");
            if (startNode != null)
            {
                // Same semantics as std::stoul + SafeAdd: fall back to 1 on parse failure/overflow.
                var start = GetTextFromXmlNode(startNode);
                if (uint.TryParse(start, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    iteration = parsed;
                    var childrenLength = (uint)node.ChildNodes.Count;
                    if ((ulong)iteration + childrenLength - 1 > uint.MaxValue)
                    {
                        iteration = 1;
                    }
                }
                else
                {
                    iteration = 1;
                }
            }

            var listChild = node.FirstChild;
            uint totalCharacterLength = 0;
            bool isFirstListElement = true;
            while (listChild != null)
            {
                string listElementString = isFirstListElement ? string.Empty : "\n";
                if (!isListOrdered)
                {
                    listElementString += "\x2022 ";
                }
                else
                {
                    listElementString += iteration.ToString(CultureInfo.InvariantCulture) + ". ";
                }

                // Note: the C++ assigns (rather than adds) here; preserved for parity.
                totalCharacterLength = (uint)listElementString.Length;

                var run = new Run { Text = listElementString };
                StyleTextElement(adaptiveTextElement, renderContext, renderArgs, new TextRunStyleParameters(false, false, false, isInHyperlink), run);
                inlines.Add(run);

                uint textCharacterLength = AddTextInlines(
                    adaptiveTextElement, renderContext, renderArgs, listChild, new TextRunStyleParameters(false, false, false, isInHyperlink), inlines);

                totalCharacterLength += textCharacterLength;
                iteration++;
                listChild = listChild.NextSibling;
                isFirstListElement = false;
            }

            return totalCharacterLength;
        }

        private static uint AddLinkInline(
            IAdaptiveTextElement adaptiveTextElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            XmlNode node,
            bool isStrikethrough,
            bool isItalic,
            bool isUnderline,
            InlineCollection inlines)
        {
            var hrefNode = node.Attributes?.GetNamedItem("href");
            if (hrefNode == null)
            {
                throw new ArgumentException("Link is missing href");
            }

            var href = GetTextFromXmlNode(hrefNode);
            var uri = new Uri(href, UriKind.Absolute);

            var hyperlink = new Hyperlink { NavigateUri = uri };
            var characterLength = AddTextInlines(
                adaptiveTextElement, renderContext, renderArgs, node, new TextRunStyleParameters(isStrikethrough, isItalic, isUnderline, true), hyperlink.Inlines);
            inlines.Add(hyperlink);
            return characterLength;
        }

        public static uint AddSingleTextInline(
            IAdaptiveTextElement adaptiveTextElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            string stringToParse,
            TextRunStyleParameters styleParameters,
            InlineCollection inlines)
        {
            var run = new Run();
            var parser = new DateTimeParser(adaptiveTextElement.Language);
            run.Text = parser.GenerateString(stringToParse ?? string.Empty);
            StyleTextElement(adaptiveTextElement, renderContext, renderArgs, styleParameters, run);
            inlines.Add(run);
            return (uint)(stringToParse?.Length ?? 0);
        }

        public static uint AddTextInlines(
            IAdaptiveTextElement adaptiveTextElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            XmlNode node,
            TextRunStyleParameters styleParameters,
            InlineCollection inlines)
        {
            var childNode = node.FirstChild;
            uint totalCharacterLength = 0;
            try
            {
                while (childNode != null)
                {
                    var nodeName = childNode.Name;
                    bool isLinkResult = nodeName == "a";
                    bool isBoldResult = nodeName == "strong";
                    bool isItalicResult = nodeName == "em";
                    bool isTextResult = nodeName == "#text";

                    uint nodeCharacterLength;
                    if (isLinkResult)
                    {
                        nodeCharacterLength = AddLinkInline(
                            adaptiveTextElement,
                            renderContext,
                            renderArgs,
                            childNode,
                            styleParameters.IsStrikethrough,
                            styleParameters.IsItalic,
                            styleParameters.IsUnderline,
                            inlines);
                    }
                    else if (isTextResult)
                    {
                        var text = GetTextFromXmlNode(childNode);
                        nodeCharacterLength = AddSingleTextInline(adaptiveTextElement, renderContext, renderArgs, text, styleParameters, inlines);
                    }
                    else
                    {
                        var textElementToUse = adaptiveTextElement;
                        if (isBoldResult || isItalicResult)
                        {
                            textElementToUse = Util.CopyTextElement(adaptiveTextElement);
                            if (isBoldResult)
                            {
                                textElementToUse.Weight = TextWeight.Bolder;
                            }
                        }

                        nodeCharacterLength = AddTextInlines(
                            textElementToUse,
                            renderContext,
                            renderArgs,
                            childNode,
                            new TextRunStyleParameters(
                                styleParameters.IsStrikethrough,
                                styleParameters.IsItalic || isItalicResult,
                                styleParameters.IsUnderline,
                                styleParameters.IsInHyperlink),
                            inlines);
                    }

                    childNode = childNode.NextSibling;
                    totalCharacterLength += nodeCharacterLength;
                }
            }
            catch (Exception)
            {
                renderContext.AddWarning(WarningStatusCode.CustomWarning, "Adding Text Inlines failed.");
            }

            return totalCharacterLength;
        }

        public static uint AddHtmlInlines(
            IAdaptiveTextElement adaptiveTextElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            XmlNode node,
            bool isInHyperlink,
            InlineCollection inlines)
        {
            var childNode = node.FirstChild;
            if (childNode != null && childNode.Name == "root")
            {
                childNode = childNode.FirstChild;
            }

            uint totalCharacterLength = 0;
            while (childNode != null)
            {
                var nodeName = childNode.Name;
                bool isOrderedListResult = nodeName == "ol";
                bool isUnorderedListResult = nodeName == "ul";
                bool isParagraphResult = nodeName == "p";

                uint nodeCharacterLength;
                if (isOrderedListResult || isUnorderedListResult)
                {
                    nodeCharacterLength = AddListInlines(
                        adaptiveTextElement, renderContext, renderArgs, childNode, isOrderedListResult, isInHyperlink, inlines);
                }
                else if (isParagraphResult)
                {
                    nodeCharacterLength = AddTextInlines(
                        adaptiveTextElement, renderContext, renderArgs, childNode, new TextRunStyleParameters(false, false, false, isInHyperlink), inlines);

                    if (childNode.NextSibling != null)
                    {
                        inlines.Add(new LineBreak());
                    }
                }
                else
                {
                    nodeCharacterLength = AddHtmlInlines(adaptiveTextElement, renderContext, renderArgs, childNode, isInHyperlink, inlines);
                }

                childNode = childNode.NextSibling;
                totalCharacterLength += nodeCharacterLength;
            }

            return totalCharacterLength;
        }
    }
}
