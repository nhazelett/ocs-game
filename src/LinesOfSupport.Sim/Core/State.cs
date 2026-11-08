namespace LinesOfSupport.Sim.Core;

// Runtime state. Mutated only by Session. Read by policies, the harness and the game through IReadOnly views.
// Money in cents, quantities in milli-units, fractions in permille.

public enum ArrangementStatus { Pending, Active, Suspended, Ended, Declined }

public sealed class Arrangement
{
    public required string Id { get; init; }
    public required string Instrument { get; init; }
    public string? VendorId { get; init; }
    public string? VehicleId { get; init; }
    public required string SiteId { get; init; }
    public required string CapabilityId { get; init; }
    public required List<string> Lines { get; init; }
    public required string SupplierIslandId { get; init; }
    public long QuantityMilli { get; init; }
    /// <summary>Supplies lines: capability id to milli-units per day sized at award.</summary>
    public Dictionary<string, long> SuppliedQuantityMilli { get; } = [];
    public int StartDay { get; set; }
    public int EndDay { get; set; }
    public long DailyCostCents { get; init; }
    public long MobilizationCents { get; init; }
    public long ObligatedCents { get; set; }
    public long IncurredCents { get; set; }
    /// <summary>Cost incurred but not yet covered by obligation, never negative. Always zero for a
    /// coordination arrangement (no money moves). The per-arrangement term of the metric
    /// <c>cost.unobligatedIncurredCents</c>, which is the sum of this over all arrangements (Q11).</summary>
    public long UnobligatedIncurredCents => IsCoordination ? 0 : Math.Max(0, IncurredCents - ObligatedCents);
    public string? FundingLineId { get; set; }
    /// <summary>IDIQ orders: the part of ObligatedCents drawn from the vehicle's minimum guarantee, which was obligated
    /// when the IDIQ was awarded. It never returns to a funding line on termination (M3-06).</summary>
    public long FromGuaranteeCents { get; init; }
    public int ReliabilityPermille { get; set; }
    public bool Unauthorized { get; init; }
    public bool IsCoordination => Instrument == "coordination";
    public ArrangementStatus Status { get; set; } = ArrangementStatus.Pending;
    public string? CardId { get; init; }
    public int CreatedDay { get; init; }
    public string CauseId { get; init; } = "";
    /// <summary>Consumable supply from off-island waiting for the next surface link run.</summary>
    public long InTransitMilli { get; set; }
    public UcaState? Uca { get; set; }
    public int DaysActive { get; set; }
    public bool MobilizationCharged { get; set; }
    /// <summary>The COR designated on this arrangement, or null for none (M3-04).</summary>
    public string? CorId { get; set; }
}

/// <summary>A COR on the roster (M3-04). When the COR rotates home, the relief keeps the designations at a lower quality.</summary>
public sealed class CorState
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public string? Unit { get; init; }
    public required string Quality { get; set; }
    /// <summary>Day the nominated COR rotated home and an untrained relief took over, or null.</summary>
    public int? RotatedDay { get; set; }
}

/// <summary>A report of a vendor failure on its way to the office (M3-04): from the COR, or from the unit when there is none.</summary>
public sealed record CorReport(string ArrangementId, int FailureDay, int ArriveDay, string? CorId, string FailureRecordId)
{
    public string? RecordId { get; set; }
}

public sealed class UcaState
{
    public long NteCents { get; init; }
    public long QuotedTotalCents { get; init; }
    public int IssuedDay { get; init; }
    public int QualifyingProposalDay { get; init; }
    public bool ProposalReceived { get; set; }
    public bool Definitized { get; set; }
    public bool DefinitizationInWork { get; set; }
    public bool Overdue { get; set; }
    public int? HhqInquiryDay { get; set; }
    public int UnpaidDays { get; set; }
}

public sealed class CapabilityState
{
    public required string SiteId { get; init; }
    public required string CapabilityId { get; init; }
    public long BufferMilli { get; set; }
    public int BufferDaysPermille { get; init; }
    public bool Critical { get; init; }
    public int TodayPermille { get; set; } = 1000;
    public List<int> History { get; } = [];
    public int DownDays { get; set; }
    public int DegradedDays { get; set; }
    public int MinPermille { get; set; } = 1000;
    public string? OpenShortfallCause { get; set; }
}

public enum WorkKind { Award, Survey, Definitize, Ratification, PaymentRework, HhqResponse, Terminate, Denial, EstablishVehicle, CureNotice, ExternalSupportRequest, AcsaOrder, HostNationRequest }

public sealed class WorkItem
{
    public required string Id { get; init; }
    public required WorkKind Kind { get; init; }
    public required string Label { get; init; }
    public int WaitUntilDay { get; set; }
    public long HoursRemainingMilli { get; set; }
    public long HoursTotalMilli { get; init; }
    public int CreatedDay { get; init; }
    public int? CompletedDay { get; set; }
    public object? Payload { get; init; }
    public string CauseId { get; init; } = "";
}

