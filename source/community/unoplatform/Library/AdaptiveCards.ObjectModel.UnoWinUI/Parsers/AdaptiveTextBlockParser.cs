using System.Collections.Generic;
using Windows.Data.Json;

namespace AdaptiveCards.ObjectModel.WinUI3;

internal class AdaptiveTextBlockParser : IAdaptiveElementParser
{
    public AdaptiveTextBlockParser()
    {

    }

    public IAdaptiveCardElement FromJson(
        JsonObject inputJson,
        AdaptiveElementParserRegistration elementParsers,
        AdaptiveActionParserRegistration actionParsers,
        IList<AdaptiveWarning> warnings)
    {
        AdaptiveElementParserRegistration.FromJson<AdaptiveTextBlock, TextBlock, TextBlockParser>(
                   inputJson, elementParsers, actionParsers, warnings)
            .as< winrt::AdaptiveCards::ObjectModel::Xaml_OM::IAdaptiveCardElement > ();
    }
}
