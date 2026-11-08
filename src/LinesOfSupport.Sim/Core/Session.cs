using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

/// <summary>
/// One game. Deterministic from (scenario, rules version, seed, prep choices, input log) per ADR-0002.
/// Usage: Create, then for each day: Apply zero or more actions, then AdvanceDay, until Finished.
/// </summary>
public sealed partial class Session
{
    public ScenarioBundle Bundle { get; }
    public ScenarioFile Scenario { get; }
    public RulesFile Rules { get; }
    public long Seed { get; }
    public IReadOnlyList<string> PrepChoices { get; }

    /// <summary>The day about to be played, starting at 1. Greater than LastDay once finished.</summary>
    public int Day { get; private set; } = 1;
    public int LastDay => Scenario.Days;
    public bool Finished => Day > LastDay;

    private readonly DeterministicRandom _rngWork, _rngVendor, _rngEvents, _rngAggregate, _rngUca, _rngUnauthorized;
    private readonly List<CapabilityState> _caps = [];
    private readonly List<Arrangement> _arrangements = [];
    private readonly List<WorkItem> _work = [];
    private readonly List<FundingLineState> _funding = [];
    private readonly List<CardState> _cards = [];
    private readonly List<IslandKnowledge> _knowledge = [];
    private readonly List<CauseRecord> _records = [];
    private readonly List<PlayerAction> _applied = [];
    private readonly Dictionary<string, int> _linkClosedThrough = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (int Start, bool Fired)> _eventSchedule = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _eventRecordIds = new(StringComparer.Ordinal);
    // Seeded effect durations (effect daysRange), keyed "eventId#effectIndex"; drawn at creation (M2-19).
    private readonly Dictionary<string, int> _effectDays = new(StringComparer.Ordinal);
    private readonly HashSet<string> _forecasted = new(StringComparer.Ordinal);
    // Unplanned arrivals from site.populationJump: known to the player from the notice day on (M2-19).
    private readonly List<PopulationJump> _popJumps = [];

    internal sealed record PopulationJump(string SiteId, int NoticeDay, int ArrivalDay, int Extra, int Base, string RecordId);
    private readonly Dictionary<string, string> _linkClosureCause = new(StringComparer.Ordinal);
    private readonly List<string> _capOrder;
    private readonly Dictionary<string, CapabilityData> _capData;
    private readonly Dictionary<string, long> _demandPerPersonMilli = new(StringComparer.Ordinal);
    private readonly int[] _missionWeights;

    // Shop and money
    private readonly int _people;
    private readonly long _hoursPerDayMilli;
    private int _systemOnlineDay;
    private int _manualFactorPermille;
    private long _cash;
    private readonly long _cashInitial;
    private int _cashReplenishLagDays;
    private int? _cashReplenishDay;
    private int? _gpcAvailableDay;
    private int? _payingAgentDay;
    private long? _warrantCents;
    private readonly long _gpcSingle, _gpcMonthly;
    private readonly List<(int Day, long Cents)> _gpcSpend = [];

    // Counters for metrics
    private long _mission, _sortie, _throughput, _endurance, _resilience, _missionWeightDays;
    private int _unauthorized, _ucaOverdue, _hhqInquiries, _ratifications, _surveys, _paymentErrors,
        _confusionDays, _vendorFailures, _bpaDeclines, _eventsFired, _rejected;
    private long _hoursWorkedMilli;
    private int _overnightFrom, _overnightTo;
    private int _recordSeq, _workSeq;

