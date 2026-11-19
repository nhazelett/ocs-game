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
/// <param name="Says">What the person sending the prep message says about it (M2R-02); null when the scenario wrote nothing.</param>
/// <param name="Default">Shown preselected (scenario prepBriefing.defaults; they fit the budget). Never true for a choice
/// that changes nothing here.</param>
/// <param name="Changes">One line, shown in view, for what the choice changes in this scenario (DESK-76, D32): "The
/// contract-writing system works from day 3, not day 12."; null when it changes nothing here.</param>
/// <param name="ChangesHere">False when the choice changes nothing in this scenario (D32): the page does not offer it,
/// and it is never a default. Still accepted by <see cref="PrepScreen.Check"/>, so old saves and logs start.</param>
public sealed record PrepOption(string Id, string Label, int PrepDays, IReadOnlyList<PrepEffectLine> Effects,
    string? Says = null, bool Default = false, string? Changes = null, bool ChangesHere = true);

/// <summary>The prep screen: the budget and every choice. The choices together cost more than the budget.</summary>
/// <param name="From">Who sends the prep message (M2R-02), or null.</param>
/// <param name="Intro">Their opening, e.g. "You have fourteen days before wheels up."</param>
/// <param name="Defaults">The preselected picks, in rules order; empty when the scenario names none.</param>
/// <param name="Where">Where the player is going, in two sentences from the sender (DESK-76, D32): the place, how many
/// people, what is known to be coming. Shown before the list; null when the scenario wrote none.</param>
/// <remarks>Options are every rules choice in rules order; the page offers only those with ChangesHere (D32), and
/// OfferedDays sums those.</remarks>
public sealed record PrepView(string ScenarioTitle, int BudgetDays, int AllChoicesDays, IReadOnlyList<PrepOption> Options,
    PersonView? From = null, string? Intro = null, IReadOnlyList<string>? Defaults = null, string? Where = null)
{
    /// <summary>The choices the page offers: those that change something here (DESK-76, D32).</summary>
    public IReadOnlyList<PrepOption> Offered => [.. Options.Where(o => o.ChangesHere)];
    /// <summary>Prep days of every offered choice together.</summary>
    public int OfferedDays => Offered.Sum(o => o.PrepDays);
}

/// <summary>Whether a set of picks can start a game. Stops use the same wording Session.Create would throw.</summary>
public sealed record PrepCheck(bool Accepted, int UsedDays, int BudgetDays, IReadOnlyList<string> Stops);

/// <summary>Builds the prep screen and checks picks against the scenario's prep-day budget.</summary>
public static class PrepScreen
{
    private static string Money(long cents) => Session.Money(cents);

    public static PrepView View(ScenarioBundle bundle)
    {
        var pb = bundle.Scenario.PrepBriefing;
        var options = bundle.Rules.PrepChoices.Select(c => Choice(bundle, c.Id)).ToList();
        PersonView? from = null;
        if (pb is not null && bundle.Scenario.People.FirstOrDefault(p => p.Id == pb.From) is { } p)
            from = new PersonView(p.Id, p.Name, p.Rank, p.Unit, p.Role, p.Portrait, p.Lines,
                p.Vendor is null ? null : bundle.Scenario.Vendors.FirstOrDefault(v => v.Id == p.Vendor)?.Name);
        return new PrepView(bundle.Scenario.Title, bundle.Scenario.PrepDays, options.Sum(o => o.PrepDays), options,
            from, pb?.Intro, options.Where(o => o.Default).Select(o => o.Id).ToList(),
            string.IsNullOrWhiteSpace(pb?.Where) ? null : pb!.Where);
    }

    /// <summary>One rules prep choice as this scenario sees it, listed or not: its effects, what the person says, and the
    /// one line for what it changes here (DESK-76, D32).</summary>
    public static PrepOption Choice(ScenarioBundle bundle, string id)
    {
        var c = bundle.Rules.PrepChoices.First(x => x.Id == id);
        var lines = c.Effects.Select(e => Line(bundle, e)).ToList();
        var changes = lines.Where(l => l.Changes is not null).Select(l => l.Changes!).ToList();
        var pb = bundle.Scenario.PrepBriefing;
        var here = changes.Count > 0;
        return new PrepOption(c.Id, c.Label, c.PrepDays, [.. lines.Select(l => l.Effect)],
            pb is not null && pb.Choices.TryGetValue(c.Id, out var says) ? says : null,
            here && (pb?.Defaults.Contains(c.Id) ?? false), here ? string.Join(" ", changes) : null, here);
    }

