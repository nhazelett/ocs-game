using System.Text.Json;
using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Data;

/// <summary>
/// A scenario plus the rules it names, loaded from disk. Immutable; variation is applied per seed by
/// <see cref="Instantiate"/>, which returns a new <see cref="ScenarioFile"/> for that seed.
/// </summary>
public sealed class ScenarioBundle
{
    public string Directory { get; }
    public ScenarioFile Scenario { get; }
    public RulesFile Rules { get; }
    private readonly JsonNode _scenarioNode;

    private ScenarioBundle(string directory, JsonNode scenarioNode, ScenarioFile scenario, RulesFile rules)
    {
        Directory = directory;
        _scenarioNode = scenarioNode;
        Scenario = scenario;
        Rules = rules;
    }

    /// <summary>
    /// Loads scenarioDir/scenario.json and rulesDir/rules-{rulesVersion}.json.
    /// rulesDir defaults to the sibling "rules" folder of the scenario's parent (scenarios/rules).
    /// Schema validation is the harness's and CI's job; this loader checks what the engine depends on.
    /// </summary>
    public static ScenarioBundle Load(string scenarioDir, string? rulesDir = null)
    {
        var dir = Path.GetFullPath(scenarioDir);
        if (File.Exists(dir) && dir.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            dir = Path.GetDirectoryName(dir)!;
        var scenarioPath = Path.Combine(dir, "scenario.json");
        if (!File.Exists(scenarioPath))
            throw new FileNotFoundException($"No scenario.json in {dir}");
        var node = JsonNode.Parse(File.ReadAllText(scenarioPath))
                   ?? throw new InvalidDataException($"{scenarioPath} is empty");
        var scenario = node.Deserialize<ScenarioFile>(Json.Options)
                       ?? throw new InvalidDataException($"{scenarioPath} did not deserialize");

        rulesDir ??= Path.Combine(Path.GetDirectoryName(dir)!, "rules");
        var rulesPath = Path.Combine(rulesDir, $"rules-{scenario.RulesVersion}.json");
        if (!File.Exists(rulesPath))
            throw new FileNotFoundException($"Scenario {scenario.Id} names rulesVersion {scenario.RulesVersion}, but {rulesPath} does not exist");
        var rules = JsonSerializer.Deserialize<RulesFile>(File.ReadAllText(rulesPath), Json.Options)
                    ?? throw new InvalidDataException($"{rulesPath} did not deserialize");
        if (rules.RulesVersion != scenario.RulesVersion)
            throw new InvalidDataException($"{rulesPath} declares rulesVersion {rules.RulesVersion}, expected {scenario.RulesVersion}");

        var bundle = new ScenarioBundle(dir, node, scenario, rules);
        bundle.CheckReferences(scenario);
        return bundle;
    }

    /// <summary>Applies the scenario's variation ranges for this seed. Fixed situations are never touched.</summary>
    public ScenarioFile Instantiate(long seed)
    {
        if (Scenario.Variation.Count == 0) return Scenario;
        var rng = DeterministicRandom.ForStream(seed, "variation");
        var node = _scenarioNode.DeepClone();
        var fixedIds = Scenario.FixedSituations.ToHashSet(StringComparer.Ordinal);
        foreach (var v in Scenario.Variation)
        {
            var (parent, key) = Resolve(node, v.Path);
            if (parent is null) throw new InvalidDataException($"variation path {v.Path} does not resolve");
            if (TouchesFixed(node, v.Path, fixedIds)) continue;
            JsonNode? value = v.Kind switch
            {
                "intRange" => JsonValue.Create(rng.NextInt((int)(v.Min ?? 0), (int)(v.Max ?? 0))),
                // Drawn as a whole number of thousandths so the result is reproducible to the digit.
                "numberRange" => JsonValue.Create(DrawThousandths(rng, v.Min ?? 0, v.Max ?? 0)),
                "boolean" => JsonValue.Create(rng.Roll(Fixed.Permille(v.ProbabilityTrue ?? 0.5))),
                "choice" => v.Choices is { Count: > 0 } c ? JsonNode.Parse(c[rng.NextInt(0, c.Count - 1)].GetRawText()) : null,
                _ => throw new InvalidDataException($"variation kind {v.Kind} is not implemented"),
            };
            if (value is null) continue;
            Set(parent, key, value);
        }
        return node.Deserialize<ScenarioFile>(Json.Options)!;
    }

    private static double DrawThousandths(DeterministicRandom rng, double min, double max)
    {
        var lo = Fixed.Milli(min);
        var hi = Fixed.Milli(max);
        return rng.NextInt((int)lo, (int)hi) / 1000.0;
    }

    private static bool TouchesFixed(JsonNode root, string path, HashSet<string> fixedIds)
    {
        // A path like /vendors/2/reliability is fixed when /vendors/2/id is in fixedSituations.
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        JsonNode? cursor = root;
        foreach (var p in parts)
        {
            if (cursor is JsonObject o && o["id"] is JsonValue idv && idv.TryGetValue<string>(out var id) && fixedIds.Contains(id))
                return true;
            cursor = cursor switch
            {
                JsonObject obj => obj[Unescape(p)],
                JsonArray arr when int.TryParse(p, out var i) && i >= 0 && i < arr.Count => arr[i],
                _ => null,
            };
            if (cursor is null) return false;
        }
        return false;
    }

    private static (JsonNode? parent, string key) Resolve(JsonNode root, string pointer)
    {
        var parts = pointer.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Unescape).ToArray();
        if (parts.Length == 0) return (null, "");
        JsonNode? cursor = root;
        for (var i = 0; i < parts.Length - 1 && cursor is not null; i++)
        {
            cursor = cursor switch
            {
                JsonObject o => o[parts[i]],
                JsonArray a when int.TryParse(parts[i], out var idx) && idx >= 0 && idx < a.Count => a[idx],
                _ => null,
            };
        }
        return (cursor, parts[^1]);
    }

