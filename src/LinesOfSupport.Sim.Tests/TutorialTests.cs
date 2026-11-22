using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-08: the tutorial scenario (skeleton 2, docs/design/opening-and-guidance.md 8).</summary>
public class TutorialTests
{
    private static Session Taught() => Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Scenario("tutorial"), "golden", "approach1-taught.json")));

    [Fact]
    public void It_is_a_tutorial_with_a_cast_a_briefing_and_a_turnover()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        Assert.True(s.Scenario.Tutorial);
        Assert.Equal(16, s.LastDay);   // DESK-80 (D31): it ends when its lessons do
        var turnover = s.View.Turnover();
        Assert.Equal("Capt Jordan Mills", turnover.From!.DisplayName);
        Assert.Contains("Ortiz", turnover.LastLine);
        Assert.Equal(4, turnover.Sections.Count);
    }

    [Theory]
    [InlineData(1, "MSgt Carla Ortiz")]
    [InlineData(4, "SMSgt Ray Dunn")]
    [InlineData(9, "Capt Ana Ruiz")]
    public void On_the_taught_path_each_request_leads_the_morning_it_arrives(int day, string who)
    {
        var log = InputLog.Load(Path.Combine(Repo.Scenario("tutorial"), "golden", "approach1-taught.json"));
        var s = Session.Create(Repo.Load("tutorial"), log.Seed, log.PrepChoices);
        while (s.Day < day)
        {
            foreach (var a in log.Actions.Where(a => a.Day == s.Day)) Assert.True(s.Apply(a).Accepted);
            s.AdvanceDay();
        }
        var lead = s.View.MorningBoard().Lead!;
        Assert.Equal(LeadKind.Decision, lead.Kind);
        Assert.Equal(BoardLinkKind.Card, lead.LinkKind);
        Assert.Equal(who, lead.Person!.DisplayName);
    }

    [Fact]
    public void The_mentor_speaks_on_the_first_two_nights_and_then_is_gone()
    {
        // D44 (DESK-144): each note reaches the player the morning before the choice it is about; the card-or-cash note
        // is on the turnover's last page, so the mentor's inbox notes are the nights of days 1 and 2.
        var s = Session.Create(Repo.Load("tutorial"), 1);
        var mentor = s.Scenario.People.Single(p => p.Role == "outgoingCco").Id;
        var notes = s.Scenario.Events.Where(e => e.From == mentor).ToList();
        Assert.Equal(3, notes.Count);   // DESK-207: the staff meeting note is its own, sent only to a shop not joining
        Assert.All(notes, e => Assert.InRange(e.Window.LatestDay, 1, 2));
        var money = s.View.Turnover().Sections.Single(x => x.Kind == TurnoverSectionKind.Money).Text;
        Assert.Contains("Small buys go on the card if the vendor takes it, up to $20,000 for one buy; Island Mart does.", money);
        Assert.Contains("the paying agent can't buy what isn't in it.", money);
        while (!s.Finished) s.AdvanceDay();
        Assert.Equal(3, s.Records.Count(r => r.Kind == RecordKind.Event && notes.Any(e => e.Id == r.Subject)));
    }

    [Fact]
    public void The_taught_path_covers_everything_and_meets_every_pass_criterion()
    {
        var s = Taught();
        Assert.Empty(s.View.Debrief().Gaps);
        foreach (var c in s.Scenario.PassCriteria)
        {
            var v = s.Metrics[c.Metric];
            Assert.True(c.Comparator switch { "<=" => v <= c.Value, ">=" => v >= c.Value, "==" => v == c.Value, _ => false }, $"{c.Label}: {v}");
        }
    }

    [Fact]
    public void Harbor_turns_the_forklift_call_down_without_a_roll_and_owes_nothing()
    {
        foreach (var seed in new long[] { 1, 2, 3, 7, 11 })
        {
            var s = Session.Create(Repo.Load("tutorial"), seed);
            while (s.Day < 4) s.AdvanceDay();
            Assert.True(s.Apply(new AwardAction
            {
                Id = "call", Instrument = "bpaCall", Vendor = "harborEquipment", Vehicle = "equipBpa", Card = "tutForklift",
                Quantity = 3, StartDay = 8, DurationDays = 13, FundingLine = "om1", Assignee = "avery",
            }).Accepted);
            // Without the system-access prep the call is written by hand: MSgt Avery needs two days for it (DESK-02).
            for (var i = 0; i < 3 && s.Arrangements.All(a => a.Id != "call"); i++) s.AdvanceDay();
            var call = s.Arrangements.Single(a => a.Id == "call");
            Assert.Equal(ArrangementStatus.Declined, call.Status);
            Assert.Equal(0, call.ObligatedCents);
            Assert.Contains(s.Records, r => r.Kind == RecordKind.Declined && r.Text.StartsWith("Harbor Equipment Co declined the BPA call", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void After_the_window_Harbor_takes_calls_again()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        while (s.Day < 12) s.AdvanceDay();
        s.Apply(new AwardAction
        {
            Id = "late", Instrument = "bpaCall", Vendor = "harborEquipment", Vehicle = "equipBpa", Card = "tutForklift",
            Quantity = 1, StartDay = 14, DurationDays = 5, FundingLine = "om1", Assignee = "avery",
        });
        for (var i = 0; i < 3 && s.Arrangements.All(a => a.Id != "late"); i++) s.AdvanceDay();
        // The 4% to 8% aggregate decline roll still applies; seed 1 does not decline here.
        Assert.NotEqual(ArrangementStatus.Declined, s.Arrangements.Single(a => a.Id == "late").Status);
    }
}
