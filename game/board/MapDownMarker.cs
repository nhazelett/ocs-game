using Godot;

namespace LinesOfSupport.Game;

// Screen-space shape: its 22px disc and cross survive Fit map and every zoom level.
public partial class MapDownMarker : Control
{
    public override void _Draw()
    {
        DrawCircle(new Vector2(12, 12), 11, TabletopTheme.Paper);
        DrawArc(new Vector2(12, 12), 10, 0, Mathf.Tau, 32, TabletopTheme.Alert, 2, true);
        DrawLine(new Vector2(8, 8), new Vector2(16, 16), TabletopTheme.Alert, 3, true);
        DrawLine(new Vector2(8, 16), new Vector2(16, 8), TabletopTheme.Alert, 3, true);
    }
}
