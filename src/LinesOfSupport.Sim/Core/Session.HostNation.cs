using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Host nation support requests (backlog M3-06, SPEC 4.1 "Host nation support": slow to establish, then cheap; the agreement
// must exist; reliability varies; withdrawable; no negotiation model). The agreement is scenario data (actors.hostNation).
// Requests draw from a "hostNation" stream at the moment they are placed, so games without one replay exactly as before.
// Event effect hostNation.withdraw ends the host nation's support for a capability (or all) after a notice, and it ends
// inherited host-nation arrangements too: support the force arrived with is just as withdrawable as support it asked for.
public sealed partial class Session
{
    private const string HostNationInstrument = "hostNation";
    private readonly HashSet<string> _hnWithdrawn = new(StringComparer.Ordinal);
    private bool _hnWithdrawnAll;

    private sealed record HostNationPayload(HostNationRequestAction Request, string SiteId, string CapabilityId, List<string> Lines,
        long DailyCents, long TotalCents, int StartLag);

    private HostNationData? Agreement => Scenario.Actors.HostNation is { AgreementExists: true } h ? h : null;
    internal string HostNationName => Scenario.Actors.HostNation?.Name ?? "the host nation";

    /// <summary>Capabilities the host nation has withdrawn support for ("*" when it withdrew everything).</summary>
    internal IReadOnlyCollection<string> HostNationWithdrawn => _hnWithdrawnAll ? ["*"] : _hnWithdrawn;
    private bool HnWithdrawn(string capId) => _hnWithdrawnAll || _hnWithdrawn.Contains(capId);

    /// <summary>An arrangement, request or order id already taken by something in play.</summary>
    private bool IdInUse(string id)
        => _arrangements.Any(x => x.Id == id) || _external.Any(x => x.Id == id)
           || _work.Any(w => w.CompletedDay is null && w.Payload switch
           {
               AwardPayload p => p.Action.Id == id,
               AcsaPayload q => q.Order.Id == id,
               HostNationPayload h => h.Request.Id == id,
               _ => false,
           });

    private long HostNationCommittedMilli(string capId, int start, int end)
    {
        bool Overlaps(int s0, int e0) => s0 <= end && e0 >= start;
        // Inherited host-nation support comes through a named utility with its own capacity; only requests made under the
        // agreement share the agreement's capacity.
        return _arrangements.Where(a => a.Instrument == HostNationInstrument && a.CreatedDay > 0 && a.CapabilityId == capId
                                        && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active && Overlaps(a.StartDay, a.EndDay))
                            .Sum(a => a.QuantityMilli)
             + _work.Where(w => w.CompletedDay is null && w.Payload is HostNationPayload p && p.CapabilityId == capId)
                    .Sum(w => ((HostNationPayload)w.Payload!).Request.Quantity * 1000);
    }

    internal sealed record HostNationCheck(List<string> Stops, InstrumentData? Instrument, CardState? Card, string? SiteId,
        string? CapabilityId, List<string> Lines, long DailyCents, long TotalCents);

    /// <summary>Every check a host nation support request must pass, with its cost. Reads state only; draws no random numbers.</summary>
    internal HostNationCheck CheckHostNation(HostNationRequestAction o)
    {
        var stops = new List<string>();
        HostNationCheck Fail(string why) => new([.. stops, why], null, null, null, null, [], 0, 0);
        if (!Rules.Instruments.TryGetValue(HostNationInstrument, out var inst)) return Fail("hostNation is not in the rules");
        if (string.IsNullOrWhiteSpace(o.Id)) stops.Add("a request needs an id");
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
        if (siteId is null || capId is null) return Fail("a request needs a card, or a site and a capability");
        if (!Scenario.Sites.Any(s => s.Id == siteId)) return Fail($"site {siteId} does not exist");
        if (!_capData.ContainsKey(capId)) return Fail($"capability {capId} does not exist");
        if (Agreement is not { } hn) return Fail("no host nation support agreement exists in this scenario");
        if (!hn.Capacity.TryGetValue(capId, out var capacity) || capacity <= 0)
            return Fail($"the host nation support agreement does not cover {CapLabel(capId).ToLowerInvariant()}");
        if (hn.Sites.Count > 0 && !hn.Sites.Contains(siteId))
            stops.Add($"the host nation support agreement does not cover {SiteName(siteId)}");
        if (HnWithdrawn(capId)) stops.Add($"{hn.Name} has withdrawn its {CapLabel(capId).ToLowerInvariant()} support");
        if (o.Quantity <= 0) stops.Add("quantity must be positive");
        if (o.DurationDays <= 0) stops.Add("duration must be positive");
        var start = Math.Max(o.StartDay ?? Day, Day);
        var committed = HostNationCommittedMilli(capId, start, start + Math.Max(1, o.DurationDays) - 1);
        if (o.Quantity * 1000 + committed > Fixed.Milli(capacity))
            stops.Add($"the agreement provides {capacity:0} {Cap(capId).Unit}/day of {CapLabel(capId).ToLowerInvariant()} and {committed / 1000} is already requested");
        var lines = Cap(capId).Lines.Where(l => l.Kind != "supplies").Select(l => l.Id).ToList();
        var daily = o.Quantity * hn.UnitPriceCents.GetValueOrDefault(capId);
        var total = daily * Math.Max(0, o.DurationDays);
        // With no cost share there is nothing to obligate, so no funding line is needed.
        if (total > 0 || !string.IsNullOrEmpty(o.FundingLine))
        {
            var line = _funding.FirstOrDefault(f => f.Id == o.FundingLine);
            if (line is null) stops.Add("the shop's cost share needs a funding line");
            else
            {
                if (!line.UsableOn(Day)) stops.Add($"{NameOf(line.Id)} is not usable on day {Day}");
                else if (line.RemainingCents < total) stops.Add($"{NameOf(line.Id)} has {Money(line.RemainingCents)} left for {Money(total)}");
                stops.AddRange(FundingStops(line, capId, o.StartDay, o.DurationDays));
            }
        }
        return new HostNationCheck(stops, inst, card, siteId, capId, lines, daily, total);
    }

    private ActionResult ApplyHostNationRequest(HostNationRequestAction o)
    {
        var chk = CheckHostNation(o);
        if (chk.Stops.Count > 0) return ActionResult.Stop([.. chk.Stops]);
        var inst = chk.Instrument!;
        var shopDays = _rngHostNation.NextInt(inst.ShopDays);
        var lag = _rngHostNation.NextInt(inst.StartLagDays);
        var capId = chk.CapabilityId!;
        var cost = chk.TotalCents > 0 ? $"cost share {Money(chk.TotalCents)} from {NameOf(o.FundingLine)}" : "no cost to the shop";
        var rec = AddRecord(Day, RecordKind.Action, o.Id,
            $"Host nation support request to {HostNationName} started: {CapLabel(capId)} at {SiteName(chk.SiteId!)}, {o.Quantity} {Cap(capId).Unit}/day for {o.DurationDays} days; {cost}; {shopDays} shop day(s).", []);
        // A request with no cost share does not wait for FM to certify funds.
        var lagFm = chk.TotalCents > 0 ? Scenario.Actors.Fm.CertificationLagDays : 0;
        AddWork(WorkKind.HostNationRequest, $"Host nation support request for {Lower(CapLabel(capId))} at {SiteName(chk.SiteId!)}", Hours(shopDays, inst.RequiresSystemOrManual), Day + lagFm,
                new HostNationPayload(o, chk.SiteId!, capId, chk.Lines, chk.DailyCents, chk.TotalCents, lag), rec);
        return ActionResult.Ok("in work");
    }

    private void CompleteHostNationRequest(HostNationPayload p, int day, string causeId)
    {
        var o = p.Request;
        var line = p.TotalCents > 0 ? _funding.First(f => f.Id == o.FundingLine) : null;
        string? refusal = HnWithdrawn(p.CapabilityId) ? $"{HostNationName} withdrew its {CapLabel(p.CapabilityId).ToLowerInvariant()} support before the request was placed"
            : line is not null && (!line.UsableOn(day) || line.RemainingCents < p.TotalCents) ? FundingShortReason(line, p.TotalCents, day) : null;
        if (refusal is not null)
        {
            var rej = AddRecord(day, RecordKind.Rejected, o.Id, $"Host nation support request for {Lower(CapLabel(p.CapabilityId))} at {SiteName(p.SiteId)} could not be placed on day {day}: {refusal}.", [causeId]);
            if (!HnWithdrawn(p.CapabilityId)) _fundingRejections.Add((o.Id, p.SiteId, p.CapabilityId, day, rej));
            return;
        }
        if (line is not null) line.ObligatedCents += p.TotalCents;
        var earliest = day + 1 + p.StartLag;
        var start = Math.Max(o.StartDay ?? earliest, earliest);
        var hn = Agreement!;
        _arrangements.Add(new Arrangement
        {
            Id = o.Id, Instrument = HostNationInstrument, SiteId = p.SiteId, CapabilityId = p.CapabilityId, Lines = p.Lines,
            SupplierIslandId = hn.Island ?? SiteData(p.SiteId).Island, QuantityMilli = o.Quantity * 1000, DailyCostCents = p.DailyCents,
            ObligatedCents = p.TotalCents, FundingLineId = line?.Id, ReliabilityPermille = Fixed.Permille(hn.Reliability ?? 0.9),
            CreatedDay = day, CardId = o.Card, CauseId = causeId, StartDay = start, EndDay = start + o.DurationDays - 1,
        });
        var money = line is null ? "No cost to the shop." : $"Cost share of {Money(p.TotalCents)} obligated from {NameOf(line.Id)}.";
        AddRecord(day, RecordKind.WorkDone, o.Id,
            $"Host nation support request for {Lower(CapLabel(p.CapabilityId))} at {SiteName(p.SiteId)} accepted by {hn.Name}; support starts day {start}, ends day {start + o.DurationDays - 1}. {money} The host nation can withdraw it.", [causeId]);
    }

    /// <summary>Event effect hostNation.withdraw: the host nation ends its support for a capability (null: everything) after a
    /// notice. noticeDays below zero means the event gave none, so the notice comes from rules hostNation.withdrawalNoticeDays.</summary>
    internal void WithdrawHostNation(string? capId, int day, int noticeDays, string cause)
    {
        if (noticeDays < 0)
            noticeDays = Rules.Instruments.TryGetValue(HostNationInstrument, out var inst) && inst.WithdrawalNoticeDays is { } r ? _rngHostNation.NextInt(r) : 0;
        if (capId is null) _hnWithdrawnAll = true; else _hnWithdrawn.Add(capId);
        var what = capId is null ? "all its support" : $"its {CapLabel(capId).ToLowerInvariant()} support";
        var running = _arrangements.Where(a => a.Instrument == HostNationInstrument && (capId is null || a.CapabilityId == capId)
                                               && a.Status is ArrangementStatus.Active or ArrangementStatus.Pending or ArrangementStatus.Suspended).ToList();
        foreach (var a in running)
        {
            a.EndDay = Math.Min(a.EndDay, day + noticeDays - 1);
            if (a.Status == ArrangementStatus.Pending && a.EndDay < a.StartDay) a.Status = ArrangementStatus.Ended;
            // Unperformed days carry no cost share: the unspent share returns to a line that is still current.
            var performed = Math.Max(0, a.EndDay - a.StartDay + 1);
            var back = Math.Max(0, a.ObligatedCents - a.DailyCostCents * performed);
            var tail = "";
            if (back > 0 && _funding.FirstOrDefault(f => f.Id == a.FundingLineId) is { } fl)
            {
                a.ObligatedCents -= back;
                if (fl.UsableOn(day)) { fl.ObligatedCents -= back; tail = $" {Money(back)} of cost share returns to {NameOf(fl.Id)}."; }
                else tail = $" {Money(back)} of cost share is released; {NameOf(fl.Id)} has expired, so it lapses.";
            }
            var whose = a.CreatedDay > 0 ? $"host nation support for {Lower(CapLabel(a.CapabilityId))} at {SiteName(a.SiteId)}" : $"inherited host nation support for {Lower(CapLabel(a.CapabilityId))} at {SiteName(a.SiteId)}";
            AddRecord(day, RecordKind.Withdrawal, a.Id,
                $"{HostNationName} withdrew {whose} ({CapLabel(a.CapabilityId)} at {SiteName(a.SiteId)}); last day of support {a.EndDay}. Host nation support rests on an agreement the host nation can withdraw, not a contract it must perform.{tail}", [cause, a.CauseId]);
        }
        if (running.Count == 0)
            AddRecord(day, RecordKind.Event, "hostNation", $"{HostNationName} withdrew {what} under the host nation support agreement; none was running.", [cause]);
    }

    internal ActionPreview BuildHostNationPreview(HostNationRequestAction o)
    {
        var chk = CheckHostNation(o);
        var queue = CeilHours(_work.Where(w => w.CompletedDay is null).Sum(w => w.HoursRemainingMilli));
        if (chk.Instrument is not { } inst || chk.SiteId is not { } siteId || chk.CapabilityId is not { } capId)
            return new ActionPreview(false, chk.Stops, [], 0, 0, 0, 0, 0, 0, 0, queue);
        var lagFm = chk.TotalCents > 0 ? Scenario.Actors.Fm.CertificationLagDays : 0;
        int Done(int shopDays)
        {
            var hours = Hours(shopDays, inst.RequiresSystemOrManual);
            var days = _hoursPerDayMilli <= 0 ? 0 : (int)((hours + _hoursPerDayMilli - 1) / _hoursPerDayMilli);
            return Day + lagFm + Math.Max(0, days - 1);
        }
        var startMin = Math.Max(o.StartDay ?? 0, Done(inst.ShopDays.Min) + 1 + inst.StartLagDays.Min);
        var startMax = Math.Max(o.StartDay ?? 0, Done(inst.ShopDays.Max) + 1 + inst.StartLagDays.Max);
        var cap = Cap(capId);
        var site = SiteData(siteId);
        var gaps = new List<string> { $"{HostNationName} can withdraw this support: it rests on an agreement, not a contract." };
        foreach (var l in cap.Lines.Where(l => l.Kind == "supplies"))
            if (GapIfOff(cap, l) is { } g) gaps.Add(g);
        var peak = (PeakDemandMilli(siteId, capId, Math.Min(startMin, LastDay), Math.Min(startMax + Math.Max(1, o.DurationDays) - 1, LastDay)) + 999) / 1000;
        if (o.Quantity < peak)
            gaps.Add($"{o.Quantity} {Rate(cap)} against peak demand of {peak} {Rate(cap)} at {site.Name}; the difference is a shortfall unless another arrangement covers it.");
        if (chk.Card is { } card && startMin > card.RequestedStartDay)
            gaps.Add($"Earliest start is day {startMin}; the requirement asks for day {card.RequestedStartDay}.");
        if (Agreement?.Island is { } island && island != site.Island)
            gaps.Add($"Supplied from {IslandData(island).Name}: deliveries depend on the transport links to {IslandData(site.Island).Name}.");
        return new ActionPreview(chk.Stops.Count == 0, chk.Stops, gaps, chk.DailyCents, 0, chk.TotalCents,
                                 CeilHours(Hours(inst.ShopDays.Min, inst.RequiresSystemOrManual)), CeilHours(Hours(inst.ShopDays.Max, inst.RequiresSystemOrManual)),
                                 startMin, startMax, queue);
    }
}
