using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.Uno;
using AdaptiveCards.Rendering.Uno;
using Microsoft.UI.Xaml.Automation;
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
        page.LaunchExternalUrls = false;
        var filter = Environment.GetEnvironmentVariable("AC_SELFTEST_FILTER");
        foreach (var (name, test) in Tests)
        {
            if (!string.IsNullOrEmpty(filter) && !name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Console.WriteLine("SELFTEST RUN " + name);
            // Watchdog on a thread-pool timer: reports a test that does not finish even if the UI thread is blocked.
            var started = DateTime.UtcNow;
            using var watchdog = new System.Threading.Timer(
                _ => Console.WriteLine($"SELFTEST STILL RUNNING {name} ({(int)(DateTime.UtcNow - started).TotalSeconds}s)"),
                null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
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
        ("Caption tracks: picker, styled cues and cue settings", CaptionTracks),
        ("Inline input labels and revealOnHover inputs (schema 1.6)", InlineLabelsAndRevealOnHover),
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

    /// <summary>Polls until <paramref name="condition"/> holds (layout on slow devices/emulators), up to <paramref name="timeoutMs"/>.</summary>
    private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                return false;
            }

            await Task.Delay(100);
        }

        return true;
    }

    private static void AssertEqual(string? expected, string? actual, string what) =>
        Assert(expected == actual, $"{what}: expected '{expected}', got '{actual}'");

    private static void AssertEqual<T>(T expected, T actual, string what) where T : struct =>
        Assert(EqualityComparer<T>.Default.Equals(expected, actual), $"{what}: expected '{expected}', got '{actual}'");

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
        Console.WriteLine("SELFTEST STEP media card rendered");
        Click(Find<Button>(card.FrameworkElement!).First());
        Console.WriteLine("SELFTEST STEP poster clicked");
        var player = Find<MediaPlayerElement>(card.FrameworkElement!).Single();

        var deadline = DateTime.UtcNow.AddSeconds(30);
        var nextLog = DateTime.UtcNow;
        Windows.Media.Playback.MediaPlaybackState state = default;
        TimeSpan position = default;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(250);
            state = player.MediaPlayer?.PlaybackSession?.PlaybackState ?? default;
            position = player.MediaPlayer?.PlaybackSession?.Position ?? default;
            if (DateTime.UtcNow >= nextLog)
            {
                Console.WriteLine($"SELFTEST STEP media state {state}, position {position}, warnings {card.Warnings.Count}");
                nextLog = DateTime.UtcNow.AddSeconds(2);
            }

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

    private static async Task CaptionTracks(MainPage page)
    {
#if HAS_UNO
        page.Renderer.ResourceResolvers.Set("selftest", new EmbeddedMediaResolver());
        static string Vtt(string body) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("WEBVTT\n\n" + body));
        var english = Vtt("00:00.000 --> 00:59.000 line:10% align:start position:10% size:50%\n<b>Bold</b> and <i>italic</i> &amp; more\n");
        var czech = Vtt("00:00.000 --> 00:59.000\nAhoj\n");
        var card = await RenderAsync(page, $$"""
        {"type":"AdaptiveCard","version":"1.6","body":[{"type":"Media","poster":"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAFUlEQVR42mP8z8BQz0AEYBxVSF+FAAhKDveksOjmAAAAAElFTkSuQmCC",
          "sources":[{"mimeType":"video/mp4","url":"selftest://video/selftest.mp4"}],
          "captionSources":[{"mimeType":"vtt","label":"English","url":"data:text/vtt;base64,{{english}}"},
                            {"mimeType":"vtt","label":"Czech","url":"data:text/vtt;base64,{{czech}}"}]}]}
        """);
        Click(Find<Button>(card.FrameworkElement!).First());
        await Task.Delay(1200);

        var root = card.FrameworkElement!;
        var ccButton = Find<Button>(root).FirstOrDefault(b => AutomationProperties.GetName(b) == "Closed captions")
            ?? throw new TestFailure("closed captions button not found");
        Assert(IsShown(ccButton), "closed captions button should be visible");
        var items = ((MenuFlyout)ccButton.Flyout).Items.OfType<ToggleMenuFlyoutItem>().ToList();
        AssertEqual("Off,English,Czech", string.Join(",", items.Select(i => i.Text)), "caption menu entries");
        AssertEqual("English", items.Single(i => i.IsChecked).Text, "first track presented by default");

        // AC_SELFTEST_HOLD_MS keeps each stage on screen (for screenshots).
        var hold = int.TryParse(Environment.GetEnvironmentVariable("AC_SELFTEST_HOLD_MS"), out var ms) ? ms : 0;
        await Task.Delay(hold);

        TextBlock? Cue() => Find<TextBlock>(root).FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == "AdaptiveMediaCaptionCue");

        var cue = Cue() ?? throw new TestFailure("no caption cue shown for the default track");
        AssertEqual("Bold and italic & more", AutomationProperties.GetName(cue), "cue text");
        var runs = cue.Inlines.OfType<Microsoft.UI.Xaml.Documents.Run>().ToList();
        Assert(runs.Any(r => r.Text == "Bold" && r.FontWeight.Weight == Microsoft.UI.Text.FontWeights.Bold.Weight), "<b> should be bold");
        Assert(runs.Any(r => r.Text == "italic" && r.FontStyle == Windows.UI.Text.FontStyle.Italic), "<i> should be italic");
        var box = (Border)VisualTreeHelper.GetParent(cue);
        AssertEqual(VerticalAlignment.Top, box.VerticalAlignment, "line:10% places the cue from the top");
        AssertEqual(HorizontalAlignment.Left, box.HorizontalAlignment, "position/size place the cue horizontally");
        AssertEqual(TextAlignment.Left, cue.TextAlignment, "align:start");
        var host = (FrameworkElement)VisualTreeHelper.GetParent(box);
        Assert(Math.Abs(box.Margin.Top - host.ActualHeight * 0.10) < 2, $"line:10% top margin (got {box.Margin.Top}, height {host.ActualHeight})");
        Assert(Math.Abs(box.Width - host.ActualWidth * 0.50) < 2, $"size:50% width (got {box.Width}, width {host.ActualWidth})");

        ccButton.Flyout.ShowAt(ccButton);
        await Task.Delay(Math.Max(300, hold));
        ccButton.Flyout.Hide();
        items.Single(i => i.Text == "Czech").IsChecked = true;
        await Task.Delay(700 + hold);
        AssertEqual("Ahoj", Cue() is { } czechCue ? AutomationProperties.GetName(czechCue) : null, "selected track's cue");
        AssertEqual("Czech", items.Single(i => i.IsChecked).Text, "selected track checked");

        items.Single(i => i.Text == "Off").IsChecked = true;
        await Task.Delay(700);
        Assert(Cue() is null, "captions off should hide cues");
