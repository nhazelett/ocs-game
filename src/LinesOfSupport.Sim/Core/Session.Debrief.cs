using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Debrief builder (M2-08). Read-only: it diagnoses each gap from the state the game ended in (arrangements and
// their dates, survey records, scope lines, population, records by day). It changes nothing and draws no random
// number. The tag rules are written out in docs/assumptions.md ("Debrief tags").
public sealed partial class Session
{
    private int DaysPlayed => Math.Min(Day - 1, LastDay);

    private string IssueNote(string arrangementId)
        => _issues.LastOrDefault(i => i.ArrangementId == arrangementId) is { } i
            ? $" Performance issue {i.Id} opened day {i.OpenedDay}; decision: {IssueDecision(i)}."
            : "";

    /// <summary>Capability levels by day, index 0 = day 1; -1 where the site was not open.</summary>
    private int[] Band(CapabilityState cs)
    {
        var site = SiteData(cs.SiteId);
        var band = Enumerable.Repeat(-1, LastDay).ToArray();
        var i = 0;
        for (var d = 1; d <= DaysPlayed && i < cs.History.Count; d++)
            if (PopulationOn(site, d) > 0) band[d - 1] = cs.History[i++];
        return band;
    }

    /// <summary>Days after an unplanned arrival before a remaining shortfall counts as the player's (docs/assumptions.md).</summary>
    internal const int PopulationJumpReactDays = 7;

    private int? SurveyedDay(string islandId)
        => _records.FirstOrDefault(r => r.Kind == RecordKind.Knowledge && r.Subject == $"survey {islandId}")?.Day;

    private string TierOn(string islandId, int day)
    {
        var surveyed = SurveyedDay(islandId);
        if (surveyed is { } sd && sd <= day) return "surveyed";
        return Scenario.Islands.First(i => i.Id == islandId).InfoTier ?? "assumed";
    }

    private bool ActiveOn(Arrangement a, int day)
        => a.Status != ArrangementStatus.Declined && a.StartDay <= day && day <= a.EndDay && a.CreatedDay < day;

    private bool Supplies(Arrangement a, string capId)
        => (a.CapabilityId == capId && a.Lines.Any(l => Cap(capId).Lines.FirstOrDefault(x => x.Id == l)?.Kind == "core"))
           || a.SuppliedQuantityMilli.ContainsKey(capId);

    private string Line(string capId, string kind) => string.Join(" or ", Cap(capId).Lines.Where(l => l.Kind == kind).Select(l => (l.Label ?? l.Id).ToLowerInvariant()));

    private IEnumerable<(string Key, GapFactor F)> DiagnoseDay(string siteId, string capId, int d, Func<string, int, int?> gapIndexFor)
    {
        var site = SiteData(siteId);
        var cap = Cap(capId);

        foreach (var dep in cap.DependsOn)
            if (gapIndexFor(dep, d) is { } gi)
                yield return ($"dep|{gi}", new GapFactor(GapFactorKind.Dependency, GapTag.Unforeseeable, d,
                    $"{cap.Label} depends on {CapLabel(dep).ToLowerInvariant()}, which was short.", [], gi));

        // An award that would have supplied it was held back behind other sites' work and had not started (M2-14).
        foreach (var df in _deferrals.Where(x => x.SiteId == siteId && x.Capabilities.Contains(capId) && x.Day <= d))
        {
            var arr = _arrangements.FirstOrDefault(a => a.Id == df.ArrangementId);
            if (arr is not null && (arr.Status == ArrangementStatus.Declined || arr.StartDay <= d)) continue;
            var refused = _records.First(r => r.Id == df.RefusalRecordId).Subject;
            yield return ($"prio|{df.WorkId}", new GapFactor(GapFactorKind.PriorityDeferred, GapTag.Avoidable, d,
                $"{df.Label} waited behind other shop work from day {df.Day}: {site.Name}'s commander lost confidence in contracting when {refused} was refused, so its work went last when hours were short.",
                [df.RecordId, df.RefusalRecordId]));
        }

        var contributors = _arrangements.Where(a => a.SiteId == siteId && Supplies(a, capId) && ActiveOn(a, d)).ToList();
        if (contributors.Count == 0)
        {
            var any = false;
            // Only the most recent loss of supply counts: a coordination withdrawn on day 12 does not explain day 77
            // if a contract supplied the need in between.
            var lastContractEnd = _arrangements.Where(a => a.SiteId == siteId && Supplies(a, capId) && !a.IsCoordination && a.Status != ArrangementStatus.Declined && a.EndDay < d)
                                               .Select(a => a.EndDay).DefaultIfEmpty(0).Max();
            foreach (var co in _arrangements.Where(a => a.IsCoordination && a.SiteId == siteId && a.CapabilityId == capId && a.EndDay < d && a.EndDay >= lastContractEnd))
            {
                var w = _records.FirstOrDefault(r => r.Kind == RecordKind.Withdrawal && r.Subject == co.Id);
                if (w is null) continue;
                any = true;
                yield return ($"coord|{co.Id}", new GapFactor(GapFactorKind.CoordinationWithdrawn, GapTag.Avoidable, d,
                    $"Coordination {co.Id} was withdrawn on day {w.Day} and nothing contractual was in place behind it. A coordination can be withdrawn at any time; it is not a contract.", [w.Id, co.CauseId]));
            }
            foreach (var c in _commitments.Where(c => c.Kind == CommitmentKind.Handshake && c.SiteId == siteId && c.CapabilityId == capId))
            {
                var arr = _arrangements.First(a => a.Id == c.ArrangementId);
                if (arr.EndDay >= d || arr.StartDay > d) continue;
                if (c.WalkedOff)
                {
                    any = true;
                    var w = _records.FirstOrDefault(r => r.Kind == RecordKind.Withdrawal && r.Subject == c.Id);
                    yield return ($"hs|{c.Id}", new GapFactor(GapFactorKind.HandshakeLost, GapTag.Avoidable, d,
                        $"Handshake {c.Id} was left {(c.Decision == CommitmentDecision.Parked ? "parked" : "unresolved")} and the vendor walked off on day {arr.EndDay + 1}.", w is null ? [c.CauseId] : [w.Id, c.CauseId]));
                }
                else if (c.Decision == CommitmentDecision.Refused)
                {
                    any = true;
                    yield return ($"hs|{c.Id}", new GapFactor(GapFactorKind.HandshakeLost, GapTag.Avoidable, d,
                        $"Handshake {c.Id} was refused on day {c.DecidedDay} with nothing in its place.", [c.CauseId]));
                }
            }
            foreach (var decl in _arrangements.Where(a => a.Status == ArrangementStatus.Declined && a.SiteId == siteId && a.CapabilityId == capId && a.CreatedDay < d))
            {
                any = true;
                yield return ($"bpa|{decl.Id}", new GapFactor(GapFactorKind.BpaDeclined, GapTag.Unforeseeable, d,
                    $"BPA call {decl.Id} was declined on day {decl.CreatedDay}. A BPA is not binding; any call can be declined.", [decl.CauseId]));
            }
            // A capability that brings this one with it (power's fuel supply line) but was bought without that line.
            foreach (var other in _arrangements.Where(a => a.SiteId == siteId && ActiveOn(a, d) && !a.IsCoordination && !a.Unauthorized))
            {
                var oc = Cap(other.CapabilityId);
                var supplyLine = oc.Lines.FirstOrDefault(l => l.Kind == "supplies" && l.Supplies == capId);
                if (supplyLine is null || other.Lines.Contains(supplyLine.Id)) continue;
                any = true;
                var card = other.CardId is null ? null : _cards.FirstOrDefault(c => c.Id == other.CardId);
                var where = card is null ? "" : $" Requirement {card.Id}'s dependency checklist showed {cap.Label.ToLowerInvariant()} uncovered from day {card.SubmittedDay}.";
                yield return ($"line|{other.Id}", new GapFactor(GapFactorKind.MissingSupplyLine, GapTag.Avoidable, d,
                    $"{other.Id} ({oc.Label.ToLowerInvariant()}) was bought without its {(supplyLine.Label ?? supplyLine.Id).ToLowerInvariant()} line, and nothing else supplied {cap.Label.ToLowerInvariant()}.{where}", [other.CauseId]));
            }
            foreach (var card in _cards.Where(c => c.SiteId == siteId && c.CapabilityId == capId && c.SubmittedDay <= d && c.RequestedStartDay <= d))
            {
                any = true;
                var later = _arrangements.Where(a => a.SiteId == siteId && Supplies(a, capId) && a.Status != ArrangementStatus.Declined && a.StartDay > d)
                                         .OrderBy(a => a.StartDay).FirstOrDefault();
                var ended = _arrangements.Where(a => a.SiteId == siteId && Supplies(a, capId) && a.Status != ArrangementStatus.Declined && a.EndDay < d)
                                         .OrderByDescending(a => a.EndDay).FirstOrDefault();
                var tail = (ended is null ? "" : $" The last arrangement supplying it, {ended.Id}, ended on day {ended.EndDay}.")
                           + (later is null ? "" : $" {later.Id} did not start until day {later.StartDay}.{ExternalNote(later)}");
                yield return ($"card|{card.Id}|{ended?.Id}", new GapFactor(GapFactorKind.Uncovered, GapTag.Avoidable, d,
                    $"Requirement {card.Id} (submitted day {card.SubmittedDay}, needed from day {card.RequestedStartDay}) had nothing supplying it from day {d}.{tail}",
                    later is null ? (ended is null ? [] : [ended.CauseId]) : [later.CauseId]));
            }
            if (FundingLapseFactor(siteId, capId, d) is { } lapse)
            {
                any = true;
                yield return lapse;
            }
            if (!any)
                yield return ("uncovered", new GapFactor(GapFactorKind.Uncovered, GapTag.Avoidable, d,
                    $"Nothing supplied {cap.Label.ToLowerInvariant()} at {site.Name}; the site and its population were in the plan from day 1.", []));
            yield break;
        }

        // Something was supplying it. Off-island consumables ride the surface link schedule.
        if (cap.Delivery == "consumable")
            foreach (var a in contributors.Where(a => a.SupplierIslandId != site.Island))
            {
                var surveyed = SurveyedDay(site.Island);
                // Only closures of a link between the supplier's island and this site's island.
                var links = Scenario.TransportLinks.Where(l => (l.From == a.SupplierIslandId && l.To == site.Island) || (l.To == a.SupplierIslandId && l.From == site.Island))
                                                   .Select(l => l.Id).ToHashSet(StringComparer.Ordinal);
                var closures = _records.Where(r => r.Kind == RecordKind.Event && r.Day <= d && r.Day >= d - 3
                                                   && Scenario.Events.FirstOrDefault(e => e.Id == r.Subject)?.Effects.Any(x => x.Kind == "link.closed" && links.Contains(x.Target ?? "")) == true)
                                       .ToList();
                var weather = closures.Count == 0 ? "" : $" {string.Join(" ", closures.Select(c => $"On day {c.Day}: {c.Text.TrimEnd('.')}."))}";
                var forecasts = _records.Where(r => r.Kind == RecordKind.Knowledge && closures.Any(c => c.Subject == r.Subject) && r.Day < d).ToList();
                if (forecasts.Count > 0) weather += $" The forecast came on day {forecasts[0].Day}, a day ahead because the island had been surveyed.";
                var islandName = IslandData(site.Island).Name;
                var from = IslandData(a.SupplierIslandId).Name;
                var ids = new List<string> { a.CauseId };
                ids.AddRange(closures.Select(c => c.Id));
                yield return ($"island|{a.Id}|{(surveyed is { } s0 && s0 <= d)}", surveyed is { } sd && sd <= d
                    ? new GapFactor(GapFactorKind.OffIslandDependency, GapTag.Avoidable, d,
                        $"{a.Id} supplies {cap.Label.ToLowerInvariant()} from {from}, so deliveries to {islandName} wait on the surface link schedule. The survey on day {sd} showed that dependency.{weather}", ids)
                    : new GapFactor(GapFactorKind.OffIslandDependency, GapTag.Discoverable, d,
                        $"{a.Id} supplies {cap.Label.ToLowerInvariant()} from {from}, so deliveries to {islandName} wait on the surface link schedule. A survey of {islandName} would have shown that dependency; a buffer or an on-island source would have covered it.{weather}", ids));
            }

        var demand = DemandMilli(site, capId, d);
        var supplyCap = contributors.Sum(a => (a.CapabilityId == capId ? a.QuantityMilli : 0) + a.SuppliedQuantityMilli.GetValueOrDefault(capId));
        // Consumables compare the contracted daily rate with daily demand: a rate below demand drains the buffer
        // whatever the delivery schedule, so it is a sizing gap like an undersized generator.
        // An unplanned arrival (site.populationJump) the arrangements were sized for without: no path to know before
        // the notice, so unforeseeable at first; still short a week after the arrival is a sizing gap again (M2-19).
        var planned = DemandMilli(site, capId, d, planned: true);
        var jump = _popJumps.Where(j => j.SiteId == siteId && j.ArrivalDay <= d).OrderByDescending(j => j.ArrivalDay).FirstOrDefault();
        if (jump is not null && supplyCap < demand && supplyCap >= planned)
        {
            var late = d >= jump.ArrivalDay + PopulationJumpReactDays;
            var perDay = cap.Delivery == "consumable" ? " a day" : "";
            var text = $"{jump.Extra} unplanned personnel arrived at {site.Name} on day {jump.ArrivalDay} on {jump.ArrivalDay - jump.NoticeDay} days' notice (day {jump.NoticeDay}); arrangements sized for the plan's {planned / 1000} {cap.Unit}{perDay} fell short of {demand / 1000}{perDay}.";
            yield return (late ? $"jump|{jump.RecordId}|late" : $"jump|{jump.RecordId}", late
                ? new GapFactor(GapFactorKind.PopulationJump, GapTag.Avoidable, d, text + $" Still short {PopulationJumpReactDays} days after they arrived: the notice left time to resize.", [jump.RecordId])
                : new GapFactor(GapFactorKind.PopulationJump, GapTag.Unforeseeable, d, text + " Nothing before the notice showed it coming.", [jump.RecordId]));
        }
        else if (supplyCap < demand)
        {
            var step = site.Population.Where(p => p.Day <= d).OrderByDescending(p => p.Day).FirstOrDefault();
            var growth = step is { Day: > 1 } ? $" Population rose to {step.Count} on day {step.Day}, which the plan showed from day 1." : "";
            var perDay = cap.Delivery == "consumable" ? " a day" : "";
            yield return ($"demand|{step?.Day}", new GapFactor(GapFactorKind.DemandAboveSupply, GapTag.Avoidable, d,
                $"Arrangements at {site.Name} were sized for {supplyCap / 1000} {cap.Unit}{perDay} against demand of {demand / 1000}{perDay}.{growth}", contributors.Select(a => a.CauseId).ToList()));
        }

        foreach (var a in contributors)
        {
            foreach (var r in _records.Where(r => r.Day == d && r.Subject == a.Id))
            {
                switch (r.Kind)
                {
                    case RecordKind.VendorFailure:
                    {
                        var missing = cap.Lines.Where(l => l.Kind is "operator" or "maintenance" && !a.Lines.Contains(l.Id)).Select(l => (l.Label ?? l.Id).ToLowerInvariant()).ToList();
                        yield return ($"fail|{a.Id}|{missing.Count > 0}", missing.Count > 0 && !a.IsCoordination
                            ? new GapFactor(GapFactorKind.VendorFailure, GapTag.Avoidable, d,
                                $"{a.Id} failed (first on day {d}) and had no {string.Join(" or ", missing)} line; its failure odds were higher every day it ran.{CorNote(a, d)}{IssueNote(a.Id)}", [r.Id, a.CauseId])
                            : new GapFactor(GapFactorKind.VendorFailure, GapTag.Unforeseeable, d,
                                $"{a.Id} failed on day {d} with full operator and maintenance coverage.{CorNote(a, d)}{IssueNote(a.Id)}", [r.Id]));
                        break;
                    }
                    case RecordKind.ExecutionConfusion:
                        yield return ($"conf|{a.Id}", new GapFactor(GapFactorKind.ExecutionConfusion, GapTag.Avoidable, d,
                            $"{a.Id} ran a recurring service with no written terms; from day {d} the vendor and the unit disagreed on what was owed.", [r.Id, a.CauseId]));
                        break;
                }
            }
            if (CapacityShort(a.Id, d))
            {
                // The order preview showed the vendor's capacity left, so ordering past it was a known risk; the
                // supplementary buy that would have covered it was the player's to make (M3-06).
                var ev = _records.Last(r => r.Kind == RecordKind.VendorCapacity && r.Subject == a.Id && r.Day <= d);
                yield return ($"capacity|{a.Id}", new GapFactor(GapFactorKind.VendorCapacity, GapTag.Avoidable, d,
                    $"{a.Id} is an IDIQ order above what {VendorData(a.VendorId)?.Name ?? "the vendor"} could supply (capacity event, day {ev.Day}). The contract was binding, but binding does not add capacity; the order preview showed the capacity left, and a second source or a supplementary buy would have covered the rest.",
                    [ev.Id, a.CauseId]));
            }
            if (a.Uca is not null && _records.LastOrDefault(r => r.Subject == a.Id && r.Day <= d && r.Kind is RecordKind.UcaStopWork or RecordKind.UcaResumed) is { Kind: RecordKind.UcaStopWork } stop)
            {
                var overdue = _records.FirstOrDefault(r => r.Subject == a.Id && r.Kind == RecordKind.UcaOverdue && r.Day <= stop.Day);
                var why = overdue is null
                    ? "incurred costs went past what could be obligated (the UCA obligation cap or the money on the line)"
                    : $"incurred costs went past the obligation cap; it had been past due for definitization since day {overdue.Day}";
                yield return ($"stop|{a.Id}", new GapFactor(GapFactorKind.StopWork, GapTag.Avoidable, d,
                    $"{a.Id} stopped work on day {stop.Day}: {why}.", overdue is null ? [stop.Id, a.CauseId] : [stop.Id, overdue.Id]));
            }
        }
    }

    internal DebriefView BuildDebrief()
    {
        var played = DaysPlayed;
        var bands = new List<CapabilityBand>();
        var gaps = new List<GapView>();
        var bandByKey = new Dictionary<string, int[]>(StringComparer.Ordinal);
        foreach (var cs in _caps)
        {
            var band = Band(cs);
            bandByKey[$"{cs.SiteId}.{cs.CapabilityId}"] = band;
            bands.Add(new CapabilityBand(cs.SiteId, cs.CapabilityId, Cap(cs.CapabilityId).Label, band));
        }

        // Gap episodes in dependency order, so a gap can point at the gap it inherits.
        var episodes = new List<(string Site, string Cap, int Start, int End)>();
        foreach (var site in Scenario.Sites)
            foreach (var capId in _capOrder)
            {
                if (!bandByKey.TryGetValue($"{site.Id}.{capId}", out var band)) continue;
                for (var d = 1; d <= played; d++)
                {
                    if (band[d - 1] is < 0 or >= 1000) continue;
                    var start = d;
                    while (d < played && band[d] is >= 0 and < 1000) d++;
                    episodes.Add((site.Id, capId, start, d));
                }
            }
        int? GapIndexFor(string site, string cap, int day)
        {
            for (var i = 0; i < gaps.Count; i++)
                if (gaps[i].SiteId == site && gaps[i].CapabilityId == cap && gaps[i].StartDay <= day && day <= gaps[i].EndDay) return i;
            return null;
        }
        foreach (var (siteId, capId, start, end) in episodes)
        {
            var factors = new List<GapFactor>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var d = start; d <= end; d++)
                foreach (var (key, f) in DiagnoseDay(siteId, capId, d, (c, day) => GapIndexFor(siteId, c, day)))
                {
                    if (!seen.Add(key)) continue;
                    factors.Add(f.DependsOnGap is { } gi ? f with { Tag = gaps[gi].Tag } : f);
                }
            var band = bandByKey[$"{siteId}.{capId}"];
            var lowest = Enumerable.Range(start, end - start + 1).Min(d => band[d - 1]);
            var down = Enumerable.Range(start, end - start + 1).Count(d => band[d - 1] < Rules.Model.DownThresholdPermille);
            var tag = factors.Count == 0 ? GapTag.Unforeseeable : factors.Max(f => f.Tag);
            var shortfalls = _records.Where(r => r.Kind == RecordKind.Shortfall && r.Subject == $"{siteId}.{capId}" && r.Day >= start && r.Day <= end).Select(r => r.Id).ToList();
            gaps.Add(new GapView(gaps.Count, siteId, capId, Cap(capId).Label, start, end, lowest, down, tag, factors, shortfalls));
        }

        var annotate = new HashSet<RecordKind>
        {
            RecordKind.Event, RecordKind.Withdrawal, RecordKind.UcaOverdue, RecordKind.UcaStopWork, RecordKind.UcaResumed,
            RecordKind.Compliance, RecordKind.Declined, RecordKind.Definitized, RecordKind.ArrangementStart, RecordKind.ArrangementEnd,
            RecordKind.Knowledge, RecordKind.UcaProposal, RecordKind.VendorCapacity, RecordKind.Funding,
        };
        var events = _records.Where(r => annotate.Contains(r.Kind) && r.Day >= 1)
                             .Select(r => new TimelineEvent(r.Day, r.Kind.ToString(), r.Subject, r.Text, r.Id)).ToList();

        var islands = Scenario.Islands.Select(i => i.Id).ToList();
        var decisions = _records.Where(r => r.Kind is RecordKind.Action or RecordKind.Rejected)
                                .Select(r => new DecisionEntry(r.Day, r.Id, r.Text, r.Kind == RecordKind.Rejected,
                                                               islands.Select(i => new KnownIsland(i, TierOn(i, r.Day))).ToList()))
                                .ToList();

        var m = Metrics;
        int I(string k) => m.TryGetValue(k, out var v) ? (int)v : 0;
        long L(string k) => m.TryGetValue(k, out var v) ? (long)v : 0;
        double D(string k) => m.TryGetValue(k, out var v) ? v : 0;
        var score = new ScorecardView(
            D("mission.output"), D("mission.sortieGeneration"), D("mission.throughput"), D("mission.endurance"), D("mission.resilience"),
            L("cost.obligatedCents"), L("cost.incurredCents"), L("cost.unobligatedIncurredCents"),
            I("compliance.unauthorizedCommitments"), I("compliance.ucaOverdue"), I("compliance.hhqInquiries"), I("info.surveys"), I("shop.hoursWorked"),
            gaps.Count, gaps.Count(g => g.Tag == GapTag.Avoidable), gaps.Count(g => g.Tag == GapTag.Discoverable), gaps.Count(g => g.Tag == GapTag.Unforeseeable));

        var commitments = _commitments.Select(c => new CommitmentLine(
            c.Id, c.Kind == CommitmentKind.Handshake ? "handshake" : "outOfScope", c.OpenedDay, c.Decision.ToString().ToLowerInvariant(),
            c.DecidedDay, c.WalkedOff, c.InquiryDrawn, _records.FirstOrDefault(r => r.Id == c.CauseId)?.Text ?? "")).ToList();

        return new DebriefView(Scenario.Title, played, LastDay, bands, events, decisions, gaps, score, commitments);
    }
}
