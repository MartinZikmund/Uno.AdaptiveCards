// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Thrown by renderers to request fallback (port of E_PERFORM_FALLBACK).
    /// </summary>
    internal sealed class PerformFallbackException : Exception
    {
        public PerformFallbackException() : base("Perform fallback")
        {
        }
    }

    /// <summary>Port of the shared model's AdaptiveBase64Util (used for data: URIs).</summary>
    internal static class AdaptiveBase64Util
    {
        public static string ExtractDataFromUri(string dataUri)
        {
            if (dataUri == null)
            {
                return string.Empty;
            }

            int commaPosition = dataUri.LastIndexOf(',');
            if (commaPosition >= 0)
            {
                int start = commaPosition + 1;
                while (start < dataUri.Length && char.IsWhiteSpace(dataUri[start]))
                {
                    start++;
                }

                return dataUri.Substring(start);
            }

            return string.Empty;
        }

        public static byte[] Decode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return Array.Empty<byte>();
            }

            // Lenient decoding: drop anything that is not a base64 character and fix the padding.
            var sb = new StringBuilder(data.Length);
            foreach (var c in data)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/')
                {
                    sb.Append(c);
                }
                else if (c == '-')
                {
                    sb.Append('+');
                }
                else if (c == '_')
                {
                    sb.Append('/');
                }
            }

            int mod = sb.Length % 4;
            if (mod == 1)
            {
                sb.Length -= 1;
            }
            else if (mod > 1)
            {
                sb.Append('=', 4 - mod);
            }

            try
            {
                return Convert.FromBase64String(sb.ToString());
            }
            catch (FormatException)
            {
                return Array.Empty<byte>();
            }
        }
    }

    /// <summary>Port of the WinUI3 renderer's Util.cpp free functions.</summary>
    internal static class Util
    {
        private static readonly Regex s_colorRegex = new(
            "^#([0-9a-f]{2})?([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static double? TryHStringToDouble(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            // std::stod parses the longest valid prefix.
            var match = Regex.Match(input.TrimStart(), @"^[+-]?(\d+(\.\d*)?|\.\d+)([eE][+-]?\d+)?");
            if (match.Success && double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            return null;
        }

        public static Color GetColorFromString(string colorString)
        {
            if (!string.IsNullOrEmpty(colorString) && colorString[0] == '#')
            {
                var match = s_colorRegex.Match(colorString);
                if (match.Success)
                {
                    byte a = 0xFF;
                    if (match.Groups[1].Success)
                    {
                        a = byte.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    }

                    byte r = byte.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte g = byte.Parse(match.Groups[3].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte b = byte.Parse(match.Groups[4].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return Color.FromArgb(a, r, g, b);
                }
            }

            return Color.FromArgb(0, 0, 0, 0);
        }

        public static AdaptiveContainerStyleDefinition GetContainerStyleDefinition(ContainerStyle style, AdaptiveHostConfig hostConfig)
        {
            var containerStyles = hostConfig.ContainerStyles;
            return style switch
            {
                ContainerStyle.Accent => containerStyles.Accent,
                ContainerStyle.Attention => containerStyles.Attention,
                ContainerStyle.Emphasis => containerStyles.Emphasis,
                ContainerStyle.Good => containerStyles.Good,
                ContainerStyle.Warning => containerStyles.Warning,
                _ => containerStyles.Default,
            };
        }

        public static Color GetColorFromAdaptiveColor(
            AdaptiveHostConfig hostConfig, ForegroundColor adaptiveColor, ContainerStyle containerStyle, bool isSubtle, bool highlight)
        {
            var styleDefinition = GetContainerStyleDefinition(containerStyle, hostConfig);
            var colorsConfig = styleDefinition.ForegroundColors;
            var colorConfig = adaptiveColor switch
            {
                ForegroundColor.Accent => colorsConfig.Accent,
                ForegroundColor.Dark => colorsConfig.Dark,
                ForegroundColor.Light => colorsConfig.Light,
                ForegroundColor.Good => colorsConfig.Good,
                ForegroundColor.Warning => colorsConfig.Warning,
                ForegroundColor.Attention => colorsConfig.Attention,
                _ => colorsConfig.Default,
            };

            if (highlight)
            {
                var highlightColorConfig = colorConfig.HighlightColors;
                return isSubtle ? highlightColorConfig.Subtle : highlightColorConfig.Default;
            }

            return isSubtle ? colorConfig.Subtle : colorConfig.Default;
        }

        public static TextHighlighter GetHighlighter(
            IAdaptiveTextElement adaptiveTextElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var textHighlighter = new TextHighlighter();
            var hostConfig = renderContext.HostConfig;
            var adaptiveForegroundColor = adaptiveTextElement.Color ?? ForegroundColor.Default;
            bool isSubtle = adaptiveTextElement.IsSubtle ?? false;
            var containerStyle = renderArgs.ContainerStyle;

            var backgroundColor = GetColorFromAdaptiveColor(hostConfig, adaptiveForegroundColor, containerStyle, isSubtle, true);
            var foregroundColor = GetColorFromAdaptiveColor(hostConfig, adaptiveForegroundColor, containerStyle, isSubtle, false);

            textHighlighter.Background = new SolidColorBrush(backgroundColor);
            textHighlighter.Foreground = new SolidColorBrush(foregroundColor);
            return textHighlighter;
        }

        public static uint GetSpacingSizeFromSpacing(AdaptiveHostConfig hostConfig, Spacing spacing)
        {
            var spacingConfig = hostConfig.Spacing;
            return spacing switch
            {
                Spacing.None => 0,
                Spacing.Small => spacingConfig.Small,
                Spacing.Medium => spacingConfig.Medium,
                Spacing.Large => spacingConfig.Large,
                Spacing.ExtraLarge => spacingConfig.ExtraLarge,
                Spacing.Padding => spacingConfig.Padding,
                _ => spacingConfig.Default,
            };
        }

        public static Color GetBackgroundColorFromStyle(ContainerStyle style, AdaptiveHostConfig hostConfig) =>
            GetContainerStyleDefinition(style, hostConfig).BackgroundColor;

        public static Color GetBorderColorFromStyle(ContainerStyle style, AdaptiveHostConfig hostConfig) =>
            GetContainerStyleDefinition(style, hostConfig).BorderColor;

        public static string GetFontFamilyFromFontType(AdaptiveHostConfig hostConfig, FontType fontType)
        {
            var typeDefinition = GetFontType(hostConfig, fontType);
            var fontFamily = typeDefinition.FontFamily;
            if (string.IsNullOrEmpty(fontFamily))
            {
                if (fontType == FontType.Monospace)
                {
                    fontFamily = "Courier New";
                }
                else
                {
                    fontFamily = hostConfig.FontFamily;
                    if (string.IsNullOrEmpty(fontFamily))
                    {
                        fontFamily = "Segoe UI";
                    }
                }
            }

            return fontFamily;
        }

        public static uint GetFontSizeFromFontType(AdaptiveHostConfig hostConfig, FontType fontType, TextSize desiredSize)
        {
            var fontTypeDefinition = GetFontType(hostConfig, fontType);
            uint result = GetFontSize(fontTypeDefinition.FontSizes, desiredSize);

            if (result == uint.MaxValue)
            {
                fontTypeDefinition = GetFontType(hostConfig, FontType.Default);
                result = GetFontSize(fontTypeDefinition.FontSizes, desiredSize);

                if (result == uint.MaxValue)
                {
                    result = GetFontSize(hostConfig.FontSizes, desiredSize);

                    if (result == uint.MaxValue)
                    {
                        result = desiredSize switch
                        {
                            TextSize.Small => 10,
                            TextSize.Medium => 14,
                            TextSize.Large => 17,
                            TextSize.ExtraLarge => 20,
                            _ => 12,
                        };
                    }
                }
            }

            return result;
        }

        public static FontWeight GetFontWeightFromStyle(AdaptiveHostConfig hostConfig, FontType fontType, TextWeight desiredWeight)
        {
            var fontTypeDefinition = GetFontType(hostConfig, fontType);
            ushort result = GetFontWeight(fontTypeDefinition.FontWeights, desiredWeight);

            if (result == ushort.MaxValue)
            {
                fontTypeDefinition = GetFontType(hostConfig, FontType.Default);
                result = GetFontWeight(fontTypeDefinition.FontWeights, desiredWeight);

                if (result == ushort.MaxValue)
                {
                    result = GetFontWeight(hostConfig.FontWeights, desiredWeight);

                    if (result == ushort.MaxValue)
                    {
                        result = desiredWeight switch
                        {
                            TextWeight.Lighter => 200,
                            TextWeight.Bolder => 800,
                            _ => 400,
                        };
                    }
                }
            }

            return new FontWeight { Weight = result };
        }

        public static AdaptiveFontTypeDefinition GetFontType(AdaptiveHostConfig hostConfig, FontType fontType)
        {
            var fontTypes = hostConfig.FontTypes;
            return fontType == FontType.Monospace ? fontTypes.Monospace : fontTypes.Default;
        }

        public static uint GetFontSize(AdaptiveFontSizesConfig sizesConfig, TextSize desiredSize)
        {
            if (sizesConfig == null)
            {
                return uint.MaxValue;
            }

            return desiredSize switch
            {
                TextSize.Small => sizesConfig.Small,
                TextSize.Medium => sizesConfig.Medium,
                TextSize.Large => sizesConfig.Large,
                TextSize.ExtraLarge => sizesConfig.ExtraLarge,
                _ => sizesConfig.Default,
            };
        }

        public static ushort GetFontWeight(AdaptiveFontWeightsConfig weightsConfig, TextWeight desiredWeight)
        {
            if (weightsConfig == null)
            {
                return ushort.MaxValue;
            }

            return desiredWeight switch
            {
                TextWeight.Lighter => weightsConfig.Lighter,
                TextWeight.Bolder => weightsConfig.Bolder,
                _ => weightsConfig.Default,
            };
        }

        public static JsonObject StringToJsonObject(string inputString) => HStringToJsonObject(inputString);

        public static JsonObject HStringToJsonObject(string inputString)
        {
            try
            {
                return JsonNode.Parse(inputString ?? string.Empty) as JsonObject ?? new JsonObject();
            }
            catch (Exception)
            {
                return new JsonObject();
            }
        }

        public static string JsonObjectToHString(JsonObject inputJson) => inputJson?.ToJsonString() ?? string.Empty;

        public static string JsonObjectToString(JsonObject inputJson) => JsonObjectToHString(inputJson);

        private static bool MeetsRequirements(System.Collections.Generic.IEnumerable<AdaptiveRequirement> requirements, AdaptiveFeatureRegistration featureRegistration)
        {
            bool meetsRequirementsLocal = true;
            if (requirements != null)
            {
                foreach (var req in requirements)
                {
                    var registrationVersion = featureRegistration.Get(req.Name);
                    if (string.IsNullOrEmpty(registrationVersion))
                    {
                        meetsRequirementsLocal = false;
                    }
                    else
                    {
                        var requirementVersionString = req.Version;
                        if (requirementVersionString != "*" && registrationVersion != "*")
                        {
                            // The parser stores "*" requirements as "0" so any registered version satisfies them.
                            if (SemanticVersion.TryParse(requirementVersionString, out var requirementSemanticVersion) &&
                                SemanticVersion.TryParse(registrationVersion, out var registrationSemanticVersion) &&
                                registrationSemanticVersion < requirementSemanticVersion)
                            {
                                meetsRequirementsLocal = false;
                            }
                        }
                    }
                }
            }

            return meetsRequirementsLocal;
        }

        public static bool MeetsRequirements(IAdaptiveCardElement cardElement, AdaptiveFeatureRegistration featureRegistration) =>
            MeetsRequirements(cardElement?.Requirements, featureRegistration);

        /// <summary>
        /// Extension beyond the WinUI3 renderer: actions parsed by the shared model carry "requires" too
        /// (exposed on <see cref="AdaptiveActionElementBase"/>), so they can be checked against the host features.
        /// </summary>
        public static bool MeetsRequirements(IAdaptiveActionElement action, AdaptiveFeatureRegistration featureRegistration) =>
            action is AdaptiveActionElementBase actionBase ? MeetsRequirements(actionBase.Requirements, featureRegistration) : true;

        public static bool IsBackgroundImageValid(AdaptiveBackgroundImage backgroundImage) =>
            backgroundImage != null && !string.IsNullOrEmpty(backgroundImage.Url);

        private static Uri UriTryCreate(string uriString, string baseUriString = null)
        {
            if (string.IsNullOrEmpty(uriString))
            {
                return null;
            }

            if (string.IsNullOrEmpty(baseUriString))
            {
                return Uri.TryCreate(uriString, UriKind.Absolute, out var uri) ? uri : null;
            }

            if (Uri.TryCreate(baseUriString, UriKind.Absolute, out var baseUri) && Uri.TryCreate(baseUri, uriString, out var combined))
            {
                return combined;
            }

            return null;
        }

        public static Uri GetUrlFromString(AdaptiveHostConfig hostConfig, string urlString)
        {
            var uriFromAbsoluteUri = UriTryCreate(urlString);
            if (uriFromAbsoluteUri != null)
            {
                return uriFromAbsoluteUri;
            }

            return UriTryCreate(urlString, hostConfig?.ImageBaseUrl);
        }

        public static Color GenerateLHoverColor(Color originalColor)
        {
            const double hoverIncrement = 0.25;
            return Color.FromArgb(
                originalColor.A,
                (byte)(originalColor.R - (byte)(originalColor.R * hoverIncrement)),
                (byte)(originalColor.G - (byte)(originalColor.G * hoverIncrement)),
                (byte)(originalColor.B - (byte)(originalColor.B * hoverIncrement)));
        }

        public static DateTimeOffset GetDateTime(uint year, uint month, uint day)
        {
            // Local midnight of the given day (the C++ converts from local time to system time).
            var local = new DateTime((int)year, (int)month, (int)day, 0, 0, 0, DateTimeKind.Local);
            return new DateTimeOffset(local);
        }

        public static DateTimeOffset? GetDateTimeReference(uint year, uint month, uint day) => GetDateTime(year, month, day);

        public static IAdaptiveTextElement CopyTextElement(IAdaptiveTextElement textElement)
        {
            if (textElement == null)
            {
                return null;
            }

            return new AdaptiveTextRun
            {
                Color = textElement.Color,
                FontType = textElement.FontType,
                IsSubtle = textElement.IsSubtle,
                Language = textElement.Language,
                Size = textElement.Size,
                Weight = textElement.Weight,
                Text = textElement.Text,
            };
        }

        public static void RegisterDefaultElementRenderers(AdaptiveElementRendererRegistration registration, XamlBuilder xamlBuilder)
        {
            registration.Set("ActionSet", new AdaptiveActionSetRenderer());
            registration.Set("Carousel", new AdaptiveCarouselRenderer(xamlBuilder));
            registration.Set("CarouselPage", new AdaptiveCarouselPageRenderer());
            registration.Set("Column", new AdaptiveColumnRenderer());
            registration.Set("ColumnSet", new AdaptiveColumnSetRenderer());
            registration.Set("Container", new AdaptiveContainerRenderer());
            registration.Set("FactSet", new AdaptiveFactSetRenderer());
            registration.Set("Image", new AdaptiveImageRenderer(xamlBuilder));
            registration.Set("ImageSet", new AdaptiveImageSetRenderer());
            registration.Set("Input.ChoiceSet", new AdaptiveChoiceSetInputRenderer());
            registration.Set("Input.Date", new AdaptiveDateInputRenderer());
            registration.Set("Input.Number", new AdaptiveNumberInputRenderer());
            registration.Set("Input.Text", new AdaptiveTextInputRenderer());
            registration.Set("Input.Time", new AdaptiveTimeInputRenderer());
            registration.Set("Input.Toggle", new AdaptiveToggleInputRenderer());
            registration.Set("Media", new AdaptiveMediaRenderer());
            registration.Set("RichTextBlock", new AdaptiveRichTextBlockRenderer());
            registration.Set("Table", new AdaptiveTableRenderer());
            registration.Set("TextBlock", new AdaptiveTextBlockRenderer());
        }

        public static void RegisterDefaultActionRenderers(AdaptiveActionRendererRegistration registration)
        {
            registration.Set("Action.OpenUrl", new AdaptiveOpenUrlActionRenderer());
            registration.Set("Action.ShowCard", new AdaptiveShowCardActionRenderer());
            registration.Set("Action.Submit", new AdaptiveSubmitActionRenderer());
            registration.Set("Action.ToggleVisibility", new AdaptiveToggleVisibilityActionRenderer());
            registration.Set("Action.Execute", new AdaptiveExecuteActionRenderer());
        }

        public static string ExtractSvgDataFromUri(Uri imageUrl)
        {
            var dataUri = Uri.UnescapeDataString(imageUrl.OriginalString);
            int svgPosition = dataUri.IndexOf("<svg", StringComparison.Ordinal);
            return svgPosition >= 0 ? dataUri.Substring(svgPosition) : string.Empty;
        }

        public static Size ParseSizeOfSVGImageFromXmlString(string content)
        {
            try
            {
                var xmlDoc = new XmlDocument { XmlResolver = null };
                using (var reader = XmlReader.Create(
                    new StringReader(content ?? string.Empty),
                    new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }))
                {
                    xmlDoc.Load(reader);
                }

                var rootElement = xmlDoc.DocumentElement;
                if (rootElement != null && rootElement.LocalName == "svg")
                {
                    var heightAttribute = rootElement.GetAttribute("height");
                    var widthAttribute = rootElement.GetAttribute("width");
                    double height = string.IsNullOrEmpty(heightAttribute) ? 0.0 : TryHStringToDouble(heightAttribute) ?? 0.0;
                    double width = string.IsNullOrEmpty(widthAttribute) ? 0.0 : TryHStringToDouble(widthAttribute) ?? 0.0;
                    return new Size((float)width, (float)height);
                }
            }
            catch (XmlException)
            {
            }

            return new Size();
        }

        public static async Task<Size> ParseSizeOfSVGImageFromStreamAsync(IRandomAccessStream stream)
        {
            using var inputStream = stream.GetInputStreamAt(0).AsStreamForRead();
            using var reader = new StreamReader(inputStream, Encoding.UTF8, true, 4096, leaveOpen: true);
            var svgString = await reader.ReadToEndAsync().ConfigureAwait(false);
            return ParseSizeOfSVGImageFromXmlString(svgString);
        }

        public static bool IsSvgImage(Uri imageUrl)
        {
            if (imageUrl == null)
            {
                return false;
            }

            string imagePath = imageUrl.IsAbsoluteUri ? imageUrl.AbsolutePath : imageUrl.OriginalString;
            if (imageUrl.IsAbsoluteUri && imageUrl.Scheme == "data")
            {
                imagePath = GetDataUriPath(imageUrl);
                int semicolonPos = imagePath.IndexOf(';');
                var mediaType = semicolonPos >= 0 ? imagePath.Substring(0, semicolonPos) : imagePath;
                return mediaType.Contains("svg", StringComparison.Ordinal);
            }

            int dot = imagePath.LastIndexOf('.');
            return imagePath.Substring(dot + 1) == "svg";
        }

        /// <summary>
        /// Equivalent of Windows.Foundation.Uri.Path for a data: URI (everything after "data:"), unescaped.
        /// System.Uri escapes/normalizes data URIs differently, so read the original string instead.
        /// </summary>
        internal static string GetDataUriPath(Uri uri)
        {
            var s = uri?.OriginalString ?? string.Empty;
            if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(5);
            }

            return s;
        }

        /// <summary>Converts a byte array to a random access stream positioned at 0.</summary>
        /// <remarks>
        /// The C++ uses DataWriter + DetachStream; DataWriter.DetachStream is not implemented in Uno, so wrap a MemoryStream instead.
        /// </remarks>
        internal static Task<IRandomAccessStream> BytesToRandomAccessStreamAsync(byte[] bytes)
        {
            IRandomAccessStream stream = new MemoryStream(bytes ?? Array.Empty<byte>()).AsRandomAccessStream();
            stream.Seek(0);
            return Task.FromResult(stream);
        }
    }
}
