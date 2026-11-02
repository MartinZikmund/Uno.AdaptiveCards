// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace AdaptiveCards.Rendering.Uno
{
    public interface IAdaptiveElementRenderer
    {
        UIElement Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs);
    }

    public interface IAdaptiveActionRenderer
    {
        UIElement Render(IAdaptiveActionElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs);
    }

    public sealed class AdaptiveElementRendererRegistration
    {
        private readonly Dictionary<string, IAdaptiveElementRenderer> _registration = new(StringComparer.Ordinal);

        public void Set(string type, IAdaptiveElementRenderer renderer) => _registration[type] = renderer;

        public IAdaptiveElementRenderer Get(string type) =>
            type != null && _registration.TryGetValue(type, out var renderer) ? renderer : null;

        public void Remove(string type) => _registration.Remove(type);
    }

    public sealed class AdaptiveActionRendererRegistration
    {
        private readonly Dictionary<string, IAdaptiveActionRenderer> _registration = new(StringComparer.Ordinal);

        public void Set(string type, IAdaptiveActionRenderer renderer) => _registration[type] = renderer;

        public IAdaptiveActionRenderer Get(string type) =>
            type != null && _registration.TryGetValue(type, out var renderer) ? renderer : null;

        public void Remove(string type) => _registration.Remove(type);
    }

    /// <summary>Port of the shared FeatureRegistration: features (and versions) supported by the host.</summary>
    public sealed class AdaptiveFeatureRegistration
    {
        internal const string AdaptiveCardsFeature = "adaptiveCards";
        internal const string SharedModelVersion = "1.6";

        private readonly Dictionary<string, string> _supportedFeatures = new(StringComparer.Ordinal)
        {
            [AdaptiveCardsFeature] = SharedModelVersion,
        };

        public void Set(string name, string version)
        {
            if (version != "*")
            {
                _ = new SemanticVersion(version);
            }

            if (!_supportedFeatures.TryGetValue(name, out var existing))
            {
                _supportedFeatures[name] = version;
            }
            else if (existing != version)
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.InvalidPropertyValue, "Attempting to add a feature with a differing version");
            }
        }

        public string Get(string name) => name != null && _supportedFeatures.TryGetValue(name, out var v) ? v : string.Empty;

        public void Remove(string name)
        {
            if (name == AdaptiveCardsFeature)
            {
                throw new AdaptiveCardParseException(
                    ErrorStatusCode.UnsupportedParserOverride, "Removing the Adaptive Cards feature is unsupported");
            }

            _supportedFeatures.Remove(name);
        }
    }

    public interface IAdaptiveCardResourceResolver
    {
        IAsyncOperation<IRandomAccessStream> GetResourceStreamAsync(AdaptiveCardGetResourceStreamArgs args);
    }

    public sealed class AdaptiveCardGetResourceStreamArgs
    {
        public AdaptiveCardGetResourceStreamArgs()
        {
        }

        public AdaptiveCardGetResourceStreamArgs(Uri uri)
        {
            Url = uri;
        }

        public Uri Url { get; }
    }

    public sealed class AdaptiveCardResourceResolvers
    {
        private readonly Dictionary<string, IAdaptiveCardResourceResolver> _resolvers = new(StringComparer.Ordinal);

        public void Set(string scheme, IAdaptiveCardResourceResolver resolver) => _resolvers[scheme] = resolver;

        public IAdaptiveCardResourceResolver Get(string scheme) =>
            scheme != null && _resolvers.TryGetValue(scheme, out var resolver) ? resolver : null;
    }
}
