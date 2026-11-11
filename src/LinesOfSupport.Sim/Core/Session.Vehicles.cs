using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Establishing a BPA (backlog M2-12, SPEC 4.1 instrument table). The scenario's vehicles are copied into the
// session at creation; established BPAs are added when their set-up work finishes.
public sealed partial class Session
{
    private List<VehicleData>? _vehicleList;
    private List<VehicleData> _vehicles => _vehicleList ??= [.. Scenario.Vehicles];
    public IReadOnlyList<VehicleData> Vehicles => _vehicles;

    /// <summary>The vehicle id a piece of set-up work will create, if it is one.</summary>
    private static string? EstablishingId(object? payload) => payload switch
    {
        EstablishBpaAction b => b.Id, EstablishIdiqAction i => i.Id, _ => null,
    };

    internal (List<string> Stops, InstrumentData? Inst, VendorData? Vendor) CheckEstablishBpa(EstablishBpaAction e)
    {
        var stops = new List<string>();
        if (!Rules.Instruments.TryGetValue("bpaEstablish", out var inst)) return (["bpaEstablish is not in the rules"], null, null);
        if (string.IsNullOrWhiteSpace(e.Id)) stops.Add("a BPA needs an id");
        else if (_vehicles.Any(v => v.Id == e.Id) || _work.Any(w => w.CompletedDay is null && EstablishingId(w.Payload) == e.Id))
            stops.Add($"{Upper(NameOf(e.Id))} already exists");
        var vendor = VendorData(e.Vendor);
        if (vendor is null) return ([.. stops, $"vendor {e.Vendor} does not exist"], inst, null);
        if (!_capData.ContainsKey(e.Capability)) return ([.. stops, $"capability {e.Capability} does not exist"], inst, vendor);
        var offer = vendor.Offers.FirstOrDefault(o => o.Capability == e.Capability);
        if (offer is null) stops.Add($"{vendor.Name} does not offer {CapLabel(e.Capability)}");
        if (e.ScopeLines.Count == 0) stops.Add("a BPA needs at least one scope line");
        foreach (var l in e.ScopeLines)
        {
            if (!Cap(e.Capability).Lines.Any(x => x.Id == l)) stops.Add($"{l} is not a {e.Capability} line");
            else if (offer is not null && !offer.Lines.Contains(l)) stops.Add($"{vendor.Name} does not offer {LineLabel(e.Capability, l)}");
        }
        if (vendor.VehicleOnly) stops.Add($"{vendor.Name} is reachable only through its existing vehicle");
        if (_standing.TryGetValue(vendor.Id, out var st) && st.DeclinesWork)
            stops.Add($"{vendor.Name} declines work from this office after walking off an unpaid handshake");
        if (e.CallLimitCents is <= 0) stops.Add("call limit must be positive");
        if (e.CallLimitCents is { } lim && lim > Rules.Thresholds.SimplifiedAcquisitionCents)
            stops.Add($"a call limit of {Money(lim)} exceeds the simplified acquisition threshold of {Money(Rules.Thresholds.SimplifiedAcquisitionCents)}");
        if (e.OrderingPeriodDays is <= 0) stops.Add("ordering period must be positive");
        return (stops, inst, vendor);
    }

    private ActionResult ApplyEstablishBpa(EstablishBpaAction e)
    {
        var (stops, inst, vendor) = CheckEstablishBpa(e);
        if (stops.Count > 0) return ActionResult.Stop([.. stops]);
        var shopDays = _rngWork.NextInt(inst!.ShopDays);
        var lines = string.Join(", ", e.ScopeLines.Select(l => LineLabel(e.Capability, l)));
        var rec = AddRecord(Day, RecordKind.Action, e.Id,
            $"BPA with {vendor!.Name} started: {CapLabel(e.Capability)} ({lines}); {Plural(shopDays, "shop day")} to set up. A BPA is not binding and obligates nothing.", []);
        AddWork(WorkKind.EstablishVehicle, $"Establish a BPA with {VendorData(e.Vendor)?.Name}", Hours(shopDays, inst.RequiresSystemOrManual), Day, e, rec);
        return ActionResult.Ok("in work");
    }

    private void CompleteEstablishBpa(EstablishBpaAction e, int day, string causeId)
    {
        var end = Math.Min(LastDay, day + (e.OrderingPeriodDays ?? LastDay));
        var limit = e.CallLimitCents ?? Rules.Thresholds.SimplifiedAcquisitionCents;
        _vehicles.Add(new VehicleData
        {
            Id = e.Id, Kind = "bpa", Vendor = e.Vendor, ScopeTags = [.. e.ScopeLines], CallLimitCents = limit,
            OrderingPeriodEndDay = end, PlaceOfPerformance = [], VisibleOnDay1 = true,
        });
        AddRecord(day, RecordKind.WorkDone, e.Id,
            $"BPA with {VendorData(e.Vendor)!.Name} is in place: calls up to {Money(limit)} each, ordering through day {end}.", [causeId]);
    }

