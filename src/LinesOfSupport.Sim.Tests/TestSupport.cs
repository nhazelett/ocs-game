using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Tests;

internal static class Repo
{
    private static readonly Lazy<string> RootLazy = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios", "rules")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repository root (with scenarios/rules) not found above the test binaries");
    });

    public static string Root => RootLazy.Value;
    public static string Scenario(string id) => Path.Combine(Root, "scenarios", id);
    public static ScenarioBundle Load(string id) => ScenarioBundle.Load(Scenario(id));
}

/// <summary>
/// DESK-12: plays logged before Spoke B had its team were played with one pool of 16 hours. Replaying one under the team,
/// each logged action nobody was given goes, as the player would divide it, to whoever on the team has the least work
/// queued for their hours (ties to the first listed). Logs that name an assignee keep it. Only for those older play logs.
/// </summary>
internal static class Divided
{
    public static Session Replay(ScenarioBundle bundle, InputLog log, int? stopAfterDay = null)
    {
        if (log.Scenario is not ("spoke-b" or "spoke-b-90")) return LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay);
        var session = Session.Create(bundle, log.Seed, log.PrepChoices, log.Team);
        var byDay = log.Actions.GroupBy(a => a.Day).ToDictionary(g => g.Key, g => g.ToList());
        var last = Math.Min(stopAfterDay ?? session.LastDay, session.LastDay);
        while (!session.Finished && session.Day <= last)
        {
            if (byDay.TryGetValue(session.Day, out var today))
                foreach (var a in today) session.Apply(Assign(session, a));
            session.AdvanceDay();
        }
        return session;
    }

    /// <summary>The logged action as the player would hand it over on a pre-team Spoke B log; any other log's as logged.</summary>
    public static PlayerAction Assign(Session s, PlayerAction a)
        => a.Assignee is not null || s.Scenario.Id is not ("spoke-b" or "spoke-b-90") ? a
            : a with { Assignee = s.View.Team.OrderBy(m => (m.HoursQueued + m.HoursSpokenForToday) / Math.Max(1, m.HoursPerDay)).First().Id };
}
