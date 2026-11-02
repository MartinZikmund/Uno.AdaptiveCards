// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Base class for input values (port of InputValue.cpp). Responsible for getting the current value at submit time
    /// and for input validation.
    /// </summary>
    internal abstract class InputValue : IAdaptiveInputValue
    {
        protected readonly IAdaptiveInputElement _adaptiveInputElement;
        protected readonly UIElement _uiInputElement;
        protected readonly Border _validationBorder;
        private UIElement _validationError;

        protected InputValue(IAdaptiveInputElement adaptiveInputElement, UIElement uiInputElement, Border validationBorder)
        {
            _adaptiveInputElement = adaptiveInputElement;
            _uiInputElement = uiInputElement;
            _validationBorder = validationBorder;
        }

        public IAdaptiveInputElement InputElement => _adaptiveInputElement;

        public UIElement ErrorMessage
        {
            get => _validationError;
            set => _validationError = value;
        }

        public abstract string CurrentValue { get; }

        public bool Validate()
        {
            var isValid = IsValueValid();
            SetValidation(isValid);
            return isValid;
        }

        public virtual void SetFocus()
        {
            if (_uiInputElement is Control control)
            {
                control.Focus(FocusState.Programmatic);
            }
        }

        internal void SetAccessibilityProperties(bool isInputValid)
        {
            if (_validationError is not DependencyObject validationError || _uiInputElement == null)
            {
                return;
            }

            try
            {
                AutomationProperties.SetLiveSetting(validationError, AutomationLiveSetting.Assertive);

                var describers = AutomationProperties.GetDescribedBy(_uiInputElement);
                if (describers != null)
                {
                    int index = describers.IndexOf(validationError);
                    bool found = index >= 0;
                    if (!isInputValid && !found)
                    {
                        describers.Add(validationError);
                        if (_validationError is FrameworkElement errorElement &&
                            FrameworkElementAutomationPeer.FromElement(errorElement) is AutomationPeer errorAutomationPeer)
                        {
                            errorAutomationPeer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
                        }
                    }
                    else if (isInputValid && found)
                    {
                        describers.RemoveAt(index);
                    }
                }

                AutomationProperties.SetIsDataValidForForm(_uiInputElement, isInputValid);
            }
            catch (NotImplementedException)
            {
                // Some automation APIs are not implemented on all Uno targets; accessibility is best-effort.
            }
        }

        protected virtual bool IsValueValid()
        {
            bool isRequiredValid = true;
            if (_adaptiveInputElement.IsRequired)
            {
                isRequiredValid = !string.IsNullOrEmpty(CurrentValue);
            }

            return isRequiredValid;
        }

        protected virtual void SetValidation(bool isInputValid)
        {
            if (_validationBorder != null)
            {
                _validationBorder.BorderThickness = isInputValid ? new Thickness(0) : new Thickness(1);
            }

            if (_validationError != null)
            {
                _validationError.Visibility = isInputValid ? Visibility.Collapsed : Visibility.Visible;
                SetAccessibilityProperties(isInputValid);
            }
        }

        internal static bool GetToggleValue(object element) => element is ToggleButton toggleButton && toggleButton.IsChecked == true;
    }

    /// <summary>Base class for Input.Text values (regex validation).</summary>
    internal abstract class TextInputBase : InputValue
    {
        protected readonly AdaptiveTextInput _adaptiveTextInput;

        protected TextInputBase(AdaptiveTextInput adaptiveTextInput, UIElement uiTextInputElement, Border validationBorder)
            : base(adaptiveTextInput, uiTextInputElement, validationBorder)
        {
            _adaptiveTextInput = adaptiveTextInput;
        }

        protected override bool IsValueValid()
        {
            if (!base.IsValueValid())
            {
                return false;
            }

            var regex = _adaptiveTextInput.Regex;
            var currentValue = CurrentValue;
            bool isRegexValid = true;
            if (!string.IsNullOrEmpty(regex) && !string.IsNullOrEmpty(currentValue))
            {
                try
                {
                    // std::regex_match requires the whole string to match.
                    isRegexValid = Regex.IsMatch(currentValue, @"\A(?:" + regex + @")\z", RegexOptions.CultureInvariant);
                }
                catch (ArgumentException)
                {
                    // Invalid pattern: std::regex would throw; treat the value as valid instead of failing the submit.
                    isRegexValid = true;
                }
            }

            return isRegexValid;
        }
    }

    internal sealed class TextInputValue : TextInputBase
    {
        private readonly TextBox _textBoxElement;

        public TextInputValue(AdaptiveTextInput adaptiveTextInput, TextBox uiTextBoxElement, Border validationBorder)
            : base(adaptiveTextInput, uiTextBoxElement, validationBorder)
        {
            _textBoxElement = uiTextBoxElement;
        }

        public override string CurrentValue => _textBoxElement.Text ?? string.Empty;
    }

    internal sealed class PasswordInputValue : TextInputBase
    {
        private readonly PasswordBox _passwordElement;

        public PasswordInputValue(AdaptiveTextInput adaptiveTextInput, PasswordBox uiPasswordElement, Border validationBorder)
            : base(adaptiveTextInput, uiPasswordElement, validationBorder)
        {
            _passwordElement = uiPasswordElement;
        }

        public override string CurrentValue => _passwordElement.Password ?? string.Empty;
    }

    internal sealed class NumberInputValue : InputValue
    {
        private static readonly Regex s_leadingDouble = new(
            @"^\s*[+-]?(?:(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?|inf(?:inity)?|nan)",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private readonly TextBox _textBoxElement;
        private readonly AdaptiveNumberInput _adaptiveNumberInput;

        public NumberInputValue(AdaptiveNumberInput adaptiveNumberInput, TextBox uiInputTextBoxElement, Border validationBorder)
            : base(adaptiveNumberInput, uiInputTextBoxElement, validationBorder)
        {
            _adaptiveNumberInput = adaptiveNumberInput;
            _textBoxElement = uiInputTextBoxElement;
        }

        public override string CurrentValue => _textBoxElement.Text ?? string.Empty;

        /// <summary>Equivalent of std::stod: parses the longest leading floating point prefix.</summary>
        internal static bool TryStod(string value, out double result)
        {
            result = 0;
            var match = s_leadingDouble.Match(value ?? string.Empty);
            if (!match.Success)
            {
                return false;
            }

            var text = match.Value.Trim();
            var lower = text.ToLowerInvariant().TrimStart('+', '-');
            bool negative = text.StartsWith("-", StringComparison.Ordinal);
            if (lower.StartsWith("inf", StringComparison.Ordinal))
            {
                result = negative ? double.NegativeInfinity : double.PositiveInfinity;
                return true;
            }

            if (lower == "nan")
            {
                result = double.NaN;
                return true;
            }

            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        protected override bool IsValueValid()
        {
            if (!base.IsValueValid())
            {
                return false;
            }

            var max = _adaptiveNumberInput.Max;
            var min = _adaptiveNumberInput.Min;
            var currentValue = CurrentValue;
            bool isValid = true;
            if (!string.IsNullOrEmpty(currentValue))
            {
                if (TryStod(currentValue, out var currentDouble))
                {
                    if (max.HasValue)
                    {
                        isValid &= currentDouble <= max.Value;
                    }

                    if (min.HasValue)
                    {
                        isValid &= currentDouble >= min.Value;
                    }
                }
                else
                {
                    isValid = false;
                }
            }

            return isValid;
        }
    }

    internal sealed class DateInputValue : InputValue
    {
        private readonly AdaptiveDateInput _adaptiveDateInput;
        private readonly CalendarDatePicker _datePickerElement;

        public DateInputValue(AdaptiveDateInput adaptiveDateInput, CalendarDatePicker uiDatePickerElement, Border validationBorder)
            : base(adaptiveDateInput, uiDatePickerElement, validationBorder)
        {
            _adaptiveDateInput = adaptiveDateInput;
            _datePickerElement = uiDatePickerElement;
        }

        public override string CurrentValue
        {
            get
            {
                var date = _datePickerElement.Date;
                if (date.HasValue)
                {
                    // Equivalent of DateTimeFormatter "{year.full}-{month.integer(2)}-{day.integer(2)}" (local time).
                    var local = date.Value.ToLocalTime();
                    return local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }

                return string.Empty;
            }
        }
    }

    internal sealed class TimeInputValue : InputValue
    {
        private readonly AdaptiveTimeInput _adaptiveTimeInput;
        private readonly TimePicker _timePickerElement;

        public TimeInputValue(AdaptiveTimeInput adaptiveTimeInput, TimePicker uiTimePickerElement, Border validationBorder)
            : base(adaptiveTimeInput, uiTimePickerElement, validationBorder)
        {
            _adaptiveTimeInput = adaptiveTimeInput;
            _timePickerElement = uiTimePickerElement;
        }

        public override string CurrentValue
        {
            get
            {
                var selected = _timePickerElement.SelectedTime;
                if (selected.HasValue)
                {
                    long totalMinutes = (long)selected.Value.TotalMinutes;
                    long hours = totalMinutes / 60;
                    long minutesPastTheHour = totalMinutes - (hours * 60);
                    return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", hours, minutesPastTheHour);
                }

                return string.Empty;
            }
        }

        protected override bool IsValueValid()
        {
            if (!base.IsValueValid())
            {
                return false;
            }

            bool isMaxMinValid = true;
            var selected = _timePickerElement.SelectedTime;
            if (selected.HasValue)
            {
                var currentTime = selected.Value;

                if (!string.IsNullOrEmpty(_adaptiveTimeInput.Min) &&
                    DateTimePreparser.TryParseSimpleTime(_adaptiveTimeInput.Min, out var minHours, out var minMinutes))
                {
                    var minTime = TimeSpan.FromMinutes((minHours * 60) + minMinutes);
                    isMaxMinValid &= currentTime >= minTime;
                }

                if (!string.IsNullOrEmpty(_adaptiveTimeInput.Max) &&
                    DateTimePreparser.TryParseSimpleTime(_adaptiveTimeInput.Max, out var maxHours, out var maxMinutes))
                {
                    var maxTime = TimeSpan.FromMinutes((maxHours * 60) + maxMinutes);
                    isMaxMinValid &= currentTime <= maxTime;
                }
            }

            return isMaxMinValid;
        }
    }

    internal sealed class ToggleInputValue : InputValue
    {
        private readonly AdaptiveToggleInput _adaptiveToggleInput;
        private readonly CheckBox _checkBoxElement;

        public ToggleInputValue(AdaptiveToggleInput adaptiveToggleInput, CheckBox uiCheckBoxElement, Border validationBorder)
            : base(adaptiveToggleInput, uiCheckBoxElement, validationBorder)
        {
            _adaptiveToggleInput = adaptiveToggleInput;
            _checkBoxElement = uiCheckBoxElement;
        }

        public override string CurrentValue =>
            GetToggleValue(_checkBoxElement) ? _adaptiveToggleInput.ValueOn : _adaptiveToggleInput.ValueOff;

        protected override bool IsValueValid()
        {
            bool meetsRequirement = true;
            if (_adaptiveInputElement.IsRequired)
            {
                meetsRequirement = GetToggleValue(_checkBoxElement);
            }

            return meetsRequirement;
        }
    }

    internal static class ChoiceSetInputValueHelpers
    {
        public static string GetChoiceValue(AdaptiveChoiceSetInput choiceInput, int selectedIndex)
        {
            if (selectedIndex >= 0 && selectedIndex < choiceInput.Choices.Count)
            {
                return choiceInput.Choices[selectedIndex].Value ?? string.Empty;
            }

            return string.Empty;
        }
    }

    internal sealed class CompactChoiceSetInputValue : InputValue
    {
        private readonly Selector _selectorElement;
        private readonly AdaptiveChoiceSetInput _adaptiveChoiceSetInput;

        public CompactChoiceSetInputValue(AdaptiveChoiceSetInput adaptiveChoiceSetInput, Selector choiceSetSelector, Border validationBorder)
            : base(adaptiveChoiceSetInput, choiceSetSelector, validationBorder)
        {
            _adaptiveChoiceSetInput = adaptiveChoiceSetInput;
            _selectorElement = choiceSetSelector;
        }

        public override string CurrentValue => ChoiceSetInputValueHelpers.GetChoiceValue(_adaptiveChoiceSetInput, _selectorElement.SelectedIndex);
    }

    internal sealed class ExpandedChoiceSetInputValue : InputValue
    {
        private readonly Panel _panelElement;
        private readonly AdaptiveChoiceSetInput _adaptiveChoiceSetInput;

        public ExpandedChoiceSetInputValue(AdaptiveChoiceSetInput adaptiveChoiceSetInput, Panel choiceSetPanelElement, Border validationBorder)
            : base(adaptiveChoiceSetInput, choiceSetPanelElement, validationBorder)
        {
            _adaptiveChoiceSetInput = adaptiveChoiceSetInput;
            _panelElement = choiceSetPanelElement;
        }

        public override string CurrentValue
        {
            get
            {
                var children = _panelElement.Children;
                if (_adaptiveChoiceSetInput.IsMultiSelect)
                {
                    var multiSelectValues = new StringBuilder();
                    for (int index = 0; index < children.Count; index++)
                    {
                        if (GetToggleValue(children[index]))
                        {
                            multiSelectValues.Append(ChoiceSetInputValueHelpers.GetChoiceValue(_adaptiveChoiceSetInput, index)).Append(',');
                        }
                    }

                    if (multiSelectValues.Length > 0)
                    {
                        multiSelectValues.Length--;
                    }

                    return multiSelectValues.ToString();
                }

                int selectedIndex = -1;
                for (int index = 0; index < children.Count; index++)
                {
                    if (GetToggleValue(children[index]))
                    {
                        selectedIndex = index;
                        break;
                    }
                }

                return ChoiceSetInputValueHelpers.GetChoiceValue(_adaptiveChoiceSetInput, selectedIndex);
            }
        }

        public override void SetFocus()
        {
            if (_panelElement.Children.Count > 0 && _panelElement.Children[0] is Control firstChoice)
            {
                firstChoice.Focus(FocusState.Programmatic);
            }
        }
    }

    internal sealed class FilteredChoiceSetInputValue : InputValue
    {
        private readonly AutoSuggestBox _autoSuggestBox;
        private readonly AdaptiveChoiceSetInput _adaptiveChoiceSetInput;

        public FilteredChoiceSetInputValue(AdaptiveChoiceSetInput adaptiveChoiceSetInput, AutoSuggestBox autoSuggestBox, Border validationBorder)
            : base(adaptiveChoiceSetInput, autoSuggestBox, validationBorder)
        {
            _adaptiveChoiceSetInput = adaptiveChoiceSetInput;
            _autoSuggestBox = autoSuggestBox;
        }

        public override string CurrentValue => GetSelectedChoice()?.Value ?? string.Empty;

        protected override bool IsValueValid()
        {
            var text = _autoSuggestBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return !_adaptiveInputElement.IsRequired;
            }

            return GetSelectedChoice() != null;
        }

        private AdaptiveChoiceInput GetSelectedChoice()
        {
            var text = (_autoSuggestBox.Text ?? string.Empty).ToLowerInvariant();
            foreach (var choice in _adaptiveChoiceSetInput.Choices)
            {
                if (text == (choice.Title ?? string.Empty).ToLowerInvariant())
                {
                    return choice;
                }
            }

            return null;
        }
    }
}
