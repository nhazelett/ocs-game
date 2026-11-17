using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// A quiet day, and what stops a skip (DESK-75, docs/design/desk-redesign.md D30). The 17 Nov gameplay review found twelve
// days in a row in the tutorial with nothing to decide, each ended by the same button. A day is quiet when nothing is open:
// no request without cover, no decision waiting, no teammate idle while work is ready, nothing short and nothing new this
// morning. On a quiet day the day's plan can offer Skip ahead, which ends days until the next morning that is not quiet or
// the next day the shop is waiting on (support starting or ending, a site opening), seven days at most. A forecast from
// the plan, like the day's plan: it ignores rolls. Read only.
public sealed partial class Session
{
    /// <summary>A skip ends at most this many days.</summary>
    internal const int SkipMaxDays = 7;

    internal bool BuildQuietDay() => QuietBlocker() is null;

    /// <summary>What keeps today from being quiet, in a few words, or null when it is quiet. For tests and the plan.</summary>
    internal string? QuietBlocker()
    {
        if (Finished) return "the deployment is over";
        if (Day >= LastDay) return "it is the last day";
        if (BuildWhereThingsStand().FirstOrDefault(r => r.State is RequestStanding.Nothing or RequestStanding.Asking or RequestStanding.Short) is { } open)
            return $"{Lower(open.Title)} is not covered";
        if (_commitments.Any(c => c.Decision is CommitmentDecision.Open or CommitmentDecision.Parked))
            return "a commitment waits for your decision";
        if (_issues.Any(i => i.Outcome == CureOutcome.None && i.CureNoticeDay is null))
            return "a vendor problem waits for your decision";
        var board = BuildMorningBoard();
        if (board.New.Count > 0) return "there is news this morning";
        if (board.Items.Any(i => i.Severity == BoardSeverity.Alert)) return "something was short yesterday";
        if (IdleWhileWorkWaits() is { } idle) return idle;
        return null;
    }

    /// <summary>A teammate with free hours today while someone else's ready work carries over; null when none.</summary>
    private string? IdleWhileWorkWaits()
    {
        var (people, _) = PlanPeople();
        var carried = people.SelectMany(p => p.Work.Where(w => w.WorkId is not null && !w.FinishesToday).Select(w => (Who: p, Item: w))).FirstOrDefault();
        if (carried.Item is null) return null;
        var idle = BuildTeam().FirstOrDefault(m => m.Role != "you" && m.FreeHoursToday > 0 && m.Id != carried.Who.MemberId);
        return idle is null ? null : $"{(idle.Rank is { Length: > 0 } r ? $"{r} {idle.Name}" : idle.Name)} has free hours while {(carried.Who.Name == "You" ? "your" : carried.Who.Name + "'s")} work carries over";
    }

    /// <summary>
    /// What would stop a skip from today, as one sentence, and the morning it stops on at the latest: the next day the shop
    /// is waiting on (support starting or ending, a site opening), seven days on, or the last day. A job finishing needs no
    /// stop of its own: an answer is news the next morning, and a buy's real event is its start. Null when
    /// today is not quiet.
    /// </summary>
    internal SkipStopsView? BuildSkipStops()
    {
        if (!BuildQuietDay()) return null;
        var next = NextKnownDay();
        var cap = Math.Min(LastDay, Day + SkipMaxDays);
        if (next is { } n && n.Day <= cap)
            return new SkipStopsView($"Stops when anything needs you, and on day {n.Day} {n.Why}.", n.Day);
        return cap == LastDay
            ? new SkipStopsView($"Stops when anything needs you, and on day {LastDay}, the last day.", LastDay)
            : new SkipStopsView($"Stops when anything needs you, and after {SkipMaxDays} days at most, on day {cap}.", cap);
    }

    /// <summary>The next morning something the shop is waiting on happens, and why, in "when ..." words.</summary>
    private (int Day, string Why)? NextKnownDay()
    {
        var found = new List<(int Day, int Order, string Why)>();
        foreach (var a in _arrangements.Where(a => a.Status == ArrangementStatus.Pending && a.StartDay > Day && !ForShop(a.CapabilityId)))
            found.Add((a.StartDay, 0, $"when {SupplierName(a)} starts {Lower(CapLabel(a.CapabilityId))} at {SiteName(a.SiteId)}"));
        foreach (var a in _arrangements.Where(a => a.Status is ArrangementStatus.Active or ArrangementStatus.Pending or ArrangementStatus.Suspended
                                                   && a.EndDay >= Day && a.EndDay < LastDay && !ForShop(a.CapabilityId)))
            found.Add((a.EndDay + 1, 1, $"after {Lower(CapLabel(a.CapabilityId))} at {SiteName(a.SiteId)} from {SupplierName(a)} ends"));
        foreach (var site in Scenario.Sites.Where(s => s.OpensDay > Day))
            found.Add((site.OpensDay, 3, $"when {site.Name} opens"));
        var first = found.Where(f => f.Day > Day).OrderBy(f => f.Day).ThenBy(f => f.Order).FirstOrDefault();
        return first.Why is null ? null : (first.Day, first.Why);
    }
}
