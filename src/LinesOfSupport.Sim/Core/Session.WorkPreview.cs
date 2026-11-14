using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// DESK-50 (Q23, D17): the actions that had no preview of their own (a survey, coordination, definitizing a UCA, a
// termination, a cure notice, a decision on a commitment, and handing work to someone else) get one, so every assignee
// picker can say when the work would come out with each person. Each check here is the one its Apply runs, moved, not
// copied. Rolled hours (definitization) are previewed as their range, never as the roll the action will draw.
public sealed partial class Session
{
    // ------------------------------------------------------------------ checks, shared with Apply

    private string? CheckSurvey(SurveyAction s)
    {
        var k = _knowledge.FirstOrDefault(x => x.IslandId == s.Island);
        if (k is null) return $"island {s.Island} does not exist";
        if (k.Tier is "surveyed" or "observed") return $"{IslandData(s.Island).Name} is already surveyed";
        if (k.SurveyReturnsDay is not null || _work.Any(w => w.Kind == WorkKind.Survey && w.CompletedDay is null && (string?)w.Payload == s.Island))
            return $"a survey of {IslandData(s.Island).Name} is already in work";
        return null;
    }

    private List<string> CheckCoordinate(CoordinateAction c)
    {
        var stops = new List<string>();
        if (string.IsNullOrWhiteSpace(c.Id) || _arrangements.Any(x => x.Id == c.Id)) stops.Add("coordination needs a new id");
        if (!Scenario.Sites.Any(s => s.Id == c.Site)) stops.Add($"site {c.Site} does not exist");
        if (!Scenario.Sites.Any(s => s.Id == c.FromSite)) stops.Add($"site {c.FromSite} does not exist");
        if (!_capData.ContainsKey(c.Capability)) stops.Add($"capability {c.Capability} does not exist");
        if (c.Quantity <= 0) stops.Add("quantity must be positive");
        return stops;
    }

    private List<string> CheckDefinitize(DefinitizeAction d)
    {
        var arr = _arrangements.FirstOrDefault(x => x.Id == d.Arrangement);
        if (arr?.Uca is not { } u) return [$"{NameOf(d.Arrangement)} is not a UCA"];
        if (u.Definitized) return [$"{NameOf(d.Arrangement)} is already definitized"];
        if (u.DefinitizationInWork) return [$"definitization of {NameOf(d.Arrangement)} is already in work"];
        if (!u.ProposalReceived) return [$"{VendorData(arr.VendorId)!.Name} has not submitted a qualifying proposal yet"];
        if (_funding.FirstOrDefault(f => f.Id == d.FundingLine) is not { } dl) return [$"{NameOf(d.FundingLine)} does not exist"];
        if (!dl.Pays(arr.CapabilityId)) return [.. FundingStops(dl, arr.CapabilityId, null, 0)];
        return [];
    }

