using System.Text.Json;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Harness;

internal sealed record LoadedScenario(string Id, int Days, int SiteCount, bool UsedSchemas, string FilePath, string? RulesDirectory);

internal static class ScenarioLoader
{
    internal static ScenarioBundle LoadBundle(LoadedScenario scenario)
    {
        if (!string.Equals(Path.GetFileName(scenario.FilePath), "scenario.json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Simulation requires a scenario.json file, as required by the Sim loader.");
        return ScenarioBundle.Load(scenario.FilePath, scenario.RulesDirectory);
    }

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
        return new LoadedScenario(id.GetString()!, dayCount, sites.GetArrayLength(), schemas is not null,
            path, schemas is null ? null : Path.Combine(schemas.ScenariosDirectory, "rules"));
    }

    private static JsonDocument LoadRules(JsonElement scenario, SchemaCatalog schemas)
    {
        if (!scenario.TryGetProperty("rulesVersion", out var requested) || requested.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("scenario.json: rulesVersion must select a shared rules file.");
        var directory = Path.Combine(schemas.ScenariosDirectory, "rules");
        var requestedVersion = requested.GetString()!;
        if (string.IsNullOrWhiteSpace(requestedVersion) || requestedVersion.IndexOfAny(['/', '\\']) >= 0 ||
            requestedVersion.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidDataException("rulesVersion must be a file-name version, not a path.");
        var selected = Path.Combine(directory, $"rules-{requestedVersion}.json");
        if (!File.Exists(selected))
            throw new InvalidDataException($"No shared rules file at {selected} for rulesVersion '{requestedVersion}'.");
        var versions = new Dictionary<string, string>(StringComparer.Ordinal);
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
            }
        }
        var result = JsonDocument.Parse(File.ReadAllText(selected));
        if (result.RootElement.GetProperty("rulesVersion").GetString() != requestedVersion)
        {
            result.Dispose();
            throw new InvalidDataException($"{selected}: rulesVersion does not match scenario rulesVersion '{requestedVersion}'.");
        }
        return result;
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
