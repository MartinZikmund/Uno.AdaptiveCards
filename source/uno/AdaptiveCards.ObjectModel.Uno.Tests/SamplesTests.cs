// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    /// <summary>Parses (and round-trips) every Adaptive Card JSON in the repository's samples folder.</summary>
    public class SamplesTests
    {
        // Cards that are expected to fail parsing (negative test cards, or templates that are invalid until expanded).
        private static readonly HashSet<string> s_expectedFailures = new(StringComparer.OrdinalIgnoreCase)
        {
            "v1.2/Tests/Action.DuplicateIds.json",
            "v1.2/Tests/Action.NestedDuplicateIds.json",
            "v1.1/Tests/InvalidMediaMix.json",
            "v1.0/Tests/AdaptiveCard.MissingVersion.json",
            "v1.0/Tests/TypeIsRequired.json",
            "v1.6/Tests/Carousel.NestedForbiddenElements.json",
        };

        private static string FindSamplesRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var samples = Path.Combine(dir.FullName, "samples");
                if (Directory.Exists(samples) && Directory.Exists(Path.Combine(dir.FullName, "source")))
                {
                    return samples;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the repository 'samples' folder.");
        }

        private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

        private static bool IsExpectedFailure(string relativePath) =>
            s_expectedFailures.Contains(relativePath) || relativePath.StartsWith("Templates/", StringComparison.OrdinalIgnoreCase);

        public static IEnumerable<object[]> SampleCards()
        {
            var root = FindSamplesRoot();
            return Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
                .Where(f => File.ReadAllText(f).Contains("\"AdaptiveCard\""))
                .Select(f => Relative(root, f))
                .OrderBy(f => f, StringComparer.Ordinal)
                .Select(f => new object[] { f });
        }

        [Fact]
        public void SamplesFolderIsFound()
        {
            Assert.True(SampleCards().Count() > 300);
        }

        [Theory]
        [MemberData(nameof(SampleCards))]
        public void SampleParsesAndRoundTrips(string relativePath)
        {
            var json = File.ReadAllText(Path.Combine(FindSamplesRoot(), relativePath));
            var result = AdaptiveCard.FromJsonString(json);

            if (result.AdaptiveCard == null)
            {
                Assert.True(IsExpectedFailure(relativePath), $"{relativePath} failed to parse: {string.Join("; ", result.Errors)}");
                return;
            }

            if (s_expectedFailures.Contains(relativePath))
            {
                Assert.Fail($"{relativePath} was expected to fail parsing but succeeded");
            }

            var serialized = result.AdaptiveCard.ToJson().ToJsonString();
            var reparsed = AdaptiveCard.FromJsonString(serialized);
            Assert.True(reparsed.AdaptiveCard != null, $"{relativePath} failed to re-parse: {string.Join("; ", reparsed.Errors)}");
            Assert.Equal(serialized, reparsed.AdaptiveCard.ToJson().ToJsonString());
        }
    }
}
