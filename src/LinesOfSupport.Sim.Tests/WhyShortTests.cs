using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-38 (docs/design/desk-redesign.md D18): every shortfall says why, where it shows, from what the office
/// knows. Nick on his 13 Nov replay could not tell why water and food at Hub were at 0%.</summary>
public class WhyShortTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static Session Tutorial(int morning)
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < morning) s.AdvanceDay();
        return s;
    }

    [Fact]
    public void Nothing_bought_says_so_and_food_says_it_needs_the_water()
    {
        var s = Tutorial(4);
        var water = s.View.WhyShort("hubSite", "water");
        var tiles = s.View.MorningBoard().Tiles;
        Assert.Equal(tiles.Single(t => t.SiteId == "hubSite" && t.CapabilityId == "water").WhyShort, water);
        Assert.NotNull(water);
        Assert.StartsWith("Nothing supplies water at Hub until", water);
        var food = s.View.WhyShort("hubSite", "food");
        Assert.Equal("No water, so no food service: food service needs water.", food);
        Assert.Equal(water, s.View.WhereThingsStand().Single(x => x.CardId == "tutWater").WhyShort);
    }

    [Fact]
    public void A_buy_being_written_names_who_and_when_it_is_done()
    {
        var s = Tutorial(3);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "purchaseOrder", Vendor = "harborBottling", Card = "tutWater", Quantity = 4500,
            DurationDays = 6, FundingLine = "om1", Assignee = "avery",
        }).Accepted);
        s.AdvanceDay();
        var why = s.View.WhyShort("hubSite", "water");
        Assert.True(why!.Contains("; MSgt Joan Avery is writing the purchase order with Harbor Bottling, done about day ", StringComparison.Ordinal), why);
    }

    [Fact]
    public void Supplied_in_full_or_before_any_day_is_played_is_null()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.All(s.View.MorningBoard().Tiles, t => Assert.Null(t.WhyShort));
        Assert.Null(s.View.WhyShort("hubSite", "water"));
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 7, FundingLine = "om1",
        }).Accepted);
        while (s.Day < 4) s.AdvanceDay();
        Assert.Null(s.View.WhyShort("hubSite", "water"));
    }

    [Fact]
    public void A_failure_nobody_has_reported_is_not_named()
    {
        // Spoke B, 90 days, with no COR on the power contract: on day 28 the generator is down and nobody checks for the
        // government, so the office only sees the site short.
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b-90"), "golden", "approach1-no-cor.json"));
        var s = Replay.Run(Repo.Load("spoke-b-90"), log, stopAfterDay: 28);
        Assert.Contains(s.Records, r => r.Kind == RecordKind.VendorFailure && r.Day == 28);
        Assert.Equal("The site reports it short. Nobody has told you why yet.", s.View.WhyShort("spokeBSite", "power"));
    }

    [Fact]
    public void A_delivery_waiting_for_the_boat_says_so()
    {
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b-90"), "golden", "approach1-no-cor.json"));
        var s = Replay.Run(Repo.Load("spoke-b-90"), log, stopAfterDay: 17);
        Assert.Equal("Port Fuel Haulers' delivery is waiting on Main Island: the route to Second Island is closed through day 18.",
            s.View.WhyShort("spokeBSite", "fuel"));
        Assert.Equal("No fuel, so no power: power needs fuel.", s.View.WhyShort("spokeBSite", "power"));
    }

    public static IEnumerable<object[]> Scenarios() => [["spoke-b"], ["spoke-b-90"], ["sustainment"], ["tutorial"]];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Every_short_capability_on_every_morning_says_why_plainly_and_changes_nothing(string scenario)
    {
        var bundle = Repo.Load(scenario);
        var runs = Directory.GetFiles(Path.Combine(Repo.Scenario(scenario), "golden"), "*.json").Order(StringComparer.Ordinal)
            .Select(p => (Path.GetFileName(p), (Func<Session>)(() => Session.Create(bundle, InputLog.Load(p).Seed, InputLog.Load(p).PrepChoices, InputLog.Load(p).Team)), InputLog.Load(p) as InputLog, (IPolicy?)null))
            .Concat(PolicyCatalog.Names.Select(n => (n, (Func<Session>)(() => Session.Create(bundle, 1)), (InputLog?)null, (IPolicy?)PolicyCatalog.Create(n))));
        var ids = bundle.Scenario.Sites.Select(x => x.Id).Concat(bundle.Scenario.Vendors.Select(v => v.Id)).ToList();
        foreach (var (name, create, log, policy) in runs)
        {
            var s = create();
            while (!s.Finished)
            {
                var actions = log is not null ? log.Actions.Where(a => a.Day == s.Day).ToList() : policy!.Decide(s.View).ToList();
                foreach (var a in actions) s.Apply(a);
                var hash = s.StateHash();
                var tiles = s.View.MorningBoard().Tiles;
                Assert.Equal(hash, s.StateHash());
                foreach (var t in tiles)
                {
                    var where = $"{scenario}/{name} day {s.Day} {t.SiteId}.{t.CapabilityId}";
                    if (t.Status is "ok" or "notOpen") { Assert.Null(t.WhyShort); continue; }
                    Assert.False(t.WhyShort is null, $"{where} at {t.TodayPermille} has no reason");
                    var why = t.WhyShort!;
                    Assert.True(char.IsUpper(why[0]), $"{where}: {why}");
                    Assert.EndsWith(".", why, StringComparison.Ordinal);
                    Assert.DoesNotContain("..", why, StringComparison.Ordinal);
                    Assert.DoesNotContain("  ", why, StringComparison.Ordinal);
                    Assert.DoesNotContain("•", why, StringComparison.Ordinal);
                    Assert.All(ids, id => Assert.False(why.Contains(" " + id + " ", StringComparison.Ordinal), $"{where}: id {id} in \"{why}\""));
                }
                s.AdvanceDay();
            }
        }
    }
}
