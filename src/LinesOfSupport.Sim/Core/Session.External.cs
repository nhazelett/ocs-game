using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// External support requests from the OCS seat (backlog M3-05, SPEC 4.1 "External support contract", SPEC 5 approach 4).
// The shop prepares the request (shop days), the SCO and higher approve it (rules approvalLagDays), an external office
// awards and obligates the task order from the unit's funding line, and the contractor mobilizes (rules
// mobilizationDays). The contract is self-managed: it brings its own operators, maintenance and consumables, so it is
// supplied on the site's own island (docs/assumptions.md). Every draw comes from the "external" stream at the moment
// of the request, so games without a request replay exactly as before and a request never shifts another draw.
public sealed partial class Session
{
    private const string ExternalInstrument = "externalSupport";
    private readonly List<ExternalSupportState> _external = [];
    internal IReadOnlyList<ExternalSupportState> ExternalRequests => _external;

    private ExternalSupportOfferData? ExternalOffer(string capId)
        => Scenario.Actors.ExternalSupport?.Offers.FirstOrDefault(o => o.Capability == capId);

    private string ExternalProvider => Scenario.Actors.ExternalSupport?.Provider ?? "the external support contractor";

    /// <summary>Who supplies an arrangement, in words: the vendor, the external support contractor, or the other unit.</summary>
    internal string SupplierName(Arrangement a)
        => a.Instrument == ExternalInstrument ? ExternalProvider : a.Instrument == AcsaInstrument ? PartnerName : a.Instrument == HostNationInstrument && a.VendorId is null ? HostNationName : a.VendorId is null ? "the other unit" : VendorData(a.VendorId)!.Name;

    /// <summary>Quantity already requested from the provider for a capability (requests not refused, contracts not ended).</summary>
    private long ExternalCommittedMilli(string capId)
        => _external.Where(r => r.CapabilityId == capId && r.Status != ExternalSupportStatus.Refused
                                && _arrangements.FirstOrDefault(a => a.Id == r.Id) is not { Status: ArrangementStatus.Ended })
                    .Sum(r => r.QuantityMilli);

    internal sealed record ExternalCheck(
        List<string> Stops, InstrumentData? Instrument, ExternalSupportOfferData? Offer, CardState? Card,
        string? SiteId, string? CapabilityId, List<string> Lines, long DailyCents, long MobilizationCents, long TotalCents);

