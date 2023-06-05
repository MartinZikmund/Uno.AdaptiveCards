using System.Collections.Generic;
using Windows.Data.Json;

namespace AdaptiveCards.ObjectModel.WinUI3;

public interface IAdaptiveCardElement
{
    ElementType ElementType { get; }

    string ElementTypeString { get; }

    HeightType Height { get; set; }

    Spacing Spacing { get; set; }

    bool Separator { get; set; }

    string Id { get; set; }

    bool IsVisible { get; set; }

    FallbackType FallbackType { get; set; }

    IAdaptiveCardElement FallbackContent { get; set; }

    JsonObject AdditionalProperties { get; set; }

    IList<AdaptiveRequirement> Requirements { get; }

    JsonObject ToJson();
}
