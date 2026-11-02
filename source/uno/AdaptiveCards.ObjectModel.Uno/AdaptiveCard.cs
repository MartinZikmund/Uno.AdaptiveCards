// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveCard : IAdaptiveElementWithRemoteResources
    {
        /// <summary>The maximum schema version supported by this object model/renderer.</summary>
        public const string RendererVersion = "1.6";

        private static readonly HashSet<string> s_knownProperties = new(StringComparer.Ordinal)
        {
            "type", "version", "body", "actions", "fallbackText", "backgroundImage", "refresh", "authentication", "minHeight",
            "speak", "lang", "verticalContentAlignment", "style", "selectAction", "height", "$schema", "rtl",
        };

        public AdaptiveCard()
        {
            InternalId = Uno.InternalId.Next();
        }

        public IList<IAdaptiveCardElement> Body { get; } = new List<IAdaptiveCardElement>();

        public IList<IAdaptiveActionElement> Actions { get; } = new List<IAdaptiveActionElement>();

        public ElementType ElementType => ElementType.AdaptiveCard;

        public uint InternalId { get; internal set; }

        public string Version { get; set; } = string.Empty;

        public string FallbackText { get; set; } = string.Empty;

        public AdaptiveBackgroundImage BackgroundImage { get; set; }

        public string Language { get; set; } = string.Empty;

        public string Speak { get; set; } = string.Empty;

        public ContainerStyle Style { get; set; } = ContainerStyle.None;

        public IAdaptiveActionElement SelectAction { get; set; }

        public VerticalContentAlignment VerticalContentAlignment { get; set; } = VerticalContentAlignment.Top;

        public HeightType Height { get; set; } = HeightType.Auto;

        public uint MinHeight { get; set; }

        public AdaptiveRefresh Refresh { get; set; }

        public AdaptiveAuthentication Authentication { get; set; }

        public bool? Rtl { get; set; }

        /// <summary>Properties on the card JSON not understood by the object model (not part of the WinUI3 projection).</summary>
        public JsonObject AdditionalProperties { get; set; } = new JsonObject();

        public JsonObject ToJson()
        {
            var root = (JsonObject)(AdditionalProperties?.DeepClone() ?? new JsonObject());
            root["type"] = "AdaptiveCard";
            root["version"] = string.IsNullOrEmpty(Version) ? "1.0" : Version;

            if (!string.IsNullOrEmpty(FallbackText))
            {
                root["fallbackText"] = FallbackText;
            }

            if (BackgroundImage != null && BackgroundImage.ShouldSerialize())
            {
                root["backgroundImage"] = BackgroundImage.ToJson();
            }

            if (Refresh != null && Refresh.ShouldSerialize())
            {
                root["refresh"] = Refresh.ToJson();
            }

            if (Authentication != null && Authentication.ShouldSerialize())
            {
                root["authentication"] = Authentication.ToJson();
            }

            if (!string.IsNullOrEmpty(Speak))
            {
                root["speak"] = Speak;
            }

            if (!string.IsNullOrEmpty(Language))
            {
                root["lang"] = Language;
            }

            if (Style != ContainerStyle.None)
            {
                root["style"] = EnumStrings.ContainerStyle.ToString(Style);
            }

            if (VerticalContentAlignment != VerticalContentAlignment.Top)
            {
                root["verticalContentAlignment"] = EnumStrings.VerticalContentAlignment.ToString(VerticalContentAlignment);
            }

            if (MinHeight != 0)
            {
                root["minHeight"] = MinHeight + "px";
            }

            if (Rtl.HasValue)
            {
                root["rtl"] = Rtl.Value;
            }

            if (Height != HeightType.Auto)
            {
                root["height"] = EnumStrings.HeightType.ToString(Height);
            }

            if (SelectAction != null)
            {
                root["selectAction"] = SelectAction.ToJson();
            }

            var body = new JsonArray();
            foreach (var element in Body)
            {
                body.Add((JsonNode)element.ToJson());
            }

            root["body"] = body;

            var actions = new JsonArray();
            foreach (var action in Actions)
            {
                actions.Add((JsonNode)action.ToJson());
            }

            root["actions"] = actions;
            return root;
        }

        public IReadOnlyList<AdaptiveRemoteResourceInformation> GetResourceInformation()
        {
            var list = new List<AdaptiveRemoteResourceInformation>();
            if (BackgroundImage != null)
            {
                list.Add(new AdaptiveRemoteResourceInformation(BackgroundImage.Url, "image"));
            }

            AdaptiveCardElementBase.GetResourceInformation(Body, list);
            AdaptiveActionElementBase.GetResourceInformation(Actions, list);
            return list;
        }

        public static AdaptiveCardParseResult FromJson(JsonObject adaptiveJson) => FromJson(adaptiveJson, null, null);

        public static AdaptiveCardParseResult FromJson(
            JsonObject adaptiveJson, AdaptiveElementParserRegistration elementRegistration, AdaptiveActionParserRegistration actionRegistration) =>
            Parse(() => (JsonObject)adaptiveJson?.DeepClone(), elementRegistration, actionRegistration);

        public static AdaptiveCardParseResult FromJsonString(string adaptiveJson) => FromJsonString(adaptiveJson, null, null);

        public static AdaptiveCardParseResult FromJsonString(
            string adaptiveJson, AdaptiveElementParserRegistration elementRegistration, AdaptiveActionParserRegistration actionRegistration) =>
            Parse(() => ParseUtil.ParseJsonString(adaptiveJson), elementRegistration, actionRegistration);

        private static AdaptiveCardParseResult Parse(
            Func<JsonNode> getJson, AdaptiveElementParserRegistration elementRegistration, AdaptiveActionParserRegistration actionRegistration)
        {
            var result = new AdaptiveCardParseResult();
            try
            {
                var context = new ParseContext(elementRegistration, actionRegistration);
                result.AdaptiveCard = Deserialize(getJson(), RendererVersion, context);
                foreach (var warning in context.Warnings)
                {
                    result.Warnings.Add(warning);
                }
            }
            catch (AdaptiveCardParseException e)
            {
                result.Errors.Add(new AdaptiveError(e.StatusCode, e.Message));
            }

            return result;
        }

        private static void ValidateLanguage(string language, ParseContext context)
        {
            if (string.IsNullOrEmpty(language))
            {
                return;
            }

            bool valid = false;
            if (language.Length == 2 || language.Length == 3)
            {
                try
                {
                    var culture = CultureInfo.GetCultureInfo(language, predefinedOnly: true);
                    valid = culture != null;
                }
                catch (CultureNotFoundException)
                {
                    valid = false;
                }
            }

            if (!valid)
            {
                context.AddWarning(WarningStatusCode.InvalidLanguage, "Invalid language identifier: " + language);
            }
        }

        /// <summary>Port of AdaptiveCard::Deserialize from the shared model.</summary>
        internal static AdaptiveCard Deserialize(JsonNode jsonNode, string rendererVersion, ParseContext context)
        {
            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            bool enforceVersion = !string.IsNullOrEmpty(rendererVersion);

            ParseUtil.ExpectTypeString(json, "AdaptiveCard");

            var version = ParseUtil.GetString(json, "version", enforceVersion);
            var fallbackText = ParseUtil.GetString(json, "fallbackText");
            var language = ParseUtil.GetString(json, "lang");
            var speak = ParseUtil.GetString(json, "speak");

            ValidateLanguage(language, context);

            if (!string.IsNullOrEmpty(language))
            {
                context.Language = language;
            }
            else
            {
                language = context.Language;
            }

            if (enforceVersion)
            {
                var rendererMaxVersion = new SemanticVersion(rendererVersion);
                var cardVersion = new SemanticVersion(version);
                if (rendererMaxVersion < cardVersion)
                {
                    if (string.IsNullOrEmpty(fallbackText))
                    {
                        fallbackText = "We're sorry, this card couldn't be displayed";
                    }

                    if (string.IsNullOrEmpty(speak))
                    {
                        speak = fallbackText;
                    }

                    context.AddWarning(WarningStatusCode.UnsupportedSchemaVersion, "Schema version not supported");
                    return MakeFallbackTextCard(fallbackText, language, speak);
                }
            }

            var card = new AdaptiveCard
            {
                Version = version,
                FallbackText = fallbackText,
                Speak = speak,
                Language = language,
            };

            card.BackgroundImage = AdaptiveBackgroundImage.Deserialize(ParseUtil.ExtractJsonValue(json, "backgroundImage"));
            card.Refresh = AdaptiveRefresh.Deserialize(context, ParseUtil.ExtractJsonValue(json, "refresh"));
            card.Authentication = AdaptiveAuthentication.Deserialize(context, ParseUtil.ExtractJsonValue(json, "authentication"));

            card.Style = ParseUtil.GetEnumValue(json, "style", ContainerStyle.None, EnumStrings.ContainerStyle);
            context.SetParentalContainerStyle(card.Style);

            card.VerticalContentAlignment =
                ParseUtil.GetEnumValue(json, "verticalContentAlignment", VerticalContentAlignment.Top, EnumStrings.VerticalContentAlignment);
            card.Height = ParseUtil.GetEnumValue(json, "height", HeightType.Auto, EnumStrings.HeightType);
            card.MinHeight = (uint)(ParseUtil.ParseSizeForPixelSize(ParseUtil.GetString(json, "minHeight"), context) ?? 0);

            foreach (var element in ParseUtil.GetElementCollection(true, context, json, "body", false))
            {
                card.Body.Add(element);
            }

            var actions = ParseUtil.GetActionCollection(context, json, "actions", false);
            ParseUtil.EnsureShowCardVersions(actions, version);
            foreach (var action in actions)
            {
                card.Actions.Add(action);
            }

            card.Rtl = ParseUtil.GetOptionalBool(json, "rtl");
            card.SelectAction = ParseUtil.GetAction(context, json, "selectAction", false);
            card.AdditionalProperties = ParseUtil.HandleUnknownProperties(json, s_knownProperties);
            return card;
        }

        internal static AdaptiveCard MakeFallbackTextCard(string fallbackText, string language, string speak)
        {
            var card = new AdaptiveCard
            {
                Version = "1.0",
                FallbackText = fallbackText,
                Style = ContainerStyle.Default,
                Speak = speak,
                Language = language,
            };

            card.Body.Add(new AdaptiveTextBlock { Text = fallbackText, Language = language });
            return card;
        }
    }
}
