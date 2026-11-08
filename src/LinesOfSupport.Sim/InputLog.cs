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

    /// <summary>
    /// For a save taken mid-game: the day about to be played when it was written. Every action dated that day
    /// has already been applied and the day has not been advanced. Absent (null) means a finished game or a plain
    /// input log, which plays to the end (M3-01, ADR-0005 addendum).
    /// </summary>
    public int? SavedDay { get; init; }

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

    /// <summary>
    /// Loads a save: replays the log up to <see cref="InputLog.SavedDay"/>, applies that day's logged actions in order,
    /// and returns the session on that day, not yet advanced. A log without SavedDay plays to the end, as
    /// <see cref="Run(ScenarioBundle, InputLog, int?)"/> does. Saving the returned session again gives the same log.
    /// </summary>
    public static Session Resume(ScenarioBundle bundle, InputLog log)
    {
        if (log.SavedDay is not int saved) return Run(bundle, log);
        if (saved < 1 || saved > bundle.Scenario.Days)
            throw new InvalidDataException($"save is on day {saved}, but {bundle.Scenario.Id} runs days 1 to {bundle.Scenario.Days}");
        var late = log.Actions.FirstOrDefault(a => a.Day > saved);
        if (late is not null)
            throw new InvalidDataException($"save is on day {saved} but holds an action dated day {late.Day}");
        var session = Run(bundle, log, stopAfterDay: saved - 1);
        foreach (var a in log.Actions.Where(a => a.Day == saved)) session.Apply(a);
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
