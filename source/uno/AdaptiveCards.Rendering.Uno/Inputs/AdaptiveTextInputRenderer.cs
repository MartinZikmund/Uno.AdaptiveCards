// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveTextInputRenderer : IAdaptiveElementRenderer
    {
        public AdaptiveTextInputRenderer()
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
                        "Text Input was stripped from card because interactivity is not supported");
                    return null;
                }

                var adaptiveTextInput = (AdaptiveTextInput)cardElement;
                if (adaptiveTextInput.TextInputStyle == TextInputStyle.Password)
                {
                    return RenderPasswordBox(adaptiveTextInput, renderContext, renderArgs);
                }

                return RenderTextBox(adaptiveTextInput, renderContext, renderArgs);
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }

        private static (UIElement InputLayout, Border ValidationBorder) HandleLayoutAndValidation(
            AdaptiveTextInput adaptiveTextInput,
            UIElement inputUIElement,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs)
        {
            UIElement textBoxParentContainer = inputUIElement;
            var regex = adaptiveTextInput.Regex;
            bool isRequired = adaptiveTextInput.IsRequired;

            Border validationBorder = null;
            if (!string.IsNullOrEmpty(regex) || isRequired)
            {
                validationBorder = XamlHelpers.CreateValidationBorder(inputUIElement, renderContext);
                textBoxParentContainer = validationBorder;
            }

            var inlineAction = adaptiveTextInput.InlineAction;
            if (inlineAction != null)
            {
                bool isMultiline = adaptiveTextInput.IsMultiline && adaptiveTextInput.TextInputStyle != TextInputStyle.Password;
                var textBoxWithInlineAction = ActionHelpers.HandleInlineAction(
                    renderContext, renderArgs, inputUIElement, textBoxParentContainer, isMultiline, inlineAction);
                if (textBoxWithInlineAction != null)
                {
                    textBoxParentContainer = textBoxWithInlineAction;
                }
            }

            if (!adaptiveTextInput.IsMultiline && textBoxParentContainer is FrameworkElement textBoxFrameworkElement)
            {
                textBoxFrameworkElement.VerticalAlignment = VerticalAlignment.Top;
            }

            var (inputLayout, _) = XamlHelpers.HandleInputLayoutAndValidation(
                adaptiveTextInput, textBoxParentContainer, !string.IsNullOrEmpty(regex), renderContext, false);

            return (inputLayout, validationBorder);
        }

        private static UIElement RenderTextBox(AdaptiveTextInput adaptiveTextInput, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var textBox = new TextBox();
            if (adaptiveTextInput.IsMultiline)
            {
                textBox.AcceptsReturn = true;
                textBox.TextWrapping = TextWrapping.Wrap;
            }

            textBox.Text = adaptiveTextInput.Value ?? string.Empty;
            textBox.MaxLength = (int)Math.Min(adaptiveTextInput.MaxLength, int.MaxValue);
            textBox.PlaceholderText = adaptiveTextInput.Placeholder ?? string.Empty;

            var inputScopeName = new InputScopeName();
            switch (adaptiveTextInput.TextInputStyle)
            {
                case TextInputStyle.Email:
                    inputScopeName.NameValue = InputScopeNameValue.EmailSmtpAddress;
                    break;
                case TextInputStyle.Tel:
                    inputScopeName.NameValue = InputScopeNameValue.TelephoneNumber;
                    break;
                case TextInputStyle.Url:
                    inputScopeName.NameValue = InputScopeNameValue.Url;
                    break;
            }

            var inputScope = new InputScope();
            inputScope.Names.Add(inputScopeName);
            textBox.InputScope = inputScope;

            var (textInputControl, validationBorder) = HandleLayoutAndValidation(adaptiveTextInput, textBox, renderContext, renderArgs);

            var inputValue = new TextInputValue(adaptiveTextInput, textBox, validationBorder);
            textBox.TextChanged += (sender, args) => inputValue.Validate();

            renderContext.AddInputValue(inputValue, renderArgs);

            XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Input.Text", textBox);

            return textInputControl;
        }

        private static UIElement RenderPasswordBox(AdaptiveTextInput adaptiveTextInput, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var passwordBox = new PasswordBox
            {
                Password = adaptiveTextInput.Value ?? string.Empty,
                MaxLength = (int)Math.Min(adaptiveTextInput.MaxLength, int.MaxValue),
                PlaceholderText = adaptiveTextInput.Placeholder ?? string.Empty,
            };

            var (textInputControl, validationBorder) = HandleLayoutAndValidation(adaptiveTextInput, passwordBox, renderContext, renderArgs);

            var inputValue = new PasswordInputValue(adaptiveTextInput, passwordBox, validationBorder);
            renderContext.AddInputValue(inputValue, renderArgs);

            return textInputControl;
        }
    }
}
