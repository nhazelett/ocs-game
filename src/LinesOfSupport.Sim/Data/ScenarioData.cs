using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinesOfSupport.Sim.Data;

// Deserialization targets for scenario.json and rules-*.json (schema v0).
// These mirror scenarios/schema/*.schema.json. The schema is the contract; these types follow it.
// Doubles here are converted to integers once, at load, in Compiled*; the simulation never uses them directly.

public sealed record IntRange(int Min, int Max);

public sealed record ScenarioFile
{
    public int SchemaVersion { get; init; }
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Summary { get; init; }
    public string RulesVersion { get; init; } = "";
    public int Days { get; init; }
    public int PrepDays { get; init; }
    public int? DefaultSeed { get; init; }
    public List<IslandData> Islands { get; init; } = [];
    public List<TransportLinkData> TransportLinks { get; init; } = [];
    public List<SiteData> Sites { get; init; } = [];
    public List<RequirementCardData> RequirementCards { get; init; } = [];
    public List<VendorData> Vendors { get; init; } = [];
    public List<VehicleData> Vehicles { get; init; } = [];
    public List<InheritedArrangementData> InheritedArrangements { get; init; } = [];
    public List<FundingLineData> FundingLines { get; init; } = [];
    /// <summary>Contracting officer's representatives the units have nominated (M3-04). Empty means the scenario does not
    /// model COR reporting: every vendor failure is seen the day it happens, as before.</summary>
    public List<CorData> Cors { get; init; } = [];
    public ShopData Shop { get; init; } = new();
    public ActorsData Actors { get; init; } = new();
    public List<EventData> Events { get; init; } = [];
    public List<string> FixedSituations { get; init; } = [];
    public List<VariationData> Variation { get; init; } = [];
    public List<PassCriterionData> PassCriteria { get; init; } = [];
    public List<string> ModelAssumptions { get; init; } = [];
}

public sealed record IslandData
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsHub { get; init; }
    public bool? Grid { get; init; }
    public bool? FuelPoint { get; init; }
    public string? WaterSource { get; init; }
    public string? InfoTier { get; init; }
}

public sealed record TransportLinkData
{
    public string Id { get; init; } = "";
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public bool Bidirectional { get; init; } = true;
    public string Mode { get; init; } = "";
    public double CapacityPerTrip { get; init; }
    public List<int> ScheduleDays { get; init; } = [];
    public bool WeatherSensitive { get; init; }
    public bool PriorityBumpable { get; init; }
}

public sealed record PopulationStep(int Day, int Count);

public sealed record SiteCapabilityData
{
    public string Id { get; init; } = "";
    public double? BufferDays { get; init; }
    public double? InitialBufferDays { get; init; }
    public bool Critical { get; init; }
    public List<string> InitialArrangements { get; init; } = [];
}

public sealed record SiteData
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Island { get; init; } = "";
    public int OpensDay { get; init; }
    public int? ClosesDay { get; init; }
    public List<PopulationStep> Population { get; init; } = [];
    public double Tempo { get; init; } = 1.0;
    public List<SiteCapabilityData> Capabilities { get; init; } = [];
}

public sealed record RequirementCardData
{
    public string Id { get; init; } = "";
    public string Site { get; init; } = "";
    public string Capability { get; init; } = "";
    public int SubmittedDay { get; init; }
    public int RequestedStartDay { get; init; }
    public int DurationDays { get; init; }
    public string? QuantityHint { get; init; }
    public Dictionary<string, bool> Lines { get; init; } = [];
    public string SubmittedBy { get; init; } = "";
    public bool FlaggedEnduring { get; init; }
}

public sealed record VendorOfferData
{
    public string Capability { get; init; } = "";
    public List<string> Lines { get; init; } = [];
    public double CapacityPerDay { get; init; }
    public int LeadTimeDays { get; init; }
    public long UnitPriceCents { get; init; }
    public double MobilizationFraction { get; init; }
    /// <summary>Island the consumable stock ships from when it is not the vendor's own (a local reseller stocking from the hub). Null: the vendor's island.</summary>
    public string? SourceIsland { get; init; }
}

