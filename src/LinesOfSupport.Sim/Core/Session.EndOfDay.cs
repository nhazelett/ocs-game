namespace LinesOfSupport.Sim.Core;

// The end-of-day check (DESK-04, docs/design/desk-redesign.md D1). Before the one "End the day" button ends the day, the
// game shows what is still open, as sentences: a request that is due and untouched, and a teammate with nothing to do.
// Read only: it changes nothing and draws nothing, so asking for it never changes a game.
public sealed partial class Session
{
    /// <summary>A request is due when it is wanted within this many days (or already late).</summary>
    internal const int EndOfDayDueWithinDays = 3;

    internal IReadOnlyList<string> BuildEndOfDayCheck()
    {
        var open = new List<string>();
        foreach (var card in _cards.Where(c => c.SubmittedDay <= Day).OrderBy(c => c.RequestedStartDay).ThenBy(c => c.SubmittedDay))
        {
            if (card.RequestedStartDay > Day + EndOfDayDueWithinDays || Touched(card)) continue;
            var title = CardTitle(card);
            open.Add(card.RequestedStartDay <= Day
                ? $"{title} was wanted from day {card.RequestedStartDay} and nothing is arranged or in work."
                : $"{title} is wanted from day {card.RequestedStartDay} and nothing is arranged or in work yet.");
        }
        foreach (var m in BuildTeam().Where(m => m.Role != "you" && m.FreeHoursToday > 0))
        {
            var name = m.Rank is { Length: > 0 } r ? $"{r} {m.Name}" : m.Name;
            open.Add(m.FreeHoursToday >= m.HoursPerDay
                ? $"{name} has nothing to work on today; their {HoursText(m.HoursPerDay)} hours go to walk-in small buys."
                : $"{name} has {HoursText(m.FreeHoursToday)} free hour{(m.FreeHoursToday == 1 ? "" : "s")} today.");
        }
        return open;
    }

    /// <summary>Something is under way for the request: an arrangement tied to it, work for it, or live support for its
    /// capability at its site on the day it is wanted.</summary>
    private bool Touched(CardState card)
    {
        if (_arrangements.Any(a => a.CardId == card.Id && a.Status != ArrangementStatus.Declined)) return true;
        if (_work.Any(w => w.CompletedDay is null && (w.SubjectId?.Contains(card.Id, StringComparison.Ordinal) ?? false))) return true;
        var day = Math.Max(card.RequestedStartDay, Day);
        return _arrangements.Any(a => a.SiteId == card.SiteId && a.CapabilityId == card.CapabilityId && Live(a)
                                      && a.StartDay <= day && a.EndDay >= day);
    }
}
