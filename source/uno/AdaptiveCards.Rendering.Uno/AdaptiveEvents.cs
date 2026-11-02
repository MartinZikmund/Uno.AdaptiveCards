// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveActionEventArgs
    {
        internal AdaptiveActionEventArgs(IAdaptiveActionElement action, AdaptiveInputs inputs)
        {
            Action = action;
            Inputs = inputs;
        }

        public IAdaptiveActionElement Action { get; }

        public AdaptiveInputs Inputs { get; }
    }

    public sealed class AdaptiveMediaEventArgs
    {
        internal AdaptiveMediaEventArgs(AdaptiveMedia media)
        {
            Media = media;
        }

        public AdaptiveMedia Media { get; }
    }

    public sealed class AdaptiveActionInvoker
    {
        private readonly WeakReference<RenderedAdaptiveCard> _renderResult;

        public AdaptiveActionInvoker()
        {
        }

        public AdaptiveActionInvoker(RenderedAdaptiveCard renderResult)
        {
            _renderResult = new WeakReference<RenderedAdaptiveCard>(renderResult);
        }

        public void SendActionEvent(IAdaptiveActionElement actionElement)
        {
            if (_renderResult != null && _renderResult.TryGetTarget(out var renderResult))
            {
                renderResult.SendActionEvent(actionElement);
            }
        }
    }

    public sealed class AdaptiveMediaEventInvoker
    {
        private readonly WeakReference<RenderedAdaptiveCard> _renderResult;

        public AdaptiveMediaEventInvoker()
        {
        }

        public AdaptiveMediaEventInvoker(RenderedAdaptiveCard renderResult)
        {
            _renderResult = new WeakReference<RenderedAdaptiveCard>(renderResult);
        }

        public void SendMediaClickedEvent(AdaptiveMedia mediaElement)
        {
            if (_renderResult != null && _renderResult.TryGetTarget(out var renderResult))
            {
                renderResult.SendMediaClickedEvent(mediaElement);
            }
        }
    }
}
