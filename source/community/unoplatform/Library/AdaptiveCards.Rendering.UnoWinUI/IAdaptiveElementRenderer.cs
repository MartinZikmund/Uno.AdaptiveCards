using AdaptiveCards.ObjectModel.WinUI3;

namespace AdaptiveCards.Rendering.WinUI3;

interface IAdaptiveElementRenderer
{
    Microsoft.UI.Xaml.UIElement? Render(
        IAdaptiveCardElement element,
        AdaptiveRenderContext context,
        AdaptiveRenderArgs renderArgs);
}
