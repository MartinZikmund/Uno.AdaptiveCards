// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>WebVTT cue settings (https://www.w3.org/TR/webvtt1/#webvtt-cue-settings).</summary>
    internal readonly record struct CaptionCueSettings(
        double? LinePercent, int? LineNumber, double? PositionPercent, double SizePercent, string Align)
    {
        public static CaptionCueSettings Default { get; } = new(null, null, null, 100, "center");

        public bool IsDefaultPlacement => LinePercent is null && LineNumber is null && PositionPercent is null && SizePercent >= 100;
    }

    /// <summary>A WebVTT/SRT cue. <see cref="Markup"/> keeps the cue text with its inline tags.</summary>
    internal sealed record CaptionCue(TimeSpan Start, TimeSpan End, string Markup, CaptionCueSettings Settings)
    {
        public string Text => WebVttCaptions.StripMarkup(Markup);
    }

    /// <summary>A labelled caption track (one per Media.captionSources entry).</summary>
    internal sealed class CaptionTrack
    {
        public CaptionTrack(string label) => Label = label;

        public string Label { get; }

        public IReadOnlyList<CaptionCue> Cues { get; set; } = Array.Empty<CaptionCue>();
    }

    /// <summary>
    /// Renders Media.captionSources on Uno targets, where Windows.Media.Core.TimedTextSource,
    /// MediaSource.ExternalTimedTextSources and platform-presented timed text are not implemented (issue #1).
    /// Behaves like the WinUI3 renderer with the platform pipeline: every caption source becomes a labelled track, the
    /// first one is presented by default, a closed-captions button lets the user pick another track or turn captions
    /// off, cue markup (b/i/u/c/v/lang/ruby, entities) is styled and the WebVTT cue settings (line, position, size, align)
    /// are honored. The Windows App SDK head uses the platform timed text pipeline exactly like the WinUI3 renderer.
    /// </summary>
    internal static class WebVttCaptions
    {
        public const string CaptionsButtonName = "Closed captions";
        public const string CaptionsOffLabel = "Off";
        public const string CueAutomationId = "AdaptiveMediaCaptionCue";

        private static readonly Regex s_timing = new(
            @"^\s*((?:\d+:)?\d{1,2}:\d{2}[.,]\d{3})\s+-->\s+((?:\d+:)?\d{1,2}:\d{2}[.,]\d{3})(.*)$", RegexOptions.CultureInvariant);

        private static readonly Regex s_tags = new("<[^>]*>", RegexOptions.CultureInvariant);

        private static readonly ConditionalWeakTable<MediaPlayerElement, CaptionHost> s_hosts = new();

        private sealed class CaptionHost
        {
            public required Grid Root { get; init; }

            public required Grid CueLayer { get; init; }

            public required StackPanel BottomCues { get; init; }

            public required Button CaptionsButton { get; init; }

            public List<CaptionTrack> Tracks { get; } = new();

            public int SelectedTrack { get; set; }

            public DispatcherTimer? Timer { get; set; }

            public string LastRendered { get; set; } = string.Empty;
        }

        /// <summary>Hosts the media element in a grid with a caption layer and a closed-captions button.</summary>
        public static FrameworkElement CreateHost(MediaPlayerElement mediaElement)
        {
            var bottomCues = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(8, 0, 8, 12),
                IsHitTestVisible = false,
            };
            var cueLayer = new Grid { IsHitTestVisible = false };
            cueLayer.Children.Add(bottomCues);

            var captionsButton = new Button
            {
                Content = "CC",
                FontSize = 11,
                Padding = new Thickness(6, 2, 6, 2),
                MinWidth = 0,
                MinHeight = 0,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 6, 6, 0),
                Foreground = new SolidColorBrush(Colors.White),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xB0, 0, 0, 0)),
                Visibility = Visibility.Collapsed,
            };
            AutomationProperties.SetName(captionsButton, CaptionsButtonName);
            ToolTipService.SetToolTip(captionsButton, CaptionsButtonName);
            captionsButton.Flyout = new MenuFlyout();

            var overlay = new Grid();
            overlay.Children.Add(cueLayer);
            overlay.Children.Add(captionsButton);

            // The overlay follows the player's visibility (the player is collapsed behind the poster until clicked).
            overlay.Visibility = mediaElement.Visibility;
            mediaElement.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (s, dp) => overlay.Visibility = ((UIElement)s).Visibility);

            var root = new Grid();
            root.Children.Add(mediaElement);
            root.Children.Add(overlay);
            var host = new CaptionHost { Root = root, CueLayer = cueLayer, BottomCues = bottomCues, CaptionsButton = captionsButton };
            root.SizeChanged += (s, e) => { host.LastRendered = string.Empty; };
            s_hosts.AddOrUpdate(mediaElement, host);
            return root;
        }

        // ---------- parsing ----------

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
                    text.Add(lines[j].Trim());
                }

                cues.Add(new CaptionCue(
                    ParseTime(match.Groups[1].Value),
                    ParseTime(match.Groups[2].Value),
                    string.Join("\n", text),
                    ParseSettings(match.Groups[3].Value)));
                i = j;
            }

            return cues;
        }

        internal static CaptionCueSettings ParseSettings(string settings)
        {
            var result = CaptionCueSettings.Default;
            foreach (var token in settings.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var colon = token.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                var name = token.Substring(0, colon);
                var value = token.Substring(colon + 1).Split(',')[0];
                switch (name)
                {
                    case "line":
                        if (TryParsePercent(value, out var linePercent))
                        {
                            result = result with { LinePercent = linePercent, LineNumber = null };
                        }
                        else if (int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var lineNumber))
                        {
                            result = result with { LineNumber = lineNumber, LinePercent = null };
                        }

                        break;
                    case "position":
                        if (TryParsePercent(value, out var position))
                        {
                            result = result with { PositionPercent = position };
                        }

                        break;
                    case "size":
                        if (TryParsePercent(value, out var size))
                        {
                            result = result with { SizePercent = size };
                        }

                        break;
                    case "align":
                        if (value is "start" or "center" or "end" or "left" or "right")
                        {
                            result = result with { Align = value };
                        }

                        break;
                }
            }

            return result;
        }

        private static bool TryParsePercent(string value, out double percent)
        {
            percent = 0;
            return value.EndsWith('%')
                && double.TryParse(value.AsSpan(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out percent)
                && percent >= 0 && percent <= 100;
        }

        private static TimeSpan ParseTime(string value)
        {
            var parts = value.Replace(',', '.').Split(':');
            double seconds = double.Parse(parts[^1], CultureInfo.InvariantCulture);
            double minutes = double.Parse(parts[^2], CultureInfo.InvariantCulture);
            double hours = parts.Length > 2 ? double.Parse(parts[^3], CultureInfo.InvariantCulture) : 0;
            return TimeSpan.FromSeconds(hours * 3600 + minutes * 60 + seconds);
        }

        internal static string StripMarkup(string markup) => DecodeEntities(s_tags.Replace(markup, string.Empty));

        private static string DecodeEntities(string text) =>
            text.Contains('&') ? WebUtility.HtmlDecode(text).Replace(' ', ' ') : text;

        public static IEnumerable<CaptionCue> GetActiveCues(IReadOnlyList<CaptionCue> cues, TimeSpan position) =>
            cues.Where(c => position >= c.Start && position < c.End);

        public static string GetCueText(IReadOnlyList<CaptionCue> cues, TimeSpan position) =>
            string.Join("\n", GetActiveCues(cues, position).Select(c => c.Text));

        // ---------- styled cue rendering ----------

        /// <summary>Builds the inlines for a cue's markup: b/i/u/c/v/lang/ruby tags, timestamps and entities.</summary>
        internal static void AppendInlines(InlineCollection inlines, string markup)
        {
            int bold = 0, italic = 0, underline = 0;
            int index = 0;
            while (index < markup.Length)
            {
                int tagStart = markup.IndexOf('<', index);
                var textEnd = tagStart < 0 ? markup.Length : tagStart;
                if (textEnd > index)
                {
                    AppendText(inlines, DecodeEntities(markup.Substring(index, textEnd - index)), bold > 0, italic > 0, underline > 0);
                }

                if (tagStart < 0)
                {
                    break;
                }

                int tagEnd = markup.IndexOf('>', tagStart);
                if (tagEnd < 0)
                {
                    AppendText(inlines, DecodeEntities(markup.Substring(tagStart)), bold > 0, italic > 0, underline > 0);
                    break;
                }

                var tag = markup.Substring(tagStart + 1, tagEnd - tagStart - 1).Trim();
                bool closing = tag.StartsWith('/');
                var name = (closing ? tag.Substring(1) : tag).Split(new[] { ' ', '.', '\t' }, 2)[0].ToLowerInvariant();
                int delta = closing ? -1 : 1;
                switch (name)
                {
                    case "b":
                        bold = Math.Max(0, bold + delta);
                        break;
                    case "i":
                        italic = Math.Max(0, italic + delta);
                        break;
                    case "u":
                        underline = Math.Max(0, underline + delta);
                        break;

                    // c (classes), v (voice), lang, ruby/rt and timestamps carry no styling without a style sheet.
                }

                index = tagEnd + 1;
            }
        }

        private static void AppendText(InlineCollection inlines, string text, bool bold, bool italic, bool underline)
        {
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    inlines.Add(new LineBreak());
                }

                if (lines[i].Length == 0)
                {
                    continue;
                }

                var run = new Run { Text = lines[i] };
                if (bold)
                {
                    run.FontWeight = FontWeights.Bold;
                }

                if (italic)
                {
                    run.FontStyle = FontStyle.Italic;
                }

                if (underline)
                {
                    run.TextDecorations = TextDecorations.Underline;
                }

                inlines.Add(run);
            }
        }

        private static Border CreateCueElement(CaptionCue cue, double fontSize)
        {
            var textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = fontSize,
                Foreground = new SolidColorBrush(Colors.White),
                TextAlignment = cue.Settings.Align switch
                {
                    "start" or "left" => TextAlignment.Left,
                    "end" or "right" => TextAlignment.Right,
                    _ => TextAlignment.Center,
                },
                IsHitTestVisible = false,
            };
            AutomationProperties.SetAutomationId(textBlock, CueAutomationId);
            AutomationProperties.SetName(textBlock, cue.Text);
            if (s_tags.IsMatch(cue.Markup))
            {
                AppendInlines(textBlock.Inlines, cue.Markup);
            }
            else
            {
                textBlock.Text = DecodeEntities(cue.Markup);
            }

            return new Border
            {
                Child = textBlock,
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 2, 0, 0),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xB0, 0, 0, 0)),
                HorizontalAlignment = textBlock.TextAlignment switch
                {
                    TextAlignment.Left => HorizontalAlignment.Left,
                    TextAlignment.Right => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Center,
                },
                IsHitTestVisible = false,
            };
        }

        /// <summary>Positions a cue that has explicit WebVTT settings inside a <paramref name="width"/> x <paramref name="height"/> video area.</summary>
        internal static void PlaceCue(FrameworkElement element, CaptionCueSettings settings, double width, double height, double lineHeight)
        {
            // Horizontal: the cue box is size% wide; position% is its anchor according to align (WebVTT defaults).
            var size = Math.Clamp(settings.SizePercent, 0, 100);
            var position = settings.PositionPercent ?? settings.Align switch
            {
                "start" or "left" => 0,
                "end" or "right" => 100,
                _ => 50,
            };
            var left = settings.Align switch
            {
                "start" or "left" => position,
                "end" or "right" => position - size,
                _ => position - size / 2,
            };
            left = Math.Clamp(left, 0, 100 - size);
            element.HorizontalAlignment = HorizontalAlignment.Left;
            element.Width = Math.Max(0, width * size / 100);

            // Vertical: line% places the top of the box; a line number counts lines from the top (>= 0) or bottom (< 0).
            double top = 0, bottom = 0;
            var vertical = VerticalAlignment.Bottom;
            if (settings.LinePercent is { } linePercent)
            {
                vertical = VerticalAlignment.Top;
                top = height * linePercent / 100;
            }
            else if (settings.LineNumber is { } lineNumber)
            {
                if (lineNumber >= 0)
                {
                    vertical = VerticalAlignment.Top;
                    top = lineNumber * lineHeight;
                }
                else
                {
                    bottom = (-lineNumber - 1) * lineHeight;
                }
            }
            else
            {
                bottom = 12;
            }

            element.VerticalAlignment = vertical;
            element.Margin = new Thickness(width * left / 100, top, 0, bottom);
        }

        private static void Render(CaptionHost host, TimeSpan position)
        {
            var track = host.SelectedTrack >= 0 && host.SelectedTrack < host.Tracks.Count ? host.Tracks[host.SelectedTrack] : null;
            var active = track is null ? new List<CaptionCue>() : GetActiveCues(track.Cues, position).ToList();
            var width = host.Root.ActualWidth;
            var height = host.Root.ActualHeight;
            var key = string.Join("|", active.Select(c => c.GetHashCode())) + "@" + width + "x" + height;
            if (key == host.LastRendered)
            {
                return;
            }

            host.LastRendered = key;
            host.BottomCues.Children.Clear();
            for (int i = host.CueLayer.Children.Count - 1; i >= 0; i--)
            {
                if (host.CueLayer.Children[i] != host.BottomCues)
                {
                    host.CueLayer.Children.RemoveAt(i);
                }
            }

            // Caption text scales with the video like platform-presented captions (about 1/18 of the video height).
            var fontSize = height > 0 ? Math.Clamp(height / 18, 12, 32) : 14;
            foreach (var cue in active)
            {
                var element = CreateCueElement(cue, fontSize);
                if (cue.Settings.IsDefaultPlacement)
                {
                    host.BottomCues.Children.Add(element);
                }
                else
                {
                    PlaceCue(element, cue.Settings, width, height, fontSize * 1.4);
                    host.CueLayer.Children.Add(element);
                }
            }
        }

        // ---------- track selection ----------

        private static void UpdateCaptionsMenu(CaptionHost host)
        {
            var flyout = (MenuFlyout)host.CaptionsButton.Flyout;
            flyout.Items.Clear();
            for (int i = -1; i < host.Tracks.Count; i++)
            {
                var index = i;
                var item = new ToggleMenuFlyoutItem
                {
                    Text = index < 0 ? CaptionsOffLabel : host.Tracks[index].Label,
                    IsChecked = index == host.SelectedTrack,
                };
                item.RegisterPropertyChangedCallback(ToggleMenuFlyoutItem.IsCheckedProperty, (s, dp) =>
                {
                    var toggle = (ToggleMenuFlyoutItem)s;
                    if (toggle.IsChecked && host.SelectedTrack != index)
                    {
                        SelectTrack(host, index);
                    }
                    else if (!toggle.IsChecked && host.SelectedTrack == index)
                    {
                        // Keep exactly one entry checked (radio behavior).
                        toggle.IsChecked = true;
                    }
                });
                flyout.Items.Add(item);
            }

            host.CaptionsButton.Visibility = host.Tracks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void SelectTrack(CaptionHost host, int index)
        {
            host.SelectedTrack = index;
            host.LastRendered = string.Empty;
            foreach (var item in ((MenuFlyout)host.CaptionsButton.Flyout).Items.OfType<ToggleMenuFlyoutItem>())
            {
                var itemIndex = item.Text == CaptionsOffLabel ? -1 : host.Tracks.FindIndex(t => t.Label == item.Text);
                item.IsChecked = itemIndex == index;
            }
        }

        // ---------- loading ----------

        /// <summary>
        /// Loads all caption sources as labelled tracks (the first is presented, like WinUI's PlatformPresented track 0)
        /// and keeps the caption layer in sync with playback.
        /// </summary>
        public static void Attach(
            MediaPlayerElement mediaElement,
            IReadOnlyList<(AdaptiveCaptionSource Source, Uri Url)> captionSources,
            AdaptiveRenderContext renderContext)
        {
            if (!s_hosts.TryGetValue(mediaElement, out var host) || captionSources.Count == 0)
            {
                return;
            }

            host.Tracks.Clear();
            host.SelectedTrack = 0;
            var tracks = captionSources
                .Select((c, i) => new CaptionTrack(string.IsNullOrEmpty(c.Source.Label) ? "Track " + (i + 1) : c.Source.Label))
                .ToList();
            host.Tracks.AddRange(tracks);
            UpdateCaptionsMenu(host);

            for (int i = 0; i < captionSources.Count; i++)
            {
                _ = LoadTrackAsync(tracks[i], captionSources[i].Url, renderContext, host);
            }

            if (host.Timer is null)
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                var weakElement = new WeakReference<MediaPlayerElement>(mediaElement);
                timer.Tick += (s, e) =>
                {
                    if (!weakElement.TryGetTarget(out var element))
                    {
                        timer.Stop();
                        return;
                    }

                    Render(host, element.MediaPlayer?.PlaybackSession?.Position ?? TimeSpan.Zero);
                };
                mediaElement.Unloaded += (s, e) => timer.Stop();
                mediaElement.Loaded += (s, e) => timer.Start();
                host.Timer = timer;
                timer.Start();
            }
        }

        private static async Task LoadTrackAsync(CaptionTrack track, Uri captionUrl, AdaptiveRenderContext renderContext, CaptionHost host)
        {
            try
            {
                string vtt;
                var resolver = renderContext.ResourceResolvers?.Get(captionUrl.Scheme);
                if (resolver != null)
                {
                    var stream = await resolver.GetResourceStreamAsync(new AdaptiveCardGetResourceStreamArgs(captionUrl)).AsTask();
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

                track.Cues = Parse(vtt);
                host.LastRendered = string.Empty;
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
                ? Encoding.UTF8.GetString(Convert.FromBase64String(data))
                : Uri.UnescapeDataString(data);
        }
    }
}
