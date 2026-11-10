using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

/// <summary>
/// What can be picked together in the award editor, and why not (M2R-35, Nick's 9 Nov playthrough: a dropdown should not
/// offer a cash buy from the host nation and then refuse it). Only structural facts about the pair: who the vendor is, how
/// it can be reached, how it takes payment, what the shop holds today. Quantity, price and funding limits stay with the
/// preview, because they depend on numbers the player has not typed yet.
/// </summary>
public sealed partial class Session
{
    internal AwardChoicesView? BuildAwardChoices(string siteId, string capId, string? cardId, string? vendorId, string? instrumentId)
    {
        if (!Scenario.Sites.Any(s => s.Id == siteId) || !_capData.ContainsKey(capId)) return null;
        var card = cardId is null ? null : _cards.FirstOrDefault(c => c.Id == cardId);
        var lines = card?.Lines.Where(kv => kv.Value).Select(kv => kv.Key).ToList()
                    ?? Cap(capId).Lines.Where(l => l.Kind == "core").Select(l => l.Id).ToList();
        var offering = Scenario.Vendors.Where(v => v.Offers.Any(o => o.Capability == capId)).ToList();
        var vendor = vendorId is null ? null : offering.FirstOrDefault(v => v.Id == vendorId);
        var instId = instrumentId is not null && AwardableInM0.Contains(instrumentId) ? instrumentId : null;

        var instruments = Rules.Instruments.Where(kv => AwardableInM0.Contains(kv.Key)).Select(kv =>
        {
            // The vendor's own reason first (it explains every greyed entry at once), then what the shop lacks today,
            // then what this vendor and this instrument lack together.
            var reason = (vendor is null ? null : VendorReason(vendor, siteId, capId, lines))
                         ?? (InstrumentUnavailable(kv.Key) is { } why ? Sentence(why) : null)
                         ?? (vendor is null ? null : PairReason(vendor, kv.Key, siteId, capId, lines));
            return new ChoiceOption(kv.Key, kv.Value.Label, reason is null, reason);
        }).ToList();

        var vendors = offering.Select(v =>
        {
            var reason = VendorReason(v, siteId, capId, lines) ?? (instId is null ? null : PairReason(v, instId, siteId, capId, lines));
            return new ChoiceOption(v.Id, v.Name, reason is null, reason);
        }).ToList();

        var vehicles = _vehicles.Where(v => v.VisibleOnDay1 && offering.Any(o => o.Id == v.Vendor)).Select(v =>
        {
            var reason = VehicleReason(v, vendor, instId, siteId, capId, lines);
            return new ChoiceOption(v.Id, NameOf(v.Id), reason is null, reason);
        }).ToList();

        var immediate = instId is "sf44" or "gpc";
        var funding = _funding.Select(f =>
        {
            string? reason = immediate ? (instId == "sf44" ? "A cash buy is paid from the cash box, not a funding line." : "A card buy is paid on the card, not a funding line.")
                : !f.UsableOn(Day) ? $"{NameOf(f.Id)} is not usable on day {Day}."
                : !f.Pays(capId) ? $"{NameOf(f.Id)} does not pay for {CapLabel(capId).ToLowerInvariant()}."
                : null;
            return new ChoiceOption(f.Id, NameOf(f.Id), reason is null, reason);
        }).ToList();

        return new AwardChoicesView(vendor?.Id, instId, instruments, vendors, vehicles, funding);
    }

    /// <summary>Why this vendor cannot be bought from at all today, whatever the instrument; null when it can.</summary>
    private string? VendorReason(VendorData v, string siteId, string capId, List<string> lines)
    {
        if (v.VehicleOnly && !_vehicles.Any(x => x.Vendor == v.Id && x.VisibleOnDay1))
        {
            var via = Scenario.InheritedArrangements.FirstOrDefault(a => a.Vendor == v.Id)?.Instrument;
            return via == AcsaInstrument
                ? $"{Capitalize(v.Name)} supplies under the ACSA. That is the J4's to arrange, not a purchase by this office."
                : via == HostNationInstrumentId
                    ? HostNationVendorReason(v, siteId, capId)
                    : $"{v.Name} is reached only through an agreement this office does not hold.";
        }
        if (_standing.TryGetValue(v.Id, out var standing) && standing.DeclinesWork)
            return $"{v.Name} declines work from this office after walking off an unpaid handshake.";
        var offer = v.Offers.First(o => o.Capability == capId);
        var missing = lines.Where(l => !offer.Lines.Contains(l)).Select(l => LineLabel(capId, l).ToLowerInvariant()).ToList();
        if (missing.Count > 0) return $"{v.Name} does not offer {JoinWords(missing)}.";
        return null;
    }

