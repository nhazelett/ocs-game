using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim;

// Counterfactual re-run engine (backlog M3-02, SPEC 4.3 debrief view 4, SPEC 5). Pick one decision, change it,
// re-run with the same seed, compare. The picker UI is separate and still needs a prototype (SPEC 9).

/// <summary>What a counterfactual changes in the input log.</summary>
public enum ChangeKind { Replace, Remove, Insert, Prep }

/// <summary>
/// One change to a played game. Build with <see cref="Replace"/>, <see cref="Remove"/>, <see cref="Insert"/> or
/// <see cref="SetPrep"/>. ActionIndex is an index into <see cref="InputLog.Actions"/>
/// (the same order as <see cref="Counterfactual.Choices"/>).
/// </summary>
public sealed record CounterfactualChange(ChangeKind Kind, int? ActionIndex, int? Day, PlayerAction? Action, IReadOnlyList<string>? PrepChoices)
{
    /// <summary>Take a different action instead of the one at <paramref name="actionIndex"/>, on the same day.</summary>
    public static CounterfactualChange Replace(int actionIndex, PlayerAction instead) => new(ChangeKind.Replace, actionIndex, null, instead, null);

    /// <summary>Do not take the action at <paramref name="actionIndex"/>.</summary>
    public static CounterfactualChange Remove(int actionIndex) => new(ChangeKind.Remove, actionIndex, null, null, null);

    /// <summary>
    /// Also take <paramref name="action"/> on <paramref name="day"/>: after that day's logged actions, or just before the
    /// logged action at <paramref name="beforeActionIndex"/> (which must be dated that day), since order within a day matters
    /// (a requirement line switched on after an award does not change the award).
    /// </summary>
    public static CounterfactualChange Insert(int day, PlayerAction action, int? beforeActionIndex = null)
        => new(ChangeKind.Insert, beforeActionIndex, day, action, null);

    /// <summary>Make different pre-deployment choices (M2-09). The re-run differs from day 1.</summary>
    public static CounterfactualChange SetPrep(IReadOnlyList<string> prepChoices) => new(ChangeKind.Prep, null, null, null, prepChoices);
}

/// <summary>A logged action the player can pick to change.</summary>
public sealed record CounterfactualChoice(int ActionIndex, int Day, string Kind, string Text, bool Rejected);

/// <summary>How a gap moved between the played game and the re-run.</summary>
public enum GapChange { Gone, Shrunk, Unchanged, Worse, New }

/// <summary>
/// One gap compared across the two runs, over the original gap's days (or the new gap's days, for New).
/// Shortfall is the sum over those days of (1000 minus the capability's permille): 0 means full every day.
/// </summary>
public sealed record GapDiff(
    GapChange Change, string SiteId, string CapabilityId, string Label, int StartDay, int EndDay,
    int OriginalShortfall, int AlternativeShortfall, int OriginalDownDays, int AlternativeDownDays,
    int? OriginalGap, int? AlternativeGap);

/// <summary>The counterfactual view: what changed, both scorecards, and every gap compared.</summary>
/// <param name="FromDay">First day the two runs can differ.</param>
/// <param name="HorizonDay">Last day compared: the scenario's last day, or the last day played for a mid-game save.</param>
/// <param name="NoLongerApplied">Actions the re-run refused that the played game did not (later logged actions, or the change itself), each as
/// "Day N: action: reason" (a handshake that never happened, an award whose scope the change broke). One changed
/// decision often moves later ones; the picker shows these so the comparison is read honestly.</param>
public sealed record CounterfactualView(
    int FromDay, int HorizonDay, string ChangeText,
    ScorecardView Original, ScorecardView Alternative, double MissionOutputDelta,
    IReadOnlyList<GapDiff> Gaps, IReadOnlyList<string> NoLongerApplied);

/// <summary>Both sessions, the changed log, and the view.</summary>
public sealed record CounterfactualResult(Session Original, Session Alternative, InputLog AlternativeLog, CounterfactualView View);