public sealed record VendorData
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Island { get; init; } = "";
    public List<VendorOfferData> Offers { get; init; } = [];
    public double Reliability { get; init; }
    public bool AcceptsGpc { get; init; }
    public double GpcSurchargeFraction { get; init; }
    public bool AcceptsCash { get; init; }
    public bool Vetted { get; init; }
    public string InfoTier { get; init; } = "estimated";
    public IntRange? QualifyingProposalLagDays { get; init; }
    public bool VehicleOnly { get; init; }
    /// <summary>Off-island (stateside or regional) supplier (M3-06): goods come by strategic lift to the port of entry,
    /// which is Island. Lift lists the modes it ships by ("airlift", "sealift"); empty means a local vendor.</summary>
    public List<string> Lift { get; init; } = [];
    public bool OffIsland => Lift.Count > 0;
}

public sealed record VehicleData
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Vendor { get; init; } = "";
    public List<string> ScopeTags { get; init; } = [];
    public long? CeilingCents { get; init; }
    public long ObligatedCents { get; init; }
    public long? CallLimitCents { get; init; }
    public int OrderingPeriodEndDay { get; init; }
    public List<string> PlaceOfPerformance { get; init; } = [];
    public bool OwnedByOtherService { get; init; }
    public bool VisibleOnDay1 { get; init; }
}

public sealed record UcaStateData
{
    public long NteCents { get; init; }
    public long ObligatedCents { get; init; }
    public int IssuedDay { get; init; }
    public int? QualifyingProposalDay { get; init; }
    public bool Definitized { get; init; }
    /// <summary>The price definitization settles at (M3-16); null means the daily cost over the whole period of performance.</summary>
    public long? QuotedTotalCents { get; init; }
}

public sealed record InheritedArrangementData
{
    public string Id { get; init; } = "";
    public string Instrument { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string? Vehicle { get; init; }
    public string Site { get; init; } = "";
    public string Capability { get; init; } = "";
    public List<string> Lines { get; init; } = [];
    public int StartDay { get; init; }
    public int EndDay { get; init; }
    public int? NoticeDays { get; init; }
    public UcaStateData? UcaState { get; init; }
    public string? Cor { get; init; }
    public bool VisibleProblem { get; init; } = true;
    /// <summary>Units per day (M3-16); null means the vendor's capacity for the capability.</summary>
    public double? Quantity { get; init; }
    /// <summary>Cost per day of performance (M3-16); null means quantity times the vendor's unit price (zero for host nation support).</summary>
    public long? DailyCostCents { get; init; }
    /// <summary>Obligated for performance from day 1 on, already counted against FundingLine (M3-16). For a UCA, money
    /// obligated before day 1 is ucaState.obligatedCents instead.</summary>
    public long? ObligatedCents { get; init; }
    /// <summary>The line the arrangement is funded from (M3-16); a UCA obligates as it incurs from this line.</summary>
    public string? FundingLine { get; init; }
}

/// <summary>A nominated COR (DFARS 201.602-2): who watches a contract's performance for the government (M3-04).</summary>
public sealed record CorData
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    /// <summary>Unit that nominated the COR (text only).</summary>
    public string? Unit { get; init; }
    /// <summary>A key of rules cor.qualities: sets report lag and fidelity.</summary>
    public string Quality { get; init; } = "fair";
}

public sealed record FundingLineData
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = "OM";
    public long AmountCents { get; init; }
    public int AvailableDay { get; init; }
    public int ExpiresDay { get; init; }
    /// <summary>Capabilities the line is certified for; absent or empty means any (M3-03).</summary>
    public List<string> Capabilities { get; init; } = [];
}

