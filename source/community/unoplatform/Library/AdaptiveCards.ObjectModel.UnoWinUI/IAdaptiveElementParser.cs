using System.Collections.Generic;

namespace AdaptiveCards.ObjectModel.WinUI3;

internal interface IAdaptiveElementParser
{
    IAdaptiveCardElement FromJson(
        Windows.Data.Json.JsonObject inputJson,
        AdaptiveElementParserRegistration elementParsers,
        AdaptiveActionParserRegistration actionParsers,
        IList<AdaptiveWarning> warnings);
}