public static class Counterfactual
{
    /// <summary>The decisions a player can change: every logged action, in log order.</summary>
    public static IReadOnlyList<CounterfactualChoice> Choices(Session played)
    {
        var actions = played.AppliedActions;
        var rejected = played.AppliedRejected;
        var list = new List<CounterfactualChoice>(actions.Count);
        for (var i = 0; i < actions.Count; i++)
            list.Add(new CounterfactualChoice(i, actions[i].Day, KindOf(actions[i]), Session.Describe(actions[i]), rejected[i]));
        return list;
    }

    /// <summary>
    /// Re-runs <paramref name="log"/> with <paramref name="change"/> under the same seed and compares the two games.
    /// A finished log is compared over the whole scenario; a mid-game save (SavedDay) over the days already played,
    /// and its change must fall on one of those days.
    /// </summary>
    public static CounterfactualResult Run(ScenarioBundle bundle, InputLog log, CounterfactualChange change)
    {
        var horizon = log.SavedDay is int saved ? saved - 1 : bundle.Scenario.Days;
        if (horizon < 1) throw new ArgumentException("nothing has been played yet, so there is nothing to compare");
        var (altLog, fromDay, text) = Changed(log, change);
        if (fromDay > horizon) throw new ArgumentException($"the change is on day {fromDay}, after the last day compared ({horizon})");

        var original = Replay.Run(bundle, log with { SavedDay = null }, horizon);
        var alternative = Replay.Run(bundle, altLog with { SavedDay = null }, horizon);
        var o = original.View.Debrief();
        var a = alternative.View.Debrief();
        var view = new CounterfactualView(fromDay, horizon, text, o.Scorecard, a.Scorecard,
            a.Scorecard.MissionOutput - o.Scorecard.MissionOutput, Compare(o, a, horizon), NoLongerApplied(original, alternative));
        return new CounterfactualResult(original, alternative, altLog, view);
    }

    // ------------------------------------------------------------------ internals

    private static (InputLog Log, int FromDay, string Text) Changed(InputLog log, CounterfactualChange c)
    {
        var actions = log.Actions.ToList();
        PlayerAction At(int? i) => i is int k && k >= 0 && k < actions.Count
            ? actions[k] : throw new ArgumentOutOfRangeException(nameof(c), $"action index {i} is not in the log ({actions.Count} actions)");
        switch (c.Kind)
        {
            case ChangeKind.Replace:
            {
                var old = At(c.ActionIndex);
                var instead = (c.Action ?? throw new ArgumentException("Replace needs an action")) with { Day = old.Day };
                actions[c.ActionIndex!.Value] = instead;
                return (log with { Actions = actions }, old.Day, $"Day {old.Day}: {Session.Describe(instead)} instead of {Session.Describe(old)}.");
            }
            case ChangeKind.Remove:
            {
                var old = At(c.ActionIndex);
                actions.RemoveAt(c.ActionIndex!.Value);
                return (log with { Actions = actions }, old.Day, $"Day {old.Day}: no {Session.Describe(old)}.");
            }
            case ChangeKind.Insert:
            {
                var day = c.Day ?? throw new ArgumentException("Insert needs a day");
                if (day < 1) throw new ArgumentOutOfRangeException(nameof(c), "Insert day starts at 1");
                var added = (c.Action ?? throw new ArgumentException("Insert needs an action")) with { Day = day };
                int at;
                if (c.ActionIndex is int before)
                {
                    if (At(before).Day != day)
                        throw new ArgumentException($"action {before} is dated day {actions[before].Day}, not day {day}");
                    at = before;
                }
                else at = actions.FindLastIndex(x => x.Day <= day) + 1;
                actions.Insert(at, added);
                return (log with { Actions = actions }, day, $"Day {day}: also {Session.Describe(added)}.");
            }
            case ChangeKind.Prep:
            {
                var prep = (c.PrepChoices ?? throw new ArgumentException("SetPrep needs choices")).ToList();
                var was = log.PrepChoices.Count == 0 ? "none" : string.Join(", ", log.PrepChoices);
                var now = prep.Count == 0 ? "none" : string.Join(", ", prep);
                return (log with { PrepChoices = prep }, 1, $"Prep: {now} instead of {was}.");
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(c), $"unknown change {c.Kind}");
        }
    }

