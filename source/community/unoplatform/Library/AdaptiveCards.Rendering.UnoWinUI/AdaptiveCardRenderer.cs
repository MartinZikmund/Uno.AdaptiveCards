using AdaptiveCards.ObjectModel.WinUI3;
using System;

namespace AdaptiveCards.Rendering.WinUI3;

public class AdaptiveCardRenderer
{
    public AdaptiveCardRenderer() { }

    public RenderedAdaptiveCard RenderAdaptiveCard(AdaptiveCard adaptiveCard)
    {
        var renderedCard = new RenderedAdaptiveCard();
        renderedCard.SetOriginatingCard(adaptiveCard);
        renderedCard.SetOriginatingHostConfig(m_hostConfig);

        if (adaptiveCard is not null)
        {
            if (m_explicitDimensions)
            {
                m_xamlBuilder.SetFixedDimensions(m_desiredWidth, m_desiredHeight);
            }

            var renderContext = new AdaptiveRenderContext(
                m_hostConfig,
                m_featureRegistration,
                *m_elementRendererRegistration,
                *m_actionRendererRegistration,
                m_resourceResolvers,
                m_mergedResourceDictionary,
                m_actionSentimentResourceDictionary,
                renderedCard);

            m_xamlBuilder.SetEnableXamlImageHandling(true);
            try
            {
                renderContext.LinkCardToParent(adaptiveCard, null);
                var xamlTreeRoot =
                    ::AdaptiveCards::Rendering::Xaml_Rendering::XamlBuilder::BuildXamlTreeFromAdaptiveCard(adaptiveCard, *renderContext, m_xamlBuilder.get());
                renderedCard.SetFrameworkElement(xamlTreeRoot);
            }
            catch (Exception)
            {
                renderContext.AddError(
                    ErrorStatusCode.RenderFailed,
                    "An unrecoverable error was encountered while rendering the card");
                renderedCard.SetFrameworkElement(null);
            }
        }

        return renderedCard;
    }


    //AdaptiveCardResourceResolvers ResourceResolvers { get; };
    //AdaptiveHostConfig HostConfig;
    //Microsoft.UI.Xaml.ResourceDictionary OverrideStyles;
    //AdaptiveFeatureRegistration FeatureRegistration;

    //Boolean OverflowMaxActions;
    //String OverflowButtonText;
    //String OverflowButtonAccessibilityText;

    //void SetFixedDimensions(UInt32 desiredWidth, UInt32 desiredHeight);
    //void ResetFixedDimensions();
    //RenderedAdaptiveCard RenderAdaptiveCardFromJson(Windows.Data.Json.JsonObject adaptiveCard);
    //    public RenderedAdaptiveCard RenderAdaptiveCardFromJsonString(string adaptiveJson)
    //    {
    //        var adaptiveCardParseResult = AdaptiveCard.FromJsonString(adaptiveJson);
    //        if (adaptiveCardParseResult.AdaptiveCard is { } parsedCard)
    //        {
    //            return RenderAdaptiveCard(parsedCard);
    //        }
    //        else
    //        {
    //            // Replicate parse errors into render errors
    //            throw new NotImplementedException();
    ////            var renderedCard = winrt::make_self<implementation::RenderedAdaptiveCard>();
    ////var renderResultErrors = renderedCard->Errors.get();
    ////            for (auto&& error : adaptiveCardParseResult.Errors())
    ////            {
    ////                renderResultErrors.Append(error);
    ////            }

    ////            return *renderedCard;
    //        }
    //    }

    //AdaptiveElementRendererRegistration ElementRenderers { get; };
    //AdaptiveActionRendererRegistration ActionRenderers { get; };
}
