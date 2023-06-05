using AdaptiveCards.ObjectModel.WinUI3;

namespace AdaptiveCards.Rendering.WinUI3;

public class AdaptiveActionEventArgs
{
    internal AdaptiveActionEventArgs(
        IAdaptiveActionElement action,
        AdaptiveInputs inputs) =>
        (Action, Inputs) = (action, inputs);

    public IAdaptiveActionElement Action { get; }

    public AdaptiveInputs Inputs { get; }
}