public sealed class FundingLineState
{
    public required string Id { get; init; }
    public long AmountCents { get; init; }
    public long ObligatedCents { get; set; }
    /// <summary>The day the comptroller releases the line. A <c>funding.late</c> event can push it later (M3-03).</summary>
    public int AvailableDay { get; internal set; }
    /// <summary>The release day the scenario planned, before any delay.</summary>
    public int PlannedAvailableDay { get; init; }
    /// <summary>The fiscal year end: the last day the line can be obligated.</summary>
    public int ExpiresDay { get; init; }
    /// <summary>The capabilities the line is certified for (its purpose); empty means any (M3-03).</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];
    public long RemainingCents => AmountCents - ObligatedCents;
    public bool UsableOn(int day) => day >= AvailableDay && day <= ExpiresDay;
    /// <summary>True when the line's purpose covers the capability.</summary>
    public bool Pays(string capability) => Capabilities.Count == 0 || Capabilities.Contains(capability);
}

public sealed class CardState
{
    public required string Id { get; init; }
    public required string SiteId { get; init; }
    public required string CapabilityId { get; init; }
    public required Dictionary<string, bool> Lines { get; init; }
    public int SubmittedDay { get; init; }
    public int RequestedStartDay { get; init; }
    public int DurationDays { get; init; }
    public string? QuantityHint { get; init; }
    public string SubmittedBy { get; init; } = "";
}

/// <summary>What the player knows about an island. Surveys raise the tier; the true facts are in the scenario.</summary>
public enum CureOutcome { None, Cured, Default }

/// <summary>
/// A contracted arrangement that under-delivered on enough days to put a decision in the inbox (M2-18): issue a
/// cure notice, re-source (buy a replacement while it keeps running) or terminate. Only the cure notice is its own
/// action; re-source and terminate are ordinary awards and terminations, read back for the debrief.
/// </summary>
public sealed class PerformanceIssueState
{
    public required string Id { get; init; }
    public required string ArrangementId { get; init; }
    public required string SiteId { get; init; }
    public required string CapabilityId { get; init; }
    public string? VendorId { get; init; }
    public int OpenedDay { get; init; }
    public List<int> FailureDays { get; init; } = [];
    public int? CureNoticeDay { get; set; }
    public int? CureEndsDay { get; set; }
    public CureOutcome Outcome { get; set; }
    public string CauseId { get; init; } = "";
}

public enum CommitmentKind { Handshake, OutOfScope }

/// <summary>Where the player's decision on an unauthorized commitment stands (SPEC 4.5).</summary>
public enum CommitmentDecision { Open, Parked, Ratifying, Ratified, Refused, Denied }

/// <summary>
/// An unauthorized commitment waiting on, or past, the player's decision. A handshake (situation 1) has its own
/// arrangement with the same id; out-of-scope work (situation 2) sits on an existing contract arrangement.
/// </summary>
public sealed class CommitmentState
{
    public required string Id { get; init; }
    public required CommitmentKind Kind { get; init; }
    public required string ArrangementId { get; init; }
    public required string SiteId { get; init; }
    public required string CapabilityId { get; init; }
    public string? VendorId { get; init; }
    public int OpenedDay { get; init; }
    public CommitmentDecision Decision { get; set; } = CommitmentDecision.Open;
    public int? DecidedDay { get; set; }
    /// <summary>Handshake only: days the vendor keeps delivering unpaid before it walks off.</summary>
    public int PatienceDays { get; init; }
    public bool WalkedOff { get; set; }
    public bool InquiryDrawn { get; set; }
    public bool LongOpenCounted { get; set; }
    /// <summary>Out-of-scope only: what the vendor billed for the extra work.</summary>
    public long AmountCents { get; init; }
    public string? FundingLineId { get; set; }
    public string CauseId { get; init; } = "";
    public bool Unresolved => Decision is CommitmentDecision.Open or CommitmentDecision.Parked;
}

/// <summary>How a vendor treats this office after walking off an unpaid handshake.</summary>
public sealed class VendorStanding
{
    public bool DeclinesWork { get; init; }
    public int MarkupPermille { get; init; }
    public string CauseId { get; init; } = "";
}

public sealed class IslandKnowledge
{
    public required string IslandId { get; init; }
    public string Tier { get; set; } = "assumed";
    public int? SurveyReturnsDay { get; set; }
}

