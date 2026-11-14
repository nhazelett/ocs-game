using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Map day projection builder (M2R-09, OBJ-0004). The runs and holds are recorded by ResolveCapabilities as they
// happen; this only reads them back. Nothing here changes state or draws a random number.
public sealed partial class Session
{
    private readonly List<(string Arr, string Cap, string Site, string Link, long Milli)> _dayRuns = [];
    private readonly List<(string Arr, string Cap, string Site, string? ClosedLink, long Milli)> _dayHeld = [];

    internal MapDayView BuildMapDay()
    {
        var d = Math.Max(1, Math.Min(Day - 1, LastDay));   // the day just played; day 0 before any day
        var played = Day > 1;
        var links = Scenario.TransportLinks.Select(l =>
        {
            var closedThrough = _linkClosedThrough.TryGetValue(l.Id, out var t) && d <= t ? t : (int?)null;
            return new MapLink(l.Id, l.From, l.To, l.Bidirectional, l.Mode,
                played && (l.ScheduleDays.Count == 0 || l.ScheduleDays.Contains(Weekday(d))),
                closedThrough is null, closedThrough,
                closedThrough is null ? null : _linkClosureCause.GetValueOrDefault(l.Id));
        }).ToList();

        MapRun Run((string Arr, string Cap, string Site, string Link, long Milli) r)
        {
            var a = _arrangements.First(x => x.Id == r.Arr);
            var v = VendorData(a.VendorId);
            var link = Scenario.TransportLinks.FirstOrDefault(l => l.Id == r.Link);
            return new MapRun(a.Id, a.VendorId, v?.Name ?? SupplierName(a), r.Cap, Cap(r.Cap).Unit, a.SupplierIslandId, SiteData(r.Site).Island, r.Site,
                              r.Link, link?.Mode ?? "", r.Milli / 1000);
        }
        MapHeld HeldOf((string Arr, string Cap, string Site, string? ClosedLink, long Milli) h)
        {
            var a = _arrangements.First(x => x.Id == h.Arr);
            var v = VendorData(a.VendorId);
            return new MapHeld(a.Id, a.VendorId, v?.Name ?? SupplierName(a), h.Cap, Cap(h.Cap).Unit, a.SupplierIslandId, SiteData(h.Site).Island, h.Site,
                               h.ClosedLink, h.ClosedLink is null ? "noService" : "closed", h.Milli / 1000);
        }
        var runs = played ? _dayRuns.Select(Run).ToList() : [];
        var held = played ? _dayHeld.Select(HeldOf).ToList() : [];

        var shipments = new List<MapShipment>();
        foreach (var (id, sh) in _shipments)
        {
            var a = _arrangements.FirstOrDefault(x => x.Id == id);
            if (a is null || a.Status != ArrangementStatus.Pending || a.StartDay <= d) continue;
            shipments.Add(new MapShipment(a.Id, a.VendorId, VendorData(a.VendorId)?.Name ?? a.VendorId, a.CapabilityId, sh.Mode,
                                          SiteData(a.SiteId).Island, a.SiteId, a.StartDay, sh.Bumps, sh.CustomsHeld));
        }

        var changes = new List<MapChange>();
        if (played)
            for (var i = _overnightFrom; i < _overnightTo && i < _records.Count; i++)
            {
                var r = _records[i];
                switch (r.Kind)
                {
                    case RecordKind.Shortfall or RecordKind.Restored:
                    {
                        var dot = r.Subject.IndexOf('.');
                        if (dot <= 0) break;
                        var site = r.Subject[..dot]; var cap = r.Subject[(dot + 1)..];
                        var cs = _caps.FirstOrDefault(x => x.SiteId == site && x.CapabilityId == cap);
                        changes.Add(new MapChange(site, cap, r.Kind == RecordKind.Shortfall ? "shortfall" : "restored", cs?.TodayPermille ?? 0, null, r.Id, true,
                            r.Kind == RecordKind.Shortfall ? BuildWhyShort(site, cap) : null));
                        break;
                    }
                    case RecordKind.VendorFailure:
                    {
                        var a = _arrangements.FirstOrDefault(x => x.Id == r.Subject);
                        if (a is null) break;
                        changes.Add(new MapChange(a.SiteId, a.CapabilityId, "vendorFailure", 0, a.Id, r.Id, RecordKnown(r)));
                        break;
                    }
                }
            }
        return new MapDayView(played ? d : 0, links, runs, held, shipments, changes);
    }
}
