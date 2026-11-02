// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Windows.Foundation.Collections;

namespace AdaptiveCards.Rendering.Uno
{
    public interface IAdaptiveInputValue
    {
        IAdaptiveInputElement InputElement { get; }

        string CurrentValue { get; }

        UIElement ErrorMessage { get; set; }

        bool Validate();

        void SetFocus();
    }

    public sealed class AdaptiveInputs
    {
        private readonly Dictionary<uint, List<string>> _inputsPerCard = new();
        private readonly Dictionary<string, IAdaptiveInputValue> _inputValues = new();
        private readonly Dictionary<uint, uint> _containerCardForAction = new();
        private readonly Dictionary<uint, uint> _parentCard = new();
        private List<IAdaptiveInputValue> _lastRetrievedValues = new();

        public JsonObject AsJson()
        {
            var json = new JsonObject();
            foreach (var inputValue in _lastRetrievedValues)
            {
                var id = ((IAdaptiveCardElement)inputValue.InputElement).Id;
                json[id] = inputValue.CurrentValue ?? string.Empty;
            }

            return json;
        }

        public ValueSet AsValueSet()
        {
            var valueSet = new ValueSet();
            foreach (var inputValue in _lastRetrievedValues)
            {
                var id = ((IAdaptiveCardElement)inputValue.InputElement).Id;
                valueSet[id] = inputValue.CurrentValue ?? string.Empty;
            }

            return valueSet;
        }

        /// <summary>Convenience accessor (not part of the WinUI3 projection) returning the retrieved values as a dictionary.</summary>
        public IReadOnlyDictionary<string, string> AsDictionary()
        {
            var result = new Dictionary<string, string>();
            foreach (var inputValue in _lastRetrievedValues)
            {
                result[((IAdaptiveCardElement)inputValue.InputElement).Id] = inputValue.CurrentValue ?? string.Empty;
            }

            return result;
        }

        public bool ValidateInputs(IAdaptiveActionElement submitAction)
        {
            bool allInputsValid = true;
            var inputsToValidate = GetInputsToValidate(submitAction);
            foreach (var inputValue in inputsToValidate)
            {
                bool currentInputValid = inputValue.Validate();
                if (allInputsValid && !currentInputValid)
                {
                    inputValue.SetFocus();
                }

                allInputsValid &= currentInputValid;
            }

            _lastRetrievedValues = allInputsValid ? inputsToValidate : new List<IAdaptiveInputValue>();
            return allInputsValid;
        }

        internal void AddInputValue(IAdaptiveInputValue inputValue, AdaptiveRenderArgs renderArgs)
        {
            var cardId = renderArgs.ParentCard?.InternalId ?? 0;
            var id = ((IAdaptiveCardElement)inputValue.InputElement).Id;
            if (!_inputsPerCard.TryGetValue(cardId, out var list))
            {
                _inputsPerCard[cardId] = list = new List<string>();
            }

            list.Add(id);
            _inputValues[id] = inputValue;
        }

        private static uint GetInternalIdFromAction(IAdaptiveActionElement action) => action switch
        {
            AdaptiveExecuteAction execute => execute.InternalId,
            AdaptiveSubmitAction submit => submit.InternalId,
            _ => 0,
        };

        internal void LinkSubmitActionToCard(IAdaptiveActionElement action, AdaptiveRenderArgs renderArgs)
        {
            _containerCardForAction[GetInternalIdFromAction(action)] = renderArgs.ParentCard?.InternalId ?? 0;
        }

        internal void LinkCardToParent(uint cardId, uint parentCardId) => _parentCard[cardId] = parentCardId;

        internal IAdaptiveInputValue GetInputValue(IAdaptiveInputElement inputElement)
        {
            var id = ((IAdaptiveCardElement)inputElement).Id;
            return id != null && _inputValues.TryGetValue(id, out var value) ? value : null;
        }

        private List<IAdaptiveInputValue> GetInputsToValidate(IAdaptiveActionElement action)
        {
            var inputsToValidate = new List<IAdaptiveInputValue>();
            var actionId = GetInternalIdFromAction(action);
            _containerCardForAction.TryGetValue(actionId, out var card);

            var visited = new HashSet<uint>();
            while (card != 0 && visited.Add(card))
            {
                if (_inputsPerCard.TryGetValue(card, out var inputsInCard))
                {
                    foreach (var inputId in inputsInCard)
                    {
                        if (_inputValues.TryGetValue(inputId, out var value))
                        {
                            inputsToValidate.Add(value);
                        }
                    }
                }

                if (!_parentCard.TryGetValue(card, out card))
                {
                    break;
                }
            }

            return inputsToValidate;
        }
    }
}
