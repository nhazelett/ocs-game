using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private void BuildMapControls()
    {
        var board = GetNode<VBoxContainer>("Margin/Layout/WorkArea/BoardPanel/BoardLayout");
        var row = new HBoxContainer { Name = "MapControls" }; row.AddThemeConstantOverride("separation", 12);
        row.AddChild(DeskUi.Button("Fit map", () => Map.FitMap())); row.AddChild(DeskUi.Button("Show today's movement", () => Map.ReplayMovement()));
        var hint = DeskUi.Label("Drag to move. Scroll to zoom."); hint.SizeFlagsHorizontal = SizeFlags.ExpandFill; row.AddChild(hint); board.AddChild(row);
        Map.SitePicked += id => { var tile = session.View?.MorningBoard().Tiles.FirstOrDefault(t => t.SiteId == id); if (tile is not null) SelectCapability(id + "." + tile.CapabilityId); };
    }
}
