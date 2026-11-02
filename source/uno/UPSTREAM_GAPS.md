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
