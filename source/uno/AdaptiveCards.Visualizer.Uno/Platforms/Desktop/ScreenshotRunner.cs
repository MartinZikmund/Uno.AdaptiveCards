using System.Runtime.InteropServices.WindowsRuntime;
using AdaptiveCards.Rendering.Uno;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;

namespace AdaptiveCards.Visualizer.Uno;

/// <summary>
/// Headless screenshot mode for the desktop head:
///   AdaptiveCards.Visualizer.Uno --screenshots &lt;outputDir&gt; [nameFilter] [hostConfigName]
/// Renders every embedded sample card (optionally filtered) and writes a PNG per card plus a log of warnings/errors.
/// </summary>
internal static class ScreenshotRunner
{
    public static bool TryStart(MainPage page)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--screenshots");
        if (index < 0 || index + 1 >= args.Length)
        {
            return false;
        }

        var outputDir = args[index + 1];
        var filter = index + 2 < args.Length ? args[index + 2] : string.Empty;
        var hostConfig = index + 3 < args.Length ? args[index + 3] : null;
        _ = RunAsync(page, outputDir, filter, hostConfig);
        return true;
    }

    private static async Task RunAsync(MainPage page, string outputDir, string filter, string? hostConfigName)
    {
        Directory.CreateDirectory(outputDir);
        var log = new List<string>();
        try
        {
            if (!string.IsNullOrEmpty(hostConfigName))
            {
                var hc = SampleRepository.HostConfigs.FirstOrDefault(h => h.Name == hostConfigName);
                page.ApplyHostConfig(hc.ResourceName);
            }

            page.SetCardWidth(480);
            await Task.Delay(500);
            // The filter may also be a directory or file path with ad-hoc cards.
            IEnumerable<(string Name, Func<string> Read)> cards = Directory.Exists(filter)
                ? Directory.GetFiles(filter, "*.json", SearchOption.AllDirectories).OrderBy(f => f)
                    .Select(f => (Path.GetRelativePath(filter, f).Replace('\\', '/'), (Func<string>)(() => File.ReadAllText(f))))
                : File.Exists(filter)
                    ? new[] { (Path.GetFileName(filter), (Func<string>)(() => File.ReadAllText(filter))) }
                    : SampleRepository.SampleCards
                        .Where(s => string.IsNullOrEmpty(filter) || s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        .Select(s => (s.Name, (Func<string>)(() => SampleRepository.Read(s.ResourceName))));

            foreach (var (name, read) in cards)
            {

                RenderedAdaptiveCard? rendered = null;
                try
                {
                    rendered = page.RenderCard(read());
                    await Task.Delay(SettleMilliseconds(name));
                    var element = rendered?.FrameworkElement;
                    var file = Path.Combine(outputDir, name.Replace('/', '_').Replace(".json", ".png"));
                    if (element is not null && element.ActualWidth > 0 && element.ActualHeight > 0)
                    {
                        await SaveAsync(element, file);
                    }

                    if (Environment.GetEnvironmentVariable("AC_DUMP_PIPS") == "1" && element is not null)
                    {
                        foreach (var pager in FindAll<PipsPager>(element))
                        {
                            log.Add($"PIPS pages={pager.NumberOfPages} orient={pager.Orientation} size={pager.ActualWidth:0}x{pager.ActualHeight:0}");
                            foreach (var b in FindAll<Button>(pager))
                            {
                                var t = b.TransformToVisual(pager).TransformPoint(new Windows.Foundation.Point(0, 0));
                                log.Add($"  pip {b.Name} at {t.X:0},{t.Y:0} size {b.ActualWidth:0}x{b.ActualHeight:0} vis={b.Visibility}");
                            }
                        }
                    }

                    var problems = rendered is null
                        ? "no result"
                        : string.Join(" | ", rendered.Errors.Select(e => "E:" + e.StatusCode + " " + e.Message)
                            .Concat(rendered.Warnings.Select(w => "W:" + w.StatusCode + " " + w.Message)));
                    log.Add($"{name}\t{(element is null ? "NO-ELEMENT" : $"{element.ActualWidth:0}x{element.ActualHeight:0}")}\t{problems}");
                }
                catch (Exception ex)
                {
                    log.Add($"{name}\tEXCEPTION\t{ex}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Add("FATAL " + ex);
        }
        finally
        {
            await File.WriteAllLinesAsync(Path.Combine(outputDir, "render-log.txt"), log);
            Environment.Exit(0);
        }
    }

    // Cards referencing remote images need time for downloads to complete.
    private static int SettleMilliseconds(string name) => int.TryParse(Environment.GetEnvironmentVariable("AC_SCREENSHOT_DELAY"), out var d) ? d : 2500;

    private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T t)
            {
                yield return t;
            }

            foreach (var d in FindAll<T>(child))
            {
                yield return d;
            }
        }
    }

    private static async Task SaveAsync(UIElement element, string file)
    {
        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(element);
        var pixels = (await rtb.GetPixelsAsync()).ToArray();
        var info = new SKImageInfo(rtb.PixelWidth, rtb.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmap.GetPixels(), Math.Min(pixels.Length, info.BytesSize));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        await using var stream = File.Create(file);
        data.SaveTo(stream);
    }
}
