using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// What earned each line of the bar (DESK-106, docs/design/desk-redesign.md D38). The 18 Nov Claude play won the tutorial
// and read a debrief that graded the outcome and said nothing of the decisions that made it; the careful player on 17 Nov
// said the same ("not knowing whether any choice I made mattered"). Under each line of the verdict, one sentence names
// what carried it: the arrangements that covered the capability on the scenario's days, the day each was decided and by
// whom, and, where a part of it came from an ask, who said so. A missed line names its days and first cause. A line the
// host nation carried throughout says so. Left out: grades, a score for money, praise, hindsight about what else would
// have worked, and lines not on the bar. Read only, from records already written.
public sealed partial class Session
{
    /// <summary>At most this many arrangements are named under one line, in the order they started.</summary>
    private const int CarriedMaxNamed = 3;

    internal IReadOnlyList<CarriedView> BuildCarried(IReadOnlyList<GapView> gaps, int played)
    {
        var m = Metrics;
        var list = new List<CarriedView>();
        foreach (var p in Scenario.PassCriteria)
        {
            var met = Meets(m.TryGetValue(p.Metric, out var v) ? v : 0, p.Comparator, p.Value);
            var parts = p.Metric.Split('.');
            string? sentence = parts switch
            {
                ["site", var site, var cap, "downDays"] when Scenario.Sites.Any(s => s.Id == site) && _capData.ContainsKey(cap)
                    => CarriedSiteCapability(site, cap, met, gaps, played),
                ["mission", ..] => CarriedMission(met, gaps),
                ["compliance", "unauthorizedCommitments"] => (int)v == 0
                    ? "Nobody committed the government without authority."
                    : $"{Upper(DebriefWords.Number((int)v))} {((int)v == 1 ? "commitment was" : "commitments were")} made without authority, the first on day {_arrangements.Where(a => a.Unauthorized).Select(a => a.CreatedDay).DefaultIfEmpty(Day).Min()}.",
                _ => null,
            };
            if (sentence is not null) list.Add(new CarriedView(p.Id, SentenceCase(sentence)));
        }
        return list;
    }

    private string CarriedSiteCapability(string siteId, string capId, bool met, IReadOnlyList<GapView> gaps, int played)
    {
        var cs = _caps.FirstOrDefault(c => c.SiteId == siteId && c.CapabilityId == capId);
        var band = cs is null ? Enumerable.Repeat(-1, LastDay).ToArray() : Band(cs);
        if (!met)
        {
            var down = Enumerable.Range(1, played).Where(d => band[d - 1] >= 0 && band[d - 1] < Rules.Model.DownThresholdPermille).ToList();
            var first = gaps.FirstOrDefault(g => g.SiteId == siteId && g.CapabilityId == capId && down.Count > 0 && g.StartDay <= down[0] && down[0] <= g.EndDay);
            var cause = first?.Factors.OrderBy(f => f.DependsOnGap is null ? 0 : 1).ThenBy(f => f.FirstDay).FirstOrDefault();
            return $"Out on {DayRuns(down)}. {(cause is null ? "Nothing on record explains the first of them." : cause.Text.Trim())}";
        }

        var open = Enumerable.Range(1, played).Where(d => band[d - 1] >= 0).ToList();
        var supplied = _arrangements
            .Where(a => a.SiteId == siteId && a.Status != ArrangementStatus.Declined && Supplies(a, capId))
            .Select(a => (A: a, Days: open.Where(d => ActiveOn(a, d)).ToList()))
            .Where(x => x.Days.Count > 0)
            .OrderBy(x => x.Days[0]).ThenByDescending(x => x.Days.Count)
            .ToList();
        if (supplied.Count == 0)
            return open.Count == 0 ? "The site did not need it on any day played." : "It was never out, and nothing was arranged for it.";

        var own = supplied.Where(x => DecidedBy(x.A) is not null).ToList();
        if (own.Count == 0)
            return $"{Upper(JoinAnd(supplied.Select(x => Inherited(x.A)).Distinct().ToList()))} covered it {(supplied.Sum(x => x.Days.Count) >= open.Count ? "throughout" : "on the days it was needed")}; nothing the shop arranged was needed for it.";

        var named = supplied.Take(CarriedMaxNamed).Select(x => DecidedBy(x.A) is { } d
            ? $"{SupplierName(x.A)}{WithParts(x.A, capId)} from day {x.Days[0]}, {d}"
            : $"{Inherited(x.A)} {(x.Days[0] == 1 ? "from the start" : $"from day {x.Days[0]}")}").ToList();
        return $"{Upper(string.Join("; then ", named))}.";
    }

