// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AdaptiveCards.Rendering.Uno
{
    /// <summary>Port of RenderedElementStruct.</summary>
    internal struct RenderedElementStruct
    {
        public UIElement RenderedUI;
        public IAdaptiveCardElement AdaptiveElement;

        public RenderedElementStruct(UIElement renderedUI, IAdaptiveCardElement adaptiveElement)
        {
            RenderedUI = renderedUI;
            AdaptiveElement = adaptiveElement;
        }

        public void Deconstruct(out UIElement renderedUI, out IAdaptiveCardElement adaptiveElement)
        {
            renderedUI = RenderedUI;
            adaptiveElement = AdaptiveElement;
        }
    }

    /// <summary>
    /// Port of the WinUI3 renderer's XamlBuilder. The image-building members (BuildImage, SetImageOnUIElement, ...)
    /// live in the AdaptiveImageRenderer port as another part of this partial class, as they do in the C++ sources.
    /// </summary>
    internal sealed partial class XamlBuilder : IImageLoadTrackerListener
    {
        private readonly ImageLoadTracker _imageLoadTracker;
        private readonly HashSet<IXamlBuilderListener> _listeners = new();
        private uint _fixedWidth;
        private uint _fixedHeight;
        private bool _fixedDimensions;
        private bool _enableXamlImageHandling;

        public XamlBuilder()
        {
            _imageLoadTracker = new ImageLoadTracker();
            _imageLoadTracker.AddListener(this);
        }

        internal ImageLoadTracker ImageLoadTracker => _imageLoadTracker;

        internal bool EnableXamlImageHandling => _enableXamlImageHandling;

        internal bool HasFixedDimensions => _fixedDimensions;

        internal uint FixedWidth => _fixedWidth;

        internal uint FixedHeight => _fixedHeight;

        public void AllImagesLoaded() => FireAllImagesLoaded();

        public void ImagesLoadingHadError() => FireImagesLoadingHadError();

        public static FrameworkElement BuildXamlTreeFromAdaptiveCard(
            AdaptiveCard adaptiveCard,
            AdaptiveRenderContext renderContext,
            XamlBuilder xamlBuilder,
            ContainerStyle defaultContainerStyle = ContainerStyle.Default)
        {
            try
            {
                var hostConfig = renderContext.HostConfig;
                bool ifSupportsInteractivity = XamlHelpers.SupportsInteractivity(hostConfig);

                var adaptiveCardConfig = hostConfig.AdaptiveCard;
                bool allowCustomStyle = adaptiveCardConfig.AllowCustomStyle;

                var containerStyle = defaultContainerStyle;
                if (allowCustomStyle)
                {
                    var cardStyle = adaptiveCard.Style;
                    if (cardStyle != ContainerStyle.None)
                    {
                        containerStyle = cardStyle;
                    }
                }

                var renderArgs = new AdaptiveRenderArgs(containerStyle, null, adaptiveCard, null);

                var (bodyElementContainer, rootElement) = CreateRootCardElement(adaptiveCard, renderContext, renderArgs, xamlBuilder);
                if (rootElement == null)
                {
                    return null;
                }

                var rootAsFrameworkElement = (FrameworkElement)rootElement;

                uint cardMinHeight = adaptiveCard.MinHeight;
                if (cardMinHeight > 0)
                {
                    rootAsFrameworkElement.MinHeight = cardMinHeight;
                }

                var selectAction = adaptiveCard.SelectAction;

                // Create a new UIElement pointer to house the root element decorated with select action
                var rootSelectActionElement = ActionHelpers.HandleSelectAction(
                    null, selectAction, renderContext, renderArgs, rootElement, ifSupportsInteractivity, true);
                rootAsFrameworkElement = (FrameworkElement)rootSelectActionElement;

                AutomationProperties.SetName(rootAsFrameworkElement, adaptiveCard.Speak ?? string.Empty);

                // Enumerate the child items of the card and build xaml for them
                var body = adaptiveCard.Body;
                var bodyRenderArgs = new AdaptiveRenderArgs(containerStyle, rootAsFrameworkElement, adaptiveCard, null);

                BuildPanelChildren(body, bodyElementContainer, renderContext, bodyRenderArgs, _ => { });

                XamlHelpers.SetVerticalContentAlignmentToChildren(bodyElementContainer, adaptiveCard.VerticalContentAlignment);

                var actions = adaptiveCard.Actions;
                if (actions.Count > 0)
                {
                    if (ifSupportsInteractivity)
                    {
                        ActionHelpers.BuildActions(adaptiveCard, actions, bodyElementContainer, body.Count > 0, renderContext, renderArgs);
                    }
                    else
                    {
                        renderContext.AddWarning(
                            WarningStatusCode.InteractivityNotSupported,
                            "Actions collection was present in card, but interactivity is not supported");
                    }
                }

                bool isInShowCard = renderArgs.IsInShowCard;
                if (!isInShowCard && xamlBuilder != null)
                {
                    if (xamlBuilder._listeners.Count == 0)
                    {
                        // If we're done and no one's listening for the images to load, make sure
                        // any outstanding image loads are no longer tracked.
                        xamlBuilder._imageLoadTracker.AbandonOutstandingImages();
                    }
                    else if (xamlBuilder._imageLoadTracker.GetTotalImagesTracked() == 0)
                    {
                        // If there are no images to track, fire the all images loaded
                        // event to signal the xaml is ready
                        xamlBuilder.FireAllImagesLoaded();
                    }
                }

                return rootAsFrameworkElement;
            }
            catch (Exception ex)
            {
                XamlHelpers.ErrForRenderFailed(renderContext, ex.Message);
                return null;
            }
        }

        internal void AddListener(IXamlBuilderListener listener) => _listeners.Add(listener);

        internal void RemoveListener(IXamlBuilderListener listener) => _listeners.Remove(listener);

        public void SetFixedDimensions(uint width, uint height)
        {
            _fixedDimensions = true;
            _fixedWidth = width;
            _fixedHeight = height;
        }

        public void SetEnableXamlImageHandling(bool enableXamlImageHandling) => _enableXamlImageHandling = enableXamlImageHandling;

        private static (Panel BodyElementContainer, UIElement RootElement) CreateRootCardElement(
            AdaptiveCard adaptiveCard,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            XamlBuilder xamlBuilder)
        {
            try
            {
                // The root of an adaptive card is a composite of several elements, depending on the card
                // properties.  From back to front these are:
                // Grid - Root element, used to enable children to stack above each other and size to fit
                // Image (optional) - Holds the background image if one is set
                // Shape (optional) - Provides the background image overlay, if one is set
                // StackPanel - The container for all the card's body elements
                var rootElement = new Grid();
                var hostConfig = renderContext.HostConfig;
                var containerStyle = renderArgs.ContainerStyle;

                var backgroundColor = Util.GetBackgroundColorFromStyle(containerStyle, hostConfig);
                rootElement.Background = new SolidColorBrush(backgroundColor);

                var backgroundImage = adaptiveCard.BackgroundImage;
                if (Util.IsBackgroundImageValid(backgroundImage))
                {
                    XamlHelpers.ApplyBackgroundToRoot(rootElement, backgroundImage, renderContext);
                }

                // Outer panel that contains the main body and any inline show cards
                var spacingConfig = hostConfig.Spacing;
                uint padding = spacingConfig.Padding;

                // Configure WholeItemsPanel to not clip bleeding containers
                WholeItemsPanel.SetBleedMargin(padding);

                // Now create the inner stack panel to serve as the root host for all the
                // body elements and apply padding from host configuration
                var bodyElementHost = new WholeItemsPanel();
                bodyElementHost.SetMainPanel(true);
                bodyElementHost.SetAdaptiveHeight(true);

                if (renderArgs.IsInShowCard)
                {
                    XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.ShowCard.Card", bodyElementHost);
                }
                else
                {
                    XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.Card", bodyElementHost);
                }

                if (bodyElementHost.Style == null)
                {
                    XamlHelpers.ApplyMarginToXamlElement(hostConfig, bodyElementHost);
                }

                var adaptiveCardHeightType = adaptiveCard.Height;

                XamlHelpers.AppendXamlElementToPanel(bodyElementHost, rootElement, adaptiveCardHeightType);
                Panel bodyElementContainer = bodyElementHost;

                if (xamlBuilder != null && xamlBuilder._fixedDimensions)
                {
                    rootElement.Width = xamlBuilder._fixedWidth;
                    rootElement.Height = xamlBuilder._fixedHeight;
                    rootElement.MaxHeight = xamlBuilder._fixedHeight;
                }

                if (adaptiveCardHeightType == HeightType.Stretch)
                {
                    rootElement.VerticalAlignment = VerticalAlignment.Stretch;
                }

                if (renderContext.Rtl is bool contextRtl)
                {
                    rootElement.FlowDirection = contextRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                }

                return (bodyElementContainer, rootElement);
            }
            catch (Exception ex)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, "RootElement (visual tree root)", ex.Message);
                return (null, null);
            }
        }

        internal void FireAllImagesLoaded()
        {
            foreach (var listener in new List<IXamlBuilderListener>(_listeners))
            {
                listener.AllImagesLoaded();
            }
        }

        internal void FireImagesLoadingHadError()
        {
            foreach (var listener in new List<IXamlBuilderListener>(_listeners))
            {
                listener.ImagesLoadingHadError();
            }
        }

        private static void ShouldFallback(
            IAdaptiveCardElement element,
            IAdaptiveElementRenderer elementRenderer,
            AdaptiveRenderArgs renderArgs,
            AdaptiveFeatureRegistration featureRegistration,
            bool ancestorHasFallback)
        {
            var elementFallback = element.FallbackType;
            bool elementHasFallback = elementFallback != FallbackType.None || ancestorHasFallback;
            renderArgs.AncestorHasFallback = elementHasFallback;

            bool shouldFallback = featureRegistration != null && !Util.MeetsRequirements(element, featureRegistration);

            if (elementRenderer == null || shouldFallback)
            {
                throw new PerformFallbackException();
            }
        }

        internal static RenderedElementStruct RenderAsUIElement(
            IAdaptiveCardElement element,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            AdaptiveFeatureRegistration featureRegistration,
            bool ancestorHasFallback)
        {
            try
            {
                var elementRenderer = renderContext.ElementRenderers.Get(element.ElementTypeString);

                ShouldFallback(element, elementRenderer, renderArgs, featureRegistration, ancestorHasFallback);

                return new RenderedElementStruct(elementRenderer.Render(element, renderContext, renderArgs), element);
            }
            catch (PerformFallbackException)
            {
                try
                {
                    var (newControl, renderedElement) = XamlHelpers.RenderFallback(element, renderContext, renderArgs);
                    return new RenderedElementStruct(newControl, renderedElement);
                }
                catch (PerformFallbackException)
                {
                    // If we get here, fallback was requested by an element, but it had no fallback and no
                    // ancestor had fallback either. If an ancestor has fallback, rethrow so it handles it.
                    if (ancestorHasFallback)
                    {
                        throw;
                    }
                }
            }

            return default;
        }

        internal static void BuildPanelChildren(
            IList<IAdaptiveCardElement> children,
            Panel parentPanel,
            AdaptiveRenderContext renderContext,
            AdaptiveRenderArgs renderArgs,
            Action<UIElement> childCreatedCallback)
        {
            int iElement = 0;
            bool ancestorHasFallback = renderArgs.AncestorHasFallback;
            var featureRegistration = renderContext.FeatureRegistration;

            foreach (var element in children)
            {
                // Get fallback state
                var (newControl, renderedElement) =
                    RenderAsUIElement(element, renderContext, renderArgs, featureRegistration, ancestorHasFallback);

                // If we got a control, add a separator if needed and the control to the parent panel
                if (newControl != null)
                {
                    var hostConfig = renderContext.HostConfig;
                    var separator = XamlHelpers.AddSeparatorIfNeeded(ref iElement, element, hostConfig, renderContext, parentPanel);

                    // If the renderedElement was an input, render the label and error message
                    if (renderedElement is IAdaptiveInputElement inputElement)
                    {
                        newControl = XamlHelpers.HandleLabelAndErrorMessage(inputElement, renderContext, newControl);
                    }

                    XamlHelpers.AddRenderedControl(newControl, element, parentPanel, separator, null, childCreatedCallback);
                }

                // Revert the ancestorHasFallback value
                renderArgs.AncestorHasFallback = ancestorHasFallback;
            }

            XamlHelpers.SetSeparatorVisibility(parentPanel);
        }
    }
}
