using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Transition plans from the OCS and CCO seats (backlog M3-05, SPEC 2 "plan a transition", SPEC 3 "Transition plan").
// A plan links an outgoing arrangement to an incoming one at the same site and capability. It does two things the
// player would otherwise do by hand: it ends the outgoing at the cut-over (an ordinary termination, with its shop hours),
// and it watches the handoff, flagging a projected gap on the morning board before the outgoing runs out. It draws no
// random number and changes nothing until it acts, so games without a plan replay exactly as before.
public sealed partial class Session
{
    internal const string HandoffIncomingDelivering = "incomingDelivering";
    internal const string HandoffOnDay = "onDay";

    private readonly List<TransitionState> _transitions = [];
    /// <summary>Transition plans made this game, with their clocks and outcome (M3-05).</summary>
    public IReadOnlyList<TransitionState> Transitions => _transitions;

    private static string OwnerName(string owner) => owner == "ocs" ? "OCS seat" : "CCO seat";

    internal sealed record TransitionCheck(List<string> Stops, Arrangement? Outgoing, string? SiteId, string? CapabilityId,
                                           int OverlapDays, string Handoff, string Owner);

    /// <summary>Every check a plan must pass. Reads state only.</summary>
    internal TransitionCheck CheckTransition(PlanTransitionAction p)
    {
        var stops = new List<string>();
        var overlap = p.OverlapDays ?? Rules.Transition.DefaultOverlapDays;
        var handoff = p.Handoff ?? HandoffIncomingDelivering;
        var owner = p.Owner ?? "cco";
        TransitionCheck Done(Arrangement? o, string? site, string? cap) => new(stops, o, site, cap, overlap, handoff, owner);

        if (string.IsNullOrWhiteSpace(p.Id)) stops.Add("a transition plan needs an id");
        else if (_transitions.Any(t => t.Id == p.Id)) stops.Add($"{Upper(NameOf(p.Id))} already exists");

        var outA = _arrangements.FirstOrDefault(a => a.Id == p.Outgoing);
        if (outA is null) { stops.Add($"{NameOf(p.Outgoing)} is not an arrangement"); return Done(null, null, null); }
        if (outA.Status is ArrangementStatus.Ended or ArrangementStatus.Declined) stops.Add($"{NameOf(outA.Id)} is not running");
        if (p.Incoming == p.Outgoing) { stops.Add("the outgoing and the incoming are the same arrangement"); return Done(outA, null, null); }

        string? inSite, inCap;
        if (_arrangements.FirstOrDefault(a => a.Id == p.Incoming) is { } inA)
        {
            (inSite, inCap) = (inA.SiteId, inA.CapabilityId);
            if (inA.Status is ArrangementStatus.Ended or ArrangementStatus.Declined) stops.Add($"{NameOf(inA.Id)} is not running");
        }
        else if (_external.FirstOrDefault(r => r.Id == p.Incoming) is { } req)
        {
            (inSite, inCap) = (req.SiteId, req.CapabilityId);
            if (req.Status == ExternalSupportStatus.Refused) stops.Add($"{NameOf(req.Id)} was refused");
        }
        else { stops.Add($"{NameOf(p.Incoming)} is not an arrangement or an external support request"); return Done(outA, null, null); }

        if (inSite != outA.SiteId || inCap != outA.CapabilityId)
            stops.Add($"{NameOf(p.Incoming)} is {CapLabel(inCap!).ToLowerInvariant()} at {SiteName(inSite!)}; {NameOf(outA.Id)} is {CapLabel(outA.CapabilityId).ToLowerInvariant()} at {SiteName(outA.SiteId)}");
        if (_transitions.FirstOrDefault(t => t.Outcome == TransitionOutcome.Open && t.OutgoingId == outA.Id) is { } open)
            stops.Add($"{NameOf(outA.Id)} is already the outgoing side of {NameOf(open.Id)}");
        if (overlap < 1) stops.Add("the overlap must be at least one day: the cut-over waits for the incoming to deliver");
        if (owner is not ("cco" or "ocs")) stops.Add($"gap owner {owner} is not cco or ocs");
        if (handoff == HandoffOnDay)
        {
            if (p.CutoverDay is not { } c) stops.Add("an onDay handoff needs a cut-over day");
            else if (c <= Day) stops.Add($"the cut-over day must be after today (day {Day})");
            else if (c > outA.EndDay) stops.Add($"cut-over day {c} is after {NameOf(outA.Id)} ends (day {outA.EndDay})");
        }
        else if (handoff != HandoffIncomingDelivering) stops.Add($"handoff {handoff} is not incomingDelivering or onDay");
        return Done(outA, outA.SiteId, outA.CapabilityId);
    }