    /// <summary>Every check a request must pass, with what it would cost. Reads state only; draws no random numbers.</summary>
    internal ExternalCheck CheckExternalSupport(RequestExternalSupportAction r)
    {
        var stops = new List<string>();
        ExternalCheck Fail(string why) => new([.. stops, why], null, null, null, null, null, [], 0, 0, 0);
        if (!Rules.Instruments.TryGetValue(ExternalInstrument, out var inst)) return Fail("externalSupport is not in the rules");
        if (string.IsNullOrWhiteSpace(r.Id)) stops.Add("a request needs an id");
        else if (_arrangements.Any(x => x.Id == r.Id) || _external.Any(x => x.Id == r.Id)
                 || _work.Any(w => w.CompletedDay is null && w.Payload is AwardPayload p && p.Action.Id == r.Id))
            stops.Add($"id {r.Id} is already used");

        CardState? card = null;
        if (r.Card is not null)
        {
            card = _cards.FirstOrDefault(c => c.Id == r.Card);
            if (card is null) return Fail($"card {r.Card} does not exist");
            if (Day < card.SubmittedDay) return Fail($"card {r.Card} has not been submitted yet");
        }
        var siteId = r.Site ?? card?.SiteId;
        var capId = r.Capability ?? card?.CapabilityId;
        if (siteId is null || capId is null) return Fail("a request needs a card, or a site and a capability");
        if (!Scenario.Sites.Any(s => s.Id == siteId)) return Fail($"site {siteId} does not exist");
        if (!_capData.ContainsKey(capId)) return Fail($"capability {capId} does not exist");
        if (Scenario.Actors.ExternalSupport is null) return Fail("no external support contract covers this theater in this scenario");
        var offer = ExternalOffer(capId);
        if (offer is null) return Fail($"the external support contract does not provide {CapLabel(capId).ToLowerInvariant()}");
        if (offer.Sites.Count > 0 && !offer.Sites.Contains(siteId)) stops.Add($"the external support contract does not cover {SiteName(siteId)}");

        var cap = Cap(capId);
        var lines = r.Lines ?? card?.Lines.Where(kv => kv.Value).Select(kv => kv.Key).ToList() ?? [.. offer.Lines];
        foreach (var l in lines)
        {
            if (!cap.Lines.Any(x => x.Id == l)) stops.Add($"{l} is not a {capId} line");
            else if (!offer.Lines.Contains(l)) stops.Add($"the external support contract does not provide {LineLabel(capId, l)}");
        }
        // Minimum useful scope: the contract is self-managed, so the contractor runs and maintains what it brings.
        foreach (var kind in new[] { "core", "operator", "maintenance" })
            foreach (var l in cap.Lines.Where(x => x.Kind == kind && offer.Lines.Contains(x.Id) && !lines.Contains(x.Id)))
                stops.Add($"an external support contract is self-managed: the request must include {(l.Label ?? l.Id).ToLowerInvariant()}");

        if (r.Quantity <= 0) stops.Add("quantity must be positive");
        var min = inst.MinimumUsefulDurationDays ?? 1;
        if (r.DurationDays < min) stops.Add($"an external support contract needs at least {min} days of performance to be worth mobilizing; {r.DurationDays} requested");
        var capacity = Fixed.Milli(offer.CapacityPerDay);
        var committed = ExternalCommittedMilli(capId);
        if (r.Quantity * 1000 + committed > capacity)
            stops.Add($"the external support contract can provide {offer.CapacityPerDay:0} {cap.Unit}/day of {CapLabel(capId).ToLowerInvariant()} in total and {committed / 1000} is already requested");

        var daily = r.Quantity * offer.UnitPriceCents;
        var baseCost = daily * Math.Max(0, r.DurationDays);
        var mob = Fixed.MulPermille(baseCost, Fixed.Permille(offer.MobilizationFraction));
        var total = baseCost + mob;

        // The unit's money goes to the external office with the request, so the line is checked now and again at award.
        var line = _funding.FirstOrDefault(f => f.Id == r.FundingLine);
        if (line is null) stops.Add("a request needs a funding line: the external office obligates the unit's money");
        else
        {
            if (!line.UsableOn(Day)) stops.Add($"funding line {line.Id} is not usable on day {Day}");
            else if (line.RemainingCents < total) stops.Add($"funding line {line.Id} has {Money(line.RemainingCents)} left for an estimated {Money(total)}");
            stops.AddRange(FundingStops(line, capId, r.StartDay, r.DurationDays));
        }
        if (r.Cor is not null) stops.AddRange(CorStops(r.Cor, inst));
        return new ExternalCheck(stops, inst, offer, card, siteId, capId, lines, daily, mob, total);
    }

    private ActionResult ApplyRequestExternalSupport(RequestExternalSupportAction r)
    {
        var chk = CheckExternalSupport(r);
        if (chk.Stops.Count > 0) return ActionResult.Stop([.. chk.Stops]);
        var inst = chk.Instrument!;
        var shopDays = _rngExternal.NextInt(inst.ShopDays);
        var approval = _rngExternal.NextInt(inst.ApprovalLagDays ?? new IntRange(1, 1));
        var mobilization = _rngExternal.NextInt(inst.MobilizationDays ?? new IntRange(0, 0));
        var capId = chk.CapabilityId!;
        var where = $"{CapLabel(capId)} at {SiteName(chk.SiteId!)}";
        var lineText = string.Join(", ", chk.Lines.Select(l => LineLabel(capId, l)));
        var rec = AddRecord(Day, RecordKind.Action, r.Id,
            $"External support request {r.Id} started: {where}, {r.Quantity} {Cap(capId).Unit}/day for {r.DurationDays} days ({lineText}), estimated {Money(chk.TotalCents)} from {r.FundingLine}; {shopDays} shop day(s) to prepare, then the SCO and the external office.", []);
        _external.Add(new ExternalSupportState
        {
            Id = r.Id, SiteId = chk.SiteId!, CapabilityId = capId, Lines = chk.Lines, QuantityMilli = r.Quantity * 1000,
            DurationDays = r.DurationDays, RequestedStartDay = r.StartDay, FundingLineId = r.FundingLine, CardId = chk.Card?.Id,
            CorId = r.Cor, DailyCostCents = chk.DailyCents, MobilizationCents = chk.MobilizationCents, TotalCents = chk.TotalCents,
            ReliabilityPermille = Fixed.Permille(chk.Offer!.Reliability), RequestedDay = Day,
            ApprovalLagDays = approval, MobilizationDays = mobilization, CauseId = rec,
        });
        AddWork(WorkKind.ExternalSupportRequest, $"External support request {r.Id}", Hours(shopDays, inst.RequiresSystemOrManual),
                Day + Scenario.Actors.Fm.CertificationLagDays, r.Id, rec);
        return ActionResult.Ok("request in work");
    }

