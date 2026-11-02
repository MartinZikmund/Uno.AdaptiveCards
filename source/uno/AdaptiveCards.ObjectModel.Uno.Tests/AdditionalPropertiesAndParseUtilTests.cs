// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Ports of AdditionalPropertiesTest.cpp and ParseUtilTest.cpp.
using System.Linq;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class AdditionalPropertyTests
    {
        private const string CardLevelJson = @"{
            ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.0"",
            ""unknown"": ""testing unknown"", ""lang"": ""en"", ""fallbackText"": ""test"", ""speak"": ""test"", ""minHeight"": ""1px"",
            ""verticalContentAlignment"": ""Center"",
            ""backgroundImage"": { ""url"": ""bing.com"", ""fillMode"": ""RepeatHorizontally"", ""horizontalAlignment"": ""Center"", ""verticalAlignment"": ""Center"" },
            SELECTACTION
            ""body"": [ { ""type"": ""TextBlock"", ""text"": ""You can even draw attention to certain text with color"" } ],
            ""actions"": [ { ""type"": ""Action.Submit"", ""MyAdditionalProperty"": ""Foo"" } ]
        }";

        [Fact]
        public void CanGetAdditionalProperitesAtCardLevelTest()
        {
            var (card, _) = Deserialize(@"{""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"",""type"":""AdaptiveCard"",""version"":""1.0"",
                ""unknown"":""testing unknown"",""body"":[{""type"":""TextBlock"",""text"":""You can even draw attention to certain text with color"",
                ""wrap"":true,""color"":""attention"",""unknown"":""testing unknown""}]}", "1.0");
            Assert.Equal("{\"unknown\":\"testing unknown\"}", Canonical(card.AdditionalProperties));
        }

        [Fact]
        public void CanGetCorrectAdditionalPropertiesAtCardLevelTest()
        {
            var json = CardLevelJson.Replace("SELECTACTION",
                @"""selectAction"": { ""type"": ""Action.OpenUrl"", ""id"": ""hello"", ""title"": ""world"", ""url"": ""www.bing.com"" },");
            var (card, _) = Deserialize(json, "1.0");
            Assert.Equal("{\"unknown\":\"testing unknown\"}", Canonical(card.AdditionalProperties));
        }

        [Fact]
        public void RoundtrippingWithAdditionalPropertiesAtCardLevel()
        {
            var (card, _) = Deserialize(CardLevelJson.Replace("SELECTACTION", string.Empty), "1.0");
            Assert.Equal(
                "{\"actions\":[{\"MyAdditionalProperty\":\"Foo\",\"type\":\"Action.Submit\"}],\"backgroundImage\":{\"fillMode\":\"repeatHorizontally\",\"horizontalAlignment\":\"center\",\"url\":\"bing.com\",\"verticalAlignment\":\"center\"},\"body\":[{\"text\":\"You can even draw attention to certain text with color\",\"type\":\"TextBlock\"}],\"fallbackText\":\"test\",\"lang\":\"en\",\"minHeight\":\"1px\",\"speak\":\"test\",\"type\":\"AdaptiveCard\",\"unknown\":\"testing unknown\",\"version\":\"1.0\",\"verticalContentAlignment\":\"Center\"}",
                Serialize(card));
        }

        [Fact]
        public void CanGetAdditionalProperitesTest()
        {
            var (card, _) = Deserialize(@"{""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"",""type"":""AdaptiveCard"",""version"":""1.0"",
                ""body"":[{""type"":""TextBlock"",""text"":""You can even draw attention to certain text with color"",""wrap"":true,""color"":""attention"",""unknown"":""testing unknown""}]}", "1.0");
            Assert.Equal("{\"unknown\":\"testing unknown\"}", Canonical(card.Body.First().AdditionalProperties));
        }

        [Fact]
        public void CanGetAdditionalProperitesTest_Action()
        {
            var (card, _) = Deserialize(@"{""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"",""type"":""AdaptiveCard"",""version"":""1.0"",
                ""body"":[{""type"":""TextBlock"",""text"":""You can even draw attention to certain text with color""}],
                ""actions"":[{""type"":""Action.Submit"",""MyAdditionalProperty"":""Foo""}]}", "1.0");
            Assert.Equal("{\"MyAdditionalProperty\":\"Foo\"}", Canonical(card.Actions.First().AdditionalProperties));
        }

        [Fact]
        public void CanGetAdditionalProperitesTest_Inlines()
        {
            var context = new ParseContext(null, null);
            var textRun = AdaptiveTextRun.Deserialize(context, JsonNode.Parse(@"{""type"":""TextRun"",""text"":""Here is some text"",""MyAdditionalProperty"":""Bar""}"));
            Assert.Equal("{\"MyAdditionalProperty\":\"Bar\"}", Canonical(textRun.AdditionalProperties));
        }

        [Fact]
        public void UnknownElementRoundtripping()
        {
            var (card, _) = Deserialize(@"{""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"",""type"":""AdaptiveCard"",""version"":""1.0"",
                ""body"":[{""type"":""TextBlock"",""text"":""Standard textblock""},{""type"":""SomeRandomType"",""property"":""value"",""someOtherProperty"":""some other value""}]}", "1.0");
            Assert.Equal(
                "{\"actions\":[],\"body\":[{\"text\":\"Standard textblock\",\"type\":\"TextBlock\"},{\"property\":\"value\",\"someOtherProperty\":\"some other value\",\"type\":\"SomeRandomType\"}],\"type\":\"AdaptiveCard\",\"version\":\"1.0\"}",
                Serialize(card));
        }
    }

    public class ParseUtilTests
    {
        private static JsonObject ValidJsonObject() => (JsonObject)JsonNode.Parse("{ \"foo\": \"bar\" }");

        private static JsonObject JsonObjectWithType(string typeName) => (JsonObject)JsonNode.Parse("{ \"foo\": \"bar\", \"type\": \"" + typeName + "\"}");

        private static JsonObject JsonObjectWithAccent(string value) => (JsonObject)JsonNode.Parse("{ \"foo\": \"bar\", \"accent\": " + value + "}");

        [Fact]
        public void GetJsonValueFromStringTests()
        {
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.ParseJsonString("definitely not json"));
            var jsonValue = ParseUtil.GetJsonObjectFromString("{ \"foo\": \"bar\" }");
            Assert.Equal("bar", jsonValue["foo"].GetValue<string>());
        }

        [Fact]
        public void ThrowIfNotJsonObjectTests()
        {
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.ThrowIfNotJsonObject(null));
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.ThrowIfNotJsonObject(JsonValue.Create("str")));
            ParseUtil.ThrowIfNotJsonObject(ValidJsonObject());
        }

        // ExpectKeyAndValueTypeTests: ParseUtil::ExpectKeyAndValueType has no C# equivalent (unused by the parser).

        [Fact]
        public void GetTypeAsStringTests()
        {
            var value = ValidJsonObject();
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetTypeAsString(value));
            Assert.Equal("", ParseUtil.TryGetTypeAsString(value));
            var typedValue = JsonObjectWithType("someType");
            Assert.Equal("someType", ParseUtil.GetTypeAsString(typedValue));
            Assert.Equal("someType", ParseUtil.TryGetTypeAsString(typedValue));
        }

        [Fact]
        public void ExpectTypeStringTests()
        {
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.ExpectTypeString(ValidJsonObject(), "AdaptiveCard"));
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.ExpectTypeString(JsonObjectWithType("InvalidType"), "AdaptiveCard"));
            var validType = JsonObjectWithType("AdaptiveCard");
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.ExpectTypeString(validType, "Custom"));
            ParseUtil.ExpectTypeString(validType, "AdaptiveCard");
        }

        [Fact]
        public void ExtractJsonValueTests()
        {
            var jsonObj = ValidJsonObject();
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.ExtractJsonValue(jsonObj, "accent", true));
            Assert.Null(ParseUtil.ExtractJsonValue(jsonObj, "accent", false));
            Assert.True(ParseUtil.ExtractJsonValue(JsonObjectWithAccent("true"), "accent", true).GetValue<bool>());
        }

        [Fact]
        public void GetArrayTests()
        {
            var e = Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetArray(ValidJsonObject(), "accent", true));
            Assert.Equal(ErrorStatusCode.RequiredPropertyMissing, e.StatusCode);
            Assert.Equal("Could not parse required key: accent. It was not found", e.Message);

            Assert.Null(ParseUtil.GetArray(ValidJsonObject(), "accent", false));

            e = Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetArray(JsonObjectWithAccent("true"), "accent", true));
            Assert.Equal(ErrorStatusCode.InvalidPropertyValue, e.StatusCode);
            Assert.Equal("Could not parse specified key: accent. It was not an array", e.Message);

            e = Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetArray(JsonObjectWithAccent("{}"), "accent", true));
            Assert.Equal(ErrorStatusCode.InvalidPropertyValue, e.StatusCode);
            Assert.Equal("Could not parse specified key: accent. It was not an array", e.Message);

            e = Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetArray(JsonObjectWithAccent("[]"), "accent", true));
            Assert.Equal(ErrorStatusCode.RequiredPropertyMissing, e.StatusCode);
            Assert.Equal("Could not parse required key: accent. It was not found", e.Message);

            var arrayRet = ParseUtil.GetArray(JsonObjectWithAccent("[\"thing1\", \"thing2\"]"), "accent", true);
            Assert.Equal("thing1", arrayRet[0].GetValue<string>());
            Assert.Equal("thing2", arrayRet[1].GetValue<string>());
        }

        [Fact]
        public void GetBoolTests()
        {
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetBool(ValidJsonObject(), "accent", false, true));
            Assert.False(ParseUtil.GetBool(ValidJsonObject(), "accent", false, false));
            Assert.True(ParseUtil.GetBool(JsonObjectWithAccent("true"), "accent", false, true));
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetBool(JsonObjectWithAccent("[\"thing1\", \"thing2\"]"), "accent", false, true));
        }

        [Fact]
        public void GetIntTests()
        {
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetInt(ValidJsonObject(), "accent", 0, true));
            Assert.Equal(0, ParseUtil.GetInt(ValidJsonObject(), "accent", 0, false));
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetInt(JsonObjectWithAccent("\"Invalid\""), "accent", 0, true));
            Assert.Equal(1, ParseUtil.GetInt(JsonObjectWithAccent("1"), "accent", 0, false));
        }

        [Fact]
        public void GetOptionalIntTests()
        {
            Assert.Null(ParseUtil.GetOptionalInt(ValidJsonObject(), "accent"));
            Assert.Equal(1, ParseUtil.GetOptionalInt(JsonObjectWithAccent("1"), "accent").Value);
        }

        [Fact]
        public void GetUIntTests()
        {
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetUInt(ValidJsonObject(), "accent", 0, true));
            Assert.Equal(0U, ParseUtil.GetUInt(ValidJsonObject(), "accent", 0, false));
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetUInt(JsonObjectWithAccent("\"Invalid\""), "accent", 0, true));
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetUInt(JsonObjectWithAccent("-1"), "accent", 0, true));
            Assert.Equal(1U, ParseUtil.GetUInt(JsonObjectWithAccent("1"), "accent", 0, false));
        }

        [Fact]
        public void GetStringTests()
        {
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetString(ValidJsonObject(), "accent", true));
            Assert.Equal("", ParseUtil.GetString(ValidJsonObject(), "accent", false));
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetString(JsonObjectWithAccent("1"), "accent", true));
            Assert.Equal("Valid", ParseUtil.GetString(JsonObjectWithAccent("\"Valid\""), "accent", true));
        }

        [Fact]
        public void GetJsonStringTests()
        {
            Assert.Throws<AdaptiveCardParseException>(() => ParseUtil.GetJsonString(ValidJsonObject(), "accent", true));
            Assert.Equal("", ParseUtil.GetJsonString(ValidJsonObject(), "accent", false));
            Assert.Equal("1", ParseUtil.GetJsonString(JsonObjectWithAccent("1"), "accent", false));
            Assert.Equal("\"Valid\"", ParseUtil.GetJsonString(JsonObjectWithAccent("\"Valid\""), "accent", true));
        }

        [Fact]
        public void DuplicateKeysKeepLastValue()
        {
            // jsoncpp semantics (System.Text.Json's JsonNode.Parse would throw).
            var json = ParseUtil.GetJsonObjectFromString("{ \"type\": \"a\", \"type\": \"b\" }");
            Assert.Equal("b", json["type"].GetValue<string>());
        }

        [Fact]
        public void CommentsAndTrailingCommasAreAllowed()
        {
            var json = ParseUtil.GetJsonObjectFromString("{ /* comment */ \"a\": 1, }");
            Assert.Equal(1, json["a"].GetValue<int>());
        }
    }
}