    private ActionResult ApplyPlanTransition(PlanTransitionAction p)
    {
        var chk = CheckTransition(p);
        if (chk.Stops.Count > 0) return ActionResult.Stop([.. chk.Stops]);
        var outA = chk.Outgoing!;
        var (min, max, lost) = IncomingStart(p.Incoming);
        var inText = lost is not null ? lost : min == max ? $"starts day {min}" : $"expected to start day {min} to {max}";
        var handText = chk.Handoff == HandoffOnDay
            ? $"cut-over on day {p.CutoverDay}"
            : $"cut-over after {Plural(chk.OverlapDays, "day")} of full delivery by {NameOf(p.Incoming)}";
        TransitionState Make(string cause) => new()
        {
            Id = p.Id, OutgoingId = outA.Id, IncomingId = p.Incoming, SiteId = outA.SiteId, CapabilityId = outA.CapabilityId,
            OverlapDays = chk.OverlapDays, Handoff = chk.Handoff, CutoverDay = chk.Handoff == HandoffOnDay ? p.CutoverDay : null,
            Owner = chk.Owner, PlannedDay = Day, CauseId = cause,
        };
        var warnings = TransitionWarnings(Make(""));
        var rec = AddRecord(Day, RecordKind.Action, p.Id,
            $"Transition planned: {CapLabel(outA.CapabilityId)} at {SiteName(outA.SiteId)} from {NameOf(outA.Id)} (ends day {outA.EndDay}) to {NameOf(p.Incoming)} ({inText}); {handText}; gap owner: {OwnerName(chk.Owner)}.{(warnings.Count == 0 ? "" : $" Planner: {string.Join(" ", warnings)}")}", [outA.CauseId]);
        _transitions.Add(Make(rec));
        return ActionResult.Ok(warnings.Count == 0 ? "transition planned" : $"transition planned; {string.Join(" ", warnings)}");
    }

    /// <summary>
    /// When the incoming can start, as the player knows it: an arrangement's start day; an external support request's
    /// range from where it stands (preparation, approval, mobilization, ignoring other work in the queue). Lost names why
    /// it will not arrive.
    /// </summary>
    private (int? Min, int? Max, string? Lost) IncomingStart(string incomingId)
    {
        if (_arrangements.FirstOrDefault(a => a.Id == incomingId) is { } a)
            return a.Status switch
            {
                ArrangementStatus.Declined => (null, null, $"{Upper(NameOf(a.Id))} was declined"),
                ArrangementStatus.Ended => (null, null, $"{Upper(NameOf(a.Id))} has ended"),
                _ => (a.StartDay, a.StartDay, null),
            };
        var r = _external.First(x => x.Id == incomingId);
        if (r.Status == ExternalSupportStatus.Refused) return (null, null, $"{NameOf(r.Id)} was refused: {r.RefusedReason}");
        var inst = Rules.Instruments[ExternalInstrument];
        var approval = inst.ApprovalLagDays ?? new IntRange(1, 1);
        var mob = inst.MobilizationDays ?? new IntRange(0, 0);
        int answerMin, answerMax;
        if (r.SubmittedDay is { } s)
            (answerMin, answerMax) = (Math.Max(s + approval.Min, Day), Math.Max(s + approval.Max, Day));
        else
        {
            var w = _work.FirstOrDefault(x => x.Kind == WorkKind.ExternalSupportRequest && x.CompletedDay is null && (x.Payload as string) == r.Id);
            var remaining = w?.HoursRemainingMilli ?? 0;
            var days = _hoursPerDayMilli <= 0 ? 0 : (int)((remaining + _hoursPerDayMilli - 1) / _hoursPerDayMilli);
            var submit = Math.Max(Day, w?.WaitUntilDay ?? Day) + Math.Max(0, days - 1);
            (answerMin, answerMax) = (submit + approval.Min, submit + approval.Max);
        }
        var floor = r.RequestedStartDay ?? 0;
        return (Math.Max(floor, answerMin + 1 + mob.Min), Math.Max(floor, answerMax + 1 + mob.Max), null);
    }

    /// <summary>The outgoing's last day as planned: its own end, or the cut-over day when that comes first.</summary>
    private int PlannedOutgoingEnd(TransitionState t)
    {
        var outA = _arrangements.First(a => a.Id == t.OutgoingId);
        return t.CutoverDay is { } c ? Math.Min(c, outA.EndDay) : outA.EndDay;
    }

