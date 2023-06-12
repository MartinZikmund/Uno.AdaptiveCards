using AdaptiveCards.ObjectModel.WinUI3;
using System;

namespace AdaptiveCards.Rendering.WinUI3;

public class AdaptiveRenderContext
{
    public AdaptiveRenderContext();

    public AdaptiveHostConfig HostConfig { get; }

    AdaptiveFeatureRegistration FeatureRegistration { get; }

    AdaptiveElementRendererRegistration ElementRenderers { get; }

    AdaptiveActionRendererRegistration ActionRenderers { get; }

    AdaptiveCardResourceResolvers ResourceResolvers { get; }

    AdaptiveActionInvoker ActionInvoker { get; }

    AdaptiveMediaEventInvoker MediaEventInvoker { get; }

    Microsoft.UI.Xaml.ResourceDictionary OverrideStyles { get; }
    AdaptiveInputs UserInputs { get; }

    public bool? Rtl { get; set; }

    public TextStyle? TextStyle { get; set; }

    Windows.Foundation.IReference<AdaptiveCards.ObjectModel.WinUI3.HAlignment> HorizontalContentAlignment;

    void AddInputValue(IAdaptiveInputValue inputValue, AdaptiveRenderArgs renderArgs);

    void LinkSubmitActionToCard(AdaptiveCards.ObjectModel.WinUI3.IAdaptiveActionElement submitAction,
                                AdaptiveRenderArgs renderArgs);

    void LinkCardToParent(AdaptiveCards.ObjectModel.WinUI3.AdaptiveCard card, AdaptiveRenderArgs renderArgs);

    IAdaptiveInputValue GetInputValue(AdaptiveCards.ObjectModel.WinUI3.IAdaptiveInputElement inputElement);

    public void AddError(ErrorStatusCode statusCode, string message) => throw new NotImplementedException();

    void AddWarning(AdaptiveCards.ObjectModel.WinUI3.WarningStatusCode statusCode, String message);
}