    /// <summary>One effect: the old with and without lines, and what it changes in this scenario, or null.</summary>
    private static (PrepEffectLine Effect, string? Changes) Line(ScenarioBundle bundle, EffectData e)
    {
        var scenario = bundle.Scenario;
        var shop = scenario.Shop;
        var model = bundle.Rules.Model;
        int Int() => e.Value is { ValueKind: JsonValueKind.Number } v ? v.GetInt32() : 0;
        static string Times(double f) => f.ToString("0.##", CultureInfo.InvariantCulture);
        (PrepEffectLine, string?) Said(string with, string without, string? changes) => (new(with, without, changes is not null), changes);
        switch (e.Kind)
        {
            case "shop.gpcAvailableDay":
            {
                var day = Int();
                var changes = shop.GpcAvailableDay is not { } g ? $"Purchase cards work from day {day}; without this there are none this deployment."
                    : g > day ? $"Purchase cards work from day {day}, not day {g}." : null;
                return Said($"Purchase cards usable from day {day}.",
                    shop.GpcAvailableDay is { } g2 ? $"Purchase cards usable from day {g2}." : "No purchase card accounts this deployment.", changes);
            }
            case "shop.payingAgentAvailableDay":
            {
                var day = Int();
                var changes = shop.PayingAgentAvailableDay is not { } p ? $"A paying agent with cash for SF 44 buys from day {day}; without this there is none."
                    : p > day ? $"A paying agent with cash for SF 44 buys from day {day}, not day {p}." : null;
                return Said($"Paying agent with cash on hand from day {day} (SF 44 buys).",
                    shop.PayingAgentAvailableDay is { } p2 ? $"Paying agent from day {p2}." : "No paying agent this deployment.", changes);
            }
            case "shop.cashReplenishLagDays":
            {
                var lag = Int();
                return Said($"FM restores cash {lag} days after it runs low.", $"FM restores cash {shop.CashReplenishLagDays} days after it runs low.",
                    shop.CashReplenishLagDays > lag ? $"Finance restores your cash {lag} days after it runs low, not {shop.CashReplenishLagDays}." : null);
            }
            case "shop.systemOnlineDay":
            {
                var day = Int();
                return Said($"Contract-writing system online from day {day}.",
                    $"Contract-writing system online from day {shop.SystemOnlineDay}; written actions are manual until then.",
                    shop.SystemOnlineDay > day ? $"The contract-writing system works from day {day}, not day {shop.SystemOnlineDay}." : null);
            }
            case "model.manualContractTimeFactorOverride":
            {
                var f = e.Value is { ValueKind: JsonValueKind.Number } n ? n.GetDouble() : 1.0;
                var manualDays = shop.SystemOnlineDay > 1;
                return Said($"Manual written actions take {Times(f)} times the system time.",
                    $"Manual written actions take {Times(model.ManualContractTimeFactor)} times the system time.",
                    manualDays && f < model.ManualContractTimeFactor
                        ? $"Written work before the system is up takes {Times(f)} times as long as on the system, not {Times(model.ManualContractTimeFactor)}." : null);
            }
            case "shop.warrantCents":
            {
                var to = e.Value is { ValueKind: JsonValueKind.Number } w ? w.GetInt64() : (long?)null;
                var changes = shop.WarrantCents is not { } now ? null
                    : to is null ? $"Your warrant covers every buy in this scenario, not only those up to {Money(now)}."
                    : to > now ? $"Your warrant goes to {Money(to.Value)}, from {Money(now)}." : null;
                return Said(to is { } t ? $"Warrant of {Money(t)}." : "Warrant sufficient for this scenario.",
                    shop.WarrantCents is { } c ? $"Warrant of {Money(c)}." : "Warrant already sufficient for this scenario.", changes);
            }
            case "vehicles.infoTier":
            {
                var held = scenario.Vehicles.Where(x => x.Kind is "bpa" or "idiq" && x.VisibleOnDay1)
                    .Select(x => $"the {scenario.Vendors.FirstOrDefault(v => v.Id == x.Vendor)?.Name ?? "vendor"} {(x.Kind == "bpa" ? "BPA" : "IDIQ")}")
                    .ToList();
                return Said("Existing agreements shown on the requests they could serve.", "Existing agreements found in the buying form.",
                    held.Count == 0 ? null : $"Each request names the agreements already in place that could serve it from day 1 ({And(held)}).");
            }
            case "vendors.infoTierUpgrade":
            {
                var listed = scenario.Vendors.Where(v => Session.OnVendorList(v) && !v.VehicleOnly).Select(v => v.Name).ToList();
                return Said("Listed vendors' cards start filled in.", "Vendor cards fill in as you meet them.",
                    listed.Count == 0 ? null : $"The vendor cards for {And(listed)} start filled in: price, payment and where they deliver, with no meeting.");
            }
            case "actors.partnersConfirmed":
            {
                var who = new List<string>();
                if (scenario.Actors.AcsaPartner is { Present: true } a && a.Stock.Values.Any(n => n > 0)) who.Add(a.Name);
                if (scenario.Actors.HostNation is { AgreementExists: true } h && h.Capacity.Values.Any(n => n > 0)) who.Add(h.Name);
                return Said("Partner and host nation agreements confirmed.", "Partner and host nation agreements unconfirmed.",
                    who.Count == 0 ? null : $"Each request says what {And(who)} can spare for it before you ask.");
            }
            case "vetting.shopDaysFactor":
                return (new("Vetting recorded for the debrief.", "Not recorded.", false), null);
            default:
                return (new($"{e.Kind}: recorded for the debrief.", "Not recorded.", false), null);
        }
    }

    private static string And(IReadOnlyList<string> w)
        => w.Count switch { 0 => "", 1 => w[0], 2 => $"{w[0]} and {w[1]}", _ => string.Join(", ", w.Take(w.Count - 1)) + $" and {w[^1]}" };

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
