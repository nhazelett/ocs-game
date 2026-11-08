using System.Text.Json;

namespace LinesOfSupport.Harness;

internal sealed record LoadedScenario(string Id, int Days, int SiteCount, bool UsedSchemas);

internal static class ScenarioLoader
{
    internal static LoadedScenario Load(string argument)
    {
        var path = ResolvePath(argument);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var data = document.RootElement;
        if (data.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"{path}: scenario must be a JSON object.");
        if (!data.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(id.GetString()))
            throw new InvalidDataException($"{path}: 'id' must be a non-empty string.");
        if (!data.TryGetProperty("days", out var days) || days.ValueKind != JsonValueKind.Number || !days.TryGetInt32(out var dayCount) || dayCount <= 0)
            throw new InvalidDataException($"{path}: 'days' must be a positive Int32 integer.");
        if (!data.TryGetProperty("sites", out var sites) || sites.ValueKind != JsonValueKind.Array || sites.GetArrayLength() == 0)
            throw new InvalidDataException($"{path}: 'sites' must be a non-empty array.");

        var schemas = SchemaCatalog.Find(path);
        schemas?.Validate("scenario", data);
        // Companion files are optional before M0-04, required when their schemas are present.
        foreach (var name in new[] { "rules", "theme" })
        {
            var companionPath = Path.Combine(Path.GetDirectoryName(path)!, name + ".json");
            if (File.Exists(companionPath))
            {
                using var companion = JsonDocument.Parse(File.ReadAllText(companionPath));
                schemas?.Validate(name, companion.RootElement);
            }
            else if (schemas?.Contains(name) == true)
            {
                throw new InvalidDataException($"Missing {companionPath}; its schema is present.");
            }
        }
        return new LoadedScenario(id.GetString()!, dayCount, sites.GetArrayLength(), schemas is not null);
    }

    private static string ResolvePath(string argument)
    {
        if (Directory.Exists(argument))
            return Path.GetFullPath(Path.Combine(argument, "scenario.json"));
        if (File.Exists(argument))
            return Path.GetFullPath(argument);

        // Allow `run spoke-b` from anywhere inside the repository, without relying on the executable's location.
        if (Path.GetFileName(argument) == argument)
        {
            for (var folder = new DirectoryInfo(Directory.GetCurrentDirectory()); folder is not null; folder = folder.Parent)
            {
                var candidate = Path.Combine(folder.FullName, "scenarios", argument, "scenario.json");
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        throw new FileNotFoundException($"Scenario '{argument}' does not exist. Supply a directory, scenario.json path, or scenario folder name.");
    }
}
