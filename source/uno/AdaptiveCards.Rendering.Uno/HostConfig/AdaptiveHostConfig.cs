// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using static AdaptiveCards.Rendering.Uno.HostConfigParsing;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveHostConfig
    {
        public AdaptiveHostConfig()
        {
        }

        /// <summary>Deprecated: use FontTypes.Default.FontFamily.</summary>
        public string FontFamily { get; set; } = string.Empty;

        /// <summary>Deprecated: use FontTypes.Default.FontSizes.</summary>
        public AdaptiveFontSizesConfig FontSizes { get; set; } = new AdaptiveFontSizesConfig();

        /// <summary>Deprecated: use FontTypes.Default.FontWeights.</summary>
        public AdaptiveFontWeightsConfig FontWeights { get; set; } = new AdaptiveFontWeightsConfig();

        public AdaptiveFontTypesDefinition FontTypes { get; set; } = new AdaptiveFontTypesDefinition();

        public bool SupportsInteractivity { get; set; } = true;

        public string ImageBaseUrl { get; set; } = string.Empty;

        public AdaptiveContainerStylesDefinition ContainerStyles { get; set; } = new AdaptiveContainerStylesDefinition();

        public AdaptiveImageSizesConfig ImageSizes { get; set; } = new AdaptiveImageSizesConfig();

        public AdaptiveSeparatorConfig Separator { get; set; } = new AdaptiveSeparatorConfig();

        public AdaptiveSpacingConfig Spacing { get; set; } = new AdaptiveSpacingConfig();

        public AdaptiveCardConfig AdaptiveCard { get; set; } = new AdaptiveCardConfig();

        public AdaptiveImageSetConfig ImageSet { get; set; } = new AdaptiveImageSetConfig();

        public AdaptiveImageConfig Image { get; set; } = new AdaptiveImageConfig();

        public AdaptiveFactSetConfig FactSet { get; set; } = new AdaptiveFactSetConfig();

        public AdaptiveActionsConfig Actions { get; set; } = new AdaptiveActionsConfig();

        public AdaptiveMediaConfig Media { get; set; } = new AdaptiveMediaConfig();

        public AdaptiveInputsConfig Inputs { get; set; } = new AdaptiveInputsConfig();

        public AdaptiveTextBlockConfig TextBlock { get; set; } = new AdaptiveTextBlockConfig();

        public AdaptiveTextStylesConfig TextStyles { get; set; } = new AdaptiveTextStylesConfig();

        public AdaptiveTableConfig Table { get; set; } = new AdaptiveTableConfig();

        // The following are not in the WinUI3 IDL but exist on the WinUI3 implementation (AdaptiveHostConfig.h).

        /// <summary>When true, actions beyond Actions.MaxActions are moved into an overflow ("...") menu.</summary>
        public bool OverflowMaxActions { get; set; }

        public string OverflowButtonText { get; set; } = "...";

        public string OverflowButtonAccessibilityText { get; set; } = "More Actions";

        public bool AddSelectActionMargin { get; set; } = true;

        public static AdaptiveHostConfigParseResult FromJsonString(string hostConfigJson)
        {
            try
            {
                return Parse(ParseUtil.ParseJsonString(hostConfigJson));
            }
            catch (AdaptiveCardParseException e)
            {
                return AdaptiveHostConfigParseResult.FromError(e);
            }
        }

        public static AdaptiveHostConfigParseResult FromJson(JsonObject hostConfigJson)
        {
            try
            {
                return Parse(hostConfigJson);
            }
            catch (AdaptiveCardParseException e)
            {
                return AdaptiveHostConfigParseResult.FromError(e);
            }
        }

        /// <summary>Port of HostConfig::Deserialize from the shared model.</summary>
        private static AdaptiveHostConfigParseResult Parse(JsonNode node)
        {
            var result = new AdaptiveHostConfig();
            if (node is JsonObject json)
            {
                result.Merge(json);
            }
            else if (node != null)
            {
                throw new AdaptiveCardParseException(ErrorStatusCode.InvalidJson, "Expected JSON Object\n");
            }

            return new AdaptiveHostConfigParseResult(result);
        }

        private void Merge(JsonObject json)
        {
            var fontFamily = ParseUtil.TryGetString(json, "fontFamily");
            if (!string.IsNullOrEmpty(fontFamily))
            {
                FontFamily = fontFamily;
            }

            SupportsInteractivity = ParseUtil.GetOptionalBool(json, "supportsInteractivity") ?? SupportsInteractivity;
            ImageBaseUrl = ParseUtil.TryGetString(json, "imageBaseUrl");

            MergeIf(json, "factSet", FactSet.Merge);
            MergeIf(json, "fontSizes", FontSizes.Merge);
            MergeIf(json, "fontWeights", FontWeights.Merge);
            MergeIf(json, "fontTypes", FontTypes.Merge);
            MergeIf(json, "containerStyles", ContainerStyles.Merge);
            MergeIf(json, "image", Image.Merge);
            MergeIf(json, "imageSet", ImageSet.Merge);
            MergeIf(json, "imageSizes", ImageSizes.Merge);
            MergeIf(json, "separator", Separator.Merge);
            MergeIf(json, "spacing", Spacing.Merge);
            MergeIf(json, "adaptiveCard", AdaptiveCard.Merge);
            MergeIf(json, "actions", Actions.Merge);
            MergeIf(json, "media", Media.Merge);
            MergeIf(json, "inputs", Inputs.Merge);
            MergeIf(json, "textBlock", TextBlock.Merge);
            MergeIf(json, "textStyles", TextStyles.Merge);
            MergeIf(json, "table", Table.Merge);
        }

        private static void MergeIf(JsonObject json, string key, Action<JsonObject> merge)
        {
            if (GetObject(json, key) is { } obj)
            {
                merge(obj);
            }
        }
    }

    public sealed class AdaptiveHostConfigParseResult
    {
        public AdaptiveHostConfigParseResult()
        {
        }

        internal AdaptiveHostConfigParseResult(AdaptiveHostConfig hostConfig)
        {
            HostConfig = hostConfig;
        }

        public AdaptiveHostConfig HostConfig { get; internal set; }

        public IList<AdaptiveError> Errors { get; } = new List<AdaptiveError>();

        internal static AdaptiveHostConfigParseResult FromError(AdaptiveCardParseException e)
        {
            var result = new AdaptiveHostConfigParseResult();
            result.Errors.Add(new AdaptiveError(e.StatusCode, e.Message));
            return result;
        }
    }
}