public sealed record ShopData
{
    public int People { get; init; } = 1;
    public int SystemOnlineDay { get; init; }
    public long CashOnHandCents { get; init; }
    public int CashReplenishLagDays { get; init; } = 5;
    public int? GpcAvailableDay { get; init; }
    public long? GpcSinglePurchaseCents { get; init; }
    public long? GpcMonthlyCents { get; init; }
    public int? PayingAgentAvailableDay { get; init; }
    public long? WarrantCents { get; init; }
}

public sealed record ScoData
{
    public string Responsiveness { get; init; } = "medium";
    public bool OffersJointBoardsUnprompted { get; init; }
    public List<string> KnownVehicles { get; init; } = [];
}

public sealed record FmData { public int CertificationLagDays { get; init; } }
public sealed record CommanderData { public List<string> PriorityOrder { get; init; } = []; }
/// <summary>A host nation support agreement (M3-06). Capacity is units per day the host nation provides per capability under
/// the agreement, shared by every request on the same days; price is the shop's cost share per unit per day (0 when the
/// host nation bears it). Sites empty means the agreement covers every site.</summary>
public sealed record HostNationData
{
    public bool AgreementExists { get; init; }
    public double? Reliability { get; init; }
    public string Name { get; init; } = "the host nation";
    public string? Island { get; init; }
    public Dictionary<string, double> Capacity { get; init; } = [];
    public Dictionary<string, long> UnitPriceCents { get; init; } = [];
    public List<string> Sites { get; init; } = [];
}
/// <summary>A partner military with an ACSA (M3-06). Stock is units per day it can spare per capability, shared by all
/// orders on the same days; price is reimbursable per unit per day.</summary>
public sealed record AcsaPartnerData
{
    public bool Present { get; init; }
    public string Name { get; init; } = "the partner force";
    public string? Island { get; init; }
    public Dictionary<string, double> Stock { get; init; } = [];
    public Dictionary<string, long> UnitPriceCents { get; init; } = [];
    public double Reliability { get; init; } = 0.97;
}

/// <summary>One capability an external support contract can provide (M3-05). Price is a scenario parameter
/// (docs/assumptions.md): unit price per unit-day, mobilization as a fraction of the task order's base cost.</summary>
public sealed record ExternalSupportOfferData
{
    public string Capability { get; init; } = "";
    /// <summary>Lines the contractor provides; a request must include the core, operator and maintenance ones.</summary>
    public List<string> Lines { get; init; } = [];
    public double CapacityPerDay { get; init; }
    public long UnitPriceCents { get; init; }
    public double MobilizationFraction { get; init; }
    public double Reliability { get; init; } = 0.97;
    /// <summary>Site ids the contract can cover; empty means any.</summary>
    public List<string> Sites { get; init; } = [];
}

/// <summary>The external support contract the OCS seat can request through the SCO (M3-05). Absent: none exists.</summary>
public sealed record ExternalSupportData
{
    /// <summary>Generic provider name; no real program is named in content (docs/assumptions.md).</summary>
    public string Provider { get; init; } = "External support contractor";
    public List<ExternalSupportOfferData> Offers { get; init; } = [];
}

public sealed record ActorsData
{
    public ScoData Sco { get; init; } = new();
    public FmData Fm { get; init; } = new();
    public CommanderData Commander { get; init; } = new();
    public HostNationData? HostNation { get; init; }
    public AcsaPartnerData? AcsaPartner { get; init; }
    public ExternalSupportData? ExternalSupport { get; init; }
}

public sealed record EventWindow(int EarliestDay, int LatestDay);

public sealed record PredicateData
{
    public string Kind { get; init; } = "";
    public string? Target { get; init; }
    public string? Comparator { get; init; }
    public JsonElement? Value { get; init; }
    public int? Days { get; init; }
}

