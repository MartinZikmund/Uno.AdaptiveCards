// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace AdaptiveCards.ObjectModel.Uno.Tests;

/// <summary>
/// Checks that every type and property of the Adaptive Cards schema (schemas/1.6.0/adaptive-card.json, including
/// properties inherited through allOf) is modeled by the object model, using the WinUI3 projection's names.
/// </summary>
public class SchemaCoverageTests
{
    private readonly ITestOutputHelper _output;

    public SchemaCoverageTests(ITestOutputHelper output) => _output = output;

    private static readonly Dictionary<string, string> s_typeNames = new()
    {
        ["Action.Execute"] = "AdaptiveExecuteAction",
        ["Action.OpenUrl"] = "AdaptiveOpenUrlAction",
        ["Action.ShowCard"] = "AdaptiveShowCardAction",
        ["Action.Submit"] = "AdaptiveSubmitAction",
        ["Action.ToggleVisibility"] = "AdaptiveToggleVisibilityAction",
        ["TargetElement"] = "AdaptiveToggleVisibilityTarget",
        ["Input.Choice"] = "AdaptiveChoiceInput",
        ["Input.ChoiceSet"] = "AdaptiveChoiceSetInput",
        ["Input.Date"] = "AdaptiveDateInput",
        ["Input.Number"] = "AdaptiveNumberInput",
        ["Input.Text"] = "AdaptiveTextInput",
        ["Input.Time"] = "AdaptiveTimeInput",
        ["Input.Toggle"] = "AdaptiveToggleInput",
        ["Data.Query"] = "AdaptiveChoicesData",
        ["AdaptiveCard"] = "AdaptiveCard",
    };

    // Schema property names the WinUI3 projection exposes under a different name.
    private static readonly Dictionary<string, string> s_propertyNames = new()
    {
        ["fallback"] = "FallbackType",
        ["requires"] = "Requirements",
        ["data"] = "DataJson",
        ["choices.data"] = "ChoicesData",
        ["rtl?"] = "Rtl",
        ["firstRowAsHeader"] = "FirstRowAsHeaders",
        ["$schema"] = "",
        ["type"] = "",
    };

    // Type-specific renames in the WinUI3 projection.
    private static readonly Dictionary<string, string> s_typePropertyNames = new()
    {
        ["Image.width"] = "PixelWidth",
        ["AdaptiveCard.lang"] = "Language",
        ["Input.Text.style"] = "TextInputStyle",
        ["Input.ChoiceSet.style"] = "ChoiceSetStyle",
    };

    private static JsonObject Definitions()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "schemas", "1.6.0", "adaptive-card.json")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var schema = JsonNode.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "schemas", "1.6.0", "adaptive-card.json")))!;
        return schema["definitions"]!.AsObject();
    }

    private static IEnumerable<string> Properties(JsonObject definitions, string name)
    {
        var definition = definitions[name]!.AsObject();
        var names = new List<string>();
        if (definition["properties"] is JsonObject properties)
        {
            names.AddRange(properties.Select(p => p.Key));
        }

        if (definition["allOf"] is JsonArray allOf)
        {
            foreach (var reference in allOf.OfType<JsonObject>())
            {
                var target = reference["$ref"]?.GetValue<string>().Split('/').Last();
                if (target != null && definitions.ContainsKey(target))
                {
                    names.AddRange(Properties(definitions, target));
                }
            }
        }

        return names.Distinct();
    }

    [Fact]
    public void EverySchemaTypeAndPropertyIsModeled()
    {
        var definitions = Definitions();
        var assembly = typeof(AdaptiveCard).Assembly;
        var missing = new List<string>();
        int checkedTypes = 0, checkedProperties = 0;
        foreach (var (name, node) in definitions)
        {
            // Concrete schema types are the ones with a "type" discriminator or own properties (not enums/unions).
            var typeEnum = node?["properties"]?["type"];
            if (typeEnum is null || name.StartsWith("Extendable.", StringComparison.Ordinal))
            {
                continue;
            }

            var className = s_typeNames.TryGetValue(name, out var mapped) ? mapped : "Adaptive" + name;
            var type = assembly.GetType("AdaptiveCards.ObjectModel.Uno." + className);
            if (type is null)
            {
                missing.Add($"{name}: no class {className}");
                continue;
            }

            checkedTypes++;
            foreach (var property in Properties(definitions, name))
            {
                var propertyName = s_typePropertyNames.TryGetValue(name + "." + property, out var typeRenamed)
                    ? typeRenamed
                    : s_propertyNames.TryGetValue(property, out var renamed)
                    ? renamed
                    : char.ToUpperInvariant(property[0]) + property.Substring(1);
                if (propertyName.Length == 0)
                {
                    continue;
                }

                checkedProperties++;
                if (type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) is null)
                {
                    missing.Add($"{name}.{property}: no property {className}.{propertyName}");
                }
            }
        }

        _output.WriteLine($"{checkedTypes} schema types, {checkedProperties} properties checked");
        Assert.True(checkedTypes >= 30, $"only {checkedTypes} schema types checked");
        Assert.True(missing.Count == 0, "Schema features missing from the object model:\n" + string.Join("\n", missing));
    }
}
