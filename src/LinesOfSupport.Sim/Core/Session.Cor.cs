using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// COR and report fidelity (M3-04, SPEC 4 "performance reports arrive with lag and fidelity set by COR quality; with
// no COR, problems surface as mission effects instead of reports"). DFARS 201.602-2: the contracting officer
// designates a COR, nominated by the requiring activity, to monitor a contract's performance.
//
// Modeled only when the scenario names a COR roster. Then a failure on a contract this office awarded with written
// terms is a fact the office does not see on the day: the COR reports it after the quality's lag, and only with the
// quality's odds (a "cor" stream draw per failure, none at 100%). With no COR the unit reports only a failure that
// showed as a mission effect (the site's capability below full that day), and later. Reported failure days, not real
// ones, open a performance issue. Without a roster every failure is seen the day it happens, as before.
public sealed partial class Session
{
    private readonly List<CorState> _cors = [];
    private readonly List<CorReport> _corReports = [];

    /// <summary>The COR roster. Empty when the scenario does not model COR reporting.</summary>
    public IReadOnlyList<CorState> Cors => _cors;

    /// <summary>Failure reports scheduled or delivered so far (RecordId set once delivered).</summary>
    public IReadOnlyList<CorReport> CorReports => _corReports;

    /// <summary>True when the scenario has a COR roster, so failures on awarded contracts are seen only through reports.</summary>
    public bool CorModeled => _cors.Count > 0;

    /// <summary>True when the office sees this arrangement's failures the day they happen: no roster, or not a
    /// contract with written terms awarded by this office (cash and card buys, handshakes, coordinations, inherited
    /// host-nation support are watched by whoever uses them).</summary>
    internal bool Observed(Arrangement a) => !CorModeled || !Curable(a);

    internal CorState? CorOf(string? id) => id is null ? null : _cors.FirstOrDefault(c => c.Id == id);

    internal CorQualityData CorQuality(CorState c) => Rules.Cor.Qualities[c.Quality];

    /// <summary>"fair: reports 2 days late, about 80% of problems".</summary>
    internal string CorSummary(CorState c)
    {
        var q = CorQuality(c);
        return $"{c.Quality}: reports {(q.LagDays == 0 ? "the same day" : $"{q.LagDays} day(s) late")}, about {q.ReportPermille / 10}% of problems";
    }

    internal IEnumerable<string> CorStops(string corId, InstrumentData inst)
    {
        if (!CorModeled) { yield return "this scenario has no COR roster"; yield break; }
        if (CorOf(corId) is null) yield return $"COR {corId} is not on the roster";
        if (!inst.WrittenTerms) yield return $"a COR is designated only on a contract with written terms, not a {inst.Label}";
    }

    /// <summary>The soft gap the action editor shows for the COR choice on an award with written terms.</summary>
    internal string? CorGap(AwardAction a, InstrumentData inst)
    {
        if (!CorModeled || !inst.WrittenTerms) return null;
        if (CorOf(a.Cor) is { } c)
            return CorQuality(c) is { LagDays: 0, ReportPermille: >= 1000 }
                ? null
                : $"COR {c.Name} is {CorSummary(c)}: some vendor problems will reach you late or not at all.";
        return $"No COR named: nobody checks the vendor's deliveries for the government. Problems surface as mission effects, reported by the unit {Rules.Cor.NoCorLagDays} day(s) later.";
    }

    private ActionResult ApplyDesignateCor(DesignateCorAction c)
    {
        if (!CorModeled) return ActionResult.Stop("this scenario has no COR roster");
        var a = _arrangements.FirstOrDefault(x => x.Id == c.Arrangement);
        if (a is null) return ActionResult.Stop($"arrangement {c.Arrangement} does not exist");
        var cor = CorOf(c.Cor);
        if (cor is null) return ActionResult.Stop($"COR {c.Cor} is not on the roster");
        if (!Curable(a)) return ActionResult.Stop($"{a.Id} is not a contract with written terms awarded by this office");
        if (a.Status is not (ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended))
            return ActionResult.Stop($"{a.Id} is not running");
        if (a.CorId == cor.Id) return ActionResult.Stop($"{cor.Name} is already the COR on {a.Id}");
        var was = CorOf(a.CorId);
        a.CorId = cor.Id;
        AddRecord(Day, RecordKind.Action, a.Id,
            $"{cor.Name} designated COR on {a.Id}{(was is null ? "" : $", replacing {was.Name}")} ({CorSummary(cor)}).", []);
        return ActionResult.Ok("designated");
    }