    private string? CheckTerminate(TerminateAction t)
    {
        var arr = _arrangements.FirstOrDefault(x => x.Id == t.Arrangement);
        if (arr is null) return $"{NameOf(t.Arrangement)} does not exist";
        if (arr.Status is ArrangementStatus.Ended or ArrangementStatus.Declined) return $"{NameOf(t.Arrangement)} is not running";
        if (NotOursToEnd(arr) is { } notOurs) return notOurs;
        if (_work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == t.Arrangement))
            return $"termination of {NameOf(t.Arrangement)} is already in work";
        if (t.WithNotice && arr.NoticeDays is not > 0) return $"{NameOf(t.Arrangement)} has no termination notice clause";
        return null;
    }

    private string? CheckCureNotice(CureNoticeAction c)
    {
        var i = _issues.FirstOrDefault(x => x.Id == c.Issue);
        if (i is null) return $"{NameOf(c.Issue)} does not exist";
        if (i.CureNoticeDay is not null) return $"a cure notice on {NameOf(i.Id)} was already issued on day {i.CureNoticeDay}";
        if (_work.Any(w => w.CompletedDay is null && w.Kind == WorkKind.CureNotice && (string?)w.Payload == i.Id))
            return $"a cure notice on {NameOf(i.Id)} is already in work";
        var a = _arrangements.First(x => x.Id == i.ArrangementId);
        if (a.Status is not (ArrangementStatus.Active or ArrangementStatus.Suspended)) return $"{NameOf(a.Id)} is not running";
        return null;
    }

    private string? CheckResolve(ResolveCommitmentAction r)
    {
        var c = _commitments.FirstOrDefault(x => x.Id == r.Commitment);
        if (c is null) return $"commitment {r.Commitment} does not exist";
        if (!c.Unresolved) return $"{NameOf(c.Id)} is already {c.Decision.ToString().ToLowerInvariant()}";
        var allowed = c.Kind == CommitmentKind.Handshake ? new[] { "ratify", "park", "refuse" } : ["ratify", "deny"];
        if (!allowed.Contains(r.Choice)) return $"{r.Choice} is not a choice for {NameOf(c.Id)}; choose {string.Join(", ", allowed)}";
        if (r.FundingLine is not null && !_funding.Any(f => f.Id == r.FundingLine)) return $"{NameOf(r.FundingLine)} does not exist";
        if (c.Decision == CommitmentDecision.Parked && r.Choice == "park") return $"{NameOf(c.Id)} is already parked";
        return null;
    }

    /// <summary>The hand-over checks ApplyAssign runs, except "already doing it", which the picker shows as the current holder.</summary>
    private string? CheckAssign(AssignAction a, out WorkItem? work)
    {
        work = _work.FirstOrDefault(x => x.Id == a.Work);
        if (work is null) return "that work is not in the queue";
        if (work.CompletedDay is not null) return "that work is already done";
        if (_team.All(m => m.Id != a.To)) return "that person is not on the team";
        return null;
    }

    // ------------------------------------------------------------------ previews

    /// <summary>
    /// A preview for an action that queues work of a known size (in usual hours, min and max) and has its result
    /// <paramref name="afterDays"/> after the work is done. Hours are the assignee's own; both days are behind their queue.
    /// </summary>
    private ActionPreview WorkPreview(PlayerAction a, IReadOnlyList<string> stops, long minMilli, long maxMilli, int afterDays = 0,
        IReadOnlyList<string>? gaps = null)
    {
        var lo = PersonHours(minMilli, a.Assignee);
        var hi = PersonHours(maxMilli, a.Assignee);
        return new ActionPreview(stops.Count == 0, stops, gaps ?? [], 0, 0, 0, CeilHours(lo), CeilHours(hi),
            FinishDay(lo, a.Assignee, Day) + afterDays, FinishDay(hi, a.Assignee, Day) + afterDays, QueueHours(a.Assignee));
    }

    private ActionPreview BuildSurveyPreview(SurveyAction s)
    {
        var h = Fixed.Milli(Rules.Model.Work.SurveyShopHours);
        return WorkPreview(s, CheckSurvey(s) is { } stop ? [stop] : [], h, h, Rules.Model.Work.SurveyReturnDays);
    }

    private ActionPreview BuildDefinitizePreview(DefinitizeAction d)
    {
        var range = Rules.Uca.DefinitizationShopDays;
        return WorkPreview(d, CheckDefinitize(d), Hours(range.Min, true), Hours(range.Max, true));
    }

    private ActionPreview BuildTerminatePreview(TerminateAction t)
    {
        var stop = CheckTerminate(t);
        var arr = _arrangements.FirstOrDefault(x => x.Id == t.Arrangement);
        var h = arr is { IsCoordination: true } ? 0 : Hours(Rules.Model.Work.TerminationShopDays, true);
        // The result day is the last day the arrangement runs: the day the termination is done, plus any notice.
        var notice = t.WithNotice && stop is null ? arr!.NoticeDays ?? 0 : 0;
        return WorkPreview(t, stop is null ? [] : [stop], h, h, notice);
    }

    private ActionPreview BuildCureNoticePreview(CureNoticeAction c)
    {
        var h = Fixed.Milli(Perf.CureNoticeShopHours);
        return WorkPreview(c, CheckCureNotice(c) is { } stop ? [stop] : [], h, h);
    }

    private ActionPreview BuildResolvePreview(ResolveCommitmentAction r)
    {
        var h = r.Choice switch
        {
            "ratify" => Fixed.Milli(Rules.Model.Work.RatificationShopDays * Rules.Model.HoursPerPersonPerDay),
            "deny" => Fixed.Milli(Uc.OutOfScopeDenyShopHours),
            _ => 0,   // parking or refusing is decided at once and queues nothing
        };
        return WorkPreview(r, CheckResolve(r) is { } stop ? [stop] : [], h, h);
    }

    /// <summary>Coordination queues no work: it starts after the other unit's lag whoever arranges it.</summary>
    private ActionPreview BuildCoordinatePreview(CoordinateAction c)
    {
        var lag = Rules.Instruments["coordination"].StartLagDays;
        var stops = CheckCoordinate(c);
        return new ActionPreview(stops.Count == 0, stops, ["The other unit can withdraw it: coordination is not a contract."], 0, 0, 0, 0, 0,
            Day + lag.Min, Day + lag.Max, 0);
    }

    /// <summary>
    /// Handing work to <see cref="AssignAction.To"/> (Q23): the hours left at their pace, and the day it would be done with
    /// them. The work keeps its place in the order it was given, so it is played forward among their own work at that
    /// place, not at the back. For the person who already holds it, the preview is refused and its days are when it is
    /// done where it is. QueueAheadHours is their work given before it.
    /// </summary>
    private ActionPreview BuildAssignPreview(AssignAction a)
    {
        var stop = CheckAssign(a, out var w);
        if (stop is not null) return new ActionPreview(false, [stop], [], 0, 0, 0, 0, 0, Day, Day, QueueHours(a.To));
        var to = Member(a.To);
        var holder = w!.Assignee == to.Id;
        var hours = HoursIfAssignedMilli(w, to);
        var done = PlayQueue(to, 0, Day, w, hours) ?? Day;
        var index = _work.IndexOf(w);
        var ahead = CeilHours(_work.Take(index).Where(x => x.CompletedDay is null && x.Kind != WorkKind.GpcProgram && Member(x.Assignee).Id == to.Id)
                                   .Sum(x => x.HoursRemainingMilli));
        List<string> stops = holder ? [$"{WhoIs(to.Id)} already doing it"] : [];
        return new ActionPreview(!holder, stops, [], 0, 0, 0, CeilHours(hours), CeilHours(hours), done, done, ahead);
    }

    /// <summary>The preview of any action that has one, for the assignee picker (DESK-37, DESK-50).</summary>
    internal ActionPreview PreviewAny(PlayerAction a) => a switch
    {
        AwardAction x => BuildPreview(x),
        EstablishBpaAction x => BuildEstablishPreview(x),
        EstablishIdiqAction x => BuildEstablishIdiqPreview(x),
        HostNationRequestAction x => BuildHostNationPreview(x),
        AcsaOrderAction x => BuildAcsaPreview(x),
        RequestExternalSupportAction x => BuildExternalSupportPreview(x),
        ReviewFilesAction x => BuildReviewFilesPreview(x),
        SurveyAction x => BuildSurveyPreview(x),
        CoordinateAction x => BuildCoordinatePreview(x),
        DefinitizeAction x => BuildDefinitizePreview(x),
        TerminateAction x => BuildTerminatePreview(x),
        CureNoticeAction x => BuildCureNoticePreview(x),
        ResolveCommitmentAction x => BuildResolvePreview(x),
        AssignAction x => BuildAssignPreview(x),
        _ => BuildVisitPreview(a),
    };
}
