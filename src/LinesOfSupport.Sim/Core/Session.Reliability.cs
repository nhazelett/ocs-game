namespace LinesOfSupport.Sim.Core;

// A vendor's performance falling as an authored event (M3-18, docs/design/M3-07-representative-scenario.md chain 3):
// `vendor.reliabilityDrop` lowers an inherited arrangement's reliability from the event day, usually behind the
// preconditions `arrangement.noCureNotice` and `arrangement.notReplaced`, so a player who acted on the early slips
// never sees it. The drop is not news by itself; the failures it brings are, through the COR and the M2-18 issue flow.
public sealed partial class Session
{
    private sealed record ReliabilityDropRecord(string ArrangementId, int Day, string EventRecordId, int FromPermille, int ToPermille);
    private readonly List<ReliabilityDropRecord> _reliabilityDrops = [];

    internal void ReliabilityDrop(string arrangementId, int toPermille, int day, string cause)
    {
        var a = _arrangements.FirstOrDefault(x => x.Id == arrangementId);
        if (a is null || a.Status is ArrangementStatus.Ended or ArrangementStatus.Declined) return;
        var from = a.ReliabilityPermille;
        if (toPermille >= from) return;
        a.ReliabilityPermille = toPermille;
        _reliabilityDrops.Add(new ReliabilityDropRecord(a.Id, day, cause, from, toPermille));
    }

    /// <summary>True when a cure notice on any performance issue of the arrangement is delivered or in work.</summary>
    internal bool CureNoticeIssued(string arrangementId)
        => _issues.Any(i => i.ArrangementId == arrangementId
                            && (i.CureNoticeDay is not null
                                || _work.Any(w => w.CompletedDay is null && w.Kind == WorkKind.CureNotice && (string?)w.Payload == i.Id)));

    /// <summary>True when something awarded or arranged this game for the arrangement's site and capability is delivering.</summary>
    internal bool ReplacementDelivering(string arrangementId)
        => _arrangements.FirstOrDefault(x => x.Id == arrangementId) is { } a
           && _arrangements.Any(x => x.Id != a.Id && x.CreatedDay > 0 && x.SiteId == a.SiteId && x.CapabilityId == a.CapabilityId
                                     && x.Status == ArrangementStatus.Active);

    /// <summary>Debrief factor for a failure after an authored drop, with full line coverage (M3-18). The slips before the
    /// drop opened a performance issue: acting on it (cure notice or replacement) was what the drop's preconditions
    /// waited for, so the failure was avoidable. With no issue before the drop, null (the caller tags it).</summary>
    private (string Text, IReadOnlyList<string> Ids)? DropFactor(Arrangement a, int d, string failureRecordId)
    {
        var drop = _reliabilityDrops.LastOrDefault(x => x.ArrangementId == a.Id && x.Day <= d);
        if (drop is null) return null;
        var issue = _issues.Where(i => i.ArrangementId == a.Id && i.OpenedDay < drop.Day).OrderBy(i => i.OpenedDay).FirstOrDefault();
        if (issue is null) return null;
        return ($"{Upper(NameOf(a.Id))} failed on day {d} after its vendor's performance fell on day {drop.Day} ({drop.FromPermille / 10}% to {drop.ToPermille / 10}% on time). Performance issue {NameOf(issue.Id)}, opened day {issue.OpenedDay}, had shown the slips; a cure notice or a replacement before day {drop.Day} would have held it.",
                [failureRecordId, drop.EventRecordId, issue.CauseId]);
    }

    /// <summary>A note for a failure after a drop the player had no warning of.</summary>
    private string DropNote(Arrangement a, int d)
        => _reliabilityDrops.LastOrDefault(x => x.ArrangementId == a.Id && x.Day <= d) is { } drop
            ? $" Its vendor's performance fell on day {drop.Day} ({drop.FromPermille / 10}% to {drop.ToPermille / 10}% on time) with no slips reported before."
            : "";
}
