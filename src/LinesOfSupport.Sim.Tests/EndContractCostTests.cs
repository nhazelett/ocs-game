using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-196 (Claude play 21 Nov, eleventh play, note P2-12). The second player ended Harbor's own-initiative BPA
/// call on day 5 and the dialog said what stops and nothing about the $4,860 committed; they found it back on Payments a day
/// later. The end-contract quote now says what stays owed and what comes back to the line, as the termination charges it.</summary>
public class EndContractCostTests
{
    private static InputLog SecondLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-11", "input-log-2.json"));

    [Fact]
    public void Harbors_call_on_day_five_says_nothing_is_owed_and_the_commitment_comes_back()
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 4);
        Assert.Equal(5, s.Day);
        var words = s.View.EndWords("player-award-2")!;
        Assert.Equal("End this contract", words.Button);
        Assert.Equal("Ends this contract's support when the work and any notice are complete. Nothing is owed: it has not started, "
            + "and the $4,860 committed comes back to the O&M line.", words.Says);
    }

    [Fact]
    public void The_quote_matches_what_the_termination_charges()
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 4);
        var line = s.Funding.First(f => f.Id == s.Arrangements.First(a => a.Id == "player-award-2").FundingLineId);
        var before = line.ObligatedCents;
        Assert.True(s.Apply(new TerminateAction { Day = s.Day, Arrangement = "player-award-2" }).Accepted);
        Assert.Contains("Nothing is owed", s.View.EndWords("player-award-2")!.Says);   // the termination in work says the same
        while (s.Arrangements.First(a => a.Id == "player-award-2").Status != ArrangementStatus.Ended) s.AdvanceDay();
        Assert.Equal(0, s.Arrangements.First(a => a.Id == "player-award-2").ObligatedCents);
        Assert.Equal(before - 486000, line.ObligatedCents);
    }

    [Fact]
    public void A_running_contract_says_what_it_is_owed_what_comes_back_and_the_claim_for_skipping_the_notice()
    {
        // The sustainment scenario's inherited fuel contract at Spoke B: $7,000 a day, $420,000 committed, 15 days' notice.
        var bundle = Repo.Load("sustainment");
        var s = Session.Create(bundle, 1);
        while (s.Day < 5) s.AdvanceDay();
        var a = s.Arrangements.Single(x => x.Id == "bFuel");
        var end = new TerminateAction { Day = s.Day, Arrangement = "bFuel" };
        var last = s.View.PreviewAny(end).EarliestStartMin;
        var says = s.View.EndWords("bFuel", end)!.Says;
        var committed = a.ObligatedCents;
        var claimDays = bundle.Rules.Claims.SettlementDays;
        Assert.True(s.Apply(end).Accepted);
        while (a.Status != ArrangementStatus.Ended) s.AdvanceDay();
        Assert.Equal(last, a.EndDay);
        var claim = Assert.Single(s.View.Claims);
        var kept = a.ObligatedCents - (claim.SettledDay is null ? 0 : claim.AmountCents);   // the settlement is committed later
        Assert.Equal(700000L * claimDays, claim.AmountCents);
        Assert.Equal("Ends this contract's support when the work and any notice are complete. "
            + $"{Money(kept)} is owed for what it supplies through day {last}, and the other {Money(committed - kept)} "
            + "committed comes back to the O&M line good through day 45. Ending it without the 15 days' notice the contract requires also brings a claim "
            + $"from Second Island Fuel of about {Money(claim.AmountCents)}, charged to the O&M line good through day 45.", says);
    }

    [Fact]
    public void With_notice_there_is_no_claim_and_it_is_owed_through_the_notice()
    {
        var s = Session.Create(Repo.Load("sustainment"), 1);
        while (s.Day < 5) s.AdvanceDay();
        var end = new TerminateAction { Day = s.Day, Arrangement = "bFuel", WithNotice = true };
        var last = s.View.PreviewAny(end).EarliestStartMin;
        var says = s.View.EndWords("bFuel", end)!.Says;
        // Day -39 to day 20 is 60 days at $7,000: all $420,000 committed stays owed, nothing comes back, and no claim.
        Assert.Equal(20, last);
        Assert.Equal("Ends this contract's support when the work and any notice are complete. $420,000 is owed for what it supplies through day 20.", says);
    }

    private static string Money(long cents) => (cents / 100m).ToString("$#,##0", System.Globalization.CultureInfo.InvariantCulture);
}
