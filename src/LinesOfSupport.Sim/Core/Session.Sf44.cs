using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// The SF 44 for recurring buys (DESK-09, docs/design/desk-redesign.md D11). Two costs, and only two: every delivery is a
// trip for the CCO and the paying agent (shop hours each time; for a site across the water, the surface link must be open
// that day), and cash is paid at each delivery, so the cash box can run dry before finance tops it up and a delivery the
// box cannot cover does not happen. A one-day SF 44 is paid on the spot as before.
public sealed partial class Session
{
    private readonly Dictionary<string, (string Record, string Reason)> _sf44Held = new(StringComparer.Ordinal);

    private static bool PerDelivery(AwardAction a) => a.Instrument == "sf44" && a.DurationDays > 1;

    private Sf44DeliveriesRulesData Sf44Rules => Rules.Sf44Deliveries;

    /// <summary>Shop hours for one delivery trip: across the water costs more.</summary>
    /// <summary>Milli-hours for one delivery trip: across the water costs more; on the shop's island a shop vehicle (DESK-10) cuts it.</summary>
    private long TripMilli(string siteId, string supplierIsland, int day)
    {
        var island = SiteData(siteId).Island;
        if (supplierIsland != island) return Fixed.Milli(Sf44Rules.OffIslandTripShopHours);
        var hours = Fixed.Milli(Sf44Rules.TripShopHours);
        return island == ShopIsland ? WithShopVehicle(hours, day) : hours;
    }

    private long TripMilli(Arrangement a, int day) => TripMilli(a.SiteId, a.SupplierIslandId, day);

    /// <summary>True when every surface link between the islands is closed today (a closure, not a day off the schedule).</summary>
    private bool SurfaceClosed(string fromIsland, string toIsland, int day)
    {
        if (fromIsland == toIsland) return false;
        var links = Scenario.TransportLinks.Where(l => l.Mode is not ("aircraft" or "airlift")
                                                       && ((l.From == fromIsland && l.To == toIsland) || (l.Bidirectional && l.From == toIsland && l.To == fromIsland)))
                                           .ToList();
        return links.Count > 0 && links.All(l => !LinkOpen(l.Id, day));
    }

    /// <summary>
    /// Before the shop works: each SF 44 delivery due today is made and paid, or held. A made delivery puts its trip at the
    /// front of the trip-maker's queue. Held: the link to the site is closed (no trip, no payment), or the cash on hand
    /// does not cover it (no payment, so no delivery; finance is asked to top up).
    /// </summary>
    private void Sf44Deliveries(int d)
    {
        _sf44Held.Clear();
        var trips = new List<WorkItem>();
        foreach (var a in _arrangements.Where(x => x.PaidPerDelivery))
        {
            var due = a.StartDay <= d && a.EndDay >= d && a.Status is ArrangementStatus.Active or ArrangementStatus.Pending
                      && PopulationOn(SiteData(a.SiteId), d) > 0;
            if (!due) continue;
            var site = SiteData(a.SiteId);
            if (SurfaceClosed(a.SupplierIslandId, site.Island, d))
            {
                var rec = AddRecord(d, RecordKind.Cash, a.Id,
                    $"No delivery on {NameOf(a.Id)} today: the boat to {IslandData(site.Island).Name} is not running, so the goods and the cash both stayed on {IslandData(a.SupplierIslandId).Name}.", [a.CauseId]);
                _sf44Held[a.Id] = (rec, "the SF 44 delivery could not cross");
                continue;
            }
            var cost = a.DailyCostCents + (a.MobilizationCharged ? 0 : a.MobilizationCents);
            if (cost > _cash)
            {
                ScheduleReplenish(shortNow: true);
                var when = _cashReplenishDay is { } r ? $" Finance tops it up on day {r}." : "";
                var rec = AddRecord(d, RecordKind.Cash, a.Id,
                    $"No delivery on {NameOf(a.Id)} today: it is paid in cash at the door, {Money(cost)}, and the cash box holds {Money(_cash)}.{when}", [a.CauseId]);
                _sf44Held[a.Id] = (rec, "no cash for the SF 44 delivery");
                continue;
            }
            _cash -= cost;
            a.ObligatedCents += cost;
            ScheduleReplenish();
            _assigneeNow = a.TripAssignee;
            trips.Add(AddWork(WorkKind.Visit, $"SF 44 delivery and payment at {site.Name}", TripMilli(a, d), d,
                              new VisitPayload("sf44Trip", a.Id), a.CauseId));
            _assigneeNow = null;
        }
        // The trips happen at the delivery hour: they go to the front of the queue, in arrangement order.
        foreach (var w in trips) _work.Remove(w);
        _work.InsertRange(0, trips);
    }
}
