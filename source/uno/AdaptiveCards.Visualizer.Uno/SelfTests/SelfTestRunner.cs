using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using AdaptiveCards.Rendering.Uno;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;

namespace AdaptiveCards.Visualizer.Uno.SelfTests;

/// <summary>
/// Interaction tests for the renderer, run inside the real Uno app (any head):
///   AdaptiveCards.Visualizer.Uno --selftest &lt;outputFile&gt;
/// Each test renders a card into the page, drives the generated controls and checks the raised events/inputs.
/// </summary>
public static class SelfTestRunner
{
    private sealed class TestFailure(string message) : Exception(message);

    public static bool TryStart(MainPage page)
    {
#if SELFTEST_ON_LAUNCH
        // CI builds for device/emulator heads (Android, iOS): run on launch and report to the console/logcat.
        _ = RunToConsoleAsync(page);
        return true;
#elif __WASM__
        // In the browser: http://host/?selftest . Results go to the browser console.
        var search = global::Uno.Foundation.WebAssemblyRuntime.InvokeJS("window.location.search") ?? string.Empty;
        if (!search.Contains("selftest", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _ = RunToConsoleAsync(page);
        return true;
#else
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--selftest");
        if (index < 0)
        {
            return false;
        }

        var output = index + 1 < args.Length ? args[index + 1] : "selftest-results.txt";
        _ = RunAllAsync(page, output, exit: true);
        return true;
#endif
    }

#if __WASM__ || SELFTEST_ON_LAUNCH
    private static async Task RunToConsoleAsync(MainPage page)
    {
        await Task.Delay(1000);
        var (_, failed, report) = await RunAllAsync(page, null, exit: false);
        foreach (var line in report.Split(Environment.NewLine))
        {
            Console.WriteLine("SELFTEST " + line);
        }

        Console.WriteLine(failed == 0 ? "SELFTEST DONE OK" : "SELFTEST DONE FAILED");
    }
#endif

    public static async Task<(int Passed, int Failed, string Report)> RunAllAsync(MainPage page, string? outputFile, bool exit)
    {
        var lines = new List<string>();
        int passed = 0, failed = 0;
        var filter = Environment.GetEnvironmentVariable("AC_SELFTEST_FILTER");
        foreach (var (name, test) in Tests)
        {
            if (!string.IsNullOrEmpty(filter) && !name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Console.WriteLine("SELFTEST RUN " + name);
            try
            {
                await test(page);
                passed++;
                lines.Add("PASS " + name);
            }
            catch (Exception ex)
            {
                failed++;
                lines.Add("FAIL " + name + ": " + (ex is TestFailure ? ex.Message : ex.ToString()));
            }
        }

        lines.Add($"{passed} passed, {failed} failed");
        var report = string.Join(Environment.NewLine, lines);
        if (outputFile is not null)
        {
            await File.WriteAllTextAsync(outputFile, report);
        }

        if (exit)
        {
            Environment.Exit(failed == 0 ? 0 : 1);
        }

        return (passed, failed, report);
    }

    private static readonly (string Name, Func<MainPage, Task> Test)[] Tests =
    [
        ("Submit gathers all input values", SubmitGathersInputs),
        ("Required input blocks submit and shows error", RequiredInputBlocksSubmit),
        ("Regex and number range validation", ValidationRules),
        ("AssociatedInputs none sends no inputs", AssociatedInputsNone),
        ("Execute action carries verb and data", ExecuteAction),
        ("Inline ShowCard toggles and gathers parent inputs", InlineShowCard),
        ("ToggleVisibility toggles target elements", ToggleVisibility),
        ("OpenUrl raises action with url", OpenUrl),
        ("Overflow menu for secondary actions", OverflowActions),
        ("Disabled action is not clickable", DisabledAction),
        ("Requires fallback renders fallback content", RequiresFallback),
        ("Custom element renderer", CustomElementRenderer),
        ("SelectAction on container", SelectAction),
        ("Text input inline action", InlineAction),
        ("Rtl sets flow direction", Rtl),
        ("All sample host configs parse", HostConfigsParse),
        ("Unknown element type falls back with warning", UnknownElementFallback),
        ("Dynamic typeahead (Data.Query) choices", DynamicTypeahead),
        ("Media raises MediaClicked when inline playback is off", MediaClicked),
        ("Media inline playback shows the player", MediaInline),
        ("All sample cards render without errors", AllSamplesRender),
        ("Action sentiment styles use host config colors", ActionSentiment),
        ("Pressed buttons give visual feedback", PressFeedback),
        ("Media plays from a resource resolver stream with captions", MediaPlaybackWithCaptions),
    ];

    // ---------- helpers ----------

    private static async Task<RenderedAdaptiveCard> RenderAsync(MainPage page, string json)
    {
        var rendered = page.RenderCard(json) ?? throw new TestFailure("RenderCard returned null");
        if (rendered.FrameworkElement is null)
        {
            throw new TestFailure("No FrameworkElement; errors: " + string.Join("; ", rendered.Errors));
        }

        await Task.Delay(250);
        return rendered;
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t)
            {
                yield return t;
            }

            foreach (var d in Find<T>(child))
            {
                yield return d;
            }
        }
    }

    private static string TextOf(object? content) => content switch
    {
        string s => s,
        TextBlock tb => tb.Text,
        DependencyObject d => string.Join(" ", Find<TextBlock>(d).Select(t => t.Text)),
        _ => string.Empty,
    };

    private static Button FindButton(RenderedAdaptiveCard card, string text) =>
        Find<Button>(card.FrameworkElement!).FirstOrDefault(b => TextOf(b.Content).Contains(text, StringComparison.Ordinal))
        ?? throw new TestFailure($"Button '{text}' not found");

    private static void Click(Button button)
    {
        if (!button.IsEnabled)
        {
            throw new TestFailure("Button is disabled");
        }

        new ButtonAutomationPeer(button).Invoke();
    }

    private static async Task<AdaptiveActionEventArgs?> ClickAndCaptureAsync(RenderedAdaptiveCard card, string buttonText)
    {
        AdaptiveActionEventArgs? captured = null;
        void Handler(RenderedAdaptiveCard s, AdaptiveActionEventArgs e) => captured = e;
        card.Action += Handler;
        try
        {
            Click(FindButton(card, buttonText));
            await Task.Delay(100);
        }
        finally
        {
            card.Action -= Handler;
        }

        return captured;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new TestFailure(message);
        }
    }

