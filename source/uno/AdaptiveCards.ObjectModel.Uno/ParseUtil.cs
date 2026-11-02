// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>
    /// Port of the shared model's ParseUtil. The helpers mimic jsoncpp semantics (e.g. a missing key, a JSON null,
    /// an empty array and an empty object are all "empty").
    /// </summary>
    internal static class ParseUtil
    {
        private static readonly JsonDocumentOptions s_documentOptions = new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        };

        private static readonly JsonSerializerOptions s_compactOptions = new() { WriteIndented = false };

        public static JsonNode ParseJsonString(string jsonString)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonString ?? string.Empty, s_documentOptions);
                return ToNode(document.RootElement);
            }
            catch (Exception e) when (e is JsonException || e is ArgumentException || e is InvalidOperationException)
            {
                throw new AdaptiveCardParseException(ErrorStatusCode.InvalidJson, "Expected JSON Object (" + e.Message + ")");
            }
        }

        /// <summary>
        /// Converts a JsonElement to a mutable JsonNode. Duplicate object keys keep the last value (jsoncpp semantics),
        /// which JsonNode.Parse does not tolerate.
        /// </summary>
        private static JsonNode ToNode(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new JsonObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        obj[property.Name] = ToNode(property.Value);
                    }

                    return obj;
                case JsonValueKind.Array:
                    var array = new JsonArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        array.Add(ToNode(item));
                    }

                    return array;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return JsonValue.Create(element.Clone());
            }
        }

        public static JsonObject GetJsonObjectFromString(string jsonString)
        {
            var node = ParseJsonString(jsonString);
            if (node is JsonObject obj)
            {
                return obj;
            }

            throw new AdaptiveCardParseException(ErrorStatusCode.InvalidJson, "Expected JSON Object\n");
        }

        public static string JsonToString(JsonNode node) => node == null ? "null" : node.ToJsonString(s_compactOptions);

        public static JsonObject ThrowIfNotJsonObject(JsonNode json)
        {
            if (json is JsonObject obj)
            {
                return obj;
            }

            throw new AdaptiveCardParseException(ErrorStatusCode.InvalidJson, "Expected JSON Object\n");
        }

        /// <summary>Returns the property value or null if missing (JSON null is also returned as null).</summary>
        public static JsonNode Get(JsonObject json, string key)
        {
            if (json != null && json.TryGetPropertyValue(key, out var value))
            {
                return value;
            }

            return null;
        }

        public static bool IsEmpty(JsonNode node) =>
            node == null || (node is JsonArray a && a.Count == 0) || (node is JsonObject o && o.Count == 0);

        public static bool IsString(JsonNode node) => node is JsonValue v && v.GetValueKind() == JsonValueKind.String;

        public static bool IsBool(JsonNode node) =>
            node is JsonValue v && (v.GetValueKind() == JsonValueKind.True || v.GetValueKind() == JsonValueKind.False);

        public static bool IsNumber(JsonNode node) => node is JsonValue v && v.GetValueKind() == JsonValueKind.Number;

        /// <summary>jsoncpp asString semantics.</summary>
        public static string AsString(JsonNode node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            if (node is JsonValue v)
            {
                switch (v.GetValueKind())
                {
                    case JsonValueKind.String:
                        return v.GetValue<string>();
                    case JsonValueKind.True:
                        return "true";
                    case JsonValueKind.False:
                        return "false";
                    case JsonValueKind.Number:
                        return v.ToJsonString();
                    case JsonValueKind.Null:
                        return string.Empty;
                }
            }

            throw new AdaptiveCardParseException(ErrorStatusCode.InvalidPropertyValue, "Value is not convertible to string");
        }

        public static bool TryGetDouble(JsonNode node, out double value)
        {
            value = 0;
            if (node is JsonValue v && v.GetValueKind() == JsonValueKind.Number)
            {
                value = v.GetValue<double>();
                return true;
            }

            return false;
        }

        private static bool IsUInt(JsonNode node, out uint value)
        {
            value = 0;
            if (TryGetDouble(node, out var d) && d >= 0 && d <= uint.MaxValue && Math.Floor(d) == d)
            {
                value = (uint)d;
                return true;
            }

            return false;
        }

        private static bool IsInt(JsonNode node, out int value)
        {
            value = 0;
            if (TryGetDouble(node, out var d) && d >= int.MinValue && d <= int.MaxValue && Math.Floor(d) == d)
            {
                value = (int)d;
                return true;
            }

            return false;
        }

        public static string GetTypeAsString(JsonObject json)
        {
            const string typeKey = "type";
            if (json == null || !json.ContainsKey(typeKey))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.RequiredPropertyMissing, "The JSON element is missing the following value: " + typeKey);
            }

            return AsString(json[typeKey]);
        }

        public static string TryGetTypeAsString(JsonObject json)
        {
            try
            {
                return GetTypeAsString(json);
            }
            catch (AdaptiveCardParseException)
            {
                return string.Empty;
            }
        }

        public static string GetString(JsonObject json, string key, bool isRequired = false)
        {
            var value = Get(json, key);
            if (IsEmpty(value))
            {
                if (isRequired)
                {
                    throw new AdaptiveCardParseException(
                        ErrorStatusCode.RequiredPropertyMissing, "Property is required but was found empty: " + key);
                }

                return string.Empty;
            }

            if (!IsString(value))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Value for property " + key + " was invalid. Expected type string.");
            }

            return value.GetValue<string>();
        }

        public static string GetString(JsonObject json, string key, string defaultValue, bool isRequired)
        {
            var result = GetString(json, key, isRequired);
            return string.IsNullOrEmpty(result) ? defaultValue : result;
        }

        public static string TryGetString(JsonObject json, string key)
        {
            try
            {
                return GetString(json, key);
            }
            catch (AdaptiveCardParseException)
            {
                return string.Empty;
            }
        }

        /// <summary>Returns the JSON serialization of the property (jsoncpp toStyledString).</summary>
        public static string GetJsonString(JsonObject json, string key, bool isRequired = false)
        {
            var value = Get(json, key);
            if (IsEmpty(value))
            {
                if (isRequired)
                {
                    throw new AdaptiveCardParseException(
                        ErrorStatusCode.RequiredPropertyMissing, "Property is required but was found empty: " + key);
                }

                return string.Empty;
            }

            return JsonToString(value);
        }

        public static string GetValueAsString(JsonObject json, string key, bool isRequired = false)
        {
            var value = Get(json, key);
            if (IsEmpty(value))
            {
                if (isRequired)
                {
                    throw new AdaptiveCardParseException(
                        ErrorStatusCode.RequiredPropertyMissing, "Property is required but was found empty: " + key);
                }

                return string.Empty;
            }

            return AsString(value);
        }

        public static bool? GetOptionalBool(JsonObject json, string key)
        {
            var value = Get(json, key);
            if (IsEmpty(value))
            {
                return null;
            }

            if (!IsBool(value))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Value for property " + key + " was invalid. Expected type bool.");
            }

            return value.GetValue<bool>();
        }

        public static bool GetBool(JsonObject json, string key, bool defaultValue, bool isRequired = false)
        {
            var value = GetOptionalBool(json, key);
            if (isRequired && !value.HasValue)
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.RequiredPropertyMissing, "Property is required but was found empty: " + key);
            }

            return value ?? defaultValue;
        }

        public static uint GetUInt(JsonObject json, string key, uint defaultValue, bool isRequired = false)
        {
            var value = Get(json, key);
            if (IsEmpty(value))
            {
                if (isRequired)
                {
                    throw new AdaptiveCardParseException(
                        ErrorStatusCode.RequiredPropertyMissing, "Property is required but was found empty: " + key);
                }

                return defaultValue;
            }

            if (!IsUInt(value, out var result))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Value for property " + key + " was invalid. Expected type uInt.");
            }

            return result;
        }

        public static int? GetOptionalInt(JsonObject json, string key)
        {
            var value = Get(json, key);
            if (IsEmpty(value))
            {
                return null;
            }

            if (!IsInt(value, out var result))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Value for property " + key + " was invalid. Expected type int.");
            }

            return result;
        }

        public static int GetInt(JsonObject json, string key, int defaultValue, bool isRequired = false)
        {
            var value = GetOptionalInt(json, key);
            if (isRequired && !value.HasValue)
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.RequiredPropertyMissing, "Property is required but was found empty: " + key);
            }

            return value ?? defaultValue;
        }

        public static uint? GetOptionalUnsignedInt(JsonObject json, string key)
        {
            var value = Get(json, key);
            if (IsEmpty(value))
            {
                return null;
            }

            if (!IsUInt(value, out var result))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Value for property " + key + " was invalid. Expected type unsigned int.");
            }

            return result;
        }

        public static double? GetOptionalDouble(JsonObject json, string key)
        {
            var value = Get(json, key);
            if (IsEmpty(value))
            {
                return null;
            }

            if (!TryGetDouble(value, out var result))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Value for property " + key + " was invalid. Expected type double.");
            }

            return result;
        }

        public static T? GetOptionalEnumValue<T>(JsonObject json, string key, EnumMap<T> map) where T : struct, Enum
        {
            var value = Get(json, key);
            if (IsEmpty(value))
            {
                return null;
            }

            if (!IsString(value))
            {
                throw new AdaptiveCardParseException(ErrorStatusCode.InvalidPropertyValue, "Enum type was invalid. Expected type string.");
            }

            return map.FromString(value.GetValue<string>());
        }

        public static T GetEnumValue<T>(JsonObject json, string key, T defaultValue, EnumMap<T> map, bool isRequired = false)
            where T : struct, Enum
        {
            var value = GetOptionalEnumValue(json, key, map);
            if (isRequired && !value.HasValue)
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.RequiredPropertyMissing, "Property is required but was found empty: " + key);
            }

            return value ?? defaultValue;
        }

        public static void ExpectTypeString(JsonObject json, string expectedTypeString)
        {
            var actualType = GetTypeAsString(json);
            if (expectedTypeString != actualType)
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue,
                    "The JSON element did not have the correct type. Expected: " + expectedTypeString + ", Actual: " + actualType);
            }
        }

        public static JsonArray GetArray(JsonObject json, string key, bool isRequired = false)
        {
            var value = Get(json, key);
            if (value != null && value is not JsonArray)
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Could not parse specified key: " + key + ". It was not an array");
            }

            var array = value as JsonArray;
            if (isRequired && (array == null || array.Count == 0))
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.RequiredPropertyMissing, "Could not parse required key: " + key + ". It was not found");
            }

            return array;
        }

        public static List<string> GetStringArray(JsonObject json, string key, bool isRequired = false)
        {
            var array = GetArray(json, key, isRequired);
            var result = new List<string>();
            if (array != null)
            {
                foreach (var item in array)
                {
                    result.Add(AsString(item));
                }
            }

            return result;
        }

        public static JsonNode ExtractJsonValue(JsonObject json, string key, bool isRequired = false)
        {
            var value = Get(json, key);
            if (isRequired && IsEmpty(value))
            {
                throw new AdaptiveCardParseException(ErrorStatusCode.RequiredPropertyMissing, "Could not extract required key: " + key + ".");
            }

            return value;
        }

        public static string ToLowercase(string value) => value?.ToLowerInvariant() ?? string.Empty;

        // ----- element/action collection helpers -----

        /// <summary>Parses a single card element via the registered parsers (BaseCardElement::ParseJsonObject).</summary>
        public static IAdaptiveCardElement ParseCardElement(ParseContext context, JsonNode jsonNode)
        {
            var json = ThrowIfNotJsonObject(jsonNode);
            var typeString = GetTypeAsString(json);
            context.ShouldParse(typeString);
            var element = context.ElementParsers.Deserialize(context, json, typeString);
            if (element == null)
            {
                throw new AdaptiveCardParseException(ErrorStatusCode.InvalidPropertyValue, "Unable to parse element of type " + typeString);
            }

            return element;
        }

        public static List<IAdaptiveCardElement> GetElementCollection(
            bool isTopToBottomContainer, ParseContext context, JsonObject json, string key, bool isRequired = false, string impliedType = null)
        {
            var result = new List<IAdaptiveCardElement>();
            foreach (var element in GetElementCollection(isTopToBottomContainer, context, json, key, isRequired, impliedType, ParseCardElement))
            {
                result.Add(element);
            }

            return result;
        }

        /// <summary>Port of ParseUtil::GetElementCollection (handles bleed state, implied types and prohibited types).</summary>
        public static List<T> GetElementCollection<T>(
            bool isTopToBottomContainer,
            ParseContext context,
            JsonObject json,
            string key,
            bool isRequired,
            string impliedType,
            Func<ParseContext, JsonObject, T> parse) where T : class
        {
            var elements = new List<T>();
            var array = GetArray(json, key, isRequired);
            if (array == null || array.Count == 0)
            {
                return elements;
            }

            var previousBleedState = context.GetBleedDirection();
            int count = array.Count;
            for (int index = 0; index < count; index++)
            {
                var currentBleedState = previousBleedState;
                if (index != 0)
                {
                    currentBleedState &= isTopToBottomContainer ? ~BleedDirection.Up : ~BleedDirection.Left;
                }

                if (index != count - 1)
                {
                    currentBleedState &= isTopToBottomContainer ? ~BleedDirection.Down : ~BleedDirection.Right;
                }

                context.PushBleedDirection(currentBleedState);

                var current = ThrowIfNotJsonObject(array[index]);
                if (!string.IsNullOrEmpty(impliedType))
                {
                    var typeString = GetString(current, "type", impliedType, false);
                    if (typeString != impliedType)
                    {
                        throw new AdaptiveCardParseException(ErrorStatusCode.InvalidPropertyValue, "Unable to parse element of type " + typeString);
                    }

                    current["type"] = typeString;
                }

                try
                {
                    context.ShouldParse(GetTypeAsString(current));
                    var element = parse(context, current);
                    elements.Add(element);
                }
                catch (AdaptiveCardParseException e) when (e.StatusCode == ErrorStatusCode.ProhibitedType)
                {
                    context.AddWarning(WarningStatusCode.ProhibitedTypeDetected, e.Message);
                }

                context.PopBleedDirection();
            }

            return elements;
        }

        public static List<T> GetElementCollectionOfSingleType<T>(
            ParseContext context, JsonObject json, string key, Func<ParseContext, JsonNode, T> deserializer, bool isRequired = false)
            where T : class
        {
            var result = new List<T>();
            var array = GetArray(json, key, isRequired);
            if (array == null)
            {
                return result;
            }

            foreach (var item in array)
            {
                var element = deserializer(context, item);
                if (element != null)
                {
                    result.Add(element);
                }
            }

            return result;
        }

        public static IAdaptiveActionElement GetActionFromJsonValue(ParseContext context, JsonNode jsonNode)
        {
            if (IsEmpty(jsonNode) || jsonNode is not JsonObject json)
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Expected a Json object to extract Action element");
            }

            var typeString = GetTypeAsString(json);
            context.ShouldParse(typeString);
            return context.ActionParsers.Deserialize(context, json, typeString);
        }

        public static List<IAdaptiveActionElement> GetActionCollection(ParseContext context, JsonObject json, string key, bool isRequired = false)
        {
            var result = new List<IAdaptiveActionElement>();
            var array = GetArray(json, key, isRequired);
            if (array == null)
            {
                return result;
            }

            foreach (var item in array)
            {
                var action = GetActionFromJsonValue(context, item);
                if (action != null)
                {
                    result.Add(action);
                }
            }

            return result;
        }

        public static IAdaptiveActionElement GetAction(ParseContext context, JsonObject json, string key, bool isRequired = false)
        {
            var value = ExtractJsonValue(json, key, isRequired);
            if (!IsEmpty(value))
            {
                return GetActionFromJsonValue(context, value);
            }

            return null;
        }

        // ----- Util.cpp -----

        public static string ValidateColor(string backgroundColor, ParseContext context)
        {
            if (string.IsNullOrEmpty(backgroundColor))
            {
                return backgroundColor ?? string.Empty;
            }

            int length = backgroundColor.Length;
            bool isValidColor = backgroundColor[0] == '#' && (length == 7 || length == 9);
            for (int i = 1; i < length && isValidColor; i++)
            {
                isValidColor = Uri.IsHexDigit(backgroundColor[i]);
            }

            if (!isValidColor)
            {
                context?.AddWarning(
                    WarningStatusCode.InvalidColorFormat,
                    "Image background color specified, but doesn't follow #AARRGGBB or #RRGGBB format");
                return "#00000000";
            }

            return length == 7 ? "#FF" + backgroundColor.Substring(1, 6) : backgroundColor;
        }

        private static bool ShouldParseForExplicitDimension(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            char ch = input[0];
            if (ch == '-' || ch == '.')
            {
                return true;
            }

            bool hasDigit = false;
            foreach (var c in input)
            {
                hasDigit |= char.IsDigit(c);
                if (hasDigit && (char.IsLetter(c) || c == '.'))
                {
                    return true;
                }
            }

            return false;
        }

        public static int? ParseSizeForPixelSize(string sizeString, ParseContext context)
        {
            if (!ShouldParseForExplicitDimension(sizeString))
            {
                return null;
            }

            const string warningMessage = "expected input argument to be specified as \\d+(\\.\\d+)?px with no spaces, but received ";
            var match = Regex.Match(sizeString, "^([1-9]+\\d*)(\\.\\d+)?(px)$", RegexOptions.CultureInvariant);
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }

                context?.AddWarning(WarningStatusCode.InvalidDimensionSpecified, "out of range: " + sizeString);
                return null;
            }

            context?.AddWarning(WarningStatusCode.InvalidDimensionSpecified, warningMessage + sizeString);
            return null;
        }

        public static void EnsureShowCardVersions(IEnumerable<IAdaptiveActionElement> actions, string version)
        {
            foreach (var action in actions)
            {
                if (action is AdaptiveShowCardAction showCard && showCard.Card != null && string.IsNullOrEmpty(showCard.Card.Version))
                {
                    showCard.Card.Version = version;
                }
            }
        }

        public static JsonObject HandleUnknownProperties(JsonObject json, ISet<string> knownProperties)
        {
            var unknown = new JsonObject();
            foreach (var kvp in json)
            {
                if (!knownProperties.Contains(kvp.Key))
                {
                    unknown[kvp.Key] = kvp.Value?.DeepClone();
                }
            }

            return unknown;
        }

        public static JsonNode Clone(JsonNode node) => node?.DeepClone();
    }
}
