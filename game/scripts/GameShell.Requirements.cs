using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private readonly Dictionary<string, CheckBox> requirementToggles = new(StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, CheckBox> RequirementToggles => requirementToggles;

    private void RenderRequirementContext(SessionView? view)
    {
        DeskUi.Clear(linkedRequirement); requirementToggles.Clear();
        var cards = view?.Cards.Select(c => view.RequirementEditor(c.Id)).Where(c => c is not null).ToArray() ?? [];
        if (cards.Length == 0) { linkedRequirement.AddChild(DeskUi.Label("No submitted requirements are available.")); return; }
        if (!cards.Any(c => c!.CardId == SelectedCard)) SelectedCard = cards[0]!.CardId;
        var picker = new OptionButton { Name = "RequirementPicker", SizeFlagsHorizontal = SizeFlags.ExpandFill, FitToLongestItem = false };
        foreach (var option in cards) picker.AddItem($"{option!.Title} · {option.Person?.DisplayName ?? option.SubmittedBy}");
        picker.Select(Array.FindIndex(cards, c => c!.CardId == SelectedCard));
        picker.ItemSelected += index => { SelectedCard = cards[(int)index]!.CardId; RenderRequirementContext(session.View); };
        linkedRequirement.AddChild(picker);
        var card = cards.Single(c => c!.CardId == SelectedCard)!;
        linkedRequirement.AddChild(DeskUi.Label($"{card.Title} · {card.Person?.WithUnit ?? card.SubmittedBy}\nPeak demand: {card.PeakDemandUnits} {card.Unit}\nRequested day {card.RequestedStartDay} for {card.DurationDays} days"));
        if (session.IsReplay) linkedRequirement.AddChild(DeskUi.Label("Changing a line takes control and stops future replay actions."));
        var market = DeskUi.Button("Go to market", () => GoToMarket(card.CardId));
        market.SetMeta("market_card", card.CardId); market.Disabled = view!.Finished; linkedRequirement.AddChild(market);
        foreach (var line in card.Lines)
        {
            var toggle = new CheckBox { Text = line.Label, ButtonPressed = line.On, Disabled = view!.Finished,
                AutowrapMode = TextServer.AutowrapMode.WordSmart };
            toggle.Toggled += on => SendAction(new SetLineAction { Day = session.View!.Day, Card = card.CardId, Line = line.Id, On = on });
            linkedRequirement.AddChild(toggle); requirementToggles.Add(line.Id, toggle);
            if (!line.On && line.GapIfOff is not null)
            {
                var gap = DeskUi.Label(line.GapIfOff); gap.SetMeta("gap_line", line.Id);
                gap.AddThemeColorOverride("font_color", new Color(1, .75f, .25f)); linkedRequirement.AddChild(gap);
            }
        }
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
                linkedRequirement.AddChild(ask);
            }
        }
    }
}
