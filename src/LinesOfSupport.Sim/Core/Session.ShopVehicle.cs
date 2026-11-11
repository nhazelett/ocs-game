using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// A vehicle for the contracting office (DESK-10, docs/design/desk-redesign.md D5). Nobody asks for it: the shop buys it on
// its own initiative, as an ordinary award for a capability the rules mark forShop (no site demand, not scored). While one
// is running it gets you downtown: a vendor meeting and an SF 44 trip on the shop's own island take
// shopVehicle.visitHoursPermille of their usual hours, rounded up. That is the one rule number.
public sealed partial class Session
{
    /// <summary>The island the contracting office sits on: the hub's, else the first site's.</summary>
    private string ShopIsland => Scenario.Islands.FirstOrDefault(i => i.IsHub)?.Id ?? Scenario.Sites.First().Island;

    private bool ForShop(string capabilityId) => _capData.TryGetValue(capabilityId, out var c) && c.ForShop;

    /// <summary>True when the office has a vehicle running on this day (or starting on it).</summary>
    internal bool HasShopVehicle(int day)
        => _arrangements.Any(a => ForShop(a.CapabilityId) && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active
                                  && a.StartDay <= day && a.EndDay >= day);

    /// <summary>A visit's milli-hours with the shop vehicle's cut, if the office has one on that day.</summary>
    private long WithShopVehicle(long milli, int day)
    {
        if (milli <= 0 || !HasShopVehicle(day)) return milli;
        var pm = Rules.ShopVehicle.VisitHoursPermille;
        var cut = (milli * pm + 999) / 1000;
        return (cut + 999) / 1000 * 1000;   // whole hours, rounded up
    }

    /// <summary>Things the shop can buy that nobody asked for (DESK-10): today, the vehicle, for the shop's own site.</summary>
    internal IReadOnlyList<OwnInitiativeView> BuildOwnInitiative()
    {
        var site = Scenario.Sites.FirstOrDefault(s => s.Island == ShopIsland) ?? Scenario.Sites.First();
        var list = new List<OwnInitiativeView>();
        foreach (var cap in Rules.Model.Capabilities.Where(c => c.ForShop))
        {
            var vendors = Scenario.Vendors.Where(v => v.Offers.Any(o => o.Capability == cap.Id)).Select(v => v.Id).ToList();
            if (vendors.Count == 0) continue;
            var pm = Rules.ShopVehicle.VisitHoursPermille;
            var why = $"Nobody will ask for it. It gets you downtown: while you have one, a vendor meeting or an SF 44 delivery trip on {IslandData(ShopIsland).Name} takes {(pm == 500 ? "half" : $"{pm / 10}% of")} the hours.";
            list.Add(new OwnInitiativeView(site.Id, cap.Id, cap.Label, why, vendors, HasShopVehicle(Day)));
        }
        return list;
    }
}
