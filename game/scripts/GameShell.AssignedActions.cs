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
        Func<bool>? stillCurrent = null, Action<PlayerAction, ActionPreview>? updated = null, Action? accepted = null, Action<PlayerAction>? commit = null, Func<string?>? blocked = null, Func<PlayerAction, ActionPreview, string>? costText = null, string? inWork = null, bool showInWorkPicker = false)
    {
        var hash = session.StateHash;
        var selected = action.Assignee ?? view.Team.Single(m => m.Role == "you").Id;
        var picker = new AssigneePicker(view.AssigneeChoices(action), selected, view.Finished || inWork is not null, "Who does it");
        picker.Visible = inWork is null || showInWorkPicker; body.AddChild(picker);
        if (inWork is not null && showInWorkPicker && picker.ChoiceButtons.TryGetValue(selected, out var heldChoice))
            heldChoice.Text = "✓ " + heldChoice.Text;
        bool Current() => inWork is null && picker.IsInsideTree() && hash == session.StateHash
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
        if (inWork is not null) reason.AddThemeColorOverride("font_color", TabletopTheme.Ink);
        var cost = AddActionCost(footer, view, action, effect);
        void Refresh()
        {
            var preview = view.PreviewAny(action);
            DeskUi.Gate(button, reason, view.Finished ? DeskUi.FinishedReason : blocked?.Invoke() is { } stop ? stop : preview.HardStops.Count > 0 ? string.Join("\n\n", preview.HardStops) : null);
            warnings.Text = string.Join("\n\n", preview.SoftGaps); warnings.Visible = preview.SoftGaps.Count > 0;
            var holder = view.Team.FirstOrDefault(m => m.Work.Any(w => w.WorkId == inWork));
            var work = holder?.Work.FirstOrDefault(w => w.WorkId == inWork);
            cost.Text = holder is not null && work is not null
                ? $"{(holder.Role == "you" ? "You have" : TeamName(holder) + " has")} this task, about {DeskUi.Count(work.HoursLeft, "hour")} of work left. {effect}"
                : costText?.Invoke(action, preview) ?? ActionCostText(view, action, effect, preview);
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
