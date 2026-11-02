// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>
    /// Renders a background image honoring the Adaptive Cards fill modes (cover, repeat, repeatHorizontally,
    /// repeatVertically) and alignments. Port of the WinUI3 renderer's TileControl.
    /// </summary>
    public partial class TileControl : ContentControl
    {
        private readonly Canvas _containerElement;
        private readonly ImageBrush _brushXaml;
        private readonly List<Rectangle> _xamlChildren = new();
        private UIElement _resolvedImage;
        private Size _containerSize;
        private BitmapImage _trackedBitmap;
        private SvgImageSource _trackedSvg;

        public TileControl()
        {
            _containerElement = new Canvas();
            _brushXaml = new ImageBrush();
            Content = _containerElement;
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
        }

        public UIElement ResolvedImage => _resolvedImage;

        public AdaptiveBackgroundImage BackgroundImage { get; set; } = new AdaptiveBackgroundImage();

        public Size ImageSize { get; set; }

        public void LoadImageBrush(UIElement uielement)
        {
            _resolvedImage = uielement;

            if (_resolvedImage is Image image && image.Source is ImageSource imageSource)
            {
                if (imageSource is BitmapImage bitmapImage)
                {
                    if (_trackedBitmap != null)
                    {
                        _trackedBitmap.ImageOpened -= OnImageOpened;
                    }

                    _trackedBitmap = bitmapImage;
                    bitmapImage.ImageOpened += OnImageOpened;
                    _brushXaml.ImageSource = imageSource;

                    // The image may already be loaded (e.g. cached); refresh using its current size.
                    if (bitmapImage.PixelWidth > 0 && bitmapImage.PixelHeight > 0)
                    {
                        ImageSize = new Size(bitmapImage.PixelWidth, bitmapImage.PixelHeight);
                        RefreshContainerTile();
                    }
                }
                else if (imageSource is SvgImageSource svgImageSource)
                {
                    if (_trackedSvg != null)
                    {
                        _trackedSvg.Opened -= OnSvgImageOpened;
                    }

                    _trackedSvg = svgImageSource;
                    svgImageSource.Opened += OnSvgImageOpened;
                    _brushXaml.ImageSource = imageSource;

                    if (svgImageSource.RasterizePixelWidth > 0 && svgImageSource.RasterizePixelHeight > 0)
                    {
                        ImageSize = new Size(svgImageSource.RasterizePixelWidth, svgImageSource.RasterizePixelHeight);
                        RefreshContainerTile();
                    }
                }
            }
        }

        private void OnImageOpened(object sender, RoutedEventArgs args)
        {
            if (_resolvedImage is Image image && image.Source is BitmapSource bitmapSource)
            {
                ImageSize = new Size(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                RefreshContainerTile();
            }
        }

        private void OnSvgImageOpened(SvgImageSource sender, SvgImageSourceOpenedEventArgs args)
        {
            if (_resolvedImage is Image image && image.Source is SvgImageSource svgImageSource)
            {
                ImageSize = new Size(svgImageSource.RasterizePixelWidth, svgImageSource.RasterizePixelHeight);
                RefreshContainerTile();
            }
        }

        protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _containerSize = base.ArrangeOverride(arrangeBounds);

            // Define clip properties for the container
            _containerElement.Clip = new RectangleGeometry { Rect = new Rect(0, 0, _containerSize.Width, _containerSize.Height) };

            // Reposition the tiles
            RefreshContainerTile();

            return _containerSize;
        }

        private void RefreshContainerTile()
        {
            var backgroundImage = BackgroundImage ?? new AdaptiveBackgroundImage();
            var fillMode = backgroundImage.FillMode;
            var hAlignment = backgroundImage.HorizontalAlignment;
            var vAlignment = backgroundImage.VerticalAlignment;

            int numberImagePerColumn = 1;
            int numberImagePerRow = 1;
            double offsetVerticalAlignment = 0;
            double offsetHorizontalAlignment = 0;
            var imageSize = ImageSize;

            if (imageSize.Width != 0 && imageSize.Height != 0)
            {
                switch (fillMode)
                {
                    case BackgroundImageFillMode.RepeatHorizontally:
                        numberImagePerRow = (int)Math.Ceiling(_containerSize.Width / imageSize.Width);
                        numberImagePerColumn = 1;
                        switch (vAlignment)
                        {
                            case VAlignment.Bottom:
                                offsetVerticalAlignment = _containerSize.Height - imageSize.Height;
                                break;
                            case VAlignment.Center:
                                offsetVerticalAlignment = (_containerSize.Height - imageSize.Height) / 2.0;
                                break;
                        }

                        break;

                    case BackgroundImageFillMode.RepeatVertically:
                        numberImagePerRow = 1;
                        numberImagePerColumn = (int)Math.Ceiling(_containerSize.Height / imageSize.Height);
                        switch (hAlignment)
                        {
                            case HAlignment.Right:
                                offsetHorizontalAlignment = _containerSize.Width - imageSize.Width;
                                break;
                            case HAlignment.Center:
                                offsetHorizontalAlignment = (_containerSize.Width - imageSize.Width) / 2.0;
                                break;
                        }

                        break;

                    case BackgroundImageFillMode.Repeat:
                        numberImagePerColumn = (int)Math.Ceiling(_containerSize.Height / imageSize.Height);
                        numberImagePerRow = (int)Math.Ceiling(_containerSize.Width / imageSize.Width);
                        break;

                    case BackgroundImageFillMode.Cover:
                    default:
                        numberImagePerColumn = 1;
                        numberImagePerRow = 1;
                        offsetHorizontalAlignment = 0;
                        offsetVerticalAlignment = 0;
                        break;
                }
            }

            numberImagePerColumn = Math.Max(numberImagePerColumn, 0);
            numberImagePerRow = Math.Max(numberImagePerRow, 0);
            int numberSpriteToInstanciate = numberImagePerColumn * numberImagePerRow;
            int count = _xamlChildren.Count;

            var children = _containerElement.Children;

            // Instanciate or remove extra sprites
            if (numberSpriteToInstanciate > count)
            {
                for (int x = 0; x < numberSpriteToInstanciate - count; x++)
                {
                    var rectangle = new Rectangle();
                    children.Add(rectangle);
                    _xamlChildren.Add(rectangle);
                }
            }
            else
            {
                for (int x = 0; x < count - numberSpriteToInstanciate; x++)
                {
                    children.RemoveAt(children.Count - 1);
                    _xamlChildren.RemoveAt(_xamlChildren.Count - 1);
                }
            }

            // Convert the alignments to the brush's alignment types (they share the same ordering).
            for (int x = 0, index = 0; x < numberImagePerRow; x++)
            {
                for (int y = 0; y < numberImagePerColumn; y++, index++)
                {
                    var rectangle = _xamlChildren[index];

                    // Set the image brush on the rectangle
                    rectangle.Fill = _brushXaml;

                    double originPositionX = 0.0, originPositionY = 0.0;
                    if (fillMode != BackgroundImageFillMode.Cover)
                    {
                        originPositionX = (x * imageSize.Width) + offsetHorizontalAlignment;
                        originPositionY = (y * imageSize.Height) + offsetVerticalAlignment;
                    }

                    Canvas.SetLeft(rectangle, originPositionX);
                    Canvas.SetTop(rectangle, originPositionY);

                    double imageWidth, imageHeight;
                    if (fillMode == BackgroundImageFillMode.Cover)
                    {
                        imageWidth = _containerSize.Width;
                        imageHeight = _containerSize.Height;
                    }
                    else
                    {
                        imageWidth = imageSize.Width;
                        imageHeight = imageSize.Height;
                    }

                    rectangle.Width = imageWidth;
                    rectangle.Height = imageHeight;

                    if (fillMode == BackgroundImageFillMode.Cover)
                    {
                        _brushXaml.Stretch = Stretch.UniformToFill;
                        _brushXaml.AlignmentX = (AlignmentX)(int)hAlignment;
                        _brushXaml.AlignmentY = (AlignmentY)(int)vAlignment;
                    }
                }
            }
        }
    }
}
