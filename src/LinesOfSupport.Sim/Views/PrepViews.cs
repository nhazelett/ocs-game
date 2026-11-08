using System.Globalization;
using System.Text.Json;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Views;

// Pre-deployment window contract (backlog M2-09, SPEC 4.2, ADR-0006 addendum). Built from the bundle before any
// Session exists, so the game can show the prep screen first and then call Session.Create with the picks.

/// <summary>One thing a prep choice changes, as the player sees it: with the choice, and without it.</summary>
/// <param name="HasEffect">False for choices this build records for the debrief but does not yet model.</param>
public sealed record PrepEffectLine(string With, string Without, bool HasEffect);

/// <summary>A prep choice: what it costs in prep days and what it changes.</summary>
public sealed record PrepOption(string Id, string Label, int PrepDays, IReadOnlyList<PrepEffectLine> Effects);

/// <summary>The prep screen: the budget and every choice. The choices together cost more than the budget.</summary>
public sealed record PrepView(string ScenarioTitle, int BudgetDays, int AllChoicesDays, IReadOnlyList<PrepOption> Options);

/// <summary>Whether a set of picks can start a game. Stops use the same wording Session.Create would throw.</summary>
public sealed record PrepCheck(bool Accepted, int UsedDays, int BudgetDays, IReadOnlyList<string> Stops);

/// <summary>Builds the prep screen and checks picks against the scenario's prep-day budget.</summary>
public static class PrepScreen
{
    private static string Money(long cents) => Session.Money(cents);

    public static PrepView View(ScenarioBundle bundle)
    {
        var shop = bundle.Scenario.Shop;
        var model = bundle.Rules.Model;
        PrepEffectLine Line(EffectData e)
        {
            int Int() => e.Value is { ValueKind: JsonValueKind.Number } v ? v.GetInt32() : 0;
            return e.Kind switch
            {
                "shop.gpcAvailableDay" => new($"Purchase cards usable from day {Int()}.",
                    shop.GpcAvailableDay is { } g ? $"Purchase cards usable from day {g}." : "No purchase card accounts this deployment.", true),
                "shop.payingAgentAvailableDay" => new($"Paying agent with cash on hand from day {Int()} (SF 44 buys).",
                    shop.PayingAgentAvailableDay is { } p ? $"Paying agent from day {p}." : "No paying agent this deployment.", true),
                "shop.cashReplenishLagDays" => new($"FM restores cash {Int()} days after it runs low.",
                    $"FM restores cash {shop.CashReplenishLagDays} days after it runs low.", true),
                "shop.systemOnlineDay" => new($"Contract-writing system online from day {Int()}.",
                    $"Contract-writing system online from day {shop.SystemOnlineDay}; written actions are manual until then.", true),
                "model.manualContractTimeFactorOverride" => new(
                    $"Manual written actions take {(e.Value is { ValueKind: JsonValueKind.Number } f ? f.GetDouble() : 1.0).ToString("0.##", CultureInfo.InvariantCulture)} times the system time.",
                    $"Manual written actions take {model.ManualContractTimeFactor.ToString("0.##", CultureInfo.InvariantCulture)} times the system time.", true),
                "shop.warrantCents" => e.Value is { ValueKind: JsonValueKind.Number } w
                    ? new($"Warrant of {Money(w.GetInt64())}.", shop.WarrantCents is { } c ? $"Warrant of {Money(c)}." : "Warrant already sufficient for this scenario.", shop.WarrantCents is not null)
                    : new("Warrant sufficient for this scenario.", shop.WarrantCents is { } c2 ? $"Warrant of {Money(c2)}." : "Warrant already sufficient for this scenario.", shop.WarrantCents is not null),
                _ => new($"{e.Kind}: recorded for the debrief.", "Not recorded.", false),
            };
        }
        var options = bundle.Rules.PrepChoices.Select(c => new PrepOption(c.Id, c.Label, c.PrepDays, c.Effects.Select(Line).ToList())).ToList();
        return new PrepView(bundle.Scenario.Title, bundle.Scenario.PrepDays, options.Sum(o => o.PrepDays), options);
    }

    public static PrepCheck Check(ScenarioBundle bundle, IEnumerable<string> picks)
    {
        var stops = new List<string>();
        var used = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in picks)
        {
            if (!seen.Add(id)) { stops.Add($"prep choice {id} is picked twice"); continue; }
            var choice = bundle.Rules.PrepChoices.FirstOrDefault(c => c.Id == id);
            if (choice is null) { stops.Add($"prep choice {id} is not in rules {bundle.Rules.RulesVersion}"); continue; }
            used += choice.PrepDays;
        }
        if (used > bundle.Scenario.PrepDays)
            stops.Add($"prep choices use {used} prep-days; the scenario allows {bundle.Scenario.PrepDays}");
        return new PrepCheck(stops.Count == 0, used, bundle.Scenario.PrepDays, stops);
    }
}
