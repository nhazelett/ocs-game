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
    /// <summary>Vehicles the shop has: the scenario's plus any BPAs and IDIQs established this game.</summary>
    public IReadOnlyList<VehicleData> Vehicles => _s.Vehicles;
    /// <summary>Unauthorized commitments (SPEC 4.5): handshakes and out-of-scope work, with the decision on each.</summary>
    public IReadOnlyList<CommitmentState> Commitments => _s.Commitments;

    // People (M2R-01, docs/design/opening-and-guidance.md 5). Show DisplayName and Unit, never Id.
    /// <summary>The scenario's cast, in scenario order. Empty for a scenario without one.</summary>
    public IReadOnlyList<PersonView> People => _s.BuildPeople();
    /// <summary>One person by people[] id, or null.</summary>
    public PersonView? Person(string id) => _s.BuildPerson(id);
    /// <summary>The person who brought a requirement card, or null when the card names only a free-text submitter.</summary>
    public PersonView? CardPerson(CardState card) => _s.BuildPerson(card.PersonId);
    /// <summary>A card's name on screen: what and where ("Power for Spoke B").</summary>
    public string CardTitle(CardState card) => _s.CardTitle(card);
    /// <summary>Who an unauthorized commitment comes to the desk through: the vendor's rep when the cast has one.</summary>
    public PersonView? CommitmentPerson(CommitmentState commitment) => _s.VendorRep(commitment.VendorId);
    /// <summary>Who the player hears a record from (the same rule as <see cref="Views.BoardItem.Person"/>), or null.</summary>
    public PersonView? RecordPerson(CauseRecord record) => _s.RecordPerson(record);
    /// <summary>The SCO staff officer the shop deals with, or null when the scenario names none.</summary>
    public PersonView? ScoContact => _s.BuildPerson(_s.Scenario.Actors.Sco.Person);
    /// <summary>The finance officer, or null.</summary>
    public PersonView? FinanceContact => _s.BuildPerson(_s.Scenario.Actors.Fm.Person);
    /// <summary>The site commander, or null.</summary>
    public PersonView? Commander => _s.BuildPerson(_s.Scenario.Actors.Commander.Person);
    /// <summary>The outgoing CCO who hands over the shop, or null.</summary>
    public PersonView? OutgoingCco => _s.BuildPerson(_s.Scenario.People.FirstOrDefault(p => p.Role == "outgoingCco")?.Id);
    /// <summary>Sites whose commander lost confidence in contracting (a refused handshake). Their shop work waits behind
    /// other sites' on days the shop is short (M2-14).</summary>
    public IReadOnlyCollection<string> LostConfidenceSites => _s.LostConfidenceSites;
    /// <summary>A site unit's habit of going around contracting, 0 to 1000 permille.</summary>
    public int UnitHabit(string siteId) => _s.Habit(siteId);
    /// <summary>Performance issues (M2-18): contracted vendors that under-delivered, and the cure-notice state of each.</summary>
    public IReadOnlyList<PerformanceIssueState> PerformanceIssues => _s.PerformanceIssues;
    /// <summary>What was done about an issue: cure notice and its outcome, terminated, re-sourced, or no decision.</summary>
    public string PerformanceDecision(PerformanceIssueState issue) => _s.IssueDecision(issue);
    /// <summary>True when the scenario has a COR roster: failures on contracts this office awarded reach it only through
    /// COR or unit reports (M3-04).</summary>
    public bool CorModeled => _s.CorModeled;
    /// <summary>The COR roster with each COR's current quality; a rotated COR is the relief (M3-04).</summary>
    public IReadOnlyList<CorState> Cors => _s.Cors;
    /// <summary>"fair: reports 2 days late, about 80% of problems".</summary>
    public string CorSummary(CorState cor) => _s.CorSummary(cor);
    /// <summary>Lag and report odds for a COR's current quality (rules cor.qualities).</summary>
    public CorQualityData CorQuality(CorState cor) => _s.CorQuality(cor);
    /// <summary>False for a record the office has not been told about yet: a VendorFailure waiting for a COR or unit
    /// report, which is a CorReport record (M3-04), or a hidden inherited UCA's UcaOverdue or UcaProposal notice until its
    /// file is reviewed or higher HQ asks about it (M3-17). Known as of today, so a record can become known later. The
    /// morning board already leaves these out; record lists shown to the player should too.</summary>
    public bool Known(CauseRecord record) => _s.RecordKnown(record);

    /// <summary>True while an inherited arrangement carries a problem the handover did not mention and nobody has found
    /// (M3-17). While true, show it as an ordinary contract: no UCA clocks or state (its <c>Uca</c> is not the player's to
    /// see), <see cref="ShownReliabilityPermille"/> rather than <c>ReliabilityPermille</c>, and for an IDIQ order no
    /// vehicle ceiling or ordered value (<see cref="IdiqTermsHidden"/>).</summary>
    public bool ProblemHidden(Arrangement arrangement) => _s.ProblemHidden(arrangement);

    /// <summary>The delivery record the office believes, permille: the handover's reported reliability while a hidden
    /// problem is unfound, else the true one (M3-17).</summary>
    public int ShownReliabilityPermille(Arrangement arrangement) => _s.ShownReliability(arrangement);

    /// <summary>True while an inherited order with a hidden problem keeps this IDIQ's ceiling and ordered value out of
    /// view (M3-17). <see cref="ActionEditor"/> already nulls them in its VehicleOption; read <see cref="Idiqs"/> through
    /// this.</summary>
    public bool IdiqTermsHidden(string vehicleId) => _s.IdiqTermsHidden(vehicleId);

    /// <summary>Inherited contract files the shop can review, with the day each was reviewed (M3-17). Host nation support
    /// has no file of the shop's and is not listed.</summary>
    public IReadOnlyList<InheritedFileView> InheritedFiles() => _s.BuildInheritedFiles();

    /// <summary>What a file review would take (M3-17): hard stops, shop hours (min and max equal) and in
    /// EarliestStartMin/Max the day the findings would come in, ignoring the queue ahead. Changes nothing.</summary>
    public ActionPreview PreviewReviewFiles(ReviewFilesAction review) => _s.BuildReviewFilesPreview(review);
    public IReadOnlyDictionary<string, double> Metrics => _s.Metrics;
    public long CashCents => _s.CashCents;
    public bool SystemOnline => _s.Day >= _s.SystemOnlineDay;
    public bool GpcAvailable => _s.GpcAvailableDay is { } g && _s.Day >= g;
    public int? GpcAvailableDay => _s.GpcAvailableDay;
    /// <summary>Customer small-buy stream state (design doc 6.1).</summary>
    public bool SmallBuysHeld => _s.SmallBuysHeld;
    public int SmallBuyBacklog => _s.SmallBuyBacklog;
    public int? GpcProgramDay => _s.GpcProgramDay;
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

    /// <summary>What establishing an IDIQ would take (M3-06): hard stops, shop hours, the minimum guarantee it obligates
    /// (in TotalCents) and in EarliestStartMin/Max the first day orders could be placed. Changes nothing.</summary>
    public ActionPreview PreviewEstablish(EstablishIdiqAction idiq) => _s.BuildEstablishIdiqPreview(idiq);

    /// <summary>What an external support request would take (M3-05): the same hard stops Apply would give, the soft gaps,
    /// its estimated cost (obligated by the external office at award) and in EarliestStartMin/Max the first day of
    /// performance at the best and worst roll. Changes nothing and draws no random numbers.</summary>
    public ActionPreview PreviewExternalSupport(RequestExternalSupportAction request) => _s.BuildExternalSupportPreview(request);

    /// <summary>External support requests made this game and where each stands (M3-05).</summary>
    public IReadOnlyList<ExternalSupportRequestView> ExternalSupportRequests() => _s.BuildExternalSupportRequests();

    /// <summary>The external support contract the OCS seat can request, or null when the scenario has none.</summary>
    public ExternalSupportData? ExternalSupport => _s.Scenario.Actors.ExternalSupport;

    /// <summary>What an ACSA order would take (M3-06): hard stops, soft gaps, reimbursable cost and the first day of support
    /// at the best and worst roll. Changes nothing and draws no random numbers.</summary>
    public ActionPreview PreviewAcsa(AcsaOrderAction order) => _s.BuildAcsaPreview(order);

    /// <summary>The partner force with an ACSA, or null when none is present.</summary>
    public AcsaPartnerData? AcsaPartner => _s.Scenario.Actors.AcsaPartner is { Present: true } p ? p : null;

    /// <summary>Capabilities the partner has withdrawn its ACSA support for; "*" means all.</summary>
    public IReadOnlyCollection<string> AcsaWithdrawn => _s.AcsaWithdrawn;

    /// <summary>What a host nation support request would take (M3-06): hard stops, soft gaps, the shop's cost share and the
    /// first day of support at the best and worst roll. Changes nothing and draws no random numbers.</summary>
    public ActionPreview PreviewHostNation(HostNationRequestAction request) => _s.BuildHostNationPreview(request);

    /// <summary>The host nation support agreement, or null when none exists.</summary>
    public HostNationData? HostNation => _s.Scenario.Actors.HostNation is { AgreementExists: true } h ? h : null;

    /// <summary>Capabilities the host nation has withdrawn support for; "*" means all.</summary>
    public IReadOnlyCollection<string> HostNationWithdrawn => _s.HostNationWithdrawn;

    /// <summary>Transition plans made this game and where each stands (M3-05): the timeline of both arrangements, the
    /// projected incoming start, overlap so far, and the soft gaps the planner highlights today.</summary>
    public IReadOnlyList<TransitionView> Transitions() => _s.BuildTransitions();

    /// <summary>What a transition plan would be if made today (M3-05): the same hard stops Apply would give, and the
    /// projection and soft gaps it would show. Changes nothing and draws no random numbers.</summary>
    public TransitionPreview PreviewTransition(PlanTransitionAction plan) => _s.BuildTransitionPreview(plan);

    /// <summary>IDIQs the shop can order from, with ceiling, ordered value and the minimum guarantee not yet ordered.</summary>
    public IReadOnlyList<IdiqState> Idiqs => _s.Idiqs;

    /// <summary>Vendor claims filed after terminations that skipped a contract's notice, settled or not (M3-19).</summary>
    public IReadOnlyList<ClaimState> Claims => _s.Claims;
}
