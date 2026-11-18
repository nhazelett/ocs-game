using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-104 (FOR-NICK #23 (a), approved 18 Nov 2026): stop a buy still being written. In the 18 Nov Claude play the
/// player called the Harbor Equipment BPA for forklifts without drivers on day 4 and handed it to MSgt Avery; SMSgt Dunn
/// said that night the forklifts need drivers; on day 5 the player wrote the right purchase order with Coastal Rentals and
/// then looked everywhere for a way to stop the first call.
/// </summary>
public class StopWorkTests
{
    private static InputLog Play()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-18", "input-log.json"));

    /// <summary>The morning of day 5: the call is with Avery, Dunn has answered, nothing else bought.</summary>
    private static Session Day5() => Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: 4);

    private static WorkItem Call(Session s) => Assert.Single(s.Work, w => w.SubjectId == "player-award-2");

    private static RequestStanding Forklifts(Session s) => Assert.Single(s.View.WhereThingsStand(), e => e.CardId == "tutForklift");

    [Fact]
    public void The_call_in_work_says_what_stopping_it_does()
    {
        var s = Day5();
        var call = Call(s);
        Assert.Null(call.CompletedDay);
        // It waits on finance to certify the funds this morning, so nobody has worked on it yet.
        var expected = "Stops the BPA call with Harbor Equipment Co. No hours have gone into it yet, and nothing is owed.";
        Assert.Equal(expected, s.View.StopWorkSentence(call.Id));

        // The team page carries it beside the task, and the decisions list beside the decision that made it.
        var avery = Assert.Single(s.View.Team, m => m.Id == "avery");
        Assert.Equal(expected, Assert.Single(avery.Work, w => w.WorkId == call.Id).StopSentence);
        var decision = Assert.Single(s.View.Decisions("tutForklift"), d => d.Day == 4 && d.StopWorkId is not null);
        Assert.Equal(call.Id, decision.StopWorkId);
        Assert.Equal(expected, decision.StopSentence);
        Assert.DoesNotContain("player-award", expected, StringComparison.Ordinal);
    }

    [Fact]
    public void Stopping_leaves_the_request_as_it_stood_before_the_call()
    {
        var s = Day5();
        var call = Call(s);
        var before = s.Funding.Sum(f => f.ObligatedCents);
        var hoursWorked = s.Metrics["shop.hoursWorked"];
        var queued = Assert.Single(s.View.Team, m => m.Id == "avery").HoursQueued;
        Assert.True(s.Apply(new StopWorkAction { Work = call.Id }).Accepted);

        // The work leaves the queue; the hours already spent stay spent; nothing is obligated.
        Assert.Equal(5, call.StoppedDay);
        var avery = Assert.Single(s.View.Team, m => m.Id == "avery");
        Assert.DoesNotContain(avery.Work, w => w.WorkId == call.Id);
        Assert.True(avery.HoursQueued < queued);
        Assert.Equal(hoursWorked, s.Metrics["shop.hoursWorked"]);
        Assert.Equal(before, s.Funding.Sum(f => f.ObligatedCents));

        // The request stands as it does on the same morning of a play that never made the call.
        var log = Play();
        var without = log with { Actions = [.. log.Actions.Where(a => !(a.Day == 4 && a is AwardAction or AssignAction))] };
        var never = Forklifts(Replay.Run(Repo.Load("tutorial"), without, stopAfterDay: 4));
        var now = Forklifts(s);
        Assert.Equal(never.State, now.State);
        Assert.Equal(never.Sentence, now.Sentence);
        Assert.Equal(never.LeavesOut, now.LeavesOut);
        Assert.Equal(never.StillToFind, now.StillToFind);
        Assert.Null(now.LeavesOut);
        Assert.DoesNotContain(s.View.MorningBoard().Items, i => i.LinkId == "tutForklift" && i.Severity == BoardSeverity.Alert);
    }

    [Fact]
    public void The_decisions_list_and_the_record_say_it_was_stopped()
    {
        var s = Day5();
        var call = Call(s);
        Assert.True(s.Apply(new StopWorkAction { Work = call.Id }).Accepted);

        var decisions = s.View.Decisions("tutForklift");
        var stop = decisions[0];
        Assert.Equal("Day 5: Stopped the BPA call with Harbor Equipment Co for material handling equipment at Hub.", stop.Sentence);
        Assert.Equal("avery", stop.TeammateId);
        Assert.Equal("Stopped. No hours went into it, and nothing is owed.", stop.Now);
        Assert.Null(stop.StopWorkId);
        var original = Assert.Single(decisions, d => d.Day == 4 && d.Sentence.Contains("Harbor Equipment Co", StringComparison.Ordinal) && d != stop);
        Assert.Equal("Stopped on day 5 before it was signed; nothing was bought.", original.Now);
        Assert.Null(original.StopWorkId);
        Assert.Null(original.StopSentence);

        var rec = s.Records.Last(r => r.Kind == RecordKind.Action);
        Assert.Equal("player-award-2", rec.Subject);
        Assert.Equal("Stopped the BPA call with Harbor Equipment Co for material handling equipment at Hub before it was signed. No hours went into it, and nothing is owed.", rec.Text);
        Assert.Contains(call.CauseId, rec.CausedBy);
    }

    [Fact]
    public void A_stopped_call_is_never_awarded_or_declined_and_the_day_does_not_call_it_done()
    {
        var s = Day5();
        var call = Call(s);
        Assert.True(s.Apply(new StopWorkAction { Work = call.Id }).Accepted);
        s.AdvanceDay();
        Assert.DoesNotContain(s.View.DaySummary()!.Finished, l => l.Sentence.Contains("Harbor Equipment", StringComparison.Ordinal));
        for (var d = 0; d < 6; d++) s.AdvanceDay();
        Assert.DoesNotContain(s.Arrangements, a => a.Id == "player-award-2");
        Assert.DoesNotContain(s.Records, r => r.Subject == "player-award-2" && r.Kind is RecordKind.Declined or RecordKind.ArrangementStart);
        Assert.Equal(5, call.CompletedDay);
    }

    [Fact]
    public void Only_a_buy_still_being_written_can_be_stopped()
    {
        var s = Day5();
        var call = Call(s);
        // Anything that is not a buy (the ask about the forklifts, the staff meeting) has no button and is refused.
        foreach (var other in s.Work.Where(w => w.Kind != WorkKind.Award))
        {
            Assert.Null(s.View.StopWorkSentence(other.Id));
            var refused = s.Apply(new StopWorkAction { Work = other.Id });
            Assert.False(refused.Accepted);
        }
        Assert.False(s.Apply(new StopWorkAction { Work = "nothing" }).Accepted);
        // The card buy for water was done the day it went in: it never made work to stop.
        Assert.DoesNotContain(s.Work, w => w.SubjectId == "player-award-1");

        Assert.True(s.Apply(new StopWorkAction { Work = call.Id }).Accepted);
        Assert.Null(s.View.StopWorkSentence(call.Id));
        var again = s.Apply(new StopWorkAction { Work = call.Id });
        Assert.Equal("That work was already stopped", Assert.Single(again.HardStops));
        Assert.False(s.Apply(new AssignAction { Work = call.Id, To = "you" }).Accepted);
    }

    [Fact]
    public void Finished_work_cannot_be_stopped()
    {
        // Played through day 8 the call was sent and Harbor turned it down: there is nothing left to stop.
        var s = Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: 8);
        var call = Call(s);
        Assert.NotNull(call.CompletedDay);
        Assert.Null(s.View.StopWorkSentence(call.Id));
        Assert.Equal("That work is already done", Assert.Single(s.Apply(new StopWorkAction { Work = call.Id }).HardStops));
        Assert.Null(Assert.Single(s.View.Decisions("tutForklift"), d => d.Day == 4 && d.Sentence.Contains("Harbor Equipment Co", StringComparison.Ordinal)).StopWorkId);
    }

    [Fact]
    public void Hours_already_spent_stay_spent()
    {
        // The morning of day 10 of the same play: Avery put 8 of the 12.8 hours into the waste removal purchase order on day 9.
        var s = Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: 9);
        var order = Assert.Single(s.Work, w => w.SubjectId == "player-award-4");
        Assert.Equal(4_800, order.HoursRemainingMilli);
        Assert.Equal("Stops the purchase order with Main Island Sanitation. The 8 hours MSgt Joan Avery spent stay spent; nothing is owed.",
            s.View.StopWorkSentence(order.Id));
        var worked = s.Metrics["shop.hoursWorked"];
        Assert.True(s.Apply(new StopWorkAction { Work = order.Id }).Accepted);
        Assert.Equal(worked, s.Metrics["shop.hoursWorked"]);
        Assert.Equal(4_800, order.HoursRemainingMilli);
        Assert.EndsWith("The 8 hours MSgt Joan Avery spent stay spent; nothing is owed.", s.Records.Last().Text, StringComparison.Ordinal);
        s.AdvanceDay();
        Assert.DoesNotContain(s.Arrangements, a => a.Id == "player-award-4");
    }

    [Fact]
    public void Work_not_started_says_no_hours_went_into_it()
    {
        // Day 4 of the same play, the moment the call went in: no hours are spent on it yet.
        var log = Play();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 3);
        foreach (var a in log.Actions.Where(a => a.Day == 4)) Assert.True(s.Apply(a).Accepted);
        var call = Call(s);
        Assert.Equal("Stops the BPA call with Harbor Equipment Co. No hours have gone into it yet, and nothing is owed.", s.View.StopWorkSentence(call.Id));
        Assert.True(s.View.PreviewAny(new StopWorkAction { Work = call.Id }).Accepted);
        Assert.True(s.Apply(new StopWorkAction { Work = call.Id }).Accepted);
        Assert.EndsWith("No hours went into it, and nothing is owed.", s.Records.Last().Text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_log_with_a_stop_replays_to_the_same_state()
    {
        var s = Day5();
        Assert.True(s.Apply(new StopWorkAction { Work = Call(s).Id }).Accepted);
        for (var d = 0; d < 4; d++) s.AdvanceDay();
        var path = Path.Combine(Path.GetTempPath(), $"stopwork-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, s.ToInputLog().ToJson());
        var log = InputLog.Load(path);
        File.Delete(path);
        Assert.Contains(log.Actions, a => a is StopWorkAction);
        var again = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: s.View.Day - 1);
        Assert.Equal(s.StateHash(), again.StateHash());
    }
}
