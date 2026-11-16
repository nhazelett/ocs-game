using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-58 (docs/design/desk-redesign.md D25): asking is worth it, and the debrief says who knew. The 16 Nov Claude play
/// (docs/reviews/2026-11-16-claude-play.md, notes 14 and 27) asked Ortiz and Ruiz first and heard "Nothing I can add",
/// was told only "Without operators, it fails more often" on the order form, and was then told the forklift gap was
/// "in front of you" when only SMSgt Dunn knew and nobody had asked him.
/// </summary>
public class WhoKnewTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static string AskAndHear(string card, string person, int day)
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < day) s.AdvanceDay();
        Assert.True(s.Apply(new AskAboutAction { Card = card, Person = person }).Accepted);
        s.AdvanceDay();
        return s.Records.Last(r => r.Kind == RecordKind.Visit).Text;
    }

    [Fact]
    public void An_ask_that_finds_nothing_missing_says_what_it_confirmed()
    {
        var water = AskAndHear("tutWater", "ortiz", 1);
        Assert.Contains("Bottled water is all we need; nobody has to run anything for it.", water, StringComparison.Ordinal);
        Assert.DoesNotContain("Nothing I can add", water, StringComparison.Ordinal);

        var waste = AskAndHear("tutWaste", "ruizT", 9);
        Assert.Contains("whoever does it brings their own trucks and crews", waste, StringComparison.Ordinal);
    }

    [Fact]
    public void Somebody_with_nothing_written_for_them_says_plainly_that_nothing_is_missing()
    {
        // The water request has no confirms line for Col Ward.
        var said = AskAndHear("tutWater", "ward", 1);
        Assert.EndsWith("\"As far as I know, nothing is missing from the request for water for Hub.\"", said, StringComparison.Ordinal);
    }

    private static AwardAction Forklifts => new()
    {
        Id = "lifts", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3,
        StartDay = 8, DurationDays = 13, FundingLine = "om1",
    };

    [Fact]
    public void The_order_form_names_who_to_ask_about_a_part_the_request_leaves_out_until_they_are_asked()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 4) s.AdvanceDay();
        var before = s.View.Preview(Forklifts).SoftGaps;
        Assert.Contains("Without operators, it fails more often every day it runs. SMSgt Ray Dunn asked for this: ask about operators before you buy.", before);

        Assert.True(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn" }).Accepted);
        s.AdvanceDay();
        var after = s.View.Preview(Forklifts).SoftGaps;
        Assert.DoesNotContain(after, g => g.Contains("asked for this", StringComparison.Ordinal));
    }

    private static GapFactor ForkliftFailure(Session s)
        => s.View.Debrief().Gaps.Where(g => g.CapabilityId == "mhe")
            .SelectMany(g => g.Factors).First(f => f.Kind == GapFactorKind.VendorFailure);

    [Fact]
    public void A_failure_from_a_part_only_someone_else_knew_is_discoverable_and_names_them()
    {
        var greedy = Replay.Run(Repo.Load("tutorial"), 1, PolicyCatalog.Create("greedy"));
        var f = ForkliftFailure(greedy);
        Assert.Equal(GapTag.Discoverable, f.Tag);
        Assert.Contains("SMSgt Ray Dunn knew it needed operators, and nobody asked.", f.Text, StringComparison.Ordinal);
        Assert.Contains("Asking about the request for material handling equipment for Hub before the buy", f.Text, StringComparison.Ordinal);
        var gap = greedy.View.Debrief().Gaps.First(g => g.CapabilityId == "mhe");
        Assert.Equal(GapTag.Discoverable, gap.Tag);
    }

    [Fact]
    public void Asked_and_still_left_out_stays_avoidable()
    {
        var bundle = Repo.Load("tutorial");
        var greedy = Replay.Run(bundle, 1, PolicyCatalog.Create("greedy"));
        var log = greedy.ToSave();
        var buy = log.Actions.Select((a, i) => (a, i)).First(x => x.a is AwardAction { Card: "tutForklift" });
        // Dunn is asked the day the buy goes in; he answers well before the forklifts fail, and the buy still leaves the
        // drivers out.
        var asked = Counterfactual.Run(bundle, log,
            CounterfactualChange.Insert(buy.a.Day, new AskAboutAction { Card = "tutForklift", Person = "dunn" }, buy.i));
        var failures = asked.Alternative.View.Debrief().Gaps.Where(g => g.CapabilityId == "mhe")
            .SelectMany(g => g.Factors).Where(f => f.Kind == GapFactorKind.VendorFailure).ToList();
        Assert.NotEmpty(failures);
        Assert.All(failures, f =>
        {
            Assert.Equal(GapTag.Avoidable, f.Tag);
            Assert.DoesNotContain("nobody asked", f.Text, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void A_part_nobody_knew_about_keeps_its_old_tag()
    {
        // Spoke B 90's water request at Spoke C has no knownBy, so its failures read as before (spoke-b-90 has one card
        // with knownBy, the power request).
        foreach (var scenario in new[] { "tutorial", "spoke-b", "spoke-b-90" })
            foreach (var policy in new[] { "greedy", "textbook" })
            {
                var s = Replay.Run(Repo.Load(scenario), 1, PolicyCatalog.Create(policy));
                foreach (var f in s.View.Debrief().Gaps.SelectMany(g => g.Factors).Where(f => f.Kind == GapFactorKind.VendorFailure))
                    Assert.Equal(f.Text.Contains("and nobody asked.", StringComparison.Ordinal), f.Tag == GapTag.Discoverable && f.Text.Contains("was bought without", StringComparison.Ordinal));
            }
    }
}
