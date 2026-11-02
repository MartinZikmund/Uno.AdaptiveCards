// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Port of ObjectModelTest.cpp.
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class ObjectModelTests
    {
        private static IAdaptiveActionElement GetSelectAction(string json) =>
            ParseUtil.GetAction(new ParseContext(null, null), (JsonObject)JsonNode.Parse(json), "selectAction", false);

        [Fact]
        public void SelectActionEmptyJsonTest()
        {
            Assert.Null(ParseUtil.GetAction(new ParseContext(null, null), new JsonObject(), "selectAction", false));
        }

        [Fact]
        public void SelectActionNonExistentTest()
        {
            Assert.Null(GetSelectAction(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"": ""Container"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Now that we have defined the main rules"", ""wrap"": true } ] } ] }"));
        }

        [Fact]
        public void SelectActionInvalidTypeTest()
        {
            var selectAction = GetSelectAction(@"{ ""type"": ""ColumnSet"", ""selectAction"": { ""type"": ""Action.Invalid"", ""title"": ""Submit"", ""data"": { ""x"": 13 } } }");
            Assert.Equal(ActionType.Unsupported, selectAction.ActionType);
            Assert.IsType<AdaptiveUnsupportedAction>(selectAction);
        }

        [Fact]
        public void SelectActionOpenUrlTest()
        {
            var selectAction = GetSelectAction(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"": ""Container"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""text"", ""wrap"": true } ] } ],
                ""selectAction"": { ""type"": ""Action.OpenUrl"", ""title"": ""View"", ""url"": ""http://select-action.io"" } }");
            Assert.NotNull(selectAction);
            Assert.Equal("Action.OpenUrl", selectAction.ActionTypeString);
        }

        [Fact]
        public void SelectActionAnyJsonTest()
        {
            var selectAction = GetSelectAction(@"{ ""type"": ""ColumnSet"", ""selectAction"": { ""type"": ""Action.Submit"", ""title"": ""Submit"", ""data"": { ""x"": 13 } } }");
            Assert.NotNull(selectAction);
            Assert.Equal("Action.Submit", selectAction.ActionTypeString);
        }

        [Fact]
        public void DuplicateIdSimpleTest()
        {
            Assert.Throws<AdaptiveCardParseException>(() => Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""This card's has duplicate ids. Oh no!"" },
                    { ""type"": ""Input.Text"", ""style"": ""text"", ""id"": ""duplicate"" },
                    { ""type"": ""Input.Text"", ""style"": ""url"", ""id"": ""duplicate"" } ] }", "1.0"));
        }

        [Fact]
        public void DuplicateIdNestedTest()
        {
            Assert.Throws<AdaptiveCardParseException>(() => Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"": ""Input.Text"", ""placeholder"": ""Name"", ""style"": ""text"", ""maxLength"": 0, ""id"": ""duplicate"" } ],
                ""actions"": [ { ""type"": ""Action.Submit"", ""title"": ""Submit"", ""data"": { ""id"": ""1234567890"" } },
                    { ""type"": ""Action.ShowCard"", ""title"": ""Show Card"", ""card"": { ""type"": ""AdaptiveCard"",
                        ""body"": [ { ""type"": ""Input.Text"", ""placeholder"": ""enter comment"", ""style"": ""text"", ""maxLength"": 0, ""id"": ""duplicate"" } ],
                        ""actions"": [ { ""type"": ""Action.Submit"", ""title"": ""OK"" } ] } } ] }", "1.0"));
        }

        [Fact]
        public void MediaElementTest()
        {
            var (card, _) = Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"": ""Media"", ""poster"": ""http://adaptivecards.io/content/cats/1.png"", ""altText"": ""This is a video"",
                    ""sources"": [ { ""mimeType"": ""video/mp4"", ""url"": ""http://source1.mp4"" }, { ""mimeType"": ""video/avi"", ""url"": ""http://source2.avi"" } ] } ] }", "1.0");
            var media = Assert.IsType<AdaptiveMedia>(card.Body[0]);
            Assert.Equal(ElementType.Media, media.ElementType);
            Assert.Equal("Media", media.ElementTypeString);
            Assert.Equal("http://adaptivecards.io/content/cats/1.png", media.Poster);
            Assert.Equal("This is a video", media.AltText);
            Assert.Equal(2, media.Sources.Count);
            Assert.Equal("video/mp4", media.Sources[0].MimeType);
            Assert.Equal("http://source1.mp4", media.Sources[0].Url);
            Assert.Equal("video/avi", media.Sources[1].MimeType);
            Assert.Equal("http://source2.avi", media.Sources[1].Url);
        }

        [Fact]
        public void ShowCardSerialization()
        {
            var (mainCard, _) = Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""2.0"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""This card's action will show another card"" } ],
                ""actions"": [ { ""type"": ""Action.ShowCard"", ""title"": ""Action.ShowCard"", ""card"": { ""type"": ""AdaptiveCard"",
                    ""body"": [ { ""type"": ""TextBlock"", ""text"": ""What do you think?"" } ],
                    ""actions"": [ { ""type"": ""Action.Submit"", ""title"": ""Neat!"" } ] } } ] }", "2.0");
            var showCard = ((AdaptiveShowCardAction)mainCard.Actions[0]).Card;
            ValidateShowCard(showCard);
            var (roundTripped, _) = Deserialize(showCard.ToJson().ToJsonString(), "2.0");
            ValidateShowCard(roundTripped);
        }

        private static void ValidateShowCard(AdaptiveCard showCard)
        {
            Assert.Equal("2.0", showCard.Version);
            Assert.Single(showCard.Body);
            Assert.Equal(ElementType.TextBlock, showCard.Body[0].ElementType);
            Assert.Equal("What do you think?", ((AdaptiveTextBlock)showCard.Body[0]).Text);
            Assert.Single(showCard.Actions);
            Assert.Equal(ActionType.Submit, showCard.Actions[0].ActionType);
            Assert.Equal("Neat!", showCard.Actions[0].Title);
        }

        private const string Choices = @"""choices"": [ { ""title"": ""Red"", ""value"": ""1"" }, { ""title"": ""Green"", ""value"": ""2"" }, { ""title"": ""Blue"", ""value"": ""3"" } ]";

        [Fact]
        public void ChoiceSetWrapParsingTest()
        {
            var (card, _) = Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""Input.ChoiceSet"", ""id"": ""myColor"", ""style"": ""compact"", ""isMultiSelect"": true, ""wrap"": true, ""value"": ""1"", " + Choices + @" },
                    { ""type"": ""Input.ChoiceSet"", ""id"": ""myColor2"", ""style"": ""expanded"", ""isMultiSelect"": false, ""value"": ""1"", " + Choices + @" } ] }", "1.2");
            Assert.True(((AdaptiveChoiceSetInput)card.Body[0]).Wrap);
            Assert.False(((AdaptiveChoiceSetInput)card.Body[1]).Wrap);
        }

        [Fact]
        public void ToggleInputWrapParsingTest()
        {
            var (card, _) = Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""Input.Toggle"", ""id"": ""acceptTerms"", ""wrap"": true, ""title"": ""I accept the terms and agreements"", ""value"": ""true"", ""valueOn"": ""true"", ""valueOff"": ""false"" },
                    { ""type"": ""Input.Toggle"", ""id"": ""acceptTerms2"", ""title"": ""I accept the terms and agreements"", ""value"": ""true"", ""valueOn"": ""true"", ""valueOff"": ""false"" } ] }", "1.2");
            Assert.True(((AdaptiveToggleInput)card.Body[0]).Wrap);
            Assert.False(((AdaptiveToggleInput)card.Body[1]).Wrap);
        }

        [Fact]
        public void ChoiceSetOptionalChoicesTest()
        {
            var (card, context) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.5"",
                ""body"": [ { ""type"": ""Input.ChoiceSet"", ""id"": ""1"", ""placeholder"": ""Placeholder text"" } ] }", "1.5");
            Assert.Empty(context.Warnings);
            var choiceSet = Assert.IsType<AdaptiveChoiceSetInput>(Assert.Single(card.Body));
            Assert.Equal(ElementType.ChoiceSetInput, choiceSet.ElementType);
            Assert.Empty(choiceSet.Choices);

            // Deviation: the C# port serializes "placeholder" (the shared model dropped it on serialization).
            Assert.Equal(
                "{\"actions\":[],\"body\":[{\"id\":\"1\",\"placeholder\":\"Placeholder text\",\"style\":\"Compact\",\"type\":\"Input.ChoiceSet\"}],\"type\":\"AdaptiveCard\",\"version\":\"1.5\"}",
                Serialize(card));
        }

        [Fact]
        public void ImplicitColumnTypeTest()
        {
            Deserialize(@"{ ""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"":""ColumnSet"", ""columns"": [ { ""type"": ""Column"", ""items"": [] }, { ""items"": [] } ] } ] }", "1.0");
            var e = Assert.Throws<AdaptiveCardParseException>(() => Deserialize(@"{ ""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"":""ColumnSet"", ""columns"": [ { ""type"": ""Elephant"", ""items"": [] } ] } ] }", "1.0"));
            Assert.Equal(ErrorStatusCode.InvalidPropertyValue, e.StatusCode);
            Assert.Equal("Unable to parse element of type Elephant", e.Message);
        }

        [Fact]
        public void ImplicitImageTypeInImageSetTest()
        {
            Deserialize(@"{ ""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"":""ImageSet"", ""images"": [ { ""type"": ""Image"", ""url"": ""http://adaptivecards.io/content/cats/1.png"" },
                    { ""url"": ""http://adaptivecards.io/content/cats/1.png"" } ] } ] }", "1.0");
            var e = Assert.Throws<AdaptiveCardParseException>(() => Deserialize(@"{ ""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"":""ImageSet"", ""images"": [ { ""type"": ""Elephant"", ""url"": ""http://adaptivecards.io/content/cats/1.png"" } ] } ] }", "1.0"));
            Assert.Equal(ErrorStatusCode.InvalidPropertyValue, e.StatusCode);
            Assert.Equal("Unable to parse element of type Elephant", e.Message);
        }

        [Fact]
        public void TextBlockStyleParsingTest()
        {
            var (card, _) = Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""id"": ""heading"", ""style"": ""heading"", ""text"": ""hello"" },
                    { ""type"": ""TextBlock"", ""id"": ""heading2"", ""style"": ""Heading"", ""text"": ""hello"" },
                    { ""type"": ""TextBlock"", ""id"": ""explicit default"", ""style"": ""Default"", ""text"": ""hello"" },
                    { ""type"": ""TextBlock"", ""id"": ""invalid-heading"", ""style"": ""Footer"", ""text"": ""hello"" },
                    { ""type"": ""TextBlock"", ""id"": ""implicit default"", ""text"": ""hello"" } ] }", "1.2");
            var expectedStyles = new TextStyle?[] { TextStyle.Heading, TextStyle.Heading, TextStyle.Default, null, null };
            for (int i = 0; i < card.Body.Count; i++)
            {
                Assert.Equal(expectedStyles[i], ((AdaptiveTextBlock)card.Body[i]).Style);
            }
        }

        [Fact]
        public void PasswordStyleParseTest()
        {
            var (card, _) = Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.5"",
                ""body"": [ { ""type"": ""Input.Text"", ""id"": ""plain"", ""label"": ""Plaintext"" },
                    { ""type"": ""Input.Text"", ""id"": ""password"", ""style"": ""passWORD"", ""label"": ""Password"" } ] }", "1.5");
            Assert.Equal(2, card.Body.Count);
            Assert.Equal(TextInputStyle.Text, ((AdaptiveTextInput)card.Body[0]).TextInputStyle);
            Assert.Equal(TextInputStyle.Password, ((AdaptiveTextInput)card.Body[1]).TextInputStyle);
        }

        [Fact]
        public void PasswordWithMultilineParseTest()
        {
            var (card, context) = Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.5"",
                ""body"": [ { ""type"": ""Input.Text"", ""id"": ""theId"", ""label"": ""Password"", ""style"": ""password"", ""isMultiline"": true } ] }", "1.5");
            var warning = Assert.Single(context.Warnings);
            Assert.Equal(WarningStatusCode.InvalidValue, warning.StatusCode);
            Assert.Equal("Input.Text ignores isMultiline when using password style", warning.Message);
            var theInput = Assert.IsType<AdaptiveTextInput>(Assert.Single(card.Body));
            Assert.Equal(TextInputStyle.Password, theInput.TextInputStyle);
            Assert.True(theInput.IsMultiline);
            Assert.True(card.ToJson()["body"][0]["isMultiline"].GetValue<bool>());
        }

        [Fact]
        public void UnsupportedSchemaVersionYieldsFallbackCard()
        {
            var (card, context) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""version"": ""99.0"", ""fallbackText"": ""Too new"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""hidden"" } ] }", "1.6");
            Assert.Equal(WarningStatusCode.UnsupportedSchemaVersion, Assert.Single(context.Warnings).StatusCode);
            Assert.Equal("Too new", ((AdaptiveTextBlock)Assert.Single(card.Body)).Text);
            Assert.Equal("Too new", card.Speak);
        }
    }
}