public enum RecordKind
{
    Action, Rejected, WorkDone, ArrangementStart, ArrangementEnd, Declined, Shortfall, Restored,
    VendorFailure, ExecutionConfusion, PaymentError, Event, Inbox, Knowledge, Obligation, UcaProposal,
    UcaOverdue, UcaStopWork, UcaResumed, Definitized, Compliance, Cash, Withdrawal,
    /// <summary>A vendor's orders exceed its real capacity (IDIQ orders are accepted regardless): delivery falls short (M3-06).</summary>
    VendorCapacity,
    /// <summary>The comptroller's funding clock: a line released, late, about to expire, expired, or no money usable (M3-03).</summary>
    Funding,
    /// <summary>A vendor failure reported to the office: by the COR with lag and fidelity set by quality, or by the unit (M3-04).</summary>
    CorReport,
    /// <summary>An external support request moving through the SCO and the external office: submitted, approved and
    /// awarded, or refused (M3-05).</summary>
    ExternalSupport,
    /// <summary>A transition plan's clock: a projected gap flagged, the cut-over started, or the handoff done clean or with a
    /// gap (M3-05).</summary>
    Transition,
}

public enum TransitionOutcome { Open, Clean, Gap }

/// <summary>
/// A transition plan (M3-05): outgoing arrangement to incoming arrangement or external support request, at one site and
/// capability. Resolved the day after the outgoing's last day: Clean when the incoming is delivering then (stopped work
/// and failures aside, it is running), Gap when it is not.
/// </summary>
public sealed class TransitionState
{
    public required string Id { get; init; }
    public required string OutgoingId { get; init; }
    public required string IncomingId { get; init; }
    public required string SiteId { get; init; }
    public required string CapabilityId { get; init; }
    public int OverlapDays { get; init; }
    /// <summary>"incomingDelivering" or "onDay".</summary>
    public required string Handoff { get; init; }
    public int? CutoverDay { get; init; }
    /// <summary>"cco" or "ocs": the seat that owns a gap if one opens.</summary>
    public required string Owner { get; init; }
    public int PlannedDay { get; init; }
    /// <summary>Consecutive days, up to today, the incoming delivered at full.</summary>
    public int FullDays { get; set; }
    /// <summary>Days the outgoing and the incoming were both running.</summary>
    public int BothRunningDays { get; set; }
    public int? CutoverStartedDay { get; set; }
    public string? WarningRecordId { get; set; }
    public int? WarningDay { get; set; }
    public TransitionOutcome Outcome { get; set; } = TransitionOutcome.Open;
    public int? ResolvedDay { get; set; }
    public string? ResolvedRecordId { get; set; }
    public string CauseId { get; init; } = "";
}

public enum ExternalSupportStatus { Preparing, InApproval, Awarded, Refused }

/// <summary>
/// An external support request (M3-05, SPEC 4.1): prepared by the shop, approved by the SCO and higher, awarded by an
/// external office, then mobilized. On award it becomes the arrangement with the same id (instrument
/// "externalSupport"). The approval lag and mobilization are drawn when the request is made; the player sees the
/// approval day only once the request is submitted, and the start day only once it is awarded.
/// </summary>
public sealed class ExternalSupportState
{
    public required string Id { get; init; }
    public required string SiteId { get; init; }
    public required string CapabilityId { get; init; }
    public required List<string> Lines { get; init; }
    public long QuantityMilli { get; init; }
    public int DurationDays { get; init; }
    public int? RequestedStartDay { get; init; }
    public required string FundingLineId { get; init; }
    public string? CardId { get; init; }
    public string? CorId { get; init; }
    public long DailyCostCents { get; init; }
    public long MobilizationCents { get; init; }
    public long TotalCents { get; init; }
    public int ReliabilityPermille { get; init; }
    public int RequestedDay { get; init; }
    public int ApprovalLagDays { get; init; }
    public int MobilizationDays { get; init; }
    public ExternalSupportStatus Status { get; set; } = ExternalSupportStatus.Preparing;
    public int? SubmittedDay { get; set; }
    /// <summary>The day the external office answers: submitted day plus the approval lag.</summary>
    public int? ApprovalDay { get; set; }
    public int? AwardedDay { get; set; }
    /// <summary>First day of performance, known once awarded.</summary>
    public int? StartDay { get; set; }
    public string? RefusedReason { get; set; }
    public string CauseId { get; init; } = "";
}

/// <summary>An IDIQ the shop can order from (M3-06): ceiling, minimum guarantee and what has been ordered so far.
/// Scenario IDIQs start with their data's obligatedCents as already ordered and no guarantee left to meet.</summary>
public sealed class IdiqState
{
    public required string VehicleId { get; init; }
    public string? FundingLineId { get; init; }
    public long? CeilingCents { get; init; }
    public long MinimumGuaranteeCents { get; init; }
    public long OrderedCents { get; set; }
    /// <summary>Minimum guarantee obligated at award and not yet ordered against. Owed to the vendor regardless.</summary>
    public long GuaranteeUnorderedCents => Math.Max(0, MinimumGuaranteeCents - OrderedCents);
}

/// <summary>
/// Every state change carries one of these. CausedBy points at earlier records, so the debrief can walk a chain
/// from a shortfall back to the action, omission or event that produced it.
/// </summary>
public sealed record CauseRecord(string Id, int Day, RecordKind Kind, string Subject, string Text, IReadOnlyList<string> CausedBy);
