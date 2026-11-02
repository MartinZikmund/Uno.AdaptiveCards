// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>Element of an unknown type (port of the shared UnknownElement). Round-trips its original JSON.</summary>
    public sealed class AdaptiveUnsupportedElement : AdaptiveCardElementBase
    {
        public AdaptiveUnsupportedElement() : base(ElementType.Unsupported, "Unknown")
        {
        }

        public override JsonObject ToJson() => (JsonObject)AdditionalProperties.DeepClone();

        internal static AdaptiveUnsupportedElement Deserialize(ParseContext context, JsonObject json)
        {
            var actualType = ParseUtil.GetTypeAsString(json);
            var unknown = new AdaptiveUnsupportedElement();
            unknown.DeserializeBaseProperties(context, json);
            unknown.AdditionalProperties = (JsonObject)json.DeepClone();
            unknown.ElementTypeString = actualType;
            return unknown;
        }
    }

    /// <summary>Action of an unknown type (port of the shared UnknownAction). Round-trips its original JSON.</summary>
    public sealed class AdaptiveUnsupportedAction : AdaptiveActionElementBase
    {
        public AdaptiveUnsupportedAction() : base(ActionType.Unsupported, "UnknownAction")
        {
        }

        public override JsonObject ToJson() => (JsonObject)AdditionalProperties.DeepClone();

        internal static AdaptiveUnsupportedAction Deserialize(ParseContext context, JsonObject json)
        {
            var actualType = ParseUtil.GetTypeAsString(json);
            var unknown = new AdaptiveUnsupportedAction();
            unknown.DeserializeBaseProperties(context, json);
            unknown.AdditionalProperties = (JsonObject)json.DeepClone();
            unknown.ActionTypeString = actualType;
            return unknown;
        }
    }
}
