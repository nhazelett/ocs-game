using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Builders for the view-model contracts in Views/ViewModels.cs (M0-07, ADR-0006). Read-only: nothing here changes
// state or draws a random number, so building a view never changes a game's outcome or its state hash.
public sealed partial class Session
{
    private static int CeilHours(long milli) => (int)((milli + 999) / 1000);
    private static string Rate(CapabilityData cap) => cap.Delivery == "consumable" ? $"{cap.Unit}/day" : cap.Unit;

    internal TopStripView BuildTopStrip()
    {
        var perDay = CeilHours(_hoursPerDayMilli);
        var queued = CeilHours(_work.Where(w => w.CompletedDay is null).Sum(w => w.HoursRemainingMilli));
        return new TopStripView(
            Day, LastDay, _people, perDay, queued, perDay == 0 ? 0 : (queued + perDay - 1) / perDay,
            _cash, _cashInitial, _cashReplenishDay,
            _funding.Where(f => f.UsableOn(Day)).Sum(f => Math.Max(0, f.RemainingCents)),
            Day >= _systemOnlineDay, _systemOnlineDay,
            _payingAgentDay is { } p && Day >= p, _gpcAvailableDay is { } g && Day >= g);
    }

    // ------------------------------------------------------------------ morning board

    private static (BoardCategory, BoardSeverity)? Classify(RecordKind k) => k switch
    {
        RecordKind.Shortfall => (BoardCategory.Failure, BoardSeverity.Alert),
        RecordKind.UcaStopWork => (BoardCategory.Failure, BoardSeverity.Alert),
        RecordKind.VendorFailure => (BoardCategory.Failure, BoardSeverity.Warning),
        RecordKind.ExecutionConfusion => (BoardCategory.Failure, BoardSeverity.Warning),
        RecordKind.PaymentError => (BoardCategory.Failure, BoardSeverity.Warning),
        RecordKind.Withdrawal => (BoardCategory.VendorNotice, BoardSeverity.Alert),
        RecordKind.Declined => (BoardCategory.VendorNotice, BoardSeverity.Warning),
        RecordKind.VendorCapacity => (BoardCategory.Failure, BoardSeverity.Warning),
        RecordKind.UcaProposal => (BoardCategory.VendorNotice, BoardSeverity.Info),
        RecordKind.UcaResumed => (BoardCategory.VendorNotice, BoardSeverity.Info),
        RecordKind.UcaOverdue => (BoardCategory.HigherHq, BoardSeverity.Warning),
        RecordKind.Compliance => (BoardCategory.HigherHq, BoardSeverity.Warning),
        RecordKind.ArrangementStart => (BoardCategory.Delivery, BoardSeverity.Info),
        RecordKind.ArrangementEnd => (BoardCategory.Delivery, BoardSeverity.Info),
        RecordKind.Restored => (BoardCategory.Delivery, BoardSeverity.Info),
        RecordKind.Inbox => (BoardCategory.Request, BoardSeverity.Info),
        RecordKind.Event => (BoardCategory.Event, BoardSeverity.Warning),
        RecordKind.WorkDone or RecordKind.Knowledge or RecordKind.Definitized or RecordKind.Obligation or RecordKind.Cash
            => (BoardCategory.Report, BoardSeverity.Info),
        _ => null, // Action and Rejected are the player's own daytime acts, already seen when made.
    };

    private (BoardLinkKind, string) Link(string subject)
    {
        if (subject == "cash") return (BoardLinkKind.Cash, subject);
        if (subject.StartsWith("survey ", StringComparison.Ordinal)) return (BoardLinkKind.Island, subject[7..]);
        if (_arrangements.Any(a => a.Id == subject)) return (BoardLinkKind.Arrangement, subject);
        if (_cards.Any(c => c.Id == subject)) return (BoardLinkKind.Card, subject);
        if (Scenario.Events.Any(e => e.Id == subject)) return (BoardLinkKind.Event, subject);
        var dot = subject.IndexOf('.');
        if (dot > 0 && Scenario.Sites.Any(s => s.Id == subject[..dot]) && _capData.ContainsKey(subject[(dot + 1)..]))
            return (BoardLinkKind.SiteCapability, subject);
        if (_work.Any(w => w.Label == subject || (w.Payload as string) == subject)) return (BoardLinkKind.Work, subject);
        return (BoardLinkKind.None, subject);
    }

