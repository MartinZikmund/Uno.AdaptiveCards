// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Port of AdaptiveCarouselRenderer.cpp: a FlipView of pages plus a PipsPager page indicator.
    /// Beyond the WinUI3 renderer, this port also honors <see cref="AdaptiveCarousel.Timer"/> (auto-advance),
    /// <see cref="AdaptiveCarousel.AutoLoop"/> (wrap-around when auto-advancing) and vertical <see cref="AdaptiveCarousel.Orientation"/>.
    /// </summary>
    public sealed class AdaptiveCarouselRenderer : IAdaptiveElementRenderer
    {
        private const string VerticalItemsPanelXaml =
            "<ItemsPanelTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
            "<VirtualizingStackPanel Orientation=\"Vertical\" AreScrollSnapPointsRegular=\"True\" /></ItemsPanelTemplate>";

        private readonly XamlBuilder _xamlBuilder;

        public AdaptiveCarouselRenderer()
        {
        }

        internal AdaptiveCarouselRenderer(XamlBuilder xamlBuilder)
        {
            _xamlBuilder = xamlBuilder;
        }

        internal XamlBuilder GetXamlBuilder() => _xamlBuilder;

        public UIElement Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
        {
            var carousel = (AdaptiveCarousel)element;
            var styledCollection = (IAdaptiveContainerBase)element;
            bool isVertical = carousel.Orientation == CarouselOrientation.Vertical;

            var stackPanel = new StackPanel { Orientation = isVertical ? Orientation.Horizontal : Orientation.Vertical };
            var carouselUI = new FlipView();

            bool loopEnabled = carousel.AutoLoop ?? false;

            var pipsPager = new PipsPager
            {
                NumberOfPages = carousel.Pages.Count,
            };

            if (isVertical)
            {
                pipsPager.Orientation = Orientation.Vertical;
                pipsPager.VerticalAlignment = VerticalAlignment.Center;
                TrySetVerticalItemsPanel(carouselUI);
            }
            else
            {
                pipsPager.HorizontalAlignment = HorizontalAlignment.Center;
            }

            var hostConfig = context.HostConfig;
            carouselUI.Background = new SolidColorBrush(Util.GetColorFromString("#00FFFFFF"));

            var previousContextRtl = context.Rtl;
            var currentRtl = previousContextRtl;
            var containerRtl = carousel.Rtl;
            bool updatedRtl = false;
            if (containerRtl.HasValue)
            {
                currentRtl = containerRtl;
                context.Rtl = currentRtl;
                updatedRtl = true;
            }

            if (currentRtl.HasValue)
            {
                var flowDirection = currentRtl.Value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                carouselUI.FlowDirection = flowDirection;
                pipsPager.FlowDirection = flowDirection;
            }

            var weakCarouselUI = new WeakReference<FlipView>(carouselUI);
            var weakPipsPager = new WeakReference<PipsPager>(pipsPager);

            carouselUI.SelectionChanged += (s, e) =>
            {
                if (!weakCarouselUI.TryGetTarget(out var flipView) || !weakPipsPager.TryGetTarget(out var pager))
                {
                    return;
                }

                var val = flipView.SelectedIndex;
                if (loopEnabled && val == flipView.Items.Count)
                {
                    pager.SelectedPageIndex = 0;
                    flipView.SelectedIndex = 0;
                }
                else if (val >= 0 && pager.SelectedPageIndex != val)
                {
                    pager.SelectedPageIndex = val;
                }
            };

            pipsPager.SelectedIndexChanged += (pager, args) =>
            {
                if (weakCarouselUI.TryGetTarget(out var flipView) && flipView.SelectedIndex != pager.SelectedPageIndex)
                {
                    flipView.SelectedIndex = pager.SelectedPageIndex;
                }
            };

            stackPanel.Children.Add(carouselUI);
            stackPanel.Children.Add(pipsPager);

            var featureRegistration = context.FeatureRegistration;
            bool ancestorHasFallback = renderArgs.AncestorHasFallback;

            try
            {
                var carouselBorder = new Border();
                var gridContainer = new WholeItemsPanel();

                var containerHeightType = element.Height;
                if (containerHeightType == HeightType.Auto)
                {
                    gridContainer.VerticalAlignment = VerticalAlignment.Stretch;
                }

                uint carouselMinHeight = styledCollection.MinHeight;
                if (carouselMinHeight > 0)
                {
                    gridContainer.MinHeight = carouselMinHeight;
                }

                var fixedHeightInPixel = carousel.HeightInPixels;
                if (fixedHeightInPixel != 0)
                {
                    carouselUI.MaxHeight = fixedHeightInPixel;
                    if (isVertical)
                    {
                        // A vertical FlipView needs a definite height to page; use the fixed height.
                        carouselUI.Height = fixedHeightInPixel;
                    }
                }

                var containerStyle = XamlHelpers.HandleStylingAndPadding(styledCollection, carouselBorder, context, renderArgs);
                var newRenderArgs = new AdaptiveRenderArgs(containerStyle, renderArgs.ParentElement, renderArgs);

                foreach (var page in carousel.Pages)
                {
                    var (carouselPageUI, _) = XamlBuilder.RenderAsUIElement(page, context, newRenderArgs, featureRegistration, ancestorHasFallback);
                    if (carouselPageUI != null)
                    {
                        carouselUI.Items.Add(carouselPageUI);
                        if (fixedHeightInPixel == 0 && carouselPageUI is FrameworkElement pageFrameworkElement)
                        {
                            pageFrameworkElement.LayoutUpdated += (s, e) =>
                            {
                                if (weakCarouselUI.TryGetTarget(out var flipView))
                                {
                                    SetFlipViewMaxHeight(flipView);
                                }
                            };
                        }
                    }
                }

                if (carousel.InitialPage.HasValue && carousel.InitialPage.Value < carouselUI.Items.Count)
                {
                    carouselUI.SelectedIndex = (int)carousel.InitialPage.Value;
                }

                if (carousel.Timer is ulong timer && timer > 0 && carouselUI.Items.Count > 1)
                {
                    SetupAutoAdvance(carouselUI, TimeSpan.FromMilliseconds(timer), loopEnabled);
                }

                var verticalContentAlignment = carousel.VerticalContentAlignment ?? VerticalContentAlignment.Top;
                XamlHelpers.SetVerticalContentAlignmentToChildren(gridContainer, verticalContentAlignment);

                var backgroundImage = carousel.BackgroundImage;
                if (Util.IsBackgroundImageValid(backgroundImage))
                {
                    var rootElement = new Grid();
                    XamlHelpers.ApplyBackgroundToRoot(rootElement, backgroundImage, context);
                    carouselBorder.Child = rootElement;
                    XamlHelpers.AppendXamlElementToPanel(gridContainer, rootElement, containerHeightType);
                }
                else
                {
                    carouselBorder.Child = gridContainer;
                }

                if (updatedRtl)
                {
                    context.Rtl = previousContextRtl;
                }

                XamlHelpers.SetStyleFromResourceDictionary(context, "Adaptive.Carousel", carouselUI);

                XamlHelpers.AppendXamlElementToPanel(stackPanel, gridContainer, element.Height);

                var selectAction = styledCollection.SelectAction;
                return ActionHelpers.HandleSelectAction(
                    element, selectAction, context, renderArgs, carouselBorder, XamlHelpers.SupportsInteractivity(hostConfig), true);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(context, element.ElementTypeString, ex.Message);
            }

            return null;
        }

        private static void TrySetVerticalItemsPanel(FlipView flipView)
        {
            try
            {
                if (XamlReader.Load(VerticalItemsPanelXaml) is ItemsPanelTemplate template)
                {
                    flipView.ItemsPanel = template;
                }
            }
            catch (Exception)
            {
                // Fall back to the default (horizontal) panel if runtime XAML loading is unavailable.
            }
        }

        private static void SetupAutoAdvance(FlipView flipView, TimeSpan interval, bool loopEnabled)
        {
            var timer = new DispatcherTimer { Interval = interval };
            var weakFlipView = new WeakReference<FlipView>(flipView);

            timer.Tick += (s, e) =>
            {
                if (!weakFlipView.TryGetTarget(out var fv))
                {
                    timer.Stop();
                    return;
                }

                int count = fv.Items.Count;
                int next = fv.SelectedIndex + 1;
                if (next >= count)
                {
                    if (!loopEnabled)
                    {
                        timer.Stop();
                        return;
                    }

                    next = 0;
                }

                fv.SelectedIndex = next;
            };

            flipView.Loaded += (s, e) => timer.Start();
            flipView.Unloaded += (s, e) => timer.Stop();
        }

        internal static void SetFlipViewMaxHeight(FlipView flipView)
        {
            var selectIndex = flipView.SelectedIndex;
            if (selectIndex < 0 || selectIndex >= flipView.Items.Count)
            {
                return;
            }

            if (flipView.Items[selectIndex] is FrameworkElement carouselPageUI && flipView.IsLoaded)
            {
                float width = (float)flipView.ActualWidth;
                var noVerticalLimit = new Size(width, double.PositiveInfinity);
                carouselPageUI.Measure(noVerticalLimit);
                var maxHeight = carouselPageUI.DesiredSize.Height;
                var previousMaxHeight = flipView.MaxHeight;
                if (double.IsPositiveInfinity(previousMaxHeight) || previousMaxHeight < maxHeight)
                {
                    flipView.MaxHeight = maxHeight;
                }
            }
        }
    }
}
