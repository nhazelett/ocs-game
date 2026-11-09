namespace LinesOfSupport.Sim.Views;

// View-model contracts between the Sim and the Game (backlog M0-07, ADR-0006).
// Every type here is an immutable snapshot built from a SessionView. The Game renders them and never mutates them;
// state changes go through Session.Apply and Session.AdvanceDay only (ADR-0005). No Godot types, no doubles that
// feed outcomes: money is in cents, quantities are whole units, shop time is whole hours rounded up.
// Text is facts and limits, never advice on how to do the job.

// ------------------------------------------------------------------ people (M2R-01)

/// <summary>A person in the scenario's cast, as the player meets them. Show <see cref="DisplayName"/> and Unit; Id is for
/// lookups only and never appears in player-facing text (docs/design/opening-and-guidance.md R6).</summary>
/// <param name="Role">customer, outgoingCco, scoStaff, finance, commander or vendorRep.</param>
/// <param name="Portrait">Art key for the portrait, if the scenario names one.</param>
/// <param name="Lines">Short lines for the moments the game uses this person (keys such as "greeting", "farewell").</param>
/// <param name="VendorName">vendorRep only: the vendor's name.</param>
public sealed record PersonView(
    string Id, string Name, string Rank, string Unit, string Role, string? Portrait,
    IReadOnlyDictionary<string, string> Lines, string? VendorName = null)
{
    /// <summary>Rank and name, e.g. "Capt Ana Ruiz"; the name alone when there is no rank.</summary>
    public string DisplayName => Rank.Length == 0 ? Name : $"{Rank} {Name}";
    /// <summary>Rank, name and unit, e.g. "Capt Ana Ruiz, 3 CES".</summary>
    public string WithUnit => Unit.Length == 0 ? DisplayName : $"{DisplayName}, {Unit}";
    /// <summary>The line for a moment, or null when the scenario wrote none.</summary>
    public string? Line(string moment) => Lines.TryGetValue(moment, out var l) ? l : null;
}

// ------------------------------------------------------------------ top strip

/// <summary>The always-visible strip: day, shop hours and cash gauges, funds and system status.</summary>
/// <param name="Day">Day about to be played (1-based).</param>
/// <param name="LastDay">Last day of the scenario.</param>
/// <param name="ShopPeople">People in the contracting shop.</param>
/// <param name="ShopHoursPerDay">Hours the shop can work each day (gauge maximum).</param>
/// <param name="ShopHoursQueued">Hours of unfinished work in the queue, including items still waiting on an outside lag.</param>
/// <param name="QueueDays">ShopHoursQueued divided by ShopHoursPerDay, rounded up: how far behind the shop is.</param>
/// <param name="CashCents">Cash on hand with the paying agent.</param>
/// <param name="CashFullCents">Cash on hand when full (gauge maximum).</param>
/// <param name="CashReplenishDay">Day FM restores cash, if a replenishment is scheduled.</param>
/// <param name="FundsUsableCents">Unobligated money on funding lines usable today.</param>
/// <param name="SystemOnline">Contract-writing system available; written actions take longer and invoices are manual until then.</param>
/// <param name="SystemOnlineDay">Day the system comes online.</param>
/// <param name="PayingAgentAvailable">SF 44 cash buys possible today.</param>
/// <param name="GpcAvailable">Purchase card buys possible today.</param>
public sealed record TopStripView(
    int Day, int LastDay, int ShopPeople, int ShopHoursPerDay, int ShopHoursQueued, int QueueDays,
    long CashCents, long CashFullCents, int? CashReplenishDay, long FundsUsableCents,
    bool SystemOnline, int SystemOnlineDay, bool PayingAgentAvailable, bool GpcAvailable);

// ------------------------------------------------------------------ morning board

/// <summary>What the morning board groups items under (SPEC section 2, morning board).</summary>
public enum BoardCategory { Delivery, Failure, Report, Request, VendorNotice, HigherHq, Event }

/// <summary>How loudly an item is shown. Alert means a capability is short or work has stopped.</summary>
public enum BoardSeverity { Info, Warning, Alert }

/// <summary>What a board item links to, so the desk can open the right panel.</summary>
/// <summary>Funding (M3-03): LinkId is a funding line id, or "funding" for the shop's funding as a whole; open the ledger.</summary>
public enum BoardLinkKind { None, Arrangement, Card, SiteCapability, Island, Event, Cash, Work, Funding }