    private Session(ScenarioBundle bundle, long seed, IReadOnlyList<string> prep)
    {
        Bundle = bundle;
        Seed = seed;
        Rules = bundle.Rules;
        Scenario = bundle.Instantiate(seed);
        PrepChoices = prep;
        _rngWork = DeterministicRandom.ForStream(seed, "work");
        _rngVendor = DeterministicRandom.ForStream(seed, "vendor");
        _rngEvents = DeterministicRandom.ForStream(seed, "events");
        _rngAggregate = DeterministicRandom.ForStream(seed, "aggregate");
        _rngUca = DeterministicRandom.ForStream(seed, "uca");
        _rngUnauthorized = DeterministicRandom.ForStream(seed, "unauthorized");

        _capData = Rules.Model.Capabilities.ToDictionary(c => c.Id, StringComparer.Ordinal);
        foreach (var c in Rules.Model.Capabilities) _demandPerPersonMilli[c.Id] = Fixed.Milli(c.DemandPerPersonPerDay);
        _capOrder = TopologicalOrder(Rules.Model.Capabilities);
        var w = Rules.Model.MissionWeights;
        _missionWeights = [Fixed.Permille(w.SortieGeneration), Fixed.Permille(w.Throughput), Fixed.Permille(w.Endurance), Fixed.Permille(w.Resilience)];

        var shop = Scenario.Shop;
        _people = shop.People;
        _hoursPerDayMilli = shop.People * Fixed.Milli(Rules.Model.HoursPerPersonPerDay);
        _systemOnlineDay = shop.SystemOnlineDay;
        _manualFactorPermille = Fixed.Permille(Rules.Model.ManualContractTimeFactor);
        _cash = _cashInitial = shop.CashOnHandCents;
        _cashReplenishLagDays = shop.CashReplenishLagDays;
        _gpcAvailableDay = shop.GpcAvailableDay;
        _payingAgentDay = shop.PayingAgentAvailableDay;
        _warrantCents = shop.WarrantCents;
        _gpcSingle = shop.GpcSinglePurchaseCents ?? Rules.Thresholds.MicroPurchaseCents;
        _gpcMonthly = shop.GpcMonthlyCents ?? Rules.Thresholds.MicroPurchaseCents * 5;

        ApplyPrep(prep);

        foreach (var f in Scenario.FundingLines)
            _funding.Add(new FundingLineState { Id = f.Id, AmountCents = f.AmountCents, AvailableDay = f.AvailableDay, ExpiresDay = f.ExpiresDay });
        foreach (var c in Scenario.RequirementCards)
            _cards.Add(new CardState
            {
                Id = c.Id, SiteId = c.Site, CapabilityId = c.Capability, Lines = new Dictionary<string, bool>(c.Lines, StringComparer.Ordinal),
                SubmittedDay = c.SubmittedDay, RequestedStartDay = c.RequestedStartDay, DurationDays = c.DurationDays,
                QuantityHint = c.QuantityHint, SubmittedBy = c.SubmittedBy,
            });
        foreach (var i in Scenario.Islands)
            _knowledge.Add(new IslandKnowledge { IslandId = i.Id, Tier = i.InfoTier ?? (i.IsHub ? "observed" : "assumed") });
        foreach (var site in Scenario.Sites)
            foreach (var sc in site.Capabilities)
                _caps.Add(new CapabilityState
                {
                    SiteId = site.Id, CapabilityId = sc.Id, Critical = sc.Critical,
                    BufferDaysPermille = Fixed.Permille(sc.BufferDays ?? Rules.Model.DefaultBufferDays),
                });
        foreach (var a in Scenario.InheritedArrangements) AddInherited(a);
        foreach (var e in Scenario.Events)
        {
            var start = e.Fixed ? e.Window.EarliestDay : _rngEvents.NextInt(e.Window.EarliestDay, e.Window.LatestDay);
            _eventSchedule[e.Id] = (start, false);
            for (var i = 0; i < e.Effects.Count; i++)
                if (e.Effects[i].DaysRange is { } r)
                    _effectDays[$"{e.Id}#{i}"] = _rngEvents.NextInt(r.Min, r.Max);
        }
    }

    public static Session Create(ScenarioBundle bundle, long seed, IEnumerable<string>? prepChoices = null)
        => new(bundle, seed, (prepChoices ?? []).ToList());

    // ------------------------------------------------------------------ public read surface

    public IReadOnlyList<CauseRecord> Records => _records;
    public IReadOnlyList<Arrangement> Arrangements => _arrangements;
    public IReadOnlyList<CapabilityState> Capabilities => _caps;
    public IReadOnlyList<WorkItem> Work => _work;
    public IReadOnlyList<FundingLineState> Funding => _funding;
    public IReadOnlyList<CardState> Cards => _cards;
    public IReadOnlyList<IslandKnowledge> Knowledge => _knowledge;
    public IReadOnlyList<PlayerAction> AppliedActions => _applied;
    public long CashCents => _cash;
    public int SystemOnlineDay => _systemOnlineDay;
    public int? GpcAvailableDay => _gpcAvailableDay;
    public int? PayingAgentDay => _payingAgentDay;
    public SessionView View => new(this);

    public bool LinkOpen(string linkId, int day) => !_linkClosedThrough.TryGetValue(linkId, out var through) || day > through;

    // ------------------------------------------------------------------ prep

