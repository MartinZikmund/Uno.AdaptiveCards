// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveChoiceSetInputRenderer : IAdaptiveElementRenderer
    {
        public AdaptiveChoiceSetInputRenderer()
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
                        "ChoiceSet was stripped from card because interactivity is not supported");

                    // The C++ renderer warns but still renders the input; we strip it like every other input renderer.
                    return null;
                }

                if (cardElement is AdaptiveChoiceSetInput adaptiveChoiceSetInput)
                {
                    var choiceSetStyle = adaptiveChoiceSetInput.ChoiceSetStyle;
                    bool isMultiSelect = adaptiveChoiceSetInput.IsMultiSelect;

                    if (choiceSetStyle == ChoiceSetStyle.Compact && !isMultiSelect)
                    {
                        return BuildCompactChoiceSetInput(renderContext, renderArgs, adaptiveChoiceSetInput);
                    }
                    else if (choiceSetStyle == ChoiceSetStyle.Filtered)
                    {
                        return BuildFilteredChoiceSetInput(renderContext, renderArgs, adaptiveChoiceSetInput);
                    }
                    else
                    {
                        return BuildExpandedChoiceSetInput(renderContext, renderArgs, adaptiveChoiceSetInput, isMultiSelect);
                    }
                }
            }
            catch (Exception ex) when (ex is not PerformFallbackException)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }

            return null;
        }

        private static List<string> GetChoiceSetValueVector(AdaptiveChoiceSetInput adaptiveChoiceSetInput) =>
            (adaptiveChoiceSetInput.Value ?? string.Empty).Split(',').ToList();

        private static bool IsChoiceSelected(List<string> selectedValues, AdaptiveChoiceInput choice) =>
            selectedValues.Contains(choice.Value ?? string.Empty);

        private static UIElement BuildCompactChoiceSetInput(
            AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs, AdaptiveChoiceSetInput adaptiveChoiceSetInput)
        {
            var comboBox = new ComboBox
            {
                PlaceholderText = adaptiveChoiceSetInput.Placeholder ?? string.Empty,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            var values = GetChoiceSetValueVector(adaptiveChoiceSetInput);
            bool wrap = adaptiveChoiceSetInput.Wrap;

            int currentIndex = 0;
            int selectedIndex = -1;
            foreach (var adaptiveChoiceInput in adaptiveChoiceSetInput.Choices)
            {
                if (values.Count == 1 && IsChoiceSelected(values, adaptiveChoiceInput))
                {
                    selectedIndex = currentIndex;
                }

                comboBox.Items.Add(XamlHelpers.CreateTextBlockWithContent(adaptiveChoiceInput.Title, wrap));
                currentIndex++;
            }

            comboBox.SelectedIndex = selectedIndex;
            comboBox.Tapped += (sender, args) => args.Handled = true;

            XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Input.ChoiceSet.Compact", comboBox);

            var (inputLayout, validationBorder) =
                XamlHelpers.HandleInputLayoutAndValidation(adaptiveChoiceSetInput, comboBox, false, renderContext);

            var input = new CompactChoiceSetInputValue(adaptiveChoiceSetInput, comboBox, validationBorder);
            renderContext.AddInputValue(input, renderArgs);

            return inputLayout;
        }

        private static UIElement BuildExpandedChoiceSetInput(
            AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs, AdaptiveChoiceSetInput adaptiveChoiceSetInput, bool isMultiSelect)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };

            var values = GetChoiceSetValueVector(adaptiveChoiceSetInput);
            bool wrap = adaptiveChoiceSetInput.Wrap;

            foreach (var choice in adaptiveChoiceSetInput.Choices)
            {
                UIElement choiceItem;
                if (isMultiSelect)
                {
                    var checkBox = new CheckBox();
                    XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Input.Choice.Multiselect", checkBox);
                    checkBox.IsChecked = IsChoiceSelected(values, choice);
                    choiceItem = checkBox;
                }
                else
                {
                    var radioButton = new RadioButton { GroupName = adaptiveChoiceSetInput.Id };
                    XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Input.Choice.SingleSelect", radioButton);

                    if (values.Count == 1)
                    {
                        radioButton.IsChecked = IsChoiceSelected(values, choice);
                    }

                    radioButton.PreviewKeyDown += (sender, args) =>
                    {
                        var children = stackPanel.Children;
                        int size = children.Count;
                        int currentButtonIndex = children.IndexOf(radioButton);
                        if (currentButtonIndex >= 0 && size > 0)
                        {
                            if (args.Key == VirtualKey.Down)
                            {
                                var newButtonIndex = (currentButtonIndex + 1) % size;
                                if (children[newButtonIndex] is RadioButton next)
                                {
                                    next.IsChecked = true;
                                }
                            }
                            else if (args.Key == VirtualKey.Up)
                            {
                                var newButtonIndex = (currentButtonIndex + size - 1) % size;
                                if (children[newButtonIndex] is RadioButton previous)
                                {
                                    previous.IsChecked = true;
                                }
                            }
                        }
                    };

                    choiceItem = radioButton;
                }

                XamlHelpers.SetContent(choiceItem, choice.Title, wrap);
                XamlHelpers.AddHandledTappedEvent(choiceItem);
                XamlHelpers.AppendXamlElementToPanel(choiceItem, stackPanel);
            }

            XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Input.ChoiceSet.Expanded", stackPanel);

            var (inputLayout, _) = XamlHelpers.HandleInputLayoutAndValidation(adaptiveChoiceSetInput, stackPanel, false, renderContext, false);

            var input = new ExpandedChoiceSetInputValue(adaptiveChoiceSetInput, stackPanel, null);
            renderContext.AddInputValue(input, renderArgs);

            return inputLayout;
        }

        private static UIElement BuildFilteredChoiceSetInput(
            AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs, AdaptiveChoiceSetInput adaptiveChoiceSetInput)
        {
            var autoSuggestBox = new AutoSuggestBox();
            var choices = adaptiveChoiceSetInput.Choices;
            var values = GetChoiceSetValueVector(adaptiveChoiceSetInput);

            var choiceList = new List<string>();
            foreach (var choice in choices)
            {
                choiceList.Add(choice.Title);
                if (values.Count == 1 && IsChoiceSelected(values, choice))
                {
                    autoSuggestBox.Text = choice.Title ?? string.Empty;
                }
            }

            autoSuggestBox.ItemsSource = choiceList;

            autoSuggestBox.GotFocus += (sender, args) =>
            {
                if (sender is AutoSuggestBox box)
                {
                    box.IsSuggestionListOpen = true;
                }
            };

            List<string> StaticMatches(string currentText)
            {
                var currentResults = new List<string>();
                foreach (var choice in choices)
                {
                    var title = choice.Title ?? string.Empty;
                    if (title.ToLowerInvariant().Contains(currentText, StringComparison.Ordinal))
                    {
                        currentResults.Add(title);
                    }
                }

                return currentResults;
            }

            FilteredChoiceSetInputValue inputValue = null;
            var choicesProvider = renderContext.ChoicesProvider;
            bool isDynamic = choicesProvider != null &&
                adaptiveChoiceSetInput.ChoicesData?.ChoicesDataType == AdaptiveChoicesData.DataQueryType &&
                !string.IsNullOrEmpty(adaptiveChoiceSetInput.ChoicesData.Dataset);
            System.Threading.CancellationTokenSource pendingQuery = null;

            autoSuggestBox.TextChanged += async (sender, args) =>
            {
                var rawText = sender.Text ?? string.Empty;
                var currentText = rawText.ToLowerInvariant();
                var currentResults = StaticMatches(currentText);
                sender.ItemsSource = currentResults;

                // Dynamic typeahead (Adaptive Cards 1.6 Data.Query), an extension over the WinUI3 renderer.
                if (!isDynamic || args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen || string.IsNullOrEmpty(rawText))
                {
                    return;
                }

                pendingQuery?.Cancel();
                var cts = pendingQuery = new System.Threading.CancellationTokenSource();
                try
                {
                    await System.Threading.Tasks.Task.Delay(250, cts.Token);
                    var dynamicChoices = await choicesProvider.GetChoicesAsync(
                        new AdaptiveChoicesQuery(adaptiveChoiceSetInput, rawText), cts.Token);
                    if (cts.IsCancellationRequested || dynamicChoices == null)
                    {
                        return;
                    }

                    inputValue?.DynamicChoices.Clear();
                    inputValue?.DynamicChoices.AddRange(dynamicChoices);
                    var merged = new List<string>(currentResults);
                    foreach (var choice in dynamicChoices)
                    {
                        var title = choice.Title ?? string.Empty;
                        if (!merged.Contains(title))
                        {
                            merged.Add(title);
                        }
                    }

                    sender.ItemsSource = merged;
                    sender.IsSuggestionListOpen = merged.Count > 0;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    renderContext.TryGetRenderResult()?.Warnings.Add(
                        new AdaptiveWarning(WarningStatusCode.CustomWarning, "Dynamic choices query failed: " + ex.Message));
                }
            };

            var (inputLayout, validationBorder) =
                XamlHelpers.HandleInputLayoutAndValidation(adaptiveChoiceSetInput, autoSuggestBox, true, renderContext);

            var input = inputValue = new FilteredChoiceSetInputValue(adaptiveChoiceSetInput, autoSuggestBox, validationBorder);
            renderContext.AddInputValue(input, renderArgs);

            return inputLayout;
        }
    }
}