    internal ActionPreview BuildEstablishPreview(EstablishBpaAction e)
    {
        var (stops, inst, _) = CheckEstablishBpa(e);
        var queue = QueueHours(e.Assignee);
        if (inst is null) return new ActionPreview(false, stops, [], 0, 0, 0, 0, 0, 0, 0, queue);
        var hMin = CeilHours(PersonHours(Hours(inst.ShopDays.Min, inst.RequiresSystemOrManual), e.Assignee));
        var hMax = CeilHours(PersonHours(Hours(inst.ShopDays.Max, inst.RequiresSystemOrManual), e.Assignee));
        int Ready(int hours) => Day + Math.Max(0, WorkDays(hours * 1000L, e.Assignee) - 1);
        var gaps = new List<string> { "A BPA is not binding: the vendor may decline any call, and the odds rise with market tempo." };
        if (Day < _systemOnlineDay) gaps.Add($"The contract-writing system is offline until day {_systemOnlineDay}: set-up takes longer.");
        // Ready-day range goes in the start fields: the first day calls can be placed.
        return new ActionPreview(stops.Count == 0, stops, gaps, 0, 0, 0, hMin, hMax, Ready(hMin) + 1, Ready(hMax) + 1, queue);
    }
}

// Establishing an IDIQ and ordering from it (backlog M3-06, SPEC 4.1 "Establish an IDIQ" and "IDIQ order").
// Single award only: fair opportunity among multiple awardees is not modeled yet (docs/assumptions.md).
public sealed partial class Session
{
    private List<IdiqState>? _idiqList;
    private List<IdiqState> _idiqs => _idiqList ??= [.. Scenario.Vehicles.Where(v => v.Kind == "idiq").Select(v => new IdiqState
    {
        VehicleId = v.Id, CeilingCents = v.CeilingCents, OrderedCents = v.ObligatedCents,
    })];
    public IReadOnlyList<IdiqState> Idiqs => _idiqs;
    internal IdiqState? Idiq(string? vehicleId) => _idiqs.FirstOrDefault(i => i.VehicleId == vehicleId);

    /// <summary>Order value on IDIQ orders still in shop work against this vehicle (not yet counted in OrderedCents).</summary>
    private long PendingOrderCents(string vehicleId)
        => _work.Where(w => w.CompletedDay is null && w.Payload is AwardPayload p && p.Action.Instrument == "idiqOrder" && p.Action.Vehicle == vehicleId)
                .Sum(w => ((AwardPayload)w.Payload!).TotalCents);

    internal long MinimumGuaranteeFor(EstablishIdiqAction e, InstrumentData inst)
    {
        var floor = Fixed.MulPermille(Math.Max(0, e.CeilingCents), Fixed.Permille(inst.ObligatesMinimumGuaranteeFraction ?? 0));
        return e.MinimumGuaranteeCents ?? floor;
    }

