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

    internal (List<string> Stops, InstrumentData? Inst, VendorData? Vendor) CheckEstablishBpa(EstablishBpaAction e)
    {
        var stops = new List<string>();
        if (!Rules.Instruments.TryGetValue("bpaEstablish", out var inst)) return (["bpaEstablish is not in the rules"], null, null);
        if (string.IsNullOrWhiteSpace(e.Id)) stops.Add("a BPA needs an id");
        else if (_vehicles.Any(v => v.Id == e.Id) || _work.Any(w => w.CompletedDay is null && w.Payload is EstablishBpaAction p && p.Id == e.Id))
            stops.Add($"id {e.Id} is already used");
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
            $"BPA {e.Id} with {vendor!.Name} started: {CapLabel(e.Capability)} ({lines}); {shopDays} shop day(s) to set up. A BPA is not binding and obligates nothing.", []);
        AddWork(WorkKind.EstablishVehicle, $"Establish BPA {e.Id}", Hours(shopDays, inst.RequiresSystemOrManual), Day, e, rec);
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
            $"BPA {e.Id} with {VendorData(e.Vendor)!.Name} is in place: calls up to {Money(limit)} each, ordering through day {end}.", [causeId]);
    }

    internal ActionPreview BuildEstablishPreview(EstablishBpaAction e)
    {
        var (stops, inst, _) = CheckEstablishBpa(e);
        var queue = CeilHours(_work.Where(w => w.CompletedDay is null).Sum(w => w.HoursRemainingMilli));
        if (inst is null) return new ActionPreview(false, stops, [], 0, 0, 0, 0, 0, 0, 0, queue);
        var hMin = CeilHours(Hours(inst.ShopDays.Min, inst.RequiresSystemOrManual));
        var hMax = CeilHours(Hours(inst.ShopDays.Max, inst.RequiresSystemOrManual));
        int Ready(int hours) => _hoursPerDayMilli <= 0 ? Day : Day + Math.Max(0, (int)((hours * 1000L + _hoursPerDayMilli - 1) / _hoursPerDayMilli) - 1);
        var gaps = new List<string> { "A BPA is not binding: the vendor may decline any call, and the odds rise with market tempo." };
        if (Day < _systemOnlineDay) gaps.Add($"The contract-writing system is offline until day {_systemOnlineDay}: set-up takes longer.");
        // Ready-day range goes in the start fields: the first day calls can be placed.
        return new ActionPreview(stops.Count == 0, stops, gaps, 0, 0, 0, hMin, hMax, Ready(hMin) + 1, Ready(hMax) + 1, queue);
    }
}