/// <summary>One thing that changed overnight.</summary>
/// <param name="RecordId">The cause record this item shows; follow CausedBy for the chain.</param>
/// <param name="Day">The day the change happened (the day just played).</param>
/// <param name="LinkId">Id to open: arrangement id, card id, "site.capability", island id, event id, "cash" or work label.</param>
/// <param name="Person">Who the player hears it from (M2R-01): the card's person, the event's speaker, or the vendor's rep;
/// null when no one in particular.</param>
public sealed record BoardItem(
    string RecordId, int Day, BoardCategory Category, BoardSeverity Severity,
    BoardLinkKind LinkKind, string LinkId, string Text, IReadOnlyList<string> CausedBy, PersonView? Person = null);

/// <summary>One capability tile at a site, as of this morning.</summary>
/// <param name="TodayPermille">Share of demand met on the day just played, 0 to 1000.</param>
/// <param name="BufferDays">Days of stock on hand for consumables at current demand, rounded down; null for continuous capabilities.</param>
/// <param name="Status">"ok" (at or above the down threshold and full), "degraded", "down", or "notOpen" before the site opens.</param>
public sealed record CapabilityTile(string SiteId, string CapabilityId, string Label, int TodayPermille, int? BufferDays, string Status, bool Critical);

/// <summary>The morning board: what changed overnight, sorted alerts first, plus the capability tiles and the top strip.</summary>
/// <param name="Items">Items from the last AdvanceDay, Alert then Warning then Info, in record order within a severity. Empty on day 1.</param>
/// <param name="Lead">The one thing the day leads with (M2R-05), or null on a quiet day.</param>
public sealed record MorningBoardView(int Day, TopStripView Strip, IReadOnlyList<BoardItem> Items, IReadOnlyList<CapabilityTile> Tiles,
    LeadItem? Lead = null);

/// <summary>Why an item leads the day, in the order the fixed rule tries them (design doc 6.3).</summary>
public enum LeadKind { Decision, Overdue, Alert, Clock }

/// <summary>The one decision in front (M2R-05, R5). Text is a fact in prose, never advice (R4); open LinkKind/LinkId to act.</summary>
/// <param name="Person">Who it comes from, when someone does.</param>
/// <param name="DueDay">The day the clock or requirement is due, when there is one.</param>
/// <param name="RecordId">For an Alert, the board record it repeats.</param>
public sealed record LeadItem(LeadKind Kind, string Text, BoardLinkKind LinkKind, string LinkId, PersonView? Person, int? DueDay,
    string? RecordId = null);

// ------------------------------------------------------------------ requirement editor (OCS seat)

/// <summary>One scope line on a requirement card.</summary>
/// <param name="Kind">From rules: core, supplies, operator or maintenance.</param>
/// <param name="Supplies">For a "supplies" line, the capability it brings with it (power's fuelSupply supplies fuel).</param>
/// <param name="GapIfOff">The consequence of leaving this line off, stated as a fact; null for core lines.</param>
public sealed record RequirementLine(string Id, string Label, string Kind, bool On, string? Supplies, string? GapIfOff);

/// <summary>A capability this one depends on at the same site, and whether the player has it covered.</summary>
/// <param name="Coverage">"inRequirement" (a supplies line on this card is on), "arranged" (an arrangement at the site covers it),
/// or "none".</param>
/// <param name="CoveredBy">Arrangement ids at the site covering it, if any.</param>
public sealed record RequirementDependency(string CapabilityId, string Label, string Coverage, IReadOnlyList<string> CoveredBy);

/// <summary>The requirement editor's view of one card: what it asks for, its lines, and what it depends on.</summary>
/// <param name="PeakDemandUnits">Peak daily demand for the capability at the site over the card's window, from population.</param>
/// <param name="SubmittedBy">Who brought it, as shown: the person's rank, name and unit, or the card's free text.</param>
/// <param name="CoveredBy">Arrangements (any status but declined) already tied to this card.</param>
/// <param name="Title">What and where, e.g. "Power for Spoke B": the card's name on screen (M2R-01).</param>
/// <param name="Person">The person who brought it, when the scenario names one (M2R-01).</param>
/// <param name="Message">What they said when they brought it, in their words.</param>
public sealed record RequirementEditorView(
    string CardId, string SiteId, string SiteName, string CapabilityId, string CapabilityLabel, string Unit,
    string SubmittedBy, int SubmittedDay, int RequestedStartDay, int DurationDays, string? QuantityHint, long PeakDemandUnits,
    IReadOnlyList<RequirementLine> Lines, IReadOnlyList<RequirementDependency> Dependencies, IReadOnlyList<string> CoveredBy,
    string Title = "", PersonView? Person = null, string? Message = null);

