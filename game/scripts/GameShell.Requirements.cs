using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal Button RequestInitiativeButton { get; private set; } = null!;
    internal Dictionary<string, Button> RequirementAskButtons { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, Button> RequirementAddButtons { get; } = new(StringComparer.Ordinal);

    private void RenderOwnInitiativeBuys(SessionView view)
    {
        linkedRequirement.AddChild(DeskUi.Heading("Bought on your own initiative"));
        var ownAwards = session.CreateSave().Actions.OfType<AwardAction>().Where(a => a.Card is null).Select(a => a.Id).ToHashSet(StringComparer.Ordinal);
        var records = view.Arrangements.Where(a => a.CardId is null && !a.IsCoordination).Select(a => a.CauseId)
            .Concat(view.Work.Where(w => w.Kind == WorkKind.Award && w.SubjectId is { } id && ownAwards.Contains(id)).Select(w => w.CauseId))
            .ToHashSet(StringComparer.Ordinal);
        var buys = view.Decisions().Where(d => records.Contains(d.RecordId)).ToArray();
        if (buys.Length == 0) linkedRequirement.AddChild(DeskUi.Label("Nothing bought on your own initiative yet."));
        var hash = session.StateHash; var target = awardTarget;
        foreach (var decision in buys)
        {
            var text = decision.Sentence + "\n" + decision.Now;
            if (view.Arrangements.FirstOrDefault(a => a.CauseId == decision.RecordId) is not { } arrangement)
            {
                var entry = DeskUi.Label(text); entry.SetMeta("initiative_decision", decision.RecordId); linkedRequirement.AddChild(entry);
                continue;
            }
            var button = DeskUi.Button(text + "\nOpen its contract page →", () =>
            {
                if (hash != session.StateHash || AtFrontDoor || ReadingTurnover || PlayingDay || ReadingDaySummary
                    || !RequestWorkspace.Visible || awardTarget != target || SelectedCard is not null) return;
                OpenContractView(arrangement.Id, fromInitiative: target);
            });
            button.SetMeta("initiative_contract", arrangement.Id); button.SetMeta("initiative_decision", decision.RecordId); linkedRequirement.AddChild(button);
        }
    }

    private void RenderRequirementContext(SessionView? view)
    {
        DeskUi.Clear(linkedRequirement); DeskUi.Clear(requirementFooter); RequirementAskButtons.Clear(); RequirementAddButtons.Clear();
        RequestDecisionRows = null; RequestPartLabels.Clear(); RequestBuyWarnings.Clear();
        var scroll = (ScrollContainer)linkedRequirement.GetParent();
        scroll.FollowFocus = true;
        if (view is not null && awardTarget is not null && !view.Cards.Any(c => c.Id == awardTarget))
        {
            linkedRequirement.AddChild(DeskUi.Heading("Buy on your own initiative"));
            var target = AwardTargets(view).FirstOrDefault(t => t.Id == awardTarget);
            if (target is not null)
            {
                linkedRequirement.AddChild(DeskUi.Label(target.Label));
                var explanation = view.OwnInitiative.FirstOrDefault(t => t.SiteId == target.Site && t.CapabilityId == target.Capability);
                linkedRequirement.AddChild(DeskUi.Label(explanation?.Why ?? "Arrange support nobody has requested for this site and capability."));
            }
            RenderOwnInitiativeBuys(view);
            return;
        }
        var cards = view?.Cards.Select(c => view.RequirementEditor(c.Id)).OfType<RequirementEditorView>().ToArray() ?? [];
        if (cards.Length == 0) { linkedRequirement.AddChild(DeskUi.Label("No submitted requirements are available.")); return; }
        if (!cards.Any(c => c.CardId == SelectedCard)) SelectedCard = cards[0].CardId;
        var card = cards.Single(c => c.CardId == SelectedCard);
        var requester = DeskUi.Label(card.Person?.WithUnit ?? card.SubmittedBy); requester.SetMeta("request_asker", card.CardId); linkedRequirement.AddChild(requester);
        linkedRequirement.AddChild(DeskUi.Heading(card.Title));
        var standing = view!.WhereThingsStand().FirstOrDefault(e => e.CardId == card.CardId);
        var otherBuys = RenderOtherRequestBuys(view!, card.CardId, standing);
        if (standing is not null && !otherBuys.StandingShown)
        {
            var state = DeskUi.Label(standing.Sentence); state.SetMeta("request_standing", card.CardId); linkedRequirement.AddChild(state);
        }
        RenderRequestDecisions(view!, card.CardId, otherBuys.Stops);
        var capability = view.Rules.Model.Capabilities.Single(c => c.Id == card.CapabilityId);
        var rate = capability.Delivery == "consumable" || card.Unit is "gal" or "L" or "m3" ? card.Unit + "/day" : card.Unit;
        var peak = DeskUi.Label($"Peak demand: {DeskUi.Quantity(card.PeakDemandUnits)} {rate}\nRequested day {card.RequestedStartDay} for {card.DurationDays} days");
        peak.SetMeta("request_peak", card.CardId); linkedRequirement.AddChild(peak);
        if (standing?.WhyShort is { } whyShort)
        {
            var reason = DeskUi.Label(whyShort); reason.SetMeta("request_why_short", card.CardId); linkedRequirement.AddChild(reason);
        }
        if (standing?.OrderBy is { } order)
        {
            var deadline = DeskUi.Label(order.Sentence); deadline.SetMeta("request_order_by", card.CardId); linkedRequirement.AddChild(deadline);
        }
        foreach (var sentence in standing?.OnFile ?? [])
        {
            var known = DeskUi.Label(sentence); known.SetMeta("request_on_file", card.CardId); linkedRequirement.AddChild(known);
        }
        if (card.Message is { Length: > 0 } message) linkedRequirement.AddChild(DeskUi.Label(message));
        if (card.HeardAt is { } heardAt)
        {
            var heard = DeskUi.Label(heardAt); heard.SetMeta("heard_at", card.CardId); linkedRequirement.AddChild(heard);
            // The public card records whether the shop heard this request in the room; do not parse its prose.
            if (view.Cards.First(c => c.Id == card.CardId).RaisedInRoomDay is null)
            {
                var observedHash = session.StateHash;
                var meeting = DeskUi.Button("Find the staff meeting in People to reach", () =>
                {
                    if (observedHash == session.StateHash) OpenStaffMeeting();
                });
                meeting.SetMeta("staff_meeting_link", card.CardId);
                var blocked = DeskUi.ActionReason(linkedRequirement, meeting);
                DeskUi.Gate(meeting, blocked, view.Finished ? DeskUi.FinishedReason : null);
            }
        }
        if (session.IsReplay) linkedRequirement.AddChild(DeskUi.Label("Asking about this request or adding what you learned takes control and stops future replay actions."));

        foreach (var line in card.Lines.Where(l => l.Known))
        {
            var included = DeskUi.Label(line.On ? $"The request includes {line.Label}." : $"Not in the request yet: {line.Label}.");
            included.SetMeta("requirement_line", line.Id); included.SetMeta("included", line.On); linkedRequirement.AddChild(included);
            RequestPartLabels[line.Id] = included;
            var buyWarning = DeskUi.Label(); buyWarning.Visible = false; buyWarning.SetMeta("buy_warning_line", line.Id);
            buyWarning.AddThemeColorOverride("font_color", TabletopTheme.Warning); linkedRequirement.AddChild(buyWarning); RequestBuyWarnings[line.Id] = buyWarning;
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
            RequirementAddButtons.Add(line.Id, AddLearnedLineButton(linkedRequirement, view, card.CardId, line));
        }
        RenderRequestHelpers(view!, card);
        linkedRequirement.AddChild(DeskUi.Label("Dependency checklist"));
        if (card.NeededBy is { } neededBy) { var needs = DeskUi.Label(neededBy); needs.SetMeta("needed_by", card.CapabilityId); linkedRequirement.AddChild(needs); }
        foreach (var dependency in card.Dependencies)
        {
            var label = DeskUi.Label($"{dependency.Label}: {Words(dependency.Coverage).ToLowerInvariant()}" +
                (dependency.CoveredBy.Count > 0 ? $" · {string.Join(", ", dependency.CoveredBy.Select(view.ArrangementName))}" : ""));
            label.SetMeta("dependency", dependency.CapabilityId); linkedRequirement.AddChild(label);
            if (dependency.Coverage is not ("arranged" or "inRequirement") && dependency.UsualOwner is { } owner)
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

    private Button AddLearnedLineButton(VBoxContainer parent, SessionView view, string card, RequirementLine line)
    {
        var hash = session.StateHash;
        var add = DeskUi.Button("Add what you learned", () =>
        {
            if (hash == session.StateHash) AddLearnedRequirementLine(card, line.Id);
        });
        add.TooltipText = line.Label; add.SetMeta("add_line", line.Id);
        var reason = DeskUi.ActionReason(parent, add);
        DeskUi.Gate(add, reason, view.Finished ? DeskUi.FinishedReason : null);
        // AddLine applies immediately: it queues no work and spends no hours or money.
        var cost = DeskUi.Label($"No shop hours. Adds {line.Label.ToLowerInvariant()} to this request.");
        cost.SetMeta("action_cost", nameof(AddLineAction)); parent.AddChild(cost);
        return add;
    }

    private void AddLearnedRequirementLine(string card, string line)
    {
        if (session.View is not { Finished: false } view || PlayingDay || SelectedCard != card
            || view.RequirementEditor(card)?.Lines.Any(l => l.Id == line && l.Known && l.CanAdd) != true) return;
        SendAction(new AddLineAction { Day = view.Day, Card = card, Line = line });
    }
}
