// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Text.RegularExpressions;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>Port of the shared model's SemanticVersion (major.minor.build.revision).</summary>
    public readonly struct SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        private static readonly Regex s_versionMatch = new(@"^(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:\.(\d+))?$", RegexOptions.CultureInvariant);

        public SemanticVersion(string version)
        {
            var match = version == null ? null : s_versionMatch.Match(version);
            if (match == null || !match.Success)
            {
                throw new AdaptiveCardParseException(ErrorStatusCode.InvalidPropertyValue, "Semantic version invalid: " + version);
            }

            try
            {
                Major = match.Groups[1].Success ? uint.Parse(match.Groups[1].Value) : 0;
                Minor = match.Groups[2].Success ? uint.Parse(match.Groups[2].Value) : 0;
                Build = match.Groups[3].Success ? uint.Parse(match.Groups[3].Value) : 0;
                Revision = match.Groups[4].Success ? uint.Parse(match.Groups[4].Value) : 0;
            }
            catch (OverflowException)
            {
                throw new AdaptiveCardParseException(ErrorStatusCode.InvalidPropertyValue, "Semantic version invalid: " + version);
            }
        }

        public uint Major { get; }

        public uint Minor { get; }

        public uint Build { get; }

        public uint Revision { get; }

        public static bool TryParse(string version, out SemanticVersion result)
        {
            try
            {
                result = new SemanticVersion(version);
                return true;
            }
            catch (AdaptiveCardParseException)
            {
                result = default;
                return false;
            }
        }

        public int CompareTo(SemanticVersion other)
        {
            var c = Major.CompareTo(other.Major);
            if (c != 0) return c;
            c = Minor.CompareTo(other.Minor);
            if (c != 0) return c;
            c = Build.CompareTo(other.Build);
            if (c != 0) return c;
            return Revision.CompareTo(other.Revision);
        }

        public bool Equals(SemanticVersion other) => CompareTo(other) == 0;

        public override bool Equals(object obj) => obj is SemanticVersion v && Equals(v);

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Build, Revision);

        public static bool operator <(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) < 0;
        public static bool operator >(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) > 0;
        public static bool operator <=(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) <= 0;
        public static bool operator >=(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) >= 0;
        public static bool operator ==(SemanticVersion a, SemanticVersion b) => a.Equals(b);
        public static bool operator !=(SemanticVersion a, SemanticVersion b) => !a.Equals(b);

        public override string ToString() => $"{Major}.{Minor}.{Build}.{Revision}";
    }
}
