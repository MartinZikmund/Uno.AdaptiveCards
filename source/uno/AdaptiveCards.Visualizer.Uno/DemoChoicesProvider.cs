using AdaptiveCards.ObjectModel.Uno;
using AdaptiveCards.Rendering.Uno;

namespace AdaptiveCards.Visualizer.Uno;

/// <summary>
/// Demo <see cref="IAdaptiveChoicesProvider"/> for the Data.Query (dynamic typeahead) sample cards.
/// A real host would call its backend (e.g. a bot invoke) with the dataset and query text.
/// </summary>
public sealed class DemoChoicesProvider : IAdaptiveChoicesProvider
{
    private static readonly Dictionary<string, string[]> s_datasets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["graph.microsoft.com/users"] = ["Adele Vance", "Alex Wilber", "Diego Siciliani", "Grady Archie", "Isaiah Langer", "Lee Gu", "Megan Bowen", "Nestor Wilke"],
        ["teamScopeInOrg"] = ["Design", "Engineering", "Finance", "Marketing", "Operations", "Sales", "Support"],
        ["packagesInScope"] = ["AdaptiveCards", "Newtonsoft.Json", "System.Text.Json", "Uno.Sdk", "Uno.WinUI", "xunit"],
    };

    public async Task<IList<AdaptiveChoiceInput>> GetChoicesAsync(AdaptiveChoicesQuery query, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken); // simulate a backend round-trip
        if (!s_datasets.TryGetValue(query.Dataset, out var items))
        {
            items = Enumerable.Range(1, 5).Select(i => $"{query.Dataset} result {i}").ToArray();
        }

        IEnumerable<string> matches = items.Where(i => i.Contains(query.QueryText, StringComparison.OrdinalIgnoreCase));
        if (query.Skip is int skip)
        {
            matches = matches.Skip(skip);
        }

        if (query.Count is int count)
        {
            matches = matches.Take(count);
        }

        return matches.Select(m => new AdaptiveChoiceInput { Title = m, Value = m.ToLowerInvariant().Replace(' ', '.') }).ToList();
    }
}
