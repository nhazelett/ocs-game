using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    // Bind the engine's hours and result day; never forecast the shop's queue in Game.
    private static string ActionCostText(SessionView view, PlayerAction action, string effect, ActionPreview? preview = null)
    {
        preview ??= view.PreviewAny(action);
        static string Range(int lo, int hi) => lo == hi ? lo.ToString() : $"{lo} to {hi}";
        var member = view.Team.FirstOrDefault(m => action.Assignee is null ? m.Role == "you" : m.Id == action.Assignee);
        var whose = member is null || member.Role == "you" ? "your" : (string.IsNullOrEmpty(member.Rank) ? member.Name : member.Rank + " " + member.Name) + "'s";
        var hours = preview.ShopHoursMax == 0 ? "No shop hours" : $"{Range(preview.ShopHoursMin, preview.ShopHoursMax)} {(preview.ShopHoursMax == 1 ? "hour" : "hours")} of {whose} time";
        var money = preview.TotalCents > 0 ? $"; {DeskUi.Money(preview.TotalCents)}" : "";
        return $"{hours}{money}. {effect}" + (preview.Accepted
            ? $" Earliest result: day {Range(preview.EarliestStartMin, preview.EarliestStartMax)}{DeskUi.QueueClause(preview.QueueAheadHours)}." : "");
    }

    private static Label AddActionCost(VBoxContainer parent, SessionView view, PlayerAction action, string effect, ActionPreview? preview = null)
    {
        var label = DeskUi.Label(ActionCostText(view, action, effect, preview));
        label.SetMeta("action_cost", action.GetType().Name); parent.AddChild(label); return label;
    }
}
