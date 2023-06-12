using AdaptiveCards.ObjectModel.WinUI3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using System;

namespace AdaptiveCards.Rendering.WinUI3;

public class AdaptiveTextBlockRenderer : IAdaptiveElementRenderer
{
    public AdaptiveTextBlockRenderer()
    {
    }

    UIElement? IAdaptiveElementRenderer.Render(
        IAdaptiveCardElement cardElement,
        AdaptiveRenderContext renderContext,
        AdaptiveRenderArgs renderArgs)
    {
        try
        {
            var adaptiveTextBlock = cardElement as AdaptiveTextBlock;
            string text = adaptiveTextBlock?.Text;

            // If the text is null, return immediately without constructing a text block
            if (text is null)
            {
                renderContext.AddError(ErrorStatusCode.RequiredPropertyMissing,
                                       "Required property, \"text\", is missing from TextBlock");
                return null;
            }

            TextBlock xamlTextBlock = new();

            XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.TextBlock", xamlTextBlock);
            StyleXamlTextBlockProperties(adaptiveTextBlock, renderContext, renderArgs, xamlTextBlock);
            var inlines = xamlTextBlock.Inlines;

            // Check if this text block has a style and if so apply the appropriate styling from the host config
            var textStyleRef = adaptiveTextBlock.Style == null ? renderContext.TextStyle : adaptiveTextBlock.Style;
            TextStyle textStyle = GetValueFromRef(textStyleRef, TextStyle.Default);

            var hostConfig = renderContext.HostConfig;
            var textStylesConfig = hostConfig.TextStyles();

            if (textStyle == TextStyle.Heading)
            {
                var headingTextStyleConfig = textStylesConfig.Heading();
                SetXamlInlinesWithTextStyleConfig(adaptiveTextBlock, renderContext, renderArgs, headingTextStyleConfig, xamlTextBlock);
            }
            else if (textStyle == TextStyle.ColumnHeader)
            {
                var columnHeaderTextStyleConfig = textStylesConfig.ColumnHeader();
                SetXamlInlinesWithTextStyleConfig(adaptiveTextBlock, renderContext, renderArgs, columnHeaderTextStyleConfig, xamlTextBlock);
            }
            else
            {
                SetXamlInlines(adaptiveTextBlock, renderContext, renderArgs, false, inlines);
            }

            // Ensure left edge of text is consistent regardless of font size, so both small and large fonts
            // are flush on the left edge of the card by enabling TrimSideBearings
            xamlTextBlock.OpticalMarginAlignment(OpticalMarginAlignment.TrimSideBearings);

            // If this text block has a heading style, set the corresponding automation property
            if (textStyle == TextStyle.Heading)
            {
                AutomationProperties.SetHeadingLevel(xamlTextBlock, GetHeadingLevelFromContext(renderContext));
            }

            return xamlTextBlock;
        }
        catch (Exception ex)
        {
            XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString(), ex.Message);
            return null;
        }
    }

    private AutomationHeadingLevel GetHeadingLevelFromContext(AdaptiveRenderContext renderContext)
    {
        var textBlockConfig = renderContext.HostConfig.TextBlock;

        return textBlockConfig.HeadingLevel switch
        {
            0 or 1 => AutomationHeadingLevel.Level1,
            2 => AutomationHeadingLevel.Level2,
            3 => AutomationHeadingLevel.Level3,
            4 => AutomationHeadingLevel.Level4,
            5 => AutomationHeadingLevel.Level5,
            6 => AutomationHeadingLevel.Level6,
            7 => AutomationHeadingLevel.Level7,
            8 => AutomationHeadingLevel.Level8,
            _ => AutomationHeadingLevel.Level9,
        };
    }
}
