using System.Text.Json;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// M3-01: a save is the input log plus the day about to be played. Resuming it must land on the same state the
/// player left, and continuing must reach the same end as a game that was never saved.
/// </summary>
public class SaveResumeTests
{
    private static readonly string[] Scenarios = ["spoke-b", "spoke-b-90"];

    public static IEnumerable<object[]> ApproachLogs()
        => Scenarios.SelectMany(sc => Directory.GetFiles(Path.Combine(Repo.Scenario(sc), "golden"), "approach*.json")
                    .OrderBy(p => p, StringComparer.Ordinal).Select(p => new object[] { sc, Path.GetFileName(p) }));

    [Theory]
    [MemberData(nameof(ApproachLogs))]
    public void Every_golden_saved_on_every_day_resumes_to_the_same_state_and_end(string scenario, string file)
    {
        var bundle = Repo.Load(scenario);
        var log = InputLog.Load(Path.Combine(Repo.Scenario(scenario), "golden", file));
        var endHash = Replay.Run(bundle, log).StateHash();
        CheckEveryDay(bundle, log, endHash);
    }

    [Theory]
    [InlineData("spoke-b", "textbook")]
    [InlineData("spoke-b", "greedy")]
    [InlineData("spoke-b-90", "textbook")]
    [InlineData("spoke-b-90", "greedy")]
    public void Policy_games_saved_mid_game_resume_to_the_same_end(string scenario, string policy)
    {
        var bundle = Repo.Load(scenario);
        for (long seed = 1; seed <= 20; seed++)
        {
            var played = Replay.Run(bundle, seed, PolicyCatalog.Create(policy));
            var log = played.ToInputLog();
            var endHash = played.StateHash();
            foreach (var day in new[] { 1, 2, bundle.Scenario.Days / 3, bundle.Scenario.Days / 2, bundle.Scenario.Days - 1, bundle.Scenario.Days })
            {
                var live = PlayTo(bundle, log, day);
                var resumed = Replay.Resume(bundle, RoundTrip(live.ToSave()));
                Assert.Equal(live.StateHash(), resumed.StateHash());
                Assert.Equal(endHash, Continue(resumed, log).StateHash());
            }
        }
    }

    [Fact]
    public void A_finished_game_saves_without_a_day_and_resumes_finished()
    {
        var bundle = Repo.Load("spoke-b");
        var played = Replay.Run(bundle, 3, PolicyCatalog.Create("textbook"));
        var save = played.ToSave();
        Assert.Null(save.SavedDay);
        Assert.DoesNotContain("savedDay", save.ToJson());
        var resumed = Replay.Resume(bundle, RoundTrip(save));
        Assert.True(resumed.Finished);
        Assert.Equal(played.StateHash(), resumed.StateHash());
    }

    [Fact]
    public void A_mid_game_save_writes_its_day_and_the_actions_applied_today()
    {
        var bundle = Repo.Load("spoke-b");
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach2-fix-scope-and-bridge.json"));
        var firstDay = log.Actions.Min(a => a.Day);
        var live = PlayTo(bundle, log, firstDay);
        var save = live.ToSave();
        Assert.Equal(firstDay, save.SavedDay);
        Assert.Contains("\"savedDay\"", save.ToJson());
        Assert.Equal(log.Actions.Count(a => a.Day <= firstDay), save.Actions.Count);
        Assert.Equal(save.ToJson(), Replay.Resume(bundle, RoundTrip(save)).ToSave().ToJson());
    }

    [Fact]
    public void Saves_that_cannot_be_this_game_are_refused()
    {
        var bundle = Repo.Load("spoke-b");
        var live = PlayTo(bundle, InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach1-as-written.json")), 5);
        var save = live.ToSave();
        Assert.Throws<InvalidDataException>(() => Replay.Resume(bundle, save with { SavedDay = 0 }));
        Assert.Throws<InvalidDataException>(() => Replay.Resume(bundle, save with { SavedDay = bundle.Scenario.Days + 1 }));
        Assert.Throws<InvalidDataException>(() => Replay.Resume(bundle, save with { Scenario = "spoke-b-90" }));
        Assert.Throws<InvalidDataException>(() => Replay.Resume(bundle, save with { RulesVersion = "rules-1999-01" }));
        var late = new SurveyAction { Day = 6, Island = bundle.Scenario.Islands.First(i => !i.IsHub).Id };
        Assert.Throws<InvalidDataException>(() => Replay.Resume(bundle, save with { Actions = [.. save.Actions, late] }));
    }

    // ---------------------------------------------------------------- helpers

    private static void CheckEveryDay(ScenarioBundle bundle, InputLog log, string endHash)
    {
        for (var day = 1; day <= bundle.Scenario.Days; day++)
        {
            var live = PlayTo(bundle, log, day);
            var save = RoundTrip(live.ToSave());
            Assert.Equal(day, save.SavedDay);
            var resumed = Replay.Resume(bundle, save);
            Assert.Equal(day, resumed.Day);
            Assert.Equal(live.StateHash(), resumed.StateHash());
            Assert.Equal(live.ToSave().ToJson(), resumed.ToSave().ToJson());
            Assert.Equal(endHash, Continue(resumed, log).StateHash());
        }
    }

    /// <summary>The live game on <paramref name="day"/>: earlier days played, that day's actions applied, not advanced.</summary>
    private static Session PlayTo(ScenarioBundle bundle, InputLog log, int day)
    {
        var s = Replay.Run(bundle, log, stopAfterDay: day - 1);
        foreach (var a in log.Actions.Where(a => a.Day == day)) s.Apply(a);
        return s;
    }

    /// <summary>Finishes a resumed game with the original log's later actions, as the player would have.</summary>
    private static Session Continue(Session s, InputLog log)
    {
        var resumeDay = s.Day;
        var byDay = log.Actions.Where(a => a.Day > resumeDay).GroupBy(a => a.Day).ToDictionary(g => g.Key, g => g.ToList());
        while (!s.Finished)
        {
            if (s.Day > resumeDay && byDay.TryGetValue(s.Day, out var today))
                foreach (var a in today) s.Apply(a);
            s.AdvanceDay();
        }
        return s;
    }

    private static InputLog RoundTrip(InputLog log)
        => JsonSerializer.Deserialize<InputLog>(log.ToJson(), Json.Options) ?? throw new InvalidDataException("round trip failed");
}
