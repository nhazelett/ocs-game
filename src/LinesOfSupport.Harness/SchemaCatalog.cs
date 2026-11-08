using System.Globalization;
using System.Text.Json;
using Json.Schema;

namespace LinesOfSupport.Harness;

internal sealed class SchemaCatalog(Dictionary<string, JsonSchema> schemas, string directory)
{
    internal string ScenariosDirectory => Path.GetDirectoryName(directory)!;
    internal bool Contains(string name) => schemas.ContainsKey(name);

    internal static SchemaCatalog? Find(string scenarioPath)
    {
        for (var folder = new DirectoryInfo(Path.GetDirectoryName(scenarioPath)!); folder is not null; folder = folder.Parent)
        {
            var directory = Path.Combine(folder.FullName, "schema");
            if (Directory.Exists(directory))
                return Load(directory);
        }
        return null;
    }

    private static SchemaCatalog? Load(string directory)
    {
        var paths = Directory.GetFiles(directory, "*.json").Order(StringComparer.Ordinal).ToArray();
        if (paths.Length == 0)
            return null;

        try
        {
            var options = new BuildOptions { Dialect = Dialect.Draft202012, SchemaRegistry = new SchemaRegistry() };
            var schemas = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
            foreach (var path in paths)
            {
                // Use file:/// syntax on Windows so $ref fragments stay fragments, not filename characters.
                var fileUri = new Uri(new Uri(Path.GetFullPath(path)).AbsoluteUri);
                var schema = JsonSchema.FromFile(path, options, fileUri);
                if (schema.BaseUri != fileUri)
                    options.SchemaRegistry.Register(fileUri, schema);
                var name = Path.GetFileNameWithoutExtension(path);
                if (name.EndsWith(".schema", StringComparison.Ordinal))
                    name = name[..^7];
                if (!schemas.TryAdd(name, schema))
                    throw new InvalidDataException($"Ambiguous schemas for '{name}' in {directory}.");
            }
            if (!schemas.ContainsKey("scenario"))
                throw new InvalidDataException($"{directory}: schemas exist but scenario.json or scenario.schema.json is missing; refusing fallback validation.");
            return new SchemaCatalog(schemas, directory);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Cannot load schemas in {directory}: {ex.Message}", ex);
        }
    }

    internal void Validate(string name, JsonElement data)
    {
        if (!schemas.TryGetValue(name, out var schema))
            return;
        try
        {
            var results = schema.Evaluate(data, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List, Culture = CultureInfo.InvariantCulture, RequireFormatValidation = true
            });
            if (!results.IsValid)
                throw new InvalidDataException($"{name}.json failed JSON Schema validation: {JsonSerializer.Serialize(results)}");
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException($"Cannot evaluate {name}.json against its schema: {ex.Message}", ex);
        }
    }
}
