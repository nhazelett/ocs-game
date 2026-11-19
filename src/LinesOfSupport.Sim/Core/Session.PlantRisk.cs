using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// Goods delivered carry no plant risk (DESK-59, docs/design/desk-redesign.md D26). Operators and maintenance are the
// parts of a service that runs at the site: a plant, a generator, a forklift. A consumable (water, fuel, food) bought
// from a vendor whose offer leaves a part out is goods, and the vendor's own plant is the vendor's business, so that
// part's failure risk and warning do not apply. Continuous capabilities keep both whatever the vendor offers. Delivery
// misses stay the vendor's reliability. To strip it: this file and the lines marked DESK-59.
public sealed partial class Session
{
    /// <summary>True when leaving this operator or maintenance part out of a buy from this vendor raises its failure odds.</summary>
    internal bool PlantPartAtRisk(CapabilityData cap, CapabilityLineData line, string? vendorId)
    {
        if (line.Kind is not ("operator" or "maintenance")) return false;
        if (cap.Delivery != "consumable") return true;
        var offer = vendorId is null ? null
            : Scenario.Vendors.FirstOrDefault(v => v.Id == vendorId)?.Offers.FirstOrDefault(o => o.Capability == cap.Id);
        // Not a vendor's offer (host nation, a partner, another unit): unchanged.
        if (offer is null) return true;
        return offer.Lines.Any(id => cap.Lines.FirstOrDefault(l => l.Id == id)?.Kind == line.Kind);
    }

    /// <summary>The operator and maintenance parts this arrangement left out that carry their failure risk.</summary>
    internal List<CapabilityLineData> MissingPlantParts(Arrangement a, CapabilityData cap)
        => cap.Lines.Where(l => l.Kind is "operator" or "maintenance"
                                && !a.Lines.Any(id => cap.Lines.FirstOrDefault(x => x.Id == id)?.Kind == l.Kind)
                                && PlantPartAtRisk(cap, l, a.VendorId)).ToList();

    /// <summary>
    /// The debrief's opening for a failure with no part left out that counts: full coverage, or goods whose plant is the
    /// vendor's business (DESK-59).
    /// </summary>
    internal string FailedCovered(Arrangement a, CapabilityData cap, int day)
    {
        var goods = !a.IsCoordination && cap.Lines.Any(l => l.Kind is "operator" or "maintenance"
                                       && !a.Lines.Any(id => cap.Lines.FirstOrDefault(x => x.Id == id)?.Kind == l.Kind)
                                       && !PlantPartAtRisk(cap, l, a.VendorId));
        return goods
            ? $"{Upper(NameOf(a.Id))} missed a delivery on day {day}. It delivers goods, so the miss is its own delivery record, not a part left out."
            : PlantKinds(cap) is { Count: > 0 } kinds
                ? $"{Upper(NameOf(a.Id))} failed on day {day} with full {string.Join(" and ", kinds)} coverage."
                // DESK-112 (Claude play 19 Nov, note 28): name only the parts the capability has; waste removal has none.
                : $"{Upper(NameOf(a.Id))} failed on day {day}; no part was left out of its terms.";
    }

    /// <summary>The plant parts this capability has, operator before maintenance (DESK-112).</summary>
    private static List<string> PlantKinds(CapabilityData cap)
        => new[] { "operator", "maintenance" }.Where(k => cap.Lines.Any(l => l.Kind == k)).ToList();
}
