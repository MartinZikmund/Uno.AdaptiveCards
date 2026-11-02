// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Thrown when an action has no renderer and no fallback (E_FALLBACK_NOT_FOUND).</summary>
    internal sealed class FallbackNotFoundException : Exception
    {
        public FallbackNotFoundException() : base("Fallback not found")
        {
        }
    }

    /// <summary>Port of ActionHelpers.cpp from the WinUI3 renderer.</summary>
    internal static class ActionHelpers
    {
        internal static Thickness GetButtonMargin(AdaptiveActionsConfig actionsConfig)
        {
            var buttonMargin = new Thickness();
            uint buttonSpacing = actionsConfig.ButtonSpacing;
            if (actionsConfig.ActionsOrientation == ActionsOrientation.Horizontal)
            {
                buttonMargin.Left = buttonMargin.Right = buttonSpacing / 2;
            }
            else
            {
                buttonMargin.Top = buttonMargin.Bottom = buttonSpacing / 2;
            }

            return buttonMargin;
        }

        internal static void SetTooltip(string toolTipText, DependencyObject tooltipTarget)
        {
            if (!string.IsNullOrEmpty(toolTipText))
            {
                var tooltipTextBlock = new TextBlock { Text = toolTipText };
                var toolTip = new ToolTip { Content = tooltipTextBlock };
                ToolTipService.SetToolTip(tooltipTarget, toolTip);
            }
        }

        internal static void SetAutomationNameAndDescription(DependencyObject dependencyObject, string name, string description)
        {
            AutomationProperties.SetName(dependencyObject, name ?? string.Empty);
            AutomationProperties.SetFullDescription(dependencyObject, description ?? string.Empty);
        }

        internal static void ArrangeButtonContent(
            string actionTitle,
            string actionIconUrl,
            string actionTooltip,
            string actionAccessibilityText,
            AdaptiveActionsConfig actionsConfig,
            AdaptiveRenderContext renderContext,
            ContainerStyle containerStyle,
            AdaptiveHostConfig hostConfig,
            bool allActionsHaveIcons,
            Button button)
        {
            string title = actionTitle ?? string.Empty;
            string iconUrl = actionIconUrl ?? string.Empty;
            string tooltip = actionTooltip ?? string.Empty;
            string accessibilityText = actionAccessibilityText ?? string.Empty;
            string name = string.Empty;
            string description = string.Empty;

            if (!string.IsNullOrEmpty(accessibilityText))
            {
                name = accessibilityText;
                description = tooltip;
            }
            else if (!string.IsNullOrEmpty(title))
            {
                name = title;
                description = tooltip;
            }
            else
            {
                name = tooltip;
            }

            if (string.IsNullOrEmpty(tooltip))
            {
                tooltip = title;
            }

            SetAutomationNameAndDescription(button, name, description);
            SetTooltip(tooltip, button);

            if (!string.IsNullOrEmpty(iconUrl))
            {
                var buttonContentsStackPanel = new StackPanel();
                var adaptiveImage = new AdaptiveImage { Url = iconUrl, HorizontalAlignment = HAlignment.Center };

                UIElement buttonIcon = null;
                var elementRenderer = renderContext.ElementRenderers.Get("Image");
                if (elementRenderer != null)
                {
                    buttonIcon = elementRenderer.Render(
                        adaptiveImage, renderContext, new AdaptiveRenderArgs(containerStyle, buttonContentsStackPanel, null));
                }

                if (buttonIcon == null)
                {
                    XamlHelpers.SetContent(button, title);
                    return;
                }

                var buttonText = new TextBlock
                {
                    Text = title,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                if (buttonIcon is FrameworkElement buttonIconAsFrameworkElement)
                {
                    buttonIconAsFrameworkElement.Height = actionsConfig.IconSize;
                }

                UIElement separator = null;
                if (actionsConfig.IconPlacement == IconPlacement.AboveTitle && allActionsHaveIcons)
                {
                    buttonContentsStackPanel.Orientation = Orientation.Vertical;
                }
                else
                {
                    buttonContentsStackPanel.Orientation = Orientation.Horizontal;
                    uint spacingSize = Util.GetSpacingSizeFromSpacing(hostConfig, Spacing.Default);
                    separator = XamlHelpers.CreateSeparator(renderContext, spacingSize, spacingSize, default(Windows.UI.Color), false);
                }

                XamlHelpers.AppendXamlElementToPanel(buttonIcon, buttonContentsStackPanel);
                if (separator != null)
                {
                    XamlHelpers.AppendXamlElementToPanel(separator, buttonContentsStackPanel);
                }

                XamlHelpers.AppendXamlElementToPanel(buttonText, buttonContentsStackPanel);
                button.Content = buttonContentsStackPanel;
            }
            else
            {
                XamlHelpers.SetContent(button, title);
            }
        }

        internal static void HandleActionStyling(
            IAdaptiveActionElement adaptiveActionElement,
            FrameworkElement buttonFrameworkElement,
            bool isOverflowActionButton,
            AdaptiveRenderContext renderContext)
        {
            string actionSentiment = adaptiveActionElement?.Style ?? string.Empty;
            var resourceDictionary = renderContext.OverrideStyles;

            if (isOverflowActionButton &&
                XamlHelpers.TryGetResourceFromResourceDictionaries(resourceDictionary, "Adaptive.Action.Overflow") is Style overflowActionStyle)
            {
                buttonFrameworkElement.Style = overflowActionStyle;
            }

            if (string.IsNullOrEmpty(actionSentiment) || actionSentiment == "default")
            {
                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Action", buttonFrameworkElement);
            }
            else if (actionSentiment == "positive")
            {
                if (XamlHelpers.TryGetResourceFromResourceDictionaries(resourceDictionary, "Adaptive.Action.Positive") is Style positiveActionStyle)
                {
                    buttonFrameworkElement.Style = positiveActionStyle;
                }
                else if (XamlHelpers.TryGetResourceFromResourceDictionaries(
                    renderContext.DefaultActionSentimentDictionary, "PositiveActionDefaultStyle") is Style positiveActionDefaultStyle)
                {
                    buttonFrameworkElement.Style = positiveActionDefaultStyle;
                }
            }
            else if (actionSentiment == "destructive")
            {
                if (XamlHelpers.TryGetResourceFromResourceDictionaries(resourceDictionary, "Adaptive.Action.Destructive") is Style destructiveActionStyle)
                {
                    buttonFrameworkElement.Style = destructiveActionStyle;
                }
                else if (XamlHelpers.TryGetResourceFromResourceDictionaries(
                    renderContext.DefaultActionSentimentDictionary, "DestructiveActionDefaultStyle") is Style destructiveActionDefaultStyle)
                {
                    buttonFrameworkElement.Style = destructiveActionDefaultStyle;
                }
            }
            else
            {
                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Action." + actionSentiment, buttonFrameworkElement);
            }
        }

        internal static UIElement BuildAction(
            IAdaptiveActionElement adaptiveActionElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            bool isOverflowActionButton)
        {
            var hostConfig = renderContext.HostConfig;
            var actionsConfig = hostConfig.Actions;

            var button = CreateButton(adaptiveActionElement);
            button.Margin = GetButtonMargin(actionsConfig);

            if (actionsConfig.ActionsOrientation == ActionsOrientation.Horizontal)
            {
                button.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            else
            {
                button.HorizontalAlignment = actionsConfig.ActionAlignment switch
                {
                    ActionAlignment.Center => HorizontalAlignment.Center,
                    ActionAlignment.Left => HorizontalAlignment.Left,
                    // Note: the WinUI3 renderer maps Right to Center here; kept for parity.
                    ActionAlignment.Right => HorizontalAlignment.Center,
                    _ => HorizontalAlignment.Stretch,
                };
            }

            var containerStyle = renderArgs.ContainerStyle;
            bool allowAboveTitleIconPlacement = renderArgs.AllowAboveTitleIconPlacement;

            string title;
            string iconUrl = string.Empty;
            string tooltip = string.Empty;
            string accessibilityText = string.Empty;

            if (isOverflowActionButton)
            {
                title = hostConfig.OverflowButtonText;
                accessibilityText = hostConfig.OverflowButtonAccessibilityText;
            }
            else
            {
                title = adaptiveActionElement.Title;
                iconUrl = adaptiveActionElement.IconUrl;
                tooltip = adaptiveActionElement.Tooltip;
            }

            ArrangeButtonContent(
                title, iconUrl, tooltip, accessibilityText, actionsConfig, renderContext, containerStyle, hostConfig, allowAboveTitleIconPlacement, button);

            if (adaptiveActionElement != null)
            {
                var actionInvoker = renderContext.ActionInvoker;
                button.Click += (s, e) => actionInvoker.SendActionEvent(adaptiveActionElement);
                button.IsEnabled = adaptiveActionElement.IsEnabled;
            }

            HandleActionStyling(adaptiveActionElement, button, isOverflowActionButton, renderContext);
            return button;
        }

        internal static bool WarnForInlineShowCard(AdaptiveRenderContext renderContext, IAdaptiveActionElement action, string warning)
        {
            if (action != null && action.ActionType == ActionType.ShowCard)
            {
                renderContext.AddWarning(WarningStatusCode.UnsupportedValue, warning);
                return true;
            }

            return false;
        }

        private static bool IsKeyDown(VirtualKey key)
        {
            try
            {
                var state = InputKeyboardSource.GetKeyStateForCurrentThread(key);
                return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            }
            catch (Exception)
            {
                // Not available on every Uno target; treat modifier as not pressed.
                return false;
            }
        }

        internal static void HandleKeydownForInlineAction(KeyRoutedEventArgs args, AdaptiveActionInvoker actionInvoker, IAdaptiveActionElement inlineAction)
        {
            if (args.Key == VirtualKey.Enter)
            {
                if (!IsKeyDown(VirtualKey.Shift) && !IsKeyDown(VirtualKey.Control))
                {
                    actionInvoker.SendActionEvent(inlineAction);
                    args.Handled = true;
                }
            }
        }

        internal static UIElement HandleInlineAction(
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            UIElement textInputUIElement,
            UIElement textBoxParentContainer,
            bool isMultilineTextBox,
            IAdaptiveActionElement inlineAction)
        {
            var hostConfig = renderContext.HostConfig;

            if (WarnForInlineShowCard(renderContext, inlineAction, "Inline ShowCard not supported for InlineAction"))
            {
                return null;
            }

            var actionType = inlineAction.ActionType;
            if (actionType == ActionType.Submit || actionType == ActionType.Execute)
            {
                renderContext.LinkSubmitActionToCard(inlineAction, renderArgs);
            }

            var xamlGrid = new Grid();
            var columnDefinitions = xamlGrid.ColumnDefinitions;

            columnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var textBoxContainerAsFrameworkElement = (FrameworkElement)textBoxParentContainer;
            Grid.SetColumn(textBoxContainerAsFrameworkElement, 0);
            XamlHelpers.AppendXamlElementToPanel(textBoxContainerAsFrameworkElement, xamlGrid);

            columnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Auto) });
            uint spacingSize = Util.GetSpacingSizeFromSpacing(hostConfig, Spacing.Default);
            var separator = XamlHelpers.CreateSeparator(renderContext, spacingSize, 0, default(Windows.UI.Color), false);
            if (separator is FrameworkElement separatorAsFrameworkElement)
            {
                Grid.SetColumn(separatorAsFrameworkElement, 1);
            }

            XamlHelpers.AppendXamlElementToPanel(separator, xamlGrid);

            columnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });

            var iconUrl = inlineAction.IconUrl;
            UIElement actionUIElement;
            if (!string.IsNullOrEmpty(iconUrl))
            {
                var imageRenderer = renderContext.ElementRenderers.Get("Image");
                var adaptiveImage = new AdaptiveImage { Url = iconUrl };
                actionUIElement = imageRenderer?.Render(adaptiveImage, renderContext, renderArgs);
            }
            else
            {
                var titleTextBlock = new TextBlock { Text = inlineAction.Title, VerticalAlignment = VerticalAlignment.Center };
                var titleGrid = new Grid();
                XamlHelpers.AppendXamlElementToPanel(titleTextBlock, titleGrid);
                actionUIElement = titleGrid;
            }

            textBoxContainerAsFrameworkElement.Loaded += (s, e) =>
            {
                if (actionUIElement is FrameworkElement actionFrameworkElement)
                {
                    actionFrameworkElement.Height = textBoxContainerAsFrameworkElement.ActualHeight;
                    actionFrameworkElement.Visibility = Visibility.Visible;
                }
            };

            var touchTargetUIElement = WrapInTouchTarget(
                null, actionUIElement, inlineAction, renderContext, renderArgs, false, "Adaptive.Input.Text.InlineAction", string.Empty,
                !string.IsNullOrEmpty(iconUrl));

            if (touchTargetUIElement is FrameworkElement touchTargetFrameworkElement)
            {
                touchTargetFrameworkElement.VerticalAlignment = VerticalAlignment.Bottom;
                Grid.SetColumn(touchTargetFrameworkElement, 2);
            }

            XamlHelpers.AppendXamlElementToPanel(touchTargetUIElement, xamlGrid);

            if (!isMultilineTextBox)
            {
                var actionInvoker = renderContext.ActionInvoker;
                textInputUIElement.KeyDown += (s, args) => HandleKeydownForInlineAction(args, actionInvoker, inlineAction);
            }

            return xamlGrid;
        }

        internal static UIElement WrapInTouchTarget(
            IAdaptiveCardElement adaptiveCardElement,
            UIElement elementToWrap,
            IAdaptiveActionElement action,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            bool fullWidth,
            string style,
            string altText,
            bool allowTitleAsTooltip)
        {
            var hostConfig = renderContext.HostConfig;

            if (WarnForInlineShowCard(renderContext, action, "Inline ShowCard not supported for SelectAction"))
            {
                // Was in error, so just return the element to wrap.
                return elementToWrap;
            }

            var button = CreateButton(action);
            button.Content = elementToWrap;

            if (hostConfig.AddSelectActionMargin)
            {
                uint cardPadding = 0;
                if (fullWidth)
                {
                    cardPadding = hostConfig.Spacing.Padding;
                }

                if (adaptiveCardElement != null)
                {
                    // We want the hit target to equally split the vertical space above and below the current item.
                    // However, all we know is the spacing of the current item, which only applies to the spacing above.
                    // We don't know what the spacing of the NEXT element will be, so we can't calculate the correct
                    // spacing below. For now, we'll simply assume the bottom spacing is the same as the top.
                    // NOTE: Only apply spacings (padding, margin) for adaptive card elements to avoid adding spacings
                    // to card-level selectAction.
                    var elementSpacing = adaptiveCardElement.Spacing;
                    uint spacingSize = Util.GetSpacingSizeFromSpacing(hostConfig, elementSpacing);
                    double topBottomPadding = spacingSize / 2.0;

                    // For button padding, we apply the cardPadding and topBottomPadding (and then we negate these in
                    // the margin)
                    button.Padding = new Thickness(cardPadding, topBottomPadding, cardPadding, topBottomPadding);

                    double negativeCardMargin = cardPadding * -1.0;
                    double negativeTopBottomMargin = topBottomPadding * -1.0;
                    button.Margin = new Thickness(negativeCardMargin, negativeTopBottomMargin, negativeCardMargin, negativeTopBottomMargin);
                }
            }

            XamlHelpers.SetStyleFromResourceDictionary(renderContext, style, button);

            string tooltip = string.Empty, name = string.Empty, description = string.Empty;
            if (action != null)
            {
                string title = action.Title ?? string.Empty;
                tooltip = action.Tooltip ?? string.Empty;
                if (!string.IsNullOrEmpty(title))
                {
                    name = title;
                    description = tooltip;
                    if (string.IsNullOrEmpty(tooltip) && allowTitleAsTooltip)
                    {
                        tooltip = title;
                    }
                }
                else
                {
                    name = tooltip;
                }

                button.IsEnabled = action.IsEnabled;
            }
            else if (!string.IsNullOrEmpty(altText))
            {
                name = altText;
                tooltip = altText;
            }

            SetAutomationNameAndDescription(button, name, description);
            SetTooltip(tooltip, button);

            if (action != null)
            {
                WireButtonClickToAction(button, action, renderContext, renderArgs);
            }

            return button;
        }

        internal static void WireButtonClickToAction(
            Button button, IAdaptiveActionElement action, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            // Note that this method currently doesn't support inline show card actions, it
            // assumes the caller won't call this method if inline show card is specified.
            renderContext.LinkSubmitActionToCard(action, renderArgs);
            var actionInvoker = renderContext.ActionInvoker;
            button.Click += (s, e) => actionInvoker.SendActionEvent(action);
        }

        internal static UIElement HandleSelectAction(
            IAdaptiveCardElement adaptiveCardElement,
            IAdaptiveActionElement selectAction,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            UIElement uiElement,
            bool supportsInteractivity,
            bool fullWidthTouchTarget)
        {
            if (selectAction != null && supportsInteractivity)
            {
                return WrapInTouchTarget(
                    adaptiveCardElement, uiElement, selectAction, renderContext, renderArgs, fullWidthTouchTarget, "Adaptive.SelectAction",
                    string.Empty, true);
            }

            if (selectAction != null)
            {
                renderContext.AddWarning(WarningStatusCode.InteractivityNotSupported, "SelectAction present, but Interactivity is not supported");
            }

            return uiElement;
        }

        internal static void BuildActions(
            AdaptiveCard adaptiveCard,
            IList<IAdaptiveActionElement> children,
            Panel bodyPanel,
            bool insertSeparator,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs)
        {
            var hostConfig = renderContext.HostConfig;
            var actionsConfig = hostConfig.Actions;

            // Create a separator between the body and the actions
            if (insertSeparator)
            {
                uint spacingSize = Util.GetSpacingSizeFromSpacing(hostConfig, actionsConfig.Spacing);
                var separator = XamlHelpers.CreateSeparator(renderContext, spacingSize, 0u, default(Windows.UI.Color));
                XamlHelpers.AppendXamlElementToPanel(separator, bodyPanel);
            }

            var actionSetControl = BuildActionSetHelper(adaptiveCard, null, children, renderContext, renderArgs);
            XamlHelpers.AppendXamlElementToPanel(actionSetControl, bodyPanel);
        }

        private static Button CreateFlyoutButton(AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            // Create an action button
            if (BuildAction(null, renderContext, renderArgs, true) is Button overflowButton)
            {
                // Create a menu flyout for the overflow button
                overflowButton.Flyout = new MenuFlyout();
                return overflowButton;
            }

            return null;
        }

        private static UIElement AddOverflowFlyoutItem(
            IAdaptiveActionElement action,
            Button overflowButton,
            AdaptiveCard adaptiveCard,
            AdaptiveActionSet adaptiveActionSet,
            Panel showCardPanel,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs)
        {
            // Get the flyout items vector
            var menuFlyout = (MenuFlyout)overflowButton.Flyout;
            var flyoutItems = menuFlyout.Items;

            // Create a flyout item
            var flyoutItem = new MenuFlyoutItem { Text = action.Title ?? string.Empty };
            SetTooltip(action.Tooltip, flyoutItem);
            flyoutItem.IsEnabled = action.IsEnabled;

            // Hook the menu item up to the action invoker
            var actionInvoker = renderContext.ActionInvoker;
            flyoutItem.Click += (s, e) => actionInvoker.SendActionEvent(action);

            if (action.ActionType == ActionType.ShowCard)
            {
                // Build the inline show card.
                BuildInlineShowCard(adaptiveCard, adaptiveActionSet, action, showCardPanel, renderContext, renderArgs);
            }
            else if (action.ActionType == ActionType.Submit || action.ActionType == ActionType.Execute)
            {
                // Overflowed submit actions still need to gather inputs from their card.
                renderContext.LinkSubmitActionToCard(action, renderArgs);
            }

            flyoutItems.Add(flyoutItem);
            return flyoutItem;
        }

        internal static void BuildInlineShowCard(
            AdaptiveCard adaptiveCard,
            AdaptiveActionSet adaptiveActionSet,
            IAdaptiveActionElement action,
            Panel showCardsPanel,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs)
        {
            var hostConfig = renderContext.HostConfig;
            var showCardActionConfig = hostConfig.Actions.ShowCard;
            if (showCardActionConfig.ActionMode == ActionMode.Inline)
            {
                var actionAsShowCardAction = (AdaptiveShowCardAction)action;
                var showCard = actionAsShowCardAction.Card;

                // Build the inline show card.
                var uiShowCard = AdaptiveShowCardActionRenderer.BuildShowCard(showCard, renderContext, renderArgs, adaptiveActionSet == null);
                XamlHelpers.AppendXamlElementToPanel(uiShowCard, showCardsPanel);

                if (uiShowCard != null)
                {
                    if (adaptiveActionSet != null)
                    {
                        renderContext.AddInlineShowCard(adaptiveActionSet, actionAsShowCardAction, uiShowCard, renderArgs);
                    }
                    else
                    {
                        renderContext.AddInlineShowCard(adaptiveCard, actionAsShowCardAction, uiShowCard, renderArgs);
                    }
                }
            }
        }

        private static UIElement CreateActionButtonInActionSet(
            AdaptiveCard adaptiveCard,
            AdaptiveActionSet adaptiveActionSet,
            IAdaptiveActionElement actionToCreate,
            uint columnIndex,
            Panel actionsPanel,
            Panel showCardPanel,
            IList<ColumnDefinition> columnDefinitions,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs)
        {
            // Render each action using the registered renderer
            var action = actionToCreate;
            var actionType = action.ActionType;
            var actionRegistration = renderContext.ActionRenderers;

            IAdaptiveActionRenderer renderer = null;
            while (renderer == null)
            {
                var actionTypeString = action.ActionTypeString;
                renderer = actionRegistration.Get(actionTypeString);

                // Actions with unmet requirements (not part of the WinUI3 projection) are treated as unsupported.
                if (renderer != null && action is AdaptiveActionElementBase actionBase &&
                    renderContext.FeatureRegistration != null &&
                    !ActionMeetsRequirements(actionBase.Requirements, renderContext.FeatureRegistration))
                {
                    renderer = null;
                }

                if (renderer == null)
                {
                    switch (action.FallbackType)
                    {
                        case FallbackType.Drop:
                            XamlHelpers.WarnForFallbackDrop(renderContext, actionTypeString);
                            return null;

                        case FallbackType.Content:
                            action = action.FallbackContent;
                            actionType = action.ActionType;
                            XamlHelpers.WarnForFallbackContentElement(renderContext, actionTypeString, action.ActionTypeString);
                            break; // Go again

                        default:
                            throw new FallbackNotFoundException();
                    }
                }
            }

            var actionControl = renderer.Render(action, renderContext, renderArgs);
            XamlHelpers.AppendXamlElementToPanel(actionControl, actionsPanel);

            if (columnDefinitions != null && actionControl is FrameworkElement actionFrameworkElement)
            {
                // If using the equal width columns, we'll add a column and assign the column
                columnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
                Grid.SetColumn(actionFrameworkElement, (int)columnIndex);
            }

            if (actionType == ActionType.ShowCard)
            {
                // Build the inline show card.
                BuildInlineShowCard(adaptiveCard, adaptiveActionSet, action, showCardPanel, renderContext, renderArgs);
            }

            return actionControl;
        }

        internal static UIElement BuildActionSetHelper(
            AdaptiveCard adaptiveCard,
            AdaptiveActionSet adaptiveActionSet,
            IList<IAdaptiveActionElement> children,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs)
        {
            var hostConfig = renderContext.HostConfig;
            var actionsConfig = hostConfig.Actions;
            var actionAlignment = actionsConfig.ActionAlignment;
            var actionsOrientation = actionsConfig.ActionsOrientation;

            // Declare the panel that will host the buttons
            Panel actionsPanel;
            IList<ColumnDefinition> columnDefinitions = null;

            if (actionAlignment == ActionAlignment.Stretch && actionsOrientation == ActionsOrientation.Horizontal)
            {
                // If stretch alignment and orientation is horizontal, we use a grid with equal column widths to achieve
                // stretch behavior. For vertical orientation, we'll still just use a stack panel since the concept of
                // stretching buttons height isn't really valid, especially when the height of cards are typically
                // dynamic.
                var actionsGrid = new Grid();
                columnDefinitions = actionsGrid.ColumnDefinitions;
                actionsPanel = actionsGrid;
            }
            else
            {
                // Create a stack panel for the action buttons
                var actionStackPanel = new StackPanel
                {
                    Orientation = actionsOrientation == ActionsOrientation.Horizontal ? Orientation.Horizontal : Orientation.Vertical,
                    HorizontalAlignment = actionAlignment switch
                    {
                        ActionAlignment.Center => HorizontalAlignment.Center,
                        ActionAlignment.Left => HorizontalAlignment.Left,
                        ActionAlignment.Right => HorizontalAlignment.Right,
                        _ => HorizontalAlignment.Stretch,
                    },
                };
                actionsPanel = actionStackPanel;
            }

            var buttonMargin = GetButtonMargin(actionsConfig);
            if (actionsOrientation == ActionsOrientation.Horizontal)
            {
                // Negate the spacing on the sides so the left and right buttons are flush on the side.
                // We do NOT remove the margin from the individual button itself, since that would cause
                // the equal columns stretch behavior to not have equal columns (since the first and last
                // button would be narrower without the same margins as its peers).
                actionsPanel.Margin = new Thickness(-buttonMargin.Left, 0, -buttonMargin.Right, 0);
            }
            else
            {
                // Negate the spacing on the top and bottom so the first and last buttons don't have extra padding
                actionsPanel.Margin = new Thickness(0, -buttonMargin.Top, 0, -buttonMargin.Bottom);
            }

            // Get the max actions and whether actions over the limit should be put in an overflow menu
            uint maxActions = actionsConfig.MaxActions;
            bool overflowMaxActions = hostConfig.OverflowMaxActions;

            var showCardsStackPanel = new StackPanel();
            Button overflowButton = null;
            uint currentButtonIndex = 0;

            foreach (var action in children)
            {
                var mode = action.Mode;
                if (string.IsNullOrEmpty(action.IconUrl))
                {
                    renderArgs.AllowAboveTitleIconPlacement = false;
                }

                if (currentButtonIndex < maxActions && mode == ObjectModel.Uno.ActionMode.Primary)
                {
                    // If we have fewer than the maximum number of actions and this action's mode is primary, make a
                    // button
                    try
                    {
                        CreateActionButtonInActionSet(
                            adaptiveCard, adaptiveActionSet, action, currentButtonIndex, actionsPanel, showCardsStackPanel, columnDefinitions,
                            renderContext, renderArgs);
                        currentButtonIndex++;
                    }
                    catch (FallbackNotFoundException)
                    {
                        // No renderer and no fallback; skip.
                    }
                }
                else if (currentButtonIndex >= maxActions && mode == ObjectModel.Uno.ActionMode.Primary && !overflowMaxActions)
                {
                    // If we have more primary actions than the max actions and we're not allowed to overflow them just
                    // emit a warning
                    renderContext.AddWarning(
                        WarningStatusCode.MaxActionsExceeded,
                        "Some actions were not rendered due to exceeding the maximum number of actions allowed");

                    // Note: the WinUI3 renderer returns S_OK (a null UIElement) here, which drops the whole action set.
                    return null;
                }
                else
                {
                    // If the action's mode is secondary or we're overflowing max actions, create a flyout item on the
                    // overflow menu
                    overflowButton ??= CreateFlyoutButton(renderContext, renderArgs);
                    AddOverflowFlyoutItem(action, overflowButton, adaptiveCard, adaptiveActionSet, showCardsStackPanel, renderContext, renderArgs);

                    // If this was supposed to be a primary action but it got overflowed due to max actions, add a warning
                    if (mode == ObjectModel.Uno.ActionMode.Primary)
                    {
                        renderContext.AddWarning(
                            WarningStatusCode.MaxActionsExceeded,
                            "Some actions were moved to an overflow menu due to exceeding the maximum number of actions allowed");
                    }
                }
            }

            // Lastly add the overflow button itself to the action panel
            if (overflowButton != null)
            {
                // If using equal width columns, add another column and assign the it to the overflow button
                if (columnDefinitions != null)
                {
                    columnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
                    Grid.SetColumn(overflowButton, (int)currentButtonIndex);
                }

                // Add the overflow button to the panel
                XamlHelpers.AppendXamlElementToPanel(overflowButton, actionsPanel);

                // Register the overflow button with the render context
                if (adaptiveActionSet != null)
                {
                    renderContext.AddOverflowButton(adaptiveActionSet, overflowButton);
                }
                else
                {
                    renderContext.AddOverflowButton(adaptiveCard, overflowButton);
                }
            }

            // Reset icon placement value
            renderArgs.AllowAboveTitleIconPlacement = false;

            XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Actions", actionsPanel);

            var actionPanel = new StackPanel();

            // Add buttons and show cards to panel
            XamlHelpers.AppendXamlElementToPanel(actionsPanel, actionPanel);
            XamlHelpers.AppendXamlElementToPanel(showCardsStackPanel, actionPanel);

            return actionPanel;
        }

        private static bool ActionMeetsRequirements(IList<AdaptiveRequirement> requirements, AdaptiveFeatureRegistration featureRegistration)
        {
            if (requirements == null)
            {
                return true;
            }

            foreach (var requirement in requirements)
            {
                var registrationVersion = featureRegistration.Get(requirement.Name);
                if (string.IsNullOrEmpty(registrationVersion))
                {
                    return false;
                }

                if (requirement.Version != "*" && registrationVersion != "*" &&
                    SemanticVersion.TryParse(requirement.Version, out var required) &&
                    SemanticVersion.TryParse(registrationVersion, out var registered) &&
                    registered < required)
                {
                    return false;
                }
            }

            return true;
        }

        internal static Button CreateButton(IAdaptiveActionElement action)
        {
            var button = new Button();
            if (action != null && action.Role != ActionRole.Button)
            {
                SetAutomationType(action.Role, button);
            }

            return button;
        }

        internal static void SetAutomationType(ActionRole actionRole, Button button)
        {
            var roleType = actionRole switch
            {
                ActionRole.Link => AutomationControlType.Hyperlink,
                ActionRole.Tab => AutomationControlType.Tab,
                ActionRole.Menu => AutomationControlType.Menu,
                ActionRole.MenuItem => AutomationControlType.MenuItem,
                _ => AutomationControlType.Button,
            };

            AutomationProperties.SetAutomationControlType(button, roleType);
        }
    }
}
