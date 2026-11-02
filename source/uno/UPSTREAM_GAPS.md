# Uno Platform gaps found while porting the WinUI 3 Adaptive Cards renderer

Each gap is filed as an issue in this repository (label `uno-upstream`) so it can be moved to unoplatform/uno.
Versions: Uno.Sdk 7.0.0-dev.130 / Uno.WinUI 7.0.0-dev.2538.

| Issue | API(s) | Used for | Workaround |
|---|---|---|---|
| [#1](https://github.com/MartinZikmund/Uno.AdaptiveCards/issues/1) | `TimedTextSource`, `MediaSource.ExternalTimedTextSources`, `MediaPlaybackItem.TimedMetadataTracks` | `Media.captionSources` | captions only on Windows App SDK |
| [#2](https://github.com/MartinZikmund/Uno.AdaptiveCards/issues/2) | `MediaSource.CreateFromStream` | media via custom resource resolvers | http(s) sources use `CreateFromUri` |
| [#3](https://github.com/MartinZikmund/Uno.AdaptiveCards/issues/3) | `DataReader.LoadAsync/DetachStream`, `DataWriter.DetachStream` | data: URIs / resolver streams for images | `MemoryStream.AsRandomAccessStream()` |
| [#4](https://github.com/MartinZikmund/Uno.AdaptiveCards/issues/4) | `TextBlock.OpticalMarginAlignment` | text alignment polish | none (minor) |
| [#5](https://github.com/MartinZikmund/Uno.AdaptiveCards/issues/5) | `PointerUpThemeAnimation`, `PointerDownThemeAnimation` | button press animations in default styles | none (no-op) |
| [#6](https://github.com/MartinZikmund/Uno.AdaptiveCards/issues/6) | `VariableSizedWrapGrid` ("Occupancy map cannot be null", 0x0 with late-loading images) | ImageSet layout | `ImageSetWrapPanel` on Uno |
| [#7](https://github.com/MartinZikmund/Uno.AdaptiveCards/issues/7) | `unoapp` template with a `.Uno` name segment | Visualizer app | `global::Uno.UI...` |
| [#8](https://github.com/MartinZikmund/Uno.AdaptiveCards/issues/8) | Missing `FontFamily` falls back to a monospace font on WebAssembly | default host config font ("Segoe UI") | map to `XamlAutoFontFamily` on non-Windows |

Additional behavioral differences handled in the port (not necessarily bugs):

- Uno does not load the source of a `Collapsed` `Image`, so `ImageOpened` never fires for images the WinUI renderer
  hides until their natural size is known; the port hides them with `Opacity` instead.
- Measuring a live `FlipView` page with a different constraint from `LayoutUpdated` (as the WinUI renderer does to size
  carousels) causes a `LayoutCycleException` on Uno; the port uses `CarouselPageHost` to report natural page heights.
- `FlipView` does not page with a vertical `ItemsPanel` on Uno; vertical carousels (an extension over WinUI3) use a
  selected-page template.
| [#9](https://github.com/MartinZikmund/Uno.AdaptiveCards/issues/9) | Vertical `PipsPager` realizes a single pip | vertical carousel page indicator | `VerticalPipsIndicator` |

Also noted: `PointerPointProperties.MouseWheelDelta` is flagged not implemented on Android (used only for mouse-wheel
paging of vertical carousels; touch swipes work).
