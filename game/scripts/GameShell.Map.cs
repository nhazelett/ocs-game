using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal Label MapHint { get; private set; } = null!;

    private void BuildMapControls()
    {
        var board = GetNode<VBoxContainer>("Margin/Layout/WorkArea/BoardPanel/BoardLayout");
        var row = new HBoxContainer { Name = "MapControls" }; row.AddThemeConstantOverride("separation", 12);
        var fit = DeskUi.Button("Fit map", () => Map.FitMap()); fit.AutowrapMode = TextServer.AutowrapMode.Off; fit.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; row.AddChild(fit);
        var movement = DeskUi.Button("Show today's movement", () => Map.ReplayMovement()); movement.AutowrapMode = TextServer.AutowrapMode.Off; movement.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; row.AddChild(movement);
        var hint = MapHint = DeskUi.Label("Drag to move. Scroll to zoom."); hint.SizeFlagsHorizontal = SizeFlags.ExpandFill; row.AddChild(hint); board.AddChild(row);
        Map.SitePicked += id => { var tile = session.View?.MorningBoard().Tiles.FirstOrDefault(t => t.SiteId == id); if (tile is not null) SelectCapability(id + "." + tile.CapabilityId); };
    }
}
