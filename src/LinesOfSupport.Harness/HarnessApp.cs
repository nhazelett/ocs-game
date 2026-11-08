using System.Globalization;
using System.Text.Json;

namespace LinesOfSupport.Harness;

public static class HarnessApp
{
    private const string Help = """
        OCS: Lines of Support — M0 harness (no simulation rules yet)
        Usage:
          run <scenario> [--seed N] [--policy do-nothing|greedy|textbook] [--log <input-log.json>]
          validate <scenario-dir>
          batch <scenario> [--seeds 200] [--policies do-nothing,greedy,textbook]
        <scenario> is a directory, scenario.json path, or folder name under scenarios/.
        Exit codes: 0 success, 1 invalid data or file error, 2 invalid command/arguments.
        """;

    private static readonly string[] Policies = ["do-nothing", "greedy", "textbook"];

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
                    var scenario = ScenarioLoader.Load(args[1]);
                    if (options.TryGetValue("--log", out var log))
                    {
                        // Input-log contracts belong to Fable. M0 checks syntax without inventing replay rules.
                        using var parsedLog = JsonDocument.Parse(File.ReadAllText(log));
                        output.WriteLine("Input log: JSON parsed; actions are NOT replayed in the M0 placeholder.");
                    }
                    WritePlaceholder(output);
                    WriteValidationMode(scenario, output);
                    var result = PlaceholderRun.Advance(scenario, seed, policy);
                    output.WriteLine($"Scenario: {scenario.Id}; seed: {seed}; policy: {policy}.");
                    output.WriteLine($"Days advanced: {result.DaysAdvanced}.");
                    break;
                case "batch":
                    RequireOptions(options, ["--seeds", "--policies"]);
                    var count = IntegerOption(options, "--seeds", 200, positive: true);
                    var selectedPolicies = options.GetValueOrDefault("--policies", string.Join(',', Policies)).Split(',');
                    foreach (var selected in selectedPolicies)
                        RequirePolicy(selected);
                    if (selectedPolicies.Distinct(StringComparer.Ordinal).Count() != selectedPolicies.Length)
                        throw new UsageException("--policies must not contain duplicates.");
                    var batchScenario = ScenarioLoader.Load(args[1]);
                    WritePlaceholder(output);
                    WriteValidationMode(batchScenario, output);
                    output.WriteLine($"Scenario: {batchScenario.Id}; seeds: 0 through {count - 1}.");
                    output.WriteLine("Outcome spread (days advanced only; mission scores unavailable):");
                    output.WriteLine("| Policy | Runs | Min days | Mean days | Max days | Mission outcome |");
                    output.WriteLine("| --- | --- | --- | --- | --- | --- |");
                    foreach (var selected in selectedPolicies)
                    {
                        var minimum = int.MaxValue;
                        var maximum = 0;
                        long total = 0;
                        for (var currentSeed = 0; currentSeed < count; currentSeed++)
                        {
                            var days = PlaceholderRun.Advance(batchScenario, currentSeed, selected).DaysAdvanced;
                            minimum = Math.Min(minimum, days);
                            maximum = Math.Max(maximum, days);
                            total += days;
                        }
                        var mean = ((decimal)total / count).ToString("0.00", CultureInfo.InvariantCulture);
                        output.WriteLine($"| {selected} | {count} | {minimum} | {mean} | {maximum} | not simulated |");
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
        for (var i = 2; i < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length ||
                args[i + 1].StartsWith("--", StringComparison.Ordinal))
                throw new UsageException($"Expected an option followed by a value at '{args[i]}'.");
            if (!options.TryAdd(args[i], args[i + 1]))
                throw new UsageException($"Option '{args[i]}' was supplied more than once.");
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

    private static void WritePlaceholder(TextWriter output) =>
        output.WriteLine("M0 PLACEHOLDER: all policies do nothing; seeds have no effect; no mission outcomes are simulated.");

    private static void WriteValidationMode(LoadedScenario scenario, TextWriter output) =>
        output.WriteLine(scenario.UsedSchemas
            ? "Validation: repository JSON Schemas and harness input checks."
            : "Validation: fallback id/days/sites checks only; M0-04 schemas are not available yet.");

    private sealed class UsageException(string message) : Exception(message);
}
