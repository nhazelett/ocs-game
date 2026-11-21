using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-139 (Fable's review of PRs 405 and 406). D37 offers "Add to this contract" only where the buy left out a part:
/// a card buy that left nothing out and a contract that already brings everything leave the button off the page. And Q27:
/// a card buy cancelled while delivering reads Ended the next morning, through <see cref="SessionView.StatusToday"/>.</summary>
public class ModifyOfferedTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static Session TutorialWithCardWater()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 7, FundingLine = "om1",
        }).Accepted);
        return s;
    }

    [Fact]
    public void A_card_buy_that_left_nothing_out_is_not_offered_a_change()
    {
        var s = TutorialWithCardWater();
        var v = s.View.ModifyOptions("water")!;
        Assert.False(v.Can);
        Assert.False(v.Offered);
    }

    [Fact]
    public void Forklifts_without_drivers_are_offered_the_change_and_not_once_it_is_signed()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 4) s.AdvanceDay();
        Assert.True(s.Apply(new AwardAction
        {
            Id = "fork", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3, StartDay = 8,
            DurationDays = 13, FundingLine = "om1",
        }).Accepted);

        Assert.True(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn" }).Accepted);
        while (s.Day < 8) s.AdvanceDay();

        // Before the operators go on the request: offered, greyed, with the reason that says what to find out.
        var early = s.View.ModifyOptions("fork")!;
        Assert.True(early.Offered);
        Assert.False(early.Can);
        Assert.Equal("The request does not ask for operators. Find out what it needs first.", early.Why);

        Assert.True(s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" }).Accepted);
        while (s.Day < 9) s.AdvanceDay();
        var v = s.View.ModifyOptions("fork")!;
        Assert.True(v.Offered);
        Assert.True(v.Can, v.Why);

        Assert.True(s.Apply(new ModifyAction { Arrangement = "fork" }).Accepted);
        var writing = s.View.ModifyOptions("fork")!;
        Assert.True(writing.Offered);   // greyed: "A change to ... is already being written."
        Assert.False(writing.Can);
        while (s.Work.Last().CompletedDay is null) s.AdvanceDay();
        var after = s.View.ModifyOptions("fork")!;
        Assert.False(after.Offered);
        Assert.Equal(["Operators added from day 10."], after.Added);
    }

    [Fact]
    public void A_card_buy_cancelled_while_delivering_reads_ended_the_next_morning()
    {
        var s = TutorialWithCardWater();
        while (s.Day < 4) s.AdvanceDay();
        Assert.Equal(ArrangementStatus.Active, s.View.StatusToday("water"));
        Assert.True(s.Apply(new TerminateAction { Arrangement = "water", Assignee = "novak" }).Accepted);
        while (s.Work.Last().CompletedDay is null) s.AdvanceDay();
        Assert.Equal(5, s.Day);
        Assert.Equal(4, s.Arrangements.Single(a => a.Id == "water").EndDay);
        Assert.Equal(ArrangementStatus.Ended, s.View.StatusToday("water"));
        Assert.False(s.View.ModifyOptions("water")!.Offered);
        Assert.Null(s.View.StatusToday("nothing"));
    }

    [Fact]
    public void A_contract_ending_on_its_own_reads_ended_the_morning_after_its_last_day()
    {
        var s = TutorialWithCardWater();
        while (s.Day < 8) s.AdvanceDay();
        Assert.Equal(ArrangementStatus.Active, s.View.StatusToday("water"));
        s.AdvanceDay();
        Assert.Equal(ArrangementStatus.Ended, s.View.StatusToday("water"));
    }
}
