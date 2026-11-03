# Adaptive Cards for Uno Platform

A full C# port of the WinUI 3 Adaptive Cards renderer (`AdaptiveCards.Rendering.WinUI3`) and of the shared C++ object
model it is built on (`AdaptiveCards.ObjectModel.WinUI3`) to [Uno Platform](https://platform.uno). It renders
Adaptive Cards (schema 1.6) on every Uno target: WebAssembly, Desktop (Windows, macOS, Linux via Skia), Android, iOS
and Windows App SDK.

| Package / project | Description |
|---|---|
| `AdaptiveCards.ObjectModel.Uno` | Parser/serializer and object model. Pure `net10.0`, no UI dependency. Port of the shared C++ model (incl. fallback, `requires`, id-collision detection, markdown and `{{DATE()}}`/`{{TIME()}}` processing). |
| `AdaptiveCards.Rendering.Uno` | The renderer. Port of the WinUI 3 renderer (`source/uwp/SharedRenderer`). |
| `AdaptiveCards.Visualizer.Uno` | Sample app: browse the repo's sample cards, edit JSON live, switch host configs, inspect actions/inputs. |
| `AdaptiveCards.ObjectModel.Uno.Tests` | xUnit port of the shared model unit tests plus a parse/round-trip test over all `samples/`. |

Built with the Uno Platform 7.0 SDK (`Uno.Sdk` 7.0.0-dev, see `global.json`) and .NET 10.

## Usage

The API mirrors the C# projection of the WinUI 3 renderer. Code written against
`AdaptiveCards.Rendering.WinUI3` ports by changing namespaces (`...WinUI3` → `...Uno`), with JSON types coming from
`System.Text.Json.Nodes` instead of `Windows.Data.Json`.

```csharp
using AdaptiveCards.ObjectModel.Uno;
using AdaptiveCards.Rendering.Uno;

var renderer = new AdaptiveCardRenderer();

// Optional: host config, custom element/action renderers, resource resolvers, override styles…
var hostConfig = AdaptiveHostConfig.FromJsonString(hostConfigJson);
if (hostConfig.HostConfig is { } config)
{
    renderer.HostConfig = config;
}

RenderedAdaptiveCard rendered = renderer.RenderAdaptiveCardFromJsonString(cardJson);
if (rendered.FrameworkElement is { } element)
{
    rendered.Action += (sender, args) =>
    {
        // args.Action is Action.Submit / Execute / OpenUrl / ShowCard (popup mode) / custom
        var inputs = args.Inputs?.AsJson();
    };
    rendered.MediaClicked += (sender, args) => { /* args.Media */ };
    CardHost.Child = element;
}

foreach (var warning in rendered.Warnings) { /* … */ }
```

### Extensibility

* `AdaptiveElementParserRegistration` / `AdaptiveActionParserRegistration` – custom element and action parsers
  (`IAdaptiveElementParser`, `IAdaptiveActionParser`). Custom elements can derive from `AdaptiveCardElementBase`.
* `renderer.ElementRenderers` / `renderer.ActionRenderers` – replace or add renderers (`IAdaptiveElementRenderer`,
  `IAdaptiveActionRenderer`).
* `renderer.FeatureRegistration` – advertise host features used by `requires`.
* `renderer.ResourceResolvers` – resolve custom URI schemes for images (`IAdaptiveCardResourceResolver`).
* `renderer.OverrideStyles` – XAML style overrides, using the same style keys as the WinUI 3 renderer
  (`Adaptive.TextBlock`, `Adaptive.Action.Positive`, `Adaptive.Input.Text`, …).
* `renderer.ChoicesProvider` – dynamic typeahead for filtered `Input.ChoiceSet`s using `choices.data`
  (`Data.Query`): implement `IAdaptiveChoicesProvider.GetChoicesAsync(AdaptiveChoicesQuery, CancellationToken)` and
  return the choices for `query.Dataset` / `query.QueryText` (`Count`/`Skip` are passed through).

## Building

```bash
cd source/uno
dotnet test AdaptiveCards.ObjectModel.Uno.Tests
dotnet build AdaptiveCards.Rendering.Uno
dotnet run --project AdaptiveCards.Visualizer.Uno -f net10.0-desktop
```

Target frameworks are selected per OS (`Directory.Build.props`): desktop and WebAssembly everywhere, Android and iOS
when not on Linux (or with `-p:EnableMobileTargets=true` when the workloads are installed), Windows App SDK on Windows.
Use `-p:OverrideTargetFrameworks=...` to build a specific subset.

### Tests

* `AdaptiveCards.ObjectModel.Uno.Tests` – ~700 xUnit tests: the shared C++ model unit tests (EverythingBagel,
  fallback, id collisions, requirements, markdown, DATE/TIME, tables, carousels, …) plus parsing and round-tripping
  every card under `samples/`.
* Renderer interaction self-tests run inside the real app on any head – they render cards and drive the generated
  controls (typing, checking, invoking buttons and menu items via automation peers) and assert on the raised events:
  submit/execute input gathering, validation (required, regex, ranges), associated inputs, inline ShowCard,
  ToggleVisibility, OpenUrl, overflow menus, disabled actions, `requires` fallback, custom renderers, select actions,
  inline actions, RTL, host configs and dynamic typeahead. They also render every sample card under `samples/`
  (failing on any renderer exception or unexpected error), play a video from a resource resolver stream and check that
  its WebVTT caption is displayed, and check action sentiment colors and button press feedback.
  `AC_SELFTEST_FILTER=<substring>` runs a subset.

```bash
# Desktop (headless on Linux)
xvfb-run -a dotnet AdaptiveCards.Visualizer.Uno/bin/Debug/net10.0-desktop/AdaptiveCards.Visualizer.Uno.dll --selftest results.txt
# WebAssembly: open the published app with ?selftest – results are written to the browser console, or headless:
node ci/wasm-selftest.js http://localhost:8000/   # needs playwright
# Android / iOS: build with -p:SelfTestOnLaunch=true – results go to the device log (SELFTEST ...)
```

### Screenshots

The desktop head of the visualizer can render all sample cards headlessly:

```bash
xvfb-run -a dotnet AdaptiveCards.Visualizer.Uno/bin/Debug/net10.0-desktop/AdaptiveCards.Visualizer.Uno.dll --screenshots ./out v1.6
```

## Platform status

| Head | Build | Verified at runtime (CI) |
|---|---|---|
| Desktop (Skia: Linux/X11, Windows, macOS) | ✅ CI (Linux, Windows) | ✅ all self-tests incl. all samples and media playback with captions (Linux/X11 headless, Win32) + screenshots of all samples |
| WebAssembly (Skia) | ✅ CI | ✅ all self-tests in headless Chromium |
| Android | ✅ CI | ✅ all self-tests on an Android emulator (API 34) |
| iOS | ✅ CI | ✅ all self-tests on the iOS simulator |
| Windows App SDK | ✅ CI (msbuild) | ✅ all self-tests (unpackaged, self-contained) |

## Differences from the WinUI 3 renderer

The port is faithful to the WinUI 3 implementation; deliberate differences are documented in code comments. Notable ones:

* JSON types are `System.Text.Json.Nodes` (`JsonObject`, `JsonNode`) instead of `Windows.Data.Json`.
* `Carousel` honors `timer`, `autoLoop` and vertical `orientation` (ignored by WinUI 3).
* `ImageSet` supports the 1.6 `style` (`stacked`, `grid`) and `offset` properties (ported from the JS renderer).
* `Input.ChoiceSet` supports dynamic typeahead (`choices.data` / `Data.Query`) through `ChoicesProvider`.
* Actions honor `requires` (fall back when unmet).
* `Media.captionSources` are rendered on every head (WebVTT/SRT overlay on Uno, `TimedTextSource` on Windows App SDK).
* A few shared-model serialization bugs are fixed (e.g. `role` on `Action.OpenUrl`, `captionSources` on `Media`).

Uno Platform gaps discovered during the port are tracked in [`UPSTREAM_GAPS.md`](UPSTREAM_GAPS.md) and filed as
issues in this repository.
