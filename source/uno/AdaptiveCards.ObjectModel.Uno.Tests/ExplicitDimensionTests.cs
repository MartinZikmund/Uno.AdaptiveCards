// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Port of ExplicitDimensionTest.cpp.
using System.Linq;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class ExplicitDimensionTests
    {
        private static (AdaptiveImage Image, ParseContext Context) ParseImage(string dimensions, string rendererVersion = "1.0")
        {
            var (card, context) = Deserialize(
                "{\"$schema\":\"http://adaptivecards.io/schemas/adaptive-card.json\",\"type\":\"AdaptiveCard\",\"version\":\"1.0\"," +
                "\"body\":[{\"type\":\"Image\",\"url\":\"Image\"" + dimensions + "}]}", rendererVersion);
            return ((AdaptiveImage)card.Body.First(), context);
        }

        [Fact]
        public void PositiveTest()
        {
            var (image, _) = ParseImage(",\"width\":\"10px\",\"height\":\"50px\"");
            Assert.Equal(10U, image.PixelWidth);
            Assert.Equal(50U, image.PixelHeight);
        }

        [Fact]
        public void PositiveTestWithOneDimensionOnly()
        {
            var (image, _) = ParseImage(",\"height\":\"10px\"");
            Assert.Equal(10U, image.PixelHeight);
            Assert.Equal(0U, image.PixelWidth);
        }

        [Theory]
        [InlineData("10pic")]
        [InlineData("10px  ")]
        [InlineData("-10px")]
        public void MalformedHeightTest(string height)
        {
            var (_, context) = ParseImage(",\"height\":\"" + height + "\"");
            var warning = Assert.Single(context.Warnings);
            Assert.Equal(WarningStatusCode.InvalidDimensionSpecified, warning.StatusCode);
        }

        [Fact]
        public void MalformedUnitTypeTest()
        {
            var (image, context) = ParseImage(",\"height\":\"10.0px\"");
            Assert.Empty(context.Warnings);
            Assert.Equal(10U, image.PixelHeight);
            Assert.Equal(0U, image.PixelWidth);
        }

        [Theory]
        [InlineData(".20px", "50.1234.12px")]
        [InlineData("20, 00px", "20,0.00   px")]
        [InlineData("2000 px", "20a0px")]
        [InlineData("20.a00px", "20.00")]
        [InlineData(" 20.00px", "2 0.00px")]
        [InlineData("200px .00px", "2 0px00px")]
        [InlineData("200.00 px", "200.0.px")]
        [InlineData("20,000px", "20,000.0px")]
        [InlineData("20.px", "050px")]
        public void MalformedDimensionValuesTest(string width, string height)
        {
            var (image, context) = ParseImage(",\"width\":\"" + width + "\",\"height\":\"" + height + "\"", "1.1");
            Assert.Equal(2, context.Warnings.Count);
            Assert.Equal(0U, image.PixelHeight);
            Assert.Equal(0U, image.PixelWidth);
        }

        [Fact]
        public void MinHeightForAllElementsTest()
        {
            var (card, _) = Deserialize(@"{""type"":""AdaptiveCard"",""version"":""1.2"",""body"":[{""type"":""ColumnSet"",""minHeight"":""50px"",
                ""columns"":[{""type"":""Column"",""minHeight"":""75px"",""items"":[{""type"":""TextBlock"",""text"":""Column 1""}]}]}]}", "1.2");
            var columnSet = (AdaptiveColumnSet)card.Body.First();
            Assert.Equal(50U, columnSet.MinHeight);
            Assert.Equal(75U, columnSet.Columns.First().MinHeight);

            (card, _) = Deserialize(@"{""type"":""AdaptiveCard"",""version"":""1.2"",""body"":[{""type"":""Container"",""minHeight"":""100px"",
                ""items"":[{""type"":""TextBlock"",""text"":""This is some text""}]}]}", "1.2");
            Assert.Equal(100U, ((AdaptiveContainer)card.Body.First()).MinHeight);

            (card, _) = Deserialize(@"{""type"":""AdaptiveCard"",""version"":""1.2"",""minHeight"":""100px"",
                ""body"":[{""type"":""TextBlock"",""text"":""This is some text""}]}", "1.2");
            Assert.Equal(100U, card.MinHeight);
        }
    }

    public class ExplicitDimensionForColumnTests
    {
        private static (AdaptiveColumn Column, ParseContext Context) ParseColumns(string columns)
        {
            var (card, context) = Deserialize(
                "{\"type\":\"AdaptiveCard\",\"version\":\"1.0\",\"body\":[{\"type\":\"ColumnSet\",\"columns\":[" + columns + "]}]}", "1.0");
            return (((AdaptiveColumnSet)card.Body.First()).Columns.First(), context);
        }

        [Fact]
        public void PositiveValueTest()
        {
            var (column, _) = ParseColumns("{\"type\":\"Column\",\"width\":\"auto\",\"items\":[]}");
            Assert.Equal("auto", column.Width);
        }

        [Fact]
        public void PositiveRelativeWidthTest()
        {
            var (column, _) = ParseColumns("{\"type\":\"Column\",\"width\":\"20\",\"items\":[]},{\"type\":\"Column\",\"width\":\"20\",\"items\":[]}");
            Assert.Equal("20", column.Width);
            Assert.NotEqual(20U, column.PixelWidth);
        }

        [Fact]
        public void PositiveExplicitWidthTest()
        {
            var (column, _) = ParseColumns("{\"type\":\"Column\",\"width\":\"20px\",\"items\":[]}");
            Assert.Equal("20px", column.Width);
            Assert.Equal(20U, column.PixelWidth);
        }

        [Fact]
        public void ExplicitWidthMalformedUnitTest()
        {
            var (column, context) = ParseColumns("{\"type\":\"Column\",\"width\":\"20px20\",\"items\":[]}");
            var warning = Assert.Single(context.Warnings);
            Assert.Equal(WarningStatusCode.InvalidDimensionSpecified, warning.StatusCode);
            Assert.Equal(0U, column.PixelWidth);
            Assert.Equal("stretch", column.Width);
        }

        [Fact]
        public void ExplicitWidthMalformedValueTest()
        {
            var (column, context) = ParseColumns("{\"type\":\"Column\",\"width\":\"-20px\",\"items\":[]}");
            var warning = Assert.Single(context.Warnings);
            Assert.Equal(WarningStatusCode.InvalidDimensionSpecified, warning.StatusCode);
            Assert.Equal(0U, column.PixelWidth);
            Assert.Equal("-20px", column.Width);
        }

        [Fact]
        public void ExplicitWidthFloatValueTest()
        {
            var (column, _) = ParseColumns("{\"type\":\"Column\",\"width\":\"20.5px\",\"items\":[]}");
            Assert.Equal("20.5px", column.Width);
            Assert.Equal(20U, column.PixelWidth);
        }
    }
}
