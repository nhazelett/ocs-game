using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    // Assignee changes only the proposed action. Sim owns the hours, queue and completion forecast.
    private (Button Button, AssigneePicker Picker, Label Cost) AddAssignedAction(
        VBoxContainer body, VBoxContainer footer, SessionView view, PlayerAction action, string text, string effect,
        Func<bool>? stillCurrent = null, Action<PlayerAction, ActionPreview>? updated = null, Action? accepted = null, Action<PlayerAction>? commit = null, Func<string?>? blocked = null, Func<PlayerAction, ActionPreview, string>? costText = null)
    {
        var hash = session.StateHash;
        var selected = action.Assignee ?? view.Team.Single(m => m.Role == "you").Id;
        var picker = new AssigneePicker(view.AssigneeChoices(action), selected, view.Finished, "Who does it");
        body.AddChild(picker);
        bool Current() => picker.IsInsideTree() && hash == session.StateHash
            && !AtFrontDoor && !ReadingTurnover && !PlayingDay && !ReadingDaySummary
            && session.View is { Finished: false } && (stillCurrent?.Invoke() ?? true);
        var warnings = DeskUi.Label(""); body.AddChild(warnings);
        var button = DeskUi.Button(text, () =>
        {
            if (!Current() || blocked?.Invoke() is not null || !session.View!.PreviewAny(action).Accepted) return;
            if (commit is not null) commit(action);
            else if (SendAction(action).Accepted) accepted?.Invoke();
        });
        var reason = DeskUi.ActionReason(footer, button);
        var cost = AddActionCost(footer, view, action, effect);
        void Refresh()
        {
            var preview = view.PreviewAny(action);
            DeskUi.Gate(button, reason, view.Finished ? DeskUi.FinishedReason : blocked?.Invoke() is { } stop ? stop : preview.HardStops.Count > 0 ? string.Join("\n\n", preview.HardStops) : null);
            warnings.Text = string.Join("\n\n", preview.SoftGaps); warnings.Visible = preview.SoftGaps.Count > 0;
            cost.Text = costText?.Invoke(action, preview) ?? ActionCostText(view, action, effect, preview);
            updated?.Invoke(action, preview);
        }
        picker.SelectionChanged = () =>
        {
            if (!Current()) return;
            action = action with { Assignee = picker.Selected };
            Refresh();
        };
        Refresh();
        return (button, picker, cost);
    }
}