// ------------------------------------------------------------------ action editor (CCO seat)

/// <summary>An instrument the player can pick, with why it is or is not usable today.</summary>
/// <param name="UnavailableReason">Null when usable today; otherwise the fact that blocks it (no paying agent yet, not in this build).</param>
/// <param name="LimitCents">Per-action ceiling from its limit basis (micro-purchase, simplified acquisition), if any.</param>
/// <param name="ShopHoursMin">Shop hours a written action takes at today's manual or system speed, low end.</param>
public sealed record InstrumentOption(
    string Id, string Label, bool Binding, bool WrittenTerms, string? UnavailableReason, long? LimitCents,
    int ShopHoursMin, int ShopHoursMax, int StartLagMin, int StartLagMax);

/// <summary>A vendor offering the capability, as the market directory shows it.</summary>
/// <param name="CapacityFreeToday">Units per day not committed to overlapping arrangements starting today.</param>
/// <param name="UnitPriceCents">Quoted price per unit per day.</param>
/// <param name="MobilizationPermille">Mobilization as a share of the base price, 0 to 1000.</param>
/// <param name="VehicleOnly">Reachable only through an existing vehicle (BPA call).</param>
/// <param name="Standing">Null normally; otherwise how this vendor treats the office after an unpaid handshake
/// ("declines work", or the markup already included in UnitPriceCents).</param>
public sealed record VendorOption(
    string Id, string Name, string IslandId, string IslandName, IReadOnlyList<string> Lines, int CapacityPerDay, int CapacityFreeToday,
    int LeadTimeDays, long UnitPriceCents, int MobilizationPermille, bool AcceptsCash, bool AcceptsGpc, bool VehicleOnly,
    string? Standing = null);

/// <summary>An existing vehicle the shop knows about that could carry this action.</summary>
/// <param name="CeilingCents">IDIQs: the contract ceiling (null for BPAs, or an IDIQ with no ceiling in its data).</param>
/// <param name="OrderedCents">IDIQs: value ordered so far; the ceiling less this is what is left to order.</param>
/// <param name="GuaranteeUnorderedCents">IDIQs: minimum guarantee obligated at award and not yet ordered against.</param>
public sealed record VehicleOption(
    string Id, string Kind, string VendorId, IReadOnlyList<string> ScopeLines, long? CallLimitCents,
    int OrderingPeriodEndDay, IReadOnlyList<string> PlaceOfPerformance, bool OpenToday,
    long? CeilingCents = null, long? OrderedCents = null, long? GuaranteeUnorderedCents = null);

/// <summary>A funding line, with what is left and whether it can be used today.</summary>
/// <param name="AvailableDay">The release day as it stands (a late release moves it).</param>
/// <param name="ExpiresDay">The fiscal-year end: the last day the line can be obligated.</param>
/// <param name="PlannedAvailableDay">The release day the scenario planned; later AvailableDay means the release is late (M3-03).</param>
/// <param name="Capabilities">Capabilities the line is certified for; empty means any (M3-03).</param>
/// <param name="PaysForThis">In the action editor: whether the line's purpose covers the editor's capability (M3-03).</param>
public sealed record FundingOption(string Id, string Type, long RemainingCents, int AvailableDay, int ExpiresDay, bool UsableToday,
    int PlannedAvailableDay = 0, IReadOnlyList<string>? Capabilities = null, bool PaysForThis = true);

/// <summary>Everything the action editor needs to assemble an award for one site and capability.</summary>
/// <param name="CardId">The requirement card being acted on, if any.</param>
/// <param name="SelectedLines">Lines switched on on the card (or the capability's core lines when there is no card).</param>
public sealed record ActionEditorView(
    int Day, string SiteId, string CapabilityId, string CapabilityLabel, string Unit, string? CardId,
    IReadOnlyList<string> SelectedLines, long PeakDemandUnits, long CashCents,
    IReadOnlyList<InstrumentOption> Instruments, IReadOnlyList<VendorOption> Vendors,
    IReadOnlyList<VehicleOption> Vehicles, IReadOnlyList<FundingOption> Funding);

