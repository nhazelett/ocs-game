using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>D42 (DESK-123; Claude play 20 Nov, second play, note 1): a buy made while an ask about the same request is out
/// says when the answer is due, as the summary's last sentence. It is not an attention reason and does not stop the buy.</summary>
public class AnswerDueTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static AwardAction Forklifts() => new()
    {
        Id = "fork", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3, StartDay = 8,
        DurationDays = 13, FundingLine = "om1",
    };

    [Fact]
    public void The_water_buy_on_day_1_says_when_Ortiz_answers_and_stops_saying_it_once_he_has()
    {
        // D48 (DESK-172): the forklift buy this test used now names the operators it leaves out while Dunn is asked
        // (NotYetConfirmedTests); Island Mart's water is goods (DESK-59), so the answer due is still its last sentence.
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        AwardAction water = new()
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 7,
        };
        Assert.Null(s.View.AwardSummary(water)!.AnswerDue);

        Assert.True(s.Apply(new AskAboutAction { Card = "tutWater", Person = "ortiz" }).Accepted);
        var ask = s.Work.Last();
        var summary = s.View.AwardSummary(water)!;
        // DESK-190 (D51) adds what the same buy placed tomorrow would get (NightsWaitTests).
        Assert.StartsWith("MSgt Carla Ortiz's answer about this request comes in tonight, after you end the day; what they say may change what this buy needs. Ordered tomorrow, after the answer, it starts on day ", summary.AnswerDue);
        Assert.EndsWith(summary.AnswerDue, summary.Sentence);
        Assert.False(summary.Attention);
        Assert.Null(summary.Why);
        Assert.True(s.View.Preview(water).Accepted);

        while (ask.CompletedDay is null) s.AdvanceDay();
        var after = s.View.AwardSummary(water)!;
        Assert.Null(after.AnswerDue);
        Assert.DoesNotContain("answer about this request", after.Sentence);
    }

    [Fact]
    public void An_answer_due_on_a_later_day_names_that_night()
    {
        // D44 (DESK-144): a purchase order given to Novak on day 1 is certified on day 2 and fills his day, so an ask he
        // takes on day 2 waits behind it and its answer comes in on a later night.
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        AwardAction Water(string id) => new()
        {
            Id = id, Instrument = "purchaseOrder", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 3,
            DurationDays = 7, FundingLine = "om1", Assignee = "novak",
        };
        var first = s.Apply(Water("first"));
        Assert.True(first.Accepted, first.Message);
        s.AdvanceDay();
        var ask = s.Apply(new AskAboutAction { Card = "tutWater", Person = "ortiz", Assignee = "novak" });
        Assert.True(ask.Accepted, ask.Message);
        var helper = s.View.RequirementEditor("tutWater")!.WhoCanHelp!.First(h => h.Kind == "ask");
        Assert.True(helper.Day > s.Day, $"due day {helper.Day} on day {s.Day}");
        Assert.Equal($"SrA Eli Novak is on it; the answer comes in on the night of day {helper.Day}.", helper.Sentence);
        // The buy's answer-due sentence uses the same words (Session.ComesIn); a second buy for an arranged request has no summary.
    }

    [Fact]
    public void An_ask_about_another_request_says_nothing_on_this_buy()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 4) s.AdvanceDay();
        var water = s.Cards.First(c => c.Id != "tutForklift" && c.SubmittedDay <= s.Day);
        var person = Repo.Load("tutorial").Scenario.RequirementCards.First(c => c.Id == water.Id).KnownBy.Values.FirstOrDefault()?.Person ?? "dunn";
        s.Apply(new AskAboutAction { Card = water.Id, Person = person });
        Assert.Null(s.View.AwardSummary(Forklifts())!.AnswerDue);
    }
}
