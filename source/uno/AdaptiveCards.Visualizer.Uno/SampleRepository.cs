using System.Reflection;

namespace AdaptiveCards.Visualizer.Uno;

/// <summary>Sample cards and host configs embedded from the repository's samples folder.</summary>
public static class SampleRepository
{
    private static readonly Assembly s_assembly = typeof(SampleRepository).Assembly;

    private static IEnumerable<string> Resources(string prefix) =>
        s_assembly.GetManifestResourceNames()
            .Select(n => (Raw: n, Normalized: n.Replace('\\', '/')))
            .Where(n => n.Normalized.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(n => n.Normalized, StringComparer.OrdinalIgnoreCase)
            .Select(n => n.Raw);

    public static IReadOnlyList<(string Name, string ResourceName)> SampleCards { get; } =
        Resources("Samples/").Select(n => (n.Replace('\\', '/').Substring("Samples/".Length), n)).ToList();

    public static IReadOnlyList<(string Name, string ResourceName)> HostConfigs { get; } =
        Resources("HostConfig/").Select(n => (Path.GetFileNameWithoutExtension(n.Replace('\\', '/')), n)).ToList();

    public static string Read(string resourceName)
    {
        using var stream = s_assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Missing resource " + resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
