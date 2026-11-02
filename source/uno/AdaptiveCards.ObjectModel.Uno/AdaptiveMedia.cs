// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    /// <summary>Port of the shared ContentSource.</summary>
    public abstract class AdaptiveContentSource : IContentSource
    {
        public string MimeType { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        internal virtual JsonObject ToJson()
        {
            var root = new JsonObject();
            if (!string.IsNullOrEmpty(MimeType))
            {
                root["mimeType"] = MimeType;
            }

            if (!string.IsNullOrEmpty(Url))
            {
                root["url"] = Url;
            }

            return root;
        }

        internal void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation) =>
            resourceInformation.Add(new AdaptiveRemoteResourceInformation(Url, MimeType));

        internal void DeserializeContentSource(JsonNode jsonNode)
        {
            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            MimeType = ParseUtil.GetString(json, "mimeType", false);
            Url = ParseUtil.GetString(json, "url", false);
        }
    }

    public sealed class AdaptiveMediaSource : AdaptiveContentSource
    {
        public AdaptiveMediaSource()
        {
        }

        internal static AdaptiveMediaSource Deserialize(ParseContext context, JsonNode json)
        {
            var source = new AdaptiveMediaSource();
            source.DeserializeContentSource(json);
            return source;
        }
    }

    public sealed class AdaptiveCaptionSource : AdaptiveContentSource
    {
        public AdaptiveCaptionSource()
        {
        }

        public string Label { get; set; } = string.Empty;

        internal override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (!string.IsNullOrEmpty(Label))
            {
                root["label"] = Label;
            }

            return root;
        }

        internal static AdaptiveCaptionSource Deserialize(ParseContext context, JsonNode json)
        {
            var source = new AdaptiveCaptionSource();
            source.DeserializeContentSource(json);
            source.Label = ParseUtil.GetString((JsonObject)json, "label", false);
            return source;
        }
    }

    public sealed class AdaptiveMedia : AdaptiveCardElementBase
    {
        public AdaptiveMedia() : base(ElementType.Media, "Media")
        {
        }

        public string Poster { get; set; } = string.Empty;

        public string AltText { get; set; } = string.Empty;

        public IList<AdaptiveMediaSource> Sources { get; } = new List<AdaptiveMediaSource>();

        public IList<AdaptiveCaptionSource> CaptionSources { get; } = new List<AdaptiveCaptionSource>();

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("poster");
            knownProperties.Add("altText");
            knownProperties.Add("sources");
            knownProperties.Add("captionSources");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (!string.IsNullOrEmpty(Poster))
            {
                root["poster"] = Poster;
            }

            if (!string.IsNullOrEmpty(AltText))
            {
                root["altText"] = AltText;
            }

            var sources = new JsonArray();
            foreach (var source in Sources)
            {
                sources.Add((JsonNode)source.ToJson());
            }

            root["sources"] = sources;

            if (CaptionSources.Count > 0)
            {
                var captions = new JsonArray();
                foreach (var caption in CaptionSources)
                {
                    captions.Add((JsonNode)caption.ToJson());
                }

                root["captionSources"] = captions;
            }

            return root;
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            resourceInformation.Add(new AdaptiveRemoteResourceInformation(Poster, "image"));
            foreach (var source in Sources)
            {
                source.GetResourceInformation(resourceInformation);
            }
        }

        internal static AdaptiveMedia Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Media");
            var media = new AdaptiveMedia();
            media.DeserializeBaseProperties(context, json);
            media.Poster = ParseUtil.GetString(json, "poster", false);
            media.AltText = ParseUtil.GetString(json, "altText", false);

            var sources = ParseUtil.GetElementCollectionOfSingleType(context, json, "sources", AdaptiveMediaSource.Deserialize, true);
            string mimeBaseType = string.Empty;
            foreach (var source in sources)
            {
                var currentMimeType = source.MimeType ?? string.Empty;
                var slash = currentMimeType.IndexOf('/');
                var currentBaseType = slash >= 0 ? currentMimeType.Substring(0, slash) : currentMimeType;
                if (string.IsNullOrEmpty(mimeBaseType))
                {
                    mimeBaseType = currentBaseType;
                }
                else if (mimeBaseType != currentBaseType)
                {
                    context.AddWarning(WarningStatusCode.InvalidMediaMix, "Media element containing a mix of audio and video was dropped");
                    return null;
                }
            }

            foreach (var source in sources)
            {
                media.Sources.Add(source);
            }

            foreach (var caption in ParseUtil.GetElementCollectionOfSingleType(context, json, "captionSources", AdaptiveCaptionSource.Deserialize, false))
            {
                media.CaptionSources.Add(caption);
            }

            return media;
        }
    }

    public sealed class AdaptiveMediaParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveMedia.Deserialize(context, json);
    }
}
