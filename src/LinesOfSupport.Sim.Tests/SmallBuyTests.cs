using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-03: the customer small-buy stream and the purchase card program (design doc 6.1).</summary>
public class SmallBuyTests
{
    private static int Hub(Session s) => s.Scenario.Sites.First(x => x.Id == "hubSite").Population[0].Count;

    [Fact]
    public void Stream_is_population_math_and_the_quiet_shop_works_it_all()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        s.AdvanceDay();
        var expected = Hub(s) * s.Rules.SmallBuys.BuysPerPersonPerDayMilli / 1000;
        var strip = s.View.TopStrip();
        Assert.Equal(expected, strip.SmallBuysToday);
        Assert.Equal(0, strip.SmallBuyBacklog);
        Assert.Equal("none", strip.GpcProgram);
        Assert.Contains(s.Records, r => r.Subject == "smallBuys" && r.Text.StartsWith("CE small buys:"));
        var t = Session.Create(Repo.Load("spoke-b"), 1);
        t.AdvanceDay();
        Assert.Equal(s.StateHash(), t.StateHash());
    }

    [Fact]
    public void Small_buys_take_only_leftover_hours_so_queued_work_keeps_its_timing()
    {
        var b = Repo.Load("spoke-b");
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach3-uca-turnkey-with-bridge.json"));
        var s = Replay.Run(b, log);
        // The SPEC 5 approach 3 story has no Spoke B gap; the stream must not have moved its clock.
        Assert.DoesNotContain(s.View.Debrief().Gaps, g => g.SiteId == "spokeBSite");
        Assert.True(s.Metrics["shop.smallBuyHours"] > 0);
    }

    [Fact]
    public void A_busy_shop_builds_a_backlog_and_the_customer_goes_to_the_commander()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        // Fill the queue: the day-1 textbook awards plus surveys leave no hours for a while.
        foreach (var a in PolicyCatalog.Create("textbook").Decide(s.View).Where(a => a is not StandUpGpcProgramAction)) s.Apply(a);
        s.Apply(new AwardAction { Id = "big", Instrument = "purchaseOrder", Vendor = "vendorA", Site = "spokeBSite", Capability = "power", Lines = ["generation", "maintenance"], Quantity = 300, DurationDays = 30, FundingLine = "om1" });
        // The textbook hands fuel to the J4 now (M2R-06), so one more written buy keeps the queue full.
        s.Apply(new AwardAction { Id = "big2", Instrument = "purchaseOrder", Vendor = "vendorE", Site = "spokeBSite", Capability = "fuel", Lines = ["delivery"], Quantity = 500, DurationDays = 20, FundingLine = "om1" });
        var complained = false;
        for (var d = 0; d < 8 && !complained; d++)
        {
            s.AdvanceDay();
            complained = s.Records.Any(r => r.Subject == "smallBuys" && r.Kind == RecordKind.Compliance);
        }
        Assert.True(complained, "CE should have gone to the commander");
        Assert.True(s.Metrics["compliance.commanderConfidenceHits"] >= 1);
    }

    [Fact]
    public void Hold_and_release()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.False(s.Apply(new HoldSmallBuysAction { Hold = false }).Accepted);
        Assert.True(s.Apply(new HoldSmallBuysAction { Hold = true }).Accepted);
        Assert.False(s.Apply(new HoldSmallBuysAction { Hold = true }).Accepted);
        for (var d = 0; d < 4; d++) s.AdvanceDay();
        Assert.True(s.View.SmallBuysHeld);
        Assert.True(s.View.SmallBuyBacklog >= 12);
        Assert.Contains(s.Records, r => r.Subject == "smallBuys" && r.Kind == RecordKind.Compliance);
        Assert.True(s.Apply(new HoldSmallBuysAction { Hold = false }).Accepted);
        s.AdvanceDay();
        Assert.True(s.View.SmallBuyBacklog < 12);
    }

    [Fact]
    public void Card_program_needs_cards_is_worked_around_everything_else_and_diverts_most_of_the_stream()
    {
        var b = Repo.Load("spoke-b");
        var noCards = Session.Create(b, 1);
        Assert.False(noCards.Apply(new StandUpGpcProgramAction()).Accepted);

        var s = Session.Create(b, 1, PolicyCatalog.Create("textbook").PrepChoices(b));
        Assert.True(s.Apply(new StandUpGpcProgramAction()).Accepted);
        Assert.False(s.Apply(new StandUpGpcProgramAction()).Accepted);
        Assert.Equal("standing up", s.View.TopStrip().GpcProgram);
        var award = new AwardAction { Id = "gens", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Card = "req0007", Quantity = 360, StartDay = 7, DurationDays = 60, FundingLine = "om1" };
        Assert.True(s.Apply(award).Accepted);
        var plain = Session.Create(b, 1, PolicyCatalog.Create("textbook").PrepChoices(b));
        plain.Apply(award);
        while (s.View.TopStrip().GpcProgram != "running") { s.AdvanceDay(); plain.AdvanceDay(); }
        // The award completed on the same day in both games: the stand-up took only leftover hours.
        Assert.Equal(plain.Work.First(w => w.Kind == WorkKind.Award).CompletedDay, s.Work.First(w => w.Kind == WorkKind.Award).CompletedDay);
        s.AdvanceDay(); plain.AdvanceDay();
        var with = s.View.TopStrip(); var without = plain.View.TopStrip();
        Assert.True(with.SmallBuysToday < without.SmallBuysToday, $"{with.SmallBuysToday} vs {without.SmallBuysToday}");
        Assert.True(s.GpcProgramDay is not null);
    }

    [Fact]
    public void Actions_round_trip_through_an_input_log()
    {
        var b = Repo.Load("spoke-b");
        var s = Session.Create(b, 2, PolicyCatalog.Create("textbook").PrepChoices(b));
        s.Apply(new StandUpGpcProgramAction());
        s.Apply(new HoldSmallBuysAction { Hold = true });
        for (var d = 0; d < 5; d++) s.AdvanceDay();
        s.Apply(new HoldSmallBuysAction { Hold = false });
        while (!s.Finished) s.AdvanceDay();
        var json = s.ToInputLog().ToJson();
        Assert.Contains("\"kind\": \"standUpGpcProgram\"", json);
        Assert.Contains("\"kind\": \"holdSmallBuys\"", json);
        var path = Path.Combine(Path.GetTempPath(), $"sb-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        Assert.Equal(s.StateHash(), Replay.Run(b, InputLog.Load(path)).StateHash());
    }
}
