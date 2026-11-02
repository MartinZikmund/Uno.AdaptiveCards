// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>Port of the shared TextElementProperties, shared by TextBlock and TextRun.</summary>
    internal static class TextElementProperties
    {
        private static readonly Regex s_htmlEntities = new("&(amp|quot|lt|gt|nbsp);", RegexOptions.CultureInvariant);

        public static string ProcessHtmlEntities(string input)
        {
            if (string.IsNullOrEmpty(input) || !s_htmlEntities.IsMatch(input))
            {
                return input ?? string.Empty;
            }

            return input.Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&nbsp;", " ").Replace("&amp;", "&");
        }

        public static void PopulateKnownProperties(ISet<string> knownProperties)
        {
            knownProperties.Add("text");
            knownProperties.Add("size");
            knownProperties.Add("color");
            knownProperties.Add("weight");
            knownProperties.Add("fontType");
            knownProperties.Add("isSubtle");
        }

        public static void Serialize(IAdaptiveTextElement element, JsonObject root)
        {
            if (element.Size.HasValue)
            {
                root["size"] = EnumStrings.TextSize.ToString(element.Size.Value);
            }

            if (element.Color.HasValue)
            {
                root["color"] = EnumStrings.ForegroundColor.ToString(element.Color.Value);
            }

            if (element.Weight.HasValue)
            {
                root["weight"] = EnumStrings.TextWeight.ToString(element.Weight.Value);
            }

            if (element.FontType.HasValue)
            {
                root["fontType"] = EnumStrings.FontType.ToString(element.FontType.Value);
            }

            if (element.IsSubtle.HasValue)
            {
                root["isSubtle"] = element.IsSubtle.Value;
            }

            root["text"] = element.Text ?? string.Empty;
        }

        public static void Deserialize(IAdaptiveTextElement element, ParseContext context, JsonObject json)
        {
            element.Text = ProcessHtmlEntities(ParseUtil.GetString(json, "text", false));
            if (string.IsNullOrEmpty(element.Text))
            {
                context.AddWarning(WarningStatusCode.RequiredPropertyMissing, "required property, \"text\", is either empty or missing");
            }

            element.Size = ParseUtil.GetOptionalEnumValue(json, "size", EnumStrings.TextSize);
            element.Color = ParseUtil.GetOptionalEnumValue(json, "color", EnumStrings.ForegroundColor);
            element.Weight = ParseUtil.GetOptionalEnumValue(json, "weight", EnumStrings.TextWeight);
            element.FontType = ParseUtil.GetOptionalEnumValue(json, "fontType", EnumStrings.FontType);
            element.IsSubtle = ParseUtil.GetOptionalBool(json, "isSubtle");
            element.Language = context.Language;
        }
    }
}
