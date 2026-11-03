// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveMediaRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var adaptiveMedia = (AdaptiveMedia)cardElement;
                var hostConfig = renderContext.HostConfig;

                var posterImage = MediaHelpers.GetMediaPosterAsImage(renderContext, renderArgs, adaptiveMedia);

                if (!XamlHelpers.SupportsInteractivity(hostConfig))
                {
                    renderContext.AddWarning(
                        WarningStatusCode.InteractivityNotSupported, "Media was present in card, but interactivity is not supported");
                    return posterImage;
                }

                var posterContainer = MediaHelpers.CreatePosterContainerWithPlayButton(posterImage, renderContext, renderArgs);

                var altText = adaptiveMedia.AltText;
                var touchTargetUIElement = ActionHelpers.WrapInTouchTarget(
                    cardElement, posterContainer, null, renderContext, renderArgs, true, "Adaptive.SelectAction", altText, false);

                var mediaStackPanel = new StackPanel();
                XamlHelpers.AppendXamlElementToPanel(touchTargetUIElement, mediaStackPanel);

                bool allowInlinePlayback = hostConfig.Media.AllowInlinePlayback;
                var mediaInvoker = renderContext.MediaEventInvoker;

                string mimeType = string.Empty;
                Uri mediaSourceUrl = null;
                MediaPlayerElement mediaElement = null;

                if (allowInlinePlayback)
                {
                    mediaElement = new MediaPlayerElement();

                    (mediaSourceUrl, mimeType) = MediaHelpers.GetMediaSource(hostConfig, adaptiveMedia);
                    if (mediaSourceUrl == null)
                    {
                        renderContext.AddWarning(WarningStatusCode.UnsupportedMediaType, "Unsupported media element dropped");
                        return null;
                    }

                    mediaElement.AutoPlay = false;
                    mediaElement.AreTransportControlsEnabled = true;

                    if (mediaElement.TransportControls is { } mediaTransportControls)
                    {
                        mediaTransportControls.IsCompact = true;
                        mediaTransportControls.IsZoomButtonVisible = false;
                    }

                    if (posterImage != null)
                    {
                        mediaElement.PosterSource = posterImage.Source;
                    }

                    mediaElement.Visibility = Visibility.Collapsed;
#if HAS_UNO
                    // Host the player with a caption overlay (TimedTextSource is not implemented on Uno, issue #1).
                    XamlHelpers.AppendXamlElementToPanel(WebVttCaptions.CreateHost(mediaElement), mediaStackPanel);
#else
                    XamlHelpers.AppendXamlElementToPanel(mediaElement, mediaStackPanel);
#endif
                }

                if (touchTargetUIElement is ButtonBase touchTargetAsButtonBase)
                {
                    touchTargetAsButtonBase.Click += (sender, args) =>
                    {
                        touchTargetAsButtonBase.IsEnabled = false;
                        MediaHelpers.HandleMediaClick(
                            renderContext, adaptiveMedia, mediaElement, touchTargetAsButtonBase, mediaSourceUrl, mimeType, mediaInvoker);
                    };
                }

                return mediaStackPanel;
            }
            catch (Exception ex)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }
}
