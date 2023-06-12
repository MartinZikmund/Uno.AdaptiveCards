using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;

namespace UnoWinUISample
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            var card = new AdaptiveCard()
            {
                Body =
                {
                    new AdaptiveTextBlock()
                    {
                        Text= "Hello World!"
                    }
                }
            };
            var renderer = new AdaptiveCardRenderer();
            var renderedCard = renderer.RenderAdaptiveCard(card);
            if (renderedCard.FrameworkElement is not null)
            {
                Container.Child = renderedCard.FrameworkElement;
            }
        }
    }
}