    internal (List<string> Stops, InstrumentData? Inst, VendorData? Vendor) CheckEstablishIdiq(EstablishIdiqAction e)
    {
        var stops = new List<string>();
        if (!Rules.Instruments.TryGetValue("idiqEstablish", out var inst)) return (["idiqEstablish is not in the rules"], null, null);
        if (string.IsNullOrWhiteSpace(e.Id)) stops.Add("an IDIQ needs an id");
        else if (_vehicles.Any(v => v.Id == e.Id) || _work.Any(w => w.CompletedDay is null && EstablishingId(w.Payload) == e.Id))
            stops.Add($"{Upper(NameOf(e.Id))} already exists");
        var vendor = VendorData(e.Vendor);
        if (vendor is null) return ([.. stops, $"vendor {e.Vendor} does not exist"], inst, null);
        if (!_capData.ContainsKey(e.Capability)) return ([.. stops, $"capability {e.Capability} does not exist"], inst, vendor);
        var offer = vendor.Offers.FirstOrDefault(o => o.Capability == e.Capability);
        if (offer is null) stops.Add($"{vendor.Name} does not offer {CapLabel(e.Capability)}");
        if (e.ScopeLines.Count == 0) stops.Add("an IDIQ needs at least one scope line");
        foreach (var l in e.ScopeLines)
        {
            if (!Cap(e.Capability).Lines.Any(x => x.Id == l)) stops.Add($"{l} is not a {e.Capability} line");
            else if (offer is not null && !offer.Lines.Contains(l)) stops.Add($"{vendor.Name} does not offer {LineLabel(e.Capability, l)}");
        }
        if (vendor.VehicleOnly) stops.Add($"{vendor.Name} is reachable only through its existing vehicle");
        if (_standing.TryGetValue(vendor.Id, out var st) && st.DeclinesWork)
            stops.Add($"{vendor.Name} declines work from this office after walking off an unpaid handshake");
        if (e.CeilingCents <= 0) stops.Add("an IDIQ needs a ceiling");
        if (e.OrderingPeriodDays is <= 0) stops.Add("ordering period must be positive");
        if (_warrantCents is { } warrant && e.CeilingCents > warrant) stops.Add($"a ceiling of {Money(e.CeilingCents)} exceeds the warrant of {Money(warrant)}");
        var floor = MinimumGuaranteeFor(e with { MinimumGuaranteeCents = null }, inst);
        var min = MinimumGuaranteeFor(e, inst);
        if (e.CeilingCents > 0)
        {
            if (min < floor || min <= 0)
                stops.Add($"a minimum guarantee of {Money(min)} is nominal; this office requires at least {Money(Math.Max(floor, 1))} ({Fixed.Permille(inst.ObligatesMinimumGuaranteeFraction ?? 0) / 10m:0.#}% of the ceiling)");
            if (min > e.CeilingCents) stops.Add("the minimum guarantee cannot exceed the ceiling");
        }
        var line = _funding.FirstOrDefault(f => f.Id == e.FundingLine);
        if (line is null) stops.Add("an IDIQ needs a funding line for its minimum guarantee");
        else if (!line.UsableOn(Day)) stops.Add($"{NameOf(line.Id)} is not usable on day {Day}");
        else if (!line.Pays(e.Capability)) stops.AddRange(FundingStops(line, e.Capability, null, 0));
        else if (line.RemainingCents < min) stops.Add($"{NameOf(line.Id)} has {Money(line.RemainingCents)} left for a minimum guarantee of {Money(min)}");
        return (stops, inst, vendor);
    }

    private ActionResult ApplyEstablishIdiq(EstablishIdiqAction e)
    {
        var (stops, inst, vendor) = CheckEstablishIdiq(e);
        if (stops.Count > 0) return ActionResult.Stop([.. stops]);
        var shopDays = _rngWork.NextInt(inst!.ShopDays);
        var lines = string.Join(", ", e.ScopeLines.Select(l => LineLabel(e.Capability, l)));
        var min = MinimumGuaranteeFor(e, inst);
        var rec = AddRecord(Day, RecordKind.Action, e.Id,
            $"IDIQ with {vendor!.Name} started: {CapLabel(e.Capability)} ({lines}), ceiling {Money(e.CeilingCents)}, minimum guarantee {Money(min)} from {NameOf(e.FundingLine)}; {Plural(shopDays, "shop day")} to award.", []);
        AddWork(WorkKind.EstablishVehicle, $"Establish an IDIQ with {VendorData(e.Vendor)?.Name}", Hours(shopDays, inst.RequiresSystemOrManual), Day, e, rec);
        return ActionResult.Ok("in work");
    }

    private void CompleteEstablishIdiq(EstablishIdiqAction e, int day, string causeId)
    {
        var inst = Rules.Instruments["idiqEstablish"];
        var min = MinimumGuaranteeFor(e, inst);
        var line = _funding.First(f => f.Id == e.FundingLine);
        var vendor = VendorData(e.Vendor)!;
        if (!line.UsableOn(day) || line.RemainingCents < min)
        {
            AddRecord(day, RecordKind.Rejected, e.Id, $"IDIQ with {VendorData(e.Vendor)?.Name} could not be awarded: {NameOf(line.Id)} could not cover the minimum guarantee of {Money(min)} on day {day}.", [causeId]);
            return;
        }
        line.ObligatedCents += min;
        var end = Math.Min(LastDay, day + (e.OrderingPeriodDays ?? LastDay));
        _vehicles.Add(new VehicleData
        {
            Id = e.Id, Kind = "idiq", Vendor = e.Vendor, ScopeTags = [.. e.ScopeLines], CeilingCents = e.CeilingCents, ObligatedCents = min,
            OrderingPeriodEndDay = end, PlaceOfPerformance = [], VisibleOnDay1 = true,
        });
        _idiqs.Add(new IdiqState { VehicleId = e.Id, FundingLineId = line.Id, CeilingCents = e.CeilingCents, MinimumGuaranteeCents = min });
        AddRecord(day, RecordKind.WorkDone, e.Id,
            $"IDIQ awarded to {vendor.Name}: orders up to a ceiling of {Money(e.CeilingCents)} through day {end}. Minimum guarantee of {Money(min)} obligated from {NameOf(line.Id)}; it is owed whether or not it is ordered.", [causeId]);
    }