public sealed record EffectData
{
    public string Kind { get; init; } = "";
    public string? Target { get; init; }
    public JsonElement? Value { get; init; }
    public int? Days { get; init; }
    /// <summary>Seeded alternative to Days: drawn once per session from the events stream at creation.</summary>
    public IntRange? DaysRange { get; init; }
    /// <summary>link.closed only: days of warning a survey of the link's far island buys (a forecast record the day before).</summary>
    public int? ForecastDays { get; init; }
    public double? Fraction { get; init; }
    public string? Note { get; init; }
}

public sealed record EventData
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public EventWindow Window { get; init; } = new(0, 0);
    public List<PredicateData> Preconditions { get; init; } = [];
    public List<EffectData> Effects { get; init; } = [];
    public List<string> FollowOns { get; init; } = [];
    public bool Fixed { get; init; }
    public string? PlayerVisibleAs { get; init; }
}

public sealed record VariationData
{
    public string Path { get; init; } = "";
    public string Kind { get; init; } = "";
    public double? Min { get; init; }
    public double? Max { get; init; }
    public List<JsonElement>? Choices { get; init; }
    public double? ProbabilityTrue { get; init; }
}

public sealed record PassCriterionData
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Metric { get; init; } = "";
    public string Comparator { get; init; } = "";
    public double Value { get; init; }
}

// ---------------- rules ----------------

public sealed record RulesFile
{
    public int SchemaVersion { get; init; }
    public string RulesVersion { get; init; } = "";
    public string AsOf { get; init; } = "";
    public string? Notes { get; init; }
    public ThresholdsData Thresholds { get; init; } = new();
    public UcaRulesData Uca { get; init; } = new();
    public Dictionary<string, InstrumentData> Instruments { get; init; } = [];
    public List<PrepChoiceData> PrepChoices { get; init; } = [];
    public AggregateRatesData AggregateRates { get; init; } = new();
    public ModelData Model { get; init; } = new();
    public ScoRulesData Sco { get; init; } = new();
    public UnauthorizedRulesData Unauthorized { get; init; } = new();
    public PerformanceRulesData Performance { get; init; } = new();
    public CorRulesData Cor { get; init; } = new();
    public LiftRulesData Lift { get; init; } = new();
    public TransitionRulesData Transition { get; init; } = new();
}

/// <summary>Transition plans (M3-05, SPEC 2 and 4): the overlap a plan asks for when it does not say, and how many days
/// before the outgoing arrangement ends the planner flags a projected gap on the morning board.</summary>
public sealed record TransitionRulesData
{
    public int DefaultOverlapDays { get; init; } = 2;
    public int WarnDays { get; init; } = 5;
    public string? Notes { get; init; }
}

/// <summary>Strategic lift for off-island sourcing (M3-06, SPEC 4.1 "Stateside or off-island sourcing"): an airlift queue
/// that higher-priority cargo can bump, sealift transit, and a customs hold chance on arrival at the port of entry.</summary>
public sealed record LiftRulesData
{
    public IntRange AirliftQueueDays { get; init; } = new(7, 14);
    public double AirliftBumpChance { get; init; } = 0.2;
    public IntRange AirliftBumpDelayDays { get; init; } = new(3, 7);
    public int AirliftMaxBumps { get; init; } = 2;
    public IntRange SealiftTransitDays { get; init; } = new(30, 45);
    public double CustomsHoldChance { get; init; } = 0.25;
    public IntRange CustomsHoldDays { get; init; } = new(2, 6);
}

/// <summary>COR report lag and fidelity by quality (M3-04, DFARS 201.602-2). Permille values are 0 to 1000.</summary>
public sealed record CorRulesData
{
    public Dictionary<string, CorQualityData> Qualities { get; init; } = new(StringComparer.Ordinal)
    {
        ["good"] = new() { LagDays = 0, ReportPermille = 1000 },
        ["fair"] = new() { LagDays = 2, ReportPermille = 800 },
        ["poor"] = new() { LagDays = 4, ReportPermille = 500 },
    };
    /// <summary>With no COR, a failure is reported only when it shows as a mission effect, by the unit, this many days later.</summary>
    public int NoCorLagDays { get; init; } = 3;
    /// <summary>Quality of the untrained relief who takes over when a COR rotates home.</summary>
    public string ReliefQuality { get; init; } = "poor";
    public string? SourceUrl { get; init; }
}

