// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AdaptiveCards.ObjectModel.Uno;
using AdaptiveCards.Rendering.Uno;
using Xunit;
using Xunit.Abstractions;

namespace AdaptiveCards.ObjectModel.Uno.Tests;

/// <summary>
/// Checks that the public API of the port covers the complete WinUI3 projection: every runtimeclass, interface and
/// enum declared in the WinUI3 IDL files (source/uwp) and each of their properties, methods, events and enum values.
/// </summary>
public class ApiParityTests
{
    private readonly ITestOutputHelper _output;

    public ApiParityTests(ITestOutputHelper output) => _output = output;

    internal sealed record IdlType(string Kind, string Name, List<(string Kind, string Name)> Members);

    private static string RepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "source", "uwp")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(new[] { dir!.FullName, "source" }.Concat(parts).ToArray());
    }

    internal static List<IdlType> ParseIdl(string path)
    {
        var text = File.ReadAllText(path);
        text = Regex.Replace(text, @"//[^\n]*", string.Empty);
        text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"\[[^\]]*\]", string.Empty);
        text = Regex.Replace(text, @"declare\s*\{[^}]*\}", string.Empty);
        text = Regex.Replace(text, @"\{\s*get\s*;\s*\}", string.Empty);
        text = Regex.Replace(text, @"\{\s*get\s*;\s*set\s*;\s*\}", string.Empty);

        var declarations = Regex.Matches(text, @"\b(runtimeclass|interface|enum)\s+\w+[^;{]*\{").Count;
        var types = new List<IdlType>();
        foreach (Match m in Regex.Matches(text, @"\b(runtimeclass|interface|enum)\s+(\w+)[^{;]*\{([^{}]*)\}"))
        {
            var kind = m.Groups[1].Value;
            var name = m.Groups[2].Value;
            var body = m.Groups[3].Value;
            var members = new List<(string, string)>();
            if (kind == "enum")
            {
                foreach (var entry in body.Split(','))
                {
                    var value = entry.Split('=')[0].Trim();
                    if (value.Length > 0)
                    {
                        members.Add(("value", value));
                    }
                }
            }
            else
            {
                foreach (var raw in body.Split(';'))
                {
                    var decl = Regex.Replace(raw, @"\s+", " ").Trim();
                    if (decl.Length == 0)
                    {
                        continue;
                    }

                    var method = Regex.Match(decl, @"(\w+)\s*\(");
                    if (decl.StartsWith("event "))
                    {
                        members.Add(("event", decl.Split(' ')[^1]));
                    }
                    else if (method.Success)
                    {
                        if (method.Groups[1].Value != name)
                        {
                            members.Add(("method", method.Groups[1].Value));
                        }
                    }
                    else
                    {
                        members.Add(("property", decl.Split(' ')[^1]));
                    }
                }
            }

            types.Add(new IdlType(kind, name, members));
        }

        // Every declaration with a body must have been parsed (guards against the parser silently skipping types).
        Assert.Equal(declarations, types.Count);
        return types;
    }

    private static List<string> FindMissing(IEnumerable<IdlType> idlTypes, Assembly assembly, string ns)
    {
        var missing = new List<string>();
        foreach (var idl in idlTypes)
        {
            var type = assembly.GetType(ns + "." + idl.Name);
            if (type is null || !type.IsPublic)
            {
                missing.Add($"{idl.Kind} {idl.Name}");
                continue;
            }

            if (idl.Kind == "enum")
            {
                Assert.True(type.IsEnum, $"{idl.Name} should be an enum");
                var names = Enum.GetNames(type);
                missing.AddRange(idl.Members.Where(m => !names.Contains(m.Name)).Select(m => $"{idl.Name}.{m.Name} (enum value)"));
                continue;
            }

            var all = type.IsInterface
                ? new[] { type }.Concat(type.GetInterfaces()).ToArray()
                : new[] { type };
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            foreach (var (kind, name) in idl.Members)
            {
                bool found = kind switch
                {
                    "property" => all.Any(t => t.GetProperty(name, flags) != null),
                    "method" => all.Any(t => t.GetMethods(flags).Any(mi => mi.Name == name)),
                    "event" => all.Any(t => t.GetEvent(name, flags) != null),
                    _ => false,
                };
                if (!found)
                {
                    missing.Add($"{idl.Name}.{name} ({kind})");
                }
            }
        }

        return missing;
    }

    [Fact]
    public void ObjectModelCoversWinUI3Projection()
    {
        var idl = ParseIdl(RepoFile("uwp", "AdaptiveCardsObjectModel", "idl", "AdaptiveCards.ObjectModel.Uwp.idl"));
        Assert.True(idl.Count > 100, $"parsed only {idl.Count} IDL types");
        var missing = FindMissing(idl, typeof(AdaptiveCard).Assembly, "AdaptiveCards.ObjectModel.Uno");
        _output.WriteLine($"{idl.Count} types, {idl.Sum(t => t.Members.Count)} members checked");
        Assert.True(missing.Count == 0, "Missing from the object model:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void RendererCoversWinUI3Projection()
    {
        var idl = ParseIdl(RepoFile("uwp", "winui3", "Renderer", "idl", "AdaptiveCards.Rendering.WinUI3.idl"));
        Assert.True(idl.Count > 30, $"parsed only {idl.Count} IDL types");
        var missing = FindMissing(idl, typeof(AdaptiveCardRenderer).Assembly, "AdaptiveCards.Rendering.Uno");
        _output.WriteLine($"{idl.Count} types, {idl.Sum(t => t.Members.Count)} members checked");
        Assert.True(missing.Count == 0, "Missing from the renderer:\n" + string.Join("\n", missing));
    }
}
