using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// Event chain 1 (backlog M2-18): a contracted vendor under-delivers, the CCO decides. FAR 49.402-3: before
// terminating for default for failure to perform, the contracting officer gives a cure notice and a cure period
// (10 days). Here: cure notice (CureNoticeAction), re-source (an ordinary award while the vendor keeps running) or
// terminate (TerminateAction). Draws come from the "performance" stream only when a cure period ends, so games
// without a cure notice replay exactly as before.
public sealed partial class Session
{
    private readonly List<PerformanceIssueState> _issues = [];
    public IReadOnlyList<PerformanceIssueState> PerformanceIssues => _issues;
    private PerformanceRulesData Perf => Rules.Performance;

    /// <summary>Contracts with written terms, awarded by this office or inherited from the last rotation (M3-18: the
    /// office now administers them). Cash and card buys, handshakes, coordinations and host-nation support have no
    /// default clause to cure under; nor does a partner's ACSA support (M3-06).</summary>
    private bool Curable(Arrangement a)
        => !a.IsCoordination && !a.Unauthorized && a.Instrument != AcsaInstrument
           && Rules.Instruments.TryGetValue(a.Instrument, out var inst) && inst.WrittenTerms;

    private void PerformanceChecks(int d, Dictionary<string, Today> today)
    {
        foreach (var (id, t) in today)
        {
            if (!t.Failed) continue;
            var a = _arrangements.First(x => x.Id == id);
            if (!Curable(a) || !Observed(a)) continue; // reported failures open the issue instead (CorReporting, M3-04)
            if (IssueBlocks(a, d)) continue;
            var days = _records.Where(r => r.Kind == RecordKind.VendorFailure && r.Subject == a.Id && r.Day > d - Perf.WindowDays && r.Day <= d)
                               .Select(r => r.Day).Distinct().OrderBy(x => x).ToList();
            if (days.Count < Perf.UnderDeliveryDays) continue;
            var failures = _records.Where(r => r.Kind == RecordKind.VendorFailure && r.Subject == a.Id && days.Contains(r.Day)).Select(r => r.Id).ToList();
            OpenIssue(d, a, days, failures, "");
        }

        foreach (var i in _issues.Where(i => i.CureEndsDay == d && i.Outcome == CureOutcome.None))
        {
            var a = _arrangements.First(x => x.Id == i.ArrangementId);
            var vendor = VendorData(a.VendorId)?.Name ?? "The vendor";
            if (a.Status is not (ArrangementStatus.Active or ArrangementStatus.Pending or ArrangementStatus.Suspended))
            {
                i.Outcome = CureOutcome.Cured;
                AddRecord(d, RecordKind.Compliance, i.Id, $"Cure period on {NameOf(i.Id)} ended; {NameOf(a.Id)} was no longer running.", [i.CauseId]);
                continue;
            }
            var lateFailures = _records.Count(r => r.Kind == RecordKind.VendorFailure && r.Subject == a.Id && r.Day > i.CureNoticeDay && r.Day <= d);
            if (_rngPerformance.Roll(Perf.CureRecoverPermille))
            {
                i.Outcome = CureOutcome.Cured;
                a.ReliabilityPermille = Math.Max(a.ReliabilityPermille, Perf.CuredReliabilityPermille);
                AddRecord(d, RecordKind.Compliance, i.Id,
                    $"{vendor} cured {NameOf(i.Id)}: the cure period ended with a corrective plan in place ({lateFailures} failure day(s) during the period). Performance is back to standard.", [i.CauseId]);
            }
            else
            {
                i.Outcome = CureOutcome.Default;
                a.ReliabilityPermille = Math.Max(500, a.ReliabilityPermille - Perf.DefaultReliabilityDropPermille);
                AddRecord(d, RecordKind.Compliance, i.Id,
                    $"{vendor} did not cure {NameOf(i.Id)} and is in default. Performance keeps slipping; the contracting officer may terminate {NameOf(a.Id)} for default.", [i.CauseId]);
            }
        }
    }

    /// <summary>True when an issue on the arrangement is still open, or closed so recently that a new one would repeat it.</summary>
    private bool IssueBlocks(Arrangement a, int d)
        => _issues.LastOrDefault(i => i.ArrangementId == a.Id) is { } open
           && (open.Outcome == CureOutcome.None || (open.CureEndsDay is { } end && d <= end + Perf.CureDays));

