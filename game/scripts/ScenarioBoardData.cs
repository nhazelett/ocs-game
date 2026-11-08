using System.Text.Json;

namespace LinesOfSupport.Game;

// Read-only presentation data from existing schema fields. This does not load or execute a simulation session.
internal sealed record BoardIsland(string Id, string Name, bool IsHub);
internal sealed record BoardLink(string From, string To, string Mode);
internal sealed record BoardSite(string Id, string Name, string Island, int OpensDay);
internal sealed record ScenarioBoardData(string Id, string Title, IReadOnlyList<BoardIsland> Islands,
    IReadOnlyList<BoardLink> Links, IReadOnlyList<BoardSite> Sites)
{
    internal static ScenarioBoardData Load(string scenarioPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        var root = document.RootElement;
        var islands = root.GetProperty("islands").EnumerateArray().Select(x =>
            new BoardIsland(Text(x, "id"), Text(x, "name"), x.GetProperty("isHub").GetBoolean())).ToArray();
        if (islands.Length == 0 || islands.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != islands.Length)
            throw new InvalidDataException("The board needs non-empty islands with unique IDs.");
        var ids = islands.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var links = root.GetProperty("transportLinks").EnumerateArray().Select(x =>
            new BoardLink(Text(x, "from"), Text(x, "to"), Text(x, "mode"))).ToArray();
        var sites = root.GetProperty("sites").EnumerateArray().Select(x =>
            new BoardSite(Text(x, "id"), Text(x, "name"), Text(x, "island"), x.GetProperty("opensDay").GetInt32())).ToArray();
        if (links.Any(x => !ids.Contains(x.From) || !ids.Contains(x.To)) || sites.Any(x => !ids.Contains(x.Island)))
            throw new InvalidDataException("A board link or site references an unknown island.");
        if (sites.Any(x => x.OpensDay < 0))
            throw new InvalidDataException("A site's opensDay must be non-negative.");
        return new ScenarioBoardData(Text(root, "id"), Text(root, "title"), islands, links, sites);
    }

    private static string Text(JsonElement parent, string key)
    {
        var result = parent.GetProperty(key).GetString();
        return string.IsNullOrWhiteSpace(result)
            ? throw new InvalidDataException($"Board field '{key}' must be a non-empty string.") : result;
    }
}

internal sealed record ScenarioFolder(string Name, string ScenarioPath);

internal static class ScenarioCatalog
{
    internal static IReadOnlyList<ScenarioFolder> Discover(string scenariosDirectory) =>
        Directory.Exists(scenariosDirectory)
            ? Directory.EnumerateDirectories(scenariosDirectory)
                .Where(x => File.Exists(Path.Combine(x, "scenario.json")))
                .OrderBy(Path.GetFileName, StringComparer.Ordinal)
                .Select(x => new ScenarioFolder(Path.GetFileName(x), Path.Combine(x, "scenario.json"))).ToArray()
            : [];
}
