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
    private readonly HashSet<string> _bridges = [];
    /// <summary>Site:capability pairs handed to their usual owner (the J4); the office leaves those supply lines off its own buys.</summary>
    private readonly HashSet<string> _owned = [];

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
        // Unauthorized commitments: ratify a handshake at once (the vendor delivered in good faith and the need is
        // real); deny payment for out-of-scope work under an active contract.
        // Day 1: if purchase cards exist, start the card program so CE's small buys leave the shop (design doc 6.1).
        if (v.Day == 1 && v.GpcAvailableDay is not null && _done.Add("gpcProgram")) actions.Add(new StandUpGpcProgramAction());
        // A contracted vendor that under-delivers gets a cure notice the morning the issue arrives.
        foreach (var i in v.PerformanceIssues.Where(i => i.CureNoticeDay is null && i.Outcome == CureOutcome.None))
            if (_done.Add($"cure:{i.Id}")) actions.Add(new CureNoticeAction { Issue = i.Id });
        foreach (var c in v.Commitments.Where(c => c.Unresolved))
            actions.Add(new ResolveCommitmentAction
            {
                Commitment = c.Id, Choice = c.Kind == CommitmentKind.Handshake ? "ratify" : "deny",
                FundingLine = v.Funding.Where(f => f.UsableOn(v.Day) && f.RemainingCents > 0 && f.Pays(c.CapabilityId)).Select(f => f.Id).FirstOrDefault(),
            });
        // Read the inherited contract files on day 1, before the clocks in them run on (M3-17).
        if (v.Day == 1 && v.InheritedFiles().Any(f => f.ReviewedDay is null && !f.InReview))
            actions.Add(new ReviewFilesAction());
        // Day 1: anything with a usual owner other than this office (fuel: the J4 through the ACSA partner) goes to that owner
        // first (design doc R10, M2R-06). One request per site and capability, sized to the site's peak demand over the game,
        // for every site that lists the capability and arrived with nothing covering it. The office buys it only as a stopgap.
        if (v.Day == 1 && v.AcsaPartner is { } partner)
            foreach (var site in v.Scenario.Sites)
                foreach (var sc in site.Capabilities)
                {
                    var capData = v.Rules.Model.Capabilities.FirstOrDefault(c => c.Id == sc.Id);
                    if (capData?.UsualOwner is null || !partner.Stock.TryGetValue(sc.Id, out var stock) || stock <= 0) continue;
                    if (sc.InitialArrangements.Count > 0 || !_done.Add($"j4:{site.Id}:{sc.Id}")) continue;
                    var opens = Math.Max(1, site.OpensDay);
                    var peak = Math.Max(1, v.PeakDemandUnits(site.Id, sc.Id, opens, v.LastDay));
                    _owned.Add($"{site.Id}:{sc.Id}");
                    actions.Add(new EngageJ4Action { Id = $"t-j4-{site.Id}-{sc.Id}", Site = site.Id, Capability = sc.Id, Quantity = peak + peak / 10 });
                }
        if (v.Day == 1)
            foreach (var island in v.Scenario.Sites.Select(s => s.Island).Distinct())
                if (v.KnowledgeTier(island) is "assumed" or "estimated" && !v.SurveyPending(island))
                    actions.Add(new SurveyAction { Island = island });

        // Inherited clocks first (M3-07): definitize every inherited UCA the files show with its proposal in hand. Until
        // the file review is done and those definitizations are queued, a new requirement that is not needed for
        // weeks waits, because the shop's queue runs in order and the clocks in the files are already running.
        var clocksFirst = v.InheritedFiles().Any(f => f.InReview) || InheritedUcasToDefinitize(v).Any();
        foreach (var a in InheritedUcasToDefinitize(v))
            // Definitization obligates what the line can cover and the rest waits on money, so any current line will do.
            if (Helpers.Funding(v, 1, a.CapabilityId) is { } dl && _done.Add($"def:{a.Id}"))
                actions.Add(new DefinitizeAction { Arrangement = a.Id, FundingLine = dl.Id });

        foreach (var card in v.Cards.Where(c => c.SubmittedDay <= v.Day))
        {
            var cap = v.Rules.Model.Capabilities.First(c => c.Id == card.CapabilityId);
            if (clocksFirst && card.RequestedStartDay - v.Day > 14) continue;
            // 1. Complete the scope before buying anything. A supplies line whose capability the usual owner is providing
            //    (the J4's fuel) stays off: the office does not buy what the J4 is already bringing.
            bool Owned(CapabilityLineData l) => l.Kind == "supplies" && l.Supplies is { } t && _owned.Contains($"{card.SiteId}:{t}");
            foreach (var line in cap.Lines.Where(l => !card.Lines.GetValueOrDefault(l.Id) && !Owned(l)))
                if (_done.Add($"line:{card.Id}:{line.Id}"))
                    actions.Add(new SetLineAction { Card = card.Id, Line = line.Id, On = true });
            var allLines = cap.Lines.Where(l => !Owned(l)).Select(l => l.Id).ToList();
            var periodEnd = card.RequestedStartDay + card.DurationDays - 1;
            var peak = Math.Max(1, v.PeakDemandUnits(card.SiteId, card.CapabilityId, card.RequestedStartDay, periodEnd));
            var sized = peak + peak / 10;

            // 2. Enduring: a turnkey vendor offering every line, started now.
            var enduringId = $"t-{card.Id}-enduring";
            // Only vendors this office can actually buy from (not reachable only through someone else's vehicle,
            // such as host-nation support) with room for the quantity.
            var turnkey = Helpers.Vendors(v, card.CapabilityId, allLines)
                .Where(x => !x.Vendor.VehicleOnly && Helpers.HasCapacity(v, x.Vendor, card.CapabilityId, sized))
                .OrderBy(x => x.Offer.UnitPriceCents).ThenBy(x => x.Vendor.Id).FirstOrDefault();
            if (_done.Add($"enduring:{card.Id}"))
            {
                if (turnkey.Vendor is not null)
                {
                    var total = sized * turnkey.Offer.UnitPriceCents * card.DurationDays;
                    total += (long)(total * turnkey.Offer.MobilizationFraction);
                    var instrument = total > v.Rules.Thresholds.SimplifiedAcquisitionCents || card.RequestedStartDay - v.Day < 20 ? "uca" : "purchaseOrder";
                    var line = Helpers.Funding(v, instrument == "uca" ? 1 : total, card.CapabilityId);
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
            // The bridge covers the lead of the vendor actually chosen for the enduring award, not the fastest on paper.
            var turnkeyLead = turnkey.Offer?.LeadTimeDays
                              ?? Helpers.Vendors(v, card.CapabilityId, allLines).Select(x => x.Offer.LeadTimeDays).DefaultIfEmpty(99).Min();
            var urgent = card.RequestedStartDay - v.Day < turnkeyLead + 6;
            if (urgent && _done.Add($"bridge:{card.Id}"))
            {
                var bridgeDays = Math.Max(7, turnkeyLead + 8 - (card.RequestedStartDay - v.Day));
                var core = cap.Lines.Where(l => l.Kind is "core").Select(l => l.Id).ToList();
                var island = v.Scenario.Sites.First(s => s.Id == card.SiteId).Island;
                var bridge = Helpers.FastestRoute(v, card.SiteId, card.CapabilityId, core, sized, bridgeDays, preferIsland: null);
                if (bridge is not null)
                {
                    _bridges.Add($"t-{card.Id}-bridge");
                    actions.Add(bridge with { Id = $"t-{card.Id}-bridge", Card = card.Id, Lines = core, StartDay = card.RequestedStartDay });
                }
                foreach (var supplies in cap.Lines.Where(l => l.Kind == "supplies" && l.Supplies is not null && !Owned(l)))
                {
                    var target = supplies.Supplies!;
                    if (!v.Capabilities.Any(c => c.SiteId == card.SiteId && c.CapabilityId == target)) continue;
                    var need = Math.Max(1, v.PeakDemandUnits(card.SiteId, target, card.RequestedStartDay, card.RequestedStartDay + bridgeDays));
                    var targetCore = v.Rules.Model.Capabilities.First(c => c.Id == target).Lines.Where(l => l.Kind == "core").Select(l => l.Id).ToList();
                    var local = Helpers.FastestRoute(v, card.SiteId, target, targetCore, need, bridgeDays, preferIsland: island);
                    if (local is not null)
                    {
                        _bridges.Add($"t-{card.Id}-{target}");
                        actions.Add(local with { Id = $"t-{card.Id}-{target}", Lines = targetCore, StartDay = Math.Max(v.Day, card.RequestedStartDay - 1) });
                    }
                }
            }

            // 4. Close the bridge once the enduring arrangement has run two days.
            var enduring = v.Arrangements.FirstOrDefault(a => a.Id == enduringId);
            if (enduring is { Status: ArrangementStatus.Active, DaysActive: >= 2 })
                foreach (var b in v.Arrangements.Where(a => _bridges.Contains(a.Id) && a.CardId != enduringId && a.Status == ArrangementStatus.Active && (a.CardId == card.Id || a.Id.StartsWith($"t-{card.Id}-", StringComparison.Ordinal))))
                    if (_done.Add($"end:{b.Id}"))
                        actions.Add(new TerminateAction { Arrangement = b.Id });

            // 5. Definitize the moment the proposal is in.
            if (enduring?.Uca is { ProposalReceived: true, Definitized: false, DefinitizationInWork: false } && _done.Add($"def:{enduringId}"))
            {
                var line = Helpers.Funding(v, enduring.Uca.QuotedTotalCents - enduring.ObligatedCents, enduring.CapabilityId);
                if (line is not null) actions.Add(new DefinitizeAction { Arrangement = enduringId, FundingLine = line.Id });
            }
        }

        // 6. Plan the follow-on: anything covering a capability at a site that stays open past its end gets a
        //    successor awarded before it runs out, starting a day early for overlap.
        foreach (var a in v.Arrangements.Where(a => a.Status is ArrangementStatus.Active && !a.IsCoordination && !a.Unauthorized && a.VendorId is not null && a.Instrument != "hostNation"))
        {
            var site = v.Scenario.Sites.First(s => s.Id == a.SiteId);
            var siteEnd = Math.Min(site.ClosesDay ?? v.LastDay, v.LastDay);
            if (a.EndDay >= siteEnd || a.EndDay - v.Day > 14) continue;
            var key = $"follow:{a.Id}";
            if (_done.Contains(key)) continue;
            var replaced = _bridges.Contains(a.Id) || v.InWork($"{a.Id}-next")
                           || v.Arrangements.Any(b => b.Id != a.Id && b.SiteId == a.SiteId
                                                      && (b.CapabilityId == a.CapabilityId || b.SuppliedQuantityMilli.ContainsKey(a.CapabilityId))
                                                      && b.Status is ArrangementStatus.Pending or ArrangementStatus.Active && b.EndDay > a.EndDay);
            if (replaced) { _done.Add(key); continue; }
            var vendor = v.Scenario.Vendors.First(x => x.Id == a.VendorId);
            var offer = vendor.Offers.First(o => o.Capability == a.CapabilityId);
            // Support the office cannot buy again on the open market (a partner's ACSA stock, a vehicle-only vendor):
            // the follow-on goes to the cheapest vendor that offers the same lines and has the room.
            if (vendor.VehicleOnly)
            {
                var qtyNeed = Math.Max(1, a.QuantityMilli / 1000);
                var alt = Helpers.Vendors(v, a.CapabilityId, a.Lines)
                    .Where(x => !x.Vendor.VehicleOnly && Helpers.HasCapacity(v, x.Vendor, a.CapabilityId, qtyNeed))
                    .OrderBy(x => x.Offer.UnitPriceCents).ThenBy(x => x.Vendor.Id).FirstOrDefault();
                if (alt.Vendor is null) { _done.Add(key); continue; }
                (vendor, offer) = alt;
            }
            // Overlap a day when a new vendor takes over; the incumbent simply continues the day after.
            var start = Math.Max(v.Day + 1, a.EndDay + 1);
            var days = siteEnd - start + 1;
            var qty = Math.Max(a.QuantityMilli / 1000, v.PeakDemandUnits(a.SiteId, a.CapabilityId, start, siteEnd));
            var total = qty * offer.UnitPriceCents * days;   // incumbent: no second mobilization
            var line = Helpers.Funding(v, total, a.CapabilityId);
            if (line is null) continue;   // try again tomorrow; new money may arrive
            var instrument = total <= v.Rules.Thresholds.SimplifiedAcquisitionCents ? "purchaseOrder" : "uca";
            _done.Add(key);
            actions.Add(new AwardAction
            {
                Id = $"{a.Id}-next", Instrument = instrument, Vendor = vendor.Id, Site = a.SiteId, Capability = a.CapabilityId,
                Lines = a.Lines, Quantity = qty, StartDay = start, DurationDays = days, FundingLine = line.Id,
            });
        }

        // 7. Resize when the plan changes: if what is now known about demand (an unplanned arrival's notice) is above
        //    everything contracted for a capability at a site, buy the difference, but only by a route that costs no
        //    shop days (BPA call, cash, card): a written buy would delay the definitizations already in the queue,
        //    which costs more than a short dip. Tried again each day until capacity frees up.
        foreach (var site in v.Scenario.Sites)
        {
            var siteEnd = Math.Min(site.ClosesDay ?? v.LastDay, v.LastDay);
            if (v.Day >= siteEnd) continue;
            foreach (var cs in v.Capabilities.Where(c => c.SiteId == site.Id))
            {
                var held = v.ArrangementsFor(site.Id, cs.CapabilityId)
                            .Where(a => a.Status is ArrangementStatus.Pending or ArrangementStatus.Active && !a.IsCoordination && !a.Unauthorized && a.EndDay >= siteEnd)
                            .ToList();
                if (!held.Any(a => a.Instrument != "hostNation")) continue;
                var supplied = held.Sum(a => (a.CapabilityId == cs.CapabilityId ? a.QuantityMilli : 0) + a.SuppliedQuantityMilli.GetValueOrDefault(cs.CapabilityId)) / 1000;
                var need = v.PeakDemandUnits(site.Id, cs.CapabilityId, v.Day + 1, siteEnd);
                var key = $"resize:{site.Id}:{cs.CapabilityId}:{need}";
                if (need <= supplied || _done.Contains(key)) continue;
                var cap = v.Rules.Model.Capabilities.First(c => c.Id == cs.CapabilityId);
                var core = cap.Lines.Where(l => l.Kind == "core").Select(l => l.Id).ToList();
                var buy = Helpers.FastestRoute(v, site.Id, cs.CapabilityId, core, need - supplied, siteEnd - v.Day, site.Island);
                if (buy is not { Instrument: "sf44" or "gpc" or "bpaCall" }) continue;
                _done.Add(key);
                actions.Add(buy with { Id = $"t-resize-{site.Id}-{cs.CapabilityId}-{v.Day}" });
            }
        }

        // 8. Any consumable running dry at an open site with nothing pending: emergency coordination from the hub.
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

        // 9. CORs (M3-04): every contract with written terms gets the best COR on the roster; when a COR rotates home
        //    and the relief is worse than someone else on the roster, re-designate.
        if (v.CorModeled)
        {
            var best = Helpers.BestCor(v);
            for (var i = 0; i < actions.Count; i++)
                if (actions[i] is AwardAction { Cor: null } aw && v.Rules.Instruments.TryGetValue(aw.Instrument, out var inst) && inst.WrittenTerms)
                    actions[i] = aw with { Cor = best.Id };
            foreach (var a in v.Arrangements.Where(a => a.CorId is not null && a.CreatedDay > 0
                                                        && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended))
            {
                var cur = v.Cors.First(c => c.Id == a.CorId);
                if (cur.Id != best.Id && Helpers.CorRank(v, best) < Helpers.CorRank(v, cur) && _done.Add($"cor:{a.Id}:{best.Id}"))
                    actions.Add(new DesignateCorAction { Arrangement = a.Id, Cor = best.Id });
            }
        }
        return actions;
    }

    /// <summary>Inherited UCAs (M3-16) the player can see, with a qualifying proposal and no definitization started.</summary>
    private static IEnumerable<Arrangement> InheritedUcasToDefinitize(SessionView v)
        => v.Arrangements.Where(a => a.CreatedDay == 0 && !v.ProblemHidden(a) && a.Status is ArrangementStatus.Active or ArrangementStatus.Suspended
                                     && a.Uca is { ProposalReceived: true, Definitized: false, DefinitizationInWork: false });
}

