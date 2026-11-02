// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Port of AdaptiveColumnSetRenderer.cpp.</summary>
    public sealed class AdaptiveColumnSetRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var adaptiveColumnSet = (AdaptiveColumnSet)cardElement;
                var columnSetBorder = new Border();
                var gridContainer = new WholeItemsPanel();
                columnSetBorder.Child = gridContainer;

                var containerStyle = XamlHelpers.HandleStylingAndPadding(adaptiveColumnSet, columnSetBorder, renderContext, renderArgs);
                var newRenderArgs = new AdaptiveRenderArgs(containerStyle, renderArgs.ParentElement, renderArgs);

                var xamlGrid = new Grid();
                int currentColumn = 0;

                var columnRenderer = renderContext.ElementRenderers.Get("Column");
                if (columnRenderer == null)
                {
                    renderContext.AddWarning(WarningStatusCode.NoRendererForType, "No renderer found for type: Column");
                    return null;
                }

                var hostConfig = renderContext.HostConfig;
                bool ancestorHasFallback = renderArgs.AncestorHasFallback;

                foreach (var column in adaptiveColumnSet.Columns)
                {
                    var columnDefinitions = xamlGrid.ColumnDefinitions;
                    newRenderArgs.AncestorHasFallback = ancestorHasFallback || column.FallbackType != FallbackType.None;

                    UIElement xamlColumn;
                    try
                    {
                        xamlColumn = columnRenderer.Render(column, renderContext, newRenderArgs);
                    }
                    catch (PerformFallbackException)
                    {
                        (xamlColumn, _) = XamlHelpers.RenderFallback(column, renderContext, newRenderArgs);
                    }

                    newRenderArgs.AncestorHasFallback = ancestorHasFallback;

                    if (xamlColumn != null)
                    {
                        UIElement separator = null;
                        if (currentColumn > 0 && XamlHelpers.NeedsSeparator(column))
                        {
                            var separatorParams = XamlHelpers.GetSeparatorParameters(column, hostConfig);
                            var separatorColumnDefinition = new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Auto) };
                            columnDefinitions.Add(separatorColumnDefinition);

                            separator = XamlHelpers.CreateSeparator(
                                renderContext, separatorParams.spacing, separatorParams.thickness, separatorParams.color, false);
                            if (separator is FrameworkElement separatorAsFrameworkElement)
                            {
                                Grid.SetColumn(separatorAsFrameworkElement, currentColumn++);
                            }

                            XamlHelpers.AppendXamlElementToPanel(separator, xamlGrid);
                        }

                        var columnDefinition = new ColumnDefinition();
                        XamlHelpers.HandleColumnWidth(column, column.IsVisible, columnDefinition);
                        columnDefinitions.Add(columnDefinition);

                        if (xamlColumn is FrameworkElement columnAsFrameworkElement)
                        {
                            Grid.SetColumn(columnAsFrameworkElement, currentColumn++);
                        }

                        XamlHelpers.AddRenderedControl(xamlColumn, column, xamlGrid, separator, columnDefinition, _ => { });
                    }
                }

                XamlHelpers.SetSeparatorVisibility(xamlGrid);

                var horizontalAlignment = adaptiveColumnSet.HorizontalAlignment ?? HAlignment.Left;
                if (horizontalAlignment == HAlignment.Center)
                {
                    xamlGrid.HorizontalAlignment = HorizontalAlignment.Center;
                }
                else if (horizontalAlignment == HAlignment.Right)
                {
                    xamlGrid.HorizontalAlignment = HorizontalAlignment.Right;
                }

                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.ColumnSet", xamlGrid);
                xamlGrid.VerticalAlignment = VerticalAlignment.Stretch;

                var selectAction = adaptiveColumnSet.SelectAction;
                var columnSetHeightType = adaptiveColumnSet.Height;
                var columnSetMinHeight = adaptiveColumnSet.MinHeight;
                if (columnSetMinHeight > 0)
                {
                    gridContainer.MinHeight = columnSetMinHeight;
                }

                XamlHelpers.AppendXamlElementToPanel(xamlGrid, gridContainer, columnSetHeightType);

                return ActionHelpers.HandleSelectAction(
                    cardElement, selectAction, renderContext, renderArgs, columnSetBorder, XamlHelpers.SupportsInteractivity(hostConfig), true);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }
}
