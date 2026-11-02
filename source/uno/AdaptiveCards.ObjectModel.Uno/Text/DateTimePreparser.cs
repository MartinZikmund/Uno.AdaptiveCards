// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AdaptiveCards.ObjectModel.Uno
{
    public enum DateTimePreparsedTokenFormat
    {
        RegularString = 0,
        Time,
        DateCompact,
        DateShort,
        DateLong,
    }

    /// <summary>Port of the shared DateTimePreparsedToken.</summary>
    public sealed class DateTimePreparsedToken
    {
        public DateTimePreparsedToken(string text, DateTimePreparsedTokenFormat format)
        {
            Text = text ?? string.Empty;
            Format = format;
        }

        public DateTimePreparsedToken(string text, DateTimePreparsedTokenFormat format, int year, int month, int day)
            : this(text, format)
        {
            Year = year;
            Month = month;
            Day = day;
        }

        public string Text { get; }

        public DateTimePreparsedTokenFormat Format { get; }

        /// <summary>Day of month, 1-31.</summary>
        public int Day { get; }

        /// <summary>Month, 0-11 (as in the shared model's struct tm).</summary>
        public int Month { get; }

        /// <summary>Full year (e.g. 2017).</summary>
        public int Year { get; }

        public string GetText() => Text;

        public DateTimePreparsedTokenFormat GetFormat() => Format;

        public int GetDay() => Day;

        public int GetMonth() => Month;

        public int GetYear() => Year;
    }

    /// <summary>
    /// Splits text into regular string tokens and {{DATE(...)}} / {{TIME(...)}} tokens. Port of the shared DateTimePreparser.
    /// TIME tokens are converted to local time and emitted as regular strings formatted "hh:mm tt" (C locale %I:%M %p);
    /// DATE tokens carry the local date and the requested style.
    /// </summary>
    public sealed class DateTimePreparser
    {
        private static readonly Regex s_pattern = new(
            "\\{\\{((DATE)|(TIME))\\(([0-9]{4})-{1}([0-9]{2})-{1}([0-9]{2})T([0-9]{2}):{1}([0-9]{2}):{1}([0-9]{2})(Z|(([+-])([0-9]{2}" +
            "):{1}([0-9]{2})))((((, ?SHORT)|(, ?LONG))|(, ?COMPACT))|)\\)\\}\\}",
            RegexOptions.CultureInvariant);

        private static readonly Regex s_timeMatch = new("^([0-9]{2}):([0-9]{2})$", RegexOptions.CultureInvariant);
        private static readonly Regex s_dateMatch = new("^([0-9]{4})-([0-9]{2})-([0-9]{2})$", RegexOptions.CultureInvariant);

        private const int IsDateGroup = 2;
        private const int TimeZoneGroup = 12;
        private const int FormatGroup = 15;
        private const int StyleGroup = 16;
        private static readonly int[] s_indexer = { 4, 5, 6, 7, 8, 9, 13, 14 };

        private readonly List<DateTimePreparsedToken> m_textTokenCollection = new();
        private readonly TimeZoneInfo m_timeZone;

        public DateTimePreparser()
        {
            m_timeZone = TimeZoneInfo.Local;
        }

        public DateTimePreparser(string text) : this(text, TimeZoneInfo.Local)
        {
        }

        /// <summary>Parses <paramref name="text"/>, converting times into <paramref name="localTimeZone"/>.</summary>
        public DateTimePreparser(string text, TimeZoneInfo localTimeZone)
        {
            m_timeZone = localTimeZone ?? TimeZoneInfo.Local;
            ParseDateTime(text ?? string.Empty);
        }

        public IReadOnlyList<DateTimePreparsedToken> GetTextTokens() => m_textTokenCollection;

        public bool HasDateTokens() { return m_hasDateTokens; }

        private bool m_hasDateTokens;

        private void AddTextToken(string text, DateTimePreparsedTokenFormat format)
        {
            if (!string.IsNullOrEmpty(text))
            {
                m_textTokenCollection.Add(new DateTimePreparsedToken(text, format));
            }
        }

        private void AddDateToken(string text, DateTime date, DateTimePreparsedTokenFormat format)
        {
            m_textTokenCollection.Add(new DateTimePreparsedToken(text, format, date.Year, date.Month - 1, date.Day));
            m_hasDateTokens = true;
        }

        private static bool IsValidDate(int year, int month, int day)
        {
            if (month <= 12 && day <= 31)
            {
                if (month == 4 || month == 6 || month == 9 || month == 11)
                {
                    return day <= 30;
                }
                else if (month == 2)
                {
                    if ((year % 4 == 0 && year % 100 != 0) || year % 400 == 0)
                    {
                        return day <= 29;
                    }

                    return day <= 28;
                }

                return true;
            }

            return false;
        }

        private static bool IsValidTime(int hours, int minutes, int seconds) => hours <= 24 && minutes <= 60 && seconds <= 60;

        private void ParseDateTime(string input)
        {
            if (!input.Contains("{{"))
            {
                AddTextToken(input, DateTimePreparsedTokenFormat.RegularString);
                return;
            }

            string text = input;
            Match matches;
            while ((matches = s_pattern.Match(text)).Success)
            {
                int formatStyle = 0;
                bool isDate = matches.Groups[IsDateGroup].Success;
                var values = new int[s_indexer.Length]; // year, month, day, hour, min, sec, tzHours, tzMinutes

                if (matches.Groups[StyleGroup].Success)
                {
                    var format = matches.Groups[FormatGroup].Value;
                    bool formatHasSpace = format[1] == ' ';
                    formatStyle = format[formatHasSpace ? 2 : 1];
                }

                AddTextToken(text.Substring(0, matches.Index), DateTimePreparsedTokenFormat.RegularString);
                var suffix = text.Substring(matches.Index + matches.Length);

                if (!isDate && formatStyle != 0)
                {
                    AddTextToken(matches.Value, DateTimePreparsedTokenFormat.RegularString);
                    text = suffix;
                    continue;
                }

                for (int idx = 0; idx < s_indexer.Length; idx++)
                {
                    var group = matches.Groups[s_indexer[idx]];
                    if (group.Success)
                    {
                        values[idx] = int.Parse(group.Value, NumberStyles.None, CultureInfo.InvariantCulture);
                    }
                }

                int year = values[0], month = values[1], day = values[2], hour = values[3], minute = values[4], second = values[5];
                int tzHours = values[6], tzMinutes = values[7];

                if (IsValidDate(year, month, day) && IsValidTime(hour, minute, second) && IsValidTime(tzHours, tzMinutes, 0))
                {
                    // offset from UTC of the specified time (Z == UTC)
                    var offset = TimeSpan.Zero;
                    if (matches.Groups[TimeZoneGroup].Success)
                    {
                        offset = new TimeSpan(tzHours, tzMinutes, 0);
                        if (matches.Groups[TimeZoneGroup].Value[0] == '-')
                        {
                            offset = -offset;
                        }
                    }

                    DateTime local;
                    try
                    {
                        // struct tm/mktime normalizes out-of-range fields (e.g. hour 24, second 60, month 0, day 0)
                        var utc = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                            .AddMonths(month - 1)
                            .AddDays(day - 1)
                            .AddHours(hour)
                            .AddMinutes(minute)
                            .AddSeconds(second)
                            - offset;
                        local = TimeZoneInfo.ConvertTimeFromUtc(utc, m_timeZone);
                    }
                    catch (ArgumentException)
                    {
                        AddTextToken(matches.Value, DateTimePreparsedTokenFormat.RegularString);
                        text = suffix;
                        continue;
                    }

                    if (isDate)
                    {
                        switch (formatStyle)
                        {
                            case 'S':
                                AddDateToken(matches.Value, local, DateTimePreparsedTokenFormat.DateShort);
                                break;
                            case 'L':
                                AddDateToken(matches.Value, local, DateTimePreparsedTokenFormat.DateLong);
                                break;
                            default:
                                AddDateToken(matches.Value, local, DateTimePreparsedTokenFormat.DateCompact);
                                break;
                        }
                    }
                    else
                    {
                        AddTextToken(local.ToString("hh:mm tt", CultureInfo.InvariantCulture), DateTimePreparsedTokenFormat.RegularString);
                    }
                }
                else
                {
                    AddTextToken(matches.Value, DateTimePreparsedTokenFormat.RegularString);
                }

                text = suffix;
            }

            AddTextToken(text, DateTimePreparsedTokenFormat.RegularString);
        }

        /// <summary>Parses a time of the form HH:MM.</summary>
        public static bool TryParseSimpleTime(string value, out int hours, out int minutes)
        {
            hours = 0;
            minutes = 0;
            if (value == null)
            {
                return false;
            }

            var match = s_timeMatch.Match(value);
            if (match.Success)
            {
                int parsedHours = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                int parsedMinutes = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                if (IsValidTime(parsedHours, parsedMinutes, 0))
                {
                    hours = parsedHours;
                    minutes = parsedMinutes;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Parses a date of the form YYYY-MM-DD.</summary>
        public static bool TryParseSimpleDate(string value, out int year, out int month, out int day)
        {
            year = 0;
            month = 0;
            day = 0;
            if (value == null)
            {
                return false;
            }

            var match = s_dateMatch.Match(value);
            if (match.Success)
            {
                int parsedYear = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                int parsedMonth = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                int parsedDay = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                if (IsValidDate(parsedYear, parsedMonth, parsedDay))
                {
                    year = parsedYear;
                    month = parsedMonth;
                    day = parsedDay;
                    return true;
                }
            }

            return false;
        }
    }
}
