// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Port of FallbackTests.cpp.
using System.Collections.Generic;
using System.Linq;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class FallbackTests
    {
        private static void AssertParseError(string cardStr, ErrorStatusCode code, string reason)
        {
            var e = Assert.Throws<AdaptiveCardParseException>(() => Deserialize(cardStr, "1.2"));
            Assert.Equal(code, e.StatusCode);
            Assert.Equal(reason, e.Message);

            // The public API reports the same failure as an error instead of throwing.
            var result = AdaptiveCard.FromJsonString(cardStr);
            Assert.Null(result.AdaptiveCard);
            Assert.Equal(code, Assert.Single(result.Errors).StatusCode);
        }

        [Fact]
        public void ElementFallbackSerializationTest()
        {
            var (card, _) = Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""TextBlock with fallback drop"", ""fallback"": ""drop"" },
                    { ""type"": ""TextBlock"", ""text"": ""TextBlock with fallback content"", ""fallback"": { ""type"": ""TextBlock"", ""text"": ""fallback content goes here"" } } ] }", "1.2");
            var textBlock1 = (AdaptiveTextBlock)card.Body[0];
            Assert.Equal(FallbackType.Drop, textBlock1.FallbackType);
            var textBlock2 = (AdaptiveTextBlock)card.Body[1];
            Assert.Equal(FallbackType.Content, textBlock2.FallbackType);
            Assert.Equal("fallback content goes here", ((AdaptiveTextBlock)textBlock2.FallbackContent).Text);
            Assert.Equal(
                "{\"actions\":[],\"body\":[{\"fallback\":\"drop\",\"text\":\"TextBlock with fallback drop\",\"type\":\"TextBlock\"},{\"fallback\":{\"text\":\"fallback content goes here\",\"type\":\"TextBlock\"},\"text\":\"TextBlock with fallback content\",\"type\":\"TextBlock\"}],\"type\":\"AdaptiveCard\",\"version\":\"1.2\"}",
                Serialize(card));
        }

        [Fact]
        public void ActionFallbackSerializationTest()
        {
            var (card, _) = Deserialize(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""test text"" } ],
                ""actions"": [ { ""type"": ""Action.Submit"", ""title"": ""Drop Test"", ""fallback"": ""drop"" },
                    { ""type"": ""Action.OpenUrl"", ""title"": ""Content Test"", ""url"": ""http://example.com/"",
                      ""fallback"": { ""type"": ""Action.OpenUrl"", ""title"": ""Fallback content"", ""url"": ""http://example.com/fallback/"" } } ] }", "1.2");
            var submitAction = (AdaptiveSubmitAction)card.Actions[0];
            Assert.Equal(FallbackType.Drop, submitAction.FallbackType);
            var openUrlAction = (AdaptiveOpenUrlAction)card.Actions[1];
            Assert.Equal(FallbackType.Content, openUrlAction.FallbackType);
            var fallbackAction = (AdaptiveOpenUrlAction)openUrlAction.FallbackContent;
            Assert.Equal(ActionType.OpenUrl, fallbackAction.ActionType);
            Assert.Equal("Fallback content", fallbackAction.Title);
            Assert.Equal("http://example.com/fallback/", fallbackAction.Url.ToString());

            // Deviation: the C# port omits "role" when it equals the per-type default (Link for Action.OpenUrl);
            // the shared model emitted "role":"Link".
            Assert.Equal(
                "{\"actions\":[{\"fallback\":\"drop\",\"title\":\"Drop Test\",\"type\":\"Action.Submit\"},{\"fallback\":{\"title\":\"Fallback content\",\"type\":\"Action.OpenUrl\",\"url\":\"http://example.com/fallback/\"},\"title\":\"Content Test\",\"type\":\"Action.OpenUrl\",\"url\":\"http://example.com/\"}],\"body\":[{\"text\":\"test text\",\"type\":\"TextBlock\"}],\"type\":\"AdaptiveCard\",\"version\":\"1.2\"}",
                Serialize(card));
        }

        [Fact]
        public void FallbackSimpleDuplicateTest()
        {
            AssertParseError(@"{ ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""1"", ""id"": ""1"" },
                    { ""type"": ""Container"", ""id"": ""2"", ""items"": [
                        { ""type"": ""TextBlock"", ""text"": ""3"", ""id"": ""3"",
                          ""fallback"": { ""type"": ""TextBlock"", ""text"": ""3 fallback (this is okay)"", ""id"": ""3"" } },
                        { ""type"": ""TextBlock"", ""text"": ""4"", ""id"": ""4"",
                          ""fallback"": { ""type"": ""Container"", ""id"": ""4"", ""items"": [
                              { ""type"": ""TextBlock"", ""text"": ""4 fallback container item. id is 3. this is *not* okay"", ""id"": ""3"" } ] } } ] } ] }",
                ErrorStatusCode.IdCollision, "Collision detected for id '3'");
        }

        private const string DeepIdCard = @"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
            ""body"": [
                { ""type"": ""Container"", ""id"": ""A"", ""items"": [
                    { ""type"": ""Container"", ""id"": ""B"",
                      ""fallback"": { ""type"": ""Container"", ""id"": ""B"", ""items"": [
                          { ""type"": ""TextBlock"", BID1 ""text"": ""B Container fallback textblock"" },
                          { ""type"": ""Image"", BID2 ""url"": ""http://adaptivecards.io/content/cats/1.png"" } ] },
                      ""items"": [ { ""type"": ""ColumnSet"", ""id"": ""C"", ""columns"": [ { ""type"": ""Column"", ""id"": ""D"", ""items"": [
                          { ""type"": ""Graph"", ""id"": ""E"", ""someProperty"": ""blah"",
                            ""fallback"": { ""type"": ""Container"", ""id"": ""E"", ""items"": [
                                { ""type"": ""Image"", ""id"": ""I"", ""url"": ""http://adaptivecards.io/content/cats/2.png"" },
                                { ""type"": ""TextBlock"", ""id"": ""J"", ""text"": ""C ColumnSet fallback textblock"" } ] } } ] } ] } ] } ] },
                { ""type"": ""TextBlock"", ""id"": ""F"", ""text"": ""F TextBlock"" },
                { ""type"": ""Input.Text"", ""id"": ""G"", GFALLBACK ""placeholder"": ""G Input.Text"" },
                { ""type"": ""Graph"", ""id"": ""H"", ""someProperty"": ""foo"",
                  ""fallback"": { ""type"": ""Container"", ""id"": ""HFALLBACKID"", ""items"": [ { ""type"": ""TextBlock"", ""id"": ""K"", ""text"": ""H Graph fallback TextBlock"" } ] } } ] }";

        [Fact]
        public void ElementFallbackDeepIdCollision()
        {
            var card = DeepIdCard.Replace("BID1", "").Replace("BID2", "").Replace("GFALLBACK", "").Replace("HFALLBACKID", "E");
            AssertParseError(card, ErrorStatusCode.IdCollision, "Collision detected for id 'E'");
        }

        [Fact]
        public void ElementFallbackDeepIdOkay()
        {
            var cardStr = DeepIdCard.Replace("BID1", "\"id\": \"C\",").Replace("BID2", "\"id\": \"Z\",")
                .Replace("GFALLBACK", "\"fallback\": \"drop\",").Replace("HFALLBACKID", "L");
            var (card, _) = Deserialize(cardStr, "1.2");
            var body = card.Body;

            var containerA = (AdaptiveContainer)body[0];
            Assert.Equal("A", containerA.Id);
            var containerAB = (AdaptiveContainer)containerA.Items[0];
            Assert.Equal("B", containerAB.Id);
            Assert.Equal(FallbackType.Content, containerAB.FallbackType);
            Assert.Equal("B", ((AdaptiveContainer)containerAB.FallbackContent).Id);
            var containerABC = (AdaptiveColumnSet)containerAB.Items[0];
            Assert.Equal(FallbackType.None, containerABC.FallbackType);
            var containerABCD = containerABC.Columns[0];
            var containerABCDE = Assert.IsType<AdaptiveUnsupportedElement>(containerABCD.Items[0]);
            Assert.Equal("Graph", containerABCDE.ElementTypeString);
            Assert.Equal("E", containerABCDE.Id);
            Assert.Equal(FallbackType.Content, containerABCDE.FallbackType);
            Assert.Equal("E", ((AdaptiveContainer)containerABCDE.FallbackContent).Id);

            var inputG = (AdaptiveTextInput)body[2];
            Assert.Equal("G", inputG.Id);
            Assert.Equal(FallbackType.Drop, inputG.FallbackType);

            var unknownH = Assert.IsType<AdaptiveUnsupportedElement>(body[3]);
            Assert.Equal("H", unknownH.Id);
            Assert.Equal(FallbackType.Content, unknownH.FallbackType);
            Assert.Equal("L", ((AdaptiveContainer)unknownH.FallbackContent).Id);
        }

        [Fact]
        public void DuplicateFallbackIdSimple()
        {
            AssertParseError(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""Container"",
                    ""fallback"": { ""type"": ""Container"", ""items"": [
                        { ""type"": ""TextBlock"", ""id"": ""B"", ""text"": ""First B fallback textblock"" },
                        { ""type"": ""TextBlock"", ""id"": ""B"", ""text"": ""Second B fallback textblock"" } ] },
                    ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Non-fallback textblock"" } ] } ] }",
                ErrorStatusCode.IdCollision, "Collision detected for id 'B'");
        }

        [Fact]
        public void DuplicateFallbackId()
        {
            AssertParseError(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
                ""body"": [
                    { ""type"": ""TextBlock"", ""text"": ""First textblock"", ""fallback"": { ""type"": ""TextBlock"", ""id"": ""duplicate"", ""text"": ""First fallback textblock"" } },
                    { ""type"": ""TextBlock"", ""text"": ""Second textblock"", ""fallback"": { ""type"": ""TextBlock"", ""id"": ""duplicate"", ""text"": ""Second fallback textblock"" } } ] }",
                ErrorStatusCode.IdCollision, "Collision detected for id 'duplicate'");
        }

        [Fact]
        public void DuplicateFallbackIdNestedOk()
        {
            Deserialize(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""First textblock"", ""id"": ""duplicate"",
                    ""fallback"": { ""type"": ""TextBlock"", ""id"": ""duplicate"", ""text"": ""First fallback textblock"",
                        ""fallback"": { ""type"": ""TextBlock"", ""id"": ""duplicate"", ""text"": ""Second fallback textblock"" } } } ] }", "1.2");
        }

        [Fact]
        public void RequiresAndFallbackSerialization()
        {
            var (card, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""This element requires version 1.2"",
                    ""requires"": { ""adaptiveCards"": ""1.2"", ""foobar"": ""2"" },
                    ""fallback"": { ""type"": ""TextBlock"", ""text"": ""This element has no version requirement"" } } ] }", "1.2");
            Assert.Equal(
                "{\"actions\":[],\"body\":[{\"fallback\":{\"text\":\"This element has no version requirement\",\"type\":\"TextBlock\"},\"requires\":{\"adaptiveCards\":\"1.2.0.0\",\"foobar\":\"2.0.0.0\"},\"text\":\"This element requires version 1.2\",\"type\":\"TextBlock\"}],\"type\":\"AdaptiveCard\",\"version\":\"1.2\"}",
                Serialize(card));
        }

        /// <summary>Shared-model BaseElement::MeetsRequirements semantics (the renderer implements the same check).</summary>
        private static bool MeetsRequirements(IAdaptiveCardElement element, IDictionary<string, string> features)
        {
            foreach (var requirement in element.Requirements)
            {
                if (!features.TryGetValue(requirement.Name, out var featureVersion) || string.IsNullOrEmpty(featureVersion))
                {
                    return false;
                }

                if (new SemanticVersion(featureVersion) < new SemanticVersion(requirement.Version))
                {
                    return false;
                }
            }

            return true;
        }

        [Fact]
        public void RequiresValidation()
        {
            var (card, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""This element requires version 1.2"", ""requires"": { ""adaptiveCards"": ""1.2"", ""foobar"": ""2"" } },
                    { ""type"": ""TextBlock"", ""text"": ""This element has no explicit requirements"" } ] }", "1.2");
            var textBlock = card.Body[0];
            var textBlockNoRequires = card.Body[1];

            Assert.Equal(2, textBlock.Requirements.Count);
            Assert.Contains(textBlock.Requirements, r => r.Name == "adaptiveCards" && r.Version == "1.2");
            Assert.Contains(textBlock.Requirements, r => r.Name == "foobar" && r.Version == "2");

            var features = new Dictionary<string, string> { ["adaptiveCards"] = "1.6", ["foobar"] = "2" };
            Assert.True(MeetsRequirements(textBlock, features));
            Assert.True(MeetsRequirements(textBlockNoRequires, features));
            features.Remove("foobar");
            Assert.False(MeetsRequirements(textBlock, features));
            Assert.True(MeetsRequirements(textBlockNoRequires, features));
            features["foobar"] = "1.9.9.9";
            Assert.False(MeetsRequirements(textBlock, features));
            Assert.True(MeetsRequirements(textBlockNoRequires, features));
            features["foobar"] = "99";
            Assert.True(MeetsRequirements(textBlock, features));
            Assert.True(MeetsRequirements(textBlockNoRequires, features));
        }

        [Fact]
        public void RequiresAnyVersion()
        {
            var (card, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""t"", ""requires"": { ""foobar"": ""*"" } } ] }", "1.2");
            var requirement = Assert.Single(card.Body[0].Requirements);
            Assert.Equal("0", requirement.Version);
        }

        [Fact]
        public void InvalidRequiresVersion()
        {
            AssertParseError(@"{ ""type"": ""AdaptiveCard"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""t"", ""requires"": { ""foobar"": ""abc"" } } ] }",
                ErrorStatusCode.InvalidPropertyValue, "Invalid version in requires value: 'abc'");
        }

        [Fact]
        public void NestedFallbacksSerialization()
        {
            var (card, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""GraphV2"", ""fallback"": { ""type"": ""Graph"", ""fallback"": { ""type"": ""TextBlock"", ""text"": ""No `Graph` support?"" } } } ] }", "1.2");
            Assert.Equal(
                "{\"actions\":[],\"body\":[{\"fallback\":{\"fallback\":{\"text\":\"No `Graph` support?\",\"type\":\"TextBlock\"},\"type\":\"Graph\"},\"type\":\"GraphV2\"}],\"type\":\"AdaptiveCard\",\"version\":\"1.2\"}",
                Serialize(card));
        }

        [Fact]
        public void InvalidFallbackStringBody()
        {
            AssertParseError(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""textblock"", ""fallback"": ""steve"" } ] }",
                ErrorStatusCode.InvalidPropertyValue, "The only valid string value for the fallback property is 'drop'.");
        }

        [Fact]
        public void InvalidFallbackStringAction()
        {
            AssertParseError(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""textblock"" } ],
                ""actions"": [ { ""type"": ""Blahblah"", ""fallback"": ""steve"" } ] }",
                ErrorStatusCode.InvalidPropertyValue, "The only valid string value for the fallback property is 'drop'.");
        }

        [Fact]
        public void InvalidFallbackArray()
        {
            AssertParseError(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.2"",
                ""body"": [ { ""type"": ""TextBlock"", ""text"": ""textblock"", ""fallback"": [ { ""blah"": ""2"" } ] } ] }",
                ErrorStatusCode.InvalidPropertyValue, "Invalid value for fallback");
        }

        [Fact]
        public void ColumnFallback()
        {
            var (card, context) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"": ""ColumnSet"", ""columns"": [ { ""type"": ""Column"", ""items"": [ { ""type"": ""Graph"" } ],
                    ""fallback"": { ""type"": ""Column"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Fallback"" } ] } } ] } ] }", "1.2");
            Assert.Empty(context.Warnings);
            var column = ((AdaptiveColumnSet)card.Body[0]).Columns[0];
            Assert.Equal(FallbackType.Content, column.FallbackType);
            Assert.Equal("Column", column.FallbackContent.ElementTypeString);
        }

        [Fact]
        public void ColumnBadFallback()
        {
            var (card, context) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""body"": [ { ""type"": ""ColumnSet"", ""columns"": [ { ""type"": ""Column"", ""items"": [ { ""type"": ""Graph"" } ],
                    ""fallback"": { ""type"": ""TextBlock"", ""text"": ""Fallback"" } } ] } ] }", "1.2");
            var warning = Assert.Single(context.Warnings);
            Assert.Equal(WarningStatusCode.UnknownElementType, warning.StatusCode);
            Assert.Equal("Column Fallback must be a Column. Fallback content dropped.", warning.Message);
            var column = ((AdaptiveColumnSet)card.Body[0]).Columns[0];
            Assert.Equal(FallbackType.None, column.FallbackType);
        }
    }
}