    internal MorningBoardView BuildMorningBoard()
    {
        var items = new List<BoardItem>();
        for (var i = _overnightFrom; i < _overnightTo && i < _records.Count; i++)
        {
            var r = _records[i];
            if (Classify(r.Kind) is not var (cat, sev)) continue;
            var (lk, lid) = Link(r.Subject);
            items.Add(new BoardItem(r.Id, r.Day, cat, sev, lk, lid, r.Text, r.CausedBy));
        }
        var ordered = items.Select((it, i) => (it, i)).OrderByDescending(x => x.it.Severity).ThenBy(x => x.i).Select(x => x.it).ToList();
        return new MorningBoardView(Day, BuildTopStrip(), ordered, BuildTiles());
    }

    private List<CapabilityTile> BuildTiles()
    {
        var tiles = new List<CapabilityTile>();
        var day = Math.Min(Day, LastDay);
        foreach (var cs in _caps)
        {
            var site = SiteData(cs.SiteId);
            var cap = Cap(cs.CapabilityId);
            int? bufferDays = null;
            if (cap.Delivery == "consumable")
            {
                var demand = DemandMilli(site, cs.CapabilityId, day);
                bufferDays = demand > 0 ? (int)(cs.BufferMilli / demand) : null;
            }
            var status = day < site.OpensDay ? "notOpen"
                : cs.TodayPermille >= 1000 ? "ok"
                : cs.TodayPermille < Rules.Model.DownThresholdPermille ? "down" : "degraded";
            tiles.Add(new CapabilityTile(cs.SiteId, cs.CapabilityId, cap.Label, cs.TodayPermille, bufferDays, status, cs.Critical));
        }
        return tiles;
    }

    // ------------------------------------------------------------------ requirement editor

    private string? GapIfOff(CapabilityData cap, CapabilityLineData line)
    {
        var label = (line.Label ?? line.Id).ToLowerInvariant();
        return line.Kind switch
        {
            "core" => null,
            "supplies" when line.Supplies is { } sup =>
                $"No {label} line: {CapLabel(sup).ToLowerInvariant()} has to come from another arrangement at the site, or {cap.Label.ToLowerInvariant()} stops when it runs out.",
            "operator" => $"No {label} line: daily failure odds rise while it runs.",
            "maintenance" => $"No {label} line: daily failure odds rise while it runs.",
            _ => $"No {label} line.",
        };
    }

    private bool Live(Arrangement a) => a.Status is ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended;

    internal RequirementEditorView? BuildRequirementEditor(string cardId)
    {
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        if (card is null || Day < card.SubmittedDay) return null;
        var cap = Cap(card.CapabilityId);
        var site = SiteData(card.SiteId);
        var from = Math.Max(card.RequestedStartDay, 1);
        var to = Math.Min(from + Math.Max(1, card.DurationDays) - 1, LastDay);
        var lines = cap.Lines.Select(l => new RequirementLine(
            l.Id, l.Label ?? l.Id, l.Kind, card.Lines.TryGetValue(l.Id, out var on) && on, l.Supplies, GapIfOff(cap, l))).ToList();
        var deps = cap.DependsOn.Select(dep =>
        {
            var inCard = cap.Lines.Any(l => l.Kind == "supplies" && l.Supplies == dep && card.Lines.TryGetValue(l.Id, out var on) && on);
            var covering = _arrangements.Where(a => a.SiteId == site.Id && Live(a)
                                                    && (a.CapabilityId == dep || a.SuppliedQuantityMilli.ContainsKey(dep))
                                                    && a.StartDay <= to && a.EndDay >= from)
                                        .Select(a => a.Id).ToList();
            var coverage = inCard ? "inRequirement" : covering.Count > 0 ? "arranged" : "none";
            return new RequirementDependency(dep, CapLabel(dep), coverage, covering);
        }).ToList();
        var tied = _arrangements.Where(a => a.CardId == card.Id && a.Status != ArrangementStatus.Declined).Select(a => a.Id).ToList();
        return new RequirementEditorView(
            card.Id, site.Id, site.Name, cap.Id, cap.Label, cap.Unit, card.SubmittedBy, card.SubmittedDay,
            card.RequestedStartDay, card.DurationDays, card.QuantityHint,
            (PeakDemandMilli(site.Id, cap.Id, from, to) + 999) / 1000, lines, deps, tied);
    }

