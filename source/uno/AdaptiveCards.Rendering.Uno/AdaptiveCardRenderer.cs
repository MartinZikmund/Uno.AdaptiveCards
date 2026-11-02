// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Renders Adaptive Cards to Uno Platform (WinUI) UI. Port of AdaptiveCards.Rendering.WinUI3.AdaptiveCardRenderer.
    /// </summary>
    public sealed class AdaptiveCardRenderer
    {
        private readonly XamlBuilder _xamlBuilder;
        private ResourceDictionary _overrideDictionary;
        private AdaptiveHostConfig _hostConfig;
        private AdaptiveFeatureRegistration _featureRegistration;
        private ResourceDictionary _defaultResourceDictionary;
        private ResourceDictionary _mergedResourceDictionary;
        private ResourceDictionary _actionSentimentResourceDictionary;
        private bool _explicitDimensions;
        private uint _desiredWidth;
        private uint _desiredHeight;

        public AdaptiveCardRenderer()
        {
            ElementRenderers = new AdaptiveElementRendererRegistration();
            ActionRenderers = new AdaptiveActionRendererRegistration();
            _featureRegistration = new AdaptiveFeatureRegistration();
            _hostConfig = new AdaptiveHostConfig();
            ResourceResolvers = new AdaptiveCardResourceResolvers();
            _xamlBuilder = new XamlBuilder();

            Util.RegisterDefaultElementRenderers(ElementRenderers, _xamlBuilder);
            Util.RegisterDefaultActionRenderers(ActionRenderers);
            InitializeDefaultResourceDictionary();
            UpdateActionSentimentResourceDictionary();
        }

        public AdaptiveCardResourceResolvers ResourceResolvers { get; }

        public AdaptiveHostConfig HostConfig
        {
            get => _hostConfig;
            set
            {
                _hostConfig = value ?? new AdaptiveHostConfig();
                UpdateActionSentimentResourceDictionary();
            }
        }

        public ResourceDictionary OverrideStyles
        {
            get => _overrideDictionary;
            set
            {
                _overrideDictionary = value;
                SetMergedDictionary();
            }
        }

        public AdaptiveFeatureRegistration FeatureRegistration
        {
            get => _featureRegistration ??= new AdaptiveFeatureRegistration();
            set => _featureRegistration = value;
        }

        public bool OverflowMaxActions
        {
            get => _hostConfig.OverflowMaxActions;
            set => _hostConfig.OverflowMaxActions = value;
        }

        public string OverflowButtonText
        {
            get => _hostConfig.OverflowButtonText;
            set => _hostConfig.OverflowButtonText = value;
        }

        public string OverflowButtonAccessibilityText
        {
            get => _hostConfig.OverflowButtonAccessibilityText;
            set => _hostConfig.OverflowButtonAccessibilityText = value;
        }

        public bool AddSelectActionMargin
        {
            get => _hostConfig.AddSelectActionMargin;
            set => _hostConfig.AddSelectActionMargin = value;
        }

        /// <summary>
        /// Source of dynamic choices for filtered <c>Input.ChoiceSet</c>s using <c>choices.data</c> (Data.Query).
        /// Optional; without it only the static choices are offered (as in the WinUI3 renderer).
        /// </summary>
        public IAdaptiveChoicesProvider ChoicesProvider { get; set; }

        public AdaptiveElementRendererRegistration ElementRenderers { get; }

        public AdaptiveActionRendererRegistration ActionRenderers { get; }

        public void SetFixedDimensions(uint desiredWidth, uint desiredHeight)
        {
            _explicitDimensions = true;
            _desiredWidth = desiredWidth;
            _desiredHeight = desiredHeight;
        }

        public void ResetFixedDimensions() => _explicitDimensions = false;

        public RenderedAdaptiveCard RenderAdaptiveCard(AdaptiveCard adaptiveCard)
        {
            var renderedCard = new RenderedAdaptiveCard
            {
                OriginatingCard = adaptiveCard,
                OriginatingHostConfig = _hostConfig,
            };

            if (adaptiveCard != null)
            {
                if (_explicitDimensions)
                {
                    _xamlBuilder.SetFixedDimensions(_desiredWidth, _desiredHeight);
                }

                var renderContext = new AdaptiveRenderContext(
                    _hostConfig,
                    _featureRegistration,
                    ElementRenderers,
                    ActionRenderers,
                    ResourceResolvers,
                    _mergedResourceDictionary,
                    _actionSentimentResourceDictionary,
                    renderedCard)
                {
                    XamlBuilder = _xamlBuilder,
                    ChoicesProvider = ChoicesProvider,
                };

                _xamlBuilder.SetEnableXamlImageHandling(true);
                try
                {
                    renderContext.LinkCardToParent(adaptiveCard, null);
                    renderedCard.FrameworkElement = XamlBuilder.BuildXamlTreeFromAdaptiveCard(adaptiveCard, renderContext, _xamlBuilder);
                }
                catch (Exception e)
                {
                    renderContext.AddError(
                        ErrorStatusCode.RenderFailed, "An unrecoverable error was encountered while rendering the card: " + e.Message);
                    renderedCard.FrameworkElement = null;
                }
            }

            return renderedCard;
        }

        public RenderedAdaptiveCard RenderAdaptiveCardFromJsonString(string adaptiveJson)
        {
            var parseResult = AdaptiveCard.FromJsonString(adaptiveJson);
            if (parseResult.AdaptiveCard != null)
            {
                var rendered = RenderAdaptiveCard(parseResult.AdaptiveCard);
                foreach (var warning in parseResult.Warnings)
                {
                    rendered.Warnings.Add(warning);
                }

                return rendered;
            }

            var renderedCard = new RenderedAdaptiveCard();
            foreach (var error in parseResult.Errors)
            {
                renderedCard.Errors.Add(error);
            }

            return renderedCard;
        }

        public RenderedAdaptiveCard RenderAdaptiveCardFromJson(JsonObject adaptiveJson) =>
            RenderAdaptiveCardFromJsonString(adaptiveJson?.ToJsonString() ?? string.Empty);

        internal bool GetFixedDimensions(out uint width, out uint height)
        {
            width = _explicitDimensions ? _desiredWidth : 0;
            height = _explicitDimensions ? _desiredHeight : 0;
            return _explicitDimensions;
        }

        private void InitializeDefaultResourceDictionary()
        {
            _defaultResourceDictionary = new Themes.DefaultResources();
            _mergedResourceDictionary = _defaultResourceDictionary;
            _actionSentimentResourceDictionary = new Themes.DefaultActionSentimentResources();
        }

        private void UpdateActionSentimentResourceDictionary()
        {
            var accentColor = Util.GetColorFromAdaptiveColor(_hostConfig, ForegroundColor.Accent, ContainerStyle.Default, false, false);
            var attentionColor = Util.GetColorFromAdaptiveColor(_hostConfig, ForegroundColor.Attention, ContainerStyle.Default, false, false);
            var hoverAccentColor = Util.GenerateLHoverColor(accentColor);
            var hoverAttentionColor = Util.GenerateLHoverColor(attentionColor);

            _actionSentimentResourceDictionary["Adaptive.Action.Positive.Button.Static.Background"] = new SolidColorBrush(accentColor);
            _actionSentimentResourceDictionary["Adaptive.Action.Positive.Button.MouseOver.Background"] = new SolidColorBrush(hoverAccentColor);
            _actionSentimentResourceDictionary["Adaptive.Action.Destructive.Button.Foreground"] = new SolidColorBrush(attentionColor);
            _actionSentimentResourceDictionary["Adaptive.Action.Destructive.Button.MouseOver.Foreground"] = new SolidColorBrush(hoverAttentionColor);
        }

        private void SetMergedDictionary()
        {
            if (_overrideDictionary != null)
            {
                _mergedResourceDictionary = _overrideDictionary;
                if (!_mergedResourceDictionary.MergedDictionaries.Contains(_defaultResourceDictionary))
                {
                    _mergedResourceDictionary.MergedDictionaries.Add(_defaultResourceDictionary);
                }
            }
            else
            {
                _mergedResourceDictionary = _defaultResourceDictionary;
            }
        }
    }
}