    /// <summary>"Main Island Utilities (host nation)", or a supplier the last rotation left, said so.</summary>
    private string Inherited(Arrangement a)
        => a.Unauthorized ? $"{SupplierName(a)}, on a handshake made without authority,"
           : a.Instrument == HostNationInstrumentId || DecidedBy(a) is not null ? SupplierName(a) : $"{SupplierName(a)}, left by the last rotation,";

    /// <summary>" with operators and maintenance": the parts beyond the core the arrangement brings; empty when none.</summary>
    private string WithParts(Arrangement a, string capId)
    {
        var extra = Cap(capId).Lines.Where(l => l.Kind != "core" && a.Lines.Contains(l.Id)).Select(l => (l.Label ?? l.Id).ToLowerInvariant()).ToList();
        return extra.Count == 0 ? "" : $" with {JoinAnd(extra)}";
    }

    /// <summary>
    /// "ordered on day 5 and written by MSgt Joan Avery, after SMSgt Ray Dunn said the request needs operators", or null
    /// when the shop did not decide it (inherited, or the host nation's own).
    /// </summary>
    private string? DecidedBy(Arrangement a)
    {
        var rec = _records.FirstOrDefault(r => r.Kind == RecordKind.Action && r.Subject == a.Id);
        if (rec is null) return null;
        var verb = a.Instrument is HostNationInstrumentId or AcsaInstrument or ExternalInstrument or "coordination" ? "asked for" : "ordered";
        var work = _work.FirstOrDefault(w => w.CauseId == rec.Id);
        var who = Member(work?.Assignee ?? a.TripAssignee);
        var by = who.Role == "you" ? $"{verb} by you on day {rec.Day}" : $"{verb} on day {rec.Day} and written by {MemberName(who)}";
        if (a.CardId is not { } cardId || _cards.FirstOrDefault(c => c.Id == cardId) is not { } card) return by;
        var told = Cap(card.CapabilityId).Lines
            .Where(l => a.Lines.Contains(l.Id) && _learnedLines.TryGetValue((card.Id, l.Id), out var t) && t.Day <= rec.Day)
            .GroupBy(l => _learnedLines[(card.Id, l.Id)].Person)
            .Select(g => $"{BuildPerson(g.Key)?.DisplayName ?? "someone"} said the request needs {JoinAnd(g.Select(l => (l.Label ?? l.Id).ToLowerInvariant()).ToList())}")
            .ToList();
        return told.Count == 0 ? by : $"{by}, after {JoinAnd(told)}";
    }

    private string CarriedMission(bool met, IReadOnlyList<GapView> gaps)
    {
        var first = gaps.Where(g => g.Factors.Count == 0 || g.Factors.Any(f => f.DependsOnGap is null))
                        .OrderBy(g => g.StartDay).ThenBy(g => g.Index).FirstOrDefault() ?? gaps.FirstOrDefault();
        if (first is null) return "Carried by the lines above: no site ran short of anything.";
        var when = $"on day {first.StartDay}, when {Lower(first.Label)} at {SiteName(first.SiteId)} ran short";
        return met ? $"Carried by the lines above; it first dipped {when}." : $"It first fell {when}.";
    }

    /// <summary>"day 4", "days 9 and 10", "days 10 to 12 and 16 to 30": runs of days, not every day.</summary>
    private static string DayRuns(IReadOnlyList<int> days)
    {
        if (days.Count == 0) return "no day played";
        var runs = new List<string>();
        for (var i = 0; i < days.Count; i++)
        {
            var start = days[i];
            while (i + 1 < days.Count && days[i + 1] == days[i] + 1) i++;
            runs.Add(days[i] == start ? $"{start}" : days[i] == start + 1 ? $"{start} and {days[i]}" : $"{start} to {days[i]}");
        }
        return $"{(days.Count == 1 ? "day" : "days")} {(runs.Count == 2 && runs.Any(r => r.Contains(" and ", StringComparison.Ordinal)) ? $"{runs[0]}, and {runs[1]}" : JoinAnd(runs))}";
    }
}
