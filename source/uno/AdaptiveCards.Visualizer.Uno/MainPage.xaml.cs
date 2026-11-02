using AdaptiveCards.ObjectModel.Uno;
using AdaptiveCards.Rendering.Uno;

namespace AdaptiveCards.Visualizer.Uno;

public sealed partial class MainPage : Page
{
    private readonly AdaptiveCardRenderer _renderer = new();
    private RenderedAdaptiveCard? _renderedCard;

    public MainPage()
    {
        InitializeComponent();

        HostConfigPicker.Items.Add("(default)");
        foreach (var (name, _) in SampleRepository.HostConfigs)
        {
            HostConfigPicker.Items.Add(name);
        }

        HostConfigPicker.SelectedIndex = 0;
        PopulateSamples(string.Empty);
        Loaded += (_, _) =>
        {
#if __DESKTOP__
            if (ScreenshotRunner.TryStart(this))
            {
                return;
            }
#endif
            if (SampleList.Items.Count > 0 && SampleList.SelectedIndex < 0)
            {
                SampleList.SelectedIndex = 0;
            }
        };
    }

    public AdaptiveCardRenderer Renderer => _renderer;

    public void SetCardWidth(double width)
    {
        CardHost.Width = width;
        CardHost.HorizontalAlignment = HorizontalAlignment.Left;
        CardScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
    }

    private void PopulateSamples(string filter)
    {
        SampleList.Items.Clear();
        foreach (var (name, _) in SampleRepository.SampleCards)
        {
            if (string.IsNullOrEmpty(filter) || name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                SampleList.Items.Add(name);
            }
        }
    }

    private void OnFilterChanged(object sender, TextChangedEventArgs e) => PopulateSamples(SampleFilter.Text);

    private void OnSampleSelected(object sender, SelectionChangedEventArgs e)
    {
        if (SampleList.SelectedItem is string name)
        {
            var resource = SampleRepository.SampleCards.First(s => s.Name == name).ResourceName;
            JsonEditor.Text = SampleRepository.Read(resource);
            RenderCard();
        }
    }

    private void OnHostConfigChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyHostConfig(HostConfigPicker.SelectedIndex <= 0 ? null : SampleRepository.HostConfigs[HostConfigPicker.SelectedIndex - 1].ResourceName);
        RenderCard();
    }

    public void ApplyHostConfig(string? resourceName)
    {
        if (resourceName is null)
        {
            _renderer.HostConfig = new AdaptiveHostConfig();
            return;
        }

        var result = AdaptiveHostConfig.FromJsonString(SampleRepository.Read(resourceName));
        _renderer.HostConfig = result.HostConfig ?? new AdaptiveHostConfig();
        foreach (var error in result.Errors)
        {
            Log($"Host config error: {error.StatusCode} {error.Message}");
        }
    }

    private void OnRenderClick(object sender, RoutedEventArgs e) => RenderCard();

    public RenderedAdaptiveCard? RenderCard(string? json = null)
    {
        if (json is not null)
        {
            JsonEditor.Text = json;
        }

        EventLog.Text = string.Empty;
        if (_renderedCard is not null)
        {
            _renderedCard.Action -= OnCardAction;
            _renderedCard.MediaClicked -= OnMediaClicked;
        }

        var cardJson = JsonEditor.Text ?? string.Empty;
        if (RewriteAssetUrls.IsChecked == true)
        {
            // The sample cards reference https://adaptivecards.io/content/..., which no longer serves the assets.
            // The same files are in this repository (source/nodejs/adaptivecards-site/.../content).
            cardJson = cardJson.Replace(
                "https://adaptivecards.io/content/",
                "https://raw.githubusercontent.com/microsoft/AdaptiveCards/main/source/nodejs/adaptivecards-site/themes/adaptivecards/source/content/");
        }

        _renderedCard = _renderer.RenderAdaptiveCardFromJsonString(cardJson);
        _renderedCard.Action += OnCardAction;
        _renderedCard.MediaClicked += OnMediaClicked;

        foreach (var error in _renderedCard.Errors)
        {
            Log($"Error: {error.StatusCode} {error.Message}");
        }

        foreach (var warning in _renderedCard.Warnings)
        {
            Log($"Warning: {warning.StatusCode} {warning.Message}");
        }

        CardHost.Child = _renderedCard.FrameworkElement;
        return _renderedCard;
    }

    private void OnMediaClicked(RenderedAdaptiveCard sender, AdaptiveMediaEventArgs args) =>
        Log($"MediaClicked: {args.Media.Sources.FirstOrDefault()?.Url}");

    private async void OnCardAction(RenderedAdaptiveCard sender, AdaptiveActionEventArgs args)
    {
        var inputs = args.Inputs?.AsJson()?.ToJsonString() ?? "{}";
        switch (args.Action)
        {
            case AdaptiveOpenUrlAction openUrl:
                Log($"Action.OpenUrl: {openUrl.Url}");
                if (openUrl.Url is { IsAbsoluteUri: true } url)
                {
                    try
                    {
                        await Windows.System.Launcher.LaunchUriAsync(url);
                    }
                    catch (Exception ex)
                    {
                        Log($"  launch failed: {ex.Message}");
                    }
                }

                break;
            case AdaptiveSubmitAction submit:
                Log($"Action.Submit '{submit.Title}' data={submit.DataJson?.ToJsonString() ?? "null"} inputs={inputs}");
                break;
            case AdaptiveExecuteAction execute:
                Log($"Action.Execute '{execute.Title}' verb={execute.Verb} data={execute.DataJson?.ToJsonString() ?? "null"} inputs={inputs}");
                break;
            case AdaptiveShowCardAction showCard:
                Log($"Action.ShowCard '{showCard.Title}' (popup mode)");
                break;
            default:
                Log($"{args.Action.ActionTypeString} '{args.Action.Title}'");
                break;
        }
    }

    private void Log(string message) => EventLog.Text += message + Environment.NewLine;
}
