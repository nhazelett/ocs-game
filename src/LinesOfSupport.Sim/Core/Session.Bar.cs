using System.Globalization;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// The commander's bar (DESK-74, docs/design/desk-redesign.md D28). Every scenario has pass lines and no screen showed
// them: the careful tutorial player said "Trying to do: the game never told me up front", and a player who had missed
// three lines read the debrief as a list of gaps with no verdict. The commander says the lines in their own words on the
// turnover's last page, with how long the player is there; Today shows each with its count so far; the debrief opens
// with the verdict. Read only: built from the metrics, changes nothing.
public sealed partial class Session
{
    /// <summary>The bar as it stands: one line per pass line with the commander's words, the count so far and its state.</summary>
    internal BarView BuildBar()
    {
        var m = Metrics;
        var lines = Scenario.PassCriteria.Select(p => BarLineFor(p, m)).ToList();
        var says = string.Join(" ", Scenario.PassCriteria.Select(p => p.Says ?? Upper(p.Label.TrimEnd('.')) + "."));
        var length = $"That's for the {Plural(LastDay, "day")} you're here.";
        return new BarView(BuildPerson(Scenario.Actors.Commander.Person), LastDay, Finished,
            (says.Length == 0 ? length : says + " " + length).Trim(), lines);
    }

    private BarLine BarLineFor(Data.PassCriterionData p, IReadOnlyDictionary<string, double> m)
    {
        var count = m.TryGetValue(p.Metric, out var v) ? v : 0;
        var ok = Meets(count, p.Comparator, p.Value);
        var parts = p.Metric.Split('.');
        string text;
        BarState state;
        var sofar = Finished ? "" : " so far";
        if (parts is ["site", var site, var cap, "downDays"] && Scenario.Sites.Any(s => s.Id == site) && _capData.ContainsKey(cap))
        {
            var n = (long)count;
            text = $"{CapLabel(cap)} at {SiteName(site)}: out {Plural(n, "day")}{sofar}.";
            // Amber once the days allowed are used up: one more bad day misses it. A line that allows none is met or missed.
            state = !ok ? BarState.Missed : !Finished && p.Comparator == "<=" && p.Value > 0 && n >= p.Value ? BarState.AtRisk : BarState.Met;
        }
        else if (p.Metric == "compliance.unauthorizedCommitments")
        {
            text = $"Unauthorized commitments: {((long)count).ToString(CultureInfo.InvariantCulture)}{sofar}.";
            state = ok ? BarState.Met : BarState.Missed;
        }
        else if (p.Metric.StartsWith("mission.", StringComparison.Ordinal))
        {
            // A running average: under the bar is amber while days remain, and missed only at the end.
            text = $"Mission output: {Math.Floor(count * 100).ToString(CultureInfo.InvariantCulture)}%{sofar}, against {Math.Round(p.Value * 100).ToString(CultureInfo.InvariantCulture)}%.";
            state = ok ? BarState.Met : Finished ? BarState.Missed : BarState.AtRisk;
        }
        else
        {
            text = $"{p.Label}: {count.ToString("0.###", CultureInfo.InvariantCulture)}{sofar}.";
            state = ok ? BarState.Met : BarState.Missed;
        }
        return new BarLine(p.Id, p.Says ?? p.Label, text, count, state);
    }

    private static bool Meets(double a, string op, double b) => op switch
    {
        "<=" => a <= b, ">=" => a >= b, "<" => a < b, ">" => a > b, "==" => Math.Abs(a - b) < 1e-9, _ => false,
    };
}
