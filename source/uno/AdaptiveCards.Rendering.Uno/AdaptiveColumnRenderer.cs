// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Port of AdaptiveColumnRenderer.cpp.</summary>
    public sealed class AdaptiveColumnRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var adaptiveColumn = (AdaptiveColumn)cardElement;
                var columnBorder = new Border();
                var columnPanel = new WholeItemsPanel();
                columnBorder.Child = columnPanel;

                var previousContextRtl = renderContext.Rtl;
                var currentRtl = previousContextRtl;
                var containerRtl = adaptiveColumn.Rtl;
                bool updatedRtl = false;
                if (containerRtl.HasValue)
                {
                    currentRtl = containerRtl;
                    renderContext.Rtl = currentRtl;
                    updatedRtl = true;
                }

                if (currentRtl.HasValue)
                {
                    columnPanel.FlowDirection = currentRtl.Value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                }

                var containerStyle = XamlHelpers.HandleStylingAndPadding(adaptiveColumn, columnBorder, renderContext, renderArgs);
                var newRenderArgs = new AdaptiveRenderArgs(containerStyle, renderArgs.ParentElement, renderArgs);

                XamlBuilder.BuildPanelChildren(adaptiveColumn.Items, columnPanel, renderContext, newRenderArgs, _ => { });

                if (updatedRtl)
                {
                    renderContext.Rtl = previousContextRtl;
                }

                var verticalContentAlignment = adaptiveColumn.VerticalContentAlignment ?? VerticalContentAlignment.Top;
                XamlHelpers.SetVerticalContentAlignmentToChildren(columnPanel, verticalContentAlignment);

                columnPanel.VerticalAlignment = VerticalAlignment.Stretch;
                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Column", columnPanel);

                uint columnMinHeight = adaptiveColumn.MinHeight;
                if (columnMinHeight > 0)
                {
                    columnPanel.MinHeight = columnMinHeight;
                }

                var selectAction = adaptiveColumn.SelectAction;
                UIElement columnAsUIElement;
                var backgroundImage = adaptiveColumn.BackgroundImage;
                if (Util.IsBackgroundImageValid(backgroundImage))
                {
                    var rootElement = new Grid();
                    XamlHelpers.ApplyBackgroundToRoot(rootElement, backgroundImage, renderContext);
                    XamlHelpers.AppendXamlElementToPanel(columnBorder, rootElement, cardElement.Height);
                    columnAsUIElement = rootElement;
                }
                else
                {
                    columnAsUIElement = columnBorder;
                }

                var hostConfig = renderContext.HostConfig;
                return ActionHelpers.HandleSelectAction(
                    cardElement, selectAction, renderContext, renderArgs, columnAsUIElement, XamlHelpers.SupportsInteractivity(hostConfig), false);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }
}
