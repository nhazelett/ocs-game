using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// Fast, cheap, good: pick two (DESK-06, docs/design/desk-redesign.md D7). On a written contract the player says which two
// matter; the third pays. Cheap and good: you competed it, so the action takes more shop days. Fast and good: you paid a
// premium. Fast and cheap: you took the first quote, so the vendor is more likely to fall down later. Three rule numbers
// (rules pickTwo). No pick (old logs, the policies) means none of this, so nothing that ran before moves. Built to be
// stripped: delete this file, AwardAction.Priorities, Arrangement.Pick and the calls marked DESK-06.
public sealed partial class Session
{
    private static readonly string[] PickWords = ["fast", "cheap", "good"];
    internal const string CompetedPick = "cheap+good", PremiumPick = "fast+good", FirstQuotePick = "fast+cheap";

    private PickTwoRulesData PickTwo => Rules.PickTwo;

    /// <summary>The pick in a fixed order ("fast+cheap", "fast+good", "cheap+good"); null when none was made or it is not
    /// two of the three.</summary>
    internal static string? PickKey(IReadOnlyList<string>? priorities)
    {
        if (priorities is null) return null;
        var words = PickWords.Where(priorities.Contains).ToList();
        return words.Count == 2 && priorities.Count == 2 ? string.Join("+", words) : null;
    }

    internal static string PickWordsText(string key) => key.Replace("+", " and ");

    private IEnumerable<string> PickStops(AwardAction a, InstrumentData inst)
    {
        if (a.Priorities is null) yield break;
        if (!inst.WrittenTerms) { yield return "fast, cheap or good is picked on written contracts only"; yield break; }
        if (PickKey(a.Priorities) is null) yield return "pick two of fast, cheap and good";
    }

    /// <summary>Fast and good adds the premium to the daily price and the mobilization.</summary>
    private long WithPremium(string? key, long cents) => key == PremiumPick ? cents + Fixed.MulPermille(cents, PickTwo.PremiumPermille) : cents;

    private int CompeteDays(string? key) => key == CompetedPick ? PickTwo.CompeteShopDays : 0;

    private int FirstQuoteReliability(string? key, int reliabilityPermille)
        => key == FirstQuotePick ? Math.Max(0, reliabilityPermille - PickTwo.FirstQuoteFailurePermille) : reliabilityPermille;

    /// <summary>The trade in one sentence for the preview, with this award's numbers; null without a pick.</summary>
    internal string? PickTrade(AwardAction a, long totalCents)
    {
        switch (PickKey(a.Priorities))
        {
            case CompetedPick:
                return $"Cheap and good: you compete it among vendors, which adds about {Plural(PickTwo.CompeteShopDays, "day")} of shop work before award.";
            case PremiumPick:
            {
                var without = totalCents * 1000 / (1000 + PickTwo.PremiumPermille);
                return $"Fast and good: you pay a {PickTwo.PremiumPermille / 10}% premium for a quick award without cutting corners, about {Money(totalCents - without)} of this price.";
            }
            case FirstQuotePick:
                return "Fast and cheap: you take the first quote, so the award is quick and the price is the vendor's, but the vendor is more likely to fall down on the job later.";
            default:
                return null;
        }
    }

    /// <summary>What the debrief adds when the pick explains a failure or a late start; empty otherwise.</summary>
    private string PickFailureNote(Arrangement a)
        => a.Pick == FirstQuotePick ? " You took the first quote (fast and cheap), which made a failure more likely every day it ran." : "";

    private string PickLateNote(Arrangement a)
        => a.Pick == CompetedPick ? $" It was competed (cheap and good), which added about {Plural(PickTwo.CompeteShopDays, "day")} of shop work." : "";
}
