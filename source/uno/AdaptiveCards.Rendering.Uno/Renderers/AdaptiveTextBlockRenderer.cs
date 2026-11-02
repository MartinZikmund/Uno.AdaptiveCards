// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using AdaptiveCards.ObjectModel.Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace AdaptiveCards.Rendering.Uno
{
    public sealed class AdaptiveTextBlockRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement cardElement, AdaptiveRenderContext renderContext, AdaptiveRenderArgs renderArgs)
        {
            try
            {
                var adaptiveTextBlock = (AdaptiveTextBlock)cardElement;
                if (string.IsNullOrEmpty(adaptiveTextBlock.Text))
                {
                    renderContext.AddError(ErrorStatusCode.RequiredPropertyMissing, "Required property, \"text\", is missing from TextBlock");
                    return null;
                }

                var xamlTextBlock = new TextBlock();
                XamlHelpers.SetStyleFromResourceDictionary(renderContext, "Adaptive.TextBlock", xamlTextBlock);

                TextHelpers.StyleXamlTextBlockProperties(adaptiveTextBlock, renderContext, renderArgs, xamlTextBlock);

                var textStyle = adaptiveTextBlock.Style ?? renderContext.TextStyle ?? TextStyle.Default;
                var textStylesConfig = renderContext.HostConfig.TextStyles;

                if (textStyle == TextStyle.Heading)
                {
                    TextHelpers.SetXamlInlinesWithTextStyleConfig(adaptiveTextBlock, renderContext, renderArgs, textStylesConfig.Heading, xamlTextBlock);
                }
                else if (textStyle == TextStyle.ColumnHeader)
                {
                    TextHelpers.SetXamlInlinesWithTextStyleConfig(adaptiveTextBlock, renderContext, renderArgs, textStylesConfig.ColumnHeader, xamlTextBlock);
                }
                else
                {
                    TextHelpers.SetXamlInlines(adaptiveTextBlock, renderContext, renderArgs, false, xamlTextBlock.Inlines);
                }

                xamlTextBlock.OpticalMarginAlignment = OpticalMarginAlignment.TrimSideBearings;

                if (textStyle == TextStyle.Heading)
                {
                    AutomationProperties.SetHeadingLevel(xamlTextBlock, GetHeadingLevelFromContext(renderContext));
                }

                return xamlTextBlock;
            }
            catch (Exception ex)
            {
                XamlHelpers.ErrForRenderFailedForElement(renderContext, cardElement.ElementTypeString, ex.Message);
                return null;
            }
        }

        private static AutomationHeadingLevel GetHeadingLevelFromContext(AdaptiveRenderContext renderContext) =>
            renderContext.HostConfig.TextBlock.HeadingLevel switch
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
