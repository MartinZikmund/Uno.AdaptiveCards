using System;
using System.Collections.Generic;

namespace AdaptiveCards.ObjectModel.WinUI3;

public class AdaptiveCardParseResult
{
    public AdaptiveCardParseResult()
    {
    }

    internal AdaptiveCardParseResult(
        AdaptiveCard adaptiveCard,
        IList<AdaptiveError> errors,
        IList<AdaptiveWarning> warnings)
    {
        AdaptiveCard = adaptiveCard;
        Errors = errors;
        Warnings = warnings;
    }

    public AdaptiveCard? AdaptiveCard { get; }

    public IList<AdaptiveError> Errors { get; } = Array.Empty<AdaptiveError>();

    public IList<AdaptiveWarning> Warnings { get; } = Array.Empty<AdaptiveWarning>();
}
