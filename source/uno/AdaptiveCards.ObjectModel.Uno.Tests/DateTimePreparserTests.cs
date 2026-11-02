// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Ported from source/shared/cpp/AdaptiveCardsSharedModel/AdaptiveCardsSharedModelUnitTest/DateAndTimeUnitTest.cpp.
// The C++ tests only run in the Pacific time zone; here the time zone is injected instead.
using System;
using System.Linq;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class DateTimePreparserTests
    {
        private static readonly TimeZoneInfo Pacific = FindPacific();

        private static TimeZoneInfo FindPacific()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }
        }

        private static DateTimePreparser Parse(string text) => new DateTimePreparser(text, Pacific);

        private static void AssertTime(string input, string expected)
        {
            var token = Parse(input).GetTextTokens().First();
            Assert.Equal(expected, token.Text);
            Assert.Equal(DateTimePreparsedTokenFormat.RegularString, token.Format);
        }

        private static void AssertDate(string input, int day, int month, int year, DateTimePreparsedTokenFormat format)
        {
            var preparser = Parse(input);
            var token = preparser.GetTextTokens().First();
            Assert.Equal(input, token.Text);
            Assert.Equal(day, token.Day);
            Assert.Equal(month, token.Month);
            Assert.Equal(year, token.Year);
            Assert.Equal(format, token.Format);
            Assert.True(preparser.HasDateTokens());
        }

        private static void AssertUnchanged(string input)
        {
            var preparser = Parse(input);
            Assert.Equal(input, string.Concat(preparser.GetTextTokens().Select(t => t.Text)));
            Assert.All(preparser.GetTextTokens(), t => Assert.Equal(DateTimePreparsedTokenFormat.RegularString, t.Format));
            Assert.False(preparser.HasDateTokens());
        }

        // TimeTest
        [Fact] public void TransformToTimeTest() => AssertTime("{{TIME(2017-10-28T02:17:00Z)}}", "07:17 PM");
        [Fact] public void TransformToTimeTest2() => AssertTime("{{TIME(2017-10-27T18:19:09Z)}}", "11:19 AM");
        [Fact] public void TransformToTimeWithSmallPositiveOffsetTest() => AssertTime("{{TIME(2017-10-28T04:20:00+02:00)}}", "07:20 PM");
        [Fact] public void TransformToTimeWithLargePositiveOffsetTest() => AssertTime("{{TIME(2017-10-28T11:25:00+09:00)}}", "07:25 PM");
        [Fact] public void TransformToTimeWithMinusOffsetTest() => AssertTime("{{TIME(2017-10-27T22:27:00-04:00)}}", "07:27 PM");

        // DateTest
        [Fact] public void TransformDateTest() => AssertDate("{{DATE(2017-02-13T20:46:30Z, COMPACT)}}", 13, 1, 2017, DateTimePreparsedTokenFormat.DateCompact);
        [Fact] public void TransformToDateWithSmallPositiveOffset() => AssertDate("{{DATE(2017-10-28T04:20:00+02:00, COMPACT)}}", 27, 9, 2017, DateTimePreparsedTokenFormat.DateCompact);
        [Fact] public void TransformToDateWithLargePositiveOffset() => AssertDate("{{DATE(2017-10-28T11:25:00+09:00, COMPACT)}}", 27, 9, 2017, DateTimePreparsedTokenFormat.DateCompact);
        [Fact] public void TransformToDateNegativeOffset() => AssertDate("{{DATE(2017-10-27T22:27:00-04:00, COMPACT)}}", 27, 9, 2017, DateTimePreparsedTokenFormat.DateCompact);
        [Fact] public void TransformToDateRespectsOptionalSpace() => AssertDate("{{DATE(2017-10-27T22:27:00-04:00,COMPACT)}}", 27, 9, 2017, DateTimePreparsedTokenFormat.DateCompact);
        [Fact] public void TransformToDateOnlyAllowsUpToOneSpaceBeforeModifier() => AssertUnchanged("{{DATE(2017-10-27T22:27:00-04:00,  COMPACT)}}");

        // TimeAndDateInputTest
        [Fact] public void TimeWithShortFormat() => AssertUnchanged("{{TIME(2017-10-27T22:07:00Z, SHORT)}}");
        [Fact] public void TimeWithLongFormat() => AssertUnchanged("{{TIME(2017-10-27T22:27:00-04:00, LONG)}}");

        [Fact]
        public void TimeWithLongFormatInText()
        {
            AssertUnchanged("Hello {{TIME(2017-10-27T26:27:00Z, LONG)}} World!");
            Assert.Equal(3, Parse("Hello {{TIME(2017-10-27T26:27:00Z, LONG)}} World!").GetTextTokens().Count);
        }

        [Fact] public void MissingLeadingDigitOfMinutesInputTest() => AssertUnchanged("{{TIME(2017-10-27T22:7:00-04:00)}}");
        [Fact] public void MissingColumnDelimiterTest() => AssertUnchanged("{{TIME(2017-10-27T2:7:00Q04:00)}}");

        [Fact]
        public void ISO8601WithTextTest()
        {
            var tokens = Parse("You have arrived in New York on {{DATE(2017-10-27T22:23:00Z, SHORT)}}").GetTextTokens();
            Assert.Equal("{{DATE(2017-10-27T22:23:00Z, SHORT)}}", tokens.Last().Text);
            Assert.Equal(DateTimePreparsedTokenFormat.DateShort, tokens.Last().Format);
        }

        [Fact]
        public void TwoISO8601WithText()
        {
            var tokens = Parse("You have arrived in New York on {{DATE(2017-10-27T22:27:00-04:00, SHORT)}} at {{TIME(2017-10-27T22:27:00-04:00)}}.\r have a good trip").GetTextTokens();

            Assert.Equal("You have arrived in New York on ", tokens[0].Text);
            Assert.Equal(DateTimePreparsedTokenFormat.RegularString, tokens[0].Format);

            Assert.Equal("{{DATE(2017-10-27T22:27:00-04:00, SHORT)}}", tokens[1].Text);
            Assert.True(tokens[1].Day == 27 && tokens[1].Month == 9 && tokens[1].Year == 2017);
            Assert.Equal(DateTimePreparsedTokenFormat.DateShort, tokens[1].Format);

            Assert.Equal(" at ", tokens[2].Text);
            Assert.Equal(DateTimePreparsedTokenFormat.RegularString, tokens[2].Format);

            Assert.Equal("07:27 PM", tokens[3].Text);
            Assert.Equal(DateTimePreparsedTokenFormat.RegularString, tokens[3].Format);

            Assert.Equal(".\r have a good trip", tokens[4].Text);
            Assert.Equal(DateTimePreparsedTokenFormat.RegularString, tokens[4].Format);
        }

        [Fact]
        public void prefixStringISO8650suffixStringTest()
        {
            var tokens = Parse("You will arrived in Seattle on {{DATE(2017-10-27T22:23:00Z, SHORT)}}; have a good trip").GetTextTokens();

            Assert.Equal("You will arrived in Seattle on ", tokens[0].Text);
            Assert.Equal(DateTimePreparsedTokenFormat.RegularString, tokens[0].Format);

            Assert.Equal("{{DATE(2017-10-27T22:23:00Z, SHORT)}}", tokens[1].Text);
            Assert.True(tokens[1].Day == 27 && tokens[1].Month == 9 && tokens[1].Year == 2017);
            Assert.Equal(DateTimePreparsedTokenFormat.DateShort, tokens[1].Format);

            Assert.Equal("; have a good trip", tokens[2].Text);
            Assert.Equal(DateTimePreparsedTokenFormat.RegularString, tokens[2].Format);
        }

        [Fact] public void MalformedCurlybracketsTest() => AssertUnchanged("{a{DATE(2017-02-13T20:46:30Z, SHORT)}}");
        [Fact] public void MissingClosingCurlyBracketTest() => AssertUnchanged("{{DATE(2017-02-13T20:46:30Z, SHORT)}");
        [Fact] public void YearInBadFormatInputTest() => AssertUnchanged("{{DATE(2017a02-13T20:46:30Z, SHORT)}}");
        [Fact] public void DateDefaultStyleInputTest() => AssertDate("{{DATE(2017-02-13T20:46:30Z)}}", 13, 1, 2017, DateTimePreparsedTokenFormat.DateCompact);
        [Fact] public void DateLONGStyleInputTest() => AssertDate("{{DATE(2017-02-13T20:46:30Z, LONG)}}", 13, 1, 2017, DateTimePreparsedTokenFormat.DateLong);
        [Fact] public void DateSHORTStyleInputTest() => AssertDate("{{DATE(2017-02-13T20:46:30Z, SHORT)}}", 13, 1, 2017, DateTimePreparsedTokenFormat.DateShort);
        [Fact] public void DateSmallCaseLONGStyleInputTest() => AssertUnchanged("{{DATE(2017-02-13T20:46:30Z, Long)}}");
        [Fact] public void InvalidDateTest() => AssertUnchanged("{{DATE(2017-99-14T06:08:00Z)}}");
        [Fact] public void InvalidTimeTest() => AssertUnchanged("{{TIME(2017-99-14T06:08:00Z)}}");
        [Fact] public void LeapYearValidDayTest() => AssertDate("{{DATE(1992-02-29T18:08:00Z)}}", 29, 1, 1992, DateTimePreparsedTokenFormat.DateCompact);
        [Fact] public void LeapYearValidDayOnlyAtUTCTest() => AssertDate("{{DATE(1992-02-29T07:59:00Z)}}", 28, 1, 1992, DateTimePreparsedTokenFormat.DateCompact);
        [Fact] public void NoneLeapYearInvalidDayTest() => AssertUnchanged("{{DATE(1994-02-29T06:08:00Z)}}");

        // Additional coverage for the simple parsers used by Input.Date / Input.Time.
        [Theory]
        [InlineData("2017-10-27", true, 2017, 10, 27)]
        [InlineData("2016-02-29", true, 2016, 2, 29)]
        [InlineData("2017-02-29", false, 0, 0, 0)]
        [InlineData("2017-13-01", false, 0, 0, 0)]
        [InlineData("17-10-27", false, 0, 0, 0)]
        [InlineData("2017-10-27T00:00", false, 0, 0, 0)]
        public void TryParseSimpleDate(string input, bool expected, int year, int month, int day)
        {
            Assert.Equal(expected, DateTimePreparser.TryParseSimpleDate(input, out var y, out var m, out var d));
            Assert.Equal((year, month, day), (y, m, d));
        }

        [Theory]
        [InlineData("13:45", true, 13, 45)]
        [InlineData("24:00", true, 24, 0)]
        [InlineData("25:00", false, 0, 0)]
        [InlineData("1:45", false, 0, 0)]
        [InlineData("13:45:00", false, 0, 0)]
        public void TryParseSimpleTime(string input, bool expected, int hours, int minutes)
        {
            Assert.Equal(expected, DateTimePreparser.TryParseSimpleTime(input, out var h, out var m));
            Assert.Equal((hours, minutes), (h, m));
        }

        [Fact]
        public void NoDateTokensYieldsSingleRegularToken()
        {
            var preparser = Parse("plain text");
            Assert.Single(preparser.GetTextTokens());
            Assert.False(preparser.HasDateTokens());
        }
    }
}
