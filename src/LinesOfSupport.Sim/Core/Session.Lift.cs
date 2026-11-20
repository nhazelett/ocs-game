using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// A vendor who ships port to port (DESK-11, docs/design/desk-redesign.md D6 and D41). Goods bought on one island reach a
// site on another by the scheduled surface links, on the days they run. A boat between islands is bought like the shop
// vehicle: on the office's own initiative, as an ordinary award for a capability the rules mark lift (and forShop: no
// site demand, not scored), at the site it serves. While it runs, consumables for any site on that site's island cross
// from any island every day, whatever the barge schedule, and whether or not a link runs between the two islands at all.
// The one thing that stops it is the weather that stops the barge: a day a surface link between the two islands is
// closed, the boat does not sail either. No rule number. Left out: missed sailings, capacity per trip, lift by air,
// carrying continuous services (they are on site already), and a lift any policy buys.
public sealed partial class Session
{
    private bool IsLift(string capabilityId) => _capData.TryGetValue(capabilityId, out var c) && c.Lift;

    /// <summary>The boat running to this island on this day (or starting on it), if any.</summary>
    private Arrangement? LiftTo(string island, int day)
        => _arrangements.Where(a => IsLift(a.CapabilityId) && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active
                                    && a.StartDay <= day && a.EndDay >= day && SiteData(a.SiteId).Island == island)
                        .OrderBy(a => a.StartDay).ThenBy(a => a.Id, StringComparer.Ordinal).FirstOrDefault();

    /// <summary>The surface link between the two islands closed today, if any: the weather that stops the barge stops a
    /// hired boat on the same water too.</summary>
    private string? WeatherBetween(string fromIsland, string toIsland, int day)
        => Scenario.TransportLinks.FirstOrDefault(l => l.Mode is not ("aircraft" or "airlift")
                                                       && ((l.From == fromIsland && l.To == toIsland) || (l.From == toIsland && l.To == fromIsland))
                                                       && !LinkOpen(l.Id, day))?.Id;

    /// <summary>A hired boat carries goods from one island to another today; <paramref name="liftId"/> names it. When a boat
    /// runs to that island but the weather keeps it in, <paramref name="closedLink"/> names the closed link.</summary>
    private bool LiftRuns(string fromIsland, string toIsland, int day, out string? liftId, out string? closedLink)
    {
        liftId = closedLink = null;
        if (fromIsland == toIsland || LiftTo(toIsland, day) is not { } lift) return false;
        if (WeatherBetween(fromIsland, toIsland, day) is { } closed) { closedLink = closed; return false; }
        liftId = lift.Id;
        return true;
    }

    /// <summary>A scenario vendor sells a boat between islands.</summary>
    private bool AnyLiftVendor => Scenario.Vendors.Any(v => v.Offers.Any(o => IsLift(o.Capability)));

    /// <summary>The own-initiative entries for a boat: one per site off the shop's island, while a vendor sells one.</summary>
    private IEnumerable<OwnInitiativeView> LiftInitiative(Data.CapabilityData cap)
    {
        var vendors = Scenario.Vendors.Where(v => v.Offers.Any(o => o.Capability == cap.Id)).Select(v => v.Id).ToList();
        if (vendors.Count == 0) yield break;
        foreach (var site in Scenario.Sites.Where(s => s.Island != ShopIsland))
        {
            var island = IslandData(site.Island).Name;
            var runs = Scenario.TransportLinks.Where(l => l.Mode is not ("aircraft" or "airlift")
                                                          && ((l.From == site.Island) || (l.Bidirectional && l.To == site.Island))).ToList();
            var now = runs.Count == 0
                ? $"Nothing runs to {island} by sea, so goods bought on another island do not get there"
                : runs.All(l => l.ScheduleDays.Count == 0)
                ? $"The {runs[0].Mode} already runs to {island} every day, so this only matters if it stops"
                : $"Goods bought on another island reach {island} only on the {Plural(runs.SelectMany(l => l.ScheduleDays).Distinct().Count(), "day")} a week the {runs[0].Mode} runs";
            var why = $"Nobody will ask for it. {now}. While you have one, they cross every day, except when weather stops the {(runs.Count == 0 ? "boats" : runs[0].Mode)}.";
            yield return new OwnInitiativeView(site.Id, cap.Id, cap.Label, why, vendors, LiftTo(site.Island, Day) is not null);
        }
    }

    /// <summary>WhyShort's pointer when goods wait on another island for the next scheduled run.</summary>
    private string LiftHint(string toIsland)
        => AnyLiftVendor && LiftTo(toIsland, Day) is null
            ? $"; a boat of your own to {IslandData(toIsland).Name}, bought on your own initiative, would carry it every day"
            : "";
}
