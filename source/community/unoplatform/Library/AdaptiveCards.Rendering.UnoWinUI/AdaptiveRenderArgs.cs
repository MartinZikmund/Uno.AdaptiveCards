using AdaptiveCards.ObjectModel.WinUI3;

namespace AdaptiveCards.Rendering.WinUI3;

public class AdaptiveRenderArgs
{
    public AdaptiveRenderArgs() { }

    public ContainerStyle ContainerStyle { get; set; }

    public object ParentElement { get; set; }

    public bool IsInShowCard { get; set; }

    public bool AllowAboveTitleIconPlacement { get; set; }

    public bool AncestorHasFallback { get; set; }

    public bool AddContainerPadding { get; set; }

    public AdaptiveCard ParentCard { get; set; }
}
