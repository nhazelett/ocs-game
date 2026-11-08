using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Policies;

/// <summary>
/// A scripted player for balance runs. Policies see only what a player could see (SessionView) and act through
/// the same actions. They are yardsticks, not opponents: a scenario fails review if textbook loses or do-nothing wins.
/// </summary>
public interface IPolicy
{
    string Name { get; }
    IEnumerable<string> PrepChoices(ScenarioBundle bundle);
    IEnumerable<PlayerAction> Decide(SessionView view);
}

public static class PolicyCatalog
{
    public static readonly IReadOnlyList<string> Names = ["do-nothing", "greedy", "textbook"];

    public static IPolicy Create(string name) => name switch
    {
        "do-nothing" => new DoNothingPolicy(),
        "greedy" => new GreedyPolicy(),
        "textbook" => new TextbookPolicy(),
        _ => throw new ArgumentException($"unknown policy {name}; known: {string.Join(", ", Names)}"),
    };
}

/// <summary>Takes no action. The floor every scenario must beat.</summary>
public sealed class DoNothingPolicy : IPolicy
{
    public string Name => "do-nothing";
    public IEnumerable<string> PrepChoices(ScenarioBundle bundle) => [];
    public IEnumerable<PlayerAction> Decide(SessionView view) => [];
}

/// <summary>
/// The first-timer: buys exactly what each requirement card says, the fastest way available, sized to the day it
/// starts, and never investigates. No prep, no surveys, no follow-through.
/// </summary>
public sealed class GreedyPolicy : IPolicy
{
    public string Name => "greedy";
    public IEnumerable<string> PrepChoices(ScenarioBundle bundle) => [];

    public IEnumerable<PlayerAction> Decide(SessionView v)
    {
        foreach (var card in v.Cards.Where(c => c.SubmittedDay <= v.Day))
        {
            if (Helpers.CardCovered(v, card.Id)) continue;
            var lines = card.Lines.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
            var qty = Math.Max(1, v.PeakDemandUnits(card.SiteId, card.CapabilityId, card.RequestedStartDay, card.RequestedStartDay));
            var choice = Helpers.FastestRoute(v, card.SiteId, card.CapabilityId, lines, qty, card.DurationDays, preferIsland: null);
            if (choice is null) continue;
            yield return choice with { Id = $"g-{card.Id}", Card = card.Id, Lines = lines, StartDay = card.RequestedStartDay };
        }
    }
}

/// <summary>
/// The experienced CCO: preps, surveys every island with a site on day 1, completes scope before buying,
/// bridges urgent needs with the fastest legal route, sources consumables on the site's own island where it can,
/// starts an enduring turnkey arrangement early, overlaps the transition, and definitizes as soon as it is allowed.
/// Generic heuristics, not a script for one scenario.
/// </summary>
public sealed class TextbookPolicy : IPolicy
{
    public string Name => "textbook";
    private readonly HashSet<string> _done = [];

    public IEnumerable<string> PrepChoices(ScenarioBundle bundle)
    {
        var wanted = new[] { "systemAccess", "cashAndPayingAgent", "pullVehicles", "vendorLists", "templatesAndKit", "gpcAccounts" };
        var budget = bundle.Scenario.PrepDays;
        foreach (var id in wanted)
        {
            var c = bundle.Rules.PrepChoices.FirstOrDefault(x => x.Id == id);
            if (c is null || c.PrepDays > budget) continue;
            budget -= c.PrepDays;
            yield return id;
        }
    }

