// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>Base class of the built-in element parsers.</summary>
    public abstract class BuiltInElementParser : IAdaptiveElementParser
    {
        internal BuiltInElementParser()
        {
        }

        internal abstract IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json);

        public IAdaptiveCardElement FromJson(
            JsonObject inputJson,
            AdaptiveElementParserRegistration elementParsers,
            AdaptiveActionParserRegistration actionParsers,
            IList<AdaptiveWarning> warnings)
        {
            var context = new ParseContext(elementParsers, actionParsers);
            var result = context.ElementParsers.Deserialize(context, inputJson, null, this);
            if (warnings != null)
            {
                foreach (var w in context.Warnings)
                {
                    warnings.Add(w);
                }
            }

            return result;
        }
    }

    /// <summary>Base class of the built-in action parsers.</summary>
    public abstract class BuiltInActionParser : IAdaptiveActionParser
    {
        internal BuiltInActionParser()
        {
        }

        internal abstract IAdaptiveActionElement Deserialize(ParseContext context, JsonObject json);

        public IAdaptiveActionElement FromJson(
            JsonObject inputJson,
            AdaptiveElementParserRegistration elementParsers,
            AdaptiveActionParserRegistration actionParsers,
            IList<AdaptiveWarning> warnings)
        {
            var context = new ParseContext(elementParsers, actionParsers);
            var result = context.ActionParsers.Deserialize(context, inputJson, null, this);
            if (warnings != null)
            {
                foreach (var w in context.Warnings)
                {
                    warnings.Add(w);
                }
            }

            return result;
        }
    }

    public sealed class AdaptiveElementParserRegistration
    {
        private readonly Dictionary<string, IAdaptiveElementParser> _parsers = new(StringComparer.Ordinal);
        private readonly HashSet<string> _knownElements = new(StringComparer.Ordinal);
        private readonly bool _isInitializing = true;

        public AdaptiveElementParserRegistration()
        {
            Register("ActionSet", new AdaptiveActionSetParser());
            Register("Carousel", new AdaptiveCarouselParser());
            Register("CarouselPage", new AdaptiveCarouselPageParser());
            Register("Column", new AdaptiveColumnParser());
            Register("ColumnSet", new AdaptiveColumnSetParser());
            Register("Container", new AdaptiveContainerParser());
            Register("FactSet", new AdaptiveFactSetParser());
            Register("Image", new AdaptiveImageParser());
            Register("ImageSet", new AdaptiveImageSetParser());
            Register("Input.ChoiceSet", new AdaptiveChoiceSetInputParser());
            Register("Input.Date", new AdaptiveDateInputParser());
            Register("Input.Number", new AdaptiveNumberInputParser());
            Register("Input.Text", new AdaptiveTextInputParser());
            Register("Input.Time", new AdaptiveTimeInputParser());
            Register("Input.Toggle", new AdaptiveToggleInputParser());
            Register("Media", new AdaptiveMediaParser());
            Register("RichTextBlock", new AdaptiveRichTextBlockParser());
            Register("Table", new AdaptiveTableParser());
            Register("TextBlock", new AdaptiveTextBlockParser());
            _isInitializing = false;

            void Register(string type, IAdaptiveElementParser parser)
            {
                _knownElements.Add(type);
                _parsers[type] = parser;
            }
        }

        public void Set(string type, IAdaptiveElementParser parser)
        {
            if (!_isInitializing && _knownElements.Contains(type))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.UnsupportedParserOverride, "Overriding known element parsers is unsupported");
            }

            _parsers[type] = parser;
        }

        public IAdaptiveElementParser Get(string type) => type != null && _parsers.TryGetValue(type, out var parser) ? parser : null;

        public void Remove(string type)
        {
            if (_knownElements.Contains(type))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.UnsupportedParserOverride, "Overriding known element parsers is unsupported");
            }

            _parsers.Remove(type);
        }

        /// <summary>
        /// Port of BaseCardElementParserWrapper::Deserialize + SharedModelElementParser. Pushes/pops id state around the
        /// parser invocation. Falls back to <see cref="AdaptiveUnsupportedElement"/> when no parser is registered.
        /// </summary>
        internal IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json, string typeString, IAdaptiveElementParser parser = null)
        {
            typeString ??= ParseUtil.GetTypeAsString(json);
            parser ??= Get(typeString);

            var idProperty = ParseUtil.GetString(json, "id");
            var internalId = InternalId.Next();
            context.PushElement(idProperty, internalId);
            InternalId.EnterParse();

            IAdaptiveCardElement element;
            try
            {
            if (parser is BuiltInElementParser builtIn)
            {
                element = builtIn.Deserialize(context, json);
            }
            else if (parser != null)
            {
                var warnings = new List<AdaptiveWarning>();
                element = parser.FromJson((JsonObject)json.DeepClone(), this, context.ActionParsers, warnings);
                context.Warnings.AddRange(warnings);
            }
            else
            {
                element = AdaptiveUnsupportedElement.Deserialize(context, json);
            }
            }
            finally
            {
                InternalId.ExitParse();
            }

            context.PopElement();
            return element;
        }
    }

    public sealed class AdaptiveActionParserRegistration
    {
        private readonly Dictionary<string, IAdaptiveActionParser> _parsers = new(StringComparer.Ordinal);
        private readonly HashSet<string> _knownElements = new(StringComparer.Ordinal);
        private readonly bool _isInitializing = true;

        public AdaptiveActionParserRegistration()
        {
            Register("Action.Execute", new AdaptiveExecuteActionParser());
            Register("Action.OpenUrl", new AdaptiveOpenUrlActionParser());
            Register("Action.ShowCard", new AdaptiveShowCardActionParser());
            Register("Action.Submit", new AdaptiveSubmitActionParser());
            Register("Action.ToggleVisibility", new AdaptiveToggleVisibilityActionParser());
            _isInitializing = false;

            void Register(string type, IAdaptiveActionParser parser)
            {
                _knownElements.Add(type);
                _parsers[type] = parser;
            }
        }

        public void Set(string type, IAdaptiveActionParser parser)
        {
            if (!_isInitializing && _knownElements.Contains(type))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.UnsupportedParserOverride, "Overriding known action parsers is unsupported");
            }

            _parsers[type] = parser;
        }

        public IAdaptiveActionParser Get(string type) => type != null && _parsers.TryGetValue(type, out var parser) ? parser : null;

        public void Remove(string type)
        {
            if (_knownElements.Contains(type))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.UnsupportedParserOverride, "Overriding known action parsers is unsupported");
            }

            _parsers.Remove(type);
        }

        internal IAdaptiveActionElement Deserialize(ParseContext context, JsonObject json, string typeString, IAdaptiveActionParser parser = null)
        {
            typeString ??= ParseUtil.GetTypeAsString(json);
            parser ??= Get(typeString);

            var idProperty = ParseUtil.GetString(json, "id");
            var internalId = InternalId.Next();
            context.PushElement(idProperty, internalId);
            InternalId.EnterParse();

            IAdaptiveActionElement action;
            try
            {
            if (parser is BuiltInActionParser builtIn)
            {
                action = builtIn.Deserialize(context, json);
            }
            else if (parser != null)
            {
                var warnings = new List<AdaptiveWarning>();
                action = parser.FromJson((JsonObject)json.DeepClone(), context.ElementParsers, this, warnings);
                context.Warnings.AddRange(warnings);
            }
            else
            {
                action = AdaptiveUnsupportedAction.Deserialize(context, json);
            }
            }
            finally
            {
                InternalId.ExitParse();
            }

            context.PopElement();
            return action;
        }
    }
}
