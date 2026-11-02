// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveOpenUrlAction : AdaptiveActionElementBase
    {
        public AdaptiveOpenUrlAction() : base(ActionType.OpenUrl, "Action.OpenUrl")
        {
        }

        public Uri Url { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("url");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            root["url"] = Url == null ? string.Empty : (Url.IsAbsoluteUri ? Url.AbsoluteUri : Url.OriginalString);
            return root;
        }

        internal static AdaptiveOpenUrlAction Deserialize(ParseContext context, JsonObject json)
        {
            var action = new AdaptiveOpenUrlAction();
            action.DeserializeBaseProperties(context, json);
            var url = ParseUtil.GetString(json, "url", true);
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                action.Url = uri;
            }

            action.Role = ParseUtil.GetEnumValue(json, "role", ActionRole.Link, EnumStrings.ActionRole);
            return action;
        }
    }

    public sealed class AdaptiveOpenUrlActionParser : BuiltInActionParser
    {
        internal override IAdaptiveActionElement Deserialize(ParseContext context, JsonObject json) => AdaptiveOpenUrlAction.Deserialize(context, json);
    }

    public sealed class AdaptiveShowCardAction : AdaptiveActionElementBase
    {
        public AdaptiveShowCardAction() : base(ActionType.ShowCard, "Action.ShowCard")
        {
        }

        public AdaptiveCard Card { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("card");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            root["card"] = (Card ?? new AdaptiveCard()).ToJson();
            return root;
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            base.GetResourceInformation(resourceInformation);
            if (Card != null)
            {
                resourceInformation.AddRange(Card.GetResourceInformation());
            }
        }

        internal static AdaptiveShowCardAction Deserialize(ParseContext context, JsonObject json)
        {
            var action = new AdaptiveShowCardAction();
            action.DeserializeBaseProperties(context, json);
            // The inner card is parsed with the same context (and without version enforcement). Its warnings are
            // already accumulated in the shared context.
            action.Card = AdaptiveCard.Deserialize(ParseUtil.Get(json, "card"), string.Empty, context);
            return action;
        }
    }

    public sealed class AdaptiveShowCardActionParser : BuiltInActionParser
    {
        internal override IAdaptiveActionElement Deserialize(ParseContext context, JsonObject json) => AdaptiveShowCardAction.Deserialize(context, json);
    }

    public sealed class AdaptiveSubmitAction : AdaptiveActionElementBase
    {
        public AdaptiveSubmitAction() : base(ActionType.Submit, "Action.Submit")
        {
        }

        public JsonNode DataJson { get; set; }

        public AssociatedInputs AssociatedInputs { get; set; } = AssociatedInputs.Auto;

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("data");
            knownProperties.Add("associatedInputs");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (!ParseUtil.IsEmpty(DataJson))
            {
                root["data"] = DataJson.DeepClone();
            }

            if (AssociatedInputs != AssociatedInputs.Auto)
            {
                root["associatedInputs"] = EnumStrings.AssociatedInputs.ToString(AssociatedInputs);
            }

            return root;
        }

        internal static AdaptiveSubmitAction Deserialize(ParseContext context, JsonObject json)
        {
            var action = new AdaptiveSubmitAction();
            action.DeserializeBaseProperties(context, json);
            action.DataJson = ParseUtil.ExtractJsonValue(json, "data")?.DeepClone();
            action.AssociatedInputs = ParseUtil.GetEnumValue(json, "associatedInputs", AssociatedInputs.Auto, EnumStrings.AssociatedInputs);
            return action;
        }
    }

    public sealed class AdaptiveSubmitActionParser : BuiltInActionParser
    {
        internal override IAdaptiveActionElement Deserialize(ParseContext context, JsonObject json) => AdaptiveSubmitAction.Deserialize(context, json);
    }

    public sealed class AdaptiveExecuteAction : AdaptiveActionElementBase
    {
        public AdaptiveExecuteAction() : base(ActionType.Execute, "Action.Execute")
        {
        }

        public JsonNode DataJson { get; set; }

        public AssociatedInputs AssociatedInputs { get; set; } = AssociatedInputs.Auto;

        public string Verb { get; set; } = string.Empty;

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("data");
            knownProperties.Add("verb");
            knownProperties.Add("associatedInputs");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (!ParseUtil.IsEmpty(DataJson))
            {
                root["data"] = DataJson.DeepClone();
            }

            if (!string.IsNullOrEmpty(Verb))
            {
                root["verb"] = Verb;
            }

            if (AssociatedInputs != AssociatedInputs.Auto)
            {
                root["associatedInputs"] = EnumStrings.AssociatedInputs.ToString(AssociatedInputs);
            }

            return root;
        }

        internal static AdaptiveExecuteAction Deserialize(ParseContext context, JsonObject json)
        {
            var action = new AdaptiveExecuteAction();
            action.DeserializeBaseProperties(context, json);
            action.DataJson = ParseUtil.ExtractJsonValue(json, "data")?.DeepClone();
            action.Verb = ParseUtil.GetString(json, "verb");
            action.AssociatedInputs = ParseUtil.GetEnumValue(json, "associatedInputs", AssociatedInputs.Auto, EnumStrings.AssociatedInputs);
            return action;
        }
    }

    public sealed class AdaptiveExecuteActionParser : BuiltInActionParser
    {
        internal override IAdaptiveActionElement Deserialize(ParseContext context, JsonObject json) => AdaptiveExecuteAction.Deserialize(context, json);
    }

    public sealed class AdaptiveToggleVisibilityTarget
    {
        public AdaptiveToggleVisibilityTarget()
        {
        }

        public string ElementId { get; set; } = string.Empty;

        public IsVisible IsVisible { get; set; } = IsVisible.IsVisibleToggle;

        public JsonNode ToJson()
        {
            if (IsVisible == IsVisible.IsVisibleToggle)
            {
                return JsonValue.Create(ElementId ?? string.Empty);
            }

            return new JsonObject
            {
                ["elementId"] = ElementId ?? string.Empty,
                ["isVisible"] = IsVisible == IsVisible.IsVisibleTrue,
            };
        }

        internal static AdaptiveToggleVisibilityTarget Deserialize(ParseContext context, JsonNode jsonNode)
        {
            var target = new AdaptiveToggleVisibilityTarget();
            if (ParseUtil.IsString(jsonNode))
            {
                target.ElementId = jsonNode.GetValue<string>();
                return target;
            }

            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            target.ElementId = ParseUtil.GetString(json, "elementId", true);
            var isVisible = ParseUtil.Get(json, "isVisible");
            if (!ParseUtil.IsEmpty(isVisible))
            {
                if (!ParseUtil.IsBool(isVisible))
                {
                    throw new AdaptiveCardParseException(
                        ErrorStatusCode.InvalidPropertyValue, "Value for property isVisible was invalid. Expected type bool.");
                }

                target.IsVisible = isVisible.GetValue<bool>() ? IsVisible.IsVisibleTrue : IsVisible.IsVisibleFalse;
            }

            return target;
        }
    }

    public sealed class AdaptiveToggleVisibilityAction : AdaptiveActionElementBase
    {
        public AdaptiveToggleVisibilityAction() : base(ActionType.ToggleVisibility, "Action.ToggleVisibility")
        {
        }

        public IList<AdaptiveToggleVisibilityTarget> TargetElements { get; } = new List<AdaptiveToggleVisibilityTarget>();

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("targetElements");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            var targets = new JsonArray();
            foreach (var target in TargetElements)
            {
                targets.Add((JsonNode)target.ToJson());
            }

            root["targetElements"] = targets;
            return root;
        }

        internal static AdaptiveToggleVisibilityAction Deserialize(ParseContext context, JsonObject json)
        {
            var action = new AdaptiveToggleVisibilityAction();
            action.DeserializeBaseProperties(context, json);
            foreach (var target in ParseUtil.GetElementCollectionOfSingleType(
                context, json, "targetElements", AdaptiveToggleVisibilityTarget.Deserialize, true))
            {
                action.TargetElements.Add(target);
            }

            return action;
        }
    }

    public sealed class AdaptiveToggleVisibilityActionParser : BuiltInActionParser
    {
        internal override IAdaptiveActionElement Deserialize(ParseContext context, JsonObject json) =>
            AdaptiveToggleVisibilityAction.Deserialize(context, json);
    }
}
