using Windows.Data.Json;

namespace AdaptiveCards.ObjectModel.WinUI3;

public interface IAdaptiveActionElement
{
    ActionType ActionType { get; }

    string ActionTypeString { get; }

    JsonObject AdditionalProperties { get; set; }

    IAdaptiveActionElement FallbackContent { get; set; }

    FallbackType FallbackType { get; set; }

    string IconUrl { get; set; }

    string Id { get; set; }

    bool IsEnabled { get; set; }

    ActionMode Mode { get; set; }

    ActionRole Role { get; set; }

    string Style { get; set; }

    string Title { get; set; }

    string Tooltip { get; set; }

    JsonObject ToJson();
}
