// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Text;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Panel that lays out its children vertically and only shows whole items (port of WholeItemsPanel).
    /// Items that don't fit are truncated (moved out of view) and stretchable items share the remaining height.
    /// </summary>
    public partial class WholeItemsPanel : Panel
    {
        private const float OutsidePanelY = -1000.0f;

        private static uint s_bleedMargin;

        private uint _visibleCount;
        private uint _measuredCount;
        private uint _stretchableItemCount;
        private float _calculatedSize;
        private bool _allElementsRendered;
        private VerticalContentAlignment _verticalContentAlignment;
        private bool _isMainPanel;
        private bool _isTruncated;
        private bool _adaptiveHeight;

        public WholeItemsPanel()
        {
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            float currentHeight = 0;
            float maxDesiredWidth = 0;
            bool visible = true;

            var children = Children;
            uint count = (uint)children.Count;

            var noVerticalLimit = new Size(availableSize.Width, double.PositiveInfinity);
            var measuredAvailableSize = availableSize;
            _visibleCount = count;

            for (int i = 0; i < count; i++)
            {
                var child = children[i];
                child.Measure(noVerticalLimit);

                if (visible)
                {
                    var childSize = child.DesiredSize;
                    float newHeight = currentHeight + (float)childSize.Height;

                    if (newHeight > measuredAvailableSize.Height)
                    {
                        double availableHeightForItem = measuredAvailableSize.Height - currentHeight;
                        bool keepItem = _isMainPanel && i == 0; // by default, only keep the first item in the main panel.

                        // Item does not fit
                        // 1. We mark the current panel as truncated
                        //    2.1 If the child is a panel, always render it (in order to get proper truncation)
                        //    2.2 If the child is a text block, check if it can be partially rendered
                        //    2.3 If the child is an image, check if it can be cropped
                        if (child is Panel childAsPanel)
                        {
                            _isTruncated = true;
                            if (!keepItem)
                            {
                                var remainingSpace = new Size(measuredAvailableSize.Width, availableHeightForItem);
                                child.Measure(remainingSpace);
                                _isTruncated = IsAnySubgroupTruncated(childAsPanel);
                            }
                        }

                        if (child is TextBlock childAsTextBlock)
                        {
                            if (childAsTextBlock.TextWrapping == TextWrapping.NoWrap)
                            {
                                _isTruncated = true;
                            }
                            else
                            {
                                var noLimit = new Size(double.PositiveInfinity, double.PositiveInfinity);
                                childAsTextBlock.TextWrapping = TextWrapping.NoWrap;
                                childAsTextBlock.Measure(noLimit);
                                childSize = childAsTextBlock.DesiredSize;

                                if (childSize.Height <= availableHeightForItem)
                                {
                                    keepItem = true;
                                }
                                else
                                {
                                    if (_isMainPanel)
                                    {
                                        keepItem = true;
                                        child.ClearValue(FrameworkElement.MinHeightProperty);
                                    }
                                    else
                                    {
                                        keepItem = false;
                                        child.Measure(noVerticalLimit);
                                    }
                                }

                                _isTruncated = !keepItem;
                            }
                        }

                        if (child is Image childAsImage)
                        {
                            if (!HasExplicitSize(childAsImage))
                            {
                                childAsImage.Stretch = Stretch.Uniform;
                                keepItem = true;
                                _isTruncated = false;
                            }
                            else
                            {
                                _isTruncated = true;
                            }
                        }

                        if (child is Shape childAsShape)
                        {
                            if (!HasExplicitSize(childAsShape))
                            {
                                LayoutCroppedImage(childAsShape, measuredAvailableSize.Width, measuredAvailableSize.Height - currentHeight);
                                keepItem = true;
                                _isTruncated = false;
                            }
                            else
                            {
                                _isTruncated = true;
                            }
                        }

                        if (keepItem)
                        {
                            measuredAvailableSize.Height = measuredAvailableSize.Height - currentHeight;
                            child.Measure(measuredAvailableSize);
                            childSize = child.DesiredSize;
                            _visibleCount = (uint)(i + 1);
                            currentHeight = Math.Max(currentHeight + (float)childSize.Height, (float)measuredAvailableSize.Height);
                            maxDesiredWidth = Math.Max((float)childSize.Width, maxDesiredWidth);
                        }
                        else
                        {
                            _visibleCount = (uint)i;
                        }

                        visible = false;
                        continue;
                    }

                    currentHeight = newHeight;
                    maxDesiredWidth = Math.Max((float)childSize.Width, maxDesiredWidth);
                }
            }

            _calculatedSize = currentHeight;
            _allElementsRendered = _visibleCount == count;

            if (double.IsPositiveInfinity(measuredAvailableSize.Width))
            {
                return new Size(maxDesiredWidth, currentHeight);
            }

            return new Size(measuredAvailableSize.Width, currentHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            float currentHeight = 0;
            float extraPaddingPerItem = 0;
            var children = Children;
            _measuredCount = (uint)children.Count;

            if (_stretchableItemCount != 0)
            {
                extraPaddingPerItem = (float)Math.Floor((finalSize.Height - _calculatedSize) / _stretchableItemCount);
            }
            else
            {
                if (_verticalContentAlignment == VerticalContentAlignment.Center)
                {
                    currentHeight = (float)((finalSize.Height - _calculatedSize) / 2);
                }
                else if (_verticalContentAlignment == VerticalContentAlignment.Bottom)
                {
                    currentHeight = (float)(finalSize.Height - _calculatedSize);
                }
            }

            for (int i = 0; i < _measuredCount; ++i)
            {
                var child = children[i];
                var childSize = child.DesiredSize;

                if (i < _visibleCount)
                {
                    float childHeight = (float)childSize.Height;
                    float newHeight = currentHeight + (float)childSize.Height;

                    if (_allElementsRendered && IsUIElementInStretchableList(child))
                    {
                        childHeight += extraPaddingPerItem;
                        newHeight += extraPaddingPerItem;
                    }
                    else if (newHeight > finalSize.Height)
                    {
                        // If we have truncated the first item newHeight may be greater than finalSize
                        childHeight = (float)(finalSize.Height - currentHeight);
                        newHeight = (float)finalSize.Height;
                    }

                    var rc = new Rect(0, currentHeight, finalSize.Width, Math.Max(0, childHeight));
                    child.Arrange(rc);
                    currentHeight = newHeight;
                }
                else
                {
                    // Arrange the child outside the panel
                    var rc = new Rect(0, OutsidePanelY - childSize.Height, childSize.Width, childSize.Height);
                    child.Arrange(rc);
                }
            }

            // Now set the clipping region to ensure that items moved away will never be rendered
            // But we allow the items to slightly expand above the panel because we explicitly set negative
            // margins for text on the first line of a tile. Additionally, leave at least as much space on all
            // sides as specified by s_bleedMargin.
            var margin = Margin;
            double bleedMargin = s_bleedMargin;
            double x0 = -Math.Max(margin.Left, bleedMargin);
            double y0 = -Math.Max(margin.Top, bleedMargin);
            double x1 = Math.Max(margin.Left, bleedMargin) + finalSize.Width + Math.Max(margin.Right, bleedMargin);
            double y1 = Math.Max(margin.Top, bleedMargin) + finalSize.Height + Math.Max(margin.Bottom, bleedMargin);

            // The C++ passes (x0, y0, x1, y1) as (X, Y, Width, Height); keep the same rectangle.
            Clip = new RectangleGeometry { Rect = new Rect(x0, y0, Math.Max(0, x1), Math.Max(0, y1)) };

            return new Size(finalSize.Width, finalSize.Height);
        }

        protected override void OnApplyTemplate()
        {
            // Nothing to do (matches the C++).
        }

        /// <summary>Returns the alt text of the visible content of the panel.</summary>
        public string GetAltText()
        {
            var buffer = new StringBuilder();
            AppendAltText(buffer);
            return buffer.ToString();
        }

        public bool IsAllContentClippedOut() => _measuredCount > 0 && _visibleCount == 0;

        public bool IsTruncated() => _isTruncated;

        internal void AppendAltText(StringBuilder buffer)
        {
            uint count = _visibleCount;
            var children = Children;

            if (_adaptiveHeight)
            {
                // If we are rendering in adaptive height mode, we might not have rendered all the items.
                count = (uint)children.Count;
            }

            for (int i = 0; i < count && i < children.Count; i++)
            {
                AppendAltTextToUIElement(children[i], buffer);
            }
        }

        internal void SetMainPanel(bool value) => _isMainPanel = value;

        internal void SetAdaptiveHeight(bool value) => _adaptiveHeight = value;

        internal static void SetBleedMargin(uint bleedMargin) => s_bleedMargin = bleedMargin;

        internal void SetVerticalContentAlignment(VerticalContentAlignment verticalContentAlignment) =>
            _verticalContentAlignment = verticalContentAlignment;

        internal void AddElementToStretchablesList(UIElement element)
        {
            if (element is FrameworkElement elementAsFrameworkElement)
            {
                if (elementAsFrameworkElement.Tag is ElementTagContent tagAsElementTagContent)
                {
                    tagAsElementTagContent.IsStretchable = true;
                }
                else if (elementAsFrameworkElement.Tag == null)
                {
                    elementAsFrameworkElement.Tag = new ElementTagContent { IsStretchable = true };
                }
            }

            ++_stretchableItemCount;
        }

        internal bool IsUIElementInStretchableList(UIElement element) =>
            element is FrameworkElement fe && fe.Tag is ElementTagContent tag && tag.IsStretchable;

        private static bool IsAnySubgroupTruncated(Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is WholeItemsPanel childAsWholeItemPanel && childAsWholeItemPanel.IsTruncated())
                {
                    return true;
                }
            }

            return false;
        }

        private static void LayoutCroppedImage(Shape shape, double availableWidth, double availableHeight)
        {
            var margins = shape.Margin;
            double effectiveAvailableWidth = availableWidth - margins.Left - margins.Right;
            double effectiveAvailableHeight = availableHeight - margins.Top - margins.Bottom;

            shape.Width = Math.Max(0, effectiveAvailableWidth);
            shape.Height = Math.Max(0, effectiveAvailableHeight);
        }

        private static void AppendText(string text, StringBuilder buffer)
        {
            if (buffer.Length > 0)
            {
                buffer.Append('\n');
            }

            buffer.Append(text);
        }

        private static string GetAltAsString(UIElement element) => AutomationProperties.GetName(element) ?? string.Empty;

        private static void AppendAltTextToUIElement(UIElement element, StringBuilder buffer)
        {
            if (element is TextBlock textBlock)
            {
                AppendText(textBlock.Text, buffer);
            }
            else if (element is WholeItemsPanel wholeItemsPanel)
            {
                wholeItemsPanel.AppendAltText(buffer);
            }
            else if (element is Image || element is Shape)
            {
                var altText = GetAltAsString(element);
                if (!string.IsNullOrEmpty(altText))
                {
                    AppendText(altText, buffer);
                }
            }
            else if (element is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    AppendAltTextToUIElement(child, buffer);
                }
            }
            else if (element is Border border && border.Child != null)
            {
                AppendAltTextToUIElement(border.Child, buffer);
            }
        }

        private static bool HasExplicitSize(FrameworkElement element) => !double.IsNaN(element.Height) || !double.IsNaN(element.Width);
    }
}
