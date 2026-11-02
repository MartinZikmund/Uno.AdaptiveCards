// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Ports of ImageBackgroundColorTest.cpp and UnsupportedtypesParsingTest.cpp.
using System.Linq;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class ImageBackgroundColorTests
    {
        private static string GetBackgroundColor(string backgroundColorProperty)
        {
            var json = "{\"$schema\":\"http://adaptivecards.io/schemas/adaptive-card.json\",\"type\":\"AdaptiveCard\",\"version\":\"1.0\"," +
                "\"body\":[{\"type\":\"Image\",\"url\":\"Image\"" + backgroundColorProperty + "}]}";
            var (card, _) = Deserialize(json, "1.0");
            return ((AdaptiveImage)card.Body.First()).BackgroundColor;
        }

        [Fact] public void NoBackgroundColorTest() => Assert.Equal("", GetBackgroundColor(""));
        [Fact] public void AARRGGBBTest() => Assert.Equal("#ABF65314", GetBackgroundColor(",\"backgroundColor\":\"#ABF65314\""));
        [Fact] public void RRGGBBTest() => Assert.Equal("#FF00A1F1", GetBackgroundColor(",\"backgroundColor\":\"#00A1F1\""));
        [Fact] public void LowerCaseCharactersTest() => Assert.Equal("#ffa65314", GetBackgroundColor(",\"backgroundColor\":\"#ffa65314\""));
        [Fact] public void InvalidLengthTest() => Assert.Equal("#00000000", GetBackgroundColor(",\"backgroundColor\":\"#A00A1F1\""));
        [Fact] public void InvalidCharacterTest() => Assert.Equal("#00000000", GetBackgroundColor(",\"backgroundColor\":\"#ABF6P314\""));
        [Fact] public void InvalidFormatTest() => Assert.Equal("#00000000", GetBackgroundColor(",\"backgroundColor\":\"@ABF65314\""));
    }

    public class UnknownElementParsingTests
    {
        [Fact]
        public void CanGetCustomJsonPayload()
        {
            var (card, _) = Deserialize(@"{""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"",""type"":""AdaptiveCard"",""version"":""1.0"",
                ""body"":[{""type"":""Random"",""payload"":""You can even draw attention to certain text with color""}]}", "1.0");
            var elem = card.Body.First();
            Assert.Equal("Random", elem.ElementTypeString);
            Assert.IsType<AdaptiveUnsupportedElement>(elem);
            Assert.Equal("{\"payload\":\"You can even draw attention to certain text with color\",\"type\":\"Random\"}", Canonical(elem.AdditionalProperties));
        }

        [Fact]
        public void CanGetCustomJsonPayloadWithKnownElementFollowing()
        {
            var (card, _) = Deserialize(@"{""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"",""type"":""AdaptiveCard"",""version"":""1.0"",
                ""body"":[{""type"":""Unknown"",""payload"":""You can even draw attention to certain text with color""},
                {""type"":""TextBlock"",""text"":""You can even draw attention to certain text with color"",""wrap"":true,""color"":""attention"",""unknown"":""testing unknown""}]}", "1.0");
            var elem = card.Body.First();
            Assert.IsType<AdaptiveUnsupportedElement>(elem);
            Assert.Equal("{\"payload\":\"You can even draw attention to certain text with color\",\"type\":\"Unknown\"}", Canonical(elem.AdditionalProperties));
        }

        [Fact]
        public void CanGetJsonPayloadOfArrayType()
        {
            var (card, _) = Deserialize(@"{""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"",""type"":""AdaptiveCard"",""version"":""1.0"",
                ""body"":[{""type"":""RadioButton"",""payload"":[{""testloadone"":""You can even draw attention to certain text with color""},
                {""testloadtwo"":""You can even draw attention to certain text with markdown""}]},
                {""type"":""TextBlock"",""text"":""You can even draw attention to certain text with color"",""wrap"":true,""color"":""attention"",""unknown"":""testing unknown""}]}", "1.0");
            var elem = card.Body.First();
            Assert.Equal("RadioButton", elem.ElementTypeString);
            Assert.Equal(
                "{\"payload\":[{\"testloadone\":\"You can even draw attention to certain text with color\"},{\"testloadtwo\":\"You can even draw attention to certain text with markdown\"}],\"type\":\"RadioButton\"}",
                Canonical(elem.AdditionalProperties));
        }

        [Fact]
        public void CanHandleCustomAction()
        {
            var (card, _) = Deserialize(@"{""$schema"":""http://adaptivecards.io/schemas/adaptive-card.json"",""type"":""AdaptiveCard"",""version"":""1.0"",
                ""body"":[{""type"":""TextBlock"",""text"":""You can even draw attention to certain text with color"",""wrap"":true,""color"":""attention""}],
                ""actions"":[{""type"":""Alert"",""title"":""Submit"",""data"":{""id"":""1234567890""}}]}", "1.0");
            var elem = card.Actions.First();
            Assert.Equal("Alert", elem.ActionTypeString);
            Assert.IsType<AdaptiveUnsupportedAction>(elem);
            Assert.Equal("{\"data\":{\"id\":\"1234567890\"},\"title\":\"Submit\",\"type\":\"Alert\"}", Canonical(elem.AdditionalProperties));
        }

        [Fact]
        public void RoundTripTestForCustomAction()
        {
            const string testJsonString = @"{""type"":""AdaptiveCard"",""version"":""1.0"",
                ""body"":[{""type"":""TextBlock"",""text"":""You can even draw attention to certain text with color"",""wrap"":true,""color"":""Attention""}],
                ""actions"":[{""type"":""Alert"",""title"":""Submit"",""data"":{""id"":""1234567890""}}]}";
            var (card, _) = Deserialize(testJsonString, "1.0");
            Assert.Equal(Canonical(ParseUtil.ParseJsonString(testJsonString)), Serialize(card));
        }
    }
}
