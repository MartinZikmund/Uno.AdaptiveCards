// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Port of ContainerStyleTest.cpp. The JSON payloads live in TestData/ContainerStyle/{n}.json (extracted verbatim, in order).
using System.Linq;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class ContainerStyleTests
    {
        private static string Json(int index) => ReadTestData($"ContainerStyle/{index}.json");

        private static AdaptiveCard Card(int index) => Deserialize(Json(index), "1.2").Card;

        private static bool CanBleed(AdaptiveStyledCollectionElement element) => element.BleedDirection != BleedDirection.None;

        [Fact]
        public void CanCheckParentalContainerStyleTest()
        {
            var container = (AdaptiveContainer)Card(0).Body.First();
            Assert.Equal(ContainerStyle.Emphasis, container.Style);
        }

        [Fact]
        public void HaveValidPaddingFlagSetTest()
        {
            var container = (AdaptiveContainer)Card(1).Body.First();
            var container2 = (AdaptiveContainer)container.Items.Last();
            Assert.True(container2.HasPadding);
        }

        [Fact]
        public void ColumnContainerStyleTest()
        {
            var columnset = (AdaptiveColumnSet)Card(2).Body.First();
            var columns = columnset.Columns;
            Assert.Equal(ContainerStyle.None, columns[0].Style);
            var column2 = columns[2];
            Assert.Equal(ContainerStyle.Emphasis, column2.Style);
            var items = column2.Items;
            var container1 = (AdaptiveContainer)items[1];
            Assert.Equal(ContainerStyle.None, container1.Style);
            Assert.False(container1.HasPadding);
            var container2 = (AdaptiveContainer)items[2];
            Assert.Equal(ContainerStyle.Default, container2.Style);
            Assert.True(container2.HasPadding);
            var container3 = (AdaptiveContainer)items[3];
            Assert.Equal(ContainerStyle.Emphasis, container3.Style);
            Assert.False(container3.HasPadding);
        }

        [Fact]
        public void CanParseBleedPropertyTest()
        {
            Assert.True(((AdaptiveContainer)Card(3).Body.First()).Bleed);
            Assert.False(((AdaptiveContainer)Card(4).Body.First()).Bleed);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(6)]
        public void CanSerializeBleedPropertyTest(int index)
        {
            var json = Json(index);
            Assert.Equal(Canonical(ParseUtil.ParseJsonString(json)), Serialize(Deserialize(json, "1.2").Card));
        }

        [Fact]
        public void BleedPropertyConveysCorrectInformationTest()
        {
            var columnset = (AdaptiveColumnSet)Card(7).Body.First();
            var column1 = columnset.Columns[0];
            Assert.Equal(ContainerStyle.Emphasis, column1.Style);
            var container1 = (AdaptiveContainer)column1.Items.Last();
            Assert.Equal(ContainerStyle.Default, container1.Style);
            Assert.False(CanBleed(container1));
            var container2 = (AdaptiveContainer)container1.Items.Last();
            Assert.Equal("4", container2.Id);
            Assert.Equal(ContainerStyle.Default, container2.Style);
            Assert.False(CanBleed(container2));
            var container3 = (AdaptiveContainer)container2.Items.Last();
            Assert.Equal("5", container3.Id);
            Assert.Equal(ContainerStyle.Emphasis, container3.Style);
            Assert.True(CanBleed(container3));
            Assert.Equal(container1.InternalIdValue, container3.ParentalId);
        }

        [Fact]
        public void BleedPropertyColumnSetTest()
        {
            var card = Card(8);
            Assert.NotEmpty(card.Body);
            var columnset = (AdaptiveColumnSet)card.Body.First();
            Assert.Equal(ContainerStyle.Emphasis, columnset.Style);
            var columns = columnset.Columns;

            var column1 = columns[0];
            Assert.Equal(ContainerStyle.Default, column1.Style);
            Assert.Equal(BleedDirection.Down | BleedDirection.Left | BleedDirection.Up, column1.BleedDirection);
            var container = (AdaptiveContainer)column1.Items.Last();
            Assert.Equal(BleedDirection.Down | BleedDirection.Left | BleedDirection.Right, container.BleedDirection);
            Assert.Equal(column1.InternalIdValue, container.ParentalId);

            var column2 = columns[1];
            Assert.Equal(ContainerStyle.Default, column2.Style);
            Assert.True(column2.HasPadding);
            Assert.Equal(BleedDirection.Down | BleedDirection.Up, column2.BleedDirection);
            var container2 = (AdaptiveContainer)column2.Items.Last();
            Assert.Equal(BleedDirection.Down | BleedDirection.Left | BleedDirection.Right, container2.BleedDirection);

            var column3 = columns[2];
            Assert.Equal(ContainerStyle.Default, column3.Style);
            Assert.Equal(BleedDirection.Down | BleedDirection.Right | BleedDirection.Up, column3.BleedDirection);
            var container3 = (AdaptiveContainer)column3.Items.Last();
            Assert.Equal(BleedDirection.Down | BleedDirection.Right | BleedDirection.Left, container3.BleedDirection);
            Assert.Equal(column3.InternalIdValue, container3.ParentalId);
        }

        [Fact]
        public void BleedPropertyNestedColumnSetTest()
        {
            var card = Card(9);
            Assert.NotEmpty(card.Body);
            var columnset = (AdaptiveColumnSet)card.Body.First();
            Assert.Equal(ContainerStyle.Default, columnset.Style);
            var columns = columnset.Columns;

            var column1 = columns[0];
            Assert.Equal(ContainerStyle.Emphasis, column1.Style);
            Assert.False(column1.Bleed);
            Assert.False(CanBleed(column1));
            var innercolumns = ((AdaptiveColumnSet)column1.Items.Last()).Columns;
            Assert.Equal(BleedDirection.Down | BleedDirection.Left, innercolumns[0].BleedDirection);
            Assert.Equal(column1.InternalIdValue, innercolumns[0].ParentalId);
            Assert.Equal(BleedDirection.Down, innercolumns[1].BleedDirection);
            Assert.Equal(BleedDirection.Down | BleedDirection.Right, innercolumns[2].BleedDirection);
            Assert.Equal(column1.InternalIdValue, innercolumns[2].ParentalId);

            var column2 = columns[1];
            Assert.Equal(ContainerStyle.Emphasis, column2.Style);
            Assert.False(CanBleed(column2));
            var innercolumns2 = ((AdaptiveColumnSet)column2.Items.Last()).Columns;
            var innercontainer = (AdaptiveContainer)innercolumns2[0].Items[0];
            Assert.Equal(BleedDirection.Down | BleedDirection.Left, innercontainer.BleedDirection);
            Assert.Equal(column2.InternalIdValue, innercontainer.ParentalId);
            var innercontainer1 = (AdaptiveContainer)innercolumns2[1].Items[0];
            Assert.Equal(BleedDirection.Down, innercontainer1.BleedDirection);
            var innercontainer2 = (AdaptiveContainer)innercolumns2[2].Items[0];
            Assert.Equal(BleedDirection.Down | BleedDirection.Right, innercontainer2.BleedDirection);
            Assert.Equal(column2.InternalIdValue, innercontainer2.ParentalId);
        }

        [Fact]
        public void BleedPropertyNestedLeftAndRightRestrictedColumnSetTest()
        {
            var card = Card(10);
            Assert.NotEmpty(card.Body);
            var rootcontainer = (AdaptiveContainer)card.Body.First();
            var columnset = (AdaptiveColumnSet)rootcontainer.Items[0];
            Assert.Equal(ContainerStyle.Default, columnset.Style);
            var columns = columnset.Columns;

            var columnRight = columns[1];
            Assert.Equal(ContainerStyle.None, columnRight.Style);
            Assert.False(CanBleed(columnRight));
            var innercolumnset = (AdaptiveColumnSet)columnRight.Items.Last();
            Assert.False(CanBleed(innercolumnset));
            var innercolumns = innercolumnset.Columns;
            Assert.False(CanBleed(innercolumns[0]));
            var innercontainer = (AdaptiveContainer)innercolumns[0].Items[0];
            Assert.True(innercontainer.HasPadding);
            Assert.True(innercontainer.Bleed);
            Assert.Equal(BleedDirection.Down | BleedDirection.Up, innercontainer.BleedDirection);
            Assert.False(CanBleed(innercolumns[2]));
            var innercontainerRight = (AdaptiveContainer)innercolumns[2].Items[0];
            Assert.True(innercontainerRight.HasPadding);
            Assert.True(innercontainerRight.Bleed);
            Assert.Equal(BleedDirection.Down | BleedDirection.Right | BleedDirection.Up, innercontainerRight.BleedDirection);
            Assert.Equal(columnset.InternalIdValue, innercontainerRight.ParentalId);
            Assert.False(CanBleed(innercolumns[1]));

            var columnLeft = columns[0];
            Assert.Equal(ContainerStyle.None, columnLeft.Style);
            Assert.False(CanBleed(columnLeft));
        }

        [Fact]
        public void BleedSequentialColumnSets()
        {
            var card = Card(11);
            Assert.NotNull(card);
            var secondColumnSet = (AdaptiveColumnSet)card.Body[1];
            Assert.True(CanBleed(secondColumnSet.Columns[0]));
            Assert.Equal(BleedDirection.Down | BleedDirection.Left, secondColumnSet.Columns[0].BleedDirection);
        }
    }
}
