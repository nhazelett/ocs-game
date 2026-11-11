namespace LinesOfSupport.Sim.Core;

// Commander priority when shop hours are short (M2-14, SPEC 4.5 "refuse and stop it"). A refused handshake costs
// that site's commander's confidence in contracting for the rest of the game. On a day the shop cannot finish all
// ready work, that site's work waits behind every other site's; among such sites the scenario's
// actors.commander.priorityOrder decides. Work tied to no site (surveys, BPA setup) is never held back. Ordering
// otherwise stays the player's queue (creation order). Rules and simplifications: docs/assumptions.md.
public sealed partial class Session
{
    /// <summary>Site id to the record id of the refusal that cost the commander's confidence.</summary>
    private readonly Dictionary<string, string> _lostConfidence = [];
    private readonly HashSet<string> _deferredWork = [];
    private readonly List<PriorityDeferral> _deferrals = [];

    /// <summary>One work item that first got fewer shop hours than the plain queue would have given it.</summary>
    private sealed record PriorityDeferral(string WorkId, string? ArrangementId, string Label, string SiteId, IReadOnlyList<string> Capabilities, int Day, string RecordId, string RefusalRecordId);

    /// <summary>Sites whose commander has lost confidence in contracting, in the order it happened.</summary>
    public IReadOnlyCollection<string> LostConfidenceSites => _lostConfidence.Keys;

    private void LoseConfidence(string siteId, string refusalRecordId)
        => _lostConfidence.TryAdd(siteId, refusalRecordId);

    /// <summary>0 for work that is not held back; otherwise 1 + the site's place in the commander's priority order.</summary>
    private int ConfidenceRank(WorkItem w)
    {
        var site = SiteOf(w);
        if (site is null || !_lostConfidence.ContainsKey(site)) return 0;
        var order = Scenario.Actors.Commander.PriorityOrder;
        var i = order.IndexOf(site);
        return 1 + (i < 0 ? order.Count : i);
    }

    private string? SiteOf(WorkItem w) => w.Kind switch
    {
        WorkKind.Award => ((AwardPayload)w.Payload!).SiteId,
        WorkKind.Definitize => ArrangementSite(((DefinitizeAction)w.Payload!).Arrangement),
        WorkKind.Terminate or WorkKind.PaymentRework or WorkKind.ClaimSettlement => ArrangementSite((string)w.Payload!),
        WorkKind.Ratification or WorkKind.Denial => CommitmentSite((string)w.Payload!),
        WorkKind.HhqResponse => ArrangementSite((string)w.Payload!) ?? CommitmentSite((string)w.Payload!),
        _ => null,
    };

    /// <summary>The capability an award is for, plus any its supply lines would bring (power's fuel line).</summary>
    private List<string> AwardCapabilities(AwardPayload p)
        => [p.CapabilityId, .. Cap(p.CapabilityId).Lines.Where(l => l.Kind == "supplies" && l.Supplies is not null && p.Lines.Contains(l.Id)).Select(l => l.Supplies!)];

    private string? ArrangementSite(string id) => _arrangements.FirstOrDefault(a => a.Id == id)?.SiteId;
    private string? CommitmentSite(string id) => _commitments.FirstOrDefault(c => c.Id == id)?.SiteId;

    /// <summary>Records each held-back item the first day it gets fewer hours than the plain queue would have given it.</summary>
    private void RecordDeferrals(int d, List<WorkItem> readyInQueueOrder, Dictionary<WorkItem, long> spent)
    {
        // The plain queue, per person (DESK-02): what each item would have had from its own person's hours.
        var left = _team.ToDictionary(m => m.Id, m => m.HoursPerDayMilli, StringComparer.Ordinal);
        foreach (var w in readyInQueueOrder)
        {
            var who = Member(w.Assignee).Id;
            var plain = Math.Min(Math.Max(0, left[who]), w.HoursRemainingMilli + spent.GetValueOrDefault(w));
            left[who] -= plain;
            if (spent.GetValueOrDefault(w) >= plain || _deferredWork.Contains(w.Id)) continue;
            var site = SiteOf(w)!;
            var refusal = _lostConfidence[site];
            var refused = _records.First(r => r.Id == refusal).Subject;
            var rec = AddRecord(d, RecordKind.Compliance, w.Id,
                $"Shop hours short: {w.Label} for {SiteName(site)} waits behind other work. That site's commander lost confidence in contracting when {refused} was refused.",
                [refusal, w.CauseId]);
            _deferredWork.Add(w.Id);
            _deferrals.Add(new PriorityDeferral(w.Id, (w.Payload as AwardPayload)?.Action.Id, w.Label, site, w.Payload is AwardPayload p ? AwardCapabilities(p) : [], d, rec, refusal));
        }
    }
}
