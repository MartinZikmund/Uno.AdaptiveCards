// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Ports of SemanticVersionTest.cpp and ElementTest.cpp.
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class SemanticVersionTests
    {
        [Theory]
        [InlineData("1", 1, 0, 0, 0)]
        [InlineData("10.2", 10, 2, 0, 0)]
        [InlineData("100.20.3", 100, 20, 3, 0)]
        [InlineData("1000.200.30.4", 1000, 200, 30, 4)]
        [InlineData("1000.200.30.40", 1000, 200, 30, 40)]
        public void PositiveTest(string text, uint major, uint minor, uint build, uint revision)
        {
            var version = new SemanticVersion(text);
            Assert.Equal(major, version.Major);
            Assert.Equal(minor, version.Minor);
            Assert.Equal(build, version.Build);
            Assert.Equal(revision, version.Revision);
        }

        [Theory]
        [InlineData("")]
        [InlineData("text")]
        [InlineData("1234567890123456789012345678901234567890")]
        [InlineData("1.")]
        [InlineData("1.2.")]
        [InlineData("1.2.3.")]
        [InlineData("1.2.3.4.")]
        [InlineData(" 1.0")]
        [InlineData("1.0 ")]
        [InlineData("-1")]
        [InlineData("0xF")]
        [InlineData("F")]
        [InlineData("1.c")]
        public void NegativeTest(string text)
        {
            Assert.Throws<AdaptiveCardParseException>(() => new SemanticVersion(text));
        }

        [Theory]
        [InlineData("1", "1.000000.0000000.0000000", 0)]
        [InlineData("1.1", "1.001", 0)]
        [InlineData("1.0", "1.1", -1)]
        [InlineData("1.0", "1.0.1", -1)]
        [InlineData("1.1.2", "1.1.3", -1)]
        [InlineData("1.1.3.100", "1.1.4.1", -1)]
        [InlineData("1.3.1", "1.10.2", -1)]
        public void CompareTest(string left, string right, int expected)
        {
            var lhs = new SemanticVersion(left);
            var rhs = new SemanticVersion(right);
            Assert.Equal(expected == 0, lhs == rhs);
            Assert.Equal(expected != 0, lhs != rhs);
            Assert.Equal(expected < 0, lhs < rhs);
            Assert.Equal(expected > 0, lhs > rhs);
            Assert.Equal(expected >= 0, lhs >= rhs);
            Assert.Equal(expected <= 0, lhs <= rhs);
        }
    }

    public class ElementTests
    {
        private static AdaptiveChoiceSetInput ParseChoiceSet(ParseContext context, string fragment) =>
            AdaptiveChoiceSetInput.Deserialize(context, (JsonObject)JsonNode.Parse(fragment));

        [Fact]
        public void ColumnPixelWidthTest()
        {
            var columnTest = new AdaptiveColumn();
            Assert.Equal(0U, columnTest.PixelWidth);

            // Deviation: the C# port defaults the width to "auto" (the shared model used "Auto").
            Assert.Equal("auto", columnTest.Width);
            Assert.Equal("{\"items\":[],\"type\":\"Column\",\"width\":\"auto\"}", Serialize(columnTest));

            columnTest.Width = "20px";
            Assert.Equal(20U, columnTest.PixelWidth);
            Assert.Equal("20px", columnTest.Width);
            Assert.Equal("{\"items\":[],\"type\":\"Column\",\"width\":\"20px\"}", Serialize(columnTest));

            columnTest.PixelWidth = 40;
            Assert.Equal(40U, columnTest.PixelWidth);
            Assert.Equal("40px", columnTest.Width);
            Assert.Equal("{\"items\":[],\"type\":\"Column\",\"width\":\"40px\"}", Serialize(columnTest));

            columnTest.Width = "Stretch";
            Assert.Equal(0U, columnTest.PixelWidth);
            Assert.Equal("stretch", columnTest.Width);
            Assert.Equal("{\"items\":[],\"type\":\"Column\",\"width\":\"stretch\"}", Serialize(columnTest));
        }

        [Fact]
        public void ChoiceSetMultiSelectFiltered()
        {
            const string fragment = @"{
                ""type"": ""Input.ChoiceSet"", ""id"": ""theChoiceSet"", ""isMultiSelect"": true, ""style"": ""filtered"",
                ""choices"": [ { ""title"": ""Red"", ""value"": ""1"" }, { ""title"": ""Green"", ""value"": ""2"" }, { ""title"": ""Blue"", ""value"": ""3"" } ]
            }";
            var context = new ParseContext(null, null);
            var choiceSet = ParseChoiceSet(context, fragment);
            Assert.True(choiceSet.IsMultiSelect);
            Assert.Equal(ChoiceSetStyle.Filtered, choiceSet.ChoiceSetStyle);
            var warning = Assert.Single(context.Warnings);
            Assert.Equal(WarningStatusCode.InvalidValue, warning.StatusCode);
            Assert.Equal("Input.ChoiceSet does not support filtering with multiselect", warning.Message);
        }

        [Fact]
        public void ChoiceSetWithValidChoicesDataTest()
        {
            const string fragment = @"{ ""type"": ""Input.ChoiceSet"", ""id"": ""theChoiceSet"", ""style"": ""filtered"",
                ""choices.data"": { ""type"": ""Data.Query"", ""dataset"": ""graph.microsoft.com/users"" } }";
            var context = new ParseContext(null, null);
            var choicesData = ParseChoiceSet(context, fragment).ChoicesData;
            Assert.Equal("graph.microsoft.com/users", choicesData.Dataset);
            Assert.Equal("Data.Query", choicesData.ChoicesDataType);
            Assert.Equal("{\"dataset\":\"graph.microsoft.com/users\",\"type\":\"Data.Query\"}", Canonical(choicesData.ToJson()));
        }

        [Fact]
        public void ChoiceSetWithInvalidChoicesDataTypeTest()
        {
            const string fragment = @"{ ""type"": ""Input.ChoiceSet"", ""id"": ""theChoiceSet"", ""style"": ""filtered"",
                ""choices.data"": { ""type"": ""data.queryabc"", ""dataset"": ""graph.microsoft.com/users"" } }";
            var context = new ParseContext(null, null);
            ParseChoiceSet(context, fragment);
            var warning = Assert.Single(context.Warnings);
            Assert.Equal(WarningStatusCode.InvalidValue, warning.StatusCode);
            Assert.Equal("Invalid type for Choices.data, only Data.Query is supported", warning.Message);
        }

        [Fact]
        public void ChoiceSetWithInvalidChoicesDataTest()
        {
            const string fragment = @"{ ""type"": ""Input.ChoiceSet"", ""id"": ""theChoiceSet"", ""style"": ""filtered"",
                ""choices.data"": { ""type"": ""Data.Query"", ""dataset"": """" } }";
            var context = new ParseContext(null, null);
            ParseChoiceSet(context, fragment);
            var warning = Assert.Single(context.Warnings);
            Assert.Equal(WarningStatusCode.RequiredPropertyMissing, warning.StatusCode);
            Assert.Equal("non-empty string has to be given for dataset in choices.data, none given", warning.Message);
        }

        [Fact]
        public void ChoiceSetMultiselectWithChoicesDataTest()
        {
            const string fragment = @"{ ""type"": ""Input.ChoiceSet"", ""id"": ""theChoiceSet"", ""style"": ""filtered"", ""isMultiSelect"": true,
                ""choices.data"": { ""type"": ""Data.Query"", ""dataset"": ""graph.microsoft.com/users"" } }";
            var context = new ParseContext(null, null);
            ParseChoiceSet(context, fragment);
            var warning = Assert.Single(context.Warnings);
            Assert.Equal(WarningStatusCode.InvalidValue, warning.StatusCode);
            Assert.Equal("Input.ChoiceSet does not support filtering with multiselect", warning.Message);
        }
    }
}