    private void ApplyPrep(IReadOnlyList<string> prep)
    {
        if (prep.Distinct(StringComparer.Ordinal).Count() != prep.Count)
            throw new InvalidDataException($"prep choice {prep.GroupBy(p => p).First(g => g.Count() > 1).Key} is picked twice");
        var used = 0;
        foreach (var id in prep)
        {
            var choice = Rules.PrepChoices.FirstOrDefault(c => c.Id == id)
                         ?? throw new InvalidDataException($"prep choice {id} is not in rules {Rules.RulesVersion}");
            used += choice.PrepDays;
            foreach (var e in choice.Effects) Prep.Apply(this, e);
        }
        if (used > Scenario.PrepDays)
            throw new InvalidDataException($"prep choices use {used} prep-days; the scenario allows {Scenario.PrepDays}");
        if (prep.Count > 0)
            AddRecord(0, RecordKind.Action, "prep", $"Pre-deployment: {string.Join(", ", prep)} ({used} of {Scenario.PrepDays} prep-days).", []);
    }

    internal void PrepSetGpcDay(int day) => _gpcAvailableDay = _gpcAvailableDay is null ? day : Math.Min(_gpcAvailableDay.Value, day);
    internal void PrepSetPayingAgentDay(int day) => _payingAgentDay = _payingAgentDay is null ? day : Math.Min(_payingAgentDay.Value, day);
    internal void PrepSetReplenishLag(int days) => _cashReplenishLagDays = Math.Min(_cashReplenishLagDays, days);
    internal void PrepSetSystemDay(int day) => _systemOnlineDay = Math.Min(_systemOnlineDay, day);
    internal void PrepSetManualFactor(double factor) => _manualFactorPermille = Math.Min(_manualFactorPermille, Fixed.Permille(factor));
    internal void PrepSetWarrant(long? cents) => _warrantCents = cents;

    // ------------------------------------------------------------------ records

    internal string AddRecord(int day, RecordKind kind, string subject, string text, IReadOnlyList<string> causedBy)
    {
        var id = $"r{++_recordSeq}";
        _records.Add(new CauseRecord(id, day, kind, subject, text, causedBy));
        return id;
    }

    // ------------------------------------------------------------------ helpers over scenario data

    internal SiteData SiteData(string id) => Scenario.Sites.First(s => s.Id == id);
    internal VendorData? VendorData(string? id) => id is null ? null : Scenario.Vendors.FirstOrDefault(v => v.Id == id);
    internal IslandData IslandData(string id) => Scenario.Islands.First(i => i.Id == id);
    internal CapabilityData Cap(string id) => _capData[id];
    internal string SiteName(string id) => SiteData(id).Name;
    internal string CapLabel(string id) => _capData[id].Label;

    /// <summary>Population on a day, including unplanned arrivals the player has been told about (site.populationJump).</summary>
    internal int PopulationOn(SiteData site, int day)
    {
        var count = PlannedPopulationOn(site, day);
        if (count == 0) return 0;
        foreach (var j in _popJumps)
            if (j.SiteId == site.Id && j.ArrivalDay <= day) count += j.Extra;
        return count;
    }

    /// <summary>Population from the scenario plan alone (what the player could read on day 1).</summary>
    internal int PlannedPopulationOn(SiteData site, int day)
    {
        if (day < site.OpensDay || (site.ClosesDay is { } c && day > c)) return 0;
        var count = 0;
        foreach (var step in site.Population.OrderBy(p => p.Day))
            if (step.Day <= day) count = step.Count;
        return count;
    }

    /// <summary>Days for an event's effect: the seeded draw when it has a daysRange, else its days, else the fallback.</summary>
    internal int EffectDays(EventData e, int index, int fallback)
        => _effectDays.TryGetValue($"{e.Id}#{index}", out var drawn) ? drawn : e.Effects[index].Days ?? fallback;

    /// <summary>The island a vendor's goods ship from for this capability (offer sourceIsland, else the vendor's island).</summary>
    internal string StockIsland(VendorData vendor, string capability)
        => vendor.Offers.FirstOrDefault(o => o.Capability == capability)?.SourceIsland ?? vendor.Island;

    internal long DemandMilli(SiteData site, string capability, int day, bool planned = false)
    {
        var pop = planned ? PlannedPopulationOn(site, day) : PopulationOn(site, day);
        if (pop == 0) return 0;
        return pop * _demandPerPersonMilli[capability] * Fixed.Permille(site.Tempo) / 1000;
    }

