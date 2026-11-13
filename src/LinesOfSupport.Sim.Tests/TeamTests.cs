using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-02 (docs/design/desk-redesign.md D2): a named team of three, each with their own hours. Work is assigned
/// to a person and comes out of that person's hours; work nobody was given stays with you.</summary>
public class TeamTests
{
    private static AwardAction ForkliftCall(string? who) => new()
    {
        Id = "call", Instrument = "bpaCall", Vendor = "harborEquipment", Vehicle = "equipBpa", Card = "tutForklift",
        Quantity = 3, StartDay = 8, DurationDays = 13, FundingLine = "om1", Assignee = who,
    };

    [Fact]
    public void The_tutorial_office_is_you_an_experienced_and_a_junior_CCO_with_their_own_hours()
    {
        var team = Session.Create(Repo.Load("tutorial"), 1).View.Team;
        Assert.Equal(["you", "experienced", "junior"], team.Select(m => m.Role));
        Assert.Equal("You", team[0].Name);
        Assert.Equal(("MSgt", "Joan Avery"), (team[1].Rank, team[1].Name));
        Assert.All(team, m => Assert.Equal(8, m.HoursPerDay));
        Assert.Equal("Gets through work faster than most.", team[1].Pace);
        Assert.Equal("Still learning: work takes longer.", team[2].Pace);
        Assert.All(team, m => Assert.Equal(m.HoursPerDay, m.FreeHoursToday));
    }

