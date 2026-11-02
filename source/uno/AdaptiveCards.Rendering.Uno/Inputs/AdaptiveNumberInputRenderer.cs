// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Globalization;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveNumberInputRenderer : IAdaptiveElementRenderer
    {
        public AdaptiveNumberInputRenderer()
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
                        "Number input was stripped from card because interactivity is not supported");
                    return null;
                }

                var adaptiveNumberInput = (AdaptiveNumberInput)cardElement;

                var textBox = new TextBox();
                var inputScope = new InputScope();
                inputScope.Names.Add(new InputScopeName(InputScopeNameValue.Number));
                textBox.InputScope = inputScope;

                if (adaptiveNumberInput.Value is double boxValue)
                {
                    // std::wstringstream with precision digits10 (15 significant digits).
                    textBox.Text = boxValue.ToString("G15", CultureInfo.InvariantCulture);
                }

                textBox.PlaceholderText = adaptiveNumberInput.Placeholder ?? string.Empty;
                textBox.VerticalAlignment = VerticalAlignment.Top;

                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Input.Number", textBox);

                bool hasMinMax = adaptiveNumberInput.Max.HasValue || adaptiveNumberInput.Min.HasValue;
                var (inputLayout, validationBorder) =
                    XamlHelpers.HandleInputLayoutAndValidation(adaptiveNumberInput, textBox, hasMinMax, renderContext);

                var input = new NumberInputValue(adaptiveNumberInput, textBox, validationBorder);
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
