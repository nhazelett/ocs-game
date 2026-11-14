namespace LinesOfSupport.Sim.Views;

// Map day projection (backlog M2R-09, OBJ-0004). What actually moved on the day just played, taken from the
// engine's own day processing, so the illustrated map never shows a barge arriving on a day the Sim closed the
// route. Read-only, additive to ADR-0006. "Known" follows the COR rules: a delivery is seen by the site; a vendor
// failure is known only when it was reported.

/// <summary>A transport link's state on the day: whether service was scheduled and whether the route was open.</summary>
/// <param name="ClosedThroughDay">Last day of the current closure, when closed.</param>
/// <param name="ClosureRecordId">The event record that closed it, when known.</param>
public sealed record MapLink(string Id, string FromIslandId, string ToIslandId, bool Bidirectional, string Mode,
    bool ScheduledToday, bool Open, int? ClosedThroughDay, string? ClosureRecordId);

/// <summary>A delivery that actually ran on the day: a consumable crossed from the supplier's island to the site's.</summary>
/// <param name="Quantity">Whole units delivered, after vendor factors.</param>
public sealed record MapRun(string ArrangementId, string VendorId, string VendorName, string CapabilityId, string Unit,
    string FromIslandId, string ToIslandId, string SiteId, string LinkId, string Mode, long Quantity);

/// <summary>Cargo that did not move: a consumable waiting on a closed or unscheduled route.</summary>
/// <param name="Reason">"closed" (route closed) or "noService" (no scheduled run that day).</param>
public sealed record MapHeld(string ArrangementId, string VendorId, string VendorName, string CapabilityId, string Unit,
    string FromIslandId, string ToIslandId, string SiteId, string? LinkId, string Reason, long WaitingQuantity);

/// <summary>An off-island shipment (airlift or sealift) still on its way.</summary>
/// <param name="ArrivalDay">The day it is due to arrive, as currently projected.</param>
public sealed record MapShipment(string ArrangementId, string VendorId, string VendorName, string CapabilityId, string Mode,
    string ToIslandId, string SiteId, int ArrivalDay, int Bumps, bool CustomsHeld);

/// <summary>A site capability that changed state on the day: went short, failed, or came back.</summary>
/// <param name="Kind">"shortfall", "restored" or "vendorFailure".</param>
/// <param name="Known">False when the player has not been told yet (an unreported vendor failure).</param>
/// <param name="WhyShort">DESK-38 (D18): on a shortfall, why and what is in motion, one sentence; null otherwise.</param>
public sealed record MapChange(string SiteId, string CapabilityId, string Kind, int Permille, string? ArrangementId, string RecordId, bool Known,
    string? WhyShort = null);

/// <summary>Everything the map needs to draw the day just played.</summary>
public sealed record MapDayView(int Day, IReadOnlyList<MapLink> Links, IReadOnlyList<MapRun> Runs, IReadOnlyList<MapHeld> Held,
    IReadOnlyList<MapShipment> Shipments, IReadOnlyList<MapChange> Changes);