public sealed record CorQualityData
{
    public int LagDays { get; init; }
    public int ReportPermille { get; init; } = 1000;
}

/// <summary>Unauthorized commitments (SPEC 4.5). Permille values are 0 to 1000.</summary>
public sealed record UnauthorizedRulesData
{
    public IntRange PatienceDays { get; init; } = new(5, 15);
    public int WalkOffDeclinePermille { get; init; } = 500;
    public int WalkOffMarkupPermille { get; init; } = 250;
    public int ParkedInquiryAfterDays { get; init; } = 10;
    public int ParkedInquiryResponseHours { get; init; } = 24;
    public int RatifiedBridgeDays { get; init; } = 7;
    public int OutOfScopeDenyShopHours { get; init; } = 2;
    public int HabitStartPermille { get; init; }
    public int HabitRiseParkOrRefusePermille { get; init; } = 150;
    public int HabitRiseLongOpenPermille { get; init; } = 100;
    public int HabitLongOpenDays { get; init; } = 5;
    public int HabitDropQuickRatifyPermille { get; init; } = 100;
    public int QuickRatifyWithinDays { get; init; } = 3;
    public int HabitRiseOutOfScopeRatifyPermille { get; init; } = 50;
    public int HabitCapPermille { get; init; } = 600;
    public string? SourceUrl { get; init; }
}

/// <summary>Vendor under-delivery and cure notices (FAR 49.402-3). Permille values are 0 to 1000.</summary>
public sealed record PerformanceRulesData
{
    public int UnderDeliveryDays { get; init; } = 2;
    public int WindowDays { get; init; } = 3;
    public int CureNoticeShopHours { get; init; } = 4;
    public int CureDays { get; init; } = 10;
    public int CureRecoverPermille { get; init; } = 650;
    public int CuredReliabilityPermille { get; init; } = 980;
    public int DefaultReliabilityDropPermille { get; init; } = 100;
    public string? SourceUrl { get; init; }
}

public sealed record ThresholdsData
{
    public long MicroPurchaseCents { get; init; }
    public long SimplifiedAcquisitionCents { get; init; }
    public string Basis { get; init; } = "";
    public string? SourceUrl { get; init; }
}

public sealed record UcaRulesData
{
    public double ObligationCapBeforeProposal { get; init; }
    public double ObligationCapAfterProposal { get; init; }
    public int DefinitizeWithinDays { get; init; }
    public double DefinitizeAtObligatedFraction { get; init; }
    public IntRange ApprovalLagDays { get; init; } = new(1, 1);
    public IntRange DefinitizationShopDays { get; init; } = new(1, 1);
    public IntRange QualifyingProposalLagDays { get; init; } = new(14, 24);
    public int StopWorkAfterUnpaidDays { get; init; } = 3;
    public int HhqInquiryAfterOverdueDays { get; init; } = 5;
    public int DefaultNtePermilleOfQuote { get; init; } = 1100;
    public string? SourceUrl { get; init; }
}

public sealed record InstrumentData
{
    public string Label { get; init; } = "";
    public bool Binding { get; init; }
    public bool WrittenTerms { get; init; }
    public IntRange ShopDays { get; init; } = new(0, 0);
    public IntRange StartLagDays { get; init; } = new(0, 0);
    public bool RequiresPayingAgent { get; init; }
    public bool RequiresSystemOrManual { get; init; }
    public long? MaxActionCents { get; init; }
    public string LimitBasis { get; init; } = "none";
    public double? ObligatesMinimumGuaranteeFraction { get; init; }
    public double? DeclineProbabilityBase { get; init; }
    public IntRange? WithdrawalNoticeDays { get; init; }
    public IntRange? ApprovalLagDays { get; init; }
    public IntRange? MobilizationDays { get; init; }
    public int? MinimumUsefulDurationDays { get; init; }
    public string? Notes { get; init; }
}

