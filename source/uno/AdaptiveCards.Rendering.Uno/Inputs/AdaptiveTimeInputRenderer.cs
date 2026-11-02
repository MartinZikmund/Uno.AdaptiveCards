// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveTimeInputRenderer : IAdaptiveElementRenderer
    {
        public AdaptiveTimeInputRenderer()
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
                        "Time Input was stripped from card because interactivity is not supported");
                    return null;
                }

                var timePicker = new TimePicker
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Input.Time", timePicker);

                var adaptiveTimeInput = (AdaptiveTimeInput)cardElement;

                if (DateTimePreparser.TryParseSimpleTime(adaptiveTimeInput.Value, out var hours, out var minutes))
                {
                    var initialTime = TimeSpan.FromMinutes((hours * 60) + minutes);
                    timePicker.Time = initialTime;
                    // In WinUI3, setting Time also sets SelectedTime; set it explicitly in case the platform does not sync them.
                    timePicker.SelectedTime = initialTime;
                }

                bool hasMinMax = !string.IsNullOrEmpty(adaptiveTimeInput.Max) || !string.IsNullOrEmpty(adaptiveTimeInput.Min);
                var (inputLayout, validationBorder) =
                    XamlHelpers.HandleInputLayoutAndValidation(adaptiveTimeInput, timePicker, hasMinMax, renderContext);

                var input = new TimeInputValue(adaptiveTimeInput, timePicker, validationBorder);
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