    /// <summary>The shop finished the request package: it goes to the SCO and on to the external office.</summary>
    private void CompleteExternalRequestWork(string id, int day, string causeId)
    {
        var r = _external.First(x => x.Id == id);
        r.Status = ExternalSupportStatus.InApproval;
        r.SubmittedDay = day;
        r.ApprovalDay = day + r.ApprovalLagDays;
        var lag = Rules.Instruments[ExternalInstrument].ApprovalLagDays ?? new IntRange(r.ApprovalLagDays, r.ApprovalLagDays);
        AddRecord(day, RecordKind.ExternalSupport, id,
            $"External support request {id} submitted through the SCO to the external office. Approval usually takes {lag.Min} to {lag.Max} days; the contractor then mobilizes before anything arrives.", [causeId]);
    }

    /// <summary>On the approval day the external office awards the task order and obligates it, or refuses it.</summary>
    private void ExternalSupportClocks(int d)
    {
        foreach (var r in _external.Where(x => x.Status == ExternalSupportStatus.InApproval && x.ApprovalDay == d))
        {
            var line = _funding.First(f => f.Id == r.FundingLineId);
            if (!line.UsableOn(d) || line.RemainingCents < r.TotalCents)
            {
                r.Status = ExternalSupportStatus.Refused;
                r.RefusedReason = FundingShortReason(line, r.TotalCents, d);
                var rej = AddRecord(d, RecordKind.ExternalSupport, r.Id,
                    $"The external office approved {r.Id} but could not award it: {r.RefusedReason}. The request lapses; a new one starts the approval over.", [r.CauseId]);
                _fundingRejections.Add((r.Id, r.SiteId, r.CapabilityId, d, rej));
                continue;
            }
            line.ObligatedCents += r.TotalCents;
            var earliest = d + 1 + r.MobilizationDays;
            var start = Math.Max(r.RequestedStartDay ?? earliest, earliest);
            r.Status = ExternalSupportStatus.Awarded;
            r.AwardedDay = d;
            r.StartDay = start;
            var arr = new Arrangement
            {
                Id = r.Id, Instrument = ExternalInstrument, SiteId = r.SiteId, CapabilityId = r.CapabilityId, Lines = r.Lines,
                SupplierIslandId = SiteData(r.SiteId).Island, QuantityMilli = r.QuantityMilli, DailyCostCents = r.DailyCostCents,
                MobilizationCents = r.MobilizationCents, ObligatedCents = r.TotalCents, FundingLineId = line.Id,
                ReliabilityPermille = r.ReliabilityPermille, CreatedDay = d, CardId = r.CardId, CauseId = r.CauseId,
                StartDay = start, EndDay = start + r.DurationDays - 1, CorId = r.CorId,
            };
            foreach (var lineId in r.Lines)
            {
                var cl = Cap(r.CapabilityId).Lines.First(l => l.Id == lineId);
                if (cl is { Kind: "supplies", Supplies: { } target } && CapState(r.SiteId, target) is not null)
                    arr.SuppliedQuantityMilli[target] = Fixed.MulPermille(PeakDemandMilli(r.SiteId, target, arr.StartDay, arr.EndDay), Rules.Model.SupplyLineSizingPermille);
            }
            _arrangements.Add(arr);
            AddRecord(d, RecordKind.ExternalSupport, r.Id,
                $"The external office awarded {r.Id} to {ExternalProvider} after {d - r.SubmittedDay} days in approval: {CapLabel(r.CapabilityId)} at {SiteName(r.SiteId)}, {r.QuantityMilli / 1000} {Cap(r.CapabilityId).Unit}/day. Obligated {Money(r.TotalCents)} from {line.Id}. The contractor mobilizes for {r.MobilizationDays} days; performance starts day {start}, ends day {arr.EndDay}.",
                [r.CauseId]);
        }
    }

