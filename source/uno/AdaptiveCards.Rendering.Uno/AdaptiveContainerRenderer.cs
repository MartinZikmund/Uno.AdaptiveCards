// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Renders Container (and, via delegation, TableCell and CarouselPage). Port of AdaptiveContainerRenderer.cpp.</summary>
    public sealed class AdaptiveContainerRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var adaptiveContainer = (IAdaptiveContainer)cardElement;
                var adaptiveContainerBase = (IAdaptiveContainerBase)cardElement;
                var containerPanel = new WholeItemsPanel();

                var previousContextRtl = renderContext.Rtl;
                var currentRtl = previousContextRtl;
                var containerRtl = adaptiveContainer.Rtl;
                bool updatedRtl = false;
                if (containerRtl.HasValue)
                {
                    currentRtl = containerRtl;
                    renderContext.Rtl = currentRtl;
                    updatedRtl = true;
                }

                if (currentRtl.HasValue)
                {
                    containerPanel.FlowDirection = currentRtl.Value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                }

                var containerHeightType = cardElement.Height;
                if (containerHeightType == HeightType.Auto)
                {
                    containerPanel.VerticalAlignment = VerticalAlignment.Stretch;
                }

                uint containerMinHeight = adaptiveContainerBase.MinHeight;
                if (containerMinHeight > 0)
                {
                    containerPanel.MinHeight = containerMinHeight;
                }

                var containerBorder = new Border();
                var containerStyle = XamlHelpers.HandleStylingAndPadding(adaptiveContainerBase, containerBorder, renderContext, renderArgs);
                var newRenderArgs = new AdaptiveRenderArgs(containerStyle, renderArgs.ParentElement, renderArgs);

                XamlBuilder.BuildPanelChildren(adaptiveContainer.Items, containerPanel, renderContext, newRenderArgs, _ => { });

                if (updatedRtl)
                {
                    renderContext.Rtl = previousContextRtl;
                }

                var verticalContentAlignment = adaptiveContainer.VerticalContentAlignment ?? VerticalContentAlignment.Top;
                XamlHelpers.SetVerticalContentAlignmentToChildren(containerPanel, verticalContentAlignment);

                var backgroundImage = adaptiveContainer.BackgroundImage;
                if (Util.IsBackgroundImageValid(backgroundImage))
                {
                    var rootElement = new Grid();
                    XamlHelpers.ApplyBackgroundToRoot(rootElement, backgroundImage, renderContext);
                    containerBorder.Child = rootElement;
                    XamlHelpers.AppendXamlElementToPanel(containerPanel, rootElement, containerHeightType);
                }
                else
                {
                    containerBorder.Child = containerPanel;
                }

                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Container", containerPanel);

                var selectAction = adaptiveContainerBase.SelectAction;
                var hostConfig = renderContext.HostConfig;
                return ActionHelpers.HandleSelectAction(
                    cardElement, selectAction, renderContext, renderArgs, containerBorder, XamlHelpers.SupportsInteractivity(hostConfig), true);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }

    /// <summary>Renders CarouselPage by delegating to the registered Container renderer. Port of AdaptiveCarouselPageRenderer.cpp.</summary>
    public sealed class AdaptiveCarouselPageRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var containerRenderer = renderContext.ElementRenderers.Get("Container");
            return containerRenderer?.Render(cardElement, renderContext, renderArgs);
        }
    }
}
