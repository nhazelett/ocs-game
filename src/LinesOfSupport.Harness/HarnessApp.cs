using System.Globalization;
using System.Text.Json;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Harness;

public static class HarnessApp
{
    private const string Help = """
        OCS: Lines of Support — command-line harness
        Usage:
          run <scenario> [--seed N] [--policy do-nothing|greedy|textbook] [--log <input-log.json>]
          validate <scenario-dir>
          batch <scenario> [--seeds 200] [--policies do-nothing,greedy,textbook] [--json]
        <scenario> is a directory, scenario.json path, or folder name under scenarios/.
        --log replays the log's seed and actions; do not combine it with --seed or --policy.
        Exit codes: 0 success, 1 invalid data or file error, 2 invalid command/arguments.
        """;

    private static readonly IReadOnlyList<string> Policies = PolicyCatalog.Names;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h" or "help")
        {
            output.WriteLine(Help);
            return 0;
        }

        try
        {
            if (args.Length < 2)
                throw new UsageException("Specify a command and a scenario.");

            var options = ParseOptions(args);
            switch (args[0])
            {
                case "validate":
                    RequireOptions(options, []);
                    var validated = ScenarioLoader.Load(args[1]);
                    output.WriteLine($"Valid: {validated.Id} ({validated.Days} days, {validated.SiteCount} sites).");
                    WriteValidationMode(validated, output);
                    break;
                case "run":
                    RequireOptions(options, ["--seed", "--policy", "--log"]);
                    var seed = IntegerOption(options, "--seed", 0, positive: false);
                    var policy = options.GetValueOrDefault("--policy", "do-nothing");
                    RequirePolicy(policy);
                    if (options.ContainsKey("--log") && (options.ContainsKey("--seed") || options.ContainsKey("--policy")))
                        throw new UsageException("With --log, the seed and actions come from the log; omit --seed and --policy.");
                    var scenario = ScenarioLoader.Load(args[1]);
                    var bundle = ScenarioLoader.LoadBundle(scenario);
                    Session session;
                    string title;
                    if (options.TryGetValue("--log", out var logPath))
                    {
                        var log = LoadLog(logPath);
                        session = Replay.Run(bundle, log);
                        title = $"{bundle.Scenario.Title} / {Path.GetFileNameWithoutExtension(logPath)}\n{log.Note}";
                    }
                    else
                    {
                        session = Replay.Run(bundle, seed, PolicyCatalog.Create(policy));
                        title = $"{bundle.Scenario.Title} / policy {policy}";
                    }
                    output.Write(SessionReport.ToText(session, title));
                    break;
                case "batch":
                    RequireOptions(options, ["--seeds", "--policies", "--json"]);
                    var count = IntegerOption(options, "--seeds", 200, positive: true);
                    var selectedPolicies = options.GetValueOrDefault("--policies", string.Join(',', Policies)).Split(',');
                    foreach (var selected in selectedPolicies)
                        RequirePolicy(selected);
                    if (selectedPolicies.Distinct(StringComparer.Ordinal).Count() != selectedPolicies.Length)
                        throw new UsageException("--policies must not contain duplicates.");
                    var batchScenario = ScenarioLoader.Load(args[1]);
                    var report = BatchReport.Create(batchScenario, count, selectedPolicies);
                    if (options.ContainsKey("--json"))
                        output.WriteLine(report.ToJson());
                    else
                    {
                        WriteValidationMode(batchScenario, output);
                        report.WriteText(output);
                    }
                    break;
                default:
                    throw new UsageException($"Unknown command '{args[0]}'.");
            }
            return 0;
        }
        catch (UsageException ex)
        {
            error.WriteLine($"Error: {ex.Message}");
            error.WriteLine(Help);
            return 2;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException)
        {
            error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 2; i < args.Length; i++)
        {
            var key = args[i];
            if (!key.StartsWith("--", StringComparison.Ordinal))
                throw new UsageException($"Expected an option at '{key}'.");
            var value = "";
            if (key != "--json")
            {
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    throw new UsageException($"Expected an option followed by a value at '{args[i]}'.");
                value = args[++i];
            }
            if (!options.TryAdd(key, value))
                throw new UsageException($"Option '{key}' was supplied more than once.");
        }
        return options;
    }

    private static void RequireOptions(Dictionary<string, string> options, string[] allowed)
    {
        foreach (var key in options.Keys)
            if (!allowed.Contains(key, StringComparer.Ordinal))
                throw new UsageException($"Unknown option '{key}'.");
    }

    private static int IntegerOption(Dictionary<string, string> options, string key, int fallback, bool positive)
    {
        if (!options.TryGetValue(key, out var value))
            return fallback;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ||
            (positive && number <= 0))
            throw new UsageException($"{key} requires {(positive ? "a positive" : "an")} Int32 integer.");
        return number;
    }

    private static void RequirePolicy(string policy)
    {
        if (!Policies.Contains(policy, StringComparer.Ordinal))
            throw new UsageException($"Unknown policy '{policy}'. Choose {string.Join(", ", Policies)}.");
    }

    private static InputLog LoadLog(string path)
    {
        try { return InputLog.Load(path); }
        catch (NotSupportedException ex)
        {
            throw new InvalidDataException($"{path}: input log cannot be deserialized: {ex.Message}", ex);
        }
    }

    private static void WriteValidationMode(LoadedScenario scenario, TextWriter output) =>
        output.WriteLine(scenario.UsedSchemas
            ? "Validation: repository JSON Schemas and harness input checks."
            : "Validation: fallback id/days/sites checks only; M0-04 schemas are not available yet.");

    private sealed class UsageException(string message) : Exception(message);
}
