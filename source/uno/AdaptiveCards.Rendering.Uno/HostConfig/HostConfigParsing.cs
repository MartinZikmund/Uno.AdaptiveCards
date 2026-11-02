// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AdaptiveCards.ObjectModel.Uno;
using Windows.UI;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// JSON helpers used by the host config classes. Port of the shared model's HostConfig.cpp deserialization logic.
    /// </summary>
    internal static class HostConfigParsing
    {
        public static readonly EnumMap<ActionsOrientation> ActionsOrientationMap = new(new[]
        {
            (ActionsOrientation.Horizontal, "Horizontal"),
            (ActionsOrientation.Vertical, "Vertical"),
        });

        public static readonly EnumMap<ActionAlignment> ActionAlignmentMap = new(new[]
        {
            (ActionAlignment.Left, "Left"),
            (ActionAlignment.Center, "Center"),
            (ActionAlignment.Right, "Right"),
            (ActionAlignment.Stretch, "Stretch"),
        });

        public static readonly EnumMap<ActionMode> ActionModeMap = new(new[]
        {
            (ActionMode.Inline, "Inline"),
            (ActionMode.Popup, "Popup"),
        });

        public static readonly EnumMap<IconPlacement> IconPlacementMap = new(new[]
        {
            (IconPlacement.AboveTitle, "AboveTitle"),
            (IconPlacement.LeftOfTitle, "LeftOfTitle"),
        });

        private static readonly Regex s_colorRegex = new(
            "^#([0-9a-f]{2})?([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Port of ParseUtil::ExtractJsonValueAndMergeWithDefault: returns the nested object, or null when the
        /// property is missing/empty/not an object (in which case the default is kept).
        /// </summary>
        public static JsonObject GetObject(JsonObject json, string key)
        {
            var value = ParseUtil.Get(json, key);
            if (value is JsonObject obj && obj.Count > 0)
            {
                return obj;
            }

            return null;
        }

        /// <summary>Returns the string property or <paramref name="defaultValue"/> when missing/empty.</summary>
        public static string GetStringOrDefault(JsonObject json, string key, string defaultValue)
        {
            var value = ParseUtil.GetString(json, key);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        public static Color GetColorOrDefault(JsonObject json, string key, Color defaultValue)
        {
            var value = ParseUtil.GetString(json, key);
            return string.IsNullOrEmpty(value) ? defaultValue : GetColorFromString(value);
        }

        /// <summary>Port of GetColorFromString (SharedRenderer Util.cpp). Invalid strings yield transparent black.</summary>
        public static Color GetColorFromString(string colorString)
        {
            var color = default(Color);
            if (!string.IsNullOrEmpty(colorString) && colorString[0] == '#')
            {
                var match = s_colorRegex.Match(colorString);
                if (match.Success)
                {
                    color.A = 0xFF;
                    if (match.Groups[1].Success)
                    {
                        color.A = ParseChannel(match.Groups[1].Value, 0xFF);
                    }

                    color.R = ParseChannel(match.Groups[2].Value, 0);
                    color.G = ParseChannel(match.Groups[3].Value, 0);
                    color.B = ParseChannel(match.Groups[4].Value, 0);
                }
            }

            return color;
        }

        private static byte ParseChannel(string value, byte defaultValue) =>
            byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b) ? b : defaultValue;

        public static Color C(uint argb) =>
            Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
    }
}