    internal long PeakDemandMilli(string siteId, string capability, int from, int to)
    {
        var site = SiteData(siteId);
        long peak = 0;
        for (var d = Math.Max(1, from); d <= Math.Max(from, to); d++) peak = Math.Max(peak, DemandMilli(site, capability, d));
        return peak;
    }

    private CapabilityState? CapState(string siteId, string capId)
        => _caps.FirstOrDefault(c => c.SiteId == siteId && c.CapabilityId == capId);

    private static List<string> TopologicalOrder(List<CapabilityData> caps)
    {
        var order = new List<string>();
        var visiting = new HashSet<string>();
        var byId = caps.ToDictionary(c => c.Id);
        void Visit(string id)
        {
            if (order.Contains(id)) return;
            if (!visiting.Add(id)) throw new InvalidDataException($"capability dependency cycle at {id}");
            foreach (var d in byId[id].DependsOn) Visit(d);
            order.Add(id);
        }
        foreach (var c in caps) Visit(c.Id);
        return order;
    }

    internal static int Weekday(int day) => ((day - 1) % 7) + 1;

    /// <summary>True when a surface link between the two islands runs and is open on this day.</summary>
    internal bool SurfaceDeliveryRuns(string fromIsland, string toIsland, int day, out string? closedLink)
    {
        closedLink = null;
        if (fromIsland == toIsland) return true;
        foreach (var l in Scenario.TransportLinks)
        {
            if (l.Mode is "aircraft" or "airlift") continue;   // consumables do not fly in v0 (assumptions.md)
            var connects = (l.From == fromIsland && l.To == toIsland) || (l.Bidirectional && l.From == toIsland && l.To == fromIsland);
            if (!connects) continue;
            var scheduled = l.ScheduleDays.Count == 0 || l.ScheduleDays.Contains(Weekday(day));
            if (!scheduled) continue;
            if (!LinkOpen(l.Id, day)) { closedLink = l.Id; continue; }
            return true;
        }
        return false;
    }

    private void AddInherited(InheritedArrangementData a)
    {
        var vendor = VendorData(a.Vendor)!;
        var offer = vendor.Offers.FirstOrDefault(o => o.Capability == a.Capability);
        var qty = offer is null ? 0 : Fixed.Milli(offer.CapacityPerDay);
        var arr = new Arrangement
        {
            Id = a.Id, Instrument = a.Instrument, VendorId = a.Vendor, VehicleId = a.Vehicle, SiteId = a.Site,
            CapabilityId = a.Capability, Lines = [.. a.Lines], SupplierIslandId = StockIsland(vendor, a.Capability), QuantityMilli = qty,
            // Host-nation support is reimbursed outside the shop's funding lines (assumptions.md), so it bills nothing here.
            DailyCostCents = offer is null || a.Instrument == "hostNation" ? 0 : qty * offer.UnitPriceCents / 1000,
            ReliabilityPermille = Fixed.Permille(vendor.Reliability), CreatedDay = 0,
            StartDay = a.StartDay, EndDay = a.EndDay, Status = a.StartDay <= 1 ? ArrangementStatus.Active : ArrangementStatus.Pending,
        };
        _arrangements.Add(arr);
        AddRecord(0, RecordKind.ArrangementStart, a.Id, $"Inherited: {CapLabel(a.Capability)} at {SiteName(a.Site)} from {vendor.Name} ({a.Instrument}), days {a.StartDay} to {a.EndDay}.", []);
    }

    // ------------------------------------------------------------------ hashing and export

    /// <summary>A stable fingerprint of the outcome. Golden tests compare it; any rule change that moves it is reviewed on purpose.</summary>
    public string StateHash()
    {
        var sb = new StringBuilder();
        foreach (var (k, v) in Metrics.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            sb.Append(k).Append('=').Append(v.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        foreach (var r in _records)
            sb.Append(r.Id).Append('|').Append(r.Day).Append('|').Append(r.Kind).Append('|').Append(r.Subject).Append('|').Append(r.Text).Append('\n');
        foreach (var a in _arrangements)
            sb.Append(a.Id).Append('|').Append(a.Status).Append('|').Append(a.ObligatedCents).Append('|').Append(a.IncurredCents).Append('\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))).ToLowerInvariant();
    }

    public InputLog ToInputLog() => new()
    {
        Scenario = Scenario.Id, RulesVersion = Rules.RulesVersion, Seed = Seed,
        PrepChoices = [.. PrepChoices], Actions = [.. _applied],
    };
}