    private static void AssertEqual(string? expected, string? actual, string what) =>
        Assert(expected == actual, $"{what}: expected '{expected}', got '{actual}'");

    private static bool IsShown(UIElement element)
    {
        for (DependencyObject? cur = element; cur is not null; cur = VisualTreeHelper.GetParent(cur))
        {
            if (cur is UIElement u && u.Visibility != Visibility.Visible)
            {
                return false;
            }
        }

        return true;
    }

    // ---------- tests ----------

    private static async Task SubmitGathersInputs(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[
          {"type":"Input.Text","id":"name","value":"initial"},
          {"type":"Input.Number","id":"qty","value":3},
          {"type":"Input.Toggle","id":"agree","title":"Agree","value":"yes","valueOn":"yes","valueOff":"no"},
          {"type":"Input.ChoiceSet","id":"color","value":"g","choices":[{"title":"Red","value":"r"},{"title":"Green","value":"g"}]},
          {"type":"Input.ChoiceSet","id":"multi","isMultiSelect":true,"style":"expanded","value":"a,c","choices":[{"title":"A","value":"a"},{"title":"B","value":"b"},{"title":"C","value":"c"}]},
          {"type":"Input.Date","id":"date","value":"2024-02-29"},
          {"type":"Input.Time","id":"time","value":"13:45"}
        ],"actions":[{"type":"Action.Submit","title":"Send","data":{"x":1}}]}
        """);

        var textBox = Find<TextBox>(card.FrameworkElement!).First(t => t.Text == "initial");
        textBox.Text = "changed";
        var multiBoxes = Find<CheckBox>(card.FrameworkElement!).Where(c => TextOf(c.Content) is "A" or "B" or "C").ToList();
        Assert(multiBoxes.Count == 3, "expected 3 multi-select check boxes, got " + multiBoxes.Count);
        multiBoxes.First(c => TextOf(c.Content) == "B").IsChecked = true;

        var args = await ClickAndCaptureAsync(card, "Send") ?? throw new TestFailure("Action event not raised");
        var inputs = args.Inputs.AsJson();
        AssertEqual("changed", inputs["name"]?.GetValue<string>(), "name");
        AssertEqual("3", inputs["qty"]?.GetValue<string>(), "qty");
        AssertEqual("yes", inputs["agree"]?.GetValue<string>(), "agree");
        AssertEqual("g", inputs["color"]?.GetValue<string>(), "color");
        AssertEqual("a,b,c", inputs["multi"]?.GetValue<string>(), "multi");
        AssertEqual("2024-02-29", inputs["date"]?.GetValue<string>(), "date");
        AssertEqual("13:45", inputs["time"]?.GetValue<string>(), "time");
        Assert(((AdaptiveSubmitAction)args.Action).DataJson?["x"]?.GetValue<int>() == 1, "submit data");
    }

    private static async Task RequiredInputBlocksSubmit(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[
          {"type":"Input.Text","id":"req","label":"Required","isRequired":true,"errorMessage":"Please fill this"}
        ],"actions":[{"type":"Action.Submit","title":"Send"}]}
        """);

        var args = await ClickAndCaptureAsync(card, "Send");
        Assert(args is null, "submit must be blocked while a required input is empty");
        var error = Find<TextBlock>(card.FrameworkElement!).FirstOrDefault(t => t.Text == "Please fill this");
        Assert(error is not null && IsShown(error), "error message should be visible");

        Find<TextBox>(card.FrameworkElement!).First().Text = "now filled";
        args = await ClickAndCaptureAsync(card, "Send");
        Assert(args is not null, "submit should succeed once the input is filled");
        Assert(!IsShown(error!), "error message should be hidden after successful validation");
    }

    private static async Task ValidationRules(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[
          {"type":"Input.Text","id":"zip","regex":"^[0-9]{5}$","value":"abc","errorMessage":"bad zip"},
          {"type":"Input.Number","id":"n","min":1,"max":5,"value":9,"errorMessage":"out of range"}
        ],"actions":[{"type":"Action.Submit","title":"Send"}]}
        """);

        Assert(await ClickAndCaptureAsync(card, "Send") is null, "invalid regex/range must block submit");
        var boxes = Find<TextBox>(card.FrameworkElement!).ToList();
        boxes.First(b => b.Text == "abc").Text = "12345";
        boxes.First(b => b.Text == "9").Text = "4";
        var args = await ClickAndCaptureAsync(card, "Send");
        Assert(args is not null, "valid values should submit");
        AssertEqual("12345", args!.Inputs.AsJson()["zip"]?.GetValue<string>(), "zip");
    }

    private static async Task AssociatedInputsNone(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[{"type":"Input.Text","id":"req","isRequired":true,"errorMessage":"x"}],
         "actions":[{"type":"Action.Submit","title":"Cancel","associatedInputs":"none"}]}
        """);
        var args = await ClickAndCaptureAsync(card, "Cancel") ?? throw new TestFailure("Action event not raised");
        Assert(args.Inputs.AsJson().Count == 0, "no inputs expected");
    }

    private static async Task ExecuteAction(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[{"type":"Input.Text","id":"t","value":"v"}],
         "actions":[{"type":"Action.Execute","title":"Run","verb":"doIt","data":{"k":"v"}}]}
        """);
        var args = await ClickAndCaptureAsync(card, "Run") ?? throw new TestFailure("Action event not raised");
        var execute = (AdaptiveExecuteAction)args.Action;
        AssertEqual("doIt", execute.Verb, "verb");
        AssertEqual("v", args.Inputs.AsJson()["t"]?.GetValue<string>(), "input");
    }

    private static async Task InlineShowCard(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[{"type":"Input.Text","id":"outer","value":"o"}],
         "actions":[{"type":"Action.ShowCard","title":"More","card":{"type":"AdaptiveCard","body":[
            {"type":"Input.Text","id":"inner","value":"i"}],"actions":[{"type":"Action.Submit","title":"Inner submit"}]}}]}
        """);

        var innerSubmit = FindButton(card, "Inner submit");
        Assert(!IsShown(innerSubmit), "show card should start collapsed");
        Assert(await ClickAndCaptureAsync(card, "More") is null, "inline ShowCard must not raise Action");
        await Task.Delay(100);
        Assert(IsShown(innerSubmit), "show card should be visible after click");

        var args = await ClickAndCaptureAsync(card, "Inner submit") ?? throw new TestFailure("inner submit not raised");
        var inputs = args.Inputs.AsJson();
        AssertEqual("i", inputs["inner"]?.GetValue<string>(), "inner input");
        AssertEqual("o", inputs["outer"]?.GetValue<string>(), "parent card input");

        await ClickAndCaptureAsync(card, "More");
        Assert(!IsShown(innerSubmit), "show card should collapse on second click");
    }

    private static async Task ToggleVisibility(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[
          {"type":"TextBlock","id":"a","text":"Alpha"},
          {"type":"TextBlock","id":"b","text":"Beta","isVisible":false}],
         "actions":[{"type":"Action.ToggleVisibility","title":"Toggle","targetElements":["a",{"elementId":"b","isVisible":true}]}]}
        """);

        TextBlock Get(string text) => Find<TextBlock>(card.FrameworkElement!).First(t => t.Text == text);
        Assert(IsShown(Get("Alpha")) && !IsShown(Get("Beta")), "initial visibility");
        Assert(await ClickAndCaptureAsync(card, "Toggle") is null, "ToggleVisibility must not raise Action");
        Assert(!IsShown(Get("Alpha")) && IsShown(Get("Beta")), "visibility after toggle");
        await ClickAndCaptureAsync(card, "Toggle");
        Assert(IsShown(Get("Alpha")) && IsShown(Get("Beta")), "visibility after second toggle");
    }

    private static async Task OpenUrl(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[],"actions":[{"type":"Action.OpenUrl","title":"Go","url":"https://example.com/x"}]}
        """);
        var args = await ClickAndCaptureAsync(card, "Go") ?? throw new TestFailure("Action event not raised");
        AssertEqual("https://example.com/x", ((AdaptiveOpenUrlAction)args.Action).Url?.ToString(), "url");
    }

    private static async Task OverflowActions(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[],"actions":[
          {"type":"Action.Submit","title":"Primary"},
          {"type":"Action.Submit","title":"Hidden1","mode":"secondary"},
          {"type":"Action.Submit","title":"Hidden2","mode":"secondary"}]}
        """);
        FindButton(card, "Primary");
        var overflow = FindButton(card, "...");
        Assert(overflow.Flyout is MenuFlyout { Items.Count: 2 }, "overflow flyout with 2 items expected");
        Assert(!Find<Button>(card.FrameworkElement!).Any(b => TextOf(b.Content).Contains("Hidden1")), "secondary actions must not be buttons");

        AdaptiveActionEventArgs? captured = null;
        card.Action += (s, e) => captured = e;
        var item = (MenuFlyoutItem)((MenuFlyout)overflow.Flyout).Items[1];
        new MenuFlyoutItemAutomationPeer(item).Invoke();
        await Task.Delay(100);
        AssertEqual("Hidden2", captured?.Action.Title, "overflow item action");
    }

    private static async Task DisabledAction(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[],"actions":[{"type":"Action.Submit","title":"Nope","isEnabled":false}]}
        """);
        Assert(!FindButton(card, "Nope").IsEnabled, "button should be disabled");
    }

    private static async Task RequiresFallback(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[
          {"type":"TextBlock","text":"Needs feature","requires":{"superFeature":"2.0"},"fallback":{"type":"TextBlock","text":"Fallback shown"}},
          {"type":"TextBlock","text":"Dropped","requires":{"superFeature":"1.0"},"fallback":"drop"}]}
        """);
        var texts = Find<TextBlock>(card.FrameworkElement!).Select(t => t.Text).ToList();
        Assert(texts.Contains("Fallback shown") && !texts.Contains("Needs feature") && !texts.Contains("Dropped"),
            "fallback content expected, got: " + string.Join("|", texts));

        page.Renderer.FeatureRegistration.Set("superFeature", "2.0");
        try
        {
            card = await RenderAsync(page, page.CurrentJson);
            texts = Find<TextBlock>(card.FrameworkElement!).Select(t => t.Text).ToList();
            Assert(texts.Contains("Needs feature") && texts.Contains("Dropped"), "elements should render once feature is registered");
        }
        finally
        {
            page.Renderer.FeatureRegistration.Remove("superFeature");
        }
    }

    private sealed class StarRenderer : IAdaptiveElementRenderer
    {
        public UIElement Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs) =>
            new TextBlock { Text = "★ " + ((AdaptiveTextBlock)element).Text };
    }

    private static async Task CustomElementRenderer(MainPage page)
    {
        var original = page.Renderer.ElementRenderers.Get("TextBlock");
        page.Renderer.ElementRenderers.Set("TextBlock", new StarRenderer());
        try
        {
            var card = await RenderAsync(page, """{"type":"AdaptiveCard","version":"1.5","body":[{"type":"TextBlock","text":"hi"}]}""");
            Assert(Find<TextBlock>(card.FrameworkElement!).Any(t => t.Text == "★ hi"), "custom renderer output expected");
        }
        finally
        {
            page.Renderer.ElementRenderers.Set("TextBlock", original);
        }
    }

    private static async Task SelectAction(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[{"type":"Container","items":[{"type":"TextBlock","text":"Tap me"}],
          "selectAction":{"type":"Action.Submit","title":"select","data":{"s":1}}}]}
        """);
        var args = await ClickAndCaptureAsync(card, "Tap me") ?? throw new TestFailure("selectAction not raised");
        AssertEqual("select", args.Action.Title, "select action");
    }

    private static async Task InlineAction(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[{"type":"Input.Text","id":"msg","value":"hello",
          "inlineAction":{"type":"Action.Submit","title":"Reply"}}]}
        """);
        var args = await ClickAndCaptureAsync(card, "Reply") ?? throw new TestFailure("inline action not raised");
        AssertEqual("hello", args.Inputs.AsJson()["msg"]?.GetValue<string>(), "inline action inputs");
    }

    private static async Task Rtl(MainPage page)
    {
        var card = await RenderAsync(page, """{"type":"AdaptiveCard","version":"1.5","rtl":true,"body":[{"type":"TextBlock","text":"x"}]}""");
        Assert(card.FrameworkElement!.FlowDirection == FlowDirection.RightToLeft, "root should be RTL");
    }

    private static Task HostConfigsParse(MainPage page)
    {
        foreach (var (name, resource) in SampleRepository.HostConfigs)
        {
            var result = AdaptiveHostConfig.FromJsonString(SampleRepository.Read(resource));
            Assert(result.HostConfig is not null && result.Errors.Count == 0, $"{name}: {string.Join("; ", result.Errors)}");
        }

        return Task.CompletedTask;
    }

    private static async Task DynamicTypeahead(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.6","body":[{"type":"Input.ChoiceSet","id":"person","style":"filtered",
          "choices":[{"title":"Static Person","value":"static"}],
          "choices.data":{"type":"Data.Query","dataset":"graph.microsoft.com/users","count":3}}],
         "actions":[{"type":"Action.Submit","title":"Send"}]}
        """);
        var box = Find<AutoSuggestBox>(card.FrameworkElement!).Single();
        box.Text = "Me";
        await Task.Delay(800);
        var items = (box.ItemsSource as IEnumerable<string>)?.ToList() ?? [];
        Assert(items.Contains("Megan Bowen"), "dynamic choice expected, got: " + string.Join("|", items));

        box.Text = "Megan Bowen";
        await Task.Delay(800);
        var args = await ClickAndCaptureAsync(card, "Send") ?? throw new TestFailure("submit not raised");
        AssertEqual("megan.bowen", args.Inputs.AsJson()["person"]?.GetValue<string>(), "dynamic choice value");
    }

    private const string MediaCard = """
        {"type":"AdaptiveCard","version":"1.6","body":[{"type":"Media","poster":"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAFUlEQVR42mP8z8BQz0AEYBxVSF+FAAhKDveksOjmAAAAAElFTkSuQmCC",
          "sources":[{"mimeType":"video/mp4","url":"https://example.com/video.mp4"}]}]}
        """;

    private static async Task MediaClicked(MainPage page)
    {
        var original = page.Renderer.HostConfig;
        var hostConfig = AdaptiveHostConfig.FromJsonString("""{"media":{"allowInlinePlayback":false}}""").HostConfig!;
        page.Renderer.HostConfig = hostConfig;
        try
        {
            var card = await RenderAsync(page, MediaCard);
            AdaptiveMediaEventArgs? clicked = null;
            card.MediaClicked += (s, e) => clicked = e;
            Click(Find<Button>(card.FrameworkElement!).First());
            await Task.Delay(100);
            AssertEqual("https://example.com/video.mp4", clicked?.Media.Sources.FirstOrDefault()?.Url, "MediaClicked media");
            Assert(!Find<MediaPlayerElement>(card.FrameworkElement!).Any(), "no inline player expected");
        }
        finally
        {
            page.Renderer.HostConfig = original;
        }
    }

    private static async Task MediaInline(MainPage page)
    {
        var card = await RenderAsync(page, MediaCard);
        var player = Find<MediaPlayerElement>(card.FrameworkElement!).Single();
        Assert(!IsShown(player), "player should start hidden behind the poster");
        AdaptiveMediaEventArgs? clicked = null;
        card.MediaClicked += (s, e) => clicked = e;
        Click(Find<Button>(card.FrameworkElement!).First());
        await Task.Delay(200);
        Assert(IsShown(player), "player should be shown after clicking the poster");
        Assert(clicked is null, "inline playback must not raise MediaClicked");
    }

    private sealed class EmbeddedMediaResolver : IAdaptiveCardResourceResolver
    {
        public Windows.Foundation.IAsyncOperation<Windows.Storage.Streams.IRandomAccessStream> GetResourceStreamAsync(AdaptiveCardGetResourceStreamArgs args) =>
            Task.Run<Windows.Storage.Streams.IRandomAccessStream>(() =>
            {
                var stream = typeof(SelfTestRunner).Assembly.GetManifestResourceStream("SelfTests/selftest.mp4")!;
                var memory = new MemoryStream();
                stream.CopyTo(memory);
                memory.Position = 0;
                return memory.AsRandomAccessStream();
            }).AsAsyncOperation();
    }

    private static async Task MediaPlaybackWithCaptions(MainPage page)
    {
        page.Renderer.ResourceResolvers.Set("selftest", new EmbeddedMediaResolver());
        var vtt = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("WEBVTT\n\n00:00.000 --> 00:59.000\nHello captions\n"));
        var card = await RenderAsync(page, $$"""
        {"type":"AdaptiveCard","version":"1.6","body":[{"type":"Media","poster":"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAFUlEQVR42mP8z8BQz0AEYBxVSF+FAAhKDveksOjmAAAAAElFTkSuQmCC",
          "sources":[{"mimeType":"video/mp4","url":"selftest://video/selftest.mp4"}],
          "captionSources":[{"mimeType":"vtt","label":"English","url":"data:text/vtt;base64,{{vtt}}"}]}]}
        """);
        Click(Find<Button>(card.FrameworkElement!).First());
        var player = Find<MediaPlayerElement>(card.FrameworkElement!).Single();

        var deadline = DateTime.UtcNow.AddSeconds(30);
        Windows.Media.Playback.MediaPlaybackState state = default;
        TimeSpan position = default;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(250);
            state = player.MediaPlayer?.PlaybackSession?.PlaybackState ?? default;
            position = player.MediaPlayer?.PlaybackSession?.Position ?? default;
            if (position > TimeSpan.FromMilliseconds(300))
            {
                break;
            }
        }

        Assert(position > TimeSpan.FromMilliseconds(300), $"media did not play (state {state}, position {position}); warnings: {string.Join("; ", card.Warnings)}");