public sealed record PrepChoiceData
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public int PrepDays { get; init; }
    public List<EffectData> Effects { get; init; } = [];
}

public sealed record DecayCurveData(double Start, double Floor, double HalfLifeDays);

public sealed record AggregateRatesData
{
    public DecayCurveData PaymentErrorManual { get; init; } = new(0, 0, 1);
    public DecayCurveData ExecutionConfusionNoTerms { get; init; } = new(0, 0, 1);
    public DecayCurveData BpaCallDeclineBase { get; init; } = new(0, 0, 1);
    public double VendorWillingnessDecayPerLatePayment { get; init; }
}

public sealed record CapabilityLineData
{
    public string Id { get; init; } = "";
    public string? Label { get; init; }
    public string Kind { get; init; } = "core";
    public string? Supplies { get; init; }
}

public sealed record CapabilityData
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Unit { get; init; } = "";
    public double DemandPerPersonPerDay { get; init; }
    public List<string> DependsOn { get; init; } = [];
    public string Delivery { get; init; } = "continuous";
    public List<CapabilityLineData> Lines { get; init; } = [];
}

public sealed record MissionWeightsData(double SortieGeneration, double Throughput, double Endurance, double Resilience);

public sealed record FailureRulesData
{
    public int MissingOperatorPermillePerDay { get; init; }
    public int MissingMaintenancePermillePerDayAfterGrace { get; init; }
    public int MissingMaintenanceGraceDays { get; init; }
    public int MissingMaintenanceCapPermille { get; init; }
    public int ContinuousFailureSupplyPermille { get; init; }
}

public sealed record WorkRulesData
{
    public int SurveyShopHours { get; init; }
    public int SurveyReturnDays { get; init; }
    public int RatificationShopDays { get; init; }
    public int PaymentErrorReworkHours { get; init; }
    public int HhqInquiryResponseHours { get; init; }
    public int TerminationShopDays { get; init; }
}

public sealed record ModelData
{
    public List<CapabilityData> Capabilities { get; init; } = [];
    public double DefaultBufferDays { get; init; }
    public MissionWeightsData MissionWeights { get; init; } = new(0.25, 0.25, 0.25, 0.25);
    public double HoursPerPersonPerDay { get; init; } = 8;
    public double ManualContractTimeFactor { get; init; } = 1;
    public List<string> InfoTiers { get; init; } = [];
    public FailureRulesData Failure { get; init; } = new();
    public WorkRulesData Work { get; init; } = new();
    public int SupplyLineSizingPermille { get; init; } = 1250;
    public int CashReplenishThresholdPermille { get; init; } = 500;
    public int ExecutionConfusionMinDurationDays { get; init; } = 4;
    public int DownThresholdPermille { get; init; } = 500;
    public int InvoiceIntervalDays { get; init; } = 7;
}

public sealed record ScoResponseLagData(IntRange High, IntRange Medium, IntRange Low);

public sealed record ScoRulesData
{
    public ScoResponseLagData ResponseLagDaysByResponsiveness { get; init; } = new(new(1, 1), new(1, 1), new(1, 1));
    public double TerminationShopDayFactorWithGuidance { get; init; }
    public double DisputeProbabilityWithoutGuidance { get; init; }
    public double DisputeProbabilityWithGuidance { get; init; }
    public IntRange JointBoardsStandupDays { get; init; } = new(1, 1);
    public IntRange JointBoardsRequirementLagDays { get; init; } = new(1, 1);
}

public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
    };
}