    /// <summary>Soft gaps in a plan as it stands today: coverage gaps, short overlap, undersizing and funding. Reads state only.</summary>
    private List<string> TransitionWarnings(TransitionState t)
    {
        var w = new List<string>();
        if (t.Outcome != TransitionOutcome.Open) return w;
        var outA = _arrangements.First(a => a.Id == t.OutgoingId);
        var end = PlannedOutgoingEnd(t);
        var where = $"{CapLabel(t.CapabilityId)} at {SiteName(t.SiteId)}";
        var owner = $" Gap owner: {OwnerName(t.Owner)}.";
        var (min, max, lost) = IncomingStart(t.IncomingId);
        if (lost is not null)
            w.Add($"{Upper(NameOf(t.IncomingId))} will not arrive ({lost}): {where} has nothing behind {NameOf(outA.Id)} after day {end} unless another arrangement covers it.{owner}");
        else if (min > end)
            w.Add($"{Upper(NameOf(t.IncomingId))} is not expected before day {min} and {NameOf(outA.Id)} ends day {end}: a gap of at least {Plural(min - end - 1, "day")} (days {end + 1} to {min - 1}) unless another arrangement covers it.{owner}");
        else if (max > end)
            w.Add($"{Upper(NameOf(t.IncomingId))} may start as late as day {max} and {NameOf(outA.Id)} ends day {end}: up to {Plural(max - end - 1, "day")} of gap if it does.{owner}");
        else if (t.Handoff == HandoffIncomingDelivering && t.CutoverStartedDay is null && max + t.OverlapDays > end)
            w.Add($"{Upper(NameOf(outA.Id))} ends day {end}, before {Plural(t.OverlapDays, "day")} of full delivery by {NameOf(t.IncomingId)} can be verified (it starts day {max}): the handoff will be {NameOf(outA.Id)}'s own end, with {Plural(end - max!.Value + 1, "day")} of overlap.");
        else if (t.Handoff == HandoffOnDay && end - max!.Value + 1 < t.OverlapDays)
            w.Add($"Only {Plural(end - max.Value + 1, "day")} of overlap before the cut-over on day {end} if {NameOf(t.IncomingId)} starts day {max}; the plan asks for {t.OverlapDays}.");

        if (lost is null && max is { } latest)
        {
            long qty;
            int inEnd;
            if (_arrangements.FirstOrDefault(a => a.Id == t.IncomingId) is { } inA) (qty, inEnd) = (inA.QuantityMilli, inA.EndDay);
            else
            {
                var r = _external.First(x => x.Id == t.IncomingId);
                (qty, inEnd) = (r.QuantityMilli, latest + r.DurationDays - 1);
            }
            var from = Math.Min(Math.Max(end + 1, Day), LastDay);
            var to = Math.Min(inEnd, LastDay);
            if (to >= from)
            {
                var peak = (PeakDemandMilli(t.SiteId, t.CapabilityId, from, to) + 999) / 1000;
                var cap = Cap(t.CapabilityId);
                if (qty / 1000 < peak)
                    w.Add($"{Upper(NameOf(t.IncomingId))} supplies {qty / 1000} {Rate(cap)} against peak demand of {peak} {Rate(cap)} after the cut-over: the difference is a shortfall unless something else covers it.");
            }
            if (_external.FirstOrDefault(x => x.Id == t.IncomingId) is { Status: not ExternalSupportStatus.Awarded } req
                && _funding.FirstOrDefault(f => f.Id == req.FundingLineId) is { } line)
            {
                var inst = Rules.Instruments[ExternalInstrument];
                var answerLatest = latest - 1 - (inst.MobilizationDays?.Max ?? 0);
                if (line.ExpiresDay < answerLatest)
                    w.Add($"Funding: {NameOf(line.Id)} can be obligated only through day {line.ExpiresDay} and the external office may not award {NameOf(req.Id)} before day {answerLatest}: if the line lapses first the request is refused and the gap opens.");
                else if (line.RemainingCents < req.TotalCents)
                    w.Add($"Funding: {NameOf(line.Id)} has {Money(line.RemainingCents)} left for {NameOf(req.Id)}'s {Money(req.TotalCents)}: the external office will refuse it unless money is found.");
            }
        }
        return w;
    }