    [Fact]
    public void A_scenario_without_a_team_is_you_alone_with_all_the_shops_hours()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var you = Assert.Single(s.View.Team);
        Assert.Equal("you", you.Role);
        Assert.Equal(s.View.TopStrip().ShopHoursPerDay, you.HoursPerDay);
    }

    [Fact]
    public void Work_nobody_was_given_stays_with_you_and_given_work_comes_out_of_that_persons_hours()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        Assert.True(s.Apply(new FindFinanceAction()).Accepted);
        Assert.Equal("you", s.Work.Single().Assignee);

        var t = Session.Create(Repo.Load("tutorial"), 1);
        Assert.True(t.Apply(new FindFinanceAction { Assignee = "novak" }).Accepted);
        Assert.Equal("novak", t.Work.Single().Assignee);
        var team = t.View.Team;
        Assert.Equal(0, team[0].HoursSpokenForToday);
        Assert.True(team[2].HoursSpokenForToday > 0);
        Assert.Equal(team[2].HoursPerDay - team[2].HoursSpokenForToday, team[2].FreeHoursToday);
        Assert.Equal(t.Work.Single().Id, Assert.Single(team[2].Work).WorkId);
    }

    [Fact]
    public void The_experienced_CCO_gets_through_the_same_job_faster_than_the_junior()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        while (s.Day < 4) s.AdvanceDay();
        var avery = s.View.Preview(ForkliftCall("avery"));
        var you = s.View.Preview(ForkliftCall(null));
        var novak = s.View.Preview(ForkliftCall("novak"));
        Assert.True(avery.ShopHoursMax < you.ShopHoursMax && you.ShopHoursMax < novak.ShopHoursMax);
        Assert.True(avery.EarliestStartMax <= you.EarliestStartMax && you.EarliestStartMax <= novak.EarliestStartMax);
        Assert.True(avery.EarliestStartMax < novak.EarliestStartMax);
    }

    [Fact]
    public void Nobody_works_anyone_elses_queue()
    {
        // A written order on the junior, nothing on you: each day it gets the junior's 8 hours and none of yours.
        var s = Session.Create(Repo.Load("tutorial"), 1);
        while (s.Day < 9) s.AdvanceDay();
        Assert.True(s.Apply(new AwardAction
        {
            Id = "waste", Instrument = "purchaseOrder", Vendor = "sanitation", Card = "tutWaste", Quantity = 3600,
            StartDay = 13, DurationDays = 8, FundingLine = "om1", Assignee = "novak",
        }).Accepted);
        var w = s.Work.Single(x => x.Kind == WorkKind.Award);
        while (w.WaitUntilDay > s.Day) s.AdvanceDay();
        var before = w.HoursRemainingMilli;
        Assert.True(before > 8000);
        Assert.Equal(8, s.View.Team[0].FreeHoursToday);
        Assert.Equal(0, s.View.Team[2].FreeHoursToday);
        s.AdvanceDay();
        Assert.Equal(before - 8000, w.HoursRemainingMilli);
    }

    [Fact]
    public void Handing_work_over_re_reckons_the_hours_at_the_new_persons_pace_and_names_both()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        while (s.Day < 4) s.AdvanceDay();
        Assert.True(s.Apply(ForkliftCall("novak")).Accepted);
        var w = s.Work.Single();
        var junior = w.HoursRemainingMilli;
        Assert.Equal((junior * 750 / 1250 + 499) / 500 / 2.0, s.View.HoursIfAssigned(w.Id, "avery"));
        Assert.True(s.Apply(new AssignAction { Work = w.Id, To = "avery" }).Accepted);
        Assert.Equal("avery", w.Assignee);
        Assert.Equal((junior * 750 + 1249) / 1250, w.HoursRemainingMilli);
        var rec = s.Records.Last(r => r.Kind == RecordKind.Action);
        Assert.StartsWith("MSgt Joan Avery took over BPA call with Harbor Equipment Co", rec.Text, StringComparison.Ordinal);
        Assert.Contains("from SrA Eli Novak", rec.Text, StringComparison.Ordinal);

        Assert.False(s.Apply(new AssignAction { Work = w.Id, To = "avery" }).Accepted);
        Assert.False(s.Apply(new AssignAction { Work = w.Id, To = "nobody" }).Accepted);
        Assert.False(s.Apply(new AssignAction { Work = "w999", To = "you" }).Accepted);
        Assert.False(s.Apply(new FindFinanceAction { Assignee = "nobody" }).Accepted);
    }

    [Fact]
    public void Someone_on_the_way_to_finance_is_named()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        Assert.True(s.Apply(new FindFinanceAction { Assignee = "avery" }).Accepted);
        var stop = Assert.Single(s.View.PreviewVisit(new FindFinanceAction()).HardStops);
        Assert.Contains("MSgt Joan Avery is already on the way to the finance office", stop, StringComparison.Ordinal);

        var t = Session.Create(Repo.Load("tutorial"), 1);
        Assert.True(t.Apply(new FindFinanceAction()).Accepted);
        Assert.Matches("^[Yy]ou are already on the way to the finance office", Assert.Single(t.View.PreviewVisit(new FindFinanceAction()).HardStops));
    }

    [Fact]
    public void The_outgoing_CCO_introduces_the_team_by_name()
    {
        var shop = Session.Create(Repo.Load("tutorial"), 1).View.Turnover().Text(TurnoverSectionKind.Shop);
        Assert.Contains("You've got MSgt Joan Avery and SrA Eli Novak.", shop, StringComparison.Ordinal);
        Assert.Contains("Avery has done this before and works fast. Novak is new and still learning.", shop, StringComparison.Ordinal);
        Assert.Contains("You have 8 hours a day; Avery has 8, Novak has 8. Hand them work, or it stays with you.", shop, StringComparison.Ordinal);
        Assert.DoesNotContain("omeone", shop, StringComparison.Ordinal);
    }

    [Fact]
    public void Names_and_faces_are_prep_inputs_that_replay_the_same()
    {
        var bundle = Repo.Load("tutorial");
        TeamPick[] picks = [new() { Id = "avery", Name = "Dana Reyes", Portrait = "ward" }, new() { Id = "novak", Portrait = "" }];
        var s = Session.Create(bundle, 1, ["gpcAccounts"], picks);
        Assert.True(s.Apply(new FindFinanceAction { Assignee = "avery" }).Accepted);
        s.AdvanceDay();
        Assert.Equal(("Dana Reyes", "ward"), (s.View.Team[1].Name, s.View.Team[1].Portrait));
        Assert.Null(s.View.Team[2].Portrait);

        var log = s.ToInputLog();
        var again = InputLog.Load(Save(log));
        // Avery's new name and face are logged, and so is clearing the junior's scenario face (ART-18 gave the
        // tutorial team faces); his untouched name is not.
        Assert.Equal(["avery", "novak"], again.Team!.Select(t => t.Id));
        Assert.Equal((null, ""), (again.Team![1].Name, again.Team![1].Portrait));
        var replayed = Replay.Run(bundle, again, stopAfterDay: 1);
        Assert.Equal(s.View.Team.Select(m => (m.Name, m.Portrait)), replayed.View.Team.Select(m => (m.Name, m.Portrait)));

        // Untouched names leave the log without a team block, so older logs and new ones read the same.
        Assert.Null(Session.Create(bundle, 1).ToInputLog().Team);
        Assert.Throws<InvalidDataException>(() => Session.Create(bundle, 1, [], [new TeamPick { Id = "avery", Name = "  " }]));
        Assert.Throws<InvalidDataException>(() => Session.Create(bundle, 1, [], [new TeamPick { Id = "stranger", Name = "X" }]));
    }

    [Fact]
    public void A_team_needs_exactly_one_you()
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("tutorial"), "scenario.json")))!;
        root["shop"]!["team"]![0]!["role"] = "junior";
        var dir = Path.Combine(Path.GetTempPath(), $"los-team-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        var ex = Assert.ThrowsAny<Exception>(() => ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules")));
        Assert.Contains("exactly one member with role you", ex.Message, StringComparison.Ordinal);
    }

    private static string Save(InputLog log)
    {
        var path = Path.Combine(Path.GetTempPath(), $"los-team-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, log.ToJson());
        return path;
    }

    [Fact]
    public void A_teammates_pace_shows_in_the_hours_rounded_to_the_half_hour()
    {
        // DESK-14: the finance trip is 3 hours of usual work; the experienced CCO's is 2.4, shown 2.5 (it read 3, the same
        // as yours), the junior's is 4. The Sim still charges the milli-hours.
        var s = Session.Create(Repo.Load("tutorial"), 1);
        Assert.True(s.Apply(new FindFinanceAction()).Accepted);
        var w = s.Work.Single();
        Assert.Equal(3000, w.HoursRemainingMilli);
        Assert.Equal(3, s.View.HoursIfAssigned(w.Id, "you"));
        Assert.Equal(2.5, s.View.HoursIfAssigned(w.Id, "avery"));
        Assert.Equal(4, s.View.HoursIfAssigned(w.Id, "novak"));

        Assert.True(s.Apply(new AssignAction { Work = w.Id, To = "avery" }).Accepted);
        Assert.Equal(2400, w.HoursRemainingMilli);
        var avery = s.View.Team.Single(m => m.Id == "avery");
        Assert.Equal(2.5, Assert.Single(avery.Work).HoursLeft);
        Assert.Equal(2.5, avery.HoursSpokenForToday);
        Assert.Equal(5.5, avery.FreeHoursToday);
        Assert.Equal(2.5, avery.HoursQueued);
        Assert.Contains("MSgt Joan Avery has 5.5 free hours today.", s.View.EndOfDayCheck());
    }
}
