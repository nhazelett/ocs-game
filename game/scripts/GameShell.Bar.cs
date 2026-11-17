using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal VBoxContainer TodayBar { get; private set; } = null!;
    internal VBoxContainer? DebriefBar { get; private set; }
    internal Button DebriefPlayAgain { get; private set; } = null!;
    internal Button DebriefChooseScenario { get; private set; } = null!;

    private static void RenderBar(VBoxContainer parent, BarView? bar)
    {
        DeskUi.Clear(parent); parent.Visible = bar is { Lines.Count: > 0 };
        if (bar is null || bar.Lines.Count == 0) return;
        if (bar.Final)
        {
            parent.AddChild(DeskUi.Heading("The commander's verdict"));
            parent.AddChild(DeskUi.Label($"{bar.From?.DisplayName ?? "The commander"} · {bar.Days} days"));
        }
        else parent.AddChild(DeskUi.Label($"The commander's bar · {bar.Days} days"));
        foreach (var line in bar.Lines)
        {
            var state = line.State switch { BarState.AtRisk => "At risk", BarState.Missed => "Missed", _ => bar.Final ? "Met" : "Met so far" };
            var label = DeskUi.Label(line.Says + " " + line.Text + " " + state + ".");
            label.SetMeta("bar_line", line.Id); label.SetMeta("bar_state", line.State.ToString());
            label.AddThemeColorOverride("font_color", line.State switch {
                BarState.AtRisk => TabletopTheme.Warning, BarState.Missed => TabletopTheme.Alert, _ => TabletopTheme.Ink });
            parent.AddChild(label);
        }
    }
}