    // ------------------------------------------------------------------ action editor

    internal long CommittedMilli(string vendorId, string capId, int start, int end)
    {
        bool Overlaps(int s0, int e0) => s0 <= end && e0 >= start;
        return _arrangements.Where(x => x.VendorId == vendorId && x.CapabilityId == capId
                                        && x.Status is ArrangementStatus.Pending or ArrangementStatus.Active && Overlaps(x.StartDay, x.EndDay))
                            .Sum(x => x.QuantityMilli)
             + _work.Where(w => w.CompletedDay is null && w.Payload is AwardPayload p && p.Action.Vendor == vendorId && p.CapabilityId == capId
                                && Overlaps(p.Action.StartDay ?? w.CreatedDay, (p.Action.StartDay ?? w.CreatedDay) + Math.Max(1, p.Action.DurationDays) - 1))
                    .Sum(w => ((AwardPayload)w.Payload!).Action.Quantity * 1000);
    }

    private string? InstrumentUnavailable(string id)
    {
        if (!AwardableInM0.Contains(id)) return "not implemented in this build";
        return id switch
        {
            "sf44" when _payingAgentDay is null || Day < _payingAgentDay => "no paying agent with cash on hand yet",
            "gpc" when _gpcAvailableDay is null || Day < _gpcAvailableDay => "no government purchase card accounts yet",
            "bpaCall" when !_vehicles.Any(v => v.Kind == "bpa" && v.VisibleOnDay1 && Day <= v.OrderingPeriodEndDay)
                => "no BPA known to this shop is open for ordering",
            "idiqOrder" when !_vehicles.Any(v => v.Kind == "idiq" && v.VisibleOnDay1 && Day <= v.OrderingPeriodEndDay)
                => "no IDIQ known to this shop is open for ordering",
            _ => null,
        };
    }

    private long? LimitFor(InstrumentData inst) => inst.LimitBasis switch
    {
        "microPurchase" => Rules.Thresholds.MicroPurchaseCents,
        "simplifiedAcquisition" => Rules.Thresholds.SimplifiedAcquisitionCents,
        _ => inst.MaxActionCents,
    };

