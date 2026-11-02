// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class RenderedAdaptiveCard
    {
        private sealed class ShowCardInfo
        {
            public uint ActionSetId;
            public UIElement CardUIElement;
        }

        private readonly Dictionary<uint, ShowCardInfo> _showCards = new();
        private readonly Dictionary<uint, UIElement> _overflowButtons = new();
        private readonly AdaptiveInputs _inputs = new();

        public RenderedAdaptiveCard()
        {
        }

        internal RenderedAdaptiveCard(IList<AdaptiveError> errors, IList<AdaptiveWarning> warnings)
        {
            Errors = errors;
            Warnings = warnings;
        }

        public AdaptiveCard OriginatingCard { get; internal set; }

        public AdaptiveHostConfig OriginatingHostConfig { get; internal set; }

        public FrameworkElement FrameworkElement { get; internal set; }

        public AdaptiveInputs UserInputs => _inputs;

        public IList<AdaptiveError> Errors { get; } = new List<AdaptiveError>();

        public IList<AdaptiveWarning> Warnings { get; } = new List<AdaptiveWarning>();

        public event TypedEventHandler<RenderedAdaptiveCard, AdaptiveActionEventArgs> Action;

        public event TypedEventHandler<RenderedAdaptiveCard, AdaptiveMediaEventArgs> MediaClicked;

        private void HandleInlineShowCardEvent(IAdaptiveActionElement actionElement)
        {
            var showCardAction = (AdaptiveShowCardAction)actionElement;
            var showCardToToggle = showCardAction.InternalId;
            if (_showCards.TryGetValue(showCardToToggle, out var info))
            {
                if (info.CardUIElement.Visibility == Visibility.Visible)
                {
                    info.CardUIElement.Visibility = Visibility.Collapsed;
                }
                else
                {
                    info.CardUIElement.Visibility = Visibility.Visible;
                    foreach (var entry in _showCards)
                    {
                        if (entry.Value.ActionSetId == info.ActionSetId && entry.Key != showCardToToggle)
                        {
                            entry.Value.CardUIElement.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        private static void HandleToggleVisibilityClick(FrameworkElement cardFrameworkElement, IAdaptiveActionElement action)
        {
            var toggleAction = (AdaptiveToggleVisibilityAction)action;
            var parentPanels = new List<Panel>();

            foreach (var currentTarget in toggleAction.TargetElements)
            {
                var toggleId = currentTarget.ElementId;
                var toggle = currentTarget.IsVisible;
                if (cardFrameworkElement?.FindName(toggleId) is FrameworkElement toggleElement &&
                    toggleElement.Tag is ElementTagContent elementTagContent)
                {
                    var visibilityToSet = Visibility.Visible;
                    if (toggle == IsVisible.IsVisibleTrue)
                    {
                        visibilityToSet = Visibility.Visible;
                    }
                    else if (toggle == IsVisible.IsVisibleFalse)
                    {
                        visibilityToSet = Visibility.Collapsed;
                    }
                    else if (toggle == IsVisible.IsVisibleToggle)
                    {
                        visibilityToSet = elementTagContent.ExpectedVisibility ? Visibility.Collapsed : Visibility.Visible;
                    }

                    toggleElement.Visibility = visibilityToSet;
                    elementTagContent.ExpectedVisibility = visibilityToSet == Visibility.Visible;

                    parentPanels.Add(elementTagContent.ParentPanel);

                    if (elementTagContent.CardElement is AdaptiveColumn column)
                    {
                        XamlHelpers.HandleColumnWidth(column, visibilityToSet == Visibility.Visible, elementTagContent.ColumnDefinition);
                    }
                }
            }

            foreach (var parentPanel in parentPanels)
            {
                if (parentPanel != null)
                {
                    XamlHelpers.SetSeparatorVisibility(parentPanel);
                }
            }
        }

        internal void SendActionEvent(IAdaptiveActionElement actionElement)
        {
            switch (actionElement.ActionType)
            {
                case ActionType.ToggleVisibility:
                    HandleToggleVisibilityClick(FrameworkElement, actionElement);
                    return;

                case ActionType.ShowCard:
                {
                    bool handleInlineShowCard = OriginatingHostConfig?.Actions?.ShowCard?.ActionMode == ActionMode.Inline;
                    if (handleInlineShowCard)
                    {
                        handleInlineShowCard &= _showCards.ContainsKey(((AdaptiveShowCardAction)actionElement).InternalId);
                    }

                    if (handleInlineShowCard)
                    {
                        HandleInlineShowCardEvent(actionElement);
                    }
                    else
                    {
                        Action?.Invoke(this, new AdaptiveActionEventArgs(actionElement, null));
                    }

                    return;
                }

                case ActionType.Submit:
                case ActionType.Execute:
                {
                    var associatedInputs = actionElement is AdaptiveSubmitAction submit
                        ? submit.AssociatedInputs
                        : ((AdaptiveExecuteAction)actionElement).AssociatedInputs;

                    AdaptiveInputs gatheredInputs;
                    bool inputsAreValid;
                    if (associatedInputs == AssociatedInputs.None)
                    {
                        gatheredInputs = new AdaptiveInputs();
                        inputsAreValid = true;
                    }
                    else
                    {
                        gatheredInputs = UserInputs;
                        inputsAreValid = gatheredInputs.ValidateInputs(actionElement);
                    }

                    if (inputsAreValid)
                    {
                        Action?.Invoke(this, new AdaptiveActionEventArgs(actionElement, gatheredInputs));
                    }

                    return;
                }

                default:
                    Action?.Invoke(this, new AdaptiveActionEventArgs(actionElement, new AdaptiveInputs()));
                    return;
            }
        }

        internal void SendMediaClickedEvent(AdaptiveMedia mediaElement) =>
            MediaClicked?.Invoke(this, new AdaptiveMediaEventArgs(mediaElement));

        internal void AddInlineShowCard(AdaptiveActionSet actionSet, AdaptiveShowCardAction showCardAction, UIElement showCardUIElement, AdaptiveRenderArgs renderArgs) =>
            AddInlineShowCardHelper(actionSet.InternalId, showCardAction, showCardUIElement, renderArgs);

        internal void AddInlineShowCard(AdaptiveCard adaptiveCard, AdaptiveShowCardAction showCardAction, UIElement showCardUIElement, AdaptiveRenderArgs renderArgs) =>
            AddInlineShowCardHelper(adaptiveCard.InternalId, showCardAction, showCardUIElement, renderArgs);

        private void AddInlineShowCardHelper(uint actionSetId, AdaptiveShowCardAction showCardAction, UIElement showCardUIElement, AdaptiveRenderArgs renderArgs)
        {
            _showCards[showCardAction.InternalId] = new ShowCardInfo { ActionSetId = actionSetId, CardUIElement = showCardUIElement };
            LinkCardToParent(showCardAction.Card, renderArgs);
        }

        internal void AddOverflowButton(AdaptiveActionSet actionSet, UIElement actionUIElement) => _overflowButtons[actionSet.InternalId] = actionUIElement;

        internal void AddOverflowButton(AdaptiveCard actionCard, UIElement actionUIElement) => _overflowButtons[actionCard.InternalId] = actionUIElement;

        internal void AddInputValue(IAdaptiveInputValue inputValue, AdaptiveRenderArgs renderArgs) => _inputs.AddInputValue(inputValue, renderArgs);

        internal void LinkActionToCard(IAdaptiveActionElement submitAction, AdaptiveRenderArgs renderArgs) => _inputs.LinkSubmitActionToCard(submitAction, renderArgs);

        internal void LinkCardToParent(AdaptiveCard card, AdaptiveRenderArgs renderArgs)
        {
            if (card == null)
            {
                return;
            }

            uint parentCardId = renderArgs?.ParentCard?.InternalId ?? 0;
            _inputs.LinkCardToParent(card.InternalId, parentCardId);
        }

        internal IAdaptiveInputValue GetInputValue(IAdaptiveInputElement inputElement) => _inputs.GetInputValue(inputElement);
    }
}
