using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal Button RequirementMarketButton { get; private set; } = null!;
    internal Dictionary<string, Button> RequirementAskButtons { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, Button> RequirementAddButtons { get; } = new(StringComparer.Ordinal);

    private void RenderRequirementContext(SessionView? view)
    {
        DeskUi.Clear(linkedRequirement); DeskUi.Clear(requirementFooter); RequirementAskButtons.Clear(); RequirementAddButtons.Clear();
        var scroll = (ScrollContainer)linkedRequirement.GetParent();
        scroll.FollowFocus = true;
        var cards = view?.Cards.Select(c => view.RequirementEditor(c.Id)).OfType<RequirementEditorView>().ToArray() ?? [];
        if (cards.Length == 0) { linkedRequirement.AddChild(DeskUi.Label("No submitted requirements are available.")); return; }
        if (!cards.Any(c => c.CardId == SelectedCard)) SelectedCard = cards[0].CardId;
        var picker = new OptionButton { Name = "RequirementPicker", ClipText = true, SizeFlagsHorizontal = SizeFlags.ExpandFill, FitToLongestItem = false };
        foreach (var option in cards) picker.AddItem($"{option.Title} · {option.Person?.DisplayName ?? option.SubmittedBy}");
        picker.Select(Array.FindIndex(cards, c => c.CardId == SelectedCard));
        picker.ItemSelected += index => { SelectedCard = cards[(int)index].CardId; scroll.ScrollVertical = 0; RenderRequirementContext(session.View); };
        linkedRequirement.AddChild(picker);
        var card = cards.Single(c => c.CardId == SelectedCard);
        linkedRequirement.AddChild(DeskUi.Label($"{card.Title} · {card.Person?.WithUnit ?? card.SubmittedBy}\nPeak demand: {card.PeakDemandUnits} {card.Unit}\nRequested day {card.RequestedStartDay} for {card.DurationDays} days"));
        if (card.Message is { Length: > 0 } message) linkedRequirement.AddChild(DeskUi.Label(message));
        if (card.HeardAt is { } heardAt)
        {
            var heard = DeskUi.Label(heardAt); heard.SetMeta("heard_at", card.CardId); linkedRequirement.AddChild(heard);
        }
        if (session.IsReplay) linkedRequirement.AddChild(DeskUi.Label("Asking about this request or adding what you learned takes control and stops future replay actions."));

        foreach (var line in card.Lines.Where(l => l.Known))
        {
            var included = DeskUi.Label(line.On ? $"The request includes {line.Label}." : $"Not in the request yet: {line.Label}.");
            included.SetMeta("requirement_line", line.Id); included.SetMeta("included", line.On); linkedRequirement.AddChild(included);
            if (line.Learned is { } learned)
            {
                if (line.LearnedFrom is { } person)
                {
                    var speaker = DeskUi.Label($"{person.WithUnit} told you:");
                    speaker.SetMeta("learned_from", line.Id); linkedRequirement.AddChild(speaker);
                }
                var said = DeskUi.Label($"“{learned}”");
                said.SetMeta("learned_line", line.Id); linkedRequirement.AddChild(said);
            }
            if (!line.On && line.GapIfOff is { } consequence)
            {
                var gap = DeskUi.Label(consequence); gap.SetMeta("gap_line", line.Id);
                gap.AddThemeColorOverride("font_color", TabletopTheme.Warning); linkedRequirement.AddChild(gap);
            }
            if (!line.CanAdd) continue;
            var add = DeskUi.Button("Add what you learned", () => AddLearnedRequirementLine(card.CardId, line.Id));
            add.TooltipText = line.Label; add.SetMeta("add_line", line.Id); add.Disabled = view!.Finished;
            var addReason = DeskUi.ActionReason(linkedRequirement, add);
            DeskUi.Gate(add, addReason, view.Finished ? DeskUi.FinishedReason : null); RequirementAddButtons.Add(line.Id, add);
        }
        if (card.WhoToAsk?.Any(option => !option.Asked && !option.Pending) == true)
            linkedRequirement.AddChild(DeskUi.Label("A request is often missing something. These people may know."));
        foreach (var option in card.WhoToAsk ?? [])
        {
            linkedRequirement.AddChild(DeskUi.Label(option.Person.WithUnit));
            var action = new AskAboutAction { Day = view!.Day, Card = card.CardId, Person = option.Person.Id };
            var preview = view.PreviewVisit(action);
            var ask = DeskUi.Button(option.Pending ? "Visit in progress" : option.Asked ? "Already asked about this request"
                : $"Ask {option.Person.DisplayName}", () => AskAboutRequirement(card.CardId, option.Person.Id));
            ask.SetMeta("ask_person", option.Person.Id);
            ask.Disabled = view.Finished || option.Asked || option.Pending || !preview.Accepted;
            var askReason = DeskUi.ActionReason(linkedRequirement, ask);
            DeskUi.Gate(ask, askReason, view.Finished ? DeskUi.FinishedReason : option.Pending ? "The visit is already in progress."
                : option.Asked ? "You have already asked about this request." : preview.HardStops.FirstOrDefault()); RequirementAskButtons.Add(option.Person.Id, ask);
            if (option.Asked || option.Pending) continue;
            static string Range(int min, int max) => min == max ? min.ToString() : $"{min}–{max}";
            var timing = DeskUi.Label($"This takes {Range(preview.ShopHoursMin, preview.ShopHoursMax)} shop {(preview.ShopHoursMin == 1 && preview.ShopHoursMax == 1 ? "hour" : "hours")}. The earliest result is day {Range(preview.EarliestStartMin, preview.EarliestStartMax)}{DeskUi.QueueClause(preview.QueueAheadHours)}.");
            timing.SetMeta("ask_preview", option.Person.Id); linkedRequirement.AddChild(timing);
            foreach (var stop in preview.HardStops.Concat(preview.SoftGaps)) linkedRequirement.AddChild(DeskUi.Label(stop));
        }
        var market = DeskUi.Button("Go to market", () => GoToMarket(card.CardId)); RequirementMarketButton = market;
        market.SetMeta("market_card", card.CardId); market.Disabled = view!.Finished;
        var marketReason = DeskUi.ActionReason(requirementFooter, market);
        DeskUi.Gate(market, marketReason, view.Finished ? DeskUi.FinishedReason : null);
        linkedRequirement.AddChild(DeskUi.Label("Dependency checklist"));
        foreach (var dependency in card.Dependencies)
        {
            var label = DeskUi.Label($"{dependency.Label}: {Words(dependency.Coverage).ToLowerInvariant()}" +
                (dependency.CoveredBy.Count > 0 ? $" · {string.Join(", ", dependency.CoveredBy.Select(view.ArrangementName))}" : ""));
            label.SetMeta("dependency", dependency.CapabilityId); linkedRequirement.AddChild(label);
            if (dependency.UsualOwner is { } owner)
            {
                var responsibility = DeskUi.Label($"{dependency.Label} is usually {owner.TrimEnd('.')}. " +
                    (dependency.OwnerEngaged ? "The usual owner is engaged." : "The usual owner has not been engaged."));
                responsibility.SetMeta("dependency_owner", dependency.CapabilityId);
                responsibility.SetMeta("owner_engaged", dependency.OwnerEngaged); linkedRequirement.AddChild(responsibility);
            }
            if (dependency.CapabilityId == "fuel" && dependency.Coverage == "none" && !dependency.OwnerEngaged
                && view.Scenario.Actors.AcsaPartner is not null)
            {
                var ask = DeskUi.Button("Ask the J4 about fuel for " + card.SiteName, () => OpenJ4FuelVisit(card.SiteId));
                ask.SetMeta("dependency_visit", dependency.CapabilityId); ask.Disabled = view.Finished;
                var reason = DeskUi.ActionReason(linkedRequirement, ask);
                DeskUi.Gate(ask, reason, view.Finished ? DeskUi.FinishedReason : null);
            }
        }
    }

    private void AskAboutRequirement(string card, string person)
    {
        if (session.View is not { Finished: false } view || PlayingDay || SelectedCard != card
            || view.RequirementEditor(card)?.WhoToAsk?.Any(a => a.Person.Id == person && !a.Asked && !a.Pending) != true) return;
        var action = new AskAboutAction { Day = view.Day, Card = card, Person = person };
        if (view.PreviewVisit(action).Accepted) SendAction(action);
    }

    private void AddLearnedRequirementLine(string card, string line)
    {
        if (session.View is not { Finished: false } view || PlayingDay || SelectedCard != card
            || view.RequirementEditor(card)?.Lines.Any(l => l.Id == line && l.Known && l.CanAdd) != true) return;
        SendAction(new AddLineAction { Day = view.Day, Card = card, Line = line });
    }
}
