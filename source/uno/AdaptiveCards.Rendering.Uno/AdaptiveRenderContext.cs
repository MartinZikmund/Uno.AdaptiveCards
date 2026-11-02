// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveRenderContext
    {
        private readonly WeakReference<RenderedAdaptiveCard> _renderResult;

        public AdaptiveRenderContext()
        {
        }

        internal AdaptiveRenderContext(
            AdaptiveHostConfig hostConfig,
            AdaptiveFeatureRegistration featureRegistration,
            AdaptiveElementRendererRegistration elementRenderers,
            AdaptiveActionRendererRegistration actionRenderers,
            AdaptiveCardResourceResolvers resourceResolvers,
            ResourceDictionary overrideStyles,
            ResourceDictionary defaultActionSentimentStyles,
            RenderedAdaptiveCard renderResult)
        {
            HostConfig = hostConfig;
            FeatureRegistration = featureRegistration;
            ElementRenderers = elementRenderers;
            ActionRenderers = actionRenderers;
            ResourceResolvers = resourceResolvers;
            OverrideStyles = overrideStyles;
            ActionInvoker = new AdaptiveActionInvoker(renderResult);
            MediaEventInvoker = new AdaptiveMediaEventInvoker(renderResult);
            _renderResult = new WeakReference<RenderedAdaptiveCard>(renderResult);
            DefaultActionSentimentDictionary = defaultActionSentimentStyles;

            if (renderResult.OriginatingCard is { } originatingCard)
            {
                Rtl = originatingCard.Rtl;
            }
        }

        public AdaptiveHostConfig HostConfig { get; }

        public AdaptiveFeatureRegistration FeatureRegistration { get; }

        public AdaptiveElementRendererRegistration ElementRenderers { get; }

        public AdaptiveActionRendererRegistration ActionRenderers { get; }

        public AdaptiveCardResourceResolvers ResourceResolvers { get; }

        public AdaptiveActionInvoker ActionInvoker { get; }

        public AdaptiveMediaEventInvoker MediaEventInvoker { get; }

        public ResourceDictionary OverrideStyles { get; }

        public AdaptiveInputs UserInputs => GetRenderResult().UserInputs;

        public bool? Rtl { get; set; }

        public TextStyle? TextStyle { get; set; }

        public HAlignment? HorizontalContentAlignment { get; set; }

        /// <summary>Source of dynamic choices for Data.Query choice sets (see <see cref="AdaptiveCardRenderer.ChoicesProvider"/>).</summary>
        public IAdaptiveChoicesProvider ChoicesProvider { get; internal set; }

        internal ResourceDictionary DefaultActionSentimentDictionary { get; }

        /// <summary>The XamlBuilder that owns this render (used for image load tracking).</summary>
        internal XamlBuilder XamlBuilder { get; set; }

        public void AddInputValue(IAdaptiveInputValue inputValue, AdaptiveRenderArgs renderArgs) =>
            TryGetRenderResult()?.AddInputValue(inputValue, renderArgs);

        public void LinkSubmitActionToCard(IAdaptiveActionElement submitAction, AdaptiveRenderArgs renderArgs) =>
            TryGetRenderResult()?.LinkActionToCard(submitAction, renderArgs);

        public void LinkCardToParent(AdaptiveCard card, AdaptiveRenderArgs renderArgs) =>
            TryGetRenderResult()?.LinkCardToParent(card, renderArgs);

        public IAdaptiveInputValue GetInputValue(IAdaptiveInputElement inputElement) =>
            TryGetRenderResult()?.GetInputValue(inputElement);

        public void AddError(ErrorStatusCode statusCode, string message) =>
            GetRenderResult().Errors.Add(new AdaptiveError(statusCode, message));

        public void AddWarning(WarningStatusCode statusCode, string message) =>
            GetRenderResult().Warnings.Add(new AdaptiveWarning(statusCode, message));

        internal void AddInlineShowCard(AdaptiveActionSet actionSet, AdaptiveShowCardAction showCardAction, UIElement showCardUIElement, AdaptiveRenderArgs renderArgs) =>
            GetRenderResult().AddInlineShowCard(actionSet, showCardAction, showCardUIElement, renderArgs);

        internal void AddInlineShowCard(AdaptiveCard adaptiveCard, AdaptiveShowCardAction showCardAction, UIElement showCardUIElement, AdaptiveRenderArgs renderArgs) =>
            GetRenderResult().AddInlineShowCard(adaptiveCard, showCardAction, showCardUIElement, renderArgs);

        internal void AddOverflowButton(AdaptiveActionSet actionSet, UIElement actionUIElement) =>
            GetRenderResult().AddOverflowButton(actionSet, actionUIElement);

        internal void AddOverflowButton(AdaptiveCard actionCard, UIElement actionUIElement) =>
            GetRenderResult().AddOverflowButton(actionCard, actionUIElement);

        internal RenderedAdaptiveCard TryGetRenderResult() =>
            _renderResult != null && _renderResult.TryGetTarget(out var result) ? result : null;

        internal RenderedAdaptiveCard GetRenderResult() =>
            TryGetRenderResult() ?? throw new InvalidOperationException(
                "RenderResult could not be resolved. Most likely, the object is no longer alive because all the references were destroyed.");
    }
}
