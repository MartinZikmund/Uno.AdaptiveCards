// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    internal static class TestUtil
    {
        public static string TestDataPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

        public static string ReadTestData(string fileName) => File.ReadAllText(TestDataPath(fileName));

        /// <summary>Parses a card using the given renderer version (shared model DeserializeFromString semantics).</summary>
        public static (AdaptiveCard Card, ParseContext Context) Deserialize(string json, string rendererVersion = "1.6",
            AdaptiveElementParserRegistration elements = null, AdaptiveActionParserRegistration actions = null)
        {
            var context = new ParseContext(elements, actions);
            var card = AdaptiveCard.Deserialize(ParseUtil.ParseJsonString(json), rendererVersion, context);
            return (card, context);
        }

        public static AdaptiveCard Parse(string json)
        {
            var result = AdaptiveCard.FromJsonString(json);
            if (result.AdaptiveCard == null)
            {
                throw new InvalidOperationException("Parse failed: " + string.Join("; ", result.Errors));
            }

            return result.AdaptiveCard;
        }

        /// <summary>
        /// Serializes like jsoncpp's StreamWriterBuilder with no indentation: object keys sorted, compact output.
        /// The trailing newline jsoncpp appends is not included.
        /// </summary>
        public static string Canonical(JsonNode node)
        {
            return Sort(node)?.ToJsonString(new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) ?? "null";
        }

        private static JsonNode Sort(JsonNode node)
        {
            switch (node)
            {
                case JsonObject obj:
                    var sorted = new JsonObject();
                    foreach (var kvp in obj.OrderBy(k => k.Key, StringComparer.Ordinal))
                    {
                        sorted[kvp.Key] = Sort(kvp.Value);
                    }

                    return sorted;
                case JsonArray array:
                    var newArray = new JsonArray();
                    foreach (var item in array)
                    {
                        newArray.Add(Sort(item));
                    }

                    return newArray;
                default:
                    return node?.DeepClone();
            }
        }

        /// <summary>Strips the trailing "\n" from a jsoncpp serialization string literal.</summary>
        public static string Cpp(string expected) => expected.EndsWith("\n") ? expected.Substring(0, expected.Length - 1) : expected;

        public static string Serialize(AdaptiveCard card) => Canonical(card.ToJson());

        public static string Serialize(IAdaptiveCardElement element) => Canonical(element.ToJson());

        public static string Serialize(IAdaptiveActionElement action) => Canonical(action.ToJson());
    }
}