    internal ActionPreview BuildEstablishIdiqPreview(EstablishIdiqAction e)
    {
        var (stops, inst, vendor) = CheckEstablishIdiq(e);
        var queue = QueueHours(e.Assignee);
        if (inst is null) return new ActionPreview(false, stops, [], 0, 0, 0, 0, 0, 0, 0, queue);
        var min = MinimumGuaranteeFor(e, inst);
        var hMin = CeilHours(PersonHours(Hours(inst.ShopDays.Min, inst.RequiresSystemOrManual), e.Assignee));
        var hMax = CeilHours(PersonHours(Hours(inst.ShopDays.Max, inst.RequiresSystemOrManual), e.Assignee));
        int Ready(int hours) => Day + Math.Max(0, WorkDays(hours * 1000L, e.Assignee) - 1);
        var gaps = new List<string> { $"The minimum guarantee of {Money(min)} is obligated at award and owed to the vendor whether or not it is ordered." };
        if (vendor?.Offers.FirstOrDefault(o => o.Capability == e.Capability) is { } offer)
            gaps.Add($"Binding, but {vendor.Name}'s capacity is still about {offer.CapacityPerDay:0} {Cap(e.Capability).Unit}/day in total: orders above it are accepted and delivered short.");
        if (Day < _systemOnlineDay) gaps.Add($"The contract-writing system is offline until day {_systemOnlineDay}: set-up takes longer.");
        // Ready-day range goes in the start fields: the first day orders can be placed. TotalCents is the guarantee obligated.
        return new ActionPreview(stops.Count == 0, stops, gaps, 0, 0, min, hMin, hMax, Ready(hMin) + 1, Ready(hMax) + 1, queue);
    }
}

// The capacity event (SPEC 4.1: "the vendor's real capacity is still finite, so demand above it produces a capacity
// event even with a perfect contract"). Every other instrument stops at the vendor's capacity when it is awarded, so
// only IDIQ orders can push a vendor over it; vendors with no IDIQ order running are never checked, and nothing here
// draws a random number, so games without IDIQ orders replay exactly as before.
public sealed partial class Session
{
    private readonly HashSet<(string ArrangementId, int Day)> _capacityShortDays = [];
    private readonly Dictionary<string, string> _capacityEpisode = new(StringComparer.Ordinal);

    /// <summary>Days an IDIQ order delivered short because its vendor was over capacity (debrief evidence).</summary>
    internal bool CapacityShort(string arrangementId, int day) => _capacityShortDays.Contains((arrangementId, day));

    private void CapacityLimits(int d, Dictionary<string, Today> today)
    {
        var groups = _arrangements.Where(a => a.Instrument == "idiqOrder" && a.VendorId is not null && today.ContainsKey(a.Id))
                                  .Select(a => (Vendor: a.VendorId!, Cap: a.CapabilityId)).Distinct().ToList();
        foreach (var (vendorId, capId) in groups)
        {
            var vendor = VendorData(vendorId)!;
            var offer = vendor.Offers.FirstOrDefault(o => o.Capability == capId);
            if (offer is null) continue;
            var capacity = Fixed.Milli(offer.CapacityPerDay);
            var running = _arrangements.Where(a => a.VendorId == vendorId && a.CapabilityId == capId
                                                   && today.TryGetValue(a.Id, out var t) && !t.Suspended).ToList();
            var load = running.Sum(a => a.QuantityMilli);
            // Arrangements under other instruments were inside capacity when awarded; IDIQ orders take what is left,
            // oldest order first.
            var left = Math.Max(0, capacity - running.Where(a => a.Instrument != "idiqOrder").Sum(a => a.QuantityMilli));
            foreach (var a in running.Where(a => a.Instrument == "idiqOrder").OrderBy(a => a.CreatedDay).ThenBy(a => a.Id, StringComparer.Ordinal))
            {
                var give = Math.Min(a.QuantityMilli, left);
                left -= give;
                if (give >= a.QuantityMilli) { _capacityEpisode.Remove(a.Id); continue; }
                var t = today[a.Id];
                t.FactorPermille = t.FactorPermille * Fixed.Ratio(give, a.QuantityMilli) / 1000;
                _capacityShortDays.Add((a.Id, d));
                if (!_capacityEpisode.TryGetValue(a.Id, out var rec))
                {
                    var unit = Cap(capId).Unit;
                    rec = AddRecord(d, RecordKind.VendorCapacity, a.Id,
                        $"Capacity event: {vendor.Name} can supply about {offer.CapacityPerDay:0} {unit}/day of {CapLabel(capId).ToLowerInvariant()} and has {load / 1000} {unit}/day on order. "
                        + $"{Upper(NameOf(a.Id))} gets {give / 1000} of its {a.QuantityMilli / 1000} {unit}/day. A binding order does not add capacity: a second source or a supplementary buy covers the rest.",
                        [a.CauseId]);
                    _capacityEpisode[a.Id] = rec;
                }
                t.CapacityRecord = rec;
            }
        }
    }
}
