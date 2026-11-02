// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Ports of AdaptiveCardParseExceptionTest.cpp, FactUnitTest.cpp, TextParsingTest.cpp, EnumTest.cpp and
// ParserRegistrationTest.cpp. (Base64Test.cpp is not ported: the C# port uses System.Convert instead of
// AdaptiveBase64Util.)
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class AdaptiveCardParseExceptionTests
    {
        [Fact]
        public void AdaptiveCardParseExceptionTest()
        {
            var parseException = new AdaptiveCardParseException(ErrorStatusCode.InvalidJson, "error message");
            Assert.Equal("error message", parseException.Message);
            Assert.Equal(ErrorStatusCode.InvalidJson, parseException.StatusCode);
            Assert.Equal("error message", parseException.Reason);
        }

        [Fact]
        public void AdaptiveCardParseWarningTest()
        {
            var parseWarning = new AdaptiveWarning(WarningStatusCode.AssetLoadFailed, "error message");
            Assert.Equal(WarningStatusCode.AssetLoadFailed, parseWarning.StatusCode);
            Assert.Equal("error message", parseWarning.Message);
        }
    }

    public class FactTests
    {
        [Fact]
        public void DefineFromEmptyConstructor()
        {
            var emptyFact = new AdaptiveFact();
            Assert.Empty(emptyFact.Title);

            emptyFact.Title = "1 Example Title!";
            Assert.Equal("1 Example Title!", emptyFact.Title);
            emptyFact.Value = "1 Example Value!";
            Assert.Equal("1 Example Value!", emptyFact.Value);

            var jsonData = Canonical(emptyFact.ToJson());
            Assert.Equal("{\"title\":\"1 Example Title!\",\"value\":\"1 Example Value!\"}", jsonData);

            var context = new ParseContext(null, null);
            var parsedFact = AdaptiveFact.Deserialize(context, JsonNode.Parse(jsonData));
            Assert.Equal(emptyFact.Title, parsedFact.Title);
            Assert.Equal(emptyFact.Value, parsedFact.Value);
        }
    }

    public class TextParsingTests
    {
        private static string GetTextBlockText(string testString) => new AdaptiveTextBlock { Text = testString }.Text;

        [Fact]
        public void HtmlEncodingPositiveTest()
        {
            Assert.Equal("\"Foo & Bar\" <admin@example.com>", GetTextBlockText("\"Foo &amp; Bar\"&nbsp;&lt;admin@example.com&gt;"));
        }

        [Fact]
        public void HtmlEncodingAmpTest()
        {
            Assert.Equal("&nbsp;", GetTextBlockText("&amp;nbsp;"));
        }

        [Theory]
        [InlineData("some test text")]
        [InlineData("&foo;")]
        [InlineData("&am p;")]
        public void HtmlEncodingRoundtripTests(string testString)
        {
            Assert.Equal(testString, GetTextBlockText(testString));
        }
    }

    public class EnumTests
    {
        private static void EnumTest<T>(EnumMap<T> map, T validValue, string validString, Dictionary<string, T> reverseMap = null)
            where T : struct, Enum
        {
            Assert.Equal(validString, map.ToString(validValue));
            Assert.Equal(validValue, map.FromString(validString));
            Assert.Null(map.FromString("This is invalid."));
            if (reverseMap != null)
            {
                foreach (var kvp in reverseMap)
                {
                    Assert.Equal(kvp.Value, map.FromString(kvp.Key));
                }
            }
        }

        // ActionAlignment, ActionMode, ActionsOrientation and IconPlacement are host config enums and live in the
        // renderer; AdaptiveCardSchemaKey has no C# equivalent (property names are string literals).
        [Fact] public void ActionTypeTest() => EnumTest(EnumStrings.ActionType, ActionType.OpenUrl, "Action.OpenUrl");
        [Fact] public void CardElementTypeTest() => EnumTest(EnumStrings.ElementType, ElementType.AdaptiveCard, "AdaptiveCard");
        [Fact] public void ChoiceSetStyleTest() => EnumTest(EnumStrings.ChoiceSetStyle, ChoiceSetStyle.Filtered, "Filtered");
        [Fact] public void ContainerStyleTest() => EnumTest(EnumStrings.ContainerStyle, ContainerStyle.Emphasis, "Emphasis");
        [Fact] public void FontTypeTest() => EnumTest(EnumStrings.FontType, FontType.Monospace, "Monospace");
        [Fact] public void ForegroundColorTest() => EnumTest(EnumStrings.ForegroundColor, ForegroundColor.Accent, "Accent");
        [Fact] public void HeightTypeTest() => EnumTest(EnumStrings.HeightType, HeightType.Auto, "Auto");
        [Fact] public void HorizontalAlignmentTest() => EnumTest(EnumStrings.HAlignment, HAlignment.Center, "center");
        [Fact] public void ImageSizeTest() => EnumTest(EnumStrings.ImageSize, ImageSize.Large, "Large");
        [Fact] public void ImageStyleTest() => EnumTest(EnumStrings.ImageStyle, ImageStyle.Person, "person", new() { ["normal"] = ImageStyle.Default });
        [Fact] public void SeparatorThicknessTest() => EnumTest(EnumStrings.SeparatorThickness, SeparatorThickness.Thick, "thick");
        [Fact] public void SpacingTest() => EnumTest(EnumStrings.Spacing, Spacing.None, "none");
        [Fact] public void TextInputStyleTest() => EnumTest(EnumStrings.TextInputStyle, TextInputStyle.Password, "Password");
        [Fact] public void TextSizeTest() => EnumTest(EnumStrings.TextSize, TextSize.Large, "Large", new() { ["Normal"] = TextSize.Default });
        [Fact] public void TextWeightTest() => EnumTest(EnumStrings.TextWeight, TextWeight.Bolder, "Bolder", new() { ["Normal"] = TextWeight.Default });
        [Fact] public void VerticalContentAlignmentTest() => EnumTest(EnumStrings.VerticalContentAlignment, VerticalContentAlignment.Center, "Center");
        [Fact] public void CaseInsensitiveTest() => Assert.Equal(TextSize.ExtraLarge, EnumStrings.TextSize.FromString("eXtRaLaRgE"));
    }

    public class ParserRegistrationTests
    {
        private sealed class TestCustomElement : AdaptiveCardElementBase
        {
            public TestCustomElement(JsonObject value) : base(ElementType.Custom, "notRegisteredYet")
            {
                CustomImage = value["customProperty"]?.GetValue<string>();
            }

            public string CustomImage { get; }
        }

        private sealed class TestCustomAction : AdaptiveActionElementBase
        {
            public TestCustomAction(JsonObject value) : base(ActionType.Custom, "notRegisteredYet")
            {
            }
        }

        private sealed class TestCustomElementParser : IAdaptiveElementParser
        {
            public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers,
                AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings) => new TestCustomElement(inputJson);
        }

        private sealed class TestCustomActionParser : IAdaptiveActionParser
        {
            public IAdaptiveActionElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers,
                AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings) => new TestCustomAction(inputJson);
        }

        [Fact]
        public void ParserRegistrationTest()
        {
            var actionParser = new AdaptiveActionParserRegistration();
            var elementParser = new AdaptiveElementParserRegistration();
            const string elemType = "notRegisteredYet";

            Assert.Null(actionParser.Get(elemType));

            var customActionParser = new TestCustomActionParser();
            var customElementParser = new TestCustomElementParser();

            Assert.Throws<AdaptiveCardParseException>(() => actionParser.Set("Action.OpenUrl", customActionParser));
            Assert.Throws<AdaptiveCardParseException>(() => elementParser.Set("Container", customElementParser));

            actionParser.Set(elemType, customActionParser);
            Assert.Same(customActionParser, actionParser.Get(elemType));
            elementParser.Set(elemType, customElementParser);
            Assert.Same(customElementParser, elementParser.Get(elemType));

            var customActionParser2 = new TestCustomActionParser();
            actionParser.Set(elemType, customActionParser2);
            Assert.Same(customActionParser2, actionParser.Get(elemType));
            var customElementParser2 = new TestCustomElementParser();
            elementParser.Set(elemType, customElementParser2);
            Assert.Same(customElementParser2, elementParser.Get(elemType));

            actionParser.Remove(elemType);
            Assert.Null(actionParser.Get(elemType));
            actionParser.Remove(elemType);
            Assert.Null(actionParser.Get(elemType));
            elementParser.Remove(elemType);
            Assert.Null(elementParser.Get(elemType));
            elementParser.Remove(elemType);
            Assert.Null(elementParser.Get(elemType));

            Assert.NotNull(actionParser.Get("Action.OpenUrl"));
            Assert.Throws<AdaptiveCardParseException>(() => actionParser.Remove("Action.OpenUrl"));
            Assert.NotNull(actionParser.Get("Action.OpenUrl"));

            Assert.NotNull(elementParser.Get("Container"));
            Assert.Throws<AdaptiveCardParseException>(() => elementParser.Remove("Container"));
            Assert.NotNull(elementParser.Get("Container"));
        }

        [Fact]
        public void CustomParsersAreUsedDuringParsing()
        {
            var elementParser = new AdaptiveElementParserRegistration();
            elementParser.Set("notRegisteredYet", new TestCustomElementParser());
            var actionParser = new AdaptiveActionParserRegistration();
            actionParser.Set("notRegisteredYet", new TestCustomActionParser());

            var result = AdaptiveCard.FromJsonString(
                "{\"type\":\"AdaptiveCard\",\"version\":\"1.0\",\"body\":[{\"type\":\"notRegisteredYet\",\"customProperty\":\"img.png\"}]," +
                "\"actions\":[{\"type\":\"notRegisteredYet\"}]}",
                elementParser,
                actionParser);
            Assert.Empty(result.Errors);
            var element = Assert.IsType<TestCustomElement>(result.AdaptiveCard.Body[0]);
            Assert.Equal("img.png", element.CustomImage);
            Assert.Equal(ElementType.Custom, element.ElementType);
            Assert.IsType<TestCustomAction>(result.AdaptiveCard.Actions[0]);
        }
    }
}
