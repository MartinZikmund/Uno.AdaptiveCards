// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveImageSetRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var adaptiveImageSet = (AdaptiveImageSet)cardElement;

#if HAS_UNO
                // Uno's VariableSizedWrapGrid is unreliable here (issue #6); use an equivalent uniform wrap panel.
                var xamlGrid = new ImageSetWrapPanel();
#else
                var xamlGrid = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal };
#endif

                var imageSetConfig = renderContext.HostConfig.ImageSet;
                var imageSize = adaptiveImageSet.ImageSize;
                if (imageSize == ImageSize.None)
                {
                    imageSize = imageSetConfig.ImageSize;
                }

                var imageRenderer = renderContext.ElementRenderers.Get("Image");
                if (imageRenderer != null && adaptiveImageSet.Style == ImageSetStyle.Stacked && adaptiveImageSet.Images.Count is 1 or 2)
                {
                    return RenderStacked(adaptiveImageSet, imageSize, imageRenderer, renderContext, renderArgs);
                }

                if (imageRenderer != null && adaptiveImageSet.Style == ImageSetStyle.Grid)
                {
                    return RenderGrid(adaptiveImageSet, imageSize, imageRenderer, renderContext, renderArgs);
                }

                if (imageRenderer != null)
                {
                    var childRenderArgs = new AdaptiveRenderArgs(renderArgs.ContainerStyle, xamlGrid, renderArgs);
                    foreach (var adaptiveImage in adaptiveImageSet.Images)
                    {
                        adaptiveImage.Size = imageSize;
                        var uiImage = imageRenderer.Render(adaptiveImage, renderContext, childRenderArgs);
                        if (uiImage is FrameworkElement uiImageAsFrameworkElement)
                        {
                            uiImageAsFrameworkElement.MaxHeight = imageSetConfig.MaxImageHeight;
                            XamlHelpers.AppendXamlElementToPanel(uiImageAsFrameworkElement, xamlGrid);
                        }
                    }
                }
                else
                {
                    renderContext.AddWarning(WarningStatusCode.NoRendererForType, "No renderer found for type: Image");
                    return null;
                }

                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.ImageSet", xamlGrid);
                return xamlGrid;
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }

        private static uint GetEffectiveImageSize(AdaptiveRenderContext renderContext, ImageSize imageSize)
        {
            var sizes = renderContext.HostConfig.ImageSizes;
            return imageSize switch
            {
                ImageSize.Small => sizes.Small,
                ImageSize.Large => sizes.Large,
                _ => sizes.Medium,
            };
        }

        /// <summary>
        /// "grid" presentation style (Adaptive Cards 1.6, not supported by the WinUI3 renderer; ported from the JS
        /// renderer): every image fills a square cell of the effective image size, clipped as needed, 5px apart.
        /// </summary>
        private static UIElement RenderGrid(
            AdaptiveImageSet imageSet, ImageSize imageSize, IAdaptiveElementRenderer imageRenderer, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var cell = GetEffectiveImageSize(renderContext, imageSize);
            var panel = new ImageSetWrapPanel { Spacing = 5 };
            var childRenderArgs = new AdaptiveRenderArgs(renderArgs.ContainerStyle, panel, renderArgs);
            foreach (var adaptiveImage in imageSet.Images)
            {
                adaptiveImage.Size = ImageSize.Stretch;
                if (imageRenderer.Render(adaptiveImage, renderContext, childRenderArgs) is FrameworkElement uiImage)
                {
                    var host = new Border
                    {
                        Width = cell,
                        Height = cell,
                        Child = uiImage,
                    };
                    foreach (var image in FindImages(uiImage))
                    {
                        image.Stretch = Stretch.UniformToFill;
                        image.MaxWidth = double.PositiveInfinity;
                        image.MaxHeight = double.PositiveInfinity;
                    }

                    host.SizeChanged += (s, e) =>
                        ((Border)s).Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
                    panel.Children.Add(host);
                }
            }

            XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.ImageSet", panel);
            return panel;
        }

        /// <summary>
        /// "stacked" presentation style (Adaptive Cards 1.6, not supported by the WinUI3 renderer; ported from the JS
        /// renderer's StackedImageConfigurator): one image is shown round; with two images the first is placed bottom left
        /// with a border in the container's background color and the second top right, diagonally at 45 degrees.
        /// </summary>
        private static UIElement RenderStacked(
            AdaptiveImageSet imageSet, ImageSize imageSize, IAdaptiveElementRenderer imageRenderer, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            const double sign45 = 0.7071;
            const double border = 5;
            double dimension = GetEffectiveImageSize(renderContext, imageSize);
            double normalizationConstant = (dimension * sign45 - 0.5 * dimension) * 2;
            double offset = sign45 * (Math.Max(imageSet.Offset, -dimension) - normalizationConstant);

            var root = new Grid { HorizontalAlignment = HorizontalAlignment.Left };
            var childRenderArgs = new AdaptiveRenderArgs(renderArgs.ContainerStyle, root, renderArgs);

            FrameworkElement RenderRound(AdaptiveImage image)
            {
                image.Style = ImageStyle.Person;
                image.Size = ImageSize.None;
                image.PixelWidth = (uint)dimension;
                image.PixelHeight = (uint)dimension;
                var element = imageRenderer.Render(image, renderContext, childRenderArgs) as FrameworkElement;
                if (element != null)
                {
                    element.HorizontalAlignment = HorizontalAlignment.Left;
                    element.VerticalAlignment = VerticalAlignment.Top;
                }

                return element;
            }

            var first = RenderRound(imageSet.Images[0]);
            if (imageSet.Images.Count == 1 || first == null)
            {
                if (first != null)
                {
                    root.Children.Add(first);
                }

                return root;
            }

            var second = RenderRound(imageSet.Images[1]);
            var backgroundColor = Util.GetBackgroundColorFromStyle(renderArgs.ContainerStyle, renderContext.HostConfig);

            // First image: moved down by (offset + dimension), with a border in the background color, drawn on top.
            var firstHost = new Border
            {
                Child = first,
                Padding = new Thickness(border),
                Background = new SolidColorBrush(backgroundColor),
                CornerRadius = new CornerRadius((dimension + 2 * border) / 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, Math.Max(0, offset + dimension), 0, 0),
            };

            // Second image: right of the first (offset from its border box) and moved up.
            double secondLeft = dimension + 2 * border + offset;
            double secondTop = Math.Max(0, -(offset + dimension));
            if (second != null)
            {
                second.Margin = new Thickness(Math.Max(0, secondLeft), secondTop + border, 0, 0);
                root.Children.Add(second);
            }

            root.Children.Add(firstHost);
            return root;
        }

        private static IEnumerable<Image> FindImages(DependencyObject root)
        {
            if (root is Image image)
            {
                yield return image;
            }

            if (root is Border b && b.Child != null)
            {
                foreach (var i in FindImages(b.Child))
                {
                    yield return i;
                }
            }
            else if (root is Panel p)
            {
                foreach (var child in p.Children)
                {
                    foreach (var i in FindImages(child))
                    {
                        yield return i;
                    }
                }
            }
            else if (root is ContentControl c && c.Content is DependencyObject content)
            {
                foreach (var i in FindImages(content))
                {
                    yield return i;
                }
            }
        }
    }
}
