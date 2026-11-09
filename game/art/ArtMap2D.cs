using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game.Art;

public partial class ArtMap2D : Control
{
    internal ScenarioBoardData Board { get; init; } = null!;
    internal IReadOnlyList<CapabilityTile> Tiles { get; init; } = [];
    internal IReadOnlyList<MapLink> Routes { get; init; } = [];
    internal int Study { get; init; }
    private readonly Dictionary<string, Vector2> points = [];
    private Vector2 pan;
    private float zoom = 1;
    private bool dragging;
    public override void _Ready() { ClipContents = true; MouseFilter = MouseFilterEnum.Stop; }
    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseButton b)
        {
            if (b.ButtonIndex == MouseButton.Left) dragging = b.Pressed;
            if (b.Pressed && b.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
                zoom = Math.Clamp(zoom * (b.ButtonIndex == MouseButton.WheelUp ? 1.12f : 1 / 1.12f), .7f, 1.8f);
        }
        if (input is InputEventMouseMotion m && dragging) pan += m.Relative;
        QueueRedraw();
    }
    public override void _Draw()
    {
        var s = ArtStudyStyle.All[Study]; var ink = new Color(s.Ink); var muted = new Color(s.Muted);
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(s.Water));
        DrawSetTransform(pan + Size / 2 * (1 - zoom), 0, Vector2.One * zoom);
        for (var y = 24; y < 510; y += Study == 0 ? 24 : 36)
            for (var x = 14; x < 790; x += Study == 0 ? 39 : 48)
                if (Study == 0) DrawArc(new Vector2(x, y), 7, .15f, 1.4f, 8, new Color("aebfba"), 1, true);
                else { DrawLine(new Vector2(x, y), new Vector2(x + 3, y), new Color("294550")); }
        var ordered = Board.Islands.OrderByDescending(i => i.IsHub).ToArray();
        var positions = new[] { new Vector2(190, 277), new Vector2(575, 142), new Vector2(575, 371) };
        for (var i = 0; i < ordered.Length; i++) points[ordered[i].Id] = positions[i % positions.Length];
        foreach (var pair in Board.Links.GroupBy(l => (l.From, l.To)))
        {
            var from = points[pair.Key.From]; var to = points[pair.Key.To]; var normal = (to - from).Normalized().Orthogonal();
            var lines = pair.ToArray();
            for (var i = 0; i < lines.Length; i++)
            {
                var shift = normal * ((i - (lines.Length - 1) / 2f) * 28); var a = from + shift; var b = to + shift;
                var link = Routes.FirstOrDefault(r => r.FromIslandId == lines[i].From && r.ToIslandId == lines[i].To && r.Mode == lines[i].Mode);
                var color = new Color(link?.Open == false ? s.Alert : Study == 0 ? "7c8f8a" : "699a9c");
                DrawDashedLine(a, b, color, Study == 0 ? 1.2f : 2, Study == 0 ? 5 : 10, true);
                Text((a + b) / 2 + new Vector2(0, -8), (lines[i].Mode == "aircraft" ? "Air" : "Barge") + (link?.Open == false ? " · closed" : " lane"), 240, 16, color);
            }
        }
        foreach (var island in ordered)
        {
            var p = points[island.Id];
            var shape = new Vector2[22];
            for (var i = 0; i < shape.Length; i++)
            {
                var a = i * Mathf.Tau / shape.Length;
                var r = 1 + .09f * Mathf.Sin(i * 2.6f) + .06f * Mathf.Cos(i * 1.8f);
                shape[i] = p + new Vector2(Mathf.Cos(a) * 135 * r, Mathf.Sin(a) * 88 * r);
            }
            for (var ring = 3; ring >= 1; ring--)
            {
                var contour = shape.Select(v => p + (v - p) * (1 + ring * .055f)).Append(p + (shape[0] - p) * (1 + ring * .055f)).ToArray();
                DrawPolyline(contour, new Color(Study == 0 ? "a7bab2" : "285966"), 1, true);
            }
            DrawColoredPolygon(shape.Select(v => v + new Vector2(4, 7)).ToArray(), new Color(Study == 0 ? "a6b5a9" : "071e2b"));
            DrawColoredPolygon(shape, new Color(s.Land)); DrawPolyline(shape.Append(shape[0]).ToArray(), new Color(Study == 0 ? "5f7770" : "70c1b4"), 2, true);
            if (Study == 0)
            {
                for (var i = 0; i < 12; i++) { var x = -90 + i * 16; DrawLine(p + new Vector2(x, 61), p + new Vector2(x + 11, 70), new Color("a5a181"), 1, true); }
                for (var i = 0; i < 6; i++)
                {
                    var t = p + new Vector2(-108 + i * 40, 44 + i % 2 * 9);
                    DrawColoredPolygon([t + new Vector2(-7,6),t + new Vector2(0,-9),t + new Vector2(7,6)], new Color("9ba481"));
                    DrawLine(t + new Vector2(0,-9),t+new Vector2(7,6),new Color("728a75"),1,true);
                }
            }
            else
                for (var ring = 1; ring < 3; ring++) DrawArc(p, 25 + ring * 19, 2.9f, 5.7f, 30, new Color("32636a"), 1, true);
            Text(p + new Vector2(0, -65), island.Name, 290, 20, ink);
            var site = Board.Sites.First(si => si.Island == island.Id);
            var tiles = Tiles.Where(t => t.SiteId == site.Id).ToArray();
            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i]; var q = p + new Vector2((i % 3 - 1) * 76, -27 + i / 3 * 58);
                Equipment(q, tile.CapabilityId, tile.Status == "down", s);
                var shortName = tile.Label switch { "Food service" => "Food", "Waste removal" => "Waste", _ => tile.Label };
                Text(q + new Vector2(0, 28), shortName, 100, 16, ink);
                if (tile.Status == "down")
                {
                    DrawLine(q + new Vector2(14, -15), q + new Vector2(23, -6), new Color(s.Alert), 3, true);
                    DrawLine(q + new Vector2(23, -15), q + new Vector2(14, -6), new Color(s.Alert), 3, true);
                }
            }
        }
        // Labels go last so a neighbouring island's contour cannot obscure a state callout.
        foreach(var island in ordered)
        {
            var p=points[island.Id]; var site=Board.Sites.First(si=>si.Island==island.Id);
            var tiles=Tiles.Where(t=>t.SiteId==site.Id).ToArray();
            var primary = tiles.FirstOrDefault(t => t.CapabilityId == "power") ?? tiles[0];
            var down = tiles.Any(t => t.Status == "down");
            Text(p + new Vector2(0, 113), site.Name, 280, 21, ink);
            DrawRect(new Rect2(p + new Vector2(-178, 118), new Vector2(356, 26)), new Color(s.Water));
            Text(p + new Vector2(0, 137), down ? $"{primary.Label} {primary.TodayPermille / 10m:0.#}% · support interrupted" : "Supported", 340, 16, new Color(down ? s.Alert : s.Muted));
        }
        Text(new Vector2(59, 43), "N", 60, 19, ink); DrawLine(new Vector2(59, 53), new Vector2(59, 78), ink, 1);
        DrawColoredPolygon([new(59, 53), new(54, 64), new(64, 64)], ink);
        DrawSetTransform(Vector2.Zero);
    }
    private void Equipment(Vector2 p, string capability, bool down, ArtStudyStyle s)
    {
        var ink = new Color(down ? s.Alert : s.Ink); var fill = new Color(down ? Study == 0 ? "b5b39e" : "183740" : Study == 0 ? "f4ecd8" : "72bcae");
        if (capability is "fuel" or "water") { DrawCircle(p, 13, fill); DrawArc(p, 13, 0, Mathf.Tau, 32, ink, 2, true); DrawLine(p + new Vector2(10, 8), p + new Vector2(19, 14), ink, 2); }
        else if (capability == "lodging") { DrawColoredPolygon([p + new Vector2(-17, 11), p + new Vector2(0, -17), p + new Vector2(17, 11)], fill); DrawPolyline([p + new Vector2(-17, 11), p + new Vector2(0, -17), p + new Vector2(17, 11)], ink, 2, true); DrawLine(p + new Vector2(0, -14), p + new Vector2(0, 11), ink, 2); }
        else
        {
            DrawRect(new Rect2(p - new Vector2(17, 12), new Vector2(34, 24)), fill); DrawRect(new Rect2(p - new Vector2(17, 12), new Vector2(34, 24)), ink, false, 2);
            for (var i = 0; i < 3; i++) DrawLine(p + new Vector2(-11, -6 + i * 5), p + new Vector2(3, -6 + i * 5), ink, 1);
            if (capability == "power") { DrawLine(p + new Vector2(12, -12), p + new Vector2(12, -22), ink, 3); DrawCircle(p + new Vector2(10, 5), 3, down ? ink : new Color(s.Accent)); }
        }
    }
    private void Text(Vector2 p, string text, float width, int size, Color color)
        => DrawString(GetThemeDefaultFont(), p - new Vector2(width / 2, 0), text, HorizontalAlignment.Center, width, size, color);
}