    public IEnumerable<PlayerAction> Decide(SessionView v)
    {
        var actions = new List<PlayerAction>();
        if (v.Day == 1)
            foreach (var island in v.Scenario.Sites.Select(s => s.Island).Distinct())
                if (v.KnowledgeTier(island) is "assumed" or "estimated" && !v.SurveyPending(island))
                    actions.Add(new SurveyAction { Island = island });

        foreach (var card in v.Cards.Where(c => c.SubmittedDay <= v.Day))
        {
            var cap = v.Rules.Model.Capabilities.First(c => c.Id == card.CapabilityId);
            // 1. Complete the scope before buying anything.
            foreach (var line in cap.Lines.Where(l => !card.Lines.GetValueOrDefault(l.Id)))
                if (_done.Add($"line:{card.Id}:{line.Id}"))
                    actions.Add(new SetLineAction { Card = card.Id, Line = line.Id, On = true });
            var allLines = cap.Lines.Select(l => l.Id).ToList();
            var periodEnd = card.RequestedStartDay + card.DurationDays - 1;
            var peak = Math.Max(1, v.PeakDemandUnits(card.SiteId, card.CapabilityId, card.RequestedStartDay, periodEnd));
            var sized = peak + peak / 10;

            // 2. Enduring: a turnkey vendor offering every line, started now.
            var enduringId = $"t-{card.Id}-enduring";
            if (_done.Add($"enduring:{card.Id}"))
            {
                var turnkey = Helpers.Vendors(v, card.CapabilityId, allLines).OrderBy(x => x.Offer.UnitPriceCents).ThenBy(x => x.Vendor.Id).FirstOrDefault();
                if (turnkey.Vendor is not null)
                {
                    var total = sized * turnkey.Offer.UnitPriceCents * card.DurationDays;
                    total += (long)(total * turnkey.Offer.MobilizationFraction);
                    var instrument = total > v.Rules.Thresholds.SimplifiedAcquisitionCents || card.RequestedStartDay - v.Day < 20 ? "uca" : "purchaseOrder";
                    var line = Helpers.Funding(v, instrument == "uca" ? 1 : total);
                    if (line is not null && Helpers.HasCapacity(v, turnkey.Vendor, card.CapabilityId, sized))
                        actions.Add(new AwardAction
                        {
                            Id = enduringId, Instrument = instrument, Vendor = turnkey.Vendor.Id, Card = card.Id, Lines = allLines,
                            Quantity = sized, DurationDays = card.DurationDays, FundingLine = line.Id, StartDay = card.RequestedStartDay,
                        });
                }
            }

            // 3. Bridge when the need starts before a turnkey vendor could: core line now, plus each supplied capability
            //    from the site's own island where possible.
            var enduringStart = v.Arrangements.FirstOrDefault(a => a.Id == enduringId)?.StartDay;
            var turnkeyLead = Helpers.Vendors(v, card.CapabilityId, allLines).Select(x => x.Offer.LeadTimeDays).DefaultIfEmpty(99).Min();
            var urgent = card.RequestedStartDay - v.Day < turnkeyLead + 6;
            if (urgent && _done.Add($"bridge:{card.Id}"))
            {
                var bridgeDays = Math.Max(7, turnkeyLead + 8 - (card.RequestedStartDay - v.Day));
                var core = cap.Lines.Where(l => l.Kind is "core").Select(l => l.Id).ToList();
                var island = v.Scenario.Sites.First(s => s.Id == card.SiteId).Island;
                var bridge = Helpers.FastestRoute(v, card.SiteId, card.CapabilityId, core, sized, bridgeDays, preferIsland: null);
                if (bridge is not null)
                    actions.Add(bridge with { Id = $"t-{card.Id}-bridge", Card = card.Id, Lines = core, StartDay = card.RequestedStartDay });
                foreach (var supplies in cap.Lines.Where(l => l.Kind == "supplies" && l.Supplies is not null))
                {
                    var target = supplies.Supplies!;
                    if (!v.Capabilities.Any(c => c.SiteId == card.SiteId && c.CapabilityId == target)) continue;
                    var need = Math.Max(1, v.PeakDemandUnits(card.SiteId, target, card.RequestedStartDay, card.RequestedStartDay + bridgeDays));
                    var targetCore = v.Rules.Model.Capabilities.First(c => c.Id == target).Lines.Where(l => l.Kind == "core").Select(l => l.Id).ToList();
                    var local = Helpers.FastestRoute(v, card.SiteId, target, targetCore, need, bridgeDays, preferIsland: island);
                    if (local is not null)
                        actions.Add(local with { Id = $"t-{card.Id}-{target}", Lines = targetCore, StartDay = Math.Max(v.Day, card.RequestedStartDay - 1) });
                }
            }

            // 4. Close the bridge once the enduring arrangement has run two days.
            var enduring = v.Arrangements.FirstOrDefault(a => a.Id == enduringId);
            if (enduring is { Status: ArrangementStatus.Active, DaysActive: >= 2 })
                foreach (var b in v.Arrangements.Where(a => a.Id.StartsWith($"t-{card.Id}-", StringComparison.Ordinal) && a.Id != enduringId && a.Status == ArrangementStatus.Active))
                    if (_done.Add($"end:{b.Id}"))
                        actions.Add(new TerminateAction { Arrangement = b.Id });

            // 5. Definitize the moment the proposal is in.
            if (enduring?.Uca is { ProposalReceived: true, Definitized: false, DefinitizationInWork: false } && _done.Add($"def:{enduringId}"))
            {
                var line = Helpers.Funding(v, enduring.Uca.QuotedTotalCents - enduring.ObligatedCents);
                if (line is not null) actions.Add(new DefinitizeAction { Arrangement = enduringId, FundingLine = line.Id });
            }
        }

        // 6. Any consumable running dry at an open site with nothing pending: emergency coordination from the hub.
        foreach (var cs in v.Capabilities.Where(c => c.History.Count > 0 && c.TodayPermille < 1000))
        {
            var cap = v.Rules.Model.Capabilities.First(c => c.Id == cs.CapabilityId);
            if (cap.Delivery != "consumable") continue;
            var pending = v.ArrangementsFor(cs.SiteId, cs.CapabilityId).Any(a => a.Status is ArrangementStatus.Pending);
            if (pending) continue;
            var hub = v.Scenario.Sites.FirstOrDefault(s => v.Scenario.Islands.First(i => i.Id == s.Island).IsHub && s.Id != cs.SiteId);
            if (hub is null || !_done.Add($"coord:{cs.SiteId}:{cs.CapabilityId}:{v.Day / 7}")) continue;
            var need = Math.Max(1, v.PeakDemandUnits(cs.SiteId, cs.CapabilityId, v.Day, v.LastDay));
            actions.Add(new CoordinateAction { Id = $"t-coord-{cs.SiteId}-{cs.CapabilityId}-{v.Day}", Site = cs.SiteId, Capability = cs.CapabilityId, Quantity = need, FromSite = hub.Id });
        }
        return actions;
    }
}

