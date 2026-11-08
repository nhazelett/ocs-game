using System.Text.Json;

namespace LinesOfSupport.Harness;

// Content checks over Fable's schema v0. These are validation only; no simulation state or rules execute here.
internal static class ScenarioReferences
{
    internal static void Validate(JsonElement scenario, JsonElement rules)
    {
        var errors = new List<string>();
        HashSet<string> Ids(string name)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in Items(scenario, name))
                if (!ids.Add(Text(item, "id")))
                    errors.Add($"{name}: duplicate id '{Text(item, "id")}'.");
            return ids;
        }
        var islands = Ids("islands");
        var sites = Ids("sites");
        var vendors = Ids("vendors");
        var vehicles = Ids("vehicles");
        var events = Ids("events");
        var cards = Ids("requirementCards");
        var arrangements = Ids("inheritedArrangements");
        _ = Ids("transportLinks");
        _ = Ids("fundingLines");
        _ = Ids("passCriteria");
        var capabilities = Items(Object(rules, "model"), "capabilities").Select(x => Text(x, "id")).ToHashSet(StringComparer.Ordinal);
        var instruments = Object(rules, "instruments");
        var instrumentIds = instruments.ValueKind == JsonValueKind.Object
            ? instruments.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal)
            : [];
        void Reference(HashSet<string> ids, string id, string label)
        {
            if (!ids.Contains(id))
                errors.Add($"{label}: '{id}' not found.");
        }
        foreach (var link in Items(scenario, "transportLinks"))
            foreach (var end in new[] { "from", "to" })
                Reference(islands, Text(link, end), $"link {Text(link, "id")} {end} island");
        var days = scenario.GetProperty("days").GetInt32();
        foreach (var site in Items(scenario, "sites"))
        {
            var label = $"site {Text(site, "id")}";
            Reference(islands, Text(site, "island"), $"{label} island");
            foreach (var capability in Items(site, "capabilities"))
            {
                Reference(capabilities, Text(capability, "id"), $"{label} capability in rules");
                foreach (var initial in Items(capability, "initialArrangements"))
                    Reference(arrangements, initial.GetString()!, $"{label} initial arrangement");
            }
            if (Number(site, "opensDay") > days)
                errors.Add($"{label}: opens after the scenario ends.");
        }
        foreach (var card in Items(scenario, "requirementCards"))
        {
            var label = $"card {Text(card, "id")}";
            Reference(sites, Text(card, "site"), $"{label} site");
            Reference(capabilities, Text(card, "capability"), $"{label} capability in rules");
            if (Number(card, "requestedStartDay") < Number(card, "submittedDay"))
                errors.Add($"{label}: requested start before submission.");
        }
        foreach (var vendor in Items(scenario, "vendors"))
        {
            Reference(islands, Text(vendor, "island"), $"vendor {Text(vendor, "id")} island");
            foreach (var offer in Items(vendor, "offers"))
                Reference(capabilities, Text(offer, "capability"), $"vendor {Text(vendor, "id")} capability in rules");
        }
        foreach (var vehicle in Items(scenario, "vehicles"))
        {
            Reference(vendors, Text(vehicle, "vendor"), $"vehicle {Text(vehicle, "id")} vendor");
            foreach (var island in Items(vehicle, "placeOfPerformance"))
                Reference(islands, island.GetString()!, $"vehicle {Text(vehicle, "id")} island");
        }
        foreach (var arrangement in Items(scenario, "inheritedArrangements"))
        {
            var label = $"arrangement {Text(arrangement, "id")}";
            Reference(instrumentIds, Text(arrangement, "instrument"), $"{label} instrument in rules");
            Reference(vendors, Text(arrangement, "vendor"), $"{label} vendor");
            Reference(sites, Text(arrangement, "site"), $"{label} site");
            Reference(capabilities, Text(arrangement, "capability"), $"{label} capability in rules");
            if (arrangement.TryGetProperty("vehicle", out var vehicle))
                Reference(vehicles, vehicle.GetString()!, $"{label} vehicle");
        }
        foreach (var item in Items(scenario, "events"))
        {
            var window = item.GetProperty("window");
            var label = $"event {Text(item, "id")}";
            if (Number(window, "latestDay") < Number(window, "earliestDay"))
                errors.Add($"{label}: window ends before it starts.");
            if (Number(window, "earliestDay") > days)
                errors.Add($"{label}: window starts after the scenario ends.");
            foreach (var followOn in Items(item, "followOns"))
                Reference(events, followOn.GetString()!, $"{label} follow-on");
        }
        var all = new HashSet<string>(islands, StringComparer.Ordinal);
        foreach (var ids in new[] { sites, vendors, vehicles, events, cards })
            all.UnionWith(ids);
        foreach (var item in Items(scenario, "fixedSituations"))
            Reference(all, item.GetString()!, "fixedSituations");
        var actors = Object(scenario, "actors");
        foreach (var site in Items(Object(actors, "commander"), "priorityOrder"))
            Reference(sites, site.GetString()!, "commander priorityOrder site");
        foreach (var vehicle in Items(Object(actors, "sco"), "knownVehicles"))
            Reference(vehicles, vehicle.GetString()!, "SCO known vehicle");
        if (errors.Count > 0)
            throw new InvalidDataException("Scenario cross-reference validation failed:\n" + string.Join('\n', errors));
    }

    private static IEnumerable<JsonElement> Items(JsonElement parent, string key) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray() : [];
    private static JsonElement Object(JsonElement parent, string key) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(key, out var value) ? value : default;
    private static string Text(JsonElement parent, string key) =>
        parent.TryGetProperty(key, out var value) ? value.GetString()! : "";
    private static decimal Number(JsonElement parent, string key) =>
        parent.TryGetProperty(key, out var value) ? value.GetDecimal() : 0;
}
