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

public enum WorkKind { Award, Survey, Definitize, Ratification, PaymentRework, HhqResponse, Terminate, Denial, EstablishVehicle, CureNotice }

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
    public int AvailableDay { get; init; }
    public int ExpiresDay { get; init; }
    public long RemainingCents => AmountCents - ObligatedCents;
    public bool UsableOn(int day) => day >= AvailableDay && day <= ExpiresDay;
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
}

/// <summary>
/// Every state change carries one of these. CausedBy points at earlier records, so the debrief can walk a chain
/// from a shortfall back to the action, omission or event that produced it.
/// </summary>
public sealed record CauseRecord(string Id, int Day, RecordKind Kind, string Subject, string Text, IReadOnlyList<string> CausedBy);
