using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-31 (docs/design/desk-redesign.md D16): where things stand, one line per request, needing attention first.
/// Nick at the DESK gate could not remember whether he had already bought the water.</summary>
public class StandingTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static Session Taught(int morning)
    {
        var log = InputLog.Load(Path.Combine(Repo.Scenario("tutorial"), "golden", "approach1-taught.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: morning - 1);
        foreach (var a in log.Actions.Where(a => a.Day == morning)) s.Apply(a);
        return s;
    }

    [Fact]
    public void Day_one_the_water_is_wanted_and_nothing_is_arranged()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var water = Assert.Single(s.View.WhereThingsStand());
        Assert.Equal("Water for Hub", water.Title);
        Assert.Equal("MSgt Carla Ortiz", water.From!.DisplayName);
        Assert.Equal(RequestStanding.Nothing, water.State);
        Assert.Null(water.Teammate);
        Assert.Equal("Nothing is arranged yet. MSgt Carla Ortiz wants it from day 2 for 7 days.", water.Sentence);
    }

    [Fact]
    public void After_the_water_is_bought_it_says_who_and_when()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 7, FundingLine = "om1",
        }).Accepted);
        var water = Assert.Single(s.View.WhereThingsStand());
        Assert.Equal(RequestStanding.Arranged, water.State);
        Assert.Equal("Island Mart starts on day 2: 4500 gal/day through day 8.", water.Sentence);
        s.AdvanceDay();
        water = Assert.Single(s.View.WhereThingsStand());
        Assert.Equal(RequestStanding.Running, water.State);
        Assert.Equal("Island Mart is delivering 4500 gal/day through day 8.", water.Sentence);
    }

    [Fact]
    public void A_contract_in_work_names_the_teammate_writing_it_and_the_wait_for_finance()
    {
        var forklifts = Taught(4).View.WhereThingsStand().Single(x => x.CardId == "tutForklift");
        Assert.Equal(RequestStanding.Buying, forklifts.State);
        Assert.Equal("avery", forklifts.TeammateId);
        Assert.Equal("MSgt Joan Avery", forklifts.Teammate);
        Assert.Equal("MSgt Joan Avery is writing the BPA call with Harbor Equipment Co. It waits for finance to certify the funds on day 5, then about 7 hours of work.", forklifts.Sentence);
        Assert.Equal("MSgt Joan Avery is writing the BPA call with Harbor Equipment Co, about 7 hours of work left.",
            Taught(5).View.WhereThingsStand().Single(x => x.CardId == "tutForklift").Sentence);
    }

    [Fact]
    public void Asking_about_a_request_shows_until_the_answer_comes_back()
    {
        var log = InputLog.Load(Path.Combine(Repo.Scenario("tutorial"), "golden", "approach1-taught.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 3);
        Assert.True(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn", Assignee = "novak" }).Accepted);
        var forklifts = s.View.WhereThingsStand().Single(x => x.CardId == "tutForklift");
        Assert.Equal(RequestStanding.Asking, forklifts.State);
        Assert.Equal("SrA Eli Novak is asking SMSgt Ray Dunn about it; nothing is bought yet.", forklifts.Sentence);
    }

    [Fact]
    public void The_water_is_done_once_its_days_are_over_and_done_sorts_last()
    {
        var all = Taught(11).View.WhereThingsStand();
        Assert.Equal(RequestStanding.Ended, all[^1].State);
        Assert.Equal("Done: Island Mart delivered through day 8.", all[^1].Sentence);
        Assert.Equal(RequestStanding.Arranged, all.Single(x => x.CardId == "tutWaste").State);
    }

    [Fact]
    public void Support_that_failed_yesterday_reads_short_and_comes_first()
    {
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach1-as-written.json"));
        var s = Replay.Run(Repo.Load("spoke-b"), log, stopAfterDay: 10);
        var first = s.View.WhereThingsStand()[0];
        Assert.Equal("Power for Spoke B", first.Title);
        Assert.Equal(RequestStanding.Short, first.State);
        Assert.Equal("Harbor Equipment Co is meant to deliver 360 kW, but power at Spoke B was at 0% yesterday.", first.Sentence);
    }

    [Fact]
    public void A_request_wanted_already_with_nothing_arranged_comes_first()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        while (s.Day < 6) s.AdvanceDay();
        var first = s.View.WhereThingsStand()[0];
        Assert.Equal("Water for Spoke C", first.Title);
        Assert.Equal("Nothing is arranged, and it was wanted from day 5.", first.Sentence);
    }

    [Fact]
    public void A_contract_on_its_first_day_is_not_blamed_for_the_gap_before_it()
    {
        // Approach 4 buys Spoke B's power late: Meridian starts on day 17, after ten dark days. On day 17 it is running,
        // not short, though power was at 0% the day before; the day before, it is the first thing on the list.
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach4-enduring-only.json"));
        var s = Replay.Run(Repo.Load("spoke-b"), log, stopAfterDay: 15);
        var first = s.View.WhereThingsStand()[0];
        Assert.Equal("Power for Spoke B", first.Title);
        Assert.Equal(RequestStanding.Arranged, first.State);
        Assert.Equal("Meridian Site Services starts on day 17: 470 kW. It was wanted from day 7.", first.Sentence);

        s = Replay.Run(Repo.Load("spoke-b"), log, stopAfterDay: 16);
        var power = s.View.WhereThingsStand().Single(x => x.Title == "Power for Spoke B");
        Assert.Equal(RequestStanding.Running, power.State);
        Assert.Equal("Meridian Site Services is delivering 470 kW.", power.Sentence);
    }

    [Fact]
    public void A_request_missed_altogether_is_over_once_its_days_are_and_sorts_last()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        while (s.Day < 9) s.AdvanceDay();
        var all = s.View.WhereThingsStand();
        var water = all.Single(x => x.Title == "Water for Hub");
        Assert.Equal(RequestStanding.Ended, water.State);
        Assert.Equal("Its days are over: it was wanted from day 2 to day 8, and nothing was arranged.", water.Sentence);
        Assert.Same(water, all[^1]);
    }

    public static IEnumerable<object[]> Scenarios() => [["spoke-b"], ["spoke-b-90"], ["sustainment"], ["tutorial"]];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Every_morning_of_every_golden_and_policy_reads_plainly_and_changes_nothing(string scenario)
    {
        var bundle = Repo.Load(scenario);
        var runs = Directory.GetFiles(Path.Combine(Repo.Scenario(scenario), "golden"), "*.json").Order(StringComparer.Ordinal)
            .Select(p => (Path.GetFileName(p), (Func<Session>)(() => Session.Create(bundle, InputLog.Load(p).Seed, InputLog.Load(p).PrepChoices, InputLog.Load(p).Team)), InputLog.Load(p) as InputLog, (IPolicy?)null))
            .Concat(PolicyCatalog.Names.Select(n => (n, (Func<Session>)(() => Session.Create(bundle, 1)), (InputLog?)null, (IPolicy?)PolicyCatalog.Create(n))));
        foreach (var (name, create, log, policy) in runs)
        {
            var s = create();
            while (!s.Finished)
            {
                var actions = log is not null ? log.Actions.Where(a => a.Day == s.Day).ToList() : policy!.Decide(s.View).ToList();
                foreach (var a in actions) s.Apply(a);
                var hash = s.StateHash();
                var standing = s.View.WhereThingsStand();
                Assert.Equal(hash, s.StateHash());
                Assert.Equal(standing.Count, standing.Select(x => x.CardId).Distinct().Count());
                foreach (var e in standing)
                {
                    Assert.True(char.IsUpper(e.Sentence[0]), $"{scenario}/{name} day {s.Day}: {e.Sentence}");
                    Assert.DoesNotContain(e.CardId, e.Sentence, StringComparison.Ordinal);
                    Assert.DoesNotContain("•", e.Sentence, StringComparison.Ordinal);
                    Assert.DoesNotContain("  ", e.Sentence, StringComparison.Ordinal);
                    Assert.EndsWith(".", e.Sentence, StringComparison.Ordinal);
                }
                s.AdvanceDay();
            }
        }
    }
}