    private static List<string> NoLongerApplied(Session original, Session alternative)
    {
        static IEnumerable<CauseRecord> Rejections(Session s) => s.Records.Where(r => r.Kind == RecordKind.Rejected);
        var before = Rejections(original).GroupBy(r => (r.Day, r.Subject, r.Text)).ToDictionary(g => g.Key, g => g.Count());
        var list = new List<string>();
        foreach (var r in Rejections(alternative))
        {
            var key = (r.Day, r.Subject, r.Text);
            if (before.TryGetValue(key, out var n) && n > 0) { before[key] = n - 1; continue; }
            var reason = r.Text.StartsWith("Rejected: ", StringComparison.Ordinal) ? r.Text["Rejected: ".Length..] : r.Text;
            list.Add($"Day {r.Day}: {r.Subject}: {reason}");
        }
        return list;
    }

    private static List<GapDiff> Compare(DebriefView o, DebriefView a, int horizon)
    {
        var oBands = o.Timeline.ToDictionary(b => (b.SiteId, b.CapabilityId));
        var aBands = a.Timeline.ToDictionary(b => (b.SiteId, b.CapabilityId));
        int Shortfall(CapabilityBand? b, int from, int to)
        {
            if (b is null) return 0;
            var sum = 0;
            for (var d = from; d <= Math.Min(to, horizon); d++)
            {
                var p = d - 1 < b.PermilleByDay.Count ? b.PermilleByDay[d - 1] : -1;
                if (p >= 0 && p < 1000) sum += 1000 - p;
            }
            return sum;
        }
        int Down(CapabilityBand? b, int from, int to)
        {
            if (b is null) return 0;
            var n = 0;
            for (var d = from; d <= Math.Min(to, horizon); d++)
                if (d - 1 < b.PermilleByDay.Count && b.PermilleByDay[d - 1] == 0) n++;
            return n;
        }
        static bool Overlaps(GapView x, GapView y)
            => x.SiteId == y.SiteId && x.CapabilityId == y.CapabilityId && x.StartDay <= y.EndDay && y.StartDay <= x.EndDay;

        var diffs = new List<GapDiff>();
        foreach (var g in o.Gaps)
        {
            var key = (g.SiteId, g.CapabilityId);
            var ob = oBands.GetValueOrDefault(key);
            var ab = aBands.GetValueOrDefault(key);
            var os = Shortfall(ob, g.StartDay, g.EndDay);
            var al = Shortfall(ab, g.StartDay, g.EndDay);
            var change = al == 0 ? GapChange.Gone : al < os ? GapChange.Shrunk : al == os ? GapChange.Unchanged : GapChange.Worse;
            var match = a.Gaps.FirstOrDefault(x => Overlaps(x, g));
            diffs.Add(new GapDiff(change, g.SiteId, g.CapabilityId, g.Label, g.StartDay, g.EndDay,
                os, al, Down(ob, g.StartDay, g.EndDay), Down(ab, g.StartDay, g.EndDay), g.Index, match?.Index));
        }
        foreach (var g in a.Gaps.Where(x => !o.Gaps.Any(y => Overlaps(x, y))))
        {
            var key = (g.SiteId, g.CapabilityId);
            var ob = oBands.GetValueOrDefault(key);
            var ab = aBands.GetValueOrDefault(key);
            diffs.Add(new GapDiff(GapChange.New, g.SiteId, g.CapabilityId, g.Label, g.StartDay, g.EndDay,
                Shortfall(ob, g.StartDay, g.EndDay), Shortfall(ab, g.StartDay, g.EndDay),
                Down(ob, g.StartDay, g.EndDay), Down(ab, g.StartDay, g.EndDay), null, g.Index));
        }
        return diffs.OrderBy(d => d.StartDay).ThenBy(d => d.SiteId, StringComparer.Ordinal)
                    .ThenBy(d => d.CapabilityId, StringComparer.Ordinal).ToList();
    }

    private static string KindOf(PlayerAction a) => a switch
    {
        SurveyAction => "survey", SetLineAction => "setLine", AwardAction => "award", CoordinateAction => "coordinate",
        DefinitizeAction => "definitize", TerminateAction => "terminate", ResolveCommitmentAction => "resolveCommitment",
        EstablishBpaAction => "establishBpa", EstablishIdiqAction => "establishIdiq", CureNoticeAction => "cureNotice", DesignateCorAction => "designateCor", _ => a.GetType().Name,
    };
}
