using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

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
    public long PeakDemandUnits(string siteId, string capability, int from, int to)
        => (_s.PeakDemandMilli(siteId, capability, from, to) + 999) / 1000;

    public IEnumerable<Arrangement> ArrangementsFor(string siteId, string capability)
        => _s.Arrangements.Where(a => a.SiteId == siteId && (a.CapabilityId == capability || a.SuppliedQuantityMilli.ContainsKey(capability)));

    public bool InWork(string awardId)
        => _s.Work.Any(w => w.CompletedDay is null && w.Kind == WorkKind.Award && w.Label.EndsWith(" " + awardId, StringComparison.Ordinal));
}
