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
        // DESK-155 (c), Claude play 21 Nov, third play, note 28: what counts as out, said once with the lines that count it.
        if (Scenario.PassCriteria.Any(p => p.Metric.EndsWith(".downDays", StringComparison.Ordinal)))
            says += " " + OutDaySaid();
        var length = $"That's for the {Plural(LastDay, "day")} you're here.";
        var from = BuildPerson(Scenario.Actors.Commander.Person);
        return new BarView(from, LastDay, Finished, (says.Length == 0 ? length : says + " " + length).Trim(), lines)
        {
            Closing = Finished && Scenario.Tutorial && lines.Count > 0 ? TutorialClosing(from, lines) : null,
        };
    }

    /// <summary>"A day counts as out when less than half of it is supplied; at half or more it is short." From
    /// model.downThresholdPermille.</summary>
    private string OutDaySaid()
    {
        var at = Rules.Model.DownThresholdPermille;
        var share = at == 500 ? "half" : $"{PercentSaid(at)}";
        return $"A day counts as out when less than {share} of it is supplied; at {share} or more it is short.";
    }

    /// <summary>DESK-149 (a), Claude play 21 Nov, second play, note 10: the debrief had four Met lines and Play again, and
    /// nothing said the tutorial was over or how it went. "The tutorial is done: all four of Col Ward's lines held."</summary>
    private static string TutorialClosing(Views.PersonView? from, IReadOnlyList<BarLine> lines)
    {
        var whose = from is null ? "the commander's"
            : Possessive(from.Rank.Length == 0 ? from.Name : $"{from.Rank} {from.Name.Split(' ')[^1]}");
        int n = lines.Count, held = lines.Count(l => l.State == BarState.Met);
        var how = n == 1 ? (held == 1 ? $"{whose} one line held" : $"{whose} one line did not hold")
            : held == n ? (n == 2 ? $"both of {whose} lines held" : $"all {Views.DebriefWords.Number(n)} of {whose} lines held")
            : held == 0 ? $"none of {whose} {Views.DebriefWords.Number(n)} lines held"
            : $"{Views.DebriefWords.Number(held)} of {whose} {Views.DebriefWords.Number(n)} lines held";
        return $"The tutorial is done: {how}.";
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
            // D43 (DESK-129): the allowance is a total for the stay, so the count says it against what is allowed.
            text = p.Comparator == "<=" && p.Value >= 1
                ? $"{CapLabel(cap)} at {SiteName(site)}: out {n} of the {Plural((long)p.Value, "day")} allowed{sofar}."
                : p.Comparator == "<=" && p.Value == 0 ? $"{CapLabel(cap)} at {SiteName(site)}: out {Plural(n, "day")}{sofar}; none allowed."
                : $"{CapLabel(cap)} at {SiteName(site)}: out {Plural(n, "day")}{sofar}.";
            // DESK-145 (a), Claude play 21 Nov, note 2: once a day has been short but not out, say it is not an out day.
            if (n == 0 && m.TryGetValue($"site.{site}.{cap}.degradedDays", out var shortDays) && shortDays > 0
                && m.TryGetValue($"site.{site}.{cap}.minPercent", out var low) && low > 0)
                text += $" A day at {(long)low}% counts as short, not out.";
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
