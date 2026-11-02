// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveActionSet : AdaptiveCardElementBase
    {
        public AdaptiveActionSet() : base(ElementType.ActionSet, "ActionSet")
        {
        }

        public IList<IAdaptiveActionElement> Actions { get; } = new List<IAdaptiveActionElement>();

        public uint InternalId => InternalIdValue;

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("actions");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            var actions = new JsonArray();
            foreach (var action in Actions)
            {
                actions.Add((JsonNode)action.ToJson());
            }

            root["actions"] = actions;
            return root;
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            AdaptiveActionElementBase.GetResourceInformation(Actions, resourceInformation);
        }

        internal static AdaptiveActionSet Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "ActionSet");
            var actionSet = new AdaptiveActionSet();
            actionSet.DeserializeBaseProperties(context, json);
            foreach (var action in ParseUtil.GetActionCollection(context, json, "actions", false))
            {
                actionSet.Actions.Add(action);
            }

            return actionSet;
        }
    }

    public sealed class AdaptiveActionSetParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveActionSet.Deserialize(context, json);
    }
}
