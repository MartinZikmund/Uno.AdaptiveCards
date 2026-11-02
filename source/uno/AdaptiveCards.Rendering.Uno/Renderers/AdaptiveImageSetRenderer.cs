// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveImageSetRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var adaptiveImageSet = (AdaptiveImageSet)cardElement;

#if HAS_UNO
                // Uno's VariableSizedWrapGrid is unreliable here (issue #6); use an equivalent uniform wrap panel.
                var xamlGrid = new ImageSetWrapPanel();
#else
                var xamlGrid = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal };
#endif

                var imageSetConfig = renderContext.HostConfig.ImageSet;
                var imageSize = adaptiveImageSet.ImageSize;
                if (imageSize == ImageSize.None)
                {
                    imageSize = imageSetConfig.ImageSize;
                }

                var imageRenderer = renderContext.ElementRenderers.Get("Image");
                if (imageRenderer != null)
                {
                    var childRenderArgs = new AdaptiveRenderArgs(renderArgs.ContainerStyle, xamlGrid, renderArgs);
                    foreach (var adaptiveImage in adaptiveImageSet.Images)
                    {
                        adaptiveImage.Size = imageSize;
                        var uiImage = imageRenderer.Render(adaptiveImage, renderContext, childRenderArgs);
                        if (uiImage is FrameworkElement uiImageAsFrameworkElement)
                        {
                            uiImageAsFrameworkElement.MaxHeight = imageSetConfig.MaxImageHeight;
                            XamlHelpers.AppendXamlElementToPanel(uiImageAsFrameworkElement, xamlGrid);
                        }
                    }
                }
                else
                {
                    renderContext.AddWarning(WarningStatusCode.NoRendererForType, "No renderer found for type: Image");
                    return null;
                }

                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.ImageSet", xamlGrid);
                return xamlGrid;
            }
            catch (Exception ex)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }
    }
}
