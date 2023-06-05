// TODO

using AdaptiveCards.ObjectModel.WinUI3;
using System;
using Windows.Data.Json;
using Windows.Foundation.Collections;
using static AdaptiveCards.Rendering.WinUI3.Util;

namespace AdaptiveCards.Rendering.WinUI3;

public class AdaptiveInputs
{
    public AdaptiveInputs()
    {
    }

    public JsonObject AsJson() => StringToJsonObject(GetInputItemsAsJsonString());

    public ValueSet AsValueSet() => throw new NotImplementedException();

    public bool ValidateInputs(IAdaptiveActionElement submitAction) => throw new NotImplementedException();
}
