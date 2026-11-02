// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Vertical page indicator for vertical carousels, mirroring a (hidden) <see cref="PipsPager"/>. Uno's vertical
    /// PipsPager only realizes a single pip (issue #9).
    /// </summary>
    internal sealed class VerticalPipsIndicator : StackPanel
    {
        private const double PipSlot = 12;
        private readonly WeakReference<PipsPager> _pager;

        public VerticalPipsIndicator(PipsPager pager)
        {
            _pager = new WeakReference<PipsPager>(pager);
            Orientation = Orientation.Vertical;
            VerticalAlignment = VerticalAlignment.Center;
            Margin = new Thickness(4, 0, 0, 0);

            for (int i = 0; i < pager.NumberOfPages; i++)
            {
                var index = i;
                var pip = new Button
                {
                    Width = PipSlot * 2,
                    Height = PipSlot,
                    MinWidth = 0,
                    MinHeight = 0,
                    Padding = new Thickness(0),
                    BorderThickness = new Thickness(0),
                    Background = new SolidColorBrush(Colors.Transparent),
                    Content = new Ellipse { Width = 4, Height = 4, Fill = new SolidColorBrush(Colors.Gray) },
                };
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(pip, $"Page {i + 1}");
                pip.Click += (s, e) =>
                {
                    if (_pager.TryGetTarget(out var p))
                    {
                        p.SelectedPageIndex = index;
                    }
                };
                Children.Add(pip);
            }

            pager.SelectedIndexChanged += (s, e) => Update(s.SelectedPageIndex);
            Update(pager.SelectedPageIndex);
        }

        private void Update(int selected)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] is Button { Content: Ellipse ellipse })
                {
                    var isSelected = i == selected;
                    ellipse.Width = ellipse.Height = isSelected ? 6 : 4;
                    ellipse.Fill = new SolidColorBrush(isSelected ? Colors.DimGray : Colors.Gray);
                }
            }
        }
    }
}
