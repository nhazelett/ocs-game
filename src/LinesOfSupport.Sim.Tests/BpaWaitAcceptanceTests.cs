using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-243 / D69: the actual second 23 Nov player waited on a Harbor BPA quote,
/// learned about operators, then encountered Harbor's refusal and bought Coastal's different route.</summary>
public class BpaWaitAcceptanceTests
{
    private static InputLog ColdLog() => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-23", "input-log-2.json"));
    private static AwardAction Harbor(int day) => new()
    {
        Day = day, Id = "check-call", Instrument = "bpaCall", Vendor = "harborEquipment", Vehicle = "equipBpa",
        Card = "tutForklift", Site = "hubSite", Capability = "mhe", Quantity = 3, StartDay = 8,
        DurationDays = 13, FundingLine = "om1",
    };

    [Fact]
    public void The_actual_day_four_wait_forecast_names_acceptance_without_revealing_tomorrows_refusal()
    {
        var input = ColdLog();
        var s = Replay.Run(Repo.Load("tutorial"), input, stopAfterDay: 3);
        Assert.True(s.Apply(input.Actions.OfType<AskAboutAction>().Single(a => a.Day == 4)).Accepted);
        var hash = s.StateHash(); var save = s.ToSave().ToJson();
        var buy = Harbor(4);
        var preview = s.View.Preview(buy);
        Assert.True(preview.Accepted);
        Assert.Equal((8, 8), (preview.EarliestStartMin, preview.EarliestStartMax));
        var summary = s.View.AwardSummary(buy)!;
        Assert.Equal("It leaves out operators; you are asking SMSgt Ray Dunn, whose answer comes in tonight; a BPA call tomorrow still starts on day 8 if the supplier accepts.", summary.Why);
        Assert.EndsWith(summary.Why, summary.Sentence);
        Assert.EndsWith("A BPA call tomorrow still starts on day 8 if the supplier accepts.", summary.AnswerDue);
        Assert.Equal(3, summary.Sentence.Split(". ").Length);
        Assert.DoesNotContain("turning calls down", summary.Sentence);
        Assert.Equal(hash, s.StateHash()); Assert.Equal(save, s.ToSave().ToJson());
    }

    [Fact]
    public void The_next_morning_keeps_learning_refusal_and_the_actual_replacement_quote_and_gap()
    {
        var input = ColdLog();
        var s = Replay.Run(Repo.Load("tutorial"), input, stopAfterDay: 4);
        var replacement = input.Actions.OfType<AwardAction>().Single(a => a.Day == 5);
        Assert.Contains("which SMSgt Ray Dunn said the request needs", s.View.AwardSummary(replacement)!.Why);
        Assert.True(s.Apply(input.Actions.OfType<AddLineAction>().Single(a => a.Day == 5)).Accepted);
        Assert.DoesNotContain("leaves out operators", s.View.AwardSummary(replacement)!.Sentence);
        var hash = s.StateHash(); var save = s.ToSave().ToJson();
        var refusal = s.View.Preview(Harbor(5));
        Assert.False(refusal.Accepted);
        Assert.Contains(refusal.HardStops, line => line.Contains("Harbor Equipment Co") && line.Contains("turning calls down"));
        var quote = s.View.Preview(replacement);
        Assert.True(quote.Accepted);
        Assert.Equal((8, 9), (quote.EarliestStartMin, quote.EarliestStartMax));
        Assert.DoesNotContain("if the supplier accepts", s.View.AwardSummary(replacement)!.Sentence);
        Assert.Equal(hash, s.StateHash()); Assert.Equal(save, s.ToSave().ToJson());

        var finished = Replay.Run(Repo.Load("tutorial"), input);
        var a = finished.Arrangements.Single(a => a.Id == replacement.Id);
        Assert.Equal((8, 9, 9), (a.QuotedStartMin, a.QuotedStartMax, a.StartDay));
        var debrief = finished.View.Debrief();
        var gap = debrief.Gaps.Single(g => g.SiteId == "hubSite" && g.CapabilityId == "mhe");
        Assert.Equal(GapTag.Avoidable, gap.Tag);
        var uncovered = gap.Factors.Single(f => f.Kind == GapFactorKind.Uncovered);
        Assert.EndsWith("The purchase order with Coastal Rentals was quoted to start on day 8 or 9 and started on day 9.", uncovered.Text);
        Assert.Equal((8, 8), (gap.StartDay, gap.EndDay));
        Assert.DoesNotContain("would have", uncovered.Text);
    }
}
