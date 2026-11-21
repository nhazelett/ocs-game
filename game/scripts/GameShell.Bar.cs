using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal VBoxContainer TodayBar { get; private set; } = null!;
    internal VBoxContainer? DebriefBar { get; private set; }
    internal Button DebriefPlayAgain { get; private set; } = null!;
    internal Button DebriefChooseScenario { get; private set; } = null!;

    private static void RenderBar(VBoxContainer parent, BarView? bar, bool compact = false, IReadOnlyList<CarriedView>? carried = null)
    {
        DeskUi.Clear(parent); parent.Visible = bar is { Lines.Count: > 0 };
        if (bar is null || bar.Lines.Count == 0) return;
        if (bar.Final)
        {
            parent.AddChild(DeskUi.Heading("The commander's verdict"));
            parent.AddChild(DeskUi.Label($"{bar.From?.DisplayName ?? "The commander"} · {bar.Days} days"));
            if (bar.Closing is { } closing)
            {
                var conclusion = DeskUi.Label(closing); conclusion.SetMeta("bar_closing", true);
                conclusion.AddThemeFontSizeOverride("font_size", 23); parent.AddChild(conclusion);
            }
        }
        else parent.AddChild(DeskUi.Heading($"The commander's bar · {bar.Days} days"));
        foreach (var line in bar.Lines)
        {
            var state = line.State switch { BarState.AtRisk => "At risk", BarState.Missed => "Missed", _ => bar.Final ? "Met" : "Met so far" };
            var label = DeskUi.Label((compact ? "" : line.Says + " ") + line.Text + " " + state + ".");
            label.SetMeta("bar_line", line.Id); label.SetMeta("bar_state", line.State.ToString());
            label.AddThemeColorOverride("font_color", line.State switch {
                BarState.AtRisk => TabletopTheme.Warning, BarState.Missed => TabletopTheme.Alert, _ => TabletopTheme.Ink });
            if (bar.Final && carried?.FirstOrDefault(c => c.LineId == line.Id) is { } cause)
            {
                var row = new VBoxContainer(); row.AddThemeConstantOverride("separation", 4);
                row.SetMeta("verdict_line", line.Id); parent.AddChild(row); row.AddChild(label);
                var explanation = DeskUi.Label(cause.Sentence); explanation.SetMeta("bar_carried", line.Id);
                row.AddChild(explanation);
            }
            else parent.AddChild(label);
        }
    }
}