#if HAS_UNO
        // On Uno heads captions are rendered by the renderer's WebVTT overlay.
        await Task.Delay(500);
        var caption = Find<TextBlock>(card.FrameworkElement!).FirstOrDefault(t => t.Text == "Hello captions");
        Assert(caption is not null && IsShown(caption), "caption overlay should show the active cue");
#endif
    }

    private static Windows.UI.Color? BrushColor(Brush? brush) => (brush as SolidColorBrush)?.Color;

    private static async Task ActionSentiment(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[],"actions":[
          {"type":"Action.Submit","title":"Yes","style":"positive"},{"type":"Action.Submit","title":"No","style":"destructive"}]}
        """);
        var accent = page.Renderer.HostConfig.ContainerStyles.Default.ForegroundColors.Accent.Default;
        var attention = page.Renderer.HostConfig.ContainerStyles.Default.ForegroundColors.Attention.Default;

        var yes = FindButton(card, "Yes");
        var yesRoot = Find<Grid>(yes).FirstOrDefault(g => g.Name == "RootGrid");
        var yesBackground = BrushColor(yesRoot?.Background) ?? BrushColor(yes.Background);
        Assert(yesBackground == accent, $"positive action background {yesBackground} should be the accent color {accent}");

        var no = FindButton(card, "No");
        var noForeground = BrushColor(no.Foreground);
        Assert(noForeground == attention, $"destructive action foreground {noForeground} should be the attention color {attention}");
    }

    private static async Task PressFeedback(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[{"type":"Container","items":[{"type":"TextBlock","text":"Press me"}],
          "selectAction":{"type":"Action.Submit","title":"select"}}]}
        """);
        var button = Find<Button>(card.FrameworkElement!).First(b => TextOf(b.Content).Contains("Press me"));
        VisualStateManager.GoToState(button, "Pressed", true);
        await Task.Delay(400);
