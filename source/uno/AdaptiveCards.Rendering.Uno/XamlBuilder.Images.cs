// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Storage.Streams;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Image handling part of XamlBuilder (port of the XamlBuilder:: members in AdaptiveImageRenderer.cpp).</summary>
    internal partial class XamlBuilder
    {
        /// <summary>Port of XamlBuilder::ImageProperties&lt;TElement&gt;.</summary>
        internal sealed class ImageProperties
        {
            public FrameworkElement UIElement { get; init; }

            public bool IsAutoSize { get; init; }

            public object ParentElement { get; init; }

            public object ImageContainer { get; init; }

            public bool IsVisible { get; init; }

            public bool IsImageSvg { get; init; }

            public Stretch Stretch { get; init; } = Stretch.UniformToFill;
        }

        internal UIElement BuildImage(IAdaptiveCardElement adaptiveCardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            var adaptiveImage = (AdaptiveImage)adaptiveCardElement;
            var hostConfig = renderContext.HostConfig;

            var imageUrl = Util.GetUrlFromString(hostConfig, adaptiveImage.Url);
            if (imageUrl == null)
            {
                renderContext.AddWarning(WarningStatusCode.AssetLoadFailed, "Image not found");
                return null;
            }

            bool isImageSvg = Util.IsSvgImage(imageUrl);

            uint pixelWidth = adaptiveImage.PixelWidth;
            uint pixelHeight = adaptiveImage.PixelHeight;

            bool hasExplicitMeasurements = pixelWidth != 0 || pixelHeight != 0;
            bool isAspectRatioNeeded = pixelWidth != 0 && pixelHeight != 0;

            var size = ImageSize.None;
            if (!hasExplicitMeasurements)
            {
                size = adaptiveImage.Size;
            }

            if (size == ImageSize.None && !hasExplicitMeasurements)
            {
                size = hostConfig.Image.ImageSize;
            }

            var imageStyle = adaptiveImage.Style;
            var resourceResolvers = renderContext.ResourceResolvers;
            var backgroundColor = adaptiveImage.BackgroundColor;
            var isVisible = adaptiveCardElement.IsVisible;

            FrameworkElement frameworkElement;
            ImageSource imageSource;

            if (imageStyle == ImageStyle.Person)
            {
                var ellipse = new Ellipse();
                var backgroundEllipse = new Ellipse();

                var imageStretch = isAspectRatioNeeded ? Stretch.Fill : Stretch.UniformToFill;
                var parentElement = renderArgs.ParentElement;

                var imgProperties = new ImageProperties
                {
                    UIElement = ellipse,
                    IsAutoSize = size == ImageSize.Auto,
                    ParentElement = parentElement,
                    ImageContainer = ellipse,
                    IsVisible = isVisible,
                    IsImageSvg = isImageSvg,
                    Stretch = imageStretch,
                };

                imageSource = SetImageOnUIElement(imageUrl, resourceResolvers, imgProperties);

                if (size == ImageSize.None || size == ImageSize.Stretch || size == ImageSize.Auto || hasExplicitMeasurements)
                {
                    ellipse.Stretch = imageStretch;
                    backgroundEllipse.Stretch = imageStretch;
                }
                else
                {
                    ellipse.Stretch = Stretch.UniformToFill;
                    backgroundEllipse.Stretch = Stretch.UniformToFill;
                }

                if (!string.IsNullOrEmpty(backgroundColor))
                {
                    var color = Util.GetColorFromString(backgroundColor);
                    backgroundEllipse.Fill = new SolidColorBrush(color);

                    var imageGrid = new Grid();
                    XamlHelpers.AppendXamlElementToPanel(backgroundEllipse, imageGrid);
                    XamlHelpers.AppendXamlElementToPanel(ellipse, imageGrid);
                    frameworkElement = imageGrid;
                }
                else
                {
                    frameworkElement = ellipse;
                }
            }
            else
            {
                var xamlImage = new Image();

                if (!string.IsNullOrEmpty(backgroundColor))
                {
                    var border = new Border
                    {
                        Background = new SolidColorBrush(Util.GetColorFromString(backgroundColor)),
                        Child = xamlImage,
                    };
                    frameworkElement = border;
                }
                else
                {
                    frameworkElement = xamlImage;
                }

                if (isAspectRatioNeeded)
                {
                    xamlImage.Stretch = Stretch.Fill;
                }

                var imgProperties = new ImageProperties
                {
                    UIElement = xamlImage,
                    IsAutoSize = size == ImageSize.Auto,
                    ParentElement = renderArgs.ParentElement,
                    ImageContainer = frameworkElement,
                    IsVisible = isVisible,
                    IsImageSvg = isImageSvg,
                };

                imageSource = SetImageOnUIElement(imageUrl, resourceResolvers, imgProperties);
            }

            var sizeOptions = hostConfig.ImageSizes;
            if (hasExplicitMeasurements)
            {
                if (pixelWidth != 0)
                {
                    SetRasterizedPixelWidth(imageSource, pixelWidth);
                    if (imageStyle == ImageStyle.Person)
                    {
                        frameworkElement.Width = pixelWidth;
                    }
                    else
                    {
                        frameworkElement.MaxWidth = pixelWidth;
                    }
                }

                if (pixelHeight != 0)
                {
                    SetRasterizedPixelHeight(imageSource, pixelHeight);
                    if (imageStyle == ImageStyle.Person)
                    {
                        frameworkElement.Height = pixelHeight;
                    }
                    else
                    {
                        frameworkElement.MaxHeight = pixelHeight;
                    }
                }
            }
            else if (size == ImageSize.Small || size == ImageSize.Medium || size == ImageSize.Large)
            {
                uint imageSize = size switch
                {
                    ImageSize.Small => sizeOptions.Small,
                    ImageSize.Medium => sizeOptions.Medium,
                    _ => sizeOptions.Large,
                };

                frameworkElement.MaxWidth = imageSize;
                SetRasterizedPixelWidth(imageSource, imageSize);

                // We don't want to set a max height on the person ellipse as ellipses do not understand preserving
                // aspect ratio when constrained on both axes.
                if (imageStyle != ImageStyle.Person)
                {
                    frameworkElement.MaxHeight = imageSize;
                }
            }

            var adaptiveHorizontalAlignment = adaptiveImage.HorizontalAlignment ?? renderContext.HorizontalContentAlignment ?? HAlignment.Left;
            frameworkElement.HorizontalAlignment = adaptiveHorizontalAlignment switch
            {
                HAlignment.Right => HorizontalAlignment.Right,
                HAlignment.Center => HorizontalAlignment.Center,
                _ => HorizontalAlignment.Left,
            };

            frameworkElement.VerticalAlignment = VerticalAlignment.Top;
            XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Image", frameworkElement);

            AutomationProperties.SetName(frameworkElement, adaptiveImage.AltText ?? string.Empty);

            return ActionHelpers.HandleSelectAction(
                adaptiveCardElement,
                adaptiveImage.SelectAction,
                renderContext,
                renderArgs,
                frameworkElement,
                XamlHelpers.SupportsInteractivity(hostConfig),
                true);
        }

        // Note: DataReader.LoadAsync/DetachStream and DataWriter.DetachStream are not implemented in Uno, so the
        // stream conversions use the System.IO <-> WinRT stream adapters instead.
        private static IRandomAccessStream CreateStream(byte[] bytes) => new MemoryStream(bytes, writable: false).AsRandomAccessStream();

        private static async Task<byte[]> ReadAllBytesAsync(IRandomAccessStream stream)
        {
            stream.Seek(0);
            using var source = stream.CloneStream().AsStreamForRead();
            using var memory = new MemoryStream();
            await source.CopyToAsync(memory);
            return memory.ToArray();
        }

        /// <summary>Port of AdaptiveBase64Util::ExtractDataFromUri.</summary>
        private static string ExtractDataFromUri(string dataUri)
        {
            int commaPosition = dataUri.LastIndexOf(',');
            if (commaPosition < 0)
            {
                return string.Empty;
            }

            return dataUri.Substring(commaPosition + 1).TrimStart();
        }

        private static byte[] DecodeBase64(string data)
        {
            var builder = new StringBuilder(data.Length);
            foreach (var c in data)
            {
                if (!char.IsWhiteSpace(c))
                {
                    builder.Append(c);
                }
            }

            // Tolerate missing padding like the shared AdaptiveBase64Util does.
            while (builder.Length % 4 != 0)
            {
                builder.Append('=');
            }

            try
            {
                return Convert.FromBase64String(builder.ToString());
            }
            catch (FormatException)
            {
                return Array.Empty<byte>();
            }
        }

        private async Task<IRandomAccessStream> ResolveToStreamAsync(Uri imageUrl, AdaptiveCardResourceResolvers resolvers, bool isImageSvg)
        {
            var schemeName = imageUrl.Scheme;

            var resolver = resolvers?.Get(schemeName);
            if (resolver != null)
            {
                return await resolver.GetResourceStreamAsync(new AdaptiveCardGetResourceStreamArgs(imageUrl));
            }

            if (schemeName == "data")
            {
                // winrt::Uri::Path of a data URI is everything after "data:".
                var original = imageUrl.OriginalString;
                var imagePath = original.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ? original.Substring(5) : original;

                byte[] bytes;
                if (imagePath.Contains("base64", StringComparison.Ordinal))
                {
                    bytes = await Task.Run(() => DecodeBase64(ExtractDataFromUri(imagePath)));
                }
                else if (isImageSvg)
                {
                    bytes = Encoding.UTF8.GetBytes(Util.ExtractSvgDataFromUri(imageUrl) ?? string.Empty);
                }
                else
                {
                    bytes = Array.Empty<byte>();
                }

                return CreateStream(bytes);
            }

            if (!_enableXamlImageHandling && _listeners.Count == 0)
            {
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(imageUrl);
                return CreateStream(bytes);
            }

            return null;
        }

        private static Task RunOnDispatcherAsync(DispatcherQueue dispatcherQueue, Action action)
        {
            if (dispatcherQueue == null || dispatcherQueue.HasThreadAccess)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }))
            {
                tcs.SetException(new InvalidOperationException("Unable to dispatch to the UI thread."));
            }

            return tcs.Task;
        }

        private async void ResolveImageAsync(Uri uri, AdaptiveCardResourceResolvers resolvers, ImageSource imageSource, ImageProperties properties)
        {
            var isAutoSize = properties.IsAutoSize;
            var isSvg = properties.IsImageSvg;
            var weakImageSource = new WeakReference<ImageSource>(imageSource);
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            try
            {
                bool imageFiresOpenEvent = true;
                Task pendingLoad = null;
                var stream = await ResolveToStreamAsync(uri, resolvers, isSvg);

                if (!weakImageSource.TryGetTarget(out var strongImageSource))
                {
                    return;
                }

                if (stream != null)
                {
                    imageFiresOpenEvent = false;
                    if (isSvg)
                    {
                        var bytes = await ReadAllBytesAsync(stream);
                        Windows.Foundation.Size size = default;
                        if (!isAutoSize)
                        {
                            try
                            {
                                size = Util.ParseSizeOfSVGImageFromXmlString(Encoding.UTF8.GetString(bytes));
                            }
                            catch (Exception)
                            {
                                size = default;
                            }
                        }

                        await RunOnDispatcherAsync(dispatcherQueue, () =>
                        {
                            var svgImageSource = (SvgImageSource)strongImageSource;
                            if (!isAutoSize)
                            {
                                svgImageSource.RasterizePixelHeight = size.Height;
                                svgImageSource.RasterizePixelWidth = size.Width;
                            }

                            // C++ does not await SetSourceAsync; we keep the operation so the auto-size step below can run
                            // after decoding (otherwise the pixel size is still 0 and the image collapses to 0x0).
                            pendingLoad = svgImageSource.SetSourceAsync(CreateStream(bytes)).AsTask();
                        });
                    }
                    else
                    {
                        await RunOnDispatcherAsync(dispatcherQueue, () =>
                        {
                            var bitmapImage = (BitmapImage)strongImageSource;
                            pendingLoad = bitmapImage.SetSourceAsync(stream).AsTask();
                        });
                    }
                }
                else if (_enableXamlImageHandling || _listeners.Count == 0)
                {
                    if (isSvg)
                    {
                        using var httpClient = new HttpClient();
                        var content = await httpClient.GetStringAsync(uri);
                        var size = Util.ParseSizeOfSVGImageFromXmlString(content);
                        await RunOnDispatcherAsync(dispatcherQueue, () =>
                        {
                            var svgImageSource = (SvgImageSource)strongImageSource;
                            svgImageSource.RasterizePixelHeight = size.Height;
                            svgImageSource.RasterizePixelWidth = size.Width;
                            svgImageSource.UriSource = uri;
                        });
                    }
                    else
                    {
                        await RunOnDispatcherAsync(dispatcherQueue, () => ((BitmapImage)strongImageSource).UriSource = uri);
                    }
                }

                if (isAutoSize)
                {
                    if (pendingLoad != null)
                    {
                        try
                        {
                            await pendingLoad;
                        }
                        catch (Exception)
                        {
                            // Decoding failures are reported through the image's ImageFailed event / load tracker.
                        }
                    }

                    await RunOnDispatcherAsync(dispatcherQueue, () => SetAutoSize(
                        properties.UIElement, properties.ParentElement, properties.ImageContainer, properties.IsVisible, imageFiresOpenEvent));
                }
            }
            catch (Exception)
            {
                _imageLoadTracker.MarkFailedLoadImage(imageSource);
            }
        }

        private void ConfigureImageSource(Uri imageUrl, AdaptiveCardResourceResolvers resolvers, ImageSource imageSource, bool isImageSvg)
        {
            var schemeName = imageUrl.Scheme;
            bool hasResolver = resolvers?.Get(schemeName) != null;
            bool externalHandling = !(_enableXamlImageHandling || _listeners.Count == 0);
            bool schemeNameIsData = schemeName == "data";

            if (!isImageSvg && imageSource is BitmapImage bitmapImage)
            {
                if ((hasResolver || !schemeNameIsData) && externalHandling)
                {
                    bitmapImage.CreateOptions = BitmapCreateOptions.None;
                }
                else if (schemeNameIsData)
                {
                    bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                }
            }

            if ((hasResolver && externalHandling) || schemeNameIsData)
            {
                _imageLoadTracker.TrackImage(imageSource);
            }
        }

        private ImageSource SetImageOnUIElement(Uri imageUrl, AdaptiveCardResourceResolvers resolvers, ImageProperties imgProperties)
        {
            var imageSource = CreateImageSource(imgProperties.IsImageSvg);
            SetImageSourceToFrameworkElement(imgProperties.UIElement, imageSource, imgProperties.Stretch);
            ConfigureImageSource(imageUrl, resolvers, imageSource, imgProperties.IsImageSvg);
            ResolveImageAsync(imageUrl, resolvers, imageSource, imgProperties);
            return imageSource;
        }

        private static ImageSource CreateImageSource(bool isImageSvg) => isImageSvg ? new SvgImageSource() : new BitmapImage();

        private static void SetImageSourceToFrameworkElement(FrameworkElement destination, ImageSource imageSource, Stretch stretch)
        {
            switch (destination)
            {
                case Ellipse ellipse:
                    ellipse.Fill = new ImageBrush { ImageSource = imageSource, Stretch = stretch };
                    break;
                case Image image:
                    image.Source = imageSource;
                    break;
            }
        }

        private void SetAutoSize(FrameworkElement destination, object parentElement, object imageContainer, bool isVisible, bool imageFiresOpenEvent)
        {
            if (parentElement == null || !_enableXamlImageHandling)
            {
                return;
            }

            if (destination is Ellipse ellipse)
            {
                if (imageContainer is not Shape ellipseAsShape || ellipseAsShape.Fill is not ImageBrush brushAsImageBrush)
                {
                    return;
                }

                var imageSource = brushAsImageBrush.ImageSource;
                if (imageFiresOpenEvent)
                {
                    var weakParent = new WeakReference<object>(parentElement);
                    brushAsImageBrush.ImageOpened += (sender, args) =>
                    {
                        if (isVisible && sender is ImageBrush lambdaBrush && weakParent.TryGetTarget(out var lambdaParentElement))
                        {
                            XamlHelpers.SetAutoImageSize(ellipse, lambdaParentElement, lambdaBrush.ImageSource, isVisible);
                        }
                    };
                }
                else
                {
                    XamlHelpers.SetAutoImageSize(ellipse, parentElement, imageSource, isVisible);
                }
            }
            else if (destination is Image xamlImage)
            {
                var imageSource = xamlImage.Source;
                if (imageFiresOpenEvent)
                {
#if HAS_UNO
                    // Uno does not load the source of a collapsed Image, so ImageOpened would never fire. Hide the
                    // image with opacity instead until its natural size is known.
                    xamlImage.Opacity = 0;
                    xamlImage.ImageOpened += (sender, args) => ((Image)sender).Opacity = 1;
                    xamlImage.ImageFailed += (sender, args) => ((Image)sender).Opacity = 1;
#else
                    xamlImage.Visibility = Visibility.Collapsed;
#endif

                    var weakParent = new WeakReference<object>(parentElement);
                    var weakImage = new WeakReference<Image>(xamlImage);
                    xamlImage.ImageOpened += (sender, args) =>
                    {
                        if (weakImage.TryGetTarget(out var lambdaImage) && weakParent.TryGetTarget(out var lambdaParentElement))
                        {
                            XamlHelpers.SetAutoImageSize(lambdaImage, lambdaParentElement, imageSource, isVisible);
                        }
                    };
                }
                else
                {
                    XamlHelpers.SetAutoImageSize(xamlImage, parentElement, imageSource, isVisible);
                }
            }
        }

        private static void SetRasterizedPixelHeight(ImageSource imageSource, double imageSize)
        {
            if (imageSource is SvgImageSource image && double.IsInfinity(image.RasterizePixelHeight))
            {
                image.RasterizePixelHeight = imageSize;
            }
        }

        private static void SetRasterizedPixelWidth(ImageSource imageSource, double imageSize)
        {
            if (imageSource is SvgImageSource image && double.IsInfinity(image.RasterizePixelWidth))
            {
                image.RasterizePixelWidth = imageSize;
            }
        }
    }
}
