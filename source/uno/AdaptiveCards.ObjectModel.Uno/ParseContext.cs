// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>
    /// Port of the shared model's ParseContext. Tracks warnings, id collisions (see the comment in the shared
    /// ParseContext.cpp for the full algorithm), parental container styles and bleed state during parsing.
    /// </summary>
    internal sealed class ParseContext
    {
        private readonly List<(string Id, uint InternalId, bool IsFallback)> _idStack = new();
        private readonly List<KeyValuePair<string, uint>> _elementIds = new();
        private readonly List<ContainerStyle> _parentalContainerStyles = new();
        private readonly List<uint> _parentalPadding = new();
        private readonly List<BleedDirection> _parentalBleedDirection = new();
        private readonly HashSet<string> _prohibitedElementTypes = new();

        public ParseContext(AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers)
        {
            ElementParsers = elementParsers ?? new AdaptiveElementParserRegistration();
            ActionParsers = actionParsers ?? new AdaptiveActionParserRegistration();
        }

        public AdaptiveElementParserRegistration ElementParsers { get; }

        public AdaptiveActionParserRegistration ActionParsers { get; }

        public List<AdaptiveWarning> Warnings { get; } = new();

        public bool CanFallbackToAncestor { get; set; }

        public string Language { get; set; } = string.Empty;

        public void AddWarning(WarningStatusCode code, string message) => Warnings.Add(new AdaptiveWarning(code, message));

        public void PushElement(string idJsonProperty, uint internalId, bool isFallback = false)
        {
            if (internalId == InternalId.Invalid)
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Attemping to push an element on to the stack with an invalid ID");
            }

            _idStack.Add((idJsonProperty ?? string.Empty, internalId, isFallback));
        }

        public void PopElement()
        {
            var (elementId, elementInternalId, isFallback) = _idStack[_idStack.Count - 1];

            if (!string.IsNullOrEmpty(elementId))
            {
                bool haveCollision = false;
                var nearestFallbackId = GetNearestFallbackId(elementInternalId);

                foreach (var entry in _elementIds)
                {
                    if (entry.Key != elementId)
                    {
                        continue;
                    }

                    var entryFallbackId = entry.Value;

                    if (entryFallbackId == elementInternalId)
                    {
                        haveCollision = false;
                        break;
                    }

                    if (_idStack.Count >= 2)
                    {
                        var previousInStack = _idStack[_idStack.Count - 2];
                        if (previousInStack.InternalId == entryFallbackId)
                        {
                            break;
                        }
                    }

                    if (isFallback)
                    {
                        continue;
                    }

                    haveCollision = true;
                }

                if (haveCollision)
                {
                    throw new AdaptiveCardParseException(ErrorStatusCode.IdCollision, "Collision detected for id '" + elementId + "'");
                }

                if (!isFallback)
                {
                    _elementIds.Add(new KeyValuePair<string, uint>(elementId, nearestFallbackId));
                }
            }

            _idStack.RemoveAt(_idStack.Count - 1);
        }

        private uint GetNearestFallbackId(uint skipId)
        {
            for (int i = _idStack.Count - 1; i >= 0; i--)
            {
                if (_idStack[i].IsFallback && _idStack[i].InternalId != skipId)
                {
                    return _idStack[i].InternalId;
                }
            }

            return InternalId.Invalid;
        }

        public ContainerStyle GetParentalContainerStyle() =>
            _parentalContainerStyles.Count > 0 ? _parentalContainerStyles[_parentalContainerStyles.Count - 1] : ContainerStyle.Default;

        public void SetParentalContainerStyle(ContainerStyle style)
        {
            if (style != ContainerStyle.None)
            {
                _parentalContainerStyles.Add(style);
            }
        }

        public uint PaddingParentInternalId() =>
            _parentalPadding.Count > 0 ? _parentalPadding[_parentalPadding.Count - 1] : InternalId.Invalid;

        internal void SaveContextForStyledCollectionElement(AdaptiveStyledCollectionElement current)
        {
            if (current.Style != ContainerStyle.None)
            {
                _parentalContainerStyles.Add(current.Style);
            }

            if (current.HasPadding)
            {
                PushBleedDirection(BleedDirection.All);
                _parentalPadding.Add(current.InternalIdValue);
            }
        }

        internal void RestoreContextForStyledCollectionElement(AdaptiveStyledCollectionElement current)
        {
            if (_parentalContainerStyles.Count > 0 && current.Style != ContainerStyle.None)
            {
                _parentalContainerStyles.RemoveAt(_parentalContainerStyles.Count - 1);
            }

            if (current.HasPadding)
            {
                _parentalPadding.RemoveAt(_parentalPadding.Count - 1);
                PopBleedDirection();
            }
        }

        public BleedDirection GetBleedDirection() =>
            _parentalBleedDirection.Count > 0 ? _parentalBleedDirection[_parentalBleedDirection.Count - 1] : BleedDirection.All;

        public void PushBleedDirection(BleedDirection direction) => _parentalBleedDirection.Add(direction);

        public void PopBleedDirection() => _parentalBleedDirection.RemoveAt(_parentalBleedDirection.Count - 1);

        public void AddProhibitedElementType(IEnumerable<string> types)
        {
            foreach (var t in types)
            {
                _prohibitedElementTypes.Add(t);
            }
        }

        public void RemoveProhibitedElementType(IEnumerable<string> types)
        {
            foreach (var t in types)
            {
                _prohibitedElementTypes.Remove(t);
            }
        }

        public void ShouldParse(string typeString)
        {
            if (typeString != null && _prohibitedElementTypes.Contains(typeString))
            {
                throw new AdaptiveCardParseException(ErrorStatusCode.ProhibitedType, "type: " + typeString + " is prohibited");
            }
        }
    }
}
