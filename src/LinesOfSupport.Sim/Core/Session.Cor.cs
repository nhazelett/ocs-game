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
        return $"{c.Quality}: reports {(q.LagDays == 0 ? "the same day" : $"{Plural(q.LagDays, "day")} late")}, about {q.ReportPermille / 10}% of problems";
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
        {
            // DESK-07: with fit modeled, the reason is the COR's fit and load, not the quality word.
            if (c.Knows is not null && CapabilityOf(a) is { } capId)
                return CorReportPermille(c, capId, null) >= 1000 && CorQuality(c).LagDays == 0 ? null
                    : $"{CorWhy(c, capId, null, naming: true)} Some vendor problems will reach you late or not at all.";
            return CorQuality(c) is { LagDays: 0, ReportPermille: >= 1000 }
                ? null
                : $"COR {c.Name} is {CorSummary(c)}: some vendor problems will reach you late or not at all.";
        }
        return $"No COR named: nobody checks the vendor's deliveries for the government. Problems surface as mission effects, reported by the unit {Plural(Rules.Cor.NoCorLagDays, "day")} later.";
    }

    private ActionResult ApplyDesignateCor(DesignateCorAction c)
    {
        if (!CorModeled) return ActionResult.Stop("this scenario has no COR roster");
        var a = _arrangements.FirstOrDefault(x => x.Id == c.Arrangement);
        if (a is null) return ActionResult.Stop($"arrangement {NameOf(c.Arrangement)} does not exist");
        var cor = CorOf(c.Cor);
        if (cor is null) return ActionResult.Stop($"COR {c.Cor} is not on the roster");
        if (!Curable(a)) return ActionResult.Stop($"{NameOf(a.Id)} is not a contract with written terms administered by this office");
        if (a.Status is not (ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended))
            return ActionResult.Stop($"{NameOf(a.Id)} is not running");
        if (a.CorId == cor.Id) return ActionResult.Stop($"{cor.Name} is already the COR on {NameOf(a.Id)}");
        var was = CorOf(a.CorId);
        a.CorId = cor.Id;
        // DESK-197 (Claude play 21 Nov, eleventh play): the rate on this contract, as the picker and Payments say it; the
        // base rate read "about 100% of problems" for a COR every screen gave 40% on waste removal.
        AddRecord(Day, RecordKind.Action, a.Id,
            $"{cor.Name} designated COR on {NameOf(a.Id)}{(was is null ? "" : $", replacing {was.Name}")} ({CorSummary(cor, a)}).", []);
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
                                  .Select(a => ArrName(a)).ToList();
        AddRecord(day, RecordKind.CorReport, c.Id,
            $"{was} rotates home. An untrained relief takes over as COR{(covers.Count > 0 ? $" on {string.Join(", ", covers)}" : "")} ({CorSummary(c)}). Designate another COR to restore reporting.",
            [cause]);
    }

    /// <summary>Failure records of missed consumable deliveries whose day the site's stock still met in full (DESK-87).</summary>
    private readonly HashSet<string> _stockCovered = [];

    /// <summary>
    /// DESK-87: a missed consumable delivery on a day the site's stock still met demand says so, in the record the day's
    /// results, the COR report and the debrief all read. The tutorial's dining hall slips on days 11 and 12 over three days
    /// of food stock; without this the COR report said "missed" and the debrief's grid said 100%.
    /// </summary>
    private void StockCoveredFailures(Dictionary<string, Today> today)
    {
        foreach (var (id, t) in today)
        {
            if (!t.Failed || t.FailureRecord is not { } recId) continue;
            var a = _arrangements.First(x => x.Id == id);
            if (Cap(a.CapabilityId).Delivery != "consumable" || CapState(a.SiteId, a.CapabilityId) is not { TodayPermille: >= 1000 }) continue;
            var i = _records.FindIndex(r => r.Id == recId);
            _records[i] = _records[i] with { Text = _records[i].Text + " The stock on hand covered today's demand." };
            _stockCovered.Add(recId);
        }
    }

    /// <summary>DESK-184: failure records whose day another arrangement covered, and who covered it ("the purchase order
    /// from Main Island Sanitation").</summary>
    private readonly Dictionary<string, string> _coveredBy = new(StringComparer.Ordinal);

    /// <summary>
    /// DESK-184 (D50; Claude play 21 Nov, ninth play, notes P1-29, P1-35). A vendor's equipment down on a day another
    /// arrangement kept the site fully supplied says so in the failure's own record, which the morning board, the COR
    /// report and the debrief's record all read: "The SF 44 from Main Island Sanitation had equipment down on waste removal
    /// at Hub; the purchase order from Main Island Sanitation covered the day, so nothing was short." The ninth play's
    /// player saw "had equipment down" beside "met so far" and 0 alerts and could not tell a false alarm from a bridge that
    /// worked. Continuous capabilities only: a missed consumable delivery on a day the site was not short already says the
    /// stock covered it (DESK-87). A failure nothing covered is unchanged. A sentence, not a measure: the score is unchanged.
    /// </summary>
    private void OtherCoveredFailures(Dictionary<string, Today> today)
    {
        foreach (var (id, t) in today)
        {
            if (!t.Failed || t.FailureRecord is not { } recId) continue;
            var a = _arrangements.First(x => x.Id == id);
            var cap = Cap(a.CapabilityId);
            if (cap.Delivery == "consumable" || CapState(a.SiteId, a.CapabilityId) is not { TodayPermille: >= 1000 }) continue;
            var covers = _arrangements.Where(x => x.Id != id && x.SiteId == a.SiteId && today.TryGetValue(x.Id, out var xt)
                                                  && !xt.Failed && !xt.Suspended && xt.FactorPermille > 0 && xt.NoDeliveryRecord is null
                                                  && ((x.CapabilityId == a.CapabilityId && x.QuantityMilli > 0
                                                       && x.Lines.Any(l => cap.Lines.First(c => c.Id == l).Kind == "core"))
                                                      || x.SuppliedQuantityMilli.GetValueOrDefault(a.CapabilityId) > 0))
                                      .Select(x => Called(x) is var c && c == Called(a) && c.StartsWith("the ", StringComparison.Ordinal) ? "the other " + c[4..] : c)
                                      .Distinct(StringComparer.Ordinal).ToList();
            if (covers.Count == 0) continue;
            var cover = JoinAnd(covers);
            var where = $"{Lower(cap.Label)} at {SiteName(a.SiteId)}";
            var failed = t.FailureStopped
                ? $"{Upper(Called(a))} for {where} stopped: nobody was there to run it{t.FailureWhy}"
                : $"{Upper(Called(a))} had equipment down on {where}{t.FailureWhy}";
            var i = _records.FindIndex(r => r.Id == recId);
            _records[i] = _records[i] with { Text = $"{failed}; {cover} covered the day, so nothing was short." };
            _coveredBy[recId] = cover;
        }
    }

    /// <summary>DESK-184: an arrangement by what it is and who supplies it, so two from one vendor read apart: "the SF 44
    /// from Main Island Sanitation", "the purchase order from Main Island Sanitation", "Main Island Utilities (host nation)".
    /// A cover called the same as the failure is "the other purchase order from North Island Telecom".</summary>
    private string Called(Arrangement a)
    {
        if (a.Instrument is HostNationInstrument or AcsaInstrument || a.IsCoordination) return SupplierName(a);
        var what = a.Instrument switch { "sf44" => "SF 44", "gpc" => "card buy", _ => InstrumentWords(a.Instrument) };
        return $"the {what} from {SupplierName(a)}";
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
                // An authored slip (DESK-80) always reaches the office; the draw is still made.
                var caught = _rngCor.Roll(CorReportPermille(cor, a.CapabilityId, a.Id));
                if (caught || SlipsOn(a.Id, d)) _corReports.Add(new CorReport(a.Id, d, d + q.LagDays, cor.Id, t.FailureRecord));
            }
            else if (CapState(a.SiteId, a.CapabilityId) is { TodayPermille: < 1000 })
                _corReports.Add(new CorReport(a.Id, d, d + Rules.Cor.NoCorLagDays, null, t.FailureRecord));
        }

        var arrived = new List<CorReport>();
        foreach (var r in _corReports.Where(r => r.ArriveDay == d && r.RecordId is null))
        {
            var a = _arrangements.First(x => x.Id == r.ArrangementId);
            var what = Cap(a.CapabilityId).Delivery == "consumable" ? "missed its delivery" : "had equipment down";
            var late = (d > r.FailureDay ? $", reported {Plural(d - r.FailureDay, "day")} later" : "")
                       + (_stockCovered.Contains(r.FailureRecordId) ? "; the stock on hand covered it" : "")   // DESK-87
                       + (_coveredBy.TryGetValue(r.FailureRecordId, out var cover) ? $"; {cover} covered the day, so nothing was short" : "");   // DESK-184
            r.RecordId = CorOf(r.CorId) is { } cor
                ? AddRecord(d, RecordKind.CorReport, a.Id, $"COR report ({cor.Name}): {NameOf(a.Id)} {what} on day {r.FailureDay}{late}.", [r.FailureRecordId])
                : AddRecord(d, RecordKind.CorReport, a.Id,
                    $"Unit report from {SiteName(a.SiteId)}: {CapLabel(a.CapabilityId).ToLowerInvariant()} fell short on day {r.FailureDay} and the unit blames {VendorData(a.VendorId)?.Name ?? "the vendor"}{late}. That contract has no COR, so nobody was checking its deliveries for the government.",
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
    /// <param name="unforeseen">The gap's verdict is that nobody could have seen the failure coming (DESK-155 a).</param>
    internal string CorNote(Arrangement a, int day, bool unforeseen = false)
    {
        if (Observed(a)) return "";
        var r = _corReports.FirstOrDefault(x => x.ArrangementId == a.Id && x.FailureDay == day);
        if (r?.RecordId is not null)
            return r.CorId is null
                ? $" With no COR, the unit reported it on day {r.ArriveDay}, after the mission effect."
                : r.ArriveDay > day ? $" The COR reported it on day {r.ArriveDay}." : "";
        if (r is not null) return $" Its report was still on the way (due day {r.ArriveDay}) when the game ended.{SoonerCor(r, unforeseen)}";
        return a.CorId is null
            ? " No COR was designated, so nobody reported it to the office."
            : $" Its COR ({CorOf(a.CorId)!.Quality}) did not report it.";
    }

    /// <summary>DESK-149 (b), Claude play 21 Nov, second play, note 9: a report still on the way at the end was late because
    /// of the COR's lag, and the debrief said only "due day 17", so the monitor the player chose made no visible
    /// difference. DESK-155 (a), third play, note 29: said as why it happened, the lag read against "Unforeseeable", so it
    /// says when the office would have heard instead: " TSgt Pratt reports problems 2 days late; with SSgt Lane, who
    /// reports the same day, its report would have been due on day 15." A report, not a promise: the other COR's odds of
    /// catching it are not certain. On an unforeseeable gap it opens with "The failure itself could not be foreseen."
    /// Empty with no COR, no lag, or nobody sooner.</summary>
    private string SoonerCor(CorReport r, bool unforeseen)
    {
        if (CorOf(r.CorId) is not { } cor || CorQuality(cor).LagDays is var lag && lag <= 0) return "";
        var sooner = _cors.Where(c => c.Id != cor.Id && c.RotatedDay is null && CorQuality(c).LagDays < lag)
                          .OrderBy(c => CorQuality(c).LagDays).FirstOrDefault();
        if (sooner is null) return "";
        var soon = CorQuality(sooner).LagDays;
        var lead = unforeseen ? " The failure itself could not be foreseen." : "";
        // DESK-159 (Claude play 21 Nov, fourth play, note 21): what hearing sooner would have changed, so the clause does
        // not read as the reason for the verdict: nothing for the day itself; something only if it went on failing after.
        var heard = r.FailureDay + soon;
        var after = _records.Any(x => x.Kind == RecordKind.VendorFailure && x.Subject == r.ArrangementId && x.Day > heard)
            ? "you would have heard while it was still failing"
            : "hearing sooner would not have changed that day";
        return $"{lead} {cor.Name} reports problems {Plural(lag, "day")} late; with {sooner.Name}, who reports {(soon == 0 ? "the same day" : $"{Plural(soon, "day")} late")}, its report would have been due on day {heard}, but {after}.";
    }
}
