using Godot;
using System.Globalization;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    // Presentation only: match the picker and strip's ceiling to the next half hour.
    internal static string PreviewHours(ActionPreview preview, bool shop = false)
    {
        static decimal Hours(long milli, int whole) => milli < 0 ? whole : Math.Ceiling(milli / 500m) / 2;
        var lo = Hours(preview.HoursMinMilli, preview.ShopHoursMin);
        var hi = Hours(preview.HoursMaxMilli, preview.ShopHoursMax);
        static string Number(decimal value) => value.ToString("0.#", CultureInfo.InvariantCulture);
        var range = lo == hi ? Number(lo) : $"{Number(lo)} to {Number(hi)}";
        return $"{range} {(shop ? "shop " : "")}{(hi == 1 ? "hour" : "hours")}";
    }

    // Bind the engine's hours and result day; never forecast the shop's queue in Game.
    private static string ActionCostText(SessionView view, PlayerAction action, string effect, ActionPreview? preview = null)
    {
        preview ??= view.PreviewAny(action);
        static string Range(int lo, int hi) => lo == hi ? lo.ToString() : $"{lo} to {hi}";
        var member = view.Team.FirstOrDefault(m => action.Assignee is null ? m.Role == "you" : m.Id == action.Assignee);
        var whose = member is null || member.Role == "you" ? "your" : (string.IsNullOrEmpty(member.Rank) ? member.Name : member.Rank + " " + member.Name) + "'s";
        var noHours = preview.HoursMaxMilli >= 0 ? preview.HoursMaxMilli == 0 : preview.ShopHoursMax == 0;
        var hours = noHours ? "No shop hours" : $"{PreviewHours(preview)} of {whose} time";
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