/// <summary>An external support request as the OCS seat sees it (M3-05). Status: Preparing (shop work), InApproval
/// (with the SCO and the external office), Awarded, Refused. The answer day is shown as a range until it comes;
/// StartDay and EndDay are known once awarded.</summary>
public sealed record ExternalSupportRequestView(
    string Id, string SiteId, string CapabilityId, string Status, int RequestedDay, int? SubmittedDay,
    int? ExpectedAnswerMin, int? ExpectedAnswerMax, int? AwardedDay, int? StartDay, int? EndDay,
    long Quantity, long TotalCents, string FundingLineId, string? CardId, string? RefusedReason);

/// <summary>An inherited contract file the shop can review (M3-17). ReviewedDay is the day its findings came in, with
/// FindingsRecordId the Knowledge record that lists them; InReview while the review is in the shop's queue.</summary>
public sealed record InheritedFileView(
    string ArrangementId, string SiteId, string CapabilityId, string Instrument, int? ReviewedDay, bool InReview, string? FindingsRecordId);

/// <summary>
/// What an award would do if applied today, without applying it. HardStops are the same strings Apply would return
/// (the action is refused); SoftGaps are legal but carry a consequence the sim will play out.
/// </summary>
/// <param name="EarliestStartMin">Earliest first day of performance at the best roll, ignoring work already queued ahead of it.</param>
/// <param name="QueueAheadHours">Shop hours of unfinished work already in the queue.</param>
public sealed record ActionPreview(
    bool Accepted, IReadOnlyList<string> HardStops, IReadOnlyList<string> SoftGaps,
    long DailyCents, long MobilizationCents, long TotalCents,
    int ShopHoursMin, int ShopHoursMax, int EarliestStartMin, int EarliestStartMax, int QueueAheadHours);

/// <summary>
/// A transition plan as the transition planner shows it (M3-05, SPEC 3 "Transition plan"): both arrangements on a
/// timeline, the planned overlap, and the gaps it projects. Status: planned, overlapping (both running), cuttingOver
/// (termination of the outgoing in work), clean, gap.
/// </summary>
/// <param name="Handoff">"incomingDelivering" (cut over after OverlapDays of full delivery) or "onDay" (on CutoverDay).</param>
/// <param name="Owner">"cco" or "ocs": the seat that owns a gap.</param>
/// <param name="OutgoingPlannedEndDay">The outgoing's last day under the plan: its end, or the cut-over day when earlier.</param>
/// <param name="IncomingStartDay">The incoming arrangement's start day once it exists; null for a request not yet awarded.</param>
/// <param name="IncomingStartMin">Earliest projected start (equal to Max once known); null when the incoming is lost.</param>
/// <param name="IncomingLost">True when the incoming will not arrive (declined, ended, request refused).</param>
/// <param name="ProjectedGapDays">Days of gap at the earliest projected start (0 when none); null when the incoming is lost.</param>
/// <param name="FullDays">Consecutive days so far the incoming delivered at full.</param>
/// <param name="BothRunningDays">Days the outgoing and the incoming both ran.</param>
/// <param name="Warnings">Soft gaps: coverage gaps, short overlap, undersizing against peak demand, funding for a pending request.</param>
public sealed record TransitionView(
    string Id, string SiteId, string CapabilityId, string OutgoingId, string IncomingId, string Handoff, int? CutoverDay, string Owner,
    int OverlapDays, int OutgoingStartDay, int OutgoingEndDay, int OutgoingPlannedEndDay, int? IncomingStartDay,
    int? IncomingStartMin, int? IncomingStartMax, bool IncomingLost, int? ProjectedGapDays,
    int FullDays, int BothRunningDays, string Status, int PlannedDay, int? CutoverStartedDay, int? ResolvedDay,
    IReadOnlyList<string> Warnings);

/// <summary>What a transition plan would be if made today. HardStops are the strings Apply would return; Plan is null when
/// stopped.</summary>
public sealed record TransitionPreview(bool Accepted, IReadOnlyList<string> HardStops, IReadOnlyList<string> SoftGaps, TransitionView? Plan);
