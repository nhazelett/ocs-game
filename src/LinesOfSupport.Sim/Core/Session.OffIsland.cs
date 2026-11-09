using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Off-island (stateside or regional) sourcing (backlog M3-06, SPEC 4.1 "Stateside or off-island sourcing": airlift queue
// 7 to 14 days by priority, sealift 30 to 45; known vendors, lower unit price, large capacity; transport is a coordination
// that higher-priority cargo can bump; customs on arrival; nothing arrives in week one). An off-island vendor is an ordinary
// vendor with lift modes; its island is the port of entry, and onward delivery to other islands rides the surface links as
// for any vendor. The award names the lift mode. Lift draws come from a "lift" stream only for off-island awards, so games
// without them replay exactly as before.
public sealed partial class Session
{
    private LiftRulesData Lift => Rules.Lift;

    private sealed class Shipment
    {
        public required string Mode { get; init; }
        public required int PlannedStart { get; init; }
        public int Bumps { get; set; }
        public bool CustomsDone { get; set; }
        public bool CustomsHeld { get; set; }
        public List<string> DelayRecords { get; } = [];
    }

    private readonly Dictionary<string, Shipment> _shipments = new(StringComparer.Ordinal);

    /// <summary>Hard stops an award to (or a lift mode on) an off-island vendor adds.</summary>
    private IEnumerable<string> OffIslandStops(AwardAction a, VendorData vendor, string capId)
    {
        if (!vendor.OffIsland)
        {
            if (a.Lift is not null) yield return $"{vendor.Name} is local: a lift mode applies only to off-island vendors";
            yield break;
        }
        if (a.Instrument == "sf44") yield return $"{vendor.Name} is off-island: an SF 44 is a cash buy in person";
        if (a.Lift is null) { yield return $"{vendor.Name} is off-island: the award needs a lift mode ({string.Join(" or ", vendor.Lift)})"; yield break; }
        if (!vendor.Lift.Contains(a.Lift)) yield return $"{vendor.Name} does not ship by {a.Lift}";
        else if (a.Lift == "airlift" && Cap(capId).Delivery == "consumable")
            yield return $"{CapLabel(capId)} does not fly in this build: sealift only (docs/assumptions.md)";
    }

    /// <summary>Days in transit drawn when the award is made: the airlift queue or sealift transit.</summary>
    private int DrawLift(string mode) => _rngLift.NextInt(mode == "airlift" ? Lift.AirliftQueueDays : Lift.SealiftTransitDays);

    /// <summary>Best and worst days the lift adds to the start, bumps and a customs hold included.</summary>
    private (int Min, int Max) LiftRange(string mode)
        => mode == "airlift"
            ? (Lift.AirliftQueueDays.Min, Lift.AirliftQueueDays.Max + Lift.AirliftMaxBumps * Lift.AirliftBumpDelayDays.Max + Lift.CustomsHoldDays.Max)
            : (Lift.SealiftTransitDays.Min, Lift.SealiftTransitDays.Max + Lift.CustomsHoldDays.Max);

    private void RegisterShipment(Arrangement arr, string mode, int day, string causeId)
    {
        _shipments[arr.Id] = new Shipment { Mode = mode, PlannedStart = arr.StartDay };
        var port = IslandData(VendorData(arr.VendorId!)!.Island).Name;
        var bump = mode == "airlift" ? " Higher-priority cargo can bump it." : "";
        AddRecord(day, RecordKind.Event, arr.Id,
            $"{Upper(NameOf(arr.Id))} ships by {mode} to {port}: due about day {arr.StartDay}, then customs on arrival.{bump}", [causeId]);
    }

    /// <summary>On the day an off-island shipment is due: an airlift bump, then one customs roll. Either moves the
    /// period of performance later by its delay (the paid duration is unchanged).</summary>
    private void ArrivalChecks(Arrangement a, Shipment sh, int d)
    {
        void Shift(int days)
        {
            a.StartDay += days;
            a.EndDay += days;
        }
        var port = IslandData(VendorData(a.VendorId!)!.Island).Name;
        if (sh.Mode == "airlift" && sh.Bumps < Lift.AirliftMaxBumps && _rngLift.Roll(Fixed.Permille(Lift.AirliftBumpChance)))
        {
            var delay = _rngLift.NextInt(Lift.AirliftBumpDelayDays);
            sh.Bumps++;
            Shift(delay);
            sh.DelayRecords.Add(AddRecord(d, RecordKind.Event, a.Id,
                $"Airlift cargo for {NameOf(a.Id)} was bumped by higher-priority cargo; next lift puts it at {port} about day {a.StartDay}. Strategic lift is a coordination, not a contract.", [a.CauseId]));
            return;
        }
        if (sh.CustomsDone) return;
        sh.CustomsDone = true;
        if (!_rngLift.Roll(Fixed.Permille(Lift.CustomsHoldChance))) return;
        var hold = _rngLift.NextInt(Lift.CustomsHoldDays);
        sh.CustomsHeld = true;
        Shift(hold);
        sh.DelayRecords.Add(AddRecord(d, RecordKind.Event, a.Id,
            $"Customs at {port} is holding the cargo for {NameOf(a.Id)}; release expected day {a.StartDay}.", [a.CauseId]));
    }

    /// <summary>Debrief: a gap day that falls after an off-island shipment was due and before it actually arrived.</summary>
    private IEnumerable<(string Key, GapFactor F)> LiftDelayFactors(string siteId, string capId, int d)
    {
        foreach (var (id, sh) in _shipments)
        {
            if (sh.DelayRecords.Count == 0) continue;
            var a = _arrangements.First(x => x.Id == id);
            if (a.SiteId != siteId || !Supplies(a, capId) || d < sh.PlannedStart || d >= a.StartDay) continue;
            var why = string.Join(" and ", new[] { sh.Bumps > 0 ? $"bumped from the airlift {Plural(sh.Bumps, "time")}" : null, sh.CustomsHeld ? "held in customs" : null }.Where(x => x is not null));
            yield return ($"lift|{id}", new GapFactor(GapFactorKind.LiftDelay, GapTag.Unforeseeable, d,
                $"Off-island delivery {id} was due day {sh.PlannedStart} and arrived day {a.StartDay}: {why}. Strategic lift and customs are outside the shop's control; a bridge covers the wait.", [.. sh.DelayRecords]));
        }
    }
}
