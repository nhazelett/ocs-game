namespace LinesOfSupport.Sim.Views;

// Debrief view-model contract (backlog M2-08, ADR-0006 addendum). SPEC 4 "Debrief logic": timeline, decisions,
// gaps with tags, scorecard. The counterfactual view (M3) re-runs from an input log and needs no new contract here.

/// <summary>SPEC 4: avoidable (the information existed), discoverable (available for a cost), unforeseeable (no path to know).</summary>
public enum GapTag { Unforeseeable, Discoverable, Avoidable }

/// <summary>What contributed to a gap. Each kind maps to one tag rule (see docs/assumptions.md, Debrief tags).</summary>
public enum GapFactorKind
{
    Dependency, MissingSupplyLine, Uncovered, CoordinationWithdrawn, HandshakeLost, OffIslandDependency,
    DemandAboveSupply, VendorFailure, StopWork, ExecutionConfusion, BpaDeclined,
}

/// <summary>One contributor to a gap, with its own tag, the day it first applied and the evidence in plain words.</summary>
/// <param name="DependsOnGap">For Dependency: index into DebriefView.Gaps of the gap this one inherits.</param>
/// <param name="RecordIds">Cause records behind the factor (actions, events, failures), for linking.</param>
public sealed record GapFactor(GapFactorKind Kind, GapTag Tag, int FirstDay, string Text, IReadOnlyList<string> RecordIds, int? DependsOnGap = null);

/// <summary>One gap: consecutive days a capability at a site ran below 100%.</summary>
/// <param name="Tag">The most controllable factor's tag: avoidable beats discoverable beats unforeseeable.</param>
/// <param name="ShortfallRecordIds">Shortfall records in the window (their CausedBy chains are the raw trace).</param>
public sealed record GapView(
    int Index, string SiteId, string CapabilityId, string Label, int StartDay, int EndDay, int LowestPermille, int DownDays,
    GapTag Tag, IReadOnlyList<GapFactor> Factors, IReadOnlyList<string> ShortfallRecordIds);

/// <summary>A capability's level by day. PermilleByDay[0] is day 1; -1 means the site was not open that day.</summary>
public sealed record CapabilityBand(string SiteId, string CapabilityId, string Label, IReadOnlyList<int> PermilleByDay);

/// <summary>Something to annotate on the timeline.</summary>
public sealed record TimelineEvent(int Day, string Kind, string Subject, string Text, string RecordId);

/// <summary>An island's information tier as of a decision.</summary>
public sealed record KnownIsland(string IslandId, string Tier);

/// <summary>A player action with what was known when it was taken.</summary>
public sealed record DecisionEntry(int Day, string RecordId, string Text, bool Rejected, IReadOnlyList<KnownIsland> Known);

/// <summary>SPEC 4 scorecard. Fractions are 0 to 1.</summary>
public sealed record ScorecardView(
    double MissionOutput, double SortieGeneration, double Throughput, double Endurance, double Resilience,
    long ObligatedCents, long IncurredCents, long UnobligatedIncurredCents,
    int UnauthorizedCommitments, int UcaOverdue, int HhqInquiries, int Surveys, int ShopHoursWorked,
    int GapCount, int AvoidableGaps, int DiscoverableGaps, int UnforeseeableGaps);

/// <summary>An unauthorized commitment and how it was decided (ADR-0007).</summary>
public sealed record CommitmentLine(string Id, string Kind, int OpenedDay, string Decision, int? DecidedDay, bool WalkedOff, bool InquiryDrawn, string Text);

/// <summary>The debrief. Valid mid-game (covers days played so far) and at the end.</summary>
public sealed record DebriefView(
    string ScenarioTitle, int DaysPlayed, int LastDay,
    IReadOnlyList<CapabilityBand> Timeline, IReadOnlyList<TimelineEvent> Events,
    IReadOnlyList<DecisionEntry> Decisions, IReadOnlyList<GapView> Gaps,
    ScorecardView Scorecard, IReadOnlyList<CommitmentLine> Commitments);
