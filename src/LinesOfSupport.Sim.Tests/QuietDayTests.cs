using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-75 (docs/design/desk-redesign.md D30): a quiet day, and what stops a skip. Checked on every day of the three
/// 17 Nov review plays, each day sampled after that day's logged actions, when the player would end it.
/// </summary>
public class QuietDayTests
{
    private static InputLog Log(string name)
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-17-review", name + "-input-log.json")) with { SavedDay = null };

    /// <summary>Each day of the play after its logged actions, to the end of the scenario.</summary>
    private static IEnumerable<Session> Days(string log, string scenario)
    {
        var l = Log(log);
        var s = Session.Create(Repo.Load(scenario), l.Seed, l.PrepChoices, l.Team);
        while (!s.Finished)
        {
            foreach (var a in l.Actions.Where(a => a.Day == s.Day)) s.Apply(a);
            yield return s;
            s.AdvanceDay();
        }
    }

    public static TheoryData<string, string> Plays => new()
    {
        { "tutorial-careful", "tutorial" }, { "tutorial-hasty", "tutorial" }, { "spoke-b", "spoke-b" },
    };

    [Theory]
    [MemberData(nameof(Plays))]
    public void A_quiet_day_has_nothing_open_and_a_skip_that_ends(string log, string scenario)
    {
        foreach (var s in Days(log, scenario))
        {
            var quiet = s.View.QuietDay();
            var stops = s.View.SkipStops();
            Assert.Equal(quiet, stops is not null);
            if (!quiet) continue;
            Assert.Empty(s.View.MorningBoard().New);
            Assert.DoesNotContain(s.View.WhereThingsStand(), r => r.State is RequestStanding.Nothing or RequestStanding.Asking or RequestStanding.Short);
            Assert.DoesNotContain(s.View.MorningBoard().Items, i => i.Severity == BoardSeverity.Alert);
            Assert.InRange(stops!.UntilDay, s.Day + 1, Math.Min(s.Day + 7, s.LastDay));
            Assert.StartsWith("Stops when anything needs you, and ", stops.Sentence);
            Assert.DoesNotContain(s.View.EndOfDayCheck(), x => x.Contains(" and nobody is on ", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void The_careful_tutorial_play_has_short_quiet_stretches_and_the_dining_slips_end_them()
    {
        // DESK-80 (D31): the tutorial ends on day 16 and the dining hall slips on days 11 and 12, so the week of quiet
        // days this play once had is gone: day 12 has the slip's news and from day 13 the cure notice decision waits
        // (this log was played before the slips existed, so it never makes it).
        var quiet = Days("tutorial-careful", "tutorial").Where(s => s.View.QuietDay()).Select(s => s.Day).ToList();
        Assert.Equal([6, 10, 11], quiet);
    }

    [Fact]
    public void The_sentence_names_the_next_day_the_shop_waits_on()
    {
        var careful = Days("tutorial-careful", "tutorial").ToDictionary(s => s.Day, s => s.View.SkipStops());
        Assert.Equal(new SkipStopsView("Stops when anything needs you, and on day 8 after material handling equipment at Hub from Main Island Utilities (host nation) ends.", 8), careful[6]);
        Assert.Equal(new SkipStopsView("Stops when anything needs you, and on day 13 after waste removal at Hub from Main Island Utilities (host nation) ends.", 13), careful[10]);
        Assert.Equal(new SkipStopsView("Stops when anything needs you, and on day 13 when Main Island Sanitation starts waste removal at Hub.", 13), careful[11]);

        var spokeB = Days("spoke-b", "spoke-b").ToDictionary(s => s.Day, s => s.View.SkipStops());
        Assert.Equal(new SkipStopsView("Stops when anything needs you, and on day 11 when Third Island Water Works starts water at Spoke C.", 11), spokeB[8]);
        Assert.Equal(new SkipStopsView("Stops when anything needs you, and after 7 days at most, on day 21.", 21), spokeB[14]);
    }

    [Fact]
    public void A_day_is_not_quiet_with_news_an_uncovered_request_a_decision_or_idle_hands()
    {
        var hasty = Days("tutorial-hasty", "tutorial").ToDictionary(s => s.Day, s => (Quiet: s.View.QuietDay(), Check: s.View.EndOfDayCheck()));
        Assert.False(hasty[2].Quiet);    // news this morning
        Assert.False(hasty[5].Quiet);    // the forklift request has nothing on it
        Assert.Contains("SrA Eli Novak has nothing to work on today, and nobody is on material handling equipment for Hub; their 8 hours go to walk-in small buys.", hasty[5].Check);
        Assert.False(hasty[7].Quiet);    // Novak idle while Avery's work carries over
        Assert.Contains("SrA Eli Novak has nothing to work on today, and MSgt Joan Avery's work carries over; their 8 hours go to walk-in small buys.", hasty[7].Check);
        Assert.False(hasty[14].Quiet);   // a vendor problem waits for the cure notice or re-source decision
        Assert.False(hasty[16].Quiet);   // the last day: End the day ends the game
    }

    [Fact]
    public void Asking_changes_nothing()
    {
        var s = Days("spoke-b", "spoke-b").First(x => x.Day == 8);
        var before = s.StateHash();
        _ = s.View.QuietDay();
        _ = s.View.SkipStops();
        _ = s.View.EndOfDayCheck();
        Assert.Equal(before, s.StateHash());
    }
}
