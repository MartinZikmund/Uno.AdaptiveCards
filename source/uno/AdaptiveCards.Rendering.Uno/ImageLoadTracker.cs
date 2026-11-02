// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AdaptiveCards.Rendering.Uno
{
    internal interface IImageLoadTrackerListener
    {
        void AllImagesLoaded();

        void ImagesLoadingHadError();
    }

    internal interface IXamlBuilderListener
    {
        void AllImagesLoaded();

        void ImagesLoadingHadError();

        void XamlBuilderHadError();
    }

    /// <summary>Port of ImageLoadTracker: tracks outstanding image loads and notifies listeners.</summary>
    internal sealed class ImageLoadTracker
    {
        private readonly object _lock = new();
        private readonly Dictionary<BitmapImage, (RoutedEventHandler Opened, ExceptionRoutedEventHandler Failed)> _bitmapEventRevokers = new();
        private readonly Dictionary<SvgImageSource, (Windows.Foundation.TypedEventHandler<SvgImageSource, SvgImageSourceOpenedEventArgs> Opened, Windows.Foundation.TypedEventHandler<SvgImageSource, SvgImageSourceFailedEventArgs> Failed)> _svgEventRevokers = new();
        private readonly HashSet<IImageLoadTrackerListener> _listeners = new();
        private int _trackedImageCount;
        private int _totalImageCount;
        private bool _hasFailure;

        public void TrackImage(ImageSource image)
        {
            if (image is BitmapImage bitmapImage)
            {
                TrackBitmapImage(bitmapImage);
            }
            else if (image is SvgImageSource svgImage)
            {
                TrackSvgImage(svgImage);
            }
        }

        public void TrackBitmapImage(BitmapImage bitmapImage)
        {
            lock (_lock)
            {
                if (_bitmapEventRevokers.ContainsKey(bitmapImage))
                {
                    return;
                }

                RoutedEventHandler opened = (s, e) => ImageLoadResultReceived(s);
                ExceptionRoutedEventHandler failed = (s, e) => ImageLoadResultReceived(s);
                bitmapImage.ImageOpened += opened;
                bitmapImage.ImageFailed += failed;
                _bitmapEventRevokers[bitmapImage] = (opened, failed);
                _trackedImageCount++;
                _totalImageCount++;
            }
        }

        public void TrackSvgImage(SvgImageSource svgImage)
        {
            lock (_lock)
            {
                if (_svgEventRevokers.ContainsKey(svgImage))
                {
                    return;
                }

                Windows.Foundation.TypedEventHandler<SvgImageSource, SvgImageSourceOpenedEventArgs> opened = (s, e) => ImageLoadResultReceived(s);
                Windows.Foundation.TypedEventHandler<SvgImageSource, SvgImageSourceFailedEventArgs> failed = (s, e) => ImageLoadResultReceived(s);
                svgImage.Opened += opened;
                svgImage.OpenFailed += failed;
                _svgEventRevokers[svgImage] = (opened, failed);
                _trackedImageCount++;
                _totalImageCount++;
            }
        }

        public void MarkFailedLoadImage(ImageSource image)
        {
            _hasFailure = true;
            ImageLoadResultReceived(image);
        }

        public void AbandonOutstandingImages()
        {
            lock (_lock)
            {
                foreach (var kvp in _bitmapEventRevokers)
                {
                    Unsubscribe(kvp.Key, kvp.Value);
                }

                _bitmapEventRevokers.Clear();

                foreach (var kvp in _svgEventRevokers)
                {
                    Unsubscribe(kvp.Key, kvp.Value);
                }

                _svgEventRevokers.Clear();
            }
        }

        public void AddListener(IImageLoadTrackerListener listener) => _listeners.Add(listener);

        public void RemoveListener(IImageLoadTrackerListener listener) => _listeners.Remove(listener);

        public int GetTotalImagesTracked() => _totalImageCount;

        private void ImageLoadResultReceived(object sender)
        {
            bool fire;
            lock (_lock)
            {
                _trackedImageCount--;

                if (sender is BitmapImage bitmap && _bitmapEventRevokers.TryGetValue(bitmap, out var bitmapHandlers))
                {
                    Unsubscribe(bitmap, bitmapHandlers);
                }
                else if (sender is SvgImageSource svg && _svgEventRevokers.TryGetValue(svg, out var svgHandlers))
                {
                    Unsubscribe(svg, svgHandlers);
                }

                fire = _trackedImageCount == 0;
            }

            if (fire)
            {
                if (_hasFailure)
                {
                    FireImagesLoadingHadError();
                }
                else
                {
                    FireAllImagesLoaded();
                }
            }
        }

        private static void Unsubscribe(BitmapImage image, (RoutedEventHandler Opened, ExceptionRoutedEventHandler Failed) handlers)
        {
            image.ImageOpened -= handlers.Opened;
            image.ImageFailed -= handlers.Failed;
        }

        private static void Unsubscribe(
            SvgImageSource image,
            (Windows.Foundation.TypedEventHandler<SvgImageSource, SvgImageSourceOpenedEventArgs> Opened, Windows.Foundation.TypedEventHandler<SvgImageSource, SvgImageSourceFailedEventArgs> Failed) handlers)
        {
            image.Opened -= handlers.Opened;
            image.OpenFailed -= handlers.Failed;
        }

        private void FireAllImagesLoaded()
        {
            foreach (var listener in new List<IImageLoadTrackerListener>(_listeners))
            {
                listener.AllImagesLoaded();
            }
        }

        private void FireImagesLoadingHadError()
        {
            foreach (var listener in new List<IImageLoadTrackerListener>(_listeners))
            {
                listener.ImagesLoadingHadError();
            }
        }
    }
}
