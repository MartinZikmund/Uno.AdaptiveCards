// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Ports of CarouselTest.cpp and FontStylesUnitTest.cpp.
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class CarouselTests
    {
        [Fact]
        public void Parse()
        {
            var (card, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""version"": ""1.6"",
                ""body"": [ { ""type"": ""Carousel"", ""pages"": [ { ""type"": ""CarouselPage"", ""id"": ""firstCarouselPage"",
                    ""items"": [ { ""type"": ""Image"", ""url"": ""https://adaptivecards.io/content/cats/1.png"", ""altText"": ""Cat"", ""size"": ""medium"" } ] } ] } ],
                ""actions"": [ { ""type"": ""Action.OpenUrl"", ""title"": ""See more"", ""url"": ""https://adaptivecards.io"" } ] }", "1.6");
            Assert.Single(card.Body);
            Assert.Single(card.Actions);
            var carousel = Assert.IsType<AdaptiveCarousel>(card.Body[0]);
            Assert.Equal("Carousel", carousel.ElementTypeString);
            Assert.Equal(ElementType.Carousel, carousel.ElementType);
            var page = carousel.Pages[0];
            Assert.Equal("CarouselPage", page.ElementTypeString);
            Assert.Equal(ElementType.CarouselPage, page.ElementType);
            var image = Assert.IsType<AdaptiveImage>(page.Items[0]);
            Assert.Equal("Image", image.ElementTypeString);
        }

        [Fact]
        public void ParseWithProhibitedElements()
        {
            var (card, context) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""version"": ""1.6"",
                ""body"": [ { ""type"": ""Carousel"", ""pages"": [ { ""type"": ""CarouselPage"", ""id"": ""firstCarouselPage"",
                    ""items"": [ { ""type"": ""Carousel"", ""pages"": [ { ""type"": ""CarouselPage"", ""id"": ""123"",
                        ""items"": [ { ""type"": ""Image"", ""url"": ""https://adaptivecards.io/content/cats/1.png"", ""altText"": ""Cat"", ""size"": ""medium"" } ] } ] } ] } ] } ] }", "1.6");
            var warning = Assert.Single(context.Warnings);
            Assert.Equal("type: Carousel is prohibited", warning.Message);
            Assert.Single(card.Body);
            var carousel = Assert.IsType<AdaptiveCarousel>(card.Body[0]);
            Assert.Equal("CarouselPage", carousel.Pages[0].ElementTypeString);
            Assert.Empty(carousel.Pages[0].Items);
        }

        private const string ShowCardSelectActionJson = @"{ ""type"": ""AdaptiveCard"", ""version"": ""1.6"",
            ""body"": [ { ""type"": ""Carousel"", ""initialPage"": 1, ""loop"": false, ""heightInPixels"": ""100px"", ""timer"": 5000,
                ""pages"": [ { ""type"": ""CarouselPage"", ""id"": ""firstCarouselPage"", ""rtl"": true,
                    ""items"": [ { ""type"": ""Container"", ""items"": [ { ""type"": ""TextBlock"", ""wrap"": true, ""text"": ""This is a container with ShowCard action"" } ],
                        ""selectAction"": { ""type"": ""Action.ShowCard"", ""title"": ""Action.ShowCard"",
                            ""card"": { ""type"": ""AdaptiveCard"", ""body"": [ { ""type"": ""TextBlock"", ""text"": ""What do you think?"" } ] } } } ] } ] } ] }";

        [Fact]
        public void ParseWithShowCardSelectAction()
        {
            var (card, context) = Deserialize(ShowCardSelectActionJson, "1.6");
            var warning = Assert.Single(context.Warnings);
            Assert.Equal("type: Action.ShowCard is prohibited", warning.Message);
            Assert.Single(card.Body);
            var carousel = Assert.IsType<AdaptiveCarousel>(card.Body[0]);
            Assert.Equal("Carousel", carousel.ElementTypeString);
            Assert.False(carousel.AutoLoop.Value);
            Assert.Equal(1U, carousel.InitialPage.Value);
            Assert.Equal(100U, carousel.HeightInPixels);
            Assert.Equal(5000UL, carousel.Timer.Value);
            var page = carousel.Pages[0];
            Assert.True(page.Rtl.Value);
            Assert.Equal("CarouselPage", page.ElementTypeString);
            Assert.Empty(page.Items);
        }

        [Fact]
        public void ParseWithShowCardInActionSet()
        {
            var (card, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""version"": ""1.6"",
                ""body"": [ { ""type"": ""Carousel"", ""pages"": [ { ""type"": ""CarouselPage"", ""id"": ""firstCarouselPage"",
                    ""items"": [ { ""type"": ""ActionSet"", ""items"": [ { ""type"": ""Action.ShowCard"", ""title"": ""Action.ShowCard"",
                        ""card"": { ""type"": ""AdaptiveCard"", ""body"": [ { ""type"": ""TextBlock"", ""text"": ""What do you think?"" } ] } } ] } ] } ] } ] }", "1.6");
            Assert.Single(card.Body);
            var carousel = Assert.IsType<AdaptiveCarousel>(card.Body[0]);
            var page = carousel.Pages[0];
            Assert.Single(page.Items);
            var actionSet = Assert.IsType<AdaptiveActionSet>(page.Items[0]);
            Assert.Empty(actionSet.Actions);
        }

        [Fact]
        public void Serialize()
        {
            var (testCard, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""version"": ""1.6"",
                ""body"": [ { ""type"": ""Carousel"", ""initialPage"": 1, ""loop"": false, ""heightInPixels"": ""100px"", ""timer"": 5000,
                    ""pages"": [ { ""type"": ""CarouselPage"", ""id"": ""firstCarouselPage"",
                        ""items"": [ { ""type"": ""TextBlock"", ""wrap"": true, ""text"": ""Hello World!"" } ] } ] } ] }", "1.6");
            var (card, _) = Deserialize(testCard.ToJson().ToJsonString(), "1.6");
            Assert.Single(card.Body);
            var carousel = Assert.IsType<AdaptiveCarousel>(card.Body[0]);
            Assert.Equal("Carousel", carousel.ElementTypeString);
            Assert.False(carousel.AutoLoop.Value);
            Assert.Equal(1U, carousel.InitialPage.Value);
            Assert.Equal(100U, carousel.HeightInPixels);
            Assert.Equal(5000UL, carousel.Timer.Value);
            var page = carousel.Pages[0];
            Assert.Equal("CarouselPage", page.ElementTypeString);
            Assert.Single(page.Items);
            var textBlock = Assert.IsType<AdaptiveTextBlock>(page.Items[0]);
            Assert.True(textBlock.Wrap);
            Assert.Equal("Hello World!", textBlock.Text);
        }
    }

    public class FontTypeTests
    {
        [Fact]
        public void DefineFromEmptyConstructor()
        {
            var emptyTB = new AdaptiveTextBlock();
            Assert.False(emptyTB.FontType.HasValue);
            emptyTB.FontType = FontType.Default;
            Assert.Equal(FontType.Default, emptyTB.FontType);
            emptyTB.FontType = FontType.Monospace;
            Assert.Equal(FontType.Monospace, emptyTB.FontType);
        }

        [Fact]
        public void EmptyTextBlockSerialization()
        {
            var emptyTB = new AdaptiveTextBlock();
            var jsonData = Serialize(emptyTB);
            Assert.Equal("{\"text\":\"\",\"type\":\"TextBlock\"}", jsonData);
            var context = new ParseContext(null, null);
            var parsed = AdaptiveTextBlock.Deserialize(context, (JsonObject)JsonNode.Parse(jsonData));
            Assert.Equal(emptyTB.FontType, parsed.FontType);
        }
    }
}