internal static class Helpers
{
    /// <summary>Lower is better: report lag first, then the share of problems reported (M3-04).</summary>
    public static int CorRank(SessionView v, CorState c) { var q = v.CorQuality(c); return q.LagDays * 1000 + (1000 - q.ReportPermille); }

    public static CorState BestCor(SessionView v) => v.Cors.OrderBy(c => CorRank(v, c)).ThenBy(c => c.Id, StringComparer.Ordinal).First();

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

    /// <summary>The usable line with the money that expires first and is certified for the capability (M3-03).</summary>
    public static FundingLineState? Funding(SessionView v, long cents, string? capability = null)
        => v.Funding.Where(f => f.UsableOn(v.Day) && f.RemainingCents >= cents && (capability is null || f.Pays(capability)))
                    .OrderBy(f => f.ExpiresDay).FirstOrDefault();

    /// <summary>
    /// Fastest legal route for a buy: BPA call if a known BPA covers it, then cash or card under the micro-purchase
    /// threshold, then a written purchase order. Vendors on preferIsland come first when given.
    /// </summary>
    public static AwardAction? FastestRoute(SessionView v, string siteId, string capability, List<string> lines, long qty, int days, string? preferIsland)
    {
        var candidates = Vendors(v, capability, lines)
            .Where(x => !x.Vendor.VehicleOnly || v.Vehicles.Any(b => b.Vendor == x.Vendor.Id))
            .Where(x => HasCapacity(v, x.Vendor, capability, qty))
            .OrderBy(x => preferIsland is not null && (x.Offer.SourceIsland ?? x.Vendor.Island) == preferIsland ? 0 : 1)
            .ThenBy(x => x.Offer.LeadTimeDays).ThenBy(x => x.Offer.UnitPriceCents).ThenBy(x => x.Vendor.Id)
            .ToList();
        foreach (var (vendor, offer) in candidates)
        {
            var total = qty * offer.UnitPriceCents * days;
            total += (long)(total * offer.MobilizationFraction);
            var bpa = v.Vehicles.FirstOrDefault(b => b.Kind == "bpa" && b.VisibleOnDay1 && b.Vendor == vendor.Id && v.Day <= b.OrderingPeriodEndDay
                                                             && lines.All(b.ScopeTags.Contains) && (b.CallLimitCents is null || total <= b.CallLimitCents));
            var line = Funding(v, total, capability);
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
