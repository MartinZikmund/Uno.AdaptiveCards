// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveToggleInputRenderer : IAdaptiveElementRenderer
    {
        public AdaptiveToggleInputRenderer()
        {
        }

        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                if (!XamlHelpers.SupportsInteractivity(renderContext.HostConfig))
                {
                    renderContext.AddWarning(
                        WarningStatusCode.InteractivityNotSupported,
                        "Toggle Input was stripped from card because interactivity is not supported");
                    return null;
                }

                var adaptiveToggleInput = (AdaptiveToggleInput)cardElement;
                var checkBox = new CheckBox();

                XamlHelpers.SetContent(checkBox, adaptiveToggleInput.Title, adaptiveToggleInput.Wrap);

                checkBox.IsChecked = adaptiveToggleInput.ValueOn == adaptiveToggleInput.Value;
                checkBox.Tapped += (sender, args) => args.Handled = true;
                checkBox.VerticalAlignment = VerticalAlignment.Top;

                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Input.Toggle", checkBox);

                var (inputLayout, _) = XamlHelpers.HandleInputLayoutAndValidation(adaptiveToggleInput, checkBox, false, renderContext, false);

                var input = new ToggleInputValue(adaptiveToggleInput, checkBox, null);
                renderContext.AddInputValue(input, renderArgs);

                return inputLayout;
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }
}
