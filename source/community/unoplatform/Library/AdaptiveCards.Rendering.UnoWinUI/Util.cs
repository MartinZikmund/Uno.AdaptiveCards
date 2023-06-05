using Windows.Data.Json;

namespace AdaptiveCards.Rendering.WinUI3;

internal static class Util
{
    public static JsonObject StringToJsonObject(string inputString)
    {
        return HStringToJsonObject(inputString);
    }

    private static JsonObject HStringToJsonObject(string inputHString)
    {
        JsonObject? obj = null;

        if (!JsonObject.TryParse(inputHString, out var obj)
        {
            obj = new JsonObject();
        }

        return obj;
    }
}