    /// <summary>
    /// Each day after supply is resolved: count the incoming's full days, start the cut-over when the handoff condition
    /// is met, flag a projected gap once the outgoing ends within rules transition.warnDays, and resolve the plan the day
    /// after the outgoing's last day.
    /// </summary>
    private void TransitionClocks(int d, Dictionary<string, Today> today)
    {
        foreach (var t in _transitions.Where(x => x.Outcome == TransitionOutcome.Open))
        {
            var outA = _arrangements.First(a => a.Id == t.OutgoingId);
            var inA = _arrangements.FirstOrDefault(a => a.Id == t.IncomingId);
            Today? ti = null;
            var inRunning = inA is not null && today.TryGetValue(inA.Id, out ti) && !ti.Suspended;
            var inFull = inRunning && ti!.FactorPermille >= 1000 && !ti.Failed && ti.CapacityRecord is null;

            if (d > outA.EndDay)
            {
                t.ResolvedDay = d;
                if (inRunning)
                {
                    t.Outcome = TransitionOutcome.Clean;
                    t.ResolvedRecordId = AddRecord(d, RecordKind.Transition, outA.Id,
                        $"{Upper(NameOf(t.Id))} complete: {NameOf(outA.Id)} ended day {outA.EndDay} with {NameOf(t.IncomingId)} delivering; {Plural(t.BothRunningDays, "day")} of overlap.", [t.CauseId]);
                }
                else
                {
                    t.Outcome = TransitionOutcome.Gap;
                    var (min, _, lost) = IncomingStart(t.IncomingId);
                    var why = lost ?? (min is { } m && m > d ? $"{Upper(NameOf(t.IncomingId))} is not expected before day {m}" : $"{Upper(NameOf(t.IncomingId))} is not running");
                    var causes = new List<string> { t.CauseId };
                    if (t.WarningRecordId is { } wr) causes.Add(wr);
                    t.ResolvedRecordId = AddRecord(d, RecordKind.Transition, outA.Id,
                        $"{Upper(NameOf(t.Id))} left a gap: {NameOf(outA.Id)} ended day {outA.EndDay} and {why}. {CapLabel(t.CapabilityId)} at {SiteName(t.SiteId)} has nothing behind it. Gap owner: {OwnerName(t.Owner)}.", causes);
                }
                continue;
            }

            if (inRunning && today.TryGetValue(outA.Id, out var to) && !to.Suspended) t.BothRunningDays++;
            t.FullDays = inFull ? t.FullDays + 1 : 0;

            if (t.CutoverStartedDay is null)
            {
                var due = t.Handoff == HandoffOnDay ? d >= t.CutoverDay!.Value - 1 && outA.EndDay > t.CutoverDay.Value
                                                    : t.FullDays >= t.OverlapDays && outA.EndDay > d + 1;
                if (due)
                {
                    t.CutoverStartedDay = d;
                    var why = t.Handoff == HandoffOnDay
                        ? $"cut-over day {t.CutoverDay}"
                        : $"{Upper(NameOf(t.IncomingId))} delivered at full for {Plural(t.FullDays, "day")}";
                    var rec = AddRecord(d, RecordKind.Transition, outA.Id,
                        $"{Upper(NameOf(t.Id))}: {why}; cut-over started, termination of {NameOf(outA.Id)} in work so its last day is day {d + 1} if the shop gets to it.", [t.CauseId]);
                    if (!_work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (w.Payload as string) == outA.Id))
                        AddWork(WorkKind.Terminate, $"Terminate {NameOf(outA.Id)}", outA.IsCoordination ? 0 : Hours(Rules.Model.Work.TerminationShopDays, true), d, outA.Id, rec);
                }
            }

            if (t.WarningRecordId is null && PlannedOutgoingEnd(t) - d <= Rules.Transition.WarnDays)
            {
                var (min, _, lost) = IncomingStart(t.IncomingId);
                var end = PlannedOutgoingEnd(t);
                if (lost is not null || (min is { } m && m > end && !inRunning))
                {
                    t.WarningDay = d;
                    var gapText = lost is not null
                        ? $"{Upper(NameOf(t.IncomingId))} will not arrive ({lost})"
                        : $"{Upper(NameOf(t.IncomingId))} is not expected before day {min}, a gap of at least {Plural(min - end - 1, "day")}";
                    t.WarningRecordId = AddRecord(d, RecordKind.Transition, outA.Id,
                        $"Transition planner: {NameOf(outA.Id)} ends day {end} and {gapText}. {CapLabel(t.CapabilityId)} at {SiteName(t.SiteId)} goes uncovered unless something bridges it. Gap owner: {OwnerName(t.Owner)}.", [t.CauseId]);
                }
            }
        }
    }

    internal IReadOnlyList<TransitionView> BuildTransitions()
        => _transitions.Select(BuildTransition).ToList();

    private TransitionView BuildTransition(TransitionState t)
    {
        var outA = _arrangements.First(a => a.Id == t.OutgoingId);
        var (min, max, lost) = IncomingStart(t.IncomingId);
        var inA = _arrangements.FirstOrDefault(a => a.Id == t.IncomingId);
        var status = t.Outcome switch
        {
            TransitionOutcome.Clean => "clean",
            TransitionOutcome.Gap => "gap",
            _ when t.CutoverStartedDay is not null => "cuttingOver",
            _ when inA is { Status: ArrangementStatus.Active or ArrangementStatus.Suspended } => "overlapping",
            _ => "planned",
        };
        var end = PlannedOutgoingEnd(t);
        int? projectedGap = lost is not null ? null : min is { } m && m > end ? m - end - 1 : 0;
        return new TransitionView(
            t.Id, t.SiteId, t.CapabilityId, t.OutgoingId, t.IncomingId, t.Handoff, t.CutoverDay, t.Owner, t.OverlapDays,
            outA.StartDay, outA.EndDay, end, inA?.StartDay, min, max, lost is not null, projectedGap,
            t.FullDays, t.BothRunningDays, status, t.PlannedDay, t.CutoverStartedDay, t.ResolvedDay, TransitionWarnings(t));
    }

    internal TransitionPreview BuildTransitionPreview(PlanTransitionAction p)
    {
        var chk = CheckTransition(p);
        if (chk.Stops.Count > 0) return new TransitionPreview(false, chk.Stops, [], null);
        var probe = new TransitionState
        {
            Id = p.Id, OutgoingId = chk.Outgoing!.Id, IncomingId = p.Incoming, SiteId = chk.SiteId!, CapabilityId = chk.CapabilityId!,
            OverlapDays = chk.OverlapDays, Handoff = chk.Handoff, CutoverDay = chk.Handoff == HandoffOnDay ? p.CutoverDay : null,
            Owner = chk.Owner, PlannedDay = Day,
        };
        var view = BuildTransition(probe);
        return new TransitionPreview(true, [], view.Warnings, view);
    }

    /// <summary>Debrief: a gap a transition plan left (M3-05). Avoidable when the planner flagged it before the outgoing's
    /// last day; otherwise the handoff failed without warning (the incoming slipped or was lost at the last moment).</summary>
    private IEnumerable<(string Key, GapFactor F)> TransitionFactors(string siteId, string capId, int d)
    {
        foreach (var t in _transitions.Where(x => x.Outcome == TransitionOutcome.Gap && x.SiteId == siteId && x.CapabilityId == capId))
        {
            var outA = _arrangements.First(a => a.Id == t.OutgoingId);
            if (d <= outA.EndDay) continue;
            var inA = _arrangements.FirstOrDefault(a => a.Id == t.IncomingId);
            if (inA is not null && ActiveOn(inA, d)) continue;
            var later = inA is not null && inA.Status != ArrangementStatus.Declined && inA.StartDay > d ? $" {NameOf(inA.Id)} started day {inA.StartDay}." : "";
            var ids = new List<string> { t.CauseId };
            if (t.WarningRecordId is { } w) ids.Add(w);
            if (t.ResolvedRecordId is { } r) ids.Add(r);
            yield return ($"trans|{t.Id}", t.WarningDay is { } wd && wd < outA.EndDay
                ? new GapFactor(GapFactorKind.TransitionGap, GapTag.Avoidable, outA.EndDay + 1,
                    $"The hand-off from {NameOf(outA.Id)} to {NameOf(t.IncomingId)} left a gap: {NameOf(outA.Id)} ended day {outA.EndDay} before {NameOf(t.IncomingId)} was delivering.{later} The transition planner flagged it on day {wd}; a bridge or an earlier start would have covered it. Gap owner: {OwnerName(t.Owner)}.", ids)
                : new GapFactor(GapFactorKind.TransitionGap, GapTag.Unforeseeable, outA.EndDay + 1,
                    $"The hand-off from {NameOf(outA.Id)} to {NameOf(t.IncomingId)} left a gap: {NameOf(outA.Id)} ended day {outA.EndDay} before {NameOf(t.IncomingId)} was delivering.{later} The planner showed no gap before the outgoing's last day.", ids));
        }
    }
}
