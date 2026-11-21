using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using System.Text.RegularExpressions;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-165 (Claude play 21 Nov, fifth play, notes 39, 40): the debrief closes the threads the player started. A
/// failure on a buy picked fast and good, or cheap and good, says what the pick did to the odds; each cure notice the player
/// issued says where it stands at the end; one sentence says what the spare hours did for the walk-in small buys.</summary>
public class DebriefThreadsTests
{
    private static Session FifthPlay()
        => Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-5", "input-log.json")));

    private static AwardAction PowerPo(params string[] pick) => new()
    {
        Id = "po", Instrument = "purchaseOrder", Vendor = "vendorA", Site = "spokeBSite", Capability = "power",
        Lines = ["generation", "maintenance"], Quantity = 60, DurationDays = 20, FundingLine = "om1", Priorities = [.. pick],
    };

    [Fact]
    public void The_fifth_play_hears_what_fast_and_good_did_what_its_cure_notice_did_and_the_walk_in_buys()
    {
        var d = FifthPlay().View.Debrief();
        var fail = d.Gaps.SelectMany(g => g.Factors).Single(f => f.Kind == GapFactorKind.VendorFailure);
        Assert.Contains("Fast and good kept Main Island Sanitation at its usual odds of failing; fast and cheap would have raised them.", fail.Text);
        Assert.DoesNotContain("first quote", fail.Text);
        Assert.Contains(fail.Text.Trim(), d.Summary!.Text);   // still quoted word for word, so Quoted stays right
        Assert.EndsWith(" Your cure notice to Island Camp Services runs to day 23; food service at Hub was never short after it."
                        + " Spare shop hours went to 33 walk-in small buys for CE; none waited long enough for CE to go to the commander.", d.Summary.Text);
    }

    [Theory]
    [InlineData("cheap", "good", "Cheap and good kept ")]
    [InlineData("fast", "good", "Fast and good kept ")]
    [InlineData("fast", "cheap", "You took the first quote (fast and cheap)")]
    public void A_failure_on_a_picked_buy_says_what_the_pick_did_to_the_odds(string one, string two, string said)
    {
        for (var seed = 1; seed <= 60; seed++)
        {
            var s = Session.Create(Repo.Load("spoke-b"), seed);
            Assert.True(s.Apply(PowerPo(one, two)).Accepted);
            while (s.Day < 26) s.AdvanceDay();
            var po = s.Arrangements.Single(a => a.Id == "po");
            var fails = s.View.Debrief().Gaps.SelectMany(g => g.Factors)
                         .Where(f => f.Kind == GapFactorKind.VendorFailure && f.Text.StartsWith(Upper(s.View.NameOf(po.Id)), StringComparison.Ordinal)).ToList();
            if (fails.Count == 0) continue;
            Assert.All(fails, f => Assert.Contains(said, f.Text));
            Assert.All(fails, f => Assert.Single(new[] { "kept ", "first quote" }, w => f.Text.Contains(w, StringComparison.Ordinal)));
            return;
        }
        Assert.Fail("no seed in 60 had the purchase order fail");
    }

    [Theory]
    [InlineData("sustainment", "textbook")]
    [InlineData("sustainment", "approach3-cure-bfuel.json")]
    [InlineData("spoke-b", "textbook")]
    [InlineData("spoke-b-90", "approach1-cure.json")]
    public void Each_cure_notice_the_player_issued_closes_in_one_sentence(string scenario, string run)
    {
        var bundle = Repo.Load(scenario);
        var s = run.EndsWith(".json", StringComparison.Ordinal)
            ? Replay.Run(bundle, InputLog.Load(Path.Combine(Repo.Scenario(scenario), "golden", run)))
            : Replay.Run(bundle, 1, PolicyCatalog.Create(run));
        var text = s.View.Debrief().Summary!.Text;
        var noticed = s.PerformanceIssues.Where(i => i.CureNoticeDay is >= 1).ToList();
        Assert.NotEmpty(noticed);
        foreach (var i in noticed)
        {
            var vendor = s.View.NameOf(i.VendorId!);
            var a = s.Arrangements.Single(x => x.Id == i.ArrangementId);
            var opening = i.Outcome switch
            {
                CureOutcome.None => $"Your cure notice to {vendor} runs to day {i.CureEndsDay}",
                CureOutcome.Cured when a.EndDay < i.CureEndsDay => $"Your cure notice to {vendor} closed with its contract, which ended on day {a.EndDay}",
                CureOutcome.Cured => $"{vendor} cured after your cure notice and was back to standard on day {i.CureEndsDay}",
                _ => $"{vendor} did not cure after your cure notice and was in default from day {i.CureEndsDay}",
            };
            Assert.Contains(opening, text);
        }
        Assert.Equal(noticed.Count, Regex.Matches(text, "your cure notice", RegexOptions.IgnoreCase).Count);
    }

    [Fact]
    public void No_cure_notice_no_sentence_about_one()
    {
        var s = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Scenario("tutorial"), "golden", "approach1-taught.json")));
        Assert.Contains(s.PerformanceIssues, i => i.CureNoticeDay is null);
        Assert.DoesNotContain("cure notice", s.View.Debrief().Summary!.Text);
    }

    [Fact]
    public void The_walk_in_buys_say_when_they_reached_the_commander()
    {
        var bundle = Repo.Load("sustainment");
        var s = Replay.Run(bundle, 1, PolicyCatalog.Create("textbook"));
        var days = s.Records.Where(r => r.Kind == RecordKind.Compliance && r.Subject == "smallBuys").Select(r => r.Day).ToList();
        Assert.NotEmpty(days);
        var worked = (int)s.Metrics["shop.smallBuys"];
        var when = days.Count == 1 ? $"day {days[0]}" : null;
        var sentence = $"Spare shop hours went to {worked} walk-in small buys for CE; CE went to the commander about the ones waiting on ";
        Assert.Contains(sentence, s.View.Debrief().Summary!.Text);
        if (when is not null) Assert.Contains(sentence + when + ".", s.View.Debrief().Summary!.Text);
    }

    private static string Upper(string t) => t.Length == 0 ? t : char.ToUpperInvariant(t[0]) + t[1..];
}
