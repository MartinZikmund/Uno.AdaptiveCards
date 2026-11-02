// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Stored in the Tag of rendered elements; used for toggle visibility and separators.</summary>
    public sealed class ElementTagContent
    {
        private readonly WeakReference<Panel> _parentPanel;

        public ElementTagContent()
        {
        }

        internal ElementTagContent(
            IAdaptiveCardElement cardElement,
            Panel parentPanel,
            UIElement separator,
            ColumnDefinition columnDefinition,
            bool expectedVisibility,
            bool isStretchable)
        {
            CardElement = cardElement;
            _parentPanel = parentPanel != null ? new WeakReference<Panel>(parentPanel) : null;
            Separator = separator;
            ColumnDefinition = columnDefinition;
            ExpectedVisibility = expectedVisibility;
            IsStretchable = isStretchable;
        }

        public ColumnDefinition ColumnDefinition { get; }

        public IAdaptiveCardElement CardElement { get; }

        public UIElement Separator { get; }

        public Panel ParentPanel => _parentPanel != null && _parentPanel.TryGetTarget(out var panel) ? panel : null;

        public bool ExpectedVisibility { get; set; }

        public bool IsStretchable { get; set; }
    }
}