internal static class Helpers
{
    public static bool CardCovered(SessionView v, string cardId)
        => v.Arrangements.Any(a => a.CardId == cardId && a.Status is not ArrangementStatus.Declined) || v.Work.Any(w => w.CompletedDay is null && w.Label.Contains(cardId, StringComparison.Ordinal));

    public static IEnumerable<(VendorData Vendor, VendorOfferData Offer)> Vendors(SessionView v, string capability, IReadOnlyCollection<string> lines)
        => v.Scenario.Vendors.SelectMany(vd => vd.Offers.Where(o => o.Capability == capability && lines.All(o.Lines.Contains)).Select(o => (vd, o)));

    public static bool HasCapacity(SessionView v, VendorData vendor, string capability, long qty)
    {
        var offer = vendor.Offers.First(o => o.Capability == capability);
        var committed = v.Arrangements.Where(a => a.VendorId == vendor.Id && a.CapabilityId == capability && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active).Sum(a => a.QuantityMilli) / 1000;
        return committed + qty <= (long)offer.CapacityPerDay;
    }

    public static FundingLineState? Funding(SessionView v, long cents)
        => v.Funding.Where(f => f.UsableOn(v.Day) && f.RemainingCents >= cents).OrderBy(f => f.ExpiresDay).FirstOrDefault();

    /// <summary>
    /// Fastest legal route for a buy: BPA call if a known BPA covers it, then cash or card under the micro-purchase
    /// threshold, then a written purchase order. Vendors on preferIsland come first when given.
    /// </summary>
    public static AwardAction? FastestRoute(SessionView v, string siteId, string capability, List<string> lines, long qty, int days, string? preferIsland)
    {
        var candidates = Vendors(v, capability, lines)
            .Where(x => !x.Vendor.VehicleOnly || v.Scenario.Vehicles.Any(b => b.Vendor == x.Vendor.Id))
            .Where(x => HasCapacity(v, x.Vendor, capability, qty))
            .OrderBy(x => preferIsland is not null && x.Vendor.Island == preferIsland ? 0 : 1)
            .ThenBy(x => x.Offer.LeadTimeDays).ThenBy(x => x.Offer.UnitPriceCents).ThenBy(x => x.Vendor.Id)
            .ToList();
        foreach (var (vendor, offer) in candidates)
        {
            var total = qty * offer.UnitPriceCents * days;
            total += (long)(total * offer.MobilizationFraction);
            var bpa = v.Scenario.Vehicles.FirstOrDefault(b => b.Kind == "bpa" && b.VisibleOnDay1 && b.Vendor == vendor.Id && v.Day <= b.OrderingPeriodEndDay
                                                             && lines.All(b.ScopeTags.Contains) && (b.CallLimitCents is null || total <= b.CallLimitCents));
            var line = Funding(v, total);
            if (bpa is not null && line is not null)
                return new AwardAction { Instrument = "bpaCall", Vendor = vendor.Id, Vehicle = bpa.Id, Site = siteId, Capability = capability, Quantity = qty, DurationDays = days, FundingLine = line.Id };
            if (total <= v.Rules.Thresholds.MicroPurchaseCents)
            {
                if (v.PayingAgentAvailable && vendor.AcceptsCash && total <= v.CashCents)
                    return new AwardAction { Instrument = "sf44", Vendor = vendor.Id, Site = siteId, Capability = capability, Quantity = qty, DurationDays = days };
                if (v.GpcAvailable && vendor.AcceptsGpc)
                    return new AwardAction { Instrument = "gpc", Vendor = vendor.Id, Site = siteId, Capability = capability, Quantity = qty, DurationDays = days };
            }
            if (line is not null && total <= v.Rules.Thresholds.SimplifiedAcquisitionCents)
                return new AwardAction { Instrument = "purchaseOrder", Vendor = vendor.Id, Site = siteId, Capability = capability, Quantity = qty, DurationDays = days, FundingLine = line.Id };
        }
        return null;
    }
}
