using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// Preparation choices that only bring information (DESK-76, docs/design/desk-redesign.md D32). Before this, pulling the
// existing agreements, bringing vendor lists and confirming partner agreements changed nothing anywhere. Each now does
// the small real thing its words promise, all of it information the Sim already has: the listed vendors' cards start
// filled in; each request names the existing BPAs and IDIQs that could serve it; each request names what the partner
// force and the host nation can spare for it. Knowledge never gates an action: the Sim still awards on the true facts,
// so no replay and no golden changes.
public sealed partial class Session
{
    private bool _vendorList, _agreementsPulled, _partnersConfirmed;

    internal void PrepBringVendorList() => _vendorList = true;
    internal void PrepPullAgreements() => _agreementsPulled = true;
    internal void PrepConfirmPartners() => _partnersConfirmed = true;

    /// <summary>On reach-back's vendor list: a vendor the scenario has some word of ("estimated" or "surveyed"), not one
    /// the office only assumes is there, and not one it already knows.</summary>
    internal static bool OnVendorList(VendorData v) => v.InfoTier is "estimated" or "surveyed";

    private bool ListedTermsKnown(VendorData v) => _vendorList && OnVendorList(v);

    /// <summary>
    /// What is already on file that could serve this request, one sentence each: with pulled agreements, every existing BPA
    /// and IDIQ whose vendor sells it and whose scope covers its parts; with confirmed partner agreements, what the partner
    /// force and the host nation can spare for it. Empty without those preparations.
    /// </summary>
    internal List<string> OnFile(CardState card)
    {
        var said = new List<string>();
        var cap = Cap(card.CapabilityId);
        var what = Lower(cap.Label);
        if (_agreementsPulled)
        {
            var parts = card.Lines.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
            foreach (var x in _vehicles.Where(x => x.Kind is "bpa" or "idiq" && x.VisibleOnDay1 && Day <= x.OrderingPeriodEndDay))
            {
                if (VendorData(x.Vendor) is not { } v || !v.Offers.Any(o => o.Capability == card.CapabilityId)) continue;
                if (!parts.All(l => x.ScopeTags.Contains(l))) continue;
                said.Add(x.Kind == "bpa"
                    ? $"{v.Name} has a BPA this office can call against for {what}{(x.CallLimitCents is { } lim ? $", up to {Money(lim)} a call" : "")}, open until day {x.OrderingPeriodEndDay}."
                    : $"{v.Name} has an IDIQ this office can order {what} under, open until day {x.OrderingPeriodEndDay}.");
            }
        }
        if (_partnersConfirmed)
        {
            if (Partner is { } partner)
                foreach (var c in new[] { card.CapabilityId }.Concat(cap.DependsOn))
                    if (partner.Stock.TryGetValue(c, out var n) && n > 0)
                        said.Add($"Under the ACSA, {PartnerName} can spare up to {Math.Floor(n)} {Rate(Cap(c))} of {Lower(CapLabel(c))}; the J4 asks for it.");
            if (Agreement is { } hn && (hn.Sites.Count == 0 || hn.Sites.Contains(card.SiteId))
                && hn.Capacity.TryGetValue(card.CapabilityId, out var hc) && hc > 0)
                said.Add($"Under the host nation agreement, {HostNationName} can supply up to {Math.Floor(hc)} {Rate(cap)} of {what}.");
        }
        return said;
    }
}
