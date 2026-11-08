using System.Text.Json;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim;

/// <summary>
/// A replayable game: scenario, rules version, seed, prep choices and every action with its day.
/// Saves are input logs (ADR-0002, ADR-0005). Replaying one through the same rules gives the same result.
/// </summary>
public sealed record InputLog
{
    public string Scenario { get; init; } = "";
    public string RulesVersion { get; init; } = "";
    public long Seed { get; init; }
    public string? Note { get; init; }
    public List<string> PrepChoices { get; init; } = [];
    public List<PlayerAction> Actions { get; init; } = [];

    public static InputLog Load(string path)
        => JsonSerializer.Deserialize<InputLog>(File.ReadAllText(path), Json.Options)
           ?? throw new InvalidDataException($"{path} is not an input log");

    public string ToJson() => JsonSerializer.Serialize(this, Json.Options);
}

public static class Replay
{
    /// <summary>Plays a log to the end of the scenario. Actions are applied on their day, in log order.</summary>
    public static Session Run(ScenarioBundle bundle, InputLog log, int? stopAfterDay = null)
    {
        if (log.Scenario != bundle.Scenario.Id)
            throw new InvalidDataException($"log is for scenario {log.Scenario}, bundle is {bundle.Scenario.Id}");
        if (log.RulesVersion != bundle.Rules.RulesVersion)
            throw new InvalidDataException($"log was played under rules {log.RulesVersion}, bundle has {bundle.Rules.RulesVersion}");
        var session = Session.Create(bundle, log.Seed, log.PrepChoices);
        var byDay = log.Actions.GroupBy(a => a.Day).ToDictionary(g => g.Key, g => g.ToList());
        var last = Math.Min(stopAfterDay ?? session.LastDay, session.LastDay);
        while (!session.Finished && session.Day <= last)
        {
            if (byDay.TryGetValue(session.Day, out var today))
                foreach (var a in today) session.Apply(a);
            session.AdvanceDay();
        }
        return session;
    }

    /// <summary>Plays a policy to the end of the scenario and returns the session (its input log is ToInputLog()).</summary>
    public static Session Run(ScenarioBundle bundle, long seed, Policies.IPolicy policy)
    {
        var session = Session.Create(bundle, seed, policy.PrepChoices(bundle));
        while (!session.Finished)
        {
            foreach (var a in policy.Decide(session.View).ToList()) session.Apply(a);
            session.AdvanceDay();
        }
        return session;
    }
}
