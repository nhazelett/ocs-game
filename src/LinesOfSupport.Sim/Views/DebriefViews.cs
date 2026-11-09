namespace LinesOfSupport.Sim.Views;

// Debrief view-model contract (backlog M2-08, ADR-0006 addendum). SPEC 4 "Debrief logic": timeline, decisions,
// gaps with tags, scorecard. The counterfactual view (M3-02) is in Counterfactual.cs: it re-runs a changed input log
// and compares two of these.

/// <summary>SPEC 4: avoidable (the information existed), discoverable (available for a cost), unforeseeable (no path to know).</summary>
public enum GapTag { Unforeseeable, Discoverable, Avoidable }

/// <summary>What contributed to a gap. Each kind maps to one tag rule (see docs/assumptions.md, Debrief tags).</summary>
public enum GapFactorKind
{
    Dependency, MissingSupplyLine, Uncovered, CoordinationWithdrawn, HandshakeLost, OffIslandDependency,
    DemandAboveSupply, VendorFailure, StopWork, ExecutionConfusion, BpaDeclined, PriorityDeferred,
    PopulationJump, VendorCapacity,
    /// <summary>No funding line with money could pay for the capability that day (M3-03): an expiry, a late replacement, or money spent.</summary>
    FundingLapse,
    /// <summary>The partner withdrew an ACSA order and nothing contractual was behind it (M3-06).</summary>
    AcsaWithdrawn,
    /// <summary>The host nation withdrew support (requested or inherited) and nothing contractual was behind it (M3-06).</summary>
    HostNationWithdrawn,
    /// <summary>An off-island delivery was bumped from the airlift or held in customs past its due day (M3-06).</summary>
    LiftDelay,
    /// <summary>A transition plan's outgoing arrangement ended before the incoming one was delivering (M3-05).</summary>
    TransitionGap,
    /// <summary>The capability has a usual owner (fuel: the J4 through the ACSA partner) who was never engaged, or engaged too late, and no stopgap was bought (M2R-06, R10).</summary>
    OwnerNotEngaged,
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
    int GapCount, int AvoidableGaps, int DiscoverableGaps, int UnforeseeableGaps,
    int Claims = 0, long ClaimCents = 0);

/// <summary>An unauthorized commitment and how it was decided (ADR-0007).</summary>
public sealed record CommitmentLine(string Id, string Kind, int OpenedDay, string Decision, int? DecidedDay, bool WalkedOff, bool InquiryDrawn, string Text);

/// <summary>The debrief. Valid mid-game (covers days played so far) and at the end.</summary>
public sealed record DebriefView(
    string ScenarioTitle, int DaysPlayed, int LastDay,
    IReadOnlyList<CapabilityBand> Timeline, IReadOnlyList<TimelineEvent> Events,
    IReadOnlyList<DecisionEntry> Decisions, IReadOnlyList<GapView> Gaps,
    ScorecardView Scorecard, IReadOnlyList<CommitmentLine> Commitments,
    IReadOnlyList<ClaimLine>? Claims = null, DebriefSummary? Summary = null);

/// <summary>The debrief's opening paragraph in a person's words (M2R-27, R9): the SCO's staff, else the commander; null
/// From when the cast has neither. Text is prose, no lists or labels. LeadGap indexes Gaps for the gap the paragraph is about
/// (null when there were no gaps), so the game can open that gap's evidence under it. Quoted (M2R-29) indexes that gap's
/// Factors for the factors whose text the paragraph already says word for word, in the order said, so the evidence under it
/// can show those by their tag line alone and the player reads each sentence once; empty when there is no lead gap.</summary>
public sealed record DebriefSummary(PersonView? From, string Text, int? LeadGap, IReadOnlyList<int>? Quoted = null)
{
    /// <summary>Never null: an empty list when the paragraph quoted nothing.</summary>
    public IReadOnlyList<int> Quoted { get; init; } = Quoted ?? [];
}

