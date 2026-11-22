using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// DESK-211 (D55; Claude play 22 Nov, second play, notes P1-14, P1-15, P2-15). The debrief's written-terms sentence named
// "a purchase order or a BPA call" on water, where the day-1 purchase order started day 4 to 6 with water out up to three
// days and there was no water BPA: "the debrief should only call something avoidable if an option existed that would
// have worked". It now says what the written route the shop knew that day would have cost, from the same quote engine
// the award form used, or that there was none. Left out: changing the tag, the scoring or the confusion rule, and a best
// route computed across buys.
public sealed partial class Session
{
    /// <summary>The written route the shop could place today for the request with the soonest worst start (then the
    /// cheapest), its start window and the days the request would be out by that start; a route of null when there is none.</summary>
    private WrittenAlternative WrittenAlternativeFor(CardState card)
    {
        var best = KnownRoutes(card).Where(r => r.Written).OrderBy(r => r.LeadMax).ThenBy(r => r.Cost).FirstOrDefault();
        if (best is null) return new WrittenAlternative(null, Day, 0, 0, 0, null);
        var (min, max) = (Day + best.Lead, Day + best.LeadMax);
        var o = OutDaysIf(card, max);
        return new WrittenAlternative(best.Route, Day, min, max, o?.Total ?? 0, o?.Verdict);
    }

    /// <summary>DESK-220 (Claude play 22 Nov, fourth play, note P1-35): the days this buy was disputed, as they were: "on day
    /// 3", "on days 12 and 15". "From day 3" read as a dispute that went on when water was in full every other day.</summary>
    private string DisputedDaysSaid(string arrangementId, int first)
    {
        var days = _records.Where(r => r.Kind == RecordKind.ExecutionConfusion && r.Subject == arrangementId)
                           .Select(r => r.Day).Append(first).Distinct().Order().ToList();
        if (days.Count == 1) return $"on day {days[0]}";
        return $"on days {string.Join(", ", days.Take(days.Count - 1))} and {days[^1]}";
    }

    /// <summary>The confusion factor's closing sentence, with its leading space. A route that could have started by the
    /// gap's day: " Written terms settle a dispute, at a cost in days: a purchase order with Harbor Bottling placed on day 1
    /// would have started day 2 or 3." One that could not: " A purchase order with Harbor Bottling placed on day 1 would
    /// have started day 4 to 6, with water at Hub out up to 3 days in all, two more than the commander allows; the card was
    /// the way to have water at Hub on day 3." None: " No written order the shop knew could have been placed on day 1; ...".
    /// A buy kept before DESK-211, or for no request, keeps the general sentence.</summary>
    private string WrittenRouteSentence(Arrangement a, int day)
    {
        if (a.Written is not { } w) return DebriefWords.NoTermsSettle;
        var what = $"{Lower(CapLabel(a.CapabilityId))} at {SiteName(a.SiteId)}";
        var instead = a.Instrument == "sf44" ? "a cash buy" : "the card";
        if (w.Route is null)
            return $" No written order the shop knew could have been placed on day {w.OrderDay}; {instead} was the way to have {what} on day {day}.";
        var window = w.StartMax <= w.StartMin ? $"day {w.StartMin}"
            : w.StartMax == w.StartMin + 1 ? $"day {w.StartMin} or {w.StartMax}" : $"day {w.StartMin} to {w.StartMax}";
        var cost = w.OutDays > 0 ? $", with {what} out up to {Plural(w.OutDays, "day")}{(w.Verdict is { } v ? $" in all, {v}" : "")}" : "";
        var placed = $"{w.Route} placed on day {w.OrderDay} would have started {window}{cost}";
        return w.StartMin > day
            ? $" {Upper(placed)}; {instead} was the way to have {what} on day {day}."
            : $" Written terms settle a dispute, at a cost in days: {placed}.";
    }

    /// <summary>D55 (c): a gap whose every cause is going down with a gap that was the price of no written terms reads, on
    /// its button, "Avoidable: it went down with water at Hub." Null for any other gap.</summary>
    private string? KnockOnOfTrade(GapView gap, IReadOnlyList<GapView> gaps)
    {
        if (gap.Tag != GapTag.Avoidable || gap.Factors.Count == 0
            || !gap.Factors.All(f => f.Kind == GapFactorKind.Dependency && f.DependsOnGap is { } i && i < gaps.Count && gaps[i].PriceOfNoTerms))
            return null;
        var parents = gap.Factors.Select(f => gaps[f.DependsOnGap!.Value]).DistinctBy(g => (g.SiteId, g.CapabilityId)).ToList();
        var with = string.Join(" and ", parents.Select(g => $"{Lower(CapLabel(g.CapabilityId))} at {SiteName(g.SiteId)}"));
        // DESK-215 (D56): what went down with a risk worth taking reads the same.
        return parents.All(g => g.TagLine?.StartsWith(WorthTakingPrefix, StringComparison.Ordinal) == true)
            ? $"It went down with {with}, a risk worth taking."
            : $"Avoidable: it went down with {with}.";
    }

    private const string WorthTakingPrefix = "A risk worth taking: ";

    /// <summary>D56 (DESK-215; Claude play 22 Nov, third play, notes P1-18, P2-20): a gap that was the price of no written
    /// terms, where the written route the shop knew on each buy's day could not have started by the gap's day, or there
    /// was none, reads "A risk worth taking: no written order could have started by day 3." in place of D54's line. Both
    /// players read "Avoidable" above "the card was the way to have water at Hub on day 3" as a contradiction, and asked for
    /// a move that did not exist. Null for any other gap, including one whose written route could have started in time.</summary>
    private string? WorthTaking(GapView gap)
    {
        if (!gap.PriceOfNoTerms) return null;
        string? line = null;
        foreach (var f in gap.Factors)
        {
            var rec = f.RecordIds.Count > 0 ? _records.FirstOrDefault(r => r.Id == f.RecordIds[0]) : null;
            if (_arrangements.FirstOrDefault(a => a.Id == rec?.Subject) is not { Written: { } w }) return null;
            if (w.Route is not null && w.StartMin <= f.FirstDay) return null;
            line ??= w.Route is null
                ? $"{WorthTakingPrefix}the shop knew no written order on day {w.OrderDay}."
                : $"{WorthTakingPrefix}no written order could have started by day {f.FirstDay}.";
        }
        return line;
    }
}
