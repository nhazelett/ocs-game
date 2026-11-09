using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Acquisition and cross-servicing agreement orders (backlog M3-06, SPEC 4.1 "ACSA": 2 to 5 days, partner stock limits,
// withdrawable, reimbursement later). The partner is scenario data (actors.acsaPartner). Orders draw from an "acsa"
// stream at the moment they are placed, so games without one replay exactly as before. Event effect acsa.withdraw ends
// the partner's support for a capability (or all) after a notice, the way its own tempo rising would.
public sealed partial class Session
{
    private const string AcsaInstrument = "acsa";
    private readonly HashSet<string> _acsaWithdrawn = new(StringComparer.Ordinal);
    private bool _acsaWithdrawnAll;

    private sealed record AcsaPayload(AcsaOrderAction Order, string SiteId, string CapabilityId, List<string> Lines,
        long DailyCents, long TotalCents, int StartLag);

    private AcsaPartnerData? Partner => Scenario.Actors.AcsaPartner is { Present: true } p ? p : null;
    internal string PartnerName => Partner?.Name ?? "the partner force";

    /// <summary>Capabilities the partner has withdrawn support for ("*" when it withdrew everything).</summary>
    internal IReadOnlyCollection<string> AcsaWithdrawn => _acsaWithdrawnAll ? ["*"] : _acsaWithdrawn;
    private bool Withdrawn(string capId) => _acsaWithdrawnAll || _acsaWithdrawn.Contains(capId);

    private long AcsaCommittedMilli(string capId, int start, int end)
    {
        bool Overlaps(int s0, int e0) => s0 <= end && e0 >= start;
        return _arrangements.Where(a => a.Instrument == AcsaInstrument && a.CapabilityId == capId
                                        && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active && Overlaps(a.StartDay, a.EndDay))
                            .Sum(a => a.QuantityMilli)
             + _work.Where(w => w.CompletedDay is null && w.Payload is AcsaPayload p && p.CapabilityId == capId)
                    .Sum(w => ((AcsaPayload)w.Payload!).Order.Quantity * 1000);
    }

    internal sealed record AcsaCheck(List<string> Stops, InstrumentData? Instrument, CardState? Card, string? SiteId,
        string? CapabilityId, List<string> Lines, long DailyCents, long TotalCents);

    /// <summary>Every check an ACSA order must pass, with its cost. Reads state only; draws no random numbers.</summary>
    internal AcsaCheck CheckAcsa(AcsaOrderAction o)
    {
        var stops = new List<string>();
        AcsaCheck Fail(string why) => new([.. stops, why], null, null, null, null, [], 0, 0);
        if (!Rules.Instruments.TryGetValue(AcsaInstrument, out var inst)) return Fail("acsa is not in the rules");
        if (string.IsNullOrWhiteSpace(o.Id)) stops.Add("an order needs an id");
        else if (IdInUse(o.Id)) stops.Add($"id {NameOf(o.Id)} is already used");
        CardState? card = null;
        if (o.Card is not null)
        {
            card = _cards.FirstOrDefault(c => c.Id == o.Card);
            if (card is null) return Fail($"{CardName(o.Card)} does not exist");
            if (Day < card.SubmittedDay) return Fail($"{CardName(o.Card)} has not been submitted yet");
        }
        var siteId = o.Site ?? card?.SiteId;
        var capId = o.Capability ?? card?.CapabilityId;
        if (siteId is null || capId is null) return Fail("an order needs a card, or a site and a capability");
        if (!Scenario.Sites.Any(s => s.Id == siteId)) return Fail($"site {siteId} does not exist");
        if (!_capData.ContainsKey(capId)) return Fail($"capability {capId} does not exist");
        if (Partner is not { } partner) return Fail("no partner force with an ACSA is present in this scenario");
        if (!partner.Stock.TryGetValue(capId, out var stock) || stock <= 0)
            return Fail($"{partner.Name} has no {CapLabel(capId).ToLowerInvariant()} to spare under the ACSA");
        if (Withdrawn(capId)) stops.Add($"{partner.Name} has withdrawn its {CapLabel(capId).ToLowerInvariant()} support: its own needs come first");
        if (o.Quantity <= 0) stops.Add("quantity must be positive");
        if (o.DurationDays <= 0) stops.Add("duration must be positive");
        var start = Math.Max(o.StartDay ?? Day, Day);
        var committed = AcsaCommittedMilli(capId, start, start + Math.Max(1, o.DurationDays) - 1);
        if (o.Quantity * 1000 + committed > Fixed.Milli(stock))
            stops.Add($"{partner.Name} can spare {stock:0} {Cap(capId).Unit}/day of {CapLabel(capId).ToLowerInvariant()} and {committed / 1000} is already ordered");
        var lines = Cap(capId).Lines.Where(l => l.Kind != "supplies").Select(l => l.Id).ToList();
        var daily = o.Quantity * partner.UnitPriceCents.GetValueOrDefault(capId);
        var total = daily * Math.Max(0, o.DurationDays);
        var line = _funding.FirstOrDefault(f => f.Id == o.FundingLine);
        if (line is null) stops.Add("an ACSA order needs a funding line for the reimbursement");
        else
        {
            if (!line.UsableOn(Day)) stops.Add($"{NameOf(line.Id)} is not usable on day {Day}");
            else if (line.RemainingCents < total) stops.Add($"{NameOf(line.Id)} has {Money(line.RemainingCents)} left for {Money(total)}");
            stops.AddRange(FundingStops(line, capId, o.StartDay, o.DurationDays));
        }
        return new AcsaCheck(stops, inst, card, siteId, capId, lines, daily, total);
    }

    private ActionResult ApplyAcsaOrder(AcsaOrderAction o)
    {
        var chk = CheckAcsa(o);
        if (chk.Stops.Count > 0) return ActionResult.Stop([.. chk.Stops]);
        var inst = chk.Instrument!;
        var shopDays = _rngAcsa.NextInt(inst.ShopDays);
        var lag = _rngAcsa.NextInt(inst.StartLagDays);
        var capId = chk.CapabilityId!;
        var rec = AddRecord(Day, RecordKind.Action, o.Id,
            $"ACSA order to {PartnerName} started: {CapLabel(capId)} at {SiteName(chk.SiteId!)}, {o.Quantity} {Cap(capId).Unit}/day for {o.DurationDays} days; reimbursable {Money(chk.TotalCents)} from {NameOf(o.FundingLine)}; {Plural(shopDays, "shop day")}.", []);
        AddWork(WorkKind.AcsaOrder, $"ACSA order for {Lower(CapLabel(capId))} at {SiteName(chk.SiteId!)}", Hours(shopDays, inst.RequiresSystemOrManual), Day + Scenario.Actors.Fm.CertificationLagDays,
                new AcsaPayload(o, chk.SiteId!, capId, chk.Lines, chk.DailyCents, chk.TotalCents, lag), rec);
        return ActionResult.Ok("in work");
    }

    private void CompleteAcsaOrder(AcsaPayload p, int day, string causeId)
    {
        var o = p.Order;
        var line = _funding.First(f => f.Id == o.FundingLine);
        string? refusal = Withdrawn(p.CapabilityId) ? $"{PartnerName} withdrew its {CapLabel(p.CapabilityId).ToLowerInvariant()} support before the order was placed"
            : !line.UsableOn(day) || line.RemainingCents < p.TotalCents ? FundingShortReason(line, p.TotalCents, day) : null;
        if (refusal is not null)
        {
            var rej = AddRecord(day, RecordKind.Rejected, o.Id, $"ACSA order for {Lower(CapLabel(p.CapabilityId))} at {SiteName(p.SiteId)} could not be placed on day {day}: {refusal}.", [causeId]);
            if (!Withdrawn(p.CapabilityId)) _fundingRejections.Add((o.Id, p.SiteId, p.CapabilityId, day, rej));
            return;
        }
        line.ObligatedCents += p.TotalCents;
        var earliest = day + 1 + p.StartLag;
        var start = Math.Max(o.StartDay ?? earliest, earliest);
        var partner = Partner!;
        _arrangements.Add(new Arrangement
        {
            Id = o.Id, Instrument = AcsaInstrument, SiteId = p.SiteId, CapabilityId = p.CapabilityId, Lines = p.Lines,
            SupplierIslandId = partner.Island ?? SiteData(p.SiteId).Island, QuantityMilli = o.Quantity * 1000, DailyCostCents = p.DailyCents,
            ObligatedCents = p.TotalCents, FundingLineId = line.Id, ReliabilityPermille = Fixed.Permille(partner.Reliability),
            CreatedDay = day, CardId = o.Card, CauseId = causeId, StartDay = start, EndDay = start + o.DurationDays - 1,
        });
        AddRecord(day, RecordKind.WorkDone, o.Id,
            $"ACSA order for {Lower(CapLabel(p.CapabilityId))} at {SiteName(p.SiteId)} placed with {partner.Name}; support starts day {start}, ends day {start + o.DurationDays - 1}. Reimbursement of {Money(p.TotalCents)} obligated from {NameOf(line.Id)}. The partner can withdraw it when its own needs rise.", [causeId]);
    }

    /// <summary>Event effect acsa.withdraw: the partner ends its support for a capability (null: everything) after a notice.</summary>
    internal void WithdrawAcsa(string? capId, int day, int noticeDays, string cause)
    {
        if (capId is null) _acsaWithdrawnAll = true; else _acsaWithdrawn.Add(capId);
        var what = capId is null ? "all its support" : $"its {CapLabel(capId).ToLowerInvariant()} support";
        var running = _arrangements.Where(a => a.Instrument == AcsaInstrument && (capId is null || a.CapabilityId == capId)
                                               && a.Status is ArrangementStatus.Active or ArrangementStatus.Pending or ArrangementStatus.Suspended).ToList();
        foreach (var a in running)
        {
            a.EndDay = Math.Min(a.EndDay, day + noticeDays - 1);
            if (a.Status == ArrangementStatus.Pending && a.EndDay < a.StartDay) a.Status = ArrangementStatus.Ended;
            // Unperformed days are not reimbursed: the unspent reimbursement returns to a line that is still current.
            var performed = Math.Max(0, a.EndDay - a.StartDay + 1);
            var back = Math.Max(0, a.ObligatedCents - a.DailyCostCents * performed);
            var tail = "";
            if (back > 0 && _funding.FirstOrDefault(f => f.Id == a.FundingLineId) is { } fl)
            {
                a.ObligatedCents -= back;
                if (fl.UsableOn(day)) { fl.ObligatedCents -= back; tail = $" {Money(back)} of reimbursement returns to {NameOf(fl.Id)}."; }
                else tail = $" {Money(back)} of reimbursement is released; {NameOf(fl.Id)} has expired, so it lapses.";
            }
            AddRecord(day, RecordKind.Withdrawal, a.Id,
                $"{PartnerName} withdrew the ACSA order ({CapLabel(a.CapabilityId)} at {SiteName(a.SiteId)}); last day of support {a.EndDay}. An ACSA order is support the partner can spare, not a contract it must perform.{tail}", [cause, a.CauseId]);
        }
        if (running.Count == 0)
            AddRecord(day, RecordKind.Event, "acsa", $"{PartnerName} withdrew {what} under the ACSA; no orders were running.", [cause]);
    }

    internal ActionPreview BuildAcsaPreview(AcsaOrderAction o)
    {
        var chk = CheckAcsa(o);
        var queue = CeilHours(_work.Where(w => w.CompletedDay is null).Sum(w => w.HoursRemainingMilli));
        if (chk.Instrument is not { } inst || chk.SiteId is not { } siteId || chk.CapabilityId is not { } capId)
            return new ActionPreview(false, chk.Stops, [], 0, 0, 0, 0, 0, 0, 0, queue);
        int Done(int shopDays)
        {
            var hours = Hours(shopDays, inst.RequiresSystemOrManual);
            var days = _hoursPerDayMilli <= 0 ? 0 : (int)((hours + _hoursPerDayMilli - 1) / _hoursPerDayMilli);
            return Day + Scenario.Actors.Fm.CertificationLagDays + Math.Max(0, days - 1);
        }
        var startMin = Math.Max(o.StartDay ?? 0, Done(inst.ShopDays.Min) + 1 + inst.StartLagDays.Min);
        var startMax = Math.Max(o.StartDay ?? 0, Done(inst.ShopDays.Max) + 1 + inst.StartLagDays.Max);
        var cap = Cap(capId);
        var site = SiteData(siteId);
        var gaps = new List<string> { $"{PartnerName} can withdraw this support when its own needs rise: an ACSA order is not a contract." };
        foreach (var l in cap.Lines.Where(l => l.Kind == "supplies"))
            if (GapIfOff(cap, l) is { } g) gaps.Add(g);
        var peak = (PeakDemandMilli(siteId, capId, Math.Min(startMin, LastDay), Math.Min(startMax + Math.Max(1, o.DurationDays) - 1, LastDay)) + 999) / 1000;
        if (o.Quantity < peak)
            gaps.Add($"{o.Quantity} {Rate(cap)} against peak demand of {peak} {Rate(cap)} at {site.Name}; the difference is a shortfall unless another arrangement covers it.");
        if (chk.Card is { } card && startMin > card.RequestedStartDay)
            gaps.Add($"Earliest start is day {startMin}; the requirement asks for day {card.RequestedStartDay}.");
        if (Partner?.Island is { } island && island != site.Island)
            gaps.Add($"Supplied from {IslandData(island).Name}: deliveries depend on the transport links to {IslandData(site.Island).Name}.");
        return new ActionPreview(chk.Stops.Count == 0, chk.Stops, gaps, chk.DailyCents, 0, chk.TotalCents,
                                 CeilHours(Hours(inst.ShopDays.Min, inst.RequiresSystemOrManual)), CeilHours(Hours(inst.ShopDays.Max, inst.RequiresSystemOrManual)),
                                 startMin, startMax, queue);
    }
}