    internal ActionPreview BuildExternalSupportPreview(RequestExternalSupportAction r)
    {
        var chk = CheckExternalSupport(r);
        var queue = CeilHours(_work.Where(w => w.CompletedDay is null).Sum(w => w.HoursRemainingMilli));
        if (chk.Instrument is not { } inst || chk.SiteId is not { } siteId || chk.CapabilityId is not { } capId)
            return new ActionPreview(false, chk.Stops, [], 0, 0, 0, 0, 0, 0, 0, queue);
        var approval = inst.ApprovalLagDays ?? new IntRange(1, 1);
        var mobilization = inst.MobilizationDays ?? new IntRange(0, 0);
        int Prepared(int shopDays)
        {
            var hours = Hours(shopDays, inst.RequiresSystemOrManual);
            var days = _hoursPerDayMilli <= 0 ? 0 : (int)((hours + _hoursPerDayMilli - 1) / _hoursPerDayMilli);
            return Day + Scenario.Actors.Fm.CertificationLagDays + Math.Max(0, days - 1);
        }
        var startMin = Math.Max(r.StartDay ?? 0, Prepared(inst.ShopDays.Min) + approval.Min + 1 + mobilization.Min);
        var startMax = Math.Max(r.StartDay ?? 0, Prepared(inst.ShopDays.Max) + approval.Max + 1 + mobilization.Max);
        var hMin = CeilHours(Hours(inst.ShopDays.Min, inst.RequiresSystemOrManual));
        var hMax = CeilHours(Hours(inst.ShopDays.Max, inst.RequiresSystemOrManual));

        var cap = Cap(capId);
        var site = SiteData(siteId);
        var gaps = new List<string>
        {
            $"Approval through the SCO and higher takes {approval.Min} to {approval.Max} days and the contractor then mobilizes for {mobilization.Min} to {mobilization.Max} days: nothing arrives before day {startMin}. It is the enduring answer; something else has to cover the need until then.",
        };
        foreach (var l in cap.Lines.Where(l => l.Kind == "supplies" && !chk.Lines.Contains(l.Id)))
            if (GapIfOff(cap, l) is { } g) gaps.Add(g);
        var end = startMax + Math.Max(1, r.DurationDays) - 1;
        var peak = (PeakDemandMilli(siteId, capId, Math.Min(startMin, LastDay), Math.Min(end, LastDay)) + 999) / 1000;
        if (r.Quantity < peak)
            gaps.Add($"{r.Quantity} {Rate(cap)} against peak demand of {peak} {Rate(cap)} at {site.Name}; the difference is a shortfall unless another arrangement covers it.");
        if (chk.Card is { } card && startMin > card.RequestedStartDay)
            gaps.Add($"Earliest start is day {startMin}; the requirement asks for day {card.RequestedStartDay}.");
        gaps.Add($"The external office obligates the task order from {r.FundingLine} when it awards, around day {Prepared(inst.ShopDays.Min) + approval.Min} to {Prepared(inst.ShopDays.Max) + approval.Max}: the line needs {Money(chk.TotalCents)} left then, or the award fails and the request starts over.");
        if (CorGap(new AwardAction { Cor = r.Cor }, inst) is { } corGap) gaps.Add(corGap);
        return new ActionPreview(chk.Stops.Count == 0, chk.Stops, gaps, chk.DailyCents, chk.MobilizationCents, chk.TotalCents,
                                 hMin, hMax, startMin, startMax, queue);
    }

    internal IReadOnlyList<ExternalSupportRequestView> BuildExternalSupportRequests()
    {
        var lag = Rules.Instruments.TryGetValue(ExternalInstrument, out var inst) ? inst.ApprovalLagDays : null;
        return _external.Select(r => new ExternalSupportRequestView(
            r.Id, r.SiteId, r.CapabilityId, r.Status.ToString(), r.RequestedDay, r.SubmittedDay,
            r.SubmittedDay is { } s && lag is { } l ? s + l.Min : null, r.SubmittedDay is { } s2 && lag is { } l2 ? s2 + l2.Max : null,
            r.AwardedDay, r.StartDay, r.StartDay is { } st ? st + r.DurationDays - 1 : null,
            r.QuantityMilli / 1000, r.TotalCents, r.FundingLineId, r.CardId, r.RefusedReason)).ToList();
    }

    /// <summary>Debrief: how an external support contract that came too late got there, for the uncovered-gap text.</summary>
    private string ExternalNote(Arrangement a)
    {
        if (a.Instrument != ExternalInstrument || _external.FirstOrDefault(r => r.Id == a.Id) is not { } r) return "";
        return $" {a.Id} is an external support contract: requested day {r.RequestedDay}, submitted day {r.SubmittedDay}, awarded day {r.AwardedDay} after {r.ApprovalLagDays} days in approval, then {r.MobilizationDays} days of mobilization. It is the enduring answer, not the first-week one; a bridge was needed until it arrived.";
    }
}
