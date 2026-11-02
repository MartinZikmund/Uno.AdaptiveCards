// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Port of TableTests.cpp.
using System.Linq;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class TableTests
    {
        private static ParseContext NewContext() => new ParseContext(null, null);

        [Fact]
        public void TableCellParse()
        {
            var context = NewContext();
            var tableCell = AdaptiveTableCell.DeserializeTableCell(context, JsonNode.Parse(@"{ ""type"": ""TableCell"",
                ""items"": [ { ""type"": ""TextBlock"", ""text"": ""flim"" }, { ""type"": ""TextBlock"", ""text"": ""iz-us"" } ], ""rtl"": true }"));
            Assert.Empty(tableCell.AdditionalProperties);
            Assert.True(tableCell.Rtl.Value);
            Assert.Equal(2, tableCell.Items.Count);
            Assert.All(tableCell.Items, item => Assert.Equal("TextBlock", item.ElementTypeString));
            Assert.Equal(
                "{\"items\":[{\"text\":\"flim\",\"type\":\"TextBlock\"},{\"text\":\"iz-us\",\"type\":\"TextBlock\"}],\"rtl\":true,\"type\":\"TableCell\"}",
                Serialize(tableCell));
        }

        [Theory]
        [InlineData("{\"type\":\"TableRow\"}")]
        [InlineData("{\"type\":\"Table\"}")]
        [InlineData("{\"items\":[],\"type\":\"TableCell\"}")]
        public void TableEmptyParseTests(string fragment)
        {
            var element = ParseUtil.ParseCardElement(NewContext(), JsonNode.Parse(fragment));
            Assert.Equal(fragment, Serialize(element));
        }

        [Fact]
        public void TableRowParse()
        {
            var context = NewContext();
            var tableRow = AdaptiveTableRow.DeserializeTableRow(context, JsonNode.Parse(@"{ ""type"": ""TableRow"",
                ""horizontalCellContentAlignment"": ""center"", ""verticalCellContentAlignment"": ""bottom"", ""style"": ""accent"",
                ""cells"": [
                    { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""the first"" }, { ""type"": ""TextBlock"", ""text"": ""the first part deux"" } ], ""rtl"": true },
                    { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""the second"" } ], ""rtl"": true } ] }"));
            Assert.Empty(tableRow.AdditionalProperties);
            Assert.Equal(2, tableRow.Cells.Count);
            Assert.Equal(ContainerStyle.Accent, tableRow.Style);
            Assert.Equal(HAlignment.Center, tableRow.HorizontalCellContentAlignment);
            Assert.Equal(VerticalContentAlignment.Bottom, tableRow.VerticalCellContentAlignment);
            Assert.Equal(
                "{\"cells\":[{\"items\":[{\"text\":\"the first\",\"type\":\"TextBlock\"},{\"text\":\"the first part deux\",\"type\":\"TextBlock\"}],\"rtl\":true,\"type\":\"TableCell\"},{\"items\":[{\"text\":\"the second\",\"type\":\"TextBlock\"}],\"rtl\":true,\"type\":\"TableCell\"}],\"horizontalCellContentAlignment\":\"center\",\"style\":\"Accent\",\"type\":\"TableRow\",\"verticalCellContentAlignment\":\"Bottom\"}",
                Serialize(tableRow));
        }

        [Fact]
        public void TableElementsParserRegistration()
        {
            var registration = new AdaptiveElementParserRegistration();
            Assert.NotNull(registration.Get("Table"));
            Assert.Null(registration.Get("TableRow"));
            Assert.Null(registration.Get("TableCell"));
        }

        [Fact]
        public void TableColumnDefinitionSimpleParse()
        {
            var columnDefinition = AdaptiveTableColumnDefinition.Deserialize(NewContext(), JsonNode.Parse(
                @"{ ""horizontalCellContentAlignment"": ""center"", ""verticalCellContentAlignment"": ""bottom"", ""width"": 1 }"));
            Assert.Equal(1U, columnDefinition.Width.Value);
            Assert.False(columnDefinition.PixelWidth.HasValue);
            Assert.Equal(HAlignment.Center, columnDefinition.HorizontalCellContentAlignment);
            Assert.Equal(VerticalContentAlignment.Bottom, columnDefinition.VerticalCellContentAlignment);
            Assert.Equal("{\"horizontalCellContentAlignment\":\"center\",\"verticalCellContentAlignment\":\"Bottom\",\"width\":1}", Canonical(columnDefinition.ToJson()));
        }

        [Fact]
        public void TableColumnDefinitionPixelParse()
        {
            var columnDefinition = AdaptiveTableColumnDefinition.Deserialize(NewContext(), JsonNode.Parse(
                @"{ ""horizontalCellContentAlignment"": ""right"", ""verticalCellContentAlignment"": ""center"", ""width"": ""100px"" }"));
            Assert.False(columnDefinition.Width.HasValue);
            Assert.Equal(100U, columnDefinition.PixelWidth.Value);
            Assert.Equal(HAlignment.Right, columnDefinition.HorizontalCellContentAlignment);
            Assert.Equal(VerticalContentAlignment.Center, columnDefinition.VerticalCellContentAlignment);
            Assert.Equal("{\"horizontalCellContentAlignment\":\"right\",\"verticalCellContentAlignment\":\"Center\",\"width\":\"100px\"}", Canonical(columnDefinition.ToJson()));
        }

        [Fact]
        public void TableColumnDefinitionMissingUnitParse()
        {
            var context = NewContext();
            var columnDefinition = AdaptiveTableColumnDefinition.Deserialize(context, JsonNode.Parse(@"{ ""width"": ""10"" }"));
            Assert.False(columnDefinition.Width.HasValue);
            Assert.False(columnDefinition.PixelWidth.HasValue);
            Assert.NotEmpty(context.Warnings);
        }

        [Fact]
        public void TableColumnDefinitionInvalidParse()
        {
            var context = NewContext();
            var columnDefinition = AdaptiveTableColumnDefinition.Deserialize(context, JsonNode.Parse(@"{ ""width"": ""10pixels"" }"));
            Assert.False(columnDefinition.Width.HasValue);
            Assert.NotEmpty(context.Warnings);
        }

        private const string HeaderRow = @"{ ""type"": ""TableRow"", ""cells"": [
            { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Name"", ""wrap"": true, ""weight"": ""Bolder"" } ] },
            { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Type"", ""wrap"": true, ""weight"": ""Bolder"" } ] },
            { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Description"", ""wrap"": true, ""weight"": ""Bolder"" } ] } ],
            ""style"": ""accent"" }";

        private const string HeaderRowJson =
            "{\"cells\":[{\"items\":[{\"text\":\"Name\",\"type\":\"TextBlock\",\"weight\":\"Bolder\",\"wrap\":true}],\"type\":\"TableCell\"},{\"items\":[{\"text\":\"Type\",\"type\":\"TextBlock\",\"weight\":\"Bolder\",\"wrap\":true}],\"type\":\"TableCell\"},{\"items\":[{\"text\":\"Description\",\"type\":\"TextBlock\",\"weight\":\"Bolder\",\"wrap\":true}],\"type\":\"TableCell\"}],\"style\":\"Accent\",\"type\":\"TableRow\"}";

        [Fact]
        public void TableFragmentParseValid()
        {
            var fragment = @"{ ""type"": ""Table"", ""gridStyle"": ""accent"", ""firstRowAsHeaders"": true,
                ""columns"": [ { ""width"": 1 }, { ""width"": 1 }, { ""width"": 3 } ],
                ""rows"": [ " + HeaderRow + @",
                    { ""type"": ""TableRow"", ""cells"": [
                        { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""columns"", ""wrap"": true } ] },
                        { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""ColumnDefinition[]"", ""wrap"": true } ] },
                        { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Defines the table's columns (number of columns, and column sizes)."", ""wrap"": true } ] } ] },
                    { ""type"": ""TableRow"", ""cells"": [
                        { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""rows"", ""wrap"": true } ] },
                        { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""TableRow[]"", ""wrap"": true } ] },
                        { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Defines the rows of the Table, each being a collection of cells. Rows are not required, which allows empty Tables to be generated via templating without breaking the rendering of the whole card."", ""wrap"": true } ] } ] } ] }";
            var context = NewContext();
            var table = (AdaptiveTable)new AdaptiveTableParser().FromJson((JsonObject)JsonNode.Parse(fragment), null, null, null);
            Assert.Empty(table.AdditionalProperties);
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(new uint?[] { 1, 1, 3 }, table.Columns.Select(c => c.Width).ToArray());
            Assert.All(table.Columns, c => Assert.False(c.PixelWidth.HasValue));
            Assert.Equal(3, table.Rows.Count);
            Assert.All(table.Rows, r => Assert.Equal(3, r.Cells.Count));
            Assert.Equal(
                "{\"columns\":[{\"width\":1},{\"width\":1},{\"width\":3}],\"gridStyle\":\"Accent\",\"rows\":[" + HeaderRowJson + ",{\"cells\":[{\"items\":[{\"text\":\"columns\",\"type\":\"TextBlock\",\"wrap\":true}],\"type\":\"TableCell\"},{\"items\":[{\"text\":\"ColumnDefinition[]\",\"type\":\"TextBlock\",\"wrap\":true}],\"type\":\"TableCell\"},{\"items\":[{\"text\":\"Defines the table's columns (number of columns, and column sizes).\",\"type\":\"TextBlock\",\"wrap\":true}],\"type\":\"TableCell\"}],\"type\":\"TableRow\"},{\"cells\":[{\"items\":[{\"text\":\"rows\",\"type\":\"TextBlock\",\"wrap\":true}],\"type\":\"TableCell\"},{\"items\":[{\"text\":\"TableRow[]\",\"type\":\"TextBlock\",\"wrap\":true}],\"type\":\"TableCell\"},{\"items\":[{\"text\":\"Defines the rows of the Table, each being a collection of cells. Rows are not required, which allows empty Tables to be generated via templating without breaking the rendering of the whole card.\",\"type\":\"TextBlock\",\"wrap\":true}],\"type\":\"TableCell\"}],\"type\":\"TableRow\"}],\"type\":\"Table\"}",
                Serialize(table));
        }

        [Fact]
        public void TableCardParseValid()
        {
            var cardJson = @"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.5"",
                ""body"": [ { ""type"": ""Table"", ""gridStyle"": ""accent"", ""firstRowAsHeaders"": true,
                    ""columns"": [ { ""width"": 1 }, { ""width"": 1 }, { ""width"": 3 } ],
                    ""rows"": [ " + HeaderRow + @",
                        { ""type"": ""TableRow"", ""cells"": [
                            { ""type"": ""TableCell"", ""style"": ""good"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""columns"", ""wrap"": true } ] },
                            { ""type"": ""TableCell"", ""style"": ""warning"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""some text"", ""wrap"": true } ] },
                            { ""type"": ""TableCell"", ""style"": ""accent"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""some text #2"", ""wrap"": true } ] } ] } ] } ] }";
            var (card, _) = Deserialize(cardJson, "1.5");
            Assert.Single(card.Body);
            Assert.Equal("Table", card.Body[0].ElementTypeString);
            Assert.Equal(
                "{\"actions\":[],\"body\":[{\"columns\":[{\"width\":1},{\"width\":1},{\"width\":3}],\"gridStyle\":\"Accent\",\"rows\":[" + HeaderRowJson + ",{\"cells\":[{\"items\":[{\"text\":\"columns\",\"type\":\"TextBlock\",\"wrap\":true}],\"style\":\"Good\",\"type\":\"TableCell\"},{\"items\":[{\"text\":\"some text\",\"type\":\"TextBlock\",\"wrap\":true}],\"style\":\"Warning\",\"type\":\"TableCell\"},{\"items\":[{\"text\":\"some text #2\",\"type\":\"TextBlock\",\"wrap\":true}],\"style\":\"Accent\",\"type\":\"TableCell\"}],\"type\":\"TableRow\"}],\"type\":\"Table\"}],\"type\":\"AdaptiveCard\",\"version\":\"1.5\"}",
                Serialize(card));
        }

        [Fact]
        public void TableCardParseOrphanedTableRow()
        {
            var (card, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.5"",
                ""body"": [ { ""type"": ""TableRow"", ""cells"": [ { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""..."" } ] } ] } ] }", "1.5");
            var bodyElem = Assert.Single(card.Body);
            Assert.Equal("TableRow", bodyElem.ElementTypeString);
            Assert.Equal(ElementType.Unsupported, bodyElem.ElementType);
            Assert.Equal(
                "{\"actions\":[],\"body\":[{\"cells\":[{\"items\":[{\"text\":\"...\",\"type\":\"TextBlock\"}],\"type\":\"TableCell\"}],\"type\":\"TableRow\"}],\"type\":\"AdaptiveCard\",\"version\":\"1.5\"}",
                Serialize(card));
        }

        [Fact]
        public void TableCardParseOrphanedTableCell()
        {
            var (card, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.5"",
                ""body"": [ { ""type"": ""TableCell"", ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Name"", ""wrap"": true, ""weight"": ""Bolder"" } ] } ] }", "1.5");
            var bodyElem = Assert.Single(card.Body);
            Assert.Equal("TableCell", bodyElem.ElementTypeString);
            Assert.Equal(ElementType.Unsupported, bodyElem.ElementType);
            Assert.Equal(
                "{\"actions\":[],\"body\":[{\"items\":[{\"text\":\"Name\",\"type\":\"TextBlock\",\"weight\":\"Bolder\",\"wrap\":true}],\"type\":\"TableCell\"}],\"type\":\"AdaptiveCard\",\"version\":\"1.5\"}",
                Serialize(card));
        }

        [Fact]
        public void TableCardParseWithImpliedTypes()
        {
            var (card, _) = Deserialize(@"{ ""type"": ""AdaptiveCard"", ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"", ""version"": ""1.5"",
                ""body"": [ { ""type"": ""Table"", ""columns"": [ { ""width"": 1 } ],
                    ""rows"": [ { ""cells"": [ { ""items"": [ { ""type"": ""TextBlock"", ""text"": ""Text goes here."" } ] } ] } ] } ] }", "1.5");
            var bodyElem = Assert.Single(card.Body);
            Assert.Equal(ElementType.Table, bodyElem.ElementType);
            var table = (AdaptiveTable)bodyElem;
            Assert.Single(table.Columns);
            var row = Assert.Single(table.Rows);
            Assert.Equal(ElementType.TableRow, row.ElementType);
            var cell = Assert.Single(row.Cells);
            Assert.Equal(ElementType.TableCell, cell.ElementType);
        }
    }
}