#if HAS_UNO
        var root = Find<Grid>(button).First(g => g.Name == "RootGrid");
        var scale = root.RenderTransform as ScaleTransform;
        Assert(scale is not null && scale.ScaleX < 1, $"pressed RootGrid should be scaled down (ScaleX={scale?.ScaleX})");
        VisualStateManager.GoToState(button, "Normal", true);
        await Task.Delay(400);
        Assert(scale!.ScaleX == 1, $"released RootGrid should be scaled back (ScaleX={scale.ScaleX})");
#else
        // Windows App SDK uses PointerDownThemeAnimation exactly like the WinUI3 renderer.
        VisualStateManager.GoToState(button, "Normal", true);
#endif
    }

    // Negative test cards that are expected to fail parsing (see AdaptiveCards.ObjectModel.Uno.Tests/SamplesTests).
    private static readonly string[] s_expectedParseFailures =
    [
        "v1.0/Tests/AdaptiveCard.MissingVersion.json",
        "v1.0/Tests/TypeIsRequired.json",
        "v1.1/Tests/InvalidMediaMix.json",
        "v1.2/Tests/Action.DuplicateIds.json",
        "v1.2/Tests/Action.NestedDuplicateIds.json",
        "v1.6/Tests/Carousel.NestedForbiddenElements.json",
    ];

    private static async Task AllSamplesRender(MainPage page)
    {
        var failures = new List<string>();
        int rendered = 0;
        foreach (var (name, resource) in SampleRepository.SampleCards)
        {
            var json = SampleRepository.Read(resource);
            if (!json.Contains("\"AdaptiveCard\"", StringComparison.Ordinal))
            {
                continue;
            }

            bool expectedFailure = s_expectedParseFailures.Contains(name);
            try
            {
                var card = page.RenderCard(json);
                await Task.Delay(30);
                var renderErrors = card?.Errors.Where(e => e.StatusCode == ErrorStatusCode.RenderFailed).ToList() ?? [];
                if (expectedFailure)
                {
                    if (card?.FrameworkElement is not null)
                    {
                        failures.Add($"{name}: expected a parse failure");
                    }
                }
                else if (card?.FrameworkElement is null || renderErrors.Count > 0)
                {
                    failures.Add($"{name}: {string.Join("; ", card?.Errors.Select(e => e.Message) ?? [])}");
                }
                else
                {
                    rendered++;
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.GetType().Name} {ex.Message}");
            }
        }

        Assert(failures.Count == 0, $"{failures.Count} cards failed ({rendered} rendered): " + string.Join(" | ", failures.Take(10)));
        Assert(rendered > 300, "expected more than 300 rendered sample cards, got " + rendered);
    }

    private static async Task UnknownElementFallback(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.5","body":[{"type":"Graph","fallback":{"type":"TextBlock","text":"No graphs here"}}]}
        """);
        Assert(Find<TextBlock>(card.FrameworkElement!).Any(t => t.Text == "No graphs here"), "fallback text expected");
        Assert(card.Warnings.Any(w => w.StatusCode == WarningStatusCode.PerformingFallback), "PerformingFallback warning expected");
    }
}