    /// <summary>
    /// Why a host nation vendor is not a purchase (M2R-37). Only points at a host nation request when the scenario has an
    /// agreement that covers this capability and it has not been withdrawn; otherwise it says the support came with the
    /// force and this office cannot add to it, so the reason never names a door the player does not have.
    /// </summary>
    private string HostNationVendorReason(VendorData v, string siteId, string capId)
    {
        var covered = Agreement is { } hn && hn.Capacity.TryGetValue(capId, out var capacity) && capacity > 0 && !HnWithdrawn(capId)
                      && (hn.Sites.Count == 0 || hn.Sites.Contains(siteId));
        return covered
            ? $"{v.Name} is asked for with a host nation request under the agreement, not bought with cash, the card or a contract."
            : $"{v.Name} supports the force under an agreement made before you arrived. This office cannot buy from it or add to that support.";
    }

    /// <summary>Why this vendor and this instrument do not go together today; null when they do. Includes the vendor's own reason.</summary>
    private string? PairReason(VendorData v, string instId, string siteId, string capId, List<string> lines)
    {
        if (VendorReason(v, siteId, capId, lines) is { } own) return own;
        switch (instId)
        {
            case "sf44" when !v.AcceptsCash: return $"{v.Name} does not take cash.";
            case "gpc" when !v.AcceptsGpc: return $"{v.Name} does not accept the card.";
            case "bpaCall" or "idiqOrder":
            {
                var kind = instId == "bpaCall" ? "bpa" : "idiq";
                var word = kind == "bpa" ? "BPA" : "IDIQ";
                var held = _vehicles.Where(x => x.Kind == kind && x.VisibleOnDay1 && x.Vendor == v.Id).ToList();
                if (held.Count == 0) return $"This shop holds no {word} with {v.Name}.";
                var reasons = held.Select(x => VehicleReason(x, v, instId, siteId, capId, lines)).ToList();
                return reasons.Any(r => r is null) ? null : reasons[0];
            }
        }
        if (v.VehicleOnly && instId is not ("bpaCall" or "idiqOrder"))
            return $"{v.Name} sells to this office only through its ordering vehicle.";
        return null;
    }

    private string? VehicleReason(VehicleData x, VendorData? vendor, string? instId, string siteId, string capId, List<string> lines)
    {
        var word = x.Kind == "bpa" ? "BPA" : "IDIQ";
        if (instId is not null && instId != (x.Kind == "bpa" ? "bpaCall" : "idiqOrder"))
            return instId is "bpaCall" or "idiqOrder" ? $"{NameOf(x.Id)} is {(x.Kind == "bpa" ? "a BPA" : "an IDIQ")}, not {(instId == "bpaCall" ? "a BPA" : "an IDIQ")}."
                                                      : $"An ordering vehicle is used only for a {word} {(x.Kind == "bpa" ? "call" : "order")}.";
        if (vendor is not null && x.Vendor != vendor.Id) return $"{NameOf(x.Id)} is with {VendorData(x.Vendor)?.Name ?? "a different vendor"}.";
        if (Day > x.OrderingPeriodEndDay) return $"{NameOf(x.Id)}'s ordering period ended on day {x.OrderingPeriodEndDay}.";
        var outside = lines.Where(l => !x.ScopeTags.Contains(l)).Select(l => LineLabel(capId, l).ToLowerInvariant()).ToList();
        if (outside.Count > 0) return $"{Capitalize(JoinWords(outside))} {(outside.Count == 1 ? "is" : "are")} outside {NameOf(x.Id)}'s scope.";
        if (x.PlaceOfPerformance.Count > 0 && !x.PlaceOfPerformance.Contains(SiteData(siteId).Island))
            return $"{SiteName(siteId)} is outside {NameOf(x.Id)}'s place of performance.";
        return null;
    }

    private static string Sentence(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..] + (s.EndsWith('.') ? "" : ".");

    private static string JoinWords(IReadOnlyList<string> w)
        => w.Count switch { 0 => "", 1 => w[0], 2 => $"{w[0]} or {w[1]}", _ => string.Join(", ", w.Take(w.Count - 1)) + $" or {w[^1]}" };
}
