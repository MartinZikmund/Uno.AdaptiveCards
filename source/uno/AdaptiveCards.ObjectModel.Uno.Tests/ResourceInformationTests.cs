// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Port of ResourceInformationTests.cpp.
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class GatherImagesTests
    {
        private static void ValidateResourceInformation((string Url, string MimeType)[] expected, IReadOnlyList<AdaptiveRemoteResourceInformation> actual)
        {
            Assert.Equal(expected.Length, actual.Count);
            foreach (var e in expected)
            {
                Assert.Contains(actual, r => r.Url == e.Url && r.MimeType == e.MimeType);
            }
        }

        [Fact]
        public void CanGatherImages()
        {
            var expected = new[]
            {
                ("BackgroundImage.png", "image"), ("Image.png", "image"), ("ImageSet.Image1.png", "image"), ("ImageSet.Image2.png", "image"),
                ("Container.Image1.png", "image"), ("Container.Image2.png", "image"), ("ColumnSet.Column1.Image.png", "image"),
                ("ColumnSet.Column2.Image.png", "image"), ("ShowCard.Image.png", "image"), ("Media.Poster.png", "image"),
                ("Media1.mp4", "video/mp4"), ("Media2.ogg", "video/ogg"),
            };
            var (card, _) = Deserialize(@"{ ""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""backgroundImage"": ""BackgroundImage.png"",
                ""body"": [
                    { ""type"": ""Image"", ""url"": ""Image.png"" },
                    { ""type"": ""Media"", ""poster"": ""Media.Poster.png"", ""sources"": [ { ""mimeType"": ""video/mp4"", ""url"": ""Media1.mp4"" }, { ""mimeType"": ""video/ogg"", ""url"": ""Media2.ogg"" } ] },
                    { ""type"": ""ImageSet"", ""images"": [ { ""type"": ""Image"", ""url"": ""ImageSet.Image1.png"" }, { ""type"": ""Image"", ""url"": ""ImageSet.Image2.png"" } ] },
                    { ""type"": ""Container"", ""items"": [ { ""type"": ""Image"", ""url"": ""Container.Image1.png"" }, { ""type"": ""Image"", ""url"": ""Container.Image2.png"" } ] },
                    { ""type"": ""ColumnSet"", ""columns"": [
                        { ""type"": ""Column"", ""items"": [ { ""type"": ""Image"", ""url"": ""ColumnSet.Column1.Image.png"" } ] },
                        { ""type"": ""Column"", ""items"": [ { ""type"": ""Image"", ""url"": ""ColumnSet.Column2.Image.png"" } ] } ] } ],
                ""actions"": [ { ""type"": ""Action.ShowCard"", ""title"": ""Action.ShowCard"",
                    ""card"": { ""type"": ""AdaptiveCard"", ""body"": [ { ""type"": ""Image"", ""url"": ""ShowCard.Image.png"" } ] } } ] }", "1.0");
            ValidateResourceInformation(expected, card.GetResourceInformation());
        }

        private sealed class TestCustomElement : AdaptiveCardElementBase
        {
            private readonly string _customImage;

            public TestCustomElement(JsonObject value) : base(ElementType.Custom, "CustomTypeWithImage")
            {
                _customImage = value["customImageProperty"]?.GetValue<string>();
            }

            public override IReadOnlyList<AdaptiveRemoteResourceInformation> GetResourceInformation() =>
                new[] { new AdaptiveRemoteResourceInformation(_customImage, "image") };
        }

        private sealed class TestCustomAction : AdaptiveActionElementBase
        {
            private readonly string _customImage;

            public TestCustomAction(JsonObject value) : base(ActionType.Custom, "CustomActionWithImage")
            {
                _customImage = value["customImageProperty"]?.GetValue<string>();
            }

            public override IReadOnlyList<AdaptiveRemoteResourceInformation> GetResourceInformation() =>
                new[] { new AdaptiveRemoteResourceInformation(_customImage, "image") };
        }

        private sealed class TestCustomParser : IAdaptiveElementParser
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
        public void CanGatherCustomImages()
        {
            var expected = new[] { ("BackgroundImage.png", "image"), ("Image.png", "image"), ("Custom.png", "image"), ("Action.Custom.png", "image") };
            var elementRegistration = new AdaptiveElementParserRegistration();
            elementRegistration.Set("CustomTypeWithImage", new TestCustomParser());
            var actionRegistration = new AdaptiveActionParserRegistration();
            actionRegistration.Set("CustomActionWithImage", new TestCustomActionParser());

            var (card, _) = Deserialize(@"{ ""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""backgroundImage"": ""BackgroundImage.png"",
                ""body"": [ { ""type"": ""Image"", ""url"": ""Image.png"" }, { ""type"": ""CustomTypeWithImage"", ""customImageProperty"": ""Custom.png"" } ],
                ""actions"": [ { ""type"": ""CustomActionWithImage"", ""customImageProperty"": ""Action.Custom.png"" } ] }", "1.0", elementRegistration, actionRegistration);
            ValidateResourceInformation(expected, card.GetResourceInformation());
        }

        [Fact]
        public void CanGatherImagesInActions()
        {
            var expected = new[] { ("BackgroundImage.png", "image"), ("Image.png", "image"), ("SubmitAction.png", "image"), ("OpenUrl.png", "image") };
            var (card, _) = Deserialize(@"{ ""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"",
                ""backgroundImage"": ""BackgroundImage.png"",
                ""body"": [ { ""type"": ""Image"", ""url"": ""Image.png"" }, { ""type"": ""CustomTypeWithImage"", ""customImageProperty"": ""Custom.png"" } ],
                ""actions"": [ { ""type"": ""Action.Submit"", ""title"": ""Snooze"", ""iconUrl"": ""SubmitAction.png"" },
                    { ""type"": ""Action.Submit"", ""title"": ""Dismiss"" },
                    { ""type"": ""Action.OpenUrl"", ""title"": ""Action.OpenUrl"", ""iconUrl"": ""OpenUrl.png"", ""url"": ""http://adaptivecards.io"" } ] }", "1.0");
            ValidateResourceInformation(expected, card.GetResourceInformation());
        }
    }
}