#else
        // Windows App SDK presents captions through the platform (TimedTextSource + transport controls).
        await Task.CompletedTask;
#endif
    }

    private static async Task InlineLabelsAndRevealOnHover(MainPage page)
    {
        var card = await RenderAsync(page, """
        {"type":"AdaptiveCard","version":"1.6","body":[
          {"type":"Input.Text","id":"name","label":"Name","labelPosition":"inline","labelWidth":40},
          {"type":"Input.Number","id":"age","label":"Age","labelPosition":"inline","labelWidth":"120px"},
          {"type":"Input.Text","id":"city","label":"City","labelPosition":"inline"},
          {"type":"Input.Text","id":"above","label":"Above"},
          {"type":"Input.Text","id":"note","label":"Note","inputStyle":"revealOnHover"}]}
        """);
        var root = card.FrameworkElement!;
        await Task.Delay(300 + (int.TryParse(Environment.GetEnvironmentVariable("AC_SELFTEST_HOLD_MS"), out var hold) ? hold : 0));

        static string LabelText(RichTextBlock label) =>
            string.Concat(label.Blocks.OfType<Microsoft.UI.Xaml.Documents.Paragraph>()
                .SelectMany(p => p.Inlines.OfType<Microsoft.UI.Xaml.Documents.Run>()).Select(r => r.Text));

        Grid InlineGrid(string labelText) => Find<Grid>(root).FirstOrDefault(g =>
            g.ColumnDefinitions.Count == 2 && g.Children.Count == 2 && g.Children[0] is RichTextBlock label && LabelText(label) == labelText)
            ?? throw new TestFailure($"no inline label layout for '{labelText}'");

        var nameGrid = InlineGrid("Name");
        AssertEqual(GridUnitType.Star, nameGrid.ColumnDefinitions[0].Width.GridUnitType, "labelWidth 40 is a proportion");
        Assert(nameGrid.ColumnDefinitions[0].Width.Value == 40 && nameGrid.ColumnDefinitions[1].Width.Value == 60, "labelWidth 40 -> 40*/60*");
        var nameLabel = (FrameworkElement)nameGrid.Children[0];
        var nameInputColumn = (FrameworkElement)nameGrid.Children[1];
        Assert(Grid.GetColumn(nameLabel) == 0 && Grid.GetColumn(nameInputColumn) == 1, "label in column 0, input in column 1");
        Assert(Find<TextBox>(nameInputColumn).Any(), "the input sits in the second column");
        await WaitUntilAsync(() => nameInputColumn.ActualWidth > 0 && nameLabel.ActualWidth > 0
            && nameInputColumn.TransformToVisual(root).TransformPoint(default).X > nameLabel.TransformToVisual(root).TransformPoint(default).X);
        var labelPos = nameLabel.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0, 0));
        var inputPos = nameInputColumn.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0, 0));
        Assert(labelPos.X < inputPos.X, $"inline label should be left of the input (label x {labelPos.X}, input column x {inputPos.X}, grid width {nameGrid.ActualWidth})");
        var labelMid = labelPos.Y + nameLabel.ActualHeight / 2;
        Assert(labelMid >= inputPos.Y && labelMid <= inputPos.Y + nameInputColumn.ActualHeight, "inline label should be vertically centered on the input");

        var ageGrid = InlineGrid("Age");
        AssertEqual(GridUnitType.Pixel, ageGrid.ColumnDefinitions[0].Width.GridUnitType, "labelWidth 120px is absolute");
        Assert(ageGrid.ColumnDefinitions[0].Width.Value == 120, "labelWidth 120px");

        var cityGrid = InlineGrid("City");
        Assert(cityGrid.ColumnDefinitions[0].Width.Value == page.Renderer.HostConfig.Inputs.Label.Width, "default inline width from host config");

        Assert(!Find<Grid>(root).Any(g => g.ColumnDefinitions.Count == 2 && g.Children.Count == 2 && g.Children[0] is RichTextBlock l && LabelText(l) == "Above"),
            "labels without labelPosition stay above the input");

        var note = Find<TextBox>(root).Single(t => AutomationProperties.GetName(t) == "Note");
        AssertEqual("hidden", AutomationProperties.GetItemStatus(note), "revealOnHover input starts without chrome");
        Assert(note.BorderBrush is SolidColorBrush { Color.A: 0 }, "revealOnHover border should be transparent until hovered/focused");
        if (note.Focus(FocusState.Programmatic))
        {
            await Task.Delay(200);
            AssertEqual("revealed", AutomationProperties.GetItemStatus(note), "focused revealOnHover input shows its chrome");
        }
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