    private void OpenIssue(int d, Arrangement a, List<int> days, List<string> causes, string seenBy)
    {
        var issueId = $"pi{_issues.Count + 1}";
        var vendor = VendorData(a.VendorId)?.Name ?? "The vendor";
        var rec = AddRecord(d, RecordKind.Inbox, issueId,
            $"Performance issue: {vendor} under-delivered on {Lower(CapLabel(a.CapabilityId))} at {SiteName(a.SiteId)} on days {string.Join(" and ", days)}{seenBy}. Decision: issue a cure notice ({Perf.CureNoticeShopHours} shop hours, then a {Perf.CureDays}-day cure period), re-source while it keeps running, or terminate.",
            [.. causes, a.CauseId]);
        _issues.Add(new PerformanceIssueState
        {
            Id = issueId, ArrangementId = a.Id, SiteId = a.SiteId, CapabilityId = a.CapabilityId, VendorId = a.VendorId,
            OpenedDay = d, FailureDays = days, CauseId = rec,
        });
    }

    private ActionResult ApplyCureNotice(CureNoticeAction c)
    {
        var i = _issues.FirstOrDefault(x => x.Id == c.Issue);
        if (i is null) return ActionResult.Stop($"{NameOf(c.Issue)} does not exist");
        if (i.CureNoticeDay is not null) return ActionResult.Stop($"a cure notice on {NameOf(i.Id)} was already issued on day {i.CureNoticeDay}");
        if (_work.Any(w => w.CompletedDay is null && w.Kind == WorkKind.CureNotice && (string?)w.Payload == i.Id))
            return ActionResult.Stop($"a cure notice on {NameOf(i.Id)} is already in work");
        var a = _arrangements.First(x => x.Id == i.ArrangementId);
        if (a.Status is not (ArrangementStatus.Active or ArrangementStatus.Suspended))
            return ActionResult.Stop($"{NameOf(a.Id)} is not running");
        var rec = AddRecord(Day, RecordKind.Action, i.Id, $"Cure notice on {NameOf(i.Id)} started: {Perf.CureNoticeShopHours} shop hours.", [i.CauseId]);
        AddWork(WorkKind.CureNotice, $"Cure notice {NameOf(i.Id)}", Fixed.Milli(Perf.CureNoticeShopHours), Day, i.Id, rec);
        return ActionResult.Ok("in work");
    }

    private void CompleteCureNotice(string issueId, int day, string causeId)
    {
        var i = _issues.First(x => x.Id == issueId);
        i.CureNoticeDay = day;
        i.CureEndsDay = day + Perf.CureDays;
        AddRecord(day, RecordKind.WorkDone, i.Id,
            $"Cure notice on {NameOf(i.Id)} delivered to {VendorData(i.VendorId)?.Name ?? "the vendor"}: {Perf.CureDays} days to cure, ending day {i.CureEndsDay}.", [causeId]);
    }

    /// <summary>What the player did about an issue, read back from the record: cure notice, terminate or re-source.</summary>
    internal string IssueDecision(PerformanceIssueState i)
    {
        var a = _arrangements.First(x => x.Id == i.ArrangementId);
        var parts = new List<string>();
        if (i.CureNoticeDay is { } cn)
            parts.Add($"cure notice day {cn}, {(i.Outcome switch { CureOutcome.Cured => "cured", CureOutcome.Default => "default", _ => "cure period running" })}");
        if (_records.Any(r => r.Kind == RecordKind.Action && r.Subject == a.Id && r.Day >= i.OpenedDay && r.Text.StartsWith("Termination", StringComparison.Ordinal))
            || _work.Any(w => w.Kind == WorkKind.Terminate && w.CreatedDay >= i.OpenedDay && (w.Payload as string) == a.Id))
            parts.Add("terminated");
        var replacement = _arrangements.FirstOrDefault(x => x.Id != a.Id && x.SiteId == i.SiteId && x.CapabilityId == i.CapabilityId
                                                             && x.CreatedDay >= i.OpenedDay && !x.IsCoordination && x.Status != ArrangementStatus.Declined);
        if (replacement is not null) parts.Add($"re-sourced ({NameOf(replacement.Id)})");
        return parts.Count == 0 ? "no decision" : string.Join("; ", parts);
    }
}
