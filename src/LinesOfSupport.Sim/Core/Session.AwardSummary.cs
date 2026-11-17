using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// The buy summary above Arrange support (DESK-82, docs/design/desk-redesign.md D33; Codex's Q26 for DESK-71). One
// sentence sums the buy up: the cost, the hours and whose, the day it starts against the day it was wanted and what is
// uncovered between, and what it is on top of. It needs the player's attention when the buy starts after the need ends
// or only adds to days already covered; the Sim says so, so the Game never reads warning prose to pick a colour. A
// forecast from the plan, like the quote: it ignores rolls. Read only.
public sealed partial class Session
{
    internal AwardSummaryView? BuildAwardSummary(AwardAction a)
    {
        var preview = BuildPreview(a);
        if (!preview.Accepted) return null;
        var chk = CheckAward(a);
        if (chk.SiteId is not { } siteId || chk.CapabilityId is not { } capId || chk.Vendor is not { } vendor) return null;
        var cap = Cap(capId);
        var site = SiteData(siteId);
        var duration = Math.Max(1, a.DurationDays);
        var start = preview.EarliestStartMin;
        var max = preview.EarliestStartMax;
        var startSaid = max == start ? $"on day {start}" : max == start + 1 ? $"on day {start} or {max}" : $"between day {start} and day {max}";

        var parts = new List<string>
        {
            $"{vendor.Name}, {Lower(cap.Label)} for {site.Name}: {Money(preview.TotalCents)} and {HoursOf(preview, a.Assignee)}.",
        };
        var card = chk.Card;
        var needEnd = card is null ? (int?)null : card.RequestedStartDay + Math.Max(1, card.DurationDays) - 1;
        if (card is null) parts.Add($"It starts {startSaid}.");
        else if (start <= card.RequestedStartDay) parts.Add($"It starts {startSaid}; it is wanted from day {card.RequestedStartDay}.");
        else
        {
            parts.Add($"It starts {startSaid}; it was wanted from day {card.RequestedStartDay}.");
            if (start <= needEnd && BeforeStartCover(card, start) is { } cover) parts.Add(cover);
        }
        var onTop = InPlace(a, siteId, capId, start, start + duration - 1).Select(x => x.Name).Distinct().ToList();
        if (onTop.Count > 0) parts.Add($"It is on top of {JoinAnd(onTop)}.");

        string? why = null;
        if (card is not null && start > needEnd)
            why = $"It starts after the need ends on day {needEnd}.";
        else if (onTop.Count > 0 && OnlyCoveredDays(a, siteId, capId, start, Math.Min(start + duration - 1, needEnd ?? int.MaxValue)))
            why = "Every day it would run is already covered.";
        if (why is not null) parts.Add(why);
        return new AwardSummaryView(string.Join(" ", parts), why is not null, why);
    }

    /// <summary>"no shop hours", "6 hours of your time", "6 to 9 hours of MSgt Joan Avery's time".</summary>
    private string HoursOf(ActionPreview p, string? assignee)
    {
        if (p.ShopHoursMax == 0) return "no shop hours";
        var m = Member(assignee);
        var whose = m.Role == "you" ? "your" : MemberName(m) + "'s";
        var hours = p.ShopHoursMin == p.ShopHoursMax
            ? (p.ShopHoursMin == 1 ? "1 hour" : $"{p.ShopHoursMin} hours")
            : $"{p.ShopHoursMin} to {p.ShopHoursMax} hours";
        return $"{hours} of {whose} time";
    }

    /// <summary>True when every day from <paramref name="from"/> to <paramref name="to"/> that has demand at the site is
    /// already met in full by live arrangements and buys being written, so this buy would only add to covered days. Days
    /// with no demand do not count; with none left, the buy covers nothing anyone needs, which the start-after-need rule
    /// already says for a request.</summary>
    private bool OnlyCoveredDays(AwardAction award, string siteId, string capId, int from, int to)
    {
        var site = SiteData(siteId);
        var any = false;
        for (var g = Math.Max(from, Math.Max(Day, 1)); g <= Math.Min(to, LastDay); g++)
        {
            var demand = DemandMilli(site, capId, g);
            if (demand <= 0) continue;
            any = true;
            var supply = _arrangements.Where(x => x.Id != award.Id && x.SiteId == siteId && Live(x) && x.StartDay <= g && x.EndDay >= g)
                                      .Sum(x => (x.CapabilityId == capId ? x.QuantityMilli : 0) + x.SuppliedQuantityMilli.GetValueOrDefault(capId));
            supply += _work.Where(w => w.CompletedDay is null).Select(w => w.Payload).OfType<AwardPayload>()
                           .Where(p => p.Action.Id != award.Id && p.SiteId == siteId && p.CapabilityId == capId && p.Action.StartDay is { } sd
                                       && sd <= g && sd + Math.Max(1, p.Action.DurationDays) - 1 >= g)
                           .Sum(p => p.Action.Quantity * 1000);
            if (supply < demand) return false;
        }
        return any;
    }
}
