using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-41 (docs/design/desk-redesign.md D21): your decisions in one list. Nick on his 13 Nov replay: "I don't know
/// what decisions I've made easily."</summary>
public class DecisionsTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    [Fact]
    public void A_buy_reads_as_what_where_who_and_how_it_stands_and_follows_it_to_running()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.Empty(s.View.Decisions());
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 7, FundingLine = "om1",
        }).Accepted);
        var d = Assert.Single(s.View.Decisions());
        Assert.Equal("Day 1: Water at Hub from Island Mart, 4500 gal/day for 7 days on a government purchase card.", d.Sentence);
        Assert.Equal("tutWater", d.CardId);
        Assert.Equal("Water for Hub", d.Request);
        Assert.Equal("Arranged: starts on day 2.", d.Now);
        s.AdvanceDay();
        s.AdvanceDay();
        Assert.Equal("Running through day 8.", s.View.Decisions().Single().Now);
    }

    [Fact]
    public void Work_in_progress_says_who_has_it_and_when_it_should_be_done_newest_first()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 3) s.AdvanceDay();
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "sanitation", Assignee = "avery" }).Accepted);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "purchaseOrder", Vendor = "harborBottling", Card = "tutWater", Quantity = 4500,
            DurationDays = 6, FundingLine = "om1", Assignee = "novak",
        }).Accepted);
        Assert.False(s.Apply(new MeetVendorAction { Vendor = "no-such-vendor" }).Accepted);
        var all = s.View.Decisions();
        Assert.Equal(2, all.Count);
        var buy = all[0];
        Assert.Equal("novak", buy.TeammateId);
        Assert.EndsWith("on a purchase order, given to SrA Eli Novak.", buy.Sentence);
        Assert.Matches(@"^Waiting until day \d+, then about [\d.]+ hours of work: done about day \d+\.$", buy.Now);
        var visit = all[1];
        Assert.StartsWith("Day 3: Sitting down with ", visit.Sentence);
        Assert.EndsWith(", given to MSgt Joan Avery.", visit.Sentence);
        Assert.Equal($"In work: done about day {s.Day}.", visit.Now);
        Assert.Single(s.View.Decisions("tutWater"));
        s.AdvanceDay();
        Assert.Equal("Done on day 3.", s.View.Decisions()[1].Now);
    }

    public static IEnumerable<object[]> Scenarios() => [["spoke-b"], ["spoke-b-90"], ["sustainment"], ["tutorial"]];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Every_accepted_action_of_every_golden_is_a_decision_in_plain_words(string scenario)
    {
        var bundle = Repo.Load(scenario);
        var ids = bundle.Scenario.Sites.Select(x => x.Id).Concat(bundle.Scenario.Vendors.Select(v => v.Id)).ToHashSet();
        foreach (var path in Directory.GetFiles(Path.Combine(Repo.Scenario(scenario), "golden"), "*.json").Order(StringComparer.Ordinal))
        {
            var log = InputLog.Load(path);
            var s = Replay.Run(bundle, log);
            var hash = s.StateHash();
            var all = s.View.Decisions();
            Assert.Equal(hash, s.StateHash());
            Assert.True(all.Count <= s.AppliedRejected.Count(r => !r));
            Assert.Equal(all.OrderByDescending(x => x.Day).Select(x => x.Day), all.Select(x => x.Day));
            foreach (var d in all)
            {
                foreach (var x in new[] { d.Sentence, d.Now })
                {
                    Assert.True(char.IsUpper(x[0]), $"{scenario}/{Path.GetFileName(path)}: {x}");
                    Assert.EndsWith(".", x, StringComparison.Ordinal);
                    Assert.DoesNotContain("..", x, StringComparison.Ordinal);
                    Assert.DoesNotContain("  ", x, StringComparison.Ordinal);
                    Assert.DoesNotContain(x.Split(' ', ',', '.', ':', ';'), w => ids.Contains(w));
                }
            }
            foreach (var card in all.Where(d => d.CardId is not null).Select(d => d.CardId!).Distinct())
                Assert.Equal(all.Where(d => d.CardId == card), s.View.Decisions(card));
        }
    }
}
