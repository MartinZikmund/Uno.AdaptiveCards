using AdaptiveCards.ObjectModel.Uno;
using Xunit;

namespace AdaptiveCards.ObjectModel.Uno.Tests;

/// <summary>Schema 1.6 properties supported by this port beyond the WinUI3 projection.</summary>
public class ExtensionPropertyTests
{
    [Fact]
    public void ImageSetStyleAndOffsetRoundTrip()
    {
        var result = AdaptiveCard.FromJsonString("""
            {"type":"AdaptiveCard","version":"1.6","body":[{"type":"ImageSet","style":"Stacked","offset":-25,
              "images":[{"type":"Image","url":"https://a/1.png"},{"type":"Image","url":"https://a/2.png"}]}]}
            """);
        Assert.Empty(result.Errors);
        var imageSet = Assert.IsType<AdaptiveImageSet>(result.AdaptiveCard.Body[0]);
        Assert.Equal(ImageSetStyle.Stacked, imageSet.Style);
        Assert.Equal(-25, imageSet.Offset);
        Assert.Empty(imageSet.AdditionalProperties);

        var json = result.AdaptiveCard.ToJson()["body"]![0]!;
        Assert.Equal("stacked", json["style"]!.GetValue<string>());
        Assert.Equal(-25, json["offset"]!.GetValue<double>());
    }

    [Fact]
    public void ImageSetGridStyleParses()
    {
        var result = AdaptiveCard.FromJsonString("""
            {"type":"AdaptiveCard","version":"1.6","body":[{"type":"ImageSet","style":"grid","images":[{"type":"Image","url":"https://a/1.png"}]}]}
            """);
        Assert.Equal(ImageSetStyle.Grid, ((AdaptiveImageSet)result.AdaptiveCard.Body[0]).Style);
    }

    [Fact]
    public void DataQueryCountAndSkipRoundTrip()
    {
        var result = AdaptiveCard.FromJsonString("""
            {"type":"AdaptiveCard","version":"1.6","body":[{"type":"Input.ChoiceSet","id":"c","style":"filtered",
              "choices.data":{"type":"Data.Query","dataset":"people","count":5,"skip":10}}]}
            """);
        Assert.Empty(result.Errors);
        var choiceSet = Assert.IsType<AdaptiveChoiceSetInput>(result.AdaptiveCard.Body[0]);
        Assert.Equal("people", choiceSet.ChoicesData.Dataset);
        Assert.Equal(5, choiceSet.ChoicesData.Count);
        Assert.Equal(10, choiceSet.ChoicesData.Skip);

        var data = result.AdaptiveCard.ToJson()["body"]![0]!["choices.data"]!;
        Assert.Equal(5, data["count"]!.GetValue<int>());
        Assert.Equal(10, data["skip"]!.GetValue<int>());
    }
}
