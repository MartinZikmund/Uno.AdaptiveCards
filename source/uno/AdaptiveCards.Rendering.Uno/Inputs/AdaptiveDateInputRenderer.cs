// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveDateInputRenderer : IAdaptiveElementRenderer
    {
        public AdaptiveDateInputRenderer()
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
                        "Date input was stripped from card because interactivity is not supported");
                    return null;
                }

                var adaptiveDateInput = (AdaptiveDateInput)cardElement;

                var datePicker = new CalendarDatePicker
                {
                    PlaceholderText = adaptiveDateInput.Placeholder ?? string.Empty,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                if (DateTimePreparser.TryParseSimpleDate(adaptiveDateInput.Value, out var year, out var month, out var day) &&
                    TryGetDateTime(year, month, day, out var value))
                {
                    datePicker.Date = value;
                }

                DateTimeOffset minDate = default;
                bool isMinValid = DateTimePreparser.TryParseSimpleDate(adaptiveDateInput.Min, out year, out month, out day) &&
                    TryGetDateTime(year, month, day, out minDate);
                if (isMinValid)
                {
                    datePicker.MinDate = minDate;
                }

                if (DateTimePreparser.TryParseSimpleDate(adaptiveDateInput.Max, out year, out month, out day) &&
                    TryGetDateTime(year, month, day, out var maxDate))
                {
                    if (isMinValid)
                    {
                        if (maxDate > minDate)
                        {
                            datePicker.MaxDate = maxDate;
                        }
                        else
                        {
                            renderContext.AddWarning(WarningStatusCode.InvalidValue, "Min value must be less than max in Input.Date");
                        }
                    }
                    else
                    {
                        datePicker.MaxDate = maxDate;
                    }
                }

                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Input.Date", datePicker);

                var (inputLayout, validationBorder) =
                    XamlHelpers.HandleInputLayoutAndValidation(adaptiveDateInput, datePicker, false, renderContext);

                var input = new DateInputValue(adaptiveDateInput, datePicker, validationBorder);
                renderContext.AddInputValue(input, renderArgs);

                return inputLayout;
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }

        /// <summary>Equivalent of Util's GetDateTime: local midnight of the given date.</summary>
        private static bool TryGetDateTime(int year, int month, int day, out DateTimeOffset result)
        {
            try
            {
                result = new DateTimeOffset(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local));
                return true;
            }
            catch (ArgumentException)
            {
                result = default;
                return false;
            }
        }
    }
}
