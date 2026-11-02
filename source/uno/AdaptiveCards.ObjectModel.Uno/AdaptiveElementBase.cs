// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>
    /// Shared implementation of the BaseElement part of the shared model (id, fallback, requires, additional properties).
    /// </summary>
    internal static class ElementBaseHelpers
    {
        private static readonly ConcurrentDictionary<Type, HashSet<string>> s_knownProperties = new();

        public static HashSet<string> GetKnownProperties(Type type, Action<HashSet<string>> populate) =>
            s_knownProperties.GetOrAdd(type, _ =>
            {
                var set = new HashSet<string>(StringComparer.Ordinal) { "fallback", "id", "requires", "type" };
                populate(set);
                return set;
            });

        public static void ParseRequires(JsonObject json, IList<AdaptiveRequirement> requirements)
        {
            var requiresValue = ParseUtil.ExtractJsonValue(json, "requires");
            if (requiresValue == null)
            {
                return;
            }

            if (requiresValue is JsonObject requiresObject)
            {
                foreach (var kvp in requiresObject)
                {
                    var memberValue = ParseUtil.AsString(kvp.Value);
                    if (memberValue == "*")
                    {
                        requirements.Add(new AdaptiveRequirement(kvp.Key, "0"));
                    }
                    else
                    {
                        if (!SemanticVersion.TryParse(memberValue, out _))
                        {
                            throw new AdaptiveCardParseException(
                                ErrorStatusCode.InvalidPropertyValue, "Invalid version in requires value: '" + memberValue + "'");
                        }

                        requirements.Add(new AdaptiveRequirement(kvp.Key, memberValue));
                    }
                }

                return;
            }

            throw new AdaptiveCardParseException(ErrorStatusCode.InvalidPropertyValue, "Invalid value for requires (should be object)");
        }

        /// <summary>Parses the fallback property. Returns the fallback content (or null).</summary>
        public static FallbackType ParseFallback<T>(
            ParseContext context, JsonObject json, string id, uint internalId, Func<ParseContext, JsonNode, T> parse, out T fallbackContent)
            where T : class
        {
            fallbackContent = null;
            var fallbackValue = ParseUtil.ExtractJsonValue(json, "fallback");
            if (ParseUtil.IsEmpty(fallbackValue))
            {
                return FallbackType.None;
            }

            if (ParseUtil.IsString(fallbackValue))
            {
                if (ParseUtil.ToLowercase(fallbackValue.GetValue<string>()) == "drop")
                {
                    return FallbackType.Drop;
                }

                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "The only valid string value for the fallback property is 'drop'.");
            }

            if (fallbackValue is JsonObject)
            {
                context.PushElement(id, internalId, true);
                var fallbackElement = parse(context, fallbackValue);
                context.PopElement();

                if (fallbackElement != null)
                {
                    fallbackContent = fallbackElement;
                    return FallbackType.Content;
                }

                throw new AdaptiveCardParseException(ErrorStatusCode.InvalidPropertyValue, "Fallback content did not parse correctly.");
            }

            throw new AdaptiveCardParseException(ErrorStatusCode.InvalidPropertyValue, "Invalid value for fallback");
        }

        public static JsonObject SerializeBase(
            JsonObject additionalProperties, string typeString, string id, FallbackType fallbackType, JsonNode fallbackJson,
            IList<AdaptiveRequirement> requirements)
        {
            var root = additionalProperties != null ? (JsonObject)additionalProperties.DeepClone() : new JsonObject();
            root["type"] = typeString;

            if (!string.IsNullOrEmpty(id))
            {
                root["id"] = id;
            }

            if (fallbackType == FallbackType.Drop)
            {
                root["fallback"] = "drop";
            }
            else if (fallbackType == FallbackType.Content && fallbackJson != null)
            {
                root["fallback"] = fallbackJson;
            }

            if (requirements != null && requirements.Count > 0)
            {
                var requires = new JsonObject();
                foreach (var requirement in requirements)
                {
                    requires[requirement.Name] = SemanticVersion.TryParse(requirement.Version, out var v) ? v.ToString() : requirement.Version;
                }

                root["requires"] = requires;
            }

            return root;
        }
    }

    /// <summary>
    /// Base class for all card elements (port of the shared BaseCardElement). Custom elements may derive from this
    /// class (passing <see cref="ElementType.Custom"/>) or implement <see cref="IAdaptiveCardElement"/> directly.
    /// </summary>
    public abstract class AdaptiveCardElementBase : IAdaptiveCardElement, IAdaptiveElementWithRemoteResources
    {
        protected AdaptiveCardElementBase(ElementType elementType, string elementTypeString)
        {
            ElementType = elementType;
            ElementTypeString = elementTypeString;
            InternalIdValue = InternalId.ForNewElement();
        }

        public ElementType ElementType { get; }

        public string ElementTypeString { get; protected internal set; }

        public HeightType Height { get; set; } = HeightType.Auto;

        public Spacing Spacing { get; set; } = Spacing.Default;

        public bool Separator { get; set; }

        public string Id { get; set; } = string.Empty;

        public bool IsVisible { get; set; } = true;

        public FallbackType FallbackType { get; set; }

        public IAdaptiveCardElement FallbackContent { get; set; }

        public JsonObject AdditionalProperties { get; set; } = new JsonObject();

        public IList<AdaptiveRequirement> Requirements { get; } = new List<AdaptiveRequirement>();

        internal uint InternalIdValue { get; set; }

        internal bool CanFallbackToAncestor { get; set; }

        /// <summary>Adds the JSON property names this element type understands.</summary>
        protected virtual void PopulateKnownProperties(ISet<string> knownProperties)
        {
            knownProperties.Add("height");
            knownProperties.Add("isVisible");
            knownProperties.Add("minHeight");
            knownProperties.Add("separator");
            knownProperties.Add("spacing");
        }

        internal HashSet<string> KnownProperties => ElementBaseHelpers.GetKnownProperties(GetType(), s => PopulateKnownProperties(s));

        public virtual JsonObject ToJson()
        {
            var root = ElementBaseHelpers.SerializeBase(
                AdditionalProperties, ElementTypeString, Id, FallbackType, FallbackContent?.ToJson(), Requirements);

            if (Height != HeightType.Auto)
            {
                root["height"] = EnumStrings.HeightType.ToString(Height);
            }

            if (Spacing != Spacing.Default)
            {
                root["spacing"] = EnumStrings.Spacing.ToString(Spacing);
            }

            if (Separator)
            {
                root["separator"] = true;
            }

            if (!IsVisible)
            {
                root["isVisible"] = false;
            }

            return root;
        }

        /// <summary>Remote resources (images, media) referenced by this element. Custom elements may override.</summary>
        public virtual IReadOnlyList<AdaptiveRemoteResourceInformation> GetResourceInformation()
        {
            var list = new List<AdaptiveRemoteResourceInformation>();
            GetResourceInformation(list);
            return list;
        }

        internal virtual void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
        }

        /// <summary>Port of BaseCardElement::Deserialize&lt;T&gt; (base properties + unknown property handling).</summary>
        internal void DeserializeBaseProperties(ParseContext context, JsonObject json)
        {
            ParseUtil.ThrowIfNotJsonObject(json);
            Id = ParseUtil.GetString(json, "id");
            FallbackType = ElementBaseHelpers.ParseFallback(
                context, json, Id, InternalIdValue, ParseUtil.ParseCardElement, out IAdaptiveCardElement fallback);
            FallbackContent = fallback;
            ElementBaseHelpers.ParseRequires(json, Requirements);

            CanFallbackToAncestor = context.CanFallbackToAncestor;
            Height = ParseUtil.GetEnumValue(json, "height", HeightType.Auto, EnumStrings.HeightType);
            IsVisible = ParseUtil.GetBool(json, "isVisible", true);
            Separator = ParseUtil.GetBool(json, "separator", false);
            Spacing = ParseUtil.GetEnumValue(json, "spacing", Spacing.Default, EnumStrings.Spacing);

            AdditionalProperties = ParseUtil.HandleUnknownProperties(json, KnownProperties);
        }

        internal static void SerializeSelectAction(JsonObject root, IAdaptiveActionElement selectAction)
        {
            if (selectAction != null)
            {
                root["selectAction"] = selectAction.ToJson();
            }
        }

        internal static void GetResourceInformation(IEnumerable<IAdaptiveCardElement> elements, List<AdaptiveRemoteResourceInformation> list)
        {
            if (elements == null)
            {
                return;
            }

            foreach (var element in elements)
            {
                if (element is IAdaptiveElementWithRemoteResources r)
                {
                    list.AddRange(r.GetResourceInformation());
                }
            }
        }
    }

    /// <summary>
    /// Base class for all actions (port of the shared BaseActionElement). Custom actions may derive from this class
    /// (passing <see cref="ActionType.Custom"/>) or implement <see cref="IAdaptiveActionElement"/> directly.
    /// </summary>
    public abstract class AdaptiveActionElementBase : IAdaptiveActionElement, IAdaptiveElementWithRemoteResources
    {
        internal const string DefaultStyle = "default";

        protected AdaptiveActionElementBase(ActionType actionType, string actionTypeString)
        {
            ActionType = actionType;
            ActionTypeString = actionTypeString;
            Role = actionType == ActionType.OpenUrl ? ActionRole.Link : ActionRole.Button;
            InternalIdValue = Uno.InternalId.ForNewElement();
        }

        public ActionType ActionType { get; }

        public string ActionTypeString { get; protected internal set; }

        public string Title { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public FallbackType FallbackType { get; set; }

        public IAdaptiveActionElement FallbackContent { get; set; }

        public string IconUrl { get; set; } = string.Empty;

        public string Style { get; set; } = DefaultStyle;

        public string Tooltip { get; set; } = string.Empty;

        public JsonObject AdditionalProperties { get; set; } = new JsonObject();

        public bool IsEnabled { get; set; } = true;

        public ActionMode Mode { get; set; } = ActionMode.Primary;

        public ActionRole Role { get; set; }

        /// <summary>Unique (per process) identifier assigned at construction.</summary>
        public uint InternalId => InternalIdValue;

        internal uint InternalIdValue { get; set; }

        internal IList<AdaptiveRequirement> Requirements { get; } = new List<AdaptiveRequirement>();

        protected virtual void PopulateKnownProperties(ISet<string> knownProperties)
        {
            knownProperties.Add("iconUrl");
            knownProperties.Add("style");
            knownProperties.Add("title");
            knownProperties.Add("mode");
            knownProperties.Add("tooltip");
            knownProperties.Add("isEnabled");
            knownProperties.Add("role");
        }

        internal HashSet<string> KnownProperties => ElementBaseHelpers.GetKnownProperties(GetType(), s => PopulateKnownProperties(s));

        public virtual JsonObject ToJson()
        {
            var root = ElementBaseHelpers.SerializeBase(
                AdditionalProperties, ActionTypeString, Id, FallbackType, FallbackContent?.ToJson(), Requirements);

            if (!string.IsNullOrEmpty(IconUrl))
            {
                root["iconUrl"] = IconUrl;
            }

            if (!string.IsNullOrEmpty(Title))
            {
                root["title"] = Title;
            }

            if (!string.IsNullOrEmpty(Style) && Style != DefaultStyle)
            {
                root["style"] = Style;
            }

            if (Mode != ActionMode.Primary)
            {
                root["mode"] = EnumStrings.Mode.ToString(Mode);
            }

            if (!string.IsNullOrEmpty(Tooltip))
            {
                root["tooltip"] = Tooltip;
            }

            if (!IsEnabled)
            {
                root["isEnabled"] = false;
            }

            // The shared model omits "role" when it is Button, which loses an explicit "button" on Action.OpenUrl
            // (whose default is Link). Compare against the per-type default instead.
            var defaultRole = ActionType == ActionType.OpenUrl ? ActionRole.Link : ActionRole.Button;
            if (Role != defaultRole)
            {
                root["role"] = EnumStrings.ActionRole.ToString(Role);
            }

            return root;
        }

        /// <summary>Remote resources (images, media) referenced by this element. Custom elements may override.</summary>
        public virtual IReadOnlyList<AdaptiveRemoteResourceInformation> GetResourceInformation()
        {
            var list = new List<AdaptiveRemoteResourceInformation>();
            GetResourceInformation(list);
            return list;
        }

        internal virtual void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            if (!string.IsNullOrEmpty(IconUrl))
            {
                resourceInformation.Add(new AdaptiveRemoteResourceInformation(IconUrl, "image"));
            }
        }

        /// <summary>Port of BaseActionElement::Deserialize&lt;T&gt;.</summary>
        internal void DeserializeBaseProperties(ParseContext context, JsonObject json)
        {
            ParseUtil.ThrowIfNotJsonObject(json);
            Id = ParseUtil.GetString(json, "id");
            FallbackType = ElementBaseHelpers.ParseFallback(
                context, json, Id, InternalIdValue, ParseUtil.GetActionFromJsonValue, out IAdaptiveActionElement fallback);
            FallbackContent = fallback;
            ElementBaseHelpers.ParseRequires(json, Requirements);

            Title = ParseUtil.GetString(json, "title");
            IconUrl = ParseUtil.GetString(json, "iconUrl");
            Style = ParseUtil.GetString(json, "style", DefaultStyle, false);
            Mode = ParseUtil.GetEnumValue(json, "mode", ActionMode.Primary, EnumStrings.Mode);
            Tooltip = ParseUtil.GetString(json, "tooltip");
            IsEnabled = ParseUtil.GetBool(json, "isEnabled", true);
            Role = ParseUtil.GetEnumValue(json, "role", ActionRole.Button, EnumStrings.ActionRole);

            AdditionalProperties = ParseUtil.HandleUnknownProperties(json, KnownProperties);
        }

        internal static void GetResourceInformation(IEnumerable<IAdaptiveActionElement> actions, List<AdaptiveRemoteResourceInformation> list)
        {
            if (actions == null)
            {
                return;
            }

            foreach (var action in actions)
            {
                if (action is IAdaptiveElementWithRemoteResources r)
                {
                    list.AddRange(r.GetResourceInformation());
                }
            }
        }
    }

    /// <summary>Base class for inputs (port of the shared BaseInputElement).</summary>
    public abstract class AdaptiveInputElementBase : AdaptiveCardElementBase, IAdaptiveInputElement
    {
        protected AdaptiveInputElementBase(ElementType elementType, string elementTypeString) : base(elementType, elementTypeString)
        {
        }

        public bool IsRequired { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("isRequired");
            knownProperties.Add("errorMessage");
            knownProperties.Add("label");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (IsRequired)
            {
                root["isRequired"] = true;
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                root["errorMessage"] = ErrorMessage;
            }

            if (!string.IsNullOrEmpty(Label))
            {
                root["label"] = Label;
            }

            return root;
        }

        /// <summary>Port of BaseInputElement::Deserialize&lt;T&gt; (inputs require an id).</summary>
        internal void DeserializeInputBaseProperties(ParseContext context, JsonObject json)
        {
            DeserializeBaseProperties(context, json);
            Id = ParseUtil.GetString(json, "id", true);
            IsRequired = ParseUtil.GetBool(json, "isRequired", false);
            ErrorMessage = ParseUtil.GetString(json, "errorMessage");
            Label = ParseUtil.GetString(json, "label");
        }
    }
}
