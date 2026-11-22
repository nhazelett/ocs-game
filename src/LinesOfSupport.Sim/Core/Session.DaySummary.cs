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

        // Turned down (DESK-57, the 16 Nov Claude play): a BPA call the vendor declined today. The play's day 5 read "done by
        // MSgt Joan Avery" and "everything was supplied in full" while Harbor Equipment had said no; the player found out a
        // day later at the foot of the request. A declined call is listed first under short or missed, with what it leaves
        // uncovered, and its work line says it was turned down.
        var turnedDown = new List<string>();
        var declined = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in _records.Where(r => r.Day == d && r.Kind == RecordKind.Declined))
        {
            if (_arrangements.FirstOrDefault(a => a.Id == r.Subject && a.Status == ArrangementStatus.Declined) is not { } arr) continue;
            declined.Add(arr.Id);
            var call = _work.Select(w => w.Payload).OfType<AwardPayload>().FirstOrDefault(p => p.Action.Id == arr.Id)?.Action;
            turnedDown.Add($"{VendorData(arr.VendorId)!.Name} turned down the BPA call for {Lower(CapLabel(arr.CapabilityId))} at {SiteName(arr.SiteId)}. {Uncovered(arr, Math.Max(call?.StartDay ?? Day, Day))}");
        }

        // Finished: everything the team completed that day, in the order given, except the trips and the meeting, which the
        // day adds and finishes by itself.
        var finished = _work.Where(w => w.CompletedDay == d && w.StoppedDay is null && w.Payload is not VisitPayload { Kind: "meeting" or "sf44Trip" })
                            .Select(w => new DaySummaryLine(Member(w.Assignee).Id, w.Payload is AwardPayload p && declined.Contains(p.Action.Id)
                                ? $"{WorkName(w)}: sent by {MemberName(Member(w.Assignee))}, and the vendor turned it down."
                                : $"{WorkName(w)}: done by {MemberName(Member(w.Assignee))}."))
                            .ToList();

        var started = new List<string>();
        var ended = new List<string>();
        foreach (var r in _records.Where(r => r.Day == d))
        {
            // DESK-189 (b) (Claude play 21 Nov, tenth play, note P2-9): the office's own support starts and ends here too,
            // for the contracting office in place of a site, as the plan says it (DESK-185).
            if (r.Kind == RecordKind.ArrangementStart && _arrangements.FirstOrDefault(a => a.Id == r.Subject) is { } s)
                started.Add(ForShop(s.CapabilityId)
                    ? $"{SupplierName(s)} started {ShopSupport(s.CapabilityId)}."
                    // DESK-224 (Claude play 22 Nov sixth, note F-1): "at Hub: 3 forklifts.", not "at Hub 3 forklifts."
                    : $"{SupplierName(s)} started {Lower(CapLabel(s.CapabilityId))} at {SiteName(s.SiteId)}{(Amount(s, Cap(s.CapabilityId)) is { Length: > 0 } n ? ":" + n : "")}.");
            if (r.Kind == RecordKind.ArrangementEnd && _arrangements.FirstOrDefault(a => a.Id == r.Subject) is { } e)
                // DESK-160 (b), Claude play 21 Nov, fourth play, note 19: said the day after its last day, so it names that day.
                ended.Add($"{Upper(ForShop(e.CapabilityId) ? ShopSupport(e.CapabilityId) : $"{Lower(CapLabel(e.CapabilityId))} at {SiteName(e.SiteId)}")} from {SupplierName(e)} ended{(e.EndDay < d ? $": its last day was day {e.EndDay}" : "")}.");
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

        // DESK-226 (Claude play 22 Nov seventh, note P2-26; second play P2-12): a miss the office heard of today that left
        // nothing short. "Everything was supplied in full" sat over "COR report (SSgt Lane): ... missed its delivery on day
        // 11; the stock on hand covered it." Only what the office was told: a COR or unit report that arrived today, or a
        // host nation or ACSA miss the news names.
        var misses = _records.Count(r => r.Day == d && r.Kind == RecordKind.CorReport)
                     + _records.Count(r => r.Day == d && TheirsCovered(r) is not null);
        var sentence = SummarySentence(d, finished.Count, started.Count, shortLines.Count, slipped.Count, turnedDown.Count, misses);
        var (said, news) = BuildDayNews();   // DESK-73 (D29): what people told you, and the day's news
        return new DaySummaryView(d, sentence,
            [.. finished.Select(Said)], [.. started.Select(SentenceCase)], [.. ended.Select(SentenceCase)],
            [.. turnedDown.Concat(shortLines).Select(SentenceCase)], [.. slipped.Select(Said)], [.. carried.Select(Said)],
            said, news);

        static DaySummaryLine Said(DaySummaryLine l) => l with { Sentence = SentenceCase(l.Sentence) };
    }

    /// <summary>What a declined call leaves uncovered from the day it was to start: another live arrangement that supplies
    /// the site then, another buy for it still in work, or nothing.</summary>
    private string Uncovered(Arrangement declined, int from)
    {
        var cap = Lower(CapLabel(declined.CapabilityId));
        var site = SiteName(declined.SiteId);
        if (_arrangements.FirstOrDefault(a => a.Id != declined.Id && a.SiteId == declined.SiteId && Supplies(a, declined.CapabilityId)
                && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active && a.StartDay <= from && from <= a.EndDay) is { } other)
            return $"{SupplierName(other)} still supplies {cap} there on day {from}.";
        if (_work.Any(w => w.CompletedDay is null && w.Payload is AwardPayload p && p.Action.Id != declined.Id
                           && p.SiteId == declined.SiteId && p.CapabilityId == declined.CapabilityId))
            return $"Another buy for {cap} at {site} is in work.";
        return $"Nothing is arranged for {cap} at {site} from day {from}; it needs another buy.";
    }

    private static string SummarySentence(int d, int finished, int started, int shortCount, int slipped, int turnedDown = 0, int misses = 0)
    {
        var parts = new List<string>
        {
            finished switch { 0 => "nothing finished", 1 => "one job finished", _ => $"{finished} jobs finished" },
        };
        if (started > 0) parts.Add(started == 1 ? "one support started" : $"{started} supports started");
        if (turnedDown > 0) parts.Add(turnedDown == 1 ? "a vendor turned down a call" : $"vendors turned down {turnedDown} calls");
        // "Everything was supplied in full" on a day a call was turned down read as all is well (the 16 Nov play, day 5).
        var missed = shortCount == 0 && misses > 0;
        parts.Add(shortCount switch { 0 when turnedDown > 0 => "no site was short", 0 when missed => "nothing was short", 0 => "everything was supplied in full", 1 => "one thing was short", _ => $"{shortCount} things were short" });
        if (slipped > 0) parts.Add(slipped == 1 ? "one job slipped" : $"{slipped} jobs slipped");
        var list = parts.Count == 1 ? parts[0] : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1];
        var though = missed ? misses == 1 ? ", though one missed delivery was reported" : $", though {misses} missed deliveries were reported" : "";
        return $"Day {d}: {list}{though}.";
    }

    /// <summary>A work item's name as a heading: a visit's label is a sentence with its hours ("Going to find the finance
    /// office: about 2 shop hours."), so only the part before the colon.</summary>
    internal static string WorkName(WorkItem w)
    {
        var label = w.Label.TrimEnd('.');
        return w.Kind == WorkKind.Visit && label.IndexOf(':') is var i and > 0 ? label[..i] : label;
    }
}
