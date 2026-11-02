// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveOpenUrlActionRenderer : IAdaptiveActionRenderer
    {
        public UIElement Render(IAdaptiveActionElement action, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                return ActionHelpers.BuildAction(action, renderContext, renderArgs, false);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, action.ActionTypeString, ex.Message);
                return null;
            }
        }
    }

    public sealed class AdaptiveShowCardActionRenderer : IAdaptiveActionRenderer
    {
        public UIElement Render(IAdaptiveActionElement action, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                return ActionHelpers.BuildAction(action, renderContext, renderArgs, false);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, action.ActionTypeString, ex.Message);
                return null;
            }
        }

        internal static UIElement BuildShowCard(
            AdaptiveCard showCard, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs, bool isBottomActionBar)
        {
            try
            {
                var hostConfig = renderContext.HostConfig;
                var showCardActionConfig = hostConfig.Actions.ShowCard;
                var showCardConfigStyle = showCardActionConfig.Style;

                bool wasInShowCard = renderArgs.IsInShowCard;
                renderArgs.IsInShowCard = true;

                renderContext.LinkCardToParent(showCard, renderArgs);
                var localUiShowCard = XamlBuilder.BuildXamlTreeFromAdaptiveCard(showCard, renderContext, null, showCardConfigStyle);
                renderArgs.IsInShowCard = wasInShowCard;

                if (localUiShowCard == null)
                {
                    return null;
                }

                uint padding = hostConfig.Spacing.Padding;

                // Set the top margin
                uint inlineTopMargin = showCardActionConfig.InlineTopMargin;

                double negativePadding = -(double)padding;
                double sideMargin = negativePadding;
                double topMargin = isBottomActionBar ? inlineTopMargin + padding : inlineTopMargin;
                double bottomMargin = negativePadding;

                localUiShowCard.Margin = new Thickness(sideMargin, topMargin, sideMargin, bottomMargin);

                // Set the visibility as Collapsed until the action is triggered
                localUiShowCard.Visibility = Visibility.Collapsed;
                return localUiShowCard;
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                renderContext.AddError(ErrorStatusCode.CustomError, "Show Card building failed with the following message:\n" + ex.Message);
                return null;
            }
        }
    }

    public sealed class AdaptiveSubmitActionRenderer : IAdaptiveActionRenderer
    {
        public UIElement Render(IAdaptiveActionElement action, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                renderContext.LinkSubmitActionToCard(action, renderArgs);
                return ActionHelpers.BuildAction(action, renderContext, renderArgs, false);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, action.ActionTypeString, ex.Message);
                return null;
            }
        }
    }

    public sealed class AdaptiveExecuteActionRenderer : IAdaptiveActionRenderer
    {
        public UIElement Render(IAdaptiveActionElement action, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                renderContext.LinkSubmitActionToCard(action, renderArgs);
                return ActionHelpers.BuildAction(action, renderContext, renderArgs, false);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, action.ActionTypeString, ex.Message);
                return null;
            }
        }
    }

    public sealed class AdaptiveToggleVisibilityActionRenderer : IAdaptiveActionRenderer
    {
        public UIElement Render(IAdaptiveActionElement action, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                return ActionHelpers.BuildAction(action, renderContext, renderArgs, false);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, action.ActionTypeString, ex.Message);
                return null;
            }
        }
    }

    public sealed class AdaptiveActionSetRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var hostConfig = renderContext.HostConfig;
                if (!XamlHelpers.SupportsInteractivity(hostConfig))
                {
                    renderContext.AddWarning(
                        WarningStatusCode.InteractivityNotSupported, "ActionSet was stripped from card because interactivity is not supported");
                    return null;
                }

                var adaptiveActionSet = (AdaptiveActionSet)cardElement;
                return ActionHelpers.BuildActionSetHelper(null, adaptiveActionSet, adaptiveActionSet.Actions, renderContext, renderArgs);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }
}
