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
        if (schemas?.Contains("rules") == true)
        {
            using var rules = LoadRules(data, schemas);
            ScenarioReferences.Validate(data, rules.RootElement);
        }
        var themePath = Path.Combine(Path.GetDirectoryName(path)!, "theme.json");
        if (File.Exists(themePath))
        {
            using var theme = JsonDocument.Parse(File.ReadAllText(themePath));
            schemas?.Validate("theme", theme.RootElement);
        }
        // Before a schema exists, still reject malformed legacy fixture data without treating it as authoritative rules.
        var legacyRules = Path.Combine(Path.GetDirectoryName(path)!, "rules.json");
        if (schemas is null && File.Exists(legacyRules))
        {
            using var ignored = JsonDocument.Parse(File.ReadAllText(legacyRules));
        }
        return new LoadedScenario(id.GetString()!, dayCount, sites.GetArrayLength(), schemas is not null);
    }

    private static JsonDocument LoadRules(JsonElement scenario, SchemaCatalog schemas)
    {
        if (!scenario.TryGetProperty("rulesVersion", out var requested) || requested.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("scenario.json: rulesVersion must select a shared rules file.");
        var directory = Path.Combine(schemas.ScenariosDirectory, "rules");
        var versions = new Dictionary<string, string>(StringComparer.Ordinal);
        string? selected = null;
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory, "*.json").Order(StringComparer.Ordinal))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                schemas.Validate("rules", document.RootElement);
                if (!document.RootElement.TryGetProperty("rulesVersion", out var version) || version.ValueKind != JsonValueKind.String)
                    throw new InvalidDataException($"{file}: missing rulesVersion.");
                var value = version.GetString()!;
                if (!versions.TryAdd(value, file))
                    throw new InvalidDataException($"Duplicate rulesVersion '{value}' in {file} and {versions[value]}.");
                if (value == requested.GetString())
                    selected = file;
            }
        }
        if (selected is null)
            throw new InvalidDataException($"No shared rules file matches rulesVersion '{requested.GetString()}' in {directory}.");
        return JsonDocument.Parse(File.ReadAllText(selected));
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
