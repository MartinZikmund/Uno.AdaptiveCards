// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Port of MediaHelpers.cpp. WinUI3's MediaElement is MediaPlayerElement.</summary>
    internal static class MediaHelpers
    {
        private const double PlayIconSize = 30;
        private const double PlayIconCornerRadius = 5;
        private const double PlayIconOpacity = .5;

        private static readonly string[] s_supportedMimeTypes = { "video/mp4", "audio/mp4", "audio/aac", "audio/mpeg" };
        private static readonly HashSet<string> s_supportedCaptionTypes = new(StringComparer.Ordinal) { "vtt", "srt" };

        public static Image GetMediaPosterAsImage(AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs, AdaptiveMedia adaptiveMedia)
        {
            var posterString = adaptiveMedia.Poster;
            if (string.IsNullOrEmpty(posterString))
            {
                posterString = renderContext.HostConfig.Media.DefaultPoster;
                if (string.IsNullOrEmpty(posterString))
                {
                    return null;
                }
            }

            var adaptiveImage = new AdaptiveImage
            {
                Url = posterString,
                AltText = adaptiveMedia.AltText,
            };

            var imageRenderer = renderContext.ElementRenderers.Get("Image");
            if (imageRenderer?.Render(adaptiveImage, renderContext, renderArgs) is UIElement posterUiElement)
            {
                // The image renderer may wrap the Image (e.g. background color border); only plain images are usable as posters.
                return posterUiElement as Image;
            }

            return null;
        }

        private static void AddDefaultPlayIcon(Panel posterPanel, AdaptiveHostConfig hostConfig, AdaptiveRenderArgs renderArgs)
        {
            var rectangle = new Rectangle
            {
                Height = PlayIconSize,
                Width = PlayIconSize,
                RadiusX = PlayIconCornerRadius,
                RadiusY = PlayIconCornerRadius,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)),
                Opacity = PlayIconOpacity,
            };

            var darkBrushColor = Util.GetColorFromAdaptiveColor(hostConfig, ForegroundColor.Dark, renderArgs.ContainerStyle, false, false);
            var darkBrush = new SolidColorBrush(darkBrushColor);
            rectangle.Stroke = darkBrush;

            var playIcon = new SymbolIcon(Symbol.Play) { Foreground = darkBrush };

            XamlHelpers.AppendXamlElementToPanel(rectangle, posterPanel);
            RelativePanel.SetAlignVerticalCenterWithPanel(rectangle, true);
            RelativePanel.SetAlignHorizontalCenterWithPanel(rectangle, true);

            XamlHelpers.AppendXamlElementToPanel(playIcon, posterPanel);
            RelativePanel.SetAlignHorizontalCenterWithPanel(playIcon, true);
            RelativePanel.SetAlignVerticalCenterWithPanel(playIcon, true);
        }

        private static void AddCustomPlayIcon(Panel posterPanel, string playIconString, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var playIconAdaptiveImage = new AdaptiveImage { Url = playIconString };
            var imageRenderer = renderContext.ElementRenderers.Get("Image");
            var playIconUIElement = imageRenderer?.Render(playIconAdaptiveImage, renderContext, renderArgs);
            if (playIconUIElement is FrameworkElement playIconAsFrameworkElement)
            {
                playIconAsFrameworkElement.Height = PlayIconSize;
                XamlHelpers.AppendXamlElementToPanel(playIconUIElement, posterPanel);
                RelativePanel.SetAlignHorizontalCenterWithPanel(playIconUIElement, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(playIconUIElement, true);
            }
        }

        private static void AddPlayIcon(Panel posterPanel, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var hostConfig = renderContext.HostConfig;
            var customPlayIconString = hostConfig.Media.PlayButton;
            if (string.IsNullOrEmpty(customPlayIconString))
            {
                AddDefaultPlayIcon(posterPanel, hostConfig, renderArgs);
            }
            else
            {
                AddCustomPlayIcon(posterPanel, customPlayIconString, renderContext, renderArgs);
            }
        }

        public static UIElement CreatePosterContainerWithPlayButton(Image posterImage, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var posterRelativePanel = new RelativePanel();
            if (posterImage != null)
            {
                XamlHelpers.AppendXamlElementToPanel(posterImage, posterRelativePanel);
            }

            AddPlayIcon(posterRelativePanel, renderContext, renderArgs);
            return posterRelativePanel;
        }

        public static (Uri, string) GetMediaSource(AdaptiveHostConfig hostConfig, AdaptiveMedia adaptiveMedia)
        {
            Uri mediaSourceUrl = null;
            string mimeType = string.Empty;

            AdaptiveMediaSource selectedSource = null;
            foreach (var source in adaptiveMedia.Sources)
            {
                var currentMimeType = source.MimeType;
                foreach (var supported in s_supportedMimeTypes)
                {
                    if (currentMimeType == supported)
                    {
                        selectedSource = source;
                        break;
                    }
                }
            }

            if (selectedSource != null)
            {
                mediaSourceUrl = Util.GetUrlFromString(hostConfig, selectedSource.Url);
                mimeType = selectedSource.MimeType;
            }

            return (mediaSourceUrl, mimeType);
        }

        private static void SetMediaSourceHelper(
            MediaPlayerElement mediaElement, AdaptiveMedia adaptiveMedia, AdaptiveRenderContext renderContext, MediaSource mediaSrc)
        {
#if HAS_UNO
            // TimedTextSource/ExternalTimedTextSources are not implemented on Uno (issue #1): captions are rendered by
            // WebVttCaptions as an overlay on the player. Only one caption track is shown (no track picker).
            foreach (var captionSource in adaptiveMedia.CaptionSources)
            {
                if (!s_supportedCaptionTypes.Contains(captionSource.MimeType))
                {
                    continue;
                }

                var captionUrl = Util.GetUrlFromString(renderContext.HostConfig, captionSource.Url);
                if (captionUrl == null)
                {
                    renderContext.AddWarning(
                        WarningStatusCode.AssetLoadFailed,
                        "Specified URI:" + captionSource.Url + " for Media Text Sources is not valid. Text sources loading has failed.");
                    continue;
                }

                WebVttCaptions.Attach(mediaElement, captionSource, captionUrl, renderContext);
                break;
            }

            mediaElement.Source = new MediaPlaybackItem(mediaSrc);
            StartPlayback(mediaElement);
            return;
#else
            foreach (var captionSource in adaptiveMedia.CaptionSources)
            {
                if (!s_supportedCaptionTypes.Contains(captionSource.MimeType))
                {
                    continue;
                }

                var timedTextUrl = Util.GetUrlFromString(renderContext.HostConfig, captionSource.Url);
                if (timedTextUrl == null)
                {
                    renderContext.AddWarning(
                        WarningStatusCode.AssetLoadFailed,
                        "Specified URI:" + captionSource.Url + " for Media Text Sources is not valid. Text sources loading has failed.");
                    continue;
                }

                try
                {
                    var resourceResolver = renderContext.ResourceResolvers?.Get(timedTextUrl.Scheme);
                    var label = captionSource.Label;
                    TimedTextSource timedTextSrc;
                    // Note: TimedTextSource (captions) is not implemented in Uno at the time of writing; when it returns
                    // null a warning is reported and the media plays without captions.
                    if (resourceResolver == null)
                    {
                        timedTextSrc = TimedTextSource.CreateFromUri(timedTextUrl);
                    }
                    else
                    {
                        var stream = resourceResolver.GetResourceStreamAsync(new AdaptiveCardGetResourceStreamArgs(timedTextUrl))
                            .AsTask().GetAwaiter().GetResult();
                        timedTextSrc = TimedTextSource.CreateFromStream(stream);
                    }

                    if (timedTextSrc == null)
                    {
                        renderContext.AddWarning(WarningStatusCode.UnsupportedMediaType, "Media captions are not supported on this platform");
                        continue;
                    }

                    timedTextSrc.Resolved += (sender, args) =>
                    {
                        if (args.Error == null && args.Tracks.Count > 0)
                        {
                            args.Tracks[0].Label = label;
                        }
                    };

                    mediaSrc.ExternalTimedTextSources.Add(timedTextSrc);
                }
                catch (Exception e)
                {
                    // Captions (TimedTextSource) are not implemented on every Uno target.
                    renderContext.AddWarning(WarningStatusCode.AssetLoadFailed, "Caption source could not be loaded: " + e.Message);
                }
            }

            var playbackItem = new MediaPlaybackItem(mediaSrc);
            try
            {
                playbackItem.TimedMetadataTracksChanged += (sender, args) =>
                {
                    if (playbackItem.TimedMetadataTracks.Count > 0)
                    {
                        playbackItem.TimedMetadataTracks.SetPresentationMode(0, TimedMetadataTrackPresentationMode.PlatformPresented);
                    }
                };
            }
            catch (NotImplementedException)
            {
            }

            mediaElement.Source = playbackItem;
            StartPlayback(mediaElement);
#endif
        }

        /// <summary>
        /// The source is only set once the user clicked the poster, so playback should start as soon as the media opens.
        /// (The WinUI3 renderer intends this but never achieves it; AutoPlay is not honored for a source assigned after
        /// the click on Uno either.)
        /// </summary>
        private static void StartPlayback(MediaPlayerElement mediaElement)
        {
            mediaElement.AutoPlay = true;
            if (mediaElement.MediaPlayer is not { } player)
            {
                // Uno creates the element's MediaPlayer when its template is applied, possibly after the source is set,
                // and only copies AutoPlay to it on Loaded: start playback once the player is attached.
                long token = 0;
                token = mediaElement.RegisterPropertyChangedCallback(MediaPlayerElement.MediaPlayerProperty, (sender, dp) =>
                {
                    if (sender is MediaPlayerElement element && element.MediaPlayer is not null)
                    {
                        element.UnregisterPropertyChangedCallback(MediaPlayerElement.MediaPlayerProperty, token);
                        StartPlayback(element);
                    }
                });
                return;
            }

            player.AutoPlay = true;
            player.MediaOpened += (sender, args) => sender.Play();
            try
            {
                player.Play();
            }
            catch (Exception)
            {
                // Media not opened yet; MediaOpened starts playback.
            }
        }

        private static async Task LoadMediaFromResolverAsync(
            IAdaptiveCardResourceResolver resourceResolver,
            Uri mediaSourceUrl,
            MediaPlayerElement mediaElement,
            string mimeType,
            AdaptiveMedia adaptiveMedia,
            AdaptiveRenderContext renderContext)
        {
            try
            {
                var randomAccessStream = await resourceResolver.GetResourceStreamAsync(new AdaptiveCardGetResourceStreamArgs(mediaSourceUrl)).AsTask();
                if (randomAccessStream != null)
                {
                    var mediaSrc = await CreateMediaSourceFromStreamAsync(randomAccessStream, mimeType);
                    SetMediaSourceHelper(mediaElement, adaptiveMedia, renderContext, mediaSrc);
                }
            }
            catch (Exception e)
            {
                renderContext.TryGetRenderResult()?.Warnings.Add(
                    new AdaptiveWarning(WarningStatusCode.AssetLoadFailed, "Media could not be loaded: " + e.Message));
            }
        }

        /// <summary>
        /// MediaSource.CreateFromStream is not implemented in Uno: there the resolved stream is exposed through a data: URI
        /// in the browser and through a temporary file elsewhere.
        /// </summary>
        private static async Task<MediaSource> CreateMediaSourceFromStreamAsync(IRandomAccessStream stream, string mimeType)
        {
            // Resolver streams are often not cloneable (e.g. MemoryStream.AsRandomAccessStream()), which both
            // MediaSource.CreateFromStream and CloneStream require: read the content once and work from a copy.
            stream.Seek(0);
            byte[] bytes;
            using (var memory = new MemoryStream())
            {
                await stream.AsStreamForRead().CopyToAsync(memory);
                bytes = memory.ToArray();
            }

#if !HAS_UNO
            var copy = new InMemoryRandomAccessStream();
            await copy.WriteAsync(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(bytes));
            copy.Seek(0);
            return MediaSource.CreateFromStream(copy, mimeType);
#else
            if (OperatingSystem.IsBrowser())
            {
                return MediaSource.CreateFromUri(new Uri("data:" + mimeType + ";base64," + Convert.ToBase64String(bytes)));
            }

            var extension = mimeType switch
            {
                "video/mp4" => ".mp4",
                "audio/mp4" => ".m4a",
                "audio/aac" => ".aac",
                "audio/mpeg" => ".mp3",
                _ => ".bin",
            };

            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AdaptiveCardsMedia_" + Guid.NewGuid().ToString("N") + extension);
            await File.WriteAllBytesAsync(path, bytes);
            return MediaSource.CreateFromUri(new Uri(path));
#endif
        }

        public static void HandleMediaClick(
            AdaptiveRenderContext renderContext,
            AdaptiveMedia adaptiveMedia,
            MediaPlayerElement mediaElement,
            UIElement posterContainer,
            Uri mediaSourceUrl,
            string mimeType,
            AdaptiveMediaEventInvoker mediaInvoker)
        {
            if (mediaElement != null)
            {
                posterContainer.Visibility = Visibility.Collapsed;
                mediaElement.Visibility = Visibility.Visible;

                var resourceResolver = renderContext.ResourceResolvers?.Get(mediaSourceUrl.Scheme);
                if (resourceResolver == null)
                {
                    var mediaSrc = MediaSource.CreateFromUri(mediaSourceUrl);
                    SetMediaSourceHelper(mediaElement, adaptiveMedia, renderContext, mediaSrc);
                }
                else
                {
                    // The WinUI3 renderer uses IAsyncOperation.Completed + GetResults(). On Uno, Completed is raised on the UI
                    // thread before the underlying task completes, so GetResults() deadlocks the UI thread (issue #10).
                    // Awaiting the operation is equivalent and works everywhere.
                    _ = LoadMediaFromResolverAsync(resourceResolver, mediaSourceUrl, mediaElement, mimeType, adaptiveMedia, renderContext);
                }

                // Deviation: the C++ (WinUI3) casts the MediaPlayer sender to MediaElement, which never succeeds, so the
                // media never auto-plays after the poster is clicked. Here the opened player is started.
                if (mediaElement.MediaPlayer is { } player)
                {
                    player.MediaOpened += (sender, args) => sender.Play();
                }
                else
                {
                    mediaElement.AutoPlay = true;
                }
            }
            else
            {
                mediaInvoker.SendMediaClickedEvent(adaptiveMedia);
            }
        }
    }
}
