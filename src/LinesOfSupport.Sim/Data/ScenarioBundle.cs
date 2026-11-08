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
        var cors = s.Cors.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        Need(cors.Count == s.Cors.Count, "COR ids must be unique");
        foreach (var c in s.Cors) Need(Rules.Cor.Qualities.ContainsKey(c.Quality), $"COR {c.Id} quality {c.Quality} not in rules cor.qualities");
        Need(Rules.Cor.Qualities.ContainsKey(Rules.Cor.ReliefQuality), $"rules cor.reliefQuality {Rules.Cor.ReliefQuality} not in cor.qualities");
        foreach (var a in s.InheritedArrangements)
        {
            if (a.Cor is not null) Need(cors.Contains(a.Cor), $"arrangement {a.Id} COR {a.Cor} not found");
            Need(Rules.Instruments.ContainsKey(a.Instrument), $"arrangement {a.Id} instrument {a.Instrument} not in rules");
            Need(vendors.Contains(a.Vendor), $"arrangement {a.Id} vendor not found");
            Need(sites.Contains(a.Site), $"arrangement {a.Id} site not found");
        }
        foreach (var e in s.Events)
        {
            foreach (var p in e.Preconditions) Need(Events.KnownPredicates.Contains(p.Kind), $"event {e.Id}: predicate kind {p.Kind} is not implemented");
            foreach (var f in e.Effects)
            {
                Need(Events.KnownEffects.Contains(f.Kind), $"event {e.Id}: effect kind {f.Kind} is not implemented");
                if (f.DaysRange is { } r) Need(r.Min <= r.Max, $"event {e.Id}: {f.Kind} daysRange max is below min");
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