    /// <summary>Event effect <c>cor.rotates</c>: the COR goes home; an untrained relief keeps the designations (M3-04).</summary>
    internal void RotateCor(string corId, int day, string cause)
    {
        var c = CorOf(corId) ?? throw new InvalidDataException($"cor.rotates: COR {corId} not found");
        if (c.RotatedDay is not null) return;
        var was = c.Name;
        c.RotatedDay = day;
        c.Quality = Rules.Cor.ReliefQuality;
        c.Name = $"{was}'s relief";
        var covers = _arrangements.Where(a => a.CorId == c.Id && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended)
                                  .Select(a => a.Id).ToList();
        AddRecord(day, RecordKind.CorReport, c.Id,
            $"{was} rotates home. An untrained relief takes over as COR{(covers.Count > 0 ? $" on {string.Join(", ", covers)}" : "")} ({CorSummary(c)}). Designate another COR to restore reporting.",
            [cause]);
    }

    /// <summary>After supply resolves: schedule reports of today's failures, deliver the ones due, open issues on them.</summary>
    private void CorReporting(int d, Dictionary<string, Today> today)
    {
        if (!CorModeled) return;
        foreach (var (id, t) in today)
        {
            if (!t.Failed || t.FailureRecord is null) continue;
            var a = _arrangements.First(x => x.Id == id);
            if (Observed(a)) continue;
            if (CorOf(a.CorId) is { } cor)
            {
                var q = CorQuality(cor);
                if (_rngCor.Roll(q.ReportPermille)) _corReports.Add(new CorReport(a.Id, d, d + q.LagDays, cor.Id, t.FailureRecord));
            }
            else if (CapState(a.SiteId, a.CapabilityId) is { TodayPermille: < 1000 })
                _corReports.Add(new CorReport(a.Id, d, d + Rules.Cor.NoCorLagDays, null, t.FailureRecord));
        }

        var arrived = new List<CorReport>();
        foreach (var r in _corReports.Where(r => r.ArriveDay == d && r.RecordId is null))
        {
            var a = _arrangements.First(x => x.Id == r.ArrangementId);
            var what = Cap(a.CapabilityId).Delivery == "consumable" ? "missed its delivery" : "had equipment down";
            var late = d > r.FailureDay ? $", reported {d - r.FailureDay} day(s) later" : "";
            r.RecordId = CorOf(r.CorId) is { } cor
                ? AddRecord(d, RecordKind.CorReport, a.Id, $"COR report ({cor.Name}): {a.Id} {what} on day {r.FailureDay}{late}.", [r.FailureRecordId])
                : AddRecord(d, RecordKind.CorReport, a.Id,
                    $"Unit report from {SiteName(a.SiteId)}: {CapLabel(a.CapabilityId).ToLowerInvariant()} fell short on day {r.FailureDay} and the unit blames {VendorData(a.VendorId)?.Name ?? "the vendor"} ({a.Id}){late}. {a.Id} has no COR, so nobody was checking its deliveries for the government.",
                    [r.FailureRecordId]);
            arrived.Add(r);
        }

        foreach (var g in arrived.GroupBy(r => r.ArrangementId))
        {
            var a = _arrangements.First(x => x.Id == g.Key);
            if (a.Status is not (ArrangementStatus.Active or ArrangementStatus.Suspended) || IssueBlocks(a, d)) continue;
            var latest = g.Max(r => r.FailureDay);
            var reports = _corReports.Where(r => r.ArrangementId == a.Id && r.RecordId is not null
                                                 && r.FailureDay > latest - Perf.WindowDays && r.FailureDay <= latest)
                                     .OrderBy(r => r.FailureDay).ToList();
            var days = reports.Select(r => r.FailureDay).Distinct().ToList();
            if (days.Count < Perf.UnderDeliveryDays) continue;
            var by = g.Last().CorId is { } cid ? $" (COR {CorOf(cid)!.Name})" : " (unit reports; no COR)";
            OpenIssue(d, a, days, [.. reports.Select(r => r.RecordId!), .. reports.Select(r => r.FailureRecordId)], by);
        }
    }

    /// <summary>Debrief: how a failure on the day reached the office, or that it never did (M3-04).</summary>
    internal string CorNote(Arrangement a, int day)
    {
        if (Observed(a)) return "";
        var r = _corReports.FirstOrDefault(x => x.ArrangementId == a.Id && x.FailureDay == day);
        if (r?.RecordId is not null)
            return r.CorId is null
                ? $" With no COR, the unit reported it on day {r.ArriveDay}, after the mission effect."
                : r.ArriveDay > day ? $" The COR reported it on day {r.ArriveDay}." : "";
        if (r is not null) return $" Its report was still on the way (due day {r.ArriveDay}) when the game ended.";
        return a.CorId is null
            ? " No COR was designated, so nobody reported it to the office."
            : $" Its COR ({CorOf(a.CorId)!.Quality}) did not report it.";
    }
}
