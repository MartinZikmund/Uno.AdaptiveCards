// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveRefresh
    {
        public AdaptiveRefresh()
        {
        }

        public IAdaptiveActionElement Action { get; set; }

        public IList<string> UserIds { get; } = new List<string>();

        internal bool ShouldSerialize() => Action != null || UserIds.Count > 0;

        public JsonObject ToJson()
        {
            var root = new JsonObject();
            if (Action != null)
            {
                root["action"] = Action.ToJson();
            }

            if (UserIds.Count > 0)
            {
                var ids = new JsonArray();
                foreach (var id in UserIds)
                {
                    ids.Add(id);
                }

                root["userIds"] = ids;
            }

            return root;
        }

        internal static AdaptiveRefresh Deserialize(ParseContext context, JsonNode jsonNode)
        {
            if (jsonNode == null)
            {
                return null;
            }

            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            var refresh = new AdaptiveRefresh { Action = ParseUtil.GetAction(context, json, "action") };
            foreach (var id in ParseUtil.GetStringArray(json, "userIds"))
            {
                refresh.UserIds.Add(id);
            }

            return refresh;
        }
    }

    public sealed class AdaptiveTokenExchangeResource
    {
        public AdaptiveTokenExchangeResource()
        {
        }

        public string Id { get; set; } = string.Empty;

        public string Uri { get; set; } = string.Empty;

        public string ProviderId { get; set; } = string.Empty;

        internal bool ShouldSerialize() => !string.IsNullOrEmpty(Id) || !string.IsNullOrEmpty(Uri) || !string.IsNullOrEmpty(ProviderId);

        public JsonObject ToJson()
        {
            var root = new JsonObject();
            if (!string.IsNullOrEmpty(Id))
            {
                root["id"] = Id;
            }

            if (!string.IsNullOrEmpty(Uri))
            {
                root["uri"] = Uri;
            }

            if (!string.IsNullOrEmpty(ProviderId))
            {
                root["providerId"] = ProviderId;
            }

            return root;
        }

        internal static AdaptiveTokenExchangeResource Deserialize(ParseContext context, JsonNode jsonNode)
        {
            if (jsonNode == null)
            {
                return null;
            }

            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            return new AdaptiveTokenExchangeResource
            {
                Id = ParseUtil.GetString(json, "id"),
                Uri = ParseUtil.GetString(json, "uri"),
                ProviderId = ParseUtil.GetString(json, "providerId"),
            };
        }
    }

    public sealed class AdaptiveAuthCardButton
    {
        public AdaptiveAuthCardButton()
        {
        }

        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        internal bool ShouldSerialize() =>
            !string.IsNullOrEmpty(Type) || !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Image) || !string.IsNullOrEmpty(Value);

        public JsonObject ToJson()
        {
            var root = new JsonObject();
            if (!string.IsNullOrEmpty(Type))
            {
                root["type"] = Type;
            }

            if (!string.IsNullOrEmpty(Title))
            {
                root["title"] = Title;
            }

            if (!string.IsNullOrEmpty(Image))
            {
                root["image"] = Image;
            }

            if (!string.IsNullOrEmpty(Value))
            {
                root["value"] = Value;
            }

            return root;
        }

        internal static AdaptiveAuthCardButton Deserialize(ParseContext context, JsonNode jsonNode)
        {
            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            return new AdaptiveAuthCardButton
            {
                Type = ParseUtil.GetString(json, "type"),
                Title = ParseUtil.GetString(json, "title"),
                Image = ParseUtil.GetString(json, "image"),
                Value = ParseUtil.GetString(json, "value"),
            };
        }
    }

    public sealed class AdaptiveAuthentication
    {
        public AdaptiveAuthentication()
        {
        }

        public string Text { get; set; } = string.Empty;

        public string ConnectionName { get; set; } = string.Empty;

        public AdaptiveTokenExchangeResource TokenExchangeResource { get; set; }

        public IList<AdaptiveAuthCardButton> Buttons { get; } = new List<AdaptiveAuthCardButton>();

        internal bool ShouldSerialize() =>
            !string.IsNullOrEmpty(Text) || !string.IsNullOrEmpty(ConnectionName) || Buttons.Count > 0 ||
            (TokenExchangeResource != null && TokenExchangeResource.ShouldSerialize());

        public JsonObject ToJson()
        {
            var root = new JsonObject();
            if (!string.IsNullOrEmpty(Text))
            {
                root["text"] = Text;
            }

            if (!string.IsNullOrEmpty(ConnectionName))
            {
                root["connectionName"] = ConnectionName;
            }

            if (TokenExchangeResource != null && TokenExchangeResource.ShouldSerialize())
            {
                root["tokenExchangeResource"] = TokenExchangeResource.ToJson();
            }

            if (Buttons.Count > 0)
            {
                var buttons = new JsonArray();
                foreach (var button in Buttons)
                {
                    if (button != null && button.ShouldSerialize())
                    {
                        buttons.Add((JsonNode)button.ToJson());
                    }
                }

                root["buttons"] = buttons;
            }

            return root;
        }

        internal static AdaptiveAuthentication Deserialize(ParseContext context, JsonNode jsonNode)
        {
            if (jsonNode == null)
            {
                return null;
            }

            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            var authentication = new AdaptiveAuthentication
            {
                Text = ParseUtil.GetString(json, "text"),
                ConnectionName = ParseUtil.GetString(json, "connectionName"),
                TokenExchangeResource = AdaptiveTokenExchangeResource.Deserialize(context, ParseUtil.ExtractJsonValue(json, "tokenExchangeResource")),
            };

            foreach (var button in ParseUtil.GetElementCollectionOfSingleType(context, json, "buttons", AdaptiveAuthCardButton.Deserialize))
            {
                authentication.Buttons.Add(button);
            }

            return authentication;
        }
    }
}
