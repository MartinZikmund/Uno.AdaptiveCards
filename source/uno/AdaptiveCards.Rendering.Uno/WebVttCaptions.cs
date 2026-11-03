// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>A WebVTT cue.</summary>
    internal readonly record struct CaptionCue(TimeSpan Start, TimeSpan End, string Text);

    /// <summary>
    /// Renders Media.captionSources (WebVTT) on Uno targets, where Windows.Media.Core.TimedTextSource and
    /// MediaSource.ExternalTimedTextSources are not implemented (issue #1). The Windows App SDK head uses the platform
    /// timed text pipeline exactly like the WinUI3 renderer.
    /// </summary>
    internal static class WebVttCaptions
    {
        private static readonly Regex s_timing = new(
            @"^\s*((?:\d+:)?\d{1,2}:\d{2}[.,]\d{3})\s+-->\s+((?:\d+:)?\d{1,2}:\d{2}[.,]\d{3})", RegexOptions.CultureInvariant);

        private static readonly Regex s_tags = new("<[^>]+>", RegexOptions.CultureInvariant);

        private static readonly ConditionalWeakTable<MediaPlayerElement, TextBlock> s_overlays = new();

        /// <summary>Hosts the media element in a grid with a caption overlay and returns the host.</summary>
        public static FrameworkElement CreateHost(MediaPlayerElement mediaElement)
        {
            var overlay = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(8, 0, 8, 12),
                Padding = new Thickness(6, 2, 6, 2),
                Foreground = new SolidColorBrush(Colors.White),
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed,
            };
            var background = new Border
            {
                Child = overlay,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xB0, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = overlay.Margin,
                IsHitTestVisible = false,
            };
            overlay.Margin = new Thickness(0);
            background.SetBinding(UIElement.VisibilityProperty, new Microsoft.UI.Xaml.Data.Binding { Source = overlay, Path = new PropertyPath("Visibility") });
            s_overlays.AddOrUpdate(mediaElement, overlay);

            var host = new Grid();
            host.Children.Add(mediaElement);
            host.Children.Add(background);
            return host;
        }

        public static IReadOnlyList<CaptionCue> Parse(string vtt)
        {
            var cues = new List<CaptionCue>();
            if (string.IsNullOrEmpty(vtt))
            {
                return cues;
            }

            var lines = vtt.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var match = s_timing.Match(lines[i]);
                if (!match.Success)
                {
                    continue;
                }

                var text = new List<string>();
                int j = i + 1;
                for (; j < lines.Length && lines[j].Trim().Length > 0; j++)
                {
                    text.Add(s_tags.Replace(lines[j], string.Empty).Trim());
                }

                cues.Add(new CaptionCue(ParseTime(match.Groups[1].Value), ParseTime(match.Groups[2].Value), string.Join("\n", text)));
                i = j;
            }

            return cues;
        }

        private static TimeSpan ParseTime(string value)
        {
            var parts = value.Replace(',', '.').Split(':');
            double seconds = double.Parse(parts[^1], CultureInfo.InvariantCulture);
            double minutes = double.Parse(parts[^2], CultureInfo.InvariantCulture);
            double hours = parts.Length > 2 ? double.Parse(parts[^3], CultureInfo.InvariantCulture) : 0;
            return TimeSpan.FromSeconds(hours * 3600 + minutes * 60 + seconds);
        }

        public static string GetCueText(IReadOnlyList<CaptionCue> cues, TimeSpan position) =>
            string.Join("\n", cues.Where(c => position >= c.Start && position < c.End).Select(c => c.Text));

        /// <summary>Loads the first supported caption source and keeps the overlay in sync with playback.</summary>
        public static async void Attach(
            MediaPlayerElement mediaElement, AdaptiveCaptionSource captionSource, Uri captionUrl, AdaptiveRenderContext renderContext)
        {
            if (!s_overlays.TryGetValue(mediaElement, out var overlay))
            {
                return;
            }

            try
            {
                string vtt;
                var resolver = renderContext.ResourceResolvers?.Get(captionUrl.Scheme);
                if (resolver != null)
                {
                    var stream = await resolver.GetResourceStreamAsync(new AdaptiveCardGetResourceStreamArgs(captionUrl));
                    using var reader = new StreamReader(stream.AsStreamForRead());
                    vtt = await reader.ReadToEndAsync();
                }
                else if (captionUrl.Scheme == "data")
                {
                    vtt = DecodeDataUri(captionUrl.OriginalString);
                }
                else
                {
                    using var client = new HttpClient();
                    vtt = await client.GetStringAsync(captionUrl);
                }

                var cues = Parse(vtt);
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                var weakElement = new WeakReference<MediaPlayerElement>(mediaElement);
                timer.Tick += (s, e) =>
                {
                    if (!weakElement.TryGetTarget(out var element))
                    {
                        timer.Stop();
                        return;
                    }

                    var position = element.MediaPlayer?.PlaybackSession?.Position ?? TimeSpan.Zero;
                    var text = GetCueText(cues, position);
                    overlay.Text = text;
                    overlay.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
                };
                mediaElement.Unloaded += (s, e) => timer.Stop();
                timer.Start();
            }
            catch (Exception e)
            {
                renderContext.TryGetRenderResult()?.Warnings.Add(
                    new AdaptiveWarning(WarningStatusCode.AssetLoadFailed, "Caption source could not be loaded: " + e.Message));
            }
        }

        private static string DecodeDataUri(string uri)
        {
            var comma = uri.IndexOf(',');
            var header = uri.Substring(0, comma);
            var data = uri.Substring(comma + 1);
            return header.Contains(";base64", StringComparison.OrdinalIgnoreCase)
                ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data))
                : Uri.UnescapeDataString(data);
        }
    }
}
