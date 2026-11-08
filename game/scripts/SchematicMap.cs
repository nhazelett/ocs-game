using Godot;

namespace LinesOfSupport.Game;

public partial class SchematicMap : Node2D
{
    public override void _Ready() => GetViewport().SizeChanged += QueueRedraw;

    public override void _Draw()
    {
        var size = GetViewportRect().Size;
        var hub = size * new Vector2(0.23f, 0.5f);
        var spokeA = size * new Vector2(0.71f, 0.25f);
        var spokeB = size * new Vector2(0.75f, 0.75f);
        var line = new Color(0.45f, 0.45f, 0.45f);
        DrawLine(hub, spokeA, line, 2, true);
        DrawLine(hub, spokeB, line, 2, true);
        DrawSite(hub, "Hub");
        DrawSite(spokeA, "Spoke A");
        DrawSite(spokeB, "Spoke B");
    }

    private void DrawSite(Vector2 point, string label)
    {
        DrawCircle(point, 32, new Color(0.28f, 0.28f, 0.28f));
        DrawCircle(point, 7, new Color(0.8f, 0.8f, 0.8f));
        DrawString(ThemeDB.FallbackFont, point + new Vector2(-48, 58), label,
            HorizontalAlignment.Center, 96, 18, new Color(0.85f, 0.85f, 0.85f));
    }
}