    private static void Set(JsonNode parent, string key, JsonNode value)
    {
        switch (parent)
        {
            case JsonObject o: o[key] = value; break;
            case JsonArray a when int.TryParse(key, out var i) && i >= 0 && i < a.Count: a[i] = value; break;
            default: throw new InvalidDataException($"cannot set {key}");
        }
    }

    private static string Unescape(string s) => s.Replace("~1", "/").Replace("~0", "~");

    private void CheckReferences(ScenarioFile s)
    {
        var caps = Rules.Model.Capabilities.Select(c => c.Id).ToHashSet();
        var islands = s.Islands.Select(i => i.Id).ToHashSet();
        var sites = s.Sites.Select(i => i.Id).ToHashSet();
        var vendors = s.Vendors.Select(i => i.Id).ToHashSet();
        void Need(bool ok, string what) { if (!ok) throw new InvalidDataException($"{s.Id}: {what}"); }
        foreach (var site in s.Sites)
        {
            Need(islands.Contains(site.Island), $"site {site.Id} island {site.Island} not found");
            foreach (var c in site.Capabilities) Need(caps.Contains(c.Id), $"site {site.Id} capability {c.Id} not in rules");
        }
        foreach (var card in s.RequirementCards)
        {
            Need(sites.Contains(card.Site), $"card {card.Id} site not found");
            var cap = Rules.Model.Capabilities.FirstOrDefault(c => c.Id == card.Capability);
            Need(cap is not null, $"card {card.Id} capability not in rules");
            foreach (var line in card.Lines.Keys)
                Need(cap!.Lines.Any(l => l.Id == line), $"card {card.Id} line {line} is not a {card.Capability} line in rules");
        }
        foreach (var v in s.Vendors)
        {
            Need(islands.Contains(v.Island), $"vendor {v.Id} island not found");
            foreach (var m in v.Lift) Need(m is "airlift" or "sealift", $"vendor {v.Id} lift mode {m} is not airlift or sealift");
            foreach (var o in v.Offers)
            {
                var cap = Rules.Model.Capabilities.FirstOrDefault(c => c.Id == o.Capability);
                Need(cap is not null, $"vendor {v.Id} capability {o.Capability} not in rules");
                foreach (var line in o.Lines)
                    Need(cap!.Lines.Any(l => l.Id == line), $"vendor {v.Id} line {line} is not a {o.Capability} line");
                if (o.SourceIsland is { } src)
                {
                    Need(islands.Contains(src), $"vendor {v.Id} {o.Capability} sourceIsland {src} not found");
                    Need(cap?.Delivery == "consumable", $"vendor {v.Id} {o.Capability}: sourceIsland applies only to consumables");
                }
            }
        }
        foreach (var veh in s.Vehicles) Need(vendors.Contains(veh.Vendor), $"vehicle {veh.Id} vendor not found");
        foreach (var f in s.FundingLines)
        {
            foreach (var c in f.Capabilities) Need(caps.Contains(c), $"funding line {f.Id} capability {c} not in rules");
            Need(f.AvailableDay <= f.ExpiresDay, $"funding line {f.Id} expires before it is released");
        }
        // The team (DESK-02): exactly one "you", unique ids, hours and a pace above zero.
        if (s.Shop.Team is { } team)
        {
            Need(team.Count > 0, "shop.team is empty");
            Need(team.Count(m => m.Role == "you") == 1, "shop.team needs exactly one member with role you");
            Need(team.Select(m => m.Id).Distinct(StringComparer.Ordinal).Count() == team.Count, "shop.team ids are not unique");
            foreach (var m in team)
            {
                Need(m.Role is "you" or "experienced" or "junior", $"team member {m.Id} role {m.Role} is not you, experienced or junior");
                Need(m.Name.Length > 0, $"team member {m.Id} has no name");
                Need(m.HoursPerDay > 0, $"team member {m.Id} needs hours a day");
                Need(m.Speed > 0, $"team member {m.Id} speed must be above zero");
            }
        }
        // People (M2R-01): unique ids, known roles, and every reference names a person in the right role.
        var people = new Dictionary<string, PersonData>(StringComparer.Ordinal);
        foreach (var p in s.People) Need(people.TryAdd(p.Id, p), $"person id {p.Id} is not unique");
        string[] roles = ["customer", "outgoingCco", "scoStaff", "finance", "commander", "vendorRep"];
        foreach (var p in s.People)
        {
            Need(roles.Contains(p.Role), $"person {p.Id} role {p.Role} is not one of {string.Join(", ", roles)}");
            Need(p.Name.Length > 0, $"person {p.Id} has no name");
            if (p.Vendor is not null) Need(vendors.Contains(p.Vendor), $"person {p.Id} vendor {p.Vendor} not found");
            Need(p.Role != "vendorRep" || p.Vendor is not null, $"person {p.Id} is a vendorRep with no vendor");
            if (p.Site is not null) Need(sites.Contains(p.Site), $"person {p.Id} site {p.Site} not found");
        }
        void NeedPerson(string? id, string what, string role)
        {
            if (id is null) return;
            Need(people.TryGetValue(id, out var p), $"{what} person {id} not found");
            if (p is not null) Need(p.Role == role, $"{what} person {id} must have role {role}, has {p.Role}");
        }
        foreach (var c in s.RequirementCards)
        {
            if (c.Person is not null) Need(people.ContainsKey(c.Person), $"card {c.Id} person {c.Person} not found");
            Need(c.Person is not null || c.SubmittedBy.Length > 0, $"card {c.Id} names neither a person nor submittedBy");
            foreach (var (line, k) in c.KnownBy)
            {
                Need(c.Lines.ContainsKey(line), $"card {c.Id} knownBy line {line} is not one of its lines");
                Need(people.ContainsKey(k.Person), $"card {c.Id} knownBy person {k.Person} not found");
                Need(k.Says.Length > 0, $"card {c.Id} knownBy {line} says nothing");
            }
            if (c.HeardAtStaffMeeting is { } h)
            {
                Need(h.FromDay >= 1 && h.FromDay < c.SubmittedDay, $"card {c.Id} heardAtStaffMeeting.fromDay must be at least 1 and before submittedDay");
                if (h.By is not null) Need(people.ContainsKey(h.By), $"card {c.Id} heardAtStaffMeeting.by person {h.By} not found");
                Need(h.Says.Length > 0, $"card {c.Id} heardAtStaffMeeting says nothing");
            }
        }
        foreach (var e in s.Events)
            if (e.From is not null) Need(people.ContainsKey(e.From), $"event {e.Id} from {e.From} not found");
        NeedPerson(s.Actors.Sco.Person, "actors.sco", "scoStaff");
        NeedPerson(s.Actors.Fm.Person, "actors.fm", "finance");
        NeedPerson(s.Actors.Commander.Person, "actors.commander", "commander");

        // Turnover and prep briefing (M2R-02).
        if (s.Turnover is { } t)
        {
            NeedPerson(t.From, "turnover.from", "outgoingCco");
            // A remark whose item is absent is never shown, so it is not a load error: derived fixtures (the game's quiet
            // smoke scenario) strip inherited arrangements and keep the rest. tools/validate/validate.py flags such
            // remarks for authors (M2R-18).
        }
        if (s.PrepBriefing is { } pb)
        {
            Need(people.ContainsKey(pb.From), $"prepBriefing.from person {pb.From} not found");
            foreach (var k in pb.Choices.Keys.Concat(pb.Defaults))
                Need(Rules.PrepChoices.Any(c => c.Id == k), $"prepBriefing choice {k} is not in rules {Rules.RulesVersion}");
            var used = pb.Defaults.Distinct().Sum(k => Rules.PrepChoices.FirstOrDefault(c => c.Id == k)?.PrepDays ?? 0);
            Need(used <= s.PrepDays, $"prepBriefing.defaults use {used} prep-days; the scenario allows {s.PrepDays}");
        }

        var cors = s.Cors.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        Need(cors.Count == s.Cors.Count, "COR ids must be unique");
        foreach (var c in s.Cors) Need(Rules.Cor.Qualities.ContainsKey(c.Quality), $"COR {c.Id} quality {c.Quality} not in rules cor.qualities");
        Need(Rules.Cor.Qualities.ContainsKey(Rules.Cor.ReliefQuality), $"rules cor.reliefQuality {Rules.Cor.ReliefQuality} not in cor.qualities");
        Need(Rules.FileReview.HoursPerArrangement >= 0, "rules fileReview.hoursPerArrangement must not be negative");
        Need(Rules.Claims.SettleShopHours >= 0 && Rules.Claims.SettlementDays >= 0, "rules claims values must not be negative");
        foreach (var a in s.InheritedArrangements)
        {
            if (a.Cor is not null) Need(cors.Contains(a.Cor), $"arrangement {a.Id} COR {a.Cor} not found");
            Need(Rules.Instruments.ContainsKey(a.Instrument), $"arrangement {a.Id} instrument {a.Instrument} not in rules");
            Need(vendors.Contains(a.Vendor), $"arrangement {a.Id} vendor not found");
            Need(sites.Contains(a.Site), $"arrangement {a.Id} site not found");
            Need(a.StartDay <= a.EndDay, $"arrangement {a.Id} ends before it starts");
            if (a.Quantity is { } q) Need(q > 0, $"arrangement {a.Id} quantity must be positive");
            if (a.DailyCostCents is { } dc) Need(dc >= 0, $"arrangement {a.Id} dailyCostCents must not be negative");
            if (a.FundingLine is not null) Need(s.FundingLines.Any(f => f.Id == a.FundingLine), $"arrangement {a.Id} funding line {a.FundingLine} not found");
            if (a.ObligatedCents is > 0) Need(a.FundingLine is not null, $"arrangement {a.Id} has obligatedCents but no fundingLine");
            if (a.NoticeDays is { } nd) Need(nd >= 0, $"arrangement {a.Id} noticeDays must not be negative");
            if (a.ReportedReliability is { } rr)
            {
                Need(rr is >= 0 and <= 1, $"arrangement {a.Id} reportedReliability must be between 0 and 1");
                Need(!a.VisibleProblem, $"arrangement {a.Id} has reportedReliability but visibleProblem is not false");
            }
            if (!a.VisibleProblem) Need(a.Instrument != "hostNation", $"arrangement {a.Id}: host nation support has no contract file to review, so its problem cannot be hidden");
            if (a.UcaState is { } u)
            {
                Need(a.Instrument == "uca", $"arrangement {a.Id} has ucaState but instrument {a.Instrument}");
                Need(u.NteCents > 0 && u.ObligatedCents <= u.NteCents, $"arrangement {a.Id} ucaState obligatedCents must be within a positive nteCents");
                Need(u.QualifyingProposalDay is null || u.QualifyingProposalDay >= u.IssuedDay, $"arrangement {a.Id} qualifying proposal before the UCA was issued");
                Need(u.Definitized || a.FundingLine is not null, $"arrangement {a.Id}: an undefinitized UCA needs a fundingLine to obligate from");
            }
        }
        foreach (var e in s.Events)
        {
            foreach (var p in e.Preconditions)
            {
                Need(Events.KnownPredicates.Contains(p.Kind), $"event {e.Id}: predicate kind {p.Kind} is not implemented");
                if (p.Kind is "arrangement.noCureNotice" or "arrangement.notReplaced")
                    Need(s.InheritedArrangements.Any(a => a.Id == p.Target), $"event {e.Id}: {p.Kind} target {p.Target} is not an inherited arrangement");
            }
            foreach (var f in e.Effects)
            {
                Need(Events.KnownEffects.Contains(f.Kind), $"event {e.Id}: effect kind {f.Kind} is not implemented");
                if (f.DaysRange is { } r) Need(r.Min <= r.Max, $"event {e.Id}: {f.Kind} daysRange max is below min");
                if (f.Kind == "vendor.declinesCalls")
                    Need(s.Vendors.Any(v => v.Id == f.Target), $"event {e.Id}: vendor.declinesCalls target {f.Target} is not a vendor");
                if (f.Kind == "vendor.reliabilityDrop")
                {
                    Need(s.InheritedArrangements.Any(a => a.Id == f.Target), $"event {e.Id}: vendor.reliabilityDrop target {f.Target} is not an inherited arrangement");
                    Need(f.Value is { ValueKind: System.Text.Json.JsonValueKind.Number } rv && rv.GetDouble() is >= 0 and <= 1,
                         $"event {e.Id}: vendor.reliabilityDrop value must be a reliability between 0 and 1");
                }
                if (f.ForecastDays is not null) Need(f.Kind == "link.closed", $"event {e.Id}: forecastDays applies only to link.closed");
                if (f.Kind == "site.populationJump")
                {
                    Need(sites.Contains(f.Target ?? ""), $"event {e.Id}: site.populationJump site {f.Target} not found");
                    Need(f.Fraction is > 0, $"event {e.Id}: site.populationJump needs a positive fraction");
                }
                if (f.Kind == "link.closed")
                    Need(s.TransportLinks.Any(l => l.Id == f.Target), $"event {e.Id}: link.closed link {f.Target} not found");
                if (f.Kind == "cor.rotates") Need(cors.Contains(f.Target ?? ""), $"event {e.Id}: cor.rotates COR {f.Target} not found");
                if (f.Kind == "funding.late")
                {
                    Need(s.FundingLines.Any(l => l.Id == f.Target), $"event {e.Id}: funding.late funding line {f.Target} not found");
                    Need(f.Days is > 0 || f.DaysRange is { Max: > 0 }, $"event {e.Id}: funding.late needs a positive days or daysRange");
                }
            }
        }
        if (s.Actors.AcsaPartner is { } partner)
        {
            if (partner.Island is { } pi) Need(islands.Contains(pi), $"acsaPartner island {pi} not found");
            foreach (var c in partner.Stock.Keys) Need(caps.Contains(c), $"acsaPartner stock capability {c} not in rules");
            foreach (var c in partner.UnitPriceCents.Keys) Need(partner.Stock.ContainsKey(c), $"acsaPartner price for {c} has no stock");
            if (partner.Present) Need(Rules.Instruments.ContainsKey("acsa"), "acsaPartner needs the acsa instrument in rules");
        }
        foreach (var e in s.Events)
            foreach (var f in e.Effects.Where(f => f.Kind == "acsa.withdraw"))
                Need(f.Target is null || caps.Contains(f.Target), $"event {e.Id}: acsa.withdraw capability {f.Target} not in rules");
        if (s.Actors.HostNation is { } hn)
        {
            if (hn.Island is { } hi) Need(islands.Contains(hi), $"hostNation island {hi} not found");
            foreach (var c in hn.Capacity.Keys) Need(caps.Contains(c), $"hostNation capacity capability {c} not in rules");
            foreach (var c in hn.UnitPriceCents.Keys) Need(hn.Capacity.ContainsKey(c), $"hostNation price for {c} has no capacity");
            foreach (var site in hn.Sites) Need(s.Sites.Any(x => x.Id == site), $"hostNation site {site} not found");
            if (hn.AgreementExists) Need(Rules.Instruments.ContainsKey("hostNation"), "hostNation agreement needs the hostNation instrument in rules");
        }
        foreach (var e in s.Events)
            foreach (var f in e.Effects.Where(f => f.Kind == "hostNation.withdraw"))
                Need(f.Target is null || caps.Contains(f.Target), $"event {e.Id}: hostNation.withdraw capability {f.Target} not in rules");
        if (s.Actors.ExternalSupport is { } ext)
        {
            Need(Rules.Instruments.ContainsKey("externalSupport"), "actors.externalSupport needs the externalSupport instrument in rules");
            Need(ext.Offers.Select(o => o.Capability).Distinct().Count() == ext.Offers.Count, "actors.externalSupport offers must name each capability once");
            foreach (var o in ext.Offers)
            {
                var cap = Rules.Model.Capabilities.FirstOrDefault(c => c.Id == o.Capability);
                Need(cap is not null, $"external support capability {o.Capability} not in rules");
                foreach (var line in o.Lines)
                    Need(cap!.Lines.Any(l => l.Id == line), $"external support line {line} is not a {o.Capability} line");
                Need(cap!.Lines.Any(l => l.Kind == "core" && o.Lines.Contains(l.Id)), $"external support {o.Capability} must offer the core line");
                foreach (var site in o.Sites) Need(sites.Contains(site), $"external support {o.Capability} site {site} not found");
            }
        }
        foreach (var c in Rules.PrepChoices)
            foreach (var f in c.Effects) Need(Prep.KnownEffects.Contains(f.Kind), $"rules prep choice {c.Id}: effect kind {f.Kind} is not implemented");
    }
}
