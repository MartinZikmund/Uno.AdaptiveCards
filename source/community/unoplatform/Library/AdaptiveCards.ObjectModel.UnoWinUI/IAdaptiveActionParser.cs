using System.Collections.Generic;

namespace AdaptiveCards.ObjectModel.WinUI3
{
    internal interface IAdaptiveActionParser
    {
        IAdaptiveActionElement FromJson(
            Windows.Data.Json.JsonObject inputJson,
            AdaptiveElementParserRegistration elementParsers,
            AdaptiveActionParserRegistration actionParsers,
            IList<AdaptiveWarning> warnings);
    }
}
