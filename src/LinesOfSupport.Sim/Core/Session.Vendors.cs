using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Vendor knowledge (DESK-05, docs/design/desk-redesign.md D6). One card per vendor the office could buy from. What the
// office has not learned is blank (null), not hidden: the vendor is always on the list with what they sell and where they
// are. Meeting them fills in everything; doing business with them does too (the office has paid them, so it knows how);
// a survey of their island, returned during play, fills in how much they can supply and where they reach. The scenario can
// say the office already knows a vendor (infoTier "observed": everything; "surveyed": supply and reach).
// Knowledge never gates an action: the Sim still awards on the true facts, so no replay changes.
public sealed partial class Session
{
    private readonly HashSet<string> _islandsSurveyedInPlay = new(StringComparer.Ordinal);

    /// <summary>Host nation and ACSA suppliers are not vendors this office buys from (D10), so they get no card.</summary>
    internal bool NotAVendor(VendorData v)
        => v.VehicleOnly && Scenario.InheritedArrangements.Any(a => a.Vendor == v.Id && a.Instrument is AcsaInstrument or HostNationInstrumentId);

    /// <summary>The office has done business with the vendor: an arrangement with them (inherited or awarded, not declined), or
    /// a vehicle with them the shop knows about.</summary>
    private bool DoneBusiness(string vendorId)
        => _arrangements.Any(a => a.VendorId == vendorId && a.Status != ArrangementStatus.Declined)
           || _vehicles.Any(x => x.Vendor == vendorId && x.VisibleOnDay1);

    /// <summary>How they take payment, their prices and lead times: known once met, after doing business, or when the
    /// scenario says the office has seen them work.</summary>
    internal bool VendorTermsKnown(VendorData v)
        => _vendorsMet.ContainsKey(v.Id) || DoneBusiness(v.Id) || v.InfoTier == "observed";

    /// <summary>How much they can supply and which islands they reach: the terms, or a survey of their island.</summary>
    internal bool VendorSupplyKnown(VendorData v)
        => VendorTermsKnown(v) || v.InfoTier == "surveyed" || _islandsSurveyedInPlay.Contains(v.Island);

    /// <summary>Islands the vendor's goods can reach: where they are stocked, and any island a surface link runs to from
    /// there (consumables do not fly in v0). Ordered as the scenario lists islands.</summary>
    internal IReadOnlyList<string> VendorReach(VendorData v)
    {
        var from = v.Offers.Select(o => StockIsland(v, o.Capability)).Append(v.Island).ToHashSet(StringComparer.Ordinal);
        var reach = new HashSet<string>(from, StringComparer.Ordinal);
        foreach (var l in Scenario.TransportLinks.Where(l => l.Mode is not ("aircraft" or "airlift")))
        {
            if (from.Contains(l.From)) reach.Add(l.To);
            if (l.Bidirectional && from.Contains(l.To)) reach.Add(l.From);
        }
        return Scenario.Islands.Where(i => reach.Contains(i.Id)).Select(i => i.Id).ToList();
    }

    private static string PaymentSentence(VendorData v) => (v.AcceptsCash, v.AcceptsGpc) switch
    {
        (true, true) => "They take cash and the card.",
        (true, false) => "They take cash, not the card.",
        (false, true) => "They take the card, not cash.",
        _ => "They take neither cash nor the card; they work on a written contract.",
    };

    private string ReachSentence(VendorData v)
    {
        var names = VendorReach(v).Select(id => IslandData(id).Name).ToList();
        return names.Count == 1 ? $"They deliver on {names[0]} only." : $"They deliver to {AndList(names)}.";
    }

    private static string AndList(IReadOnlyList<string> w)
        => w.Count switch { 0 => "", 1 => w[0], _ => string.Join(", ", w.Take(w.Count - 1)) + $" and {w[^1]}" };

    internal List<VendorCard> BuildVendorCards()
    {
        var cards = new List<VendorCard>();
        foreach (var v in Scenario.Vendors.Where(v => !NotAVendor(v)))
        {
            var terms = VendorTermsKnown(v);
            var supply = VendorSupplyKnown(v);
            var sells = v.Offers.Select(o =>
            {
                var cap = Cap(o.Capability);
                return new VendorCardOffer(o.Capability, cap.Label, cap.Unit,
                    supply ? (int)Math.Floor(o.CapacityPerDay) : null,
                    terms ? UnitPriceFor(v, o) : null,
                    terms ? o.LeadTimeDays : null);
            }).ToList();
            var rep = VendorRep(v.Id);
            var met = VendorMetDay(v.Id);
            var holding = _arrangements.Where(a => a.VendorId == v.Id && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended)
                                       .Select(a => a.Id).ToList();
            var vehicles = _vehicles.Where(x => x.Vendor == v.Id && x.VisibleOnDay1).Select(x => x.Id).ToList();

            string? knownFrom = met is { } md ? $"You met them on day {md}."
                : DoneBusiness(v.Id) ? "This office has done business with them, so their terms are on file."
                : v.InfoTier == "observed" ? "This office has seen their work before."
                : _islandsSurveyedInPlay.Contains(v.Island) ? $"Your survey of {IslandData(v.Island).Name} showed what they can supply and where they reach."
                : v.InfoTier == "surveyed" ? "The handover notes say what they can supply and where they reach."
                : null;

            var missing = new List<string>();
            if (!terms) { missing.Add("how they take payment"); missing.Add("their prices"); }
            if (!supply) { missing.Add("how much they can supply"); missing.Add("where they deliver"); }
            string? still = null;
            if (missing.Count > 0)
            {
                var how = rep is not null
                    ? (VisitInWork("vendor", v.Id) ? $"A meeting with {rep.DisplayName} is set up." : $"Meet {rep.DisplayName} to find out.")
                    : supply ? "Nobody from them has come by; buying from them is how you find out."
                    : $"Nobody from them has come by; a survey of {IslandData(v.Island).Name} shows what they can supply.";
                still = $"Not known yet: {AndList(missing)}. {how}";
            }

            cards.Add(new VendorCard(
                v.Id, v.Name, v.Island, IslandData(v.Island).Name, sells,
                terms ? v.AcceptsCash : null, terms ? v.AcceptsGpc : null,
                supply ? VendorReach(v) : null,
                holding, vehicles, rep, met, VisitInWork("vendor", v.Id),
                _standing.TryGetValue(v.Id, out var st) ? (st.DeclinesWork ? "Declines work after an unpaid handshake." : $"Quotes {st.MarkupPermille / 10}% higher after an unpaid handshake.") : null,
                knownFrom, still));
        }
        return cards;
    }
}
