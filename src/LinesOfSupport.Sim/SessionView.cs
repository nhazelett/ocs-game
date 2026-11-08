using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim;

/// <summary>
/// Read-only window on a session for policies, the harness and the game. Island facts are gated by what the player
/// has learned: KnownIsland returns null until a survey (or the scenario) puts the island at surveyed or better.
/// </summary>
public sealed class SessionView
{
    private readonly Session _s;
    internal SessionView(Session s) => _s = s;

    public int Day => _s.Day;
    public int LastDay => _s.LastDay;
    public bool Finished => _s.Finished;
    public ScenarioFile Scenario => _s.Scenario;
    public RulesFile Rules => _s.Rules;
    public IReadOnlyList<CardState> Cards => _s.Cards;
    public IReadOnlyList<Arrangement> Arrangements => _s.Arrangements;
    public IReadOnlyList<CapabilityState> Capabilities => _s.Capabilities;
    public IReadOnlyList<WorkItem> Work => _s.Work;
    public IReadOnlyList<FundingLineState> Funding => _s.Funding;
    public IReadOnlyList<CauseRecord> Records => _s.Records;
    /// <summary>Vehicles the shop has: the scenario's plus any BPAs established this game.</summary>
    public IReadOnlyList<VehicleData> Vehicles => _s.Vehicles;
    /// <summary>Unauthorized commitments (SPEC 4.5): handshakes and out-of-scope work, with the decision on each.</summary>
    public IReadOnlyList<CommitmentState> Commitments => _s.Commitments;
    /// <summary>Sites whose commander lost confidence in contracting (a refused handshake). Their shop work waits behind
    /// other sites' on days the shop is short (M2-14).</summary>
    public IReadOnlyCollection<string> LostConfidenceSites => _s.LostConfidenceSites;
    /// <summary>A site unit's habit of going around contracting, 0 to 1000 permille.</summary>
    public int UnitHabit(string siteId) => _s.Habit(siteId);
    /// <summary>Performance issues (M2-18): contracted vendors that under-delivered, and the cure-notice state of each.</summary>
    public IReadOnlyList<PerformanceIssueState> PerformanceIssues => _s.PerformanceIssues;
    /// <summary>What was done about an issue: cure notice and its outcome, terminated, re-sourced, or no decision.</summary>
    public string PerformanceDecision(PerformanceIssueState issue) => _s.IssueDecision(issue);
    public IReadOnlyDictionary<string, double> Metrics => _s.Metrics;
    public long CashCents => _s.CashCents;
    public bool SystemOnline => _s.Day >= _s.SystemOnlineDay;
    public bool GpcAvailable => _s.GpcAvailableDay is { } g && _s.Day >= g;
    public bool PayingAgentAvailable => _s.PayingAgentDay is { } p && _s.Day >= p;

    public string KnowledgeTier(string islandId) => _s.Knowledge.First(k => k.IslandId == islandId).Tier;
    public bool SurveyPending(string islandId) => _s.Knowledge.First(k => k.IslandId == islandId).SurveyReturnsDay is not null
                                                  || _s.Work.Any(w => w.Kind == WorkKind.Survey && w.CompletedDay is null && (string?)w.Payload == islandId);

    public IslandData? KnownIsland(string islandId)
        => KnowledgeTier(islandId) is "surveyed" or "observed" ? _s.IslandData(islandId) : null;

    public int Population(string siteId, int day) => _s.PopulationOn(_s.SiteData(siteId), day);
    /// <summary>Population from the scenario plan alone; differs from Population after an unplanned arrival's notice (M2-19).</summary>
    public int PlannedPopulation(string siteId, int day) => _s.PlannedPopulationOn(_s.SiteData(siteId), day);
    public long PeakDemandUnits(string siteId, string capability, int from, int to)
        => (_s.PeakDemandMilli(siteId, capability, from, to) + 999) / 1000;

    public IEnumerable<Arrangement> ArrangementsFor(string siteId, string capability)
        => _s.Arrangements.Where(a => a.SiteId == siteId && (a.CapabilityId == capability || a.SuppliedQuantityMilli.ContainsKey(capability)));

    public bool InWork(string awardId)
        => _s.Work.Any(w => w.CompletedDay is null && w.Kind == WorkKind.Award && w.Label.EndsWith(" " + awardId, StringComparison.Ordinal));

    // ---------------------------------------------------------------- view-model contracts (M0-07, ADR-0006)

    /// <summary>The always-visible strip: day, shop-hours and cash gauges, usable funds, system status.</summary>
    public TopStripView TopStrip() => _s.BuildTopStrip();

    /// <summary>What changed during the last AdvanceDay, alerts first, with the capability tiles and the top strip.</summary>
    public MorningBoardView MorningBoard() => _s.BuildMorningBoard();

    /// <summary>The requirement editor for one card; null if the card does not exist or has not been submitted yet.</summary>
    public RequirementEditorView? RequirementEditor(string cardId) => _s.BuildRequirementEditor(cardId);

    /// <summary>The action editor's pickers for a site and capability (optionally a card); null for an unknown site or capability.</summary>
    public ActionEditorView? ActionEditor(string siteId, string capabilityId, string? cardId = null) => _s.BuildActionEditor(siteId, capabilityId, cardId);

    /// <summary>What an award would do if applied today: the same hard stops Apply would give, the soft gaps, cost and timing.
    /// Changes nothing and draws no random numbers.</summary>
    public ActionPreview Preview(AwardAction award) => _s.BuildPreview(award);

    /// <summary>The debrief (SPEC 4): timeline, decisions with what was known, gaps tagged avoidable / discoverable /
    /// unforeseeable with their evidence, scorecard and commitments. Covers the days played so far.</summary>
    public DebriefView Debrief() => _s.BuildDebrief();

    /// <summary>What establishing a BPA would take: hard stops, shop hours, and in EarliestStartMin/Max the first day calls
    /// could be placed (ignoring the queue ahead). Changes nothing.</summary>
    public ActionPreview PreviewEstablish(EstablishBpaAction bpa) => _s.BuildEstablishPreview(bpa);
}