    internal ActionEditorView? BuildActionEditor(string siteId, string capId, string? cardId)
    {
        if (!Scenario.Sites.Any(s => s.Id == siteId) || !_capData.ContainsKey(capId)) return null;
        var card = cardId is null ? null : _cards.FirstOrDefault(c => c.Id == cardId && Day >= c.SubmittedDay);
        var cap = Cap(capId);
        var selected = card?.Lines.Where(kv => kv.Value).Select(kv => kv.Key).ToList()
                       ?? cap.Lines.Where(l => l.Kind == "core").Select(l => l.Id).ToList();
        var from = card is null ? Day : Math.Max(card.RequestedStartDay, Day);
        var to = card is null ? LastDay : Math.Min(from + Math.Max(1, card.DurationDays) - 1, LastDay);

        var instruments = Rules.Instruments
            .Where(kv => AwardableInM0.Contains(kv.Key))
            .Select(kv =>
            {
                var i = kv.Value;
                var hMin = CeilHours(Hours(i.ShopDays.Min, i.RequiresSystemOrManual));
                var hMax = CeilHours(Hours(i.ShopDays.Max, i.RequiresSystemOrManual));
                return new InstrumentOption(kv.Key, i.Label, i.Binding, i.WrittenTerms, InstrumentUnavailable(kv.Key), LimitFor(i),
                                            hMin, hMax, i.StartLagDays.Min, i.StartLagDays.Max);
            }).ToList();

        var vendors = Scenario.Vendors
            .Select(v => (v, o: v.Offers.FirstOrDefault(o => o.Capability == capId)))
            .Where(x => x.o is not null)
            .Select(x => new VendorOption(
                x.v.Id, x.v.Name, x.v.Island, IslandData(x.v.Island).Name, x.o!.Lines,
                (int)Math.Floor(x.o.CapacityPerDay), (int)Math.Max(0, (Fixed.Milli(x.o.CapacityPerDay) - CommittedMilli(x.v.Id, capId, Day, Day)) / 1000),
                x.o.LeadTimeDays, UnitPriceFor(x.v, x.o), Fixed.Permille(x.o.MobilizationFraction), x.v.AcceptsCash, x.v.AcceptsGpc, x.v.VehicleOnly,
                _standing.TryGetValue(x.v.Id, out var st) ? (st.DeclinesWork ? "declines work after an unpaid handshake" : $"quotes {st.MarkupPermille / 10}% higher after an unpaid handshake") : null))
            .ToList();

        var vehicles = _vehicles
            .Where(v => v.VisibleOnDay1 && vendors.Any(o => o.Id == v.Vendor))
            .Select(v => new VehicleOption(v.Id, v.Kind, v.Vendor, v.ScopeTags, v.CallLimitCents, v.OrderingPeriodEndDay,
                                           v.PlaceOfPerformance, Day <= v.OrderingPeriodEndDay,
                                           Idiq(v.Id)?.CeilingCents, Idiq(v.Id)?.OrderedCents, Idiq(v.Id)?.GuaranteeUnorderedCents))
            .ToList();

        var funding = _funding.Select(f =>
        {
            var type = Scenario.FundingLines.FirstOrDefault(d => d.Id == f.Id)?.Type ?? "";
            return new FundingOption(f.Id, type, f.RemainingCents, f.AvailableDay, f.ExpiresDay, f.UsableOn(Day));
        }).ToList();

        return new ActionEditorView(Day, siteId, capId, cap.Label, cap.Unit, card?.Id, selected,
                                    (PeakDemandMilli(siteId, capId, from, to) + 999) / 1000, _cash,
                                    instruments, vendors, vehicles, funding);
    }

