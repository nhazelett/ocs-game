using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// What happened today (DESK-40, docs/design/desk-redesign.md D20). Nick on his 13 Nov replay asked for "just an end of day
// summary". After the day plays, one page says what actually happened: what finished and who did it, what started and
// ended, what was short and why (DESK-38's sentence), what slipped against the plan the day began with (DESK-39), and what
// carries into tomorrow. Built from the day's records and work, never from the forecast; the plan is kept only to say what
// slipped against it. Read only.
public sealed partial class Session
{
    /// <summary>The plan the day just played began with (DESK-39), kept so its summary can say what slipped. Derived from
    /// state and recomputed on replay; not part of the state hash.</summary>
    private DayPlanView? _planAtStart;

    private void KeepPlan() => _planAtStart = BuildDayPlan();

    internal DaySummaryView? BuildDaySummary()
    {
        var d = Day - 1;
        if (d < 1 || _planAtStart is not { } plan || plan.Day != d) return null;

        // Finished: everything the team completed that day, in the order given, except the trips and the meeting, which the
        // day adds and finishes by itself.
        var finished = _work.Where(w => w.CompletedDay == d && w.Payload is not VisitPayload { Kind: "meeting" or "sf44Trip" })
                            .Select(w => new DaySummaryLine(Member(w.Assignee).Id, $"{WorkName(w)}: done by {MemberName(Member(w.Assignee))}."))
                            .ToList();

        var started = new List<string>();
        var ended = new List<string>();
        foreach (var r in _records.Where(r => r.Day == d))
        {
            if (r.Kind == RecordKind.ArrangementStart && _arrangements.FirstOrDefault(a => a.Id == r.Subject) is { } s && !ForShop(s.CapabilityId))
                started.Add($"{SupplierName(s)} started {Lower(CapLabel(s.CapabilityId))} at {SiteName(s.SiteId)}{Amount(s, Cap(s.CapabilityId))}.");
            if (r.Kind == RecordKind.ArrangementEnd && _arrangements.FirstOrDefault(a => a.Id == r.Subject) is { } e && !ForShop(e.CapabilityId))
                ended.Add($"{Upper(Lower(CapLabel(e.CapabilityId)))} at {SiteName(e.SiteId)} from {SupplierName(e)} ended.");
        }

        // Short: every capability below full that day, with why (DESK-38).
        var shortLines = new List<string>();
        foreach (var cs in _caps.Where(c => c.TodayPermille < 1000))
            if (BuildWhyShort(cs.SiteId, cs.CapabilityId) is { } why)
                shortLines.Add($"{CapLabel(cs.CapabilityId)} at {SiteName(cs.SiteId)} was at {cs.TodayPermille / 10}%. {why}");

        // Slipped: work the plan said should be done that day and is not.
        var planned = plan.People.SelectMany(p => p.Work.Select(w => (p.MemberId, Item: w))).ToList();
        var slipped = new List<DaySummaryLine>();
        foreach (var (who, item) in planned.Where(x => x.Item.FinishesToday && x.Item.WorkId is not null))
        {
            var w = _work.FirstOrDefault(x => x.Id == item.WorkId);
            if (w is null || w.CompletedDay == d) continue;
            slipped.Add(new DaySummaryLine(Member(w.Assignee).Id, w.CompletedDay is not null
                ? $"{WorkName(w)} was meant to be done today; it was done on day {w.CompletedDay}."
                : $"{WorkName(w)} was meant to be done today and was not: {HoursSaid(w.HoursRemainingMilli)} left, with {MemberName(Member(w.Assignee))}."));
        }

        // Carried over: open work someone put hours into that day, with what is left.
        var carried = new List<DaySummaryLine>();
        foreach (var (who, item) in planned.Where(x => !x.Item.FinishesToday && x.Item.WorkId is not null && x.Item.HoursToday > 0))
        {
            var w = _work.FirstOrDefault(x => x.Id == item.WorkId);
            if (w is null || w.CompletedDay is not null) continue;
            var done = PlayQueue(Member(w.Assignee), 0, Day, w, w.HoursRemainingMilli);
            carried.Add(new DaySummaryLine(Member(w.Assignee).Id,
                $"{WorkName(w)}: {HoursSaid(w.HoursRemainingMilli)} left with {MemberName(Member(w.Assignee))}{(done is { } dd ? $", done about day {dd}" : "")}."));
        }

        var sentence = SummarySentence(d, finished.Count, started.Count, shortLines.Count, slipped.Count);
        return new DaySummaryView(d, sentence,
            [.. finished.Select(Said)], [.. started.Select(SentenceCase)], [.. ended.Select(SentenceCase)],
            [.. shortLines.Select(SentenceCase)], [.. slipped.Select(Said)], [.. carried.Select(Said)]);

        static DaySummaryLine Said(DaySummaryLine l) => l with { Sentence = SentenceCase(l.Sentence) };
    }

    private static string SummarySentence(int d, int finished, int started, int shortCount, int slipped)
    {
        var parts = new List<string>
        {
            finished switch { 0 => "nothing finished", 1 => "one job finished", _ => $"{finished} jobs finished" },
        };
        if (started > 0) parts.Add(started == 1 ? "one support started" : $"{started} supports started");
        parts.Add(shortCount switch { 0 => "everything was supplied in full", 1 => "one thing was short", _ => $"{shortCount} things were short" });
        if (slipped > 0) parts.Add(slipped == 1 ? "one job slipped" : $"{slipped} jobs slipped");
        var list = parts.Count == 1 ? parts[0] : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1];
        return $"Day {d}: {list}.";
    }

    /// <summary>A work item's name as a heading: a visit's label is a sentence with its hours ("Going to find the finance
    /// office: about 2 shop hours."), so only the part before the colon.</summary>
    internal static string WorkName(WorkItem w)
    {
        var label = w.Label.TrimEnd('.');
        return w.Kind == WorkKind.Visit && label.IndexOf(':') is var i and > 0 ? label[..i] : label;
    }
}
