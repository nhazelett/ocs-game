using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// What covers the days before a late start (DESK-60, docs/design/desk-redesign.md D18 and D19; Claude play note 10). The
// tutorial's card buy asked for day 2 and started day 3, and nothing said the site's two days of water stock covered
// day 2, so a correct day read as a hidden gap. A start later than the request asked now says what should cover the days
// between: the stock on hand, other support already arranged, or nothing. A forecast from the plan, like the day's plan:
// it ignores rolls and events. Read only.
public sealed partial class Session
{
    /// <summary>The request's days before <paramref name="start"/>, sorted by what should cover them: the stock on hand,
    /// support already arranged, or nothing. Null when the start is not later than the request asked.</summary>
    private (List<int> Stock, List<int> Other, List<int> None, CapabilityData Cap)? CoverDays(CardState card, int start)
    {
        var from = Math.Max(card.RequestedStartDay, Math.Max(Day, 1));
        var until = Math.Min(start - 1, LastDay);
        if (until < from) return null;
        var site = SiteData(card.SiteId);
        var cap = Cap(card.CapabilityId);
        var cs = CapState(site.Id, cap.Id);
        var sc = site.Capabilities.FirstOrDefault(c => c.Id == cap.Id);
        var buffer = cs?.BufferMilli ?? 0;
        var stock = new List<int>();
        var other = new List<int>();
        var none = new List<int>();
        for (var g = Math.Max(Day, 1); g <= until; g++)
        {
            var demand = DemandMilli(site, cap.Id, g);
            if (g == site.OpensDay && sc?.InitialBufferDays is { } init && demand > 0) buffer = demand * Fixed.Permille(init) / 1000;
            if (demand == 0) continue;
            var supply = _arrangements.Where(a => a.SiteId == site.Id && Live(a) && a.StartDay <= g && CountsThrough(a, card) >= g)
                                      .Sum(a => (a.CapabilityId == cap.Id ? a.QuantityMilli : 0) + a.SuppliedQuantityMilli.GetValueOrDefault(cap.Id));
            var covered = supply >= demand ? "other" : cap.Delivery == "consumable" && buffer + supply >= demand ? "stock" : "none";
            if (cap.Delivery == "consumable")
            {
                var available = buffer + supply;
                var take = Math.Min(available, demand);
                buffer = Math.Min(available - take, Math.Max(demand * (cs?.BufferDaysPermille ?? 0) / 1000, 0));
            }
            if (g < from) continue;
            (covered == "other" ? other : covered == "stock" ? stock : none).Add(g);
        }
        return (stock, other, none, cap);
    }

    /// <summary>
    /// What should cover the request's days before <paramref name="start"/>: "The water stock on hand should cover day 2."
    /// or "Nothing covers days 2 to 4."; null when the start is not later than the request asked.
    /// </summary>
    internal string? BeforeStartCover(CardState card, int start)
    {
        if (CoverDays(card, start) is not { } days) return null;
        var (stock, other, none, cap) = days;
        var said = new List<string>();
        if (stock.Count > 0) said.Add($"The {Lower(cap.Label)} stock on hand should cover {DaysSaid(stock)}.");
        if (other.Count > 0) said.Add($"Support already arranged should cover {DaysSaid(other)}.");
        if (none.Count > 0) said.Add($"Nothing covers {DaysSaid(none)}.");
        return said.Count == 0 ? null : string.Join(" ", said);
    }

    /// <summary>"day 2", "days 2 and 3", "days 2 to 5", "days 2, 4 and 5".</summary>
    internal static string DaysSaid(IReadOnlyList<int> days)
    {
        if (days.Count == 1) return $"day {days[0]}";
        if (days[^1] - days[0] == days.Count - 1)
            return days.Count == 2 ? $"days {days[0]} and {days[1]}" : $"days {days[0]} to {days[^1]}";
        return $"days {string.Join(", ", days.Take(days.Count - 1))} and {days[^1]}";
    }
}