    internal ActionPreview BuildPreview(AwardAction a)
    {
        var chk = CheckAward(a);
        var queue = CeilHours(_work.Where(w => w.CompletedDay is null).Sum(w => w.HoursRemainingMilli));
        if (chk.Instrument is not { } inst || chk.Offer is not { } offer || chk.SiteId is not { } siteId || chk.CapabilityId is not { } capId)
            return new ActionPreview(false, chk.Stops, [], 0, 0, 0, 0, 0, 0, 0, queue);

        // Timing, best and worst roll, ignoring the queue ahead.
        var immediate = a.Instrument is "sf44" or "gpc";
        int Completion(int shopDays, int approvalLag)
        {
            if (immediate) return Day;
            var wait = Day + Scenario.Actors.Fm.CertificationLagDays + (a.Instrument == "uca" ? approvalLag : 0);
            var hours = Hours(shopDays, inst.RequiresSystemOrManual);
            var days = _hoursPerDayMilli <= 0 ? 0 : (int)((hours + _hoursPerDayMilli - 1) / _hoursPerDayMilli);
            return wait + Math.Max(0, days - 1);
        }
        var minDone = Completion(inst.ShopDays.Min, Rules.Uca.ApprovalLagDays.Min);
        var maxDone = Completion(inst.ShopDays.Max, Rules.Uca.ApprovalLagDays.Max);
        var startMin = Math.Max(a.StartDay ?? 0, minDone + 1 + inst.StartLagDays.Min + offer.LeadTimeDays);
        var startMax = Math.Max(a.StartDay ?? 0, maxDone + 1 + inst.StartLagDays.Max + offer.LeadTimeDays);
        var hMin = immediate ? 0 : CeilHours(Hours(inst.ShopDays.Min, inst.RequiresSystemOrManual));
        var hMax = immediate ? 0 : CeilHours(Hours(inst.ShopDays.Max, inst.RequiresSystemOrManual));

        var gaps = new List<string>();
        var cap = Cap(capId);
        var site = SiteData(siteId);
        var vendor = chk.Vendor!;
        foreach (var l in cap.Lines.Where(l => l.Kind != "core" && !chk.Lines.Contains(l.Id)))
            if (GapIfOff(cap, l) is { } g) gaps.Add(g);
        var end = startMax + Math.Max(1, a.DurationDays) - 1;
        var peak = (PeakDemandMilli(siteId, capId, Math.Min(startMin, LastDay), Math.Min(end, LastDay)) + 999) / 1000;
        if (a.Quantity < peak)
            gaps.Add($"{a.Quantity} {Rate(cap)} against peak demand of {peak} {Rate(cap)} at {site.Name}; the difference is a shortfall unless another arrangement covers it.");
        if (chk.Card is { } card)
        {
            if (startMin > card.RequestedStartDay)
                gaps.Add($"Earliest start is day {startMin}; the requirement asks for day {card.RequestedStartDay}.");
            var need = card.RequestedStartDay + card.DurationDays - 1;
            if (startMin + a.DurationDays - 1 < Math.Min(need, LastDay))
                gaps.Add($"Performance ends by day {startMin + a.DurationDays - 1}; the requirement runs to day {need}.");
        }
        else if (startMin > site.OpensDay && Day <= site.OpensDay)
            gaps.Add($"Earliest start is day {startMin}; {site.Name} opens day {site.OpensDay}.");
        if (!inst.WrittenTerms && a.DurationDays >= Rules.Model.ExecutionConfusionMinDurationDays)
            gaps.Add($"No written terms on a {a.DurationDays}-day service: execution-confusion events can occur while it runs.");
        if (a.Instrument == "bpaCall") gaps.Add("A BPA is not binding: the vendor may decline the call.");
        if (a.Instrument == "idiqOrder")
        {
            var free = Math.Max(0, Fixed.Milli(offer.CapacityPerDay) - CommittedMilli(vendor.Id, capId, startMin, Math.Min(end, LastDay)));
            if (a.Quantity * 1000 > free)
                gaps.Add($"{vendor.Name} has about {free / 1000} {Rate(cap)} of capacity left on those days: the order is binding and accepted, but delivery above capacity falls short (a capacity event).");
            if (Idiq(a.Vehicle) is { GuaranteeUnorderedCents: > 0 } q)
                gaps.Add($"{Money(Math.Min(chk.TotalCents, Math.Max(0, q.GuaranteeUnorderedCents - PendingOrderCents(q.VehicleId))))} of this order comes from {q.VehicleId}'s minimum guarantee, already obligated.");
        }
        if (a.Instrument == "uca")
        {
            var before = Fixed.Permille(Rules.Uca.ObligationCapBeforeProposal) / 10;
            var after = Fixed.Permille(Rules.Uca.ObligationCapAfterProposal) / 10;
            gaps.Add($"Obligations capped at {before}% of the NTE until the qualifying proposal and {after}% after; definitization is due within {Rules.Uca.DefinitizeWithinDays} days and takes shop time.");
        }
        if (vendor.Island != site.Island)
            gaps.Add($"Supplied from {IslandData(vendor.Island).Name}: deliveries depend on the transport links to {IslandData(site.Island).Name}.");
        else if (StockIsland(vendor, capId) is var stock && stock != site.Island && TierOn(site.Island, Day) == "surveyed")
            gaps.Add($"{vendor.Name} stocks this from {IslandData(stock).Name} (survey): deliveries depend on the transport links to {IslandData(site.Island).Name}.");
        if (inst.RequiresSystemOrManual && Day < _systemOnlineDay)
            gaps.Add($"The contract-writing system is offline until day {_systemOnlineDay}: the action takes longer and invoices are manual, which carries payment-error risk.");

        return new ActionPreview(chk.Stops.Count == 0, chk.Stops, gaps, chk.DailyCents, chk.MobilizationCents, chk.TotalCents,
                                 hMin, hMax, startMin, startMax, queue);
    }
}
