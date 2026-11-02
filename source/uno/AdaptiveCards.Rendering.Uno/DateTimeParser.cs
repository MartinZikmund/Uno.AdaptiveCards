// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Globalization;
using System.Text;
using AdaptiveCards.ObjectModel.Uno;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Port of DateTimeParser.cpp: turns the DATE()/TIME() tokens produced by <see cref="DateTimePreparser"/> into
    /// localized strings using the card/element language.
    /// </summary>
    internal sealed class DateTimeParser
    {
        private readonly string _languageString;

        public DateTimeParser(string language)
        {
            _languageString = language ?? string.Empty;
        }

        public string GenerateString(string text) => GenerateString(new DateTimePreparser(text ?? string.Empty));

        public string GenerateString(DateTimePreparser text)
        {
            var builder = new StringBuilder();
            CultureInfo culture = CultureInfo.CurrentCulture;
            if (text.HasDateTokens())
            {
                try
                {
                    if (!string.IsNullOrEmpty(_languageString))
                    {
                        culture = CultureInfo.GetCultureInfo(_languageString);
                    }
                }
                catch (CultureNotFoundException)
                {
                    culture = CultureInfo.CurrentCulture;
                }
            }

            foreach (var textSection in text.GetTextTokens())
            {
                switch (textSection.Format)
                {
                    case DateTimePreparsedTokenFormat.DateCompact:
                        builder.Append(ToDate(textSection).ToString("d", culture));
                        break;

                    case DateTimePreparsedTokenFormat.DateShort:
                        // C++: "%a, %b %e, %Y"
                        builder.Append(ToDate(textSection).ToString("ddd, MMM d, yyyy", culture));
                        break;

                    case DateTimePreparsedTokenFormat.DateLong:
                        // C++: "%A, %B %e, %Y"
                        builder.Append(ToDate(textSection).ToString("dddd, MMMM d, yyyy", culture));
                        break;

                    default:
                        builder.Append(textSection.Text);
                        break;
                }
            }

            return builder.ToString();
        }

        private static DateTime ToDate(DateTimePreparsedToken token)
        {
            // struct tm semantics: month is 0-based, year < 1900 is clamped to 1900, mktime normalizes out-of-range days.
            int year = token.Year >= 1900 ? token.Year : 1900;
            try
            {
                return new DateTime(year, 1, 1).AddMonths(token.Month).AddDays(token.Day - 1);
            }
            catch (ArgumentOutOfRangeException)
            {
                return new DateTime(year, 1, 1);
            }
        }
    }
}
