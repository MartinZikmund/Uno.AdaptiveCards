// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Lays out an inline input label (child 0) to the left of the input (child 1), for schema 1.6
    /// <c>labelPosition: "inline"</c>. The label column is <see cref="LabelWidth"/> percent of the width, or
    /// <see cref="LabelWidth"/> pixels when <see cref="IsPixelWidth"/>; the label is vertically centered on the input.
    /// Unlike star-sized Grid columns it also lays out sensibly when measured with an unbounded width.
    /// </summary>
    public sealed class InlineLabelPanel : Panel
    {
        public double LabelWidth { get; set; } = 30;

        public bool IsPixelWidth { get; set; }

        public double LabelSpacing { get; set; }

        private UIElement Label => Children.Count > 0 ? Children[0] : null;

        private UIElement Input => Children.Count > 1 ? Children[1] : null;

        private double LabelColumnWidth(double totalWidth) =>
            Math.Max(0, Math.Min(totalWidth, IsPixelWidth ? LabelWidth : totalWidth * LabelWidth / 100));

        protected override Size MeasureOverride(Size availableSize)
        {
            var label = Label;
            var input = Input;
            if (double.IsInfinity(availableSize.Width))
            {
                label?.Measure(new Size(IsPixelWidth ? LabelWidth : double.PositiveInfinity, availableSize.Height));
                input?.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                var labelWidth = IsPixelWidth ? LabelWidth : (label?.DesiredSize.Width ?? 0) + LabelSpacing;
                return new Size(
                    labelWidth + (input?.DesiredSize.Width ?? 0),
                    Math.Max(label?.DesiredSize.Height ?? 0, input?.DesiredSize.Height ?? 0));
            }

            var labelColumn = LabelColumnWidth(availableSize.Width);
            label?.Measure(new Size(Math.Max(0, labelColumn - LabelSpacing), availableSize.Height));
            input?.Measure(new Size(Math.Max(0, availableSize.Width - labelColumn), availableSize.Height));
            return new Size(
                availableSize.Width,
                Math.Max(label?.DesiredSize.Height ?? 0, input?.DesiredSize.Height ?? 0));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var labelColumn = LabelColumnWidth(finalSize.Width);
            if (Label is { } label)
            {
                var height = Math.Min(label.DesiredSize.Height, finalSize.Height);
                label.Arrange(new Rect(0, (finalSize.Height - height) / 2, Math.Max(0, labelColumn - LabelSpacing), height));
            }

            Input?.Arrange(new Rect(labelColumn, 0, Math.Max(0, finalSize.Width - labelColumn), finalSize.Height));
            return finalSize;
        }
    }
}
