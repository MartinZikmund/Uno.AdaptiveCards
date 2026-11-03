// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Linq;
using Xunit;

namespace AdaptiveCards.ObjectModel.Uno.Tests;

/// <summary>Schema features not projected by WinUI3 (labelPosition/labelWidth/inputStyle, metadata, refresh.expires, action requires).</summary>
public class Schema16ExtensionTests
{
    private const string Card = """
    {"type":"AdaptiveCard","version":"1.6",
     "metadata":{"webUrl":"https://example.com/card"},
     "refresh":{"action":{"type":"Action.Execute","verb":"refresh"},"userIds":["a"],"expires":"2026-12-31T00:00:00Z"},
     "body":[
       {"type":"Input.Text","id":"name","label":"Name","labelPosition":"inline","labelWidth":40,"inputStyle":"revealOnHover"},
       {"type":"Input.Number","id":"age","label":"Age","labelPosition":"inline","labelWidth":"120px"},
       {"type":"Input.Date","id":"when","label":"When","labelWidth":"bogus"},
       {"type":"Table","firstRowAsHeader":false,"columns":[{"width":1}],"rows":[]}],
     "actions":[{"type":"Action.Submit","title":"Go","requires":{"feature":"1.2"}}]}
    """;

    [Fact]
    public void ParsesSchema16Extensions()
    {
        var result = AdaptiveCard.FromJsonString(Card);
        var card = result.AdaptiveCard;
        Assert.NotNull(card);

        Assert.Equal("https://example.com/card", card.Metadata.WebUrl);
        Assert.Equal("2026-12-31T00:00:00Z", card.Refresh.Expires);

        var name = (AdaptiveTextInput)card.Body[0];
        Assert.Equal(InputLabelPosition.Inline, name.LabelPosition);
        Assert.Equal("40", name.LabelWidth);
        Assert.Equal(InputStyle.RevealOnHover, name.InputStyle);

        var age = (AdaptiveNumberInput)card.Body[1];
        Assert.Equal("120px", age.LabelWidth);
        Assert.True(AdaptiveInputElementBase.TryParseLabelWidth(age.LabelWidth, out var px, out var isPixels) && isPixels && px == 120);

        var when = (AdaptiveDateInput)card.Body[2];
        Assert.Equal(InputLabelPosition.Above, when.LabelPosition);
        Assert.Equal(string.Empty, when.LabelWidth);
        Assert.Contains(result.Warnings, w => w.StatusCode == WarningStatusCode.InvalidValue && w.Message.Contains("labelWidth"));

        Assert.False(((AdaptiveTable)card.Body[3]).FirstRowAsHeaders);

        var submit = card.Actions[0];
        var requirement = Assert.Single(((AdaptiveActionElementBase)submit).Requirements);
        Assert.Equal("feature", requirement.Name);
    }

    [Fact]
    public void RoundTripsSchema16Extensions()
    {
        var json = AdaptiveCard.FromJsonString(Card).AdaptiveCard.ToJson();
        Assert.Equal("https://example.com/card", (string)json["metadata"]!["webUrl"]!);
        Assert.Equal("2026-12-31T00:00:00Z", (string)json["refresh"]!["expires"]!);
        var body = json["body"]!.AsArray();
        Assert.Equal("inline", (string)body[0]!["labelPosition"]!);
        Assert.Equal(40, (double)body[0]!["labelWidth"]!);
        Assert.Equal("revealOnHover", (string)body[0]!["inputStyle"]!);
        Assert.Equal("120px", (string)body[1]!["labelWidth"]!);
        Assert.Null(body[2]!["labelWidth"]);
        Assert.False((bool)body[3]!["firstRowAsHeaders"]!);
        Assert.NotNull(json["actions"]![0]!["requires"]);

        // Round trip is stable.
        var again = AdaptiveCard.FromJsonString(json.ToJsonString()).AdaptiveCard.ToJson();
        Assert.Equal(json.ToJsonString(), again.ToJsonString());
    }
}
