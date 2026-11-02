// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Uniform-cell horizontal wrap panel used for ImageSet on Uno targets. Equivalent to the WinUI
    /// <c>VariableSizedWrapGrid</c> (Orientation=Horizontal, no explicit item size) used by the WinUI3 renderer, which
    /// on Uno intermittently throws "Occupancy map cannot be null" during arrange (see issue #6). The cell size is the
    /// largest desired child size, so cells don't collapse while the first image is still downloading.
    /// </summary>
    public sealed class ImageSetWrapPanel : Panel
    {
        private Size _cellSize;

        protected override Size MeasureOverride(Size availableSize)
        {
            double cellWidth = 0;
            double cellHeight = 0;
            foreach (var child in Children)
            {
                child.Measure(availableSize);
                cellWidth = Math.Max(cellWidth, child.DesiredSize.Width);
                cellHeight = Math.Max(cellHeight, child.DesiredSize.Height);
            }

            _cellSize = new Size(cellWidth, cellHeight);
            var visibleCount = 0;
            foreach (var child in Children)
            {
                if (child.Visibility == Visibility.Visible)
                {
                    visibleCount++;
                }
            }

            if (visibleCount == 0 || cellWidth <= 0)
            {
                return new Size(0, visibleCount == 0 ? 0 : cellHeight);
            }

            var perRow = double.IsInfinity(availableSize.Width)
                ? visibleCount
                : Math.Max(1, (int)Math.Floor(availableSize.Width / cellWidth));
            var rows = (int)Math.Ceiling(visibleCount / (double)perRow);
            return new Size(Math.Min(visibleCount, perRow) * cellWidth, rows * cellHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double x = 0;
            double y = 0;
            foreach (var child in Children)
            {
                if (child.Visibility != Visibility.Visible)
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                    continue;
                }

                if (x > 0 && x + _cellSize.Width > finalSize.Width + 0.5)
                {
                    x = 0;
                    y += _cellSize.Height;
                }

                child.Arrange(new Rect(x, y, _cellSize.Width, _cellSize.Height));
                x += _cellSize.Width;
            }

            return finalSize;
        }
    }
}
