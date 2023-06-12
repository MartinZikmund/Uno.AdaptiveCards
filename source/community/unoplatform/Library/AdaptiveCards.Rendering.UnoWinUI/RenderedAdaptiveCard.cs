using Microsoft.UI.Xaml;

namespace AdaptiveCards.Rendering.WinUI3;

public class RenderedAdaptiveCard
{
    private FrameworkElement? _frameworkElement;

    public RenderedAdaptiveCard()
    {

    }

    public FrameworkElement? FrameworkElement { get; }

    //AdaptiveCards.ObjectModel.WinUI3.AdaptiveCard OriginatingCard { get; };
    //AdaptiveHostConfig OriginatingHostConfig { get; };
    //AdaptiveInputs UserInputs { get; };
    //Windows.Foundation.Collections.IVector<AdaptiveCards.ObjectModel.WinUI3.AdaptiveError> Errors { get; };
    //Windows.Foundation.Collections.IVector<AdaptiveCards.ObjectModel.WinUI3.AdaptiveWarning> Warnings { get; };

    //event Windows.Foundation.TypedEventHandler<RenderedAdaptiveCard, AdaptiveActionEventArgs> Action;
    //event Windows.Foundation.TypedEventHandler<RenderedAdaptiveCard, AdaptiveMediaEventArgs> MediaClicked;

    internal void SetFrameworkElement(FrameworkElement? value) => _frameworkElement = value;
}
