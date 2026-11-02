// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Port of AdaptiveTableRenderer.cpp.</summary>
    public sealed class AdaptiveTableRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var adaptiveTable = (AdaptiveTable)cardElement;
                var xamlGrid = new Grid();

                var tableHorizontalAlignment = adaptiveTable.HorizontalCellContentAlignment;
                var contextHorizontalAlignment = renderContext.HorizontalContentAlignment;
                if (tableHorizontalAlignment.HasValue)
                {
                    renderContext.HorizontalContentAlignment = tableHorizontalAlignment;
                }

                var tableVerticalAlignment = adaptiveTable.VerticalCellContentAlignment;
                bool showGridLines = adaptiveTable.ShowGridLines;

                var columns = adaptiveTable.Columns;
                foreach (var column in columns)
                {
                    var xamlColumnDefinition = new ColumnDefinition();
                    XamlHelpers.HandleTableColumnWidth(column, xamlColumnDefinition);
                    xamlGrid.ColumnDefinitions.Add(xamlColumnDefinition);
                }

                bool firstRowAsHeaders = adaptiveTable.FirstRowAsHeaders;
                var gridStyle = adaptiveTable.GridStyle;

                uint rowNumber = 0;
                foreach (var row in adaptiveTable.Rows)
                {
                    RenderRow(row, columns, renderContext, renderArgs, tableVerticalAlignment, firstRowAsHeaders, showGridLines, gridStyle, rowNumber, xamlGrid);
                    rowNumber++;
                }

                renderContext.HorizontalContentAlignment = contextHorizontalAlignment;
                return xamlGrid;
            }
            catch (Exception ex)
            {
                // Faithful to the C++ renderer: all errors (including fallback requests) are reported and swallowed.
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }

        private static FrameworkElement RenderCell(
            AdaptiveTableCell cell,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            VerticalContentAlignment? verticalContentAlignment,
            bool showGridLines,
            ContainerStyle gridStyle,
            uint rowNumber,
            uint columnNumber)
        {
            var cellVerticalAlignment = cell.VerticalContentAlignment;
            if (cellVerticalAlignment == null)
            {
                cell.VerticalContentAlignment = verticalContentAlignment;
            }

            if (showGridLines)
            {
                renderArgs.AddContainerPadding = true;
            }

            var containerRenderer = renderContext.ElementRenderers.Get("Container");
            var cellRenderedAsContainer = containerRenderer.Render(cell, renderContext, renderArgs);

            var hostConfig = renderContext.HostConfig;
            FrameworkElement cellFrameworkElement;

            if (showGridLines)
            {
                var cellBorder = new Border();
                var borderColor = Util.GetBorderColorFromStyle(gridStyle, hostConfig);
                cellBorder.BorderBrush = new SolidColorBrush(borderColor);

                var borderThickness = new Thickness(0, 0, 1, 1);
                if (columnNumber == 0)
                {
                    borderThickness.Left = 1;
                }

                if (rowNumber == 0)
                {
                    borderThickness.Top = 1;
                }

                cellBorder.BorderThickness = borderThickness;
                cellBorder.Child = cellRenderedAsContainer;
                cellFrameworkElement = cellBorder;

                renderArgs.AddContainerPadding = false;
            }
            else
            {
                cellFrameworkElement = cellRenderedAsContainer as FrameworkElement;

                double cellSpacingDouble = hostConfig.Table.CellSpacing;
                if (cellSpacingDouble != 0 && cellFrameworkElement != null)
                {
                    var marginThickness = new Thickness(cellSpacingDouble, cellSpacingDouble, 0, 0);
                    if (columnNumber == 0)
                    {
                        marginThickness.Left = 0;
                    }

                    if (rowNumber == 0)
                    {
                        marginThickness.Top = 0;
                    }

                    if (marginThickness.Left != 0 || marginThickness.Top != 0)
                    {
                        cellFrameworkElement.Margin = marginThickness;
                    }
                }
            }

            if (cellVerticalAlignment == null)
            {
                cell.VerticalContentAlignment = null;
            }

            return cellFrameworkElement;
        }

        private static void RenderRow(
            AdaptiveTableRow row,
            IList<AdaptiveTableColumnDefinition> columns,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            VerticalContentAlignment? verticalContentAlignment,
            bool firstRowAsHeaders,
            bool showGridLines,
            ContainerStyle gridStyle,
            uint rowNumber,
            Grid xamlGrid)
        {
            xamlGrid.RowDefinitions.Add(new RowDefinition());

            var contextTextStyle = renderContext.TextStyle;
            if (rowNumber == 0 && firstRowAsHeaders)
            {
                renderContext.TextStyle = TextStyle.ColumnHeader;
            }

            var contextHorizontalAlignment = renderContext.HorizontalContentAlignment;
            var rowHorizontalAlignment = row.HorizontalCellContentAlignment;

            var rowVerticalContentAlignment = row.VerticalCellContentAlignment ?? verticalContentAlignment;

            uint columnNumber = 0;
            foreach (var cell in row.Cells)
            {
                // Cells beyond the defined columns are ignored (the C++ GetAt would fail and abort the table render).
                if (columnNumber >= columns.Count)
                {
                    break;
                }

                var columnDefinition = columns[(int)columnNumber];
                var columnHorizontalAlignment = columnDefinition.HorizontalCellContentAlignment;

                if (rowHorizontalAlignment.HasValue)
                {
                    renderContext.HorizontalContentAlignment = rowHorizontalAlignment;
                }
                else
                {
                    renderContext.HorizontalContentAlignment = columnHorizontalAlignment ?? contextHorizontalAlignment;
                }

                var cellFrameworkElement =
                    RenderCell(cell, renderContext, renderArgs, rowVerticalContentAlignment, showGridLines, gridStyle, rowNumber, columnNumber);

                if (cellFrameworkElement != null)
                {
                    Grid.SetColumn(cellFrameworkElement, (int)columnNumber);
                    Grid.SetRow(cellFrameworkElement, (int)rowNumber);
                    XamlHelpers.AppendXamlElementToPanel(cellFrameworkElement, xamlGrid);
                }

                columnNumber++;
            }

            renderContext.TextStyle = contextTextStyle;
            renderContext.HorizontalContentAlignment = contextHorizontalAlignment;
        }
    }
}
