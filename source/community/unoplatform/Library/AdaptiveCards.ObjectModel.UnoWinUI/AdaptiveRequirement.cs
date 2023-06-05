namespace AdaptiveCards.ObjectModel.WinUI3;

public class AdaptiveRequirement
{
    public AdaptiveRequirement(string requirementName, string requirementversion) =>
        (Name, Version) = (requirementName, requirementversion);

    public string Version { get; set; }

    public string Name { get; set; }
}
