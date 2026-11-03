// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Port of XamlHelpers::SeparatorParemeters.</summary>
    internal struct SeparatorParameters
    {
        // Field names follow the C++ struct (SeparatorParemeters) so ported call sites match.
        public Color color;
        public uint spacing;
        public uint thickness;
    }

    /// <summary>Port of the WinUI3 renderer's XamlHelpers namespace.</summary>
    internal static class XamlHelpers
    {
        private const string BackgroundImageOverlayBrushKey = "AdaptiveCard.BackgroundOverlayBrush";

        private static readonly Lazy<HttpClient> s_httpClient = new(() => new HttpClient());

        public static UIElement CreateSeparator(
            AdaptiveRenderContext renderContext, uint spacing, uint separatorThickness, Color separatorColor, bool isHorizontal = true)
        {
            var separator = new Grid
            {
                Background = new SolidColorBrush(separatorColor),
            };

            uint separatorMarginValue = spacing > separatorThickness ? (spacing - separatorThickness) / 2 : 0;
            var margin = new Thickness();

            if (isHorizontal)
            {
                margin.Top = margin.Bottom = separatorMarginValue;
                separator.Height = separatorThickness;
            }
            else
            {
                margin.Left = margin.Right = separatorMarginValue;
                separator.Width = separatorThickness;
            }

            separator.Margin = margin;

            SetStyleFromResourceDictionary(renderContext, "Adaptive.Separator", separator);

            return separator;
        }

        public static object TryGetResourceFromResourceDictionaries(ResourceDictionary resourceDictionary, string resourceName)
        {
            if (resourceDictionary == null)
            {
                return null;
            }

            try
            {
                // HasKey/Lookup in the C++ also consider merged dictionaries.
                return resourceDictionary.TryGetValue(resourceName, out var value) ? value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void SetStyleFromResourceDictionary(AdaptiveRenderContext renderContext, string resourceName, FrameworkElement frameworkElement)
        {
            var resourceDictionary = renderContext.OverrideStyles;
            if (TryGetResourceFromResourceDictionaries(resourceDictionary, resourceName) is Style style)
            {
                frameworkElement.Style = style;
            }
        }

        public static void SetSeparatorVisibility(Panel parentPanel)
        {
            // Iterate over the elements in a container and ensure that the correct separators are marked as visible
            bool foundPreviousVisibleElement = false;
            foreach (var child in parentPanel.Children)
            {
                if (child is FrameworkElement childAsFrameworkElement && childAsFrameworkElement.Tag is ElementTagContent elementTagContent)
                {
                    var separator = elementTagContent.Separator;
                    var visibility = child.Visibility;
                    var expectedVisibility = elementTagContent.ExpectedVisibility;

                    if (separator != null)
                    {
                        if (!expectedVisibility || !foundPreviousVisibleElement)
                        {
                            // If the element is collapsed, or if it's the first visible element, collapse the separator
                            // Images are hidden while they are retrieved, we shouldn't hide the separator
                            separator.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            // Otherwise show the separator
                            separator.Visibility = Visibility.Visible;
                        }
                    }

                    foundPreviousVisibleElement |= visibility == Visibility.Visible;
                }
            }
        }

        public static ContainerStyle HandleStylingAndPadding(
            IAdaptiveContainerBase adaptiveContainer, Border containerBorder, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var localContainerStyle = adaptiveContainer.Style;
            var parentContainerStyle = renderArgs.ContainerStyle;

            bool hasExplicitContainerStyle = true;
            if (localContainerStyle == ContainerStyle.None)
            {
                hasExplicitContainerStyle = false;
                localContainerStyle = parentContainerStyle;
            }

            var hostConfig = renderContext.HostConfig;
            var spacingConfig = hostConfig.Spacing;

            uint padding = spacingConfig.Padding;
            double paddingAsDouble = padding;

            bool addContainerPadding = renderArgs.AddContainerPadding;

            // If container style was explicitly assigned, apply background color and padding
            if (hasExplicitContainerStyle)
            {
                var backgroundColor = Util.GetBackgroundColorFromStyle(localContainerStyle, hostConfig);
                containerBorder.Background = new SolidColorBrush(backgroundColor);

                // If the container style doesn't match its parent apply padding.
                addContainerPadding |= localContainerStyle != parentContainerStyle;
            }

            if (addContainerPadding)
            {
                containerBorder.Padding = new Thickness(paddingAsDouble, paddingAsDouble, paddingAsDouble, paddingAsDouble);
            }

            // Find out which direction(s) we bleed in, and apply a negative margin to cause the
            // container to bleed
            var bleedDirection = adaptiveContainer.BleedDirection;

            var marginThickness = new Thickness(0);
            if (bleedDirection != BleedDirection.None)
            {
                if ((bleedDirection & BleedDirection.Left) != BleedDirection.None)
                {
                    marginThickness.Left = -paddingAsDouble;
                }

                if ((bleedDirection & BleedDirection.Right) != BleedDirection.None)
                {
                    marginThickness.Right = -paddingAsDouble;
                }

                if ((bleedDirection & BleedDirection.Up) != BleedDirection.None)
                {
                    marginThickness.Top = -paddingAsDouble;
                }

                if ((bleedDirection & BleedDirection.Down) != BleedDirection.None)
                {
                    marginThickness.Bottom = -paddingAsDouble;
                }

                containerBorder.Margin = marginThickness;
            }

            return localContainerStyle;
        }

        public static bool SupportsInteractivity(AdaptiveHostConfig hostConfig) => hostConfig.SupportsInteractivity;

        private static GridLength CalculateColumnWidth(bool isVisible, bool isAuto, bool isStretch, bool isUnsetWidth, uint pixelWidth, double ratioWidth)
        {
            bool isValidWidth = isAuto || isStretch || pixelWidth != 0 || isUnsetWidth || (ratioWidth > 0);

            if (!isVisible || isAuto || !isValidWidth)
            {
                // If the column isn't visible, or is set to "auto" or an invalid value ("-1", "foo"), set it to Auto
                return new GridLength(0, GridUnitType.Auto);
            }
            else if (pixelWidth != 0)
            {
                // If it's visible and pixel width is specified, use pixel width
                return new GridLength(pixelWidth, GridUnitType.Pixel);
            }
            else if (isStretch || isUnsetWidth)
            {
                // If it's visible and stretch is specified, or width is unset, use stretch with default of 1
                return new GridLength(1, GridUnitType.Star);
            }
            else
            {
                // If it's visible and the user specified a valid ratio, use that
                return new GridLength(ratioWidth, GridUnitType.Star);
            }
        }

        /// <summary>Port of _wtof: parses the longest numeric prefix, 0 on failure.</summary>
        private static double ParseLeadingDouble(string value) => Util.TryHStringToDouble(value) ?? 0.0;

        public static void HandleColumnWidth(AdaptiveColumn column, bool isVisible, ColumnDefinition columnDefinition)
        {
            var adaptiveColumnWidth = column.Width ?? string.Empty;
            bool isStretch = adaptiveColumnWidth == "stretch";
            bool isAuto = adaptiveColumnWidth == "auto";
            double widthAsDouble = ParseLeadingDouble(adaptiveColumnWidth);
            uint pixelWidth = column.PixelWidth;

            columnDefinition.Width = CalculateColumnWidth(
                isVisible, isAuto, isStretch, adaptiveColumnWidth.Length == 0, pixelWidth, widthAsDouble);
        }

        public static void HandleTableColumnWidth(AdaptiveTableColumnDefinition column, ColumnDefinition columnDefinition)
        {
            var widthRef = column.Width;
            var pixelWidthRef = column.PixelWidth;

            uint width = widthRef ?? 0;
            uint pixelWidth = pixelWidthRef ?? 0;

            bool isWidthUnset = widthRef == null && pixelWidthRef == null;

            columnDefinition.Width = CalculateColumnWidth(true, false, false, isWidthUnset, pixelWidth, width);
        }

        public static void AppendXamlElementToPanel(UIElement xamlElement, Panel panel, HeightType heightType = HeightType.Auto)
        {
            if (xamlElement == null)
            {
                return;
            }

            panel.Children.Add(xamlElement);

            if (heightType == HeightType.Stretch && panel is WholeItemsPanel wholeItemsPanel)
            {
                wholeItemsPanel.AddElementToStretchablesList(xamlElement);
            }
        }

        public static bool GetToggleValue(object item) => item is ToggleButton toggleButton && (toggleButton.IsChecked ?? false);

        public static TextBlock CreateTextBlockWithContent(string contentString, bool wrap)
        {
            var content = new TextBlock { Text = contentString ?? string.Empty };
            if (wrap)
            {
                content.TextWrapping = TextWrapping.WrapWholeWords;
            }

            return content;
        }

        public static void SetContent(object item, string contentString) => SetContent(item, contentString, false);

        public static void SetContent(object item, string contentString, bool wrap)
        {
            if (item is ContentControl contentControl)
            {
                contentControl.Content = CreateTextBlockWithContent(contentString, wrap);
            }
        }

        public static void SetVerticalContentAlignmentToChildren(object container, VerticalContentAlignment verticalContentAlignment)
        {
            if (container is WholeItemsPanel wholeItemsPanel)
            {
                wholeItemsPanel.SetVerticalContentAlignment(verticalContentAlignment);
            }
        }

        private static Image CreateBackgroundImage(AdaptiveRenderContext renderContext, TileControl tileControl, bool isSvg, Uri imageUrl)
        {
            try
            {
                if (isSvg)
                {
                    var svgImageSource = new SvgImageSource();
                    ConfigureSvgImageSourceAsync(imageUrl, svgImageSource, tileControl);

                    return new Image { Source = svgImageSource };
                }
                else
                {
                    if (imageUrl.Scheme == "data")
                    {
                        return RenderImageFromDataUri(imageUrl);
                    }
                    else
                    {
                        var bitmapImage = new BitmapImage { UriSource = imageUrl };
                        return new Image { Source = bitmapImage };
                    }
                }
            }
            catch (Exception ex)
            {
                renderContext.AddWarning(WarningStatusCode.CustomWarning, ex.Message + " Skipping rendering of background image.");
                return null;
            }
        }

        public static async void ConfigureSvgImageSourceAsync(Uri imageUrl, SvgImageSource svgImageSource, TileControl tileControl)
        {
            var weakImageSource = new WeakReference<SvgImageSource>(svgImageSource);
            var weakTileControl = new WeakReference<TileControl>(tileControl);

            try
            {
                if (imageUrl.Scheme == "data")
                {
                    var imagePath = Util.GetDataUriPath(imageUrl);
                    var foundBase64 = imagePath.IndexOf("base64", StringComparison.Ordinal);
                    byte[] bytes;

                    if (foundBase64 >= 0)
                    {
                        // Decode base 64 string
                        var data = AdaptiveBase64Util.ExtractDataFromUri(imagePath);
                        bytes = AdaptiveBase64Util.Decode(data);
                    }
                    else
                    {
                        // Extract <svg> tag from uri
                        bytes = Encoding.UTF8.GetBytes(Util.ExtractSvgDataFromUri(imageUrl));
                    }

                    if (weakImageSource.TryGetTarget(out var strongImageSource))
                    {
                        var stream = await Util.BytesToRandomAccessStreamAsync(bytes);
                        var size = await Util.ParseSizeOfSVGImageFromStreamAsync(stream);
                        stream.Seek(0);

                        strongImageSource.RasterizePixelHeight = size.Height;
                        strongImageSource.RasterizePixelWidth = size.Width;

                        await strongImageSource.SetSourceAsync(stream);

                        var image = new Image { Source = strongImageSource };

                        if (weakTileControl.TryGetTarget(out var strongTileControl))
                        {
                            strongTileControl.LoadImageBrush(image);
                        }
                    }

                    return;
                }

                // Uno: Windows.Web.Http.HttpClient is not available, use System.Net.Http instead.
                var svgString = await s_httpClient.Value.GetStringAsync(imageUrl);
                var parsedSize = Util.ParseSizeOfSVGImageFromXmlString(svgString);

                if (weakImageSource.TryGetTarget(out var imageSource))
                {
                    imageSource.RasterizePixelHeight = parsedSize.Height;
                    imageSource.RasterizePixelWidth = parsedSize.Width;

                    var image = new Image { Source = imageSource };
                    imageSource.UriSource = imageUrl;

                    if (weakTileControl.TryGetTarget(out var strongTileControl))
                    {
                        strongTileControl.LoadImageBrush(image);
                    }
                }
            }
            catch (Exception)
            {
                // fire_and_forget in the C++: failures are ignored.
            }
        }

        public static void ApplyBackgroundToRoot(Panel rootPanel, AdaptiveBackgroundImage adaptiveBackgroundImage, AdaptiveRenderContext renderContext)
        {
            // In order to reuse the image creation code paths, we simply create an adaptive card
            // image element and then build that into xaml and apply to the root.
            var imageUrl = Util.GetUrlFromString(renderContext.HostConfig, adaptiveBackgroundImage.Url);
            if (imageUrl != null)
            {
                var tileControl = new TileControl();
                bool isSvg = Util.IsSvgImage(imageUrl);

                var backgroundImage = CreateBackgroundImage(renderContext, tileControl, isSvg, imageUrl);
                if (backgroundImage != null)
                {
                    tileControl.IsEnabled = false;
                    tileControl.BackgroundImage = adaptiveBackgroundImage;

                    // SVGs need to be loaded asynchronously, so we load the image brush in ConfigureSvgImageSourceAsync
                    if (!isSvg)
                    {
                        tileControl.LoadImageBrush(backgroundImage);
                    }

                    AppendXamlElementToPanel(tileControl, rootPanel);

                    // The overlay applied to the background image is determined by a resource, so create
                    // the overlay if that resources exists
                    var resourceDictionary = renderContext.OverrideStyles;
                    if (TryGetResourceFromResourceDictionaries(resourceDictionary, BackgroundImageOverlayBrushKey) is Brush backgroundOverlayBrush)
                    {
                        var overlayRectangle = new Rectangle { Fill = backgroundOverlayBrush };
                        AppendXamlElementToPanel(overlayRectangle, rootPanel);
                    }
                }
            }
            else
            {
                renderContext.AddWarning(
                    WarningStatusCode.AssetLoadFailed,
                    "Specified URI:" + adaptiveBackgroundImage.Url + " for background image is not valid. Image loading has failed.");
            }
        }

        public static (UIElement, IAdaptiveCardElement) RenderFallback(
            IAdaptiveCardElement currentElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            if (currentElement == null)
            {
                return (null, null);
            }

            var elementRenderers = renderContext.ElementRenderers;
            var elementFallback = currentElement.FallbackType;
            var elementType = currentElement.ElementTypeString;

            bool fallbackHandled = false;
            UIElement fallbackControl = null;
            IAdaptiveCardElement renderedElement = null;
            UIElement result = null;

            switch (elementFallback)
            {
                case FallbackType.Content:
                {
                    // We have content, get the type of the fallback element
                    var fallbackElement = currentElement.FallbackContent;
                    var fallbackElementType = fallbackElement.ElementTypeString;

                    WarnForFallbackContentElement(renderContext, elementType, fallbackElementType);

                    // Try to render the fallback element
                    var fallbackElementRenderer = elementRenderers.Get(fallbackElementType);

                    if (fallbackElementRenderer != null)
                    {
                        // perform this element's fallback
                        try
                        {
                            fallbackControl = fallbackElementRenderer.Render(fallbackElement, renderContext, renderArgs);
                            renderedElement = fallbackElement;
                        }
                        catch (PerformFallbackException)
                        {
                            // The fallback content told us to fallback, make a recursive call to this method
                            (fallbackControl, renderedElement) = RenderFallback(fallbackElement, renderContext, renderArgs);
                        }
                    }
                    else
                    {
                        // The fallback content doesn't have a renderer, make a recursive call to this method
                        (fallbackControl, renderedElement) = RenderFallback(fallbackElement, renderContext, renderArgs);
                    }

                    fallbackHandled = true;
                    break;
                }

                case FallbackType.Drop:
                    // If the fallback is drop, nothing to do but warn
                    WarnForFallbackDrop(renderContext, elementType);
                    fallbackHandled = true;
                    break;

                case FallbackType.None:
                default:
                    break;
            }

            if (fallbackHandled)
            {
                // We did it, copy out the result if any
                result = fallbackControl;
            }
            else
            {
                // We didn't do it, can our ancestor?
                if (!renderArgs.AncestorHasFallback)
                {
                    // standard unknown element handling
                    renderContext.AddWarning(WarningStatusCode.NoRendererForType, "No Renderer found for type: " + elementType);
                }
                else
                {
                    throw new PerformFallbackException();
                }
            }

            return (result, renderedElement);
        }

        public static bool NeedsSeparator(IAdaptiveCardElement cardElement) =>
            cardElement.Separator || cardElement.Spacing != Spacing.None;

        public static void WarnFallbackString(AdaptiveRenderContext renderContext, string warning) =>
            renderContext.AddWarning(WarningStatusCode.PerformingFallback, warning);

        public static void WarnForFallbackContentElement(AdaptiveRenderContext renderContext, string parentElementType, string fallbackElementType) =>
            WarnFallbackString(
                renderContext,
                "Performing fallback for element of type \"" + parentElementType + "\" (fallback element type \"" + fallbackElementType + "\")");

        public static void WarnForFallbackDrop(AdaptiveRenderContext renderContext, string elementType) =>
            WarnFallbackString(renderContext, "Dropping element of type \"" + elementType + "\" for fallback");

        public static void LogErrRenderFailed(AdaptiveRenderContext renderContext, string elementType = "", string exceptionMessage = "")
        {
            var errorMessage = "Rendering failed";
            if (!string.IsNullOrEmpty(elementType))
            {
                errorMessage += " for element of type \"" + elementType + "\"";
            }

            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                errorMessage += " with the following message: \n" + exceptionMessage;
            }

            renderContext.AddError(ErrorStatusCode.RenderFailed, errorMessage);
        }

        public static void ErrForRenderFailed(AdaptiveRenderContext renderContext, string exceptionMessage = "") =>
            LogErrRenderFailed(renderContext, string.Empty, exceptionMessage);

        public static void ErrForRenderFailedForElement(AdaptiveRenderContext renderContext, string elementType, string exceptionMessage = "") =>
            LogErrRenderFailed(renderContext, elementType, exceptionMessage);

        public static void AddRenderedControl(
            UIElement newControl,
            IAdaptiveCardElement element,
            Panel parentPanel,
            UIElement separator,
            ColumnDefinition columnDefinition,
            Action<UIElement> childCreatedCallback)
        {
            if (newControl != null)
            {
                bool isVisible = element.IsVisible;
                if (!isVisible)
                {
                    newControl.Visibility = Visibility.Collapsed;
                }

                var newControlAsFrameworkElement = (FrameworkElement)newControl;
                var id = element.Id;

                if (!string.IsNullOrEmpty(id))
                {
                    newControlAsFrameworkElement.Name = id;
                }

                var heightType = element.Height;
                var tagContent = new ElementTagContent(
                    element, parentPanel, separator, columnDefinition, isVisible, heightType == HeightType.Stretch);
                newControlAsFrameworkElement.Tag = tagContent;

                AppendXamlElementToPanel(newControl, parentPanel, heightType);

                childCreatedCallback?.Invoke(newControl);
            }
        }

        public static void AddHandledTappedEvent(UIElement uiElement)
        {
            if (uiElement == null)
            {
                return;
            }

            // Tapped events bubble up to their parent, if a child element is a selectAction, mark the event handled
            uiElement.Tapped += (s, args) => args.Handled = true;
        }

        public static void SetAutoImageSize(FrameworkElement imageControl, object parentElement, ImageSource imageSource, bool setVisible)
        {
            double pixelHeight;
            double pixelWidth;

            if (imageSource is BitmapSource bitmap)
            {
                pixelHeight = bitmap.PixelHeight;
                pixelWidth = bitmap.PixelWidth;
            }
            else if (imageSource is SvgImageSource svg)
            {
                pixelHeight = svg.RasterizePixelHeight;
                pixelWidth = svg.RasterizePixelWidth;
            }
            else
            {
                return;
            }

            double maxHeight = imageControl.MaxHeight;
            double maxWidth = imageControl.MaxWidth;

            if (parentElement is ColumnDefinition parentAsColumnDefinition)
            {
                double parentWidth = parentAsColumnDefinition.ActualWidth;
                if (parentWidth >= pixelWidth)
                {
                    // Make sure to keep the aspect ratio of the image
                    maxWidth = Math.Min(maxWidth, parentWidth);
                    double aspectRatio = pixelHeight / pixelWidth;
                    maxHeight = maxWidth * aspectRatio;
                }
            }

            // Prevent an image from being stretched out if it is smaller than the
            // space allocated for it (when in auto mode).
            if (imageControl is Ellipse)
            {
                // don't need to set both width and height when image is ellipse
                // since we're proportionally scaling anyway
                imageControl.MaxWidth = Math.Min(maxWidth, pixelWidth);
            }
            else
            {
                imageControl.MaxHeight = Math.Min(maxHeight, pixelHeight);
                imageControl.MaxWidth = Math.Min(maxWidth, pixelWidth);
            }

            if (setVisible)
            {
                imageControl.Visibility = Visibility.Visible;
            }
        }

        public static UIElement AddSeparatorIfNeeded(
            ref int currentElement, IAdaptiveCardElement element, AdaptiveHostConfig hostConfig, AdaptiveRenderContext renderContext, Panel parentPanel)
        {
            // Never add a separator to the first element
            if (currentElement++ > 0)
            {
                if (NeedsSeparator(element))
                {
                    var separatorParams = GetSeparatorParameters(element, hostConfig);
                    var separator = CreateSeparator(renderContext, separatorParams.spacing, separatorParams.thickness, separatorParams.color);
                    AppendXamlElementToPanel(separator, parentPanel);
                    return separator;
                }
            }

            return null;
        }

        public static void ApplyMarginToXamlElement(AdaptiveHostConfig hostConfig, FrameworkElement element)
        {
            uint padding = hostConfig.Spacing.Padding;
            element.Margin = new Thickness(padding, padding, padding, padding);
        }

        private static void FormatLabelRunWithHostConfig(
            AdaptiveHostConfig hostConfig, AdaptiveInputLabelConfig inputLabelConfig, bool isHint, Run labelRun)
        {
            // If we're formatting the hint then use attention color
            var textColor = isHint ? ForegroundColor.Attention : inputLabelConfig.Color;

            var color = Util.GetColorFromAdaptiveColor(hostConfig, textColor, ContainerStyle.Default, false, false);
            labelRun.Foreground = new SolidColorBrush(color);

            var textSize = inputLabelConfig.Size;
            uint resultSize = Util.GetFontSizeFromFontType(hostConfig, FontType.Default, textSize);
            labelRun.FontSize = resultSize;
        }

        private static void AddRequiredHintInline(
            AdaptiveHostConfig hostConfig, AdaptiveInputLabelConfig inputLabelConfig, InlineCollection inlines)
        {
            var hintRun = new Run();
            var suffix = inputLabelConfig.Suffix;

            // If no suffix was defined, use * as default
            if (string.IsNullOrEmpty(suffix))
            {
                suffix = " *";
            }

            hintRun.Text = suffix;

            FormatLabelRunWithHostConfig(hostConfig, inputLabelConfig, true, hintRun);

            inlines.Add(hintRun);
        }

        public static UIElement RenderInputLabel(IAdaptiveInputElement adaptiveInputElement, AdaptiveRenderContext renderContext)
        {
            var inputLabel = adaptiveInputElement.Label;
            bool isRequired = adaptiveInputElement.IsRequired;

            if (!string.IsNullOrEmpty(inputLabel))
            {
                var xamlRichTextBlock = new RichTextBlock();
                var xamlParagraph = new Paragraph();
                xamlRichTextBlock.Blocks.Add(xamlParagraph);

                var xamlInlines = xamlParagraph.Inlines;

                var labelRun = new Run { Text = inputLabel };
                xamlInlines.Add(labelRun);

                var hostConfig = renderContext.HostConfig;
                var labelConfig = hostConfig.Inputs.Label;

                var inputLabelConfig = isRequired ? labelConfig.RequiredInputs : labelConfig.OptionalInputs;

                if (isRequired)
                {
                    AddRequiredHintInline(hostConfig, inputLabelConfig, xamlInlines);
                }

                FormatLabelRunWithHostConfig(hostConfig, inputLabelConfig, false, labelRun);

                xamlRichTextBlock.TextWrapping = TextWrapping.Wrap;

                return xamlRichTextBlock;
            }
            else if (isRequired)
            {
                renderContext.AddWarning(
                    WarningStatusCode.EmptyLabelInRequiredInput, "Input is required but there's no label for required hint rendering");
            }

            return null;
        }

        private static void FormatErrorMessageWithHostConfig(AdaptiveRenderContext renderContext, TextBlock errorMessage)
        {
            var hostConfig = renderContext.HostConfig;
            var errorMessageConfig = hostConfig.Inputs.ErrorMessage;

            uint resultSize = Util.GetFontSizeFromFontType(hostConfig, FontType.Default, errorMessageConfig.Size);
            errorMessage.FontSize = resultSize;

            errorMessage.FontWeight = Util.GetFontWeightFromStyle(hostConfig, FontType.Default, errorMessageConfig.Weight);
        }

        public static UIElement RenderInputErrorMessage(IAdaptiveInputElement adaptiveInputElement, AdaptiveRenderContext renderContext)
        {
            // Add the error message if present
            var errorMessage = adaptiveInputElement.ErrorMessage;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                var errorMessageTextBlock = new TextBlock { Text = errorMessage };

                // Set the color to Attention color
                var hostConfig = renderContext.HostConfig;
                var attentionColor = Util.GetColorFromAdaptiveColor(hostConfig, ForegroundColor.Attention, ContainerStyle.Default, false, false);
                errorMessageTextBlock.Foreground = new SolidColorBrush(attentionColor);

                // Format the error message through host config
                FormatErrorMessageWithHostConfig(renderContext, errorMessageTextBlock);

                // Error message should begin collapsed and only be show when validated
                errorMessageTextBlock.Visibility = Visibility.Collapsed;
                errorMessageTextBlock.TextWrapping = TextWrapping.Wrap;

                return errorMessageTextBlock;
            }

            return null;
        }

        public static Border CreateValidationBorder(UIElement childElement, AdaptiveRenderContext renderContext)
        {
            var hostConfig = renderContext.HostConfig;
            var attentionColor = Util.GetColorFromAdaptiveColor(hostConfig, ForegroundColor.Attention, ContainerStyle.Default, false, false);

            // Create a border in the attention color. The thickness is 0 for now so it won't be visibile until validation is run
            return new Border
            {
                BorderBrush = new SolidColorBrush(attentionColor),
                Child = childElement,
            };
        }

        public static UIElement HandleLabelAndErrorMessage(IAdaptiveInputElement adaptiveInput, AdaptiveRenderContext renderContext, UIElement inputLayout)
        {
            // Create a new stack panel to add the label and error message
            // The contents from the input panel will be copied to the new panel
            var inputStackPanel = new StackPanel();

            var hostConfig = renderContext.HostConfig;
            var inputsConfig = hostConfig.Inputs;

            // Render the label and add it to the stack panel (schema 1.6 inline labels go next to the input instead)
            var label = RenderInputLabel(adaptiveInput, renderContext);
            var inputBase = adaptiveInput as AdaptiveInputElementBase;
            bool inlineLabel = label != null && inputBase?.LabelPosition == InputLabelPosition.Inline;
            if (!inlineLabel)
            {
                AppendXamlElementToPanel(label, inputStackPanel);
            }

            // Render the spacing between the label and the input
            if (label != null && !inlineLabel)
            {
                var labelSpacing = inputsConfig.Label.InputSpacing;
                uint spacing = Util.GetSpacingSizeFromSpacing(hostConfig, labelSpacing);

                var separator = CreateSeparator(renderContext, spacing, 0, default);
                AppendXamlElementToPanel(separator, inputStackPanel);
            }

            // Copy the contents into the new panel and get the rendered element to set accessibility properties
            UIElement actualUIElement = null;
            if (inputLayout is Panel inputPanel)
            {
                var panelChildren = inputPanel.Children;

                // We only copy one element into the input panel, so if there's only one element, the input was rendered without
                // inline actions or validation borders
                int childrenCount = panelChildren.Count;
                if (childrenCount == 1)
                {
                    var onlyElement = panelChildren[0];

                    // We enclose multiple items using a border, so we try to check for it
                    if (onlyElement is Border inputBorder)
                    {
                        actualUIElement = inputBorder.Child;
                    }
                    else
                    {
                        actualUIElement = onlyElement;
                    }

                    panelChildren.RemoveAt(0);
                    AppendXamlElementToPanel(onlyElement, inputStackPanel);
                }
                else
                {
                    var children = panelChildren.ToList();
                    panelChildren.Clear();
                    foreach (var child in children)
                    {
                        AppendXamlElementToPanel(child, inputStackPanel);
                    }
                }
            }
            else
            {
                actualUIElement = inputLayout;
                AppendXamlElementToPanel(inputLayout, inputStackPanel);
            }

            // Render the error message
            var errorMessageControl = RenderInputErrorMessage(adaptiveInput, renderContext);
            if (errorMessageControl != null)
            {
                var errorSpacing = inputsConfig.ErrorMessage.Spacing;
                uint spacing = Util.GetSpacingSizeFromSpacing(hostConfig, errorSpacing);

                var separator = CreateSeparator(renderContext, spacing, 0, default);

                // Set the error message on the input value
                var inputValue = renderContext.GetInputValue(adaptiveInput);
                if (inputValue != null)
                {
                    inputValue.ErrorMessage = errorMessageControl;
                }

                AppendXamlElementToPanel(separator, inputStackPanel);
                AppendXamlElementToPanel(errorMessageControl, inputStackPanel);
            }

            // Assign AutomationProperties.LabeledBy
            if (label != null && actualUIElement != null)
            {
                AutomationProperties.SetLabeledBy(actualUIElement, label);

                // This is a workaround for TextBox (and AutoSuggestBox) not supporting the LabeledBy property for screen readers.
                if (adaptiveInput.IsRequired && actualUIElement is AutoSuggestBox)
                {
                    AutomationProperties.SetName(actualUIElement, adaptiveInput.Label + " required, ");
                }
                else
                {
                    AutomationProperties.SetName(actualUIElement, adaptiveInput.Label);
                }
            }

            if (inputBase?.InputStyle == InputStyle.RevealOnHover && actualUIElement is Control revealControl)
            {
                ApplyRevealOnHover(revealControl);
            }

            if (inlineLabel)
            {
                return CreateInlineLabelLayout(label, inputStackPanel, inputBase, renderContext);
            }

            return inputStackPanel;
        }

        /// <summary>
        /// Schema 1.6 labelPosition "inline" (from the JS renderer): the label sits to the left of the input, vertically
        /// centered, taking labelWidth (percent, or "Npx") or the host config's inputs.label.width percent.
        /// </summary>
        private static UIElement CreateInlineLabelLayout(
            UIElement label, Panel inputPanel, AdaptiveInputElementBase input, AdaptiveRenderContext renderContext)
        {
            var grid = new Grid();
            GridLength labelColumn, inputColumn;
            if (AdaptiveInputElementBase.TryParseLabelWidth(input.LabelWidth, out var size, out var isPixels))
            {
                labelColumn = isPixels ? new GridLength(size) : new GridLength(size, GridUnitType.Star);
                inputColumn = isPixels ? new GridLength(1, GridUnitType.Star) : new GridLength(100 - size, GridUnitType.Star);
            }
            else
            {
                var percent = renderContext.HostConfig.Inputs.Label.Width;
                labelColumn = new GridLength(percent, GridUnitType.Star);
                inputColumn = new GridLength(100 - percent, GridUnitType.Star);
            }

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = labelColumn });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = inputColumn });
            if (label is FrameworkElement labelElement)
            {
                labelElement.VerticalAlignment = VerticalAlignment.Center;
                labelElement.Margin = new Thickness(0, 0, Util.GetSpacingSizeFromSpacing(renderContext.HostConfig, Spacing.Small), 0);
            }

            Grid.SetColumn((FrameworkElement)label, 0);
            Grid.SetColumn(inputPanel, 1);
            grid.Children.Add(label);
            grid.Children.Add(inputPanel);
            return grid;
        }

        /// <summary>
        /// Schema 1.6 inputStyle "revealOnHover" (from the JS renderer): the input reads like plain text (no border or
        /// background) until it is hovered or focused.
        /// </summary>
        private static void ApplyRevealOnHover(Control control)
        {
            var transparent = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            bool hovered = false;

            void Update()
            {
                bool reveal = hovered || control.FocusState != FocusState.Unfocused;
                if (reveal)
                {
                    control.ClearValue(Control.BorderBrushProperty);
                    control.ClearValue(Control.BackgroundProperty);
                }
                else
                {
                    control.BorderBrush = transparent;
                    control.Background = transparent;
                }

                AutomationProperties.SetItemStatus(control, reveal ? "revealed" : "hidden");
            }

            control.PointerEntered += (s, e) => { hovered = true; Update(); };
            control.PointerExited += (s, e) => { hovered = false; Update(); };
            control.GotFocus += (s, e) => Update();
            control.LostFocus += (s, e) => Update();
            Update();
        }

        public static (UIElement, Border) HandleInputLayoutAndValidation(
            IAdaptiveInputElement adaptiveInput,
            UIElement inputUIElement,
            bool hasTypeSpecificValidation,
            AdaptiveRenderContext renderContext,
            bool ifValidationBorderIsNeeded = true)
        {
            // Create a stack panel for the input and related controls
            var inputStackPanel = new StackPanel();

            var inputUIElementParentContainer = inputUIElement;

            // If there's any validation on this input, put the input inside a border
            var isRequired = adaptiveInput.IsRequired;
            var hasValidation = hasTypeSpecificValidation || isRequired;

            if (hasValidation && string.IsNullOrEmpty(adaptiveInput.ErrorMessage))
            {
                renderContext.AddWarning(WarningStatusCode.MissingValidationErrorMessage, "Inputs with validation should include an errorMessage");
            }

            Border validationBorder = null;
            if (hasValidation && ifValidationBorderIsNeeded)
            {
                validationBorder = CreateValidationBorder(inputUIElement, renderContext);
                inputUIElementParentContainer = validationBorder;
            }

            AppendXamlElementToPanel(inputUIElementParentContainer, inputStackPanel);

            // Different input renderers perform stuff differently
            // Input.Text and Input.Date render the border previously so the object received as parameter may be a border
            // Input.Time and Input.Number send an object that is not a border
            UIElement actualInputUIElement;
            if (hasValidation && ifValidationBorderIsNeeded)
            {
                actualInputUIElement = validationBorder.Child;
            }
            else if (hasValidation)
            {
                if (inputUIElementParentContainer is Border containerAsBorder)
                {
                    validationBorder = containerAsBorder;
                    actualInputUIElement = containerAsBorder.Child;
                }
                else
                {
                    actualInputUIElement = inputUIElement;
                }
            }
            else
            {
                actualInputUIElement = inputUIElement;
            }

            // Self-reference to set the required property
            if (actualInputUIElement != null)
            {
                AutomationProperties.SetIsRequiredForForm(actualInputUIElement, isRequired);

                // In the case of Input.Toggle we have to add the title as a description
                if (adaptiveInput is AdaptiveToggleInput &&
                    actualInputUIElement is ContentControl uiInputElementAsContentControl &&
                    uiInputElementAsContentControl.Content is DependencyObject contentAsDependencyObject)
                {
                    try
                    {
                        AutomationProperties.GetDescribedBy(actualInputUIElement)?.Add(contentAsDependencyObject);
                    }
                    catch (Exception)
                    {
                        // Uno: AutomationProperties.DescribedBy may not be supported on all targets.
                    }
                }
            }

            return (inputStackPanel, validationBorder);
        }

        public static SeparatorParameters GetSeparatorParameters(IAdaptiveCardElement element, AdaptiveHostConfig hostConfig)
        {
            var parameters = new SeparatorParameters
            {
                spacing = Util.GetSpacingSizeFromSpacing(hostConfig, element.Spacing),
            };

            // If the element has a separator, get the separator color and thickness from the host config
            if (element.Separator)
            {
                var separatorConfig = hostConfig.Separator;
                parameters.color = separatorConfig.LineColor;
                parameters.thickness = separatorConfig.LineThickness;
            }

            return parameters;
        }

        public static Image RenderImageFromDataUri(Uri imageUrl)
        {
            var image = new Image();
            var bitmapImage = new BitmapImage
            {
                CreateOptions = BitmapCreateOptions.IgnoreImageCache,
            };

            // Decode base 64 string
            var data = AdaptiveBase64Util.ExtractDataFromUri(Util.GetDataUriPath(imageUrl));
            var decodedData = AdaptiveBase64Util.Decode(data);

            LoadDataIntoBitmapAsync(image, bitmapImage, decodedData);

            return image;
        }

        private static async void LoadDataIntoBitmapAsync(Image image, BitmapImage bitmapImage, byte[] decodedData)
        {
            try
            {
                var stream = await Util.BytesToRandomAccessStreamAsync(decodedData);
                image.Source = bitmapImage;
                await bitmapImage.SetSourceAsync(stream);
            }
            catch (Exception)
            {
                // Matches the C++ fire-and-forget completion handler.
            }
        }
    }
}
