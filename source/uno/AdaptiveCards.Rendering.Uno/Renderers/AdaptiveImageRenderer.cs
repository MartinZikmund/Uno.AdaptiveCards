// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveImageRenderer : IAdaptiveElementRenderer
    {
        private readonly XamlBuilder _xamlBuilder;

        public AdaptiveImageRenderer()
        {
        }

        internal AdaptiveImageRenderer(XamlBuilder xamlBuilder)
        {
            _xamlBuilder = xamlBuilder;
        }

        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                // A default-constructed renderer (e.g. created by the host) uses the XamlBuilder of the current render.
                var builder = _xamlBuilder ?? renderContext.XamlBuilder ?? new XamlBuilder();
                return builder.BuildImage(cardElement, renderContext, renderArgs);
            }
            catch (Exception ex)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }
}