/// <summary>Plain words for the debrief's labels (M2R-27), so no enum name reaches the screen split into words.</summary>
public static class DebriefWords
{
    /// <summary>A short label for what contributed to a gap.</summary>
    public static string Factor(GapFactorKind kind) => kind switch
    {
        GapFactorKind.Dependency => "Went down with another gap",
        GapFactorKind.MissingSupplyLine => "A supply line was left off",
        GapFactorKind.Uncovered => "Nothing covered it",
        GapFactorKind.CoordinationWithdrawn => "A coordination fell through",
        GapFactorKind.HandshakeLost => "A handshake fell through",
        GapFactorKind.OffIslandDependency => "It came across the water",
        GapFactorKind.DemandAboveSupply => "Bought too small for the demand",
        GapFactorKind.VendorFailure => "The vendor fell down",
        GapFactorKind.StopWork => "Work stopped on an overdue contract",
        GapFactorKind.ExecutionConfusion => "Confusion on the ground",
        GapFactorKind.BpaDeclined => "The vendor turned down a BPA call",
        GapFactorKind.PriorityDeferred => "Put off for something else",
        GapFactorKind.PopulationJump => "More people arrived",
        GapFactorKind.VendorCapacity => "The vendor ran out of capacity",
        GapFactorKind.FundingLapse => "No money to pay for it",
        GapFactorKind.AcsaWithdrawn => "The partner pulled its support",
        GapFactorKind.HostNationWithdrawn => "The host nation pulled its support",
        GapFactorKind.LiftDelay => "Held up in the airlift or customs",
        GapFactorKind.TransitionGap => "A hand-off left a gap",
        GapFactorKind.OwnerNotEngaged => "Nobody owned it",
        _ => "Another cause",
    };

    /// <summary>What a tag means, in one sentence (SPEC 4).</summary>
    public static string TagMeaning(GapTag tag) => tag switch
    {
        GapTag.Avoidable => "Avoidable: what you needed to know was in front of you.",
        GapTag.Discoverable => "Discoverable: you could have found it out, at a cost.",
        _ => "Unforeseeable: nobody could have known.",
    };

    private static readonly string[] SmallNumbers =
        ["no", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve"];

    private static readonly string[] Tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];

    /// <summary>A number as a person would say it: words up to ninety-nine, digits above.</summary>
    public static string Number(int n) => n switch
    {
        >= 0 and < 13 => SmallNumbers[n],
        >= 13 and < 20 => n switch { 13 => "thirteen", 15 => "fifteen", 18 => "eighteen", _ => SmallNumbers[n - 10] + "teen" },
        >= 20 and < 100 => n % 10 == 0 ? Tens[n / 10] : $"{Tens[n / 10]}-{SmallNumbers[n % 10]}",
        _ => n.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    /// <summary>A timeline entry's kind (TimelineEvent.Kind) as a word or two.</summary>
    public static string TimelineKind(string kind) => kind switch
    {
        "Event" => "News",
        "Withdrawal" => "Support withdrawn",
        "UcaOverdue" => "Contract overdue",
        "UcaStopWork" => "Work stopped",
        "UcaResumed" => "Work resumed",
        "Compliance" => "Compliance",
        "Declined" => "Turned down",
        "Definitized" => "Contract definitized",
        "ArrangementStart" => "Started",
        "ArrangementEnd" => "Ended",
        "Knowledge" => "Learned",
        "UcaProposal" => "Proposal in",
        "VendorCapacity" => "Vendor at capacity",
        "Funding" => "Money",
        "Transition" => "Hand-off",
        _ => "Update",
    };
}

/// <summary>A vendor claim after a termination without the contract's notice (M3-19): filed day, amount, settled day
/// (null while the shop has not settled it) and the filing record's text.</summary>
public sealed record ClaimLine(string ArrangementId, int FiledDay, long AmountCents, int? SettledDay, string Text);
