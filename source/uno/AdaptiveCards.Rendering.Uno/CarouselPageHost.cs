// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Hosts a carousel page inside the FlipView. Measures the page with an unbounded height and reports that natural
    /// height, so the carousel can size the FlipView to its tallest page (the WinUI3 renderer probes this from
    /// LayoutUpdated, which causes a layout cycle on Uno).
    /// </summary>
    internal sealed class CarouselPageHost : Panel
    {
        private UIElement _content;
        private double _lastNaturalHeight = -1;

        public event Action<double> NaturalHeightChanged;

        public UIElement Content
        {
            get => _content;
            set
            {
                if (_content != null)
                {
                    Children.Remove(_content);
                }

                _content = value;
                if (value != null)
                {
                    Children.Add(value);
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_content == null)
            {
                return new Size(0, 0);
            }

            _content.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            var desired = _content.DesiredSize;
            if (Math.Abs(desired.Height - _lastNaturalHeight) > 0.5)
            {
                _lastNaturalHeight = desired.Height;
                var height = desired.Height;

                // Raise outside of the measure pass.
                if (DispatcherQueue is { } queue)
                {
                    queue.TryEnqueue(() => NaturalHeightChanged?.Invoke(height));
                }
                else
                {
                    NaturalHeightChanged?.Invoke(height);
                }
            }

            return new Size(desired.Width, Math.Min(desired.Height, availableSize.Height));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _content?.Arrange(new Rect(0, 0, finalSize.Width, Math.Max(finalSize.Height, _content.DesiredSize.Height)));
            return finalSize;
        }
    }
}
