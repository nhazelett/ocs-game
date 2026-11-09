using Godot;

namespace LinesOfSupport.Game;

public partial class SchematicMap
{
    private readonly StyleBoxFlat yardStyle = new() { BgColor = new Color("b5ae8d"), CornerRadiusTopLeft = 10,
        CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 };
    private void DrawIsland(Vector2 point, bool hub, string name)
    {
        var shape = new Vector2[14];
        for (var i = 0; i < shape.Length; i++)
        {
            var angle = Mathf.Tau * i / shape.Length; var radius = i % 3 == 0 ? 225 : i % 3 == 1 ? 210 : 190;
            shape[i] = point + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .8f);
        }
        DrawColoredPolygon(shape.Select(p => point + (p - point) * 1.12f + new Vector2(5, 12)).ToArray(), new Color("236477"));
        DrawColoredPolygon(shape.Select(p => p + new Vector2(6, 12)).ToArray(), new Color("0d3041")); DrawColoredPolygon(shape, new Color("c6b98e"));
        DrawColoredPolygon(shape.Select(p => point + (p - point) * .86f).ToArray(), new Color(hub ? "82967c" : "96a080"));
        for (var i = 0; i < 8; i++)
        {
            var angle = i * 1.9f; var p = point + new Vector2(Mathf.Cos(angle) * 165, Mathf.Sin(angle) * 120);
            DrawCircle(p + new Vector2(4, 5), 13, new Color("617967")); DrawCircle(p, 12, new Color("71896c"));
        }
        DrawRect(new Rect2(point + new Vector2(-180, 112), new Vector2(75, 17)), new Color("9b8b69"));
        Text(point + new Vector2(0, -205), name, 520, 34, new Color("eef1d9"));
    }
    private void DrawSite(Vector2 point, BoardSite site)
    {
        var condition = Conditions.GetValueOrDefault(site.Id, SiteCondition.Unknown);
        DrawStyleBox(yardStyle, new Rect2(point - new Vector2(142, 93), new Vector2(284, 186)));
        if (SelectedSite == site.Id) DrawRect(new Rect2(point - new Vector2(148, 99), new Vector2(296, 198)), new Color("fff2c6"), false, 4);
        var tiles = Tiles.Where(t => t.SiteId == site.Id).ToArray();
        for (var i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i]; var p = point + new Vector2((i % 3 - 1) * 90, -45 + (i / 3) * 98);
            var live = tile.Status is "ok" or "degraded";
            DrawEquipment(p, tile.CapabilityId, new Color(tile.Status == "notOpen" ? "87958d" : tile.Status == "down" ? "576969" : "ddd9bc"), live);
            var color = new Color(tile.Status == "down" ? "a53531" : tile.Status == "degraded" ? "85572b" : "263b37");
            var label = tile.Label switch { "Food service" => "Food", "Waste removal" => "Waste", "Ground transport" => "Transport", _ => tile.Label };
            Text(p + new Vector2(0, 42), label, 100, 18, color);
            Text(p + new Vector2(0, 65), tile.Status == "notOpen" ? "—" : $"{tile.TodayPermille / 10m:0.#}%", 90, 19, color);
            if (KnownChanges.Any(c => c.SiteId == site.Id && c.CapabilityId == tile.CapabilityId)) DrawArc(p, 35, 0, Mathf.Tau, 32, new Color(tile.Status == "down" ? "ef8d72" : "e6e8af"), 3, true);
        }
        if (tiles.Length == 0) DrawEquipment(point, "lodging", new Color("899a8e"), false);
        Text(point + new Vector2(0, 150), site.Name, 400, 32, new Color("f4eed6"));
        var state = condition switch { SiteCondition.NotOpen => $"Opens day {site.OpensDay}", SiteCondition.Full => "Supported", SiteCondition.Degraded => "Partly supported", SiteCondition.Down => "Support interrupted", _ => $"Opens day {site.OpensDay}" };
        Text(point + new Vector2(0, 183), state + (LostConfidenceSites.Contains(site.Id) ? " · confidence lost" : ""), 450, 23, new Color("e0e3cf"));
    }
    private void DrawEquipment(Vector2 p, string capability, Color color, bool live)
    {
        switch (capability)
        {
            case "power":
                Box(p, new Vector2(58, 37), color); DrawRect(new Rect2(p + new Vector2(-22, -12), new Vector2(25, 20)), new Color("405558"));
                for (var i = 0; i < 4; i++) DrawLine(p + new Vector2(-19, -9 + i * 5), p + new Vector2(0, -9 + i * 5), new Color("82908a"), 2);
                DrawLine(p + new Vector2(18, -18), p + new Vector2(18, -34), new Color("43575a"), 5); DrawCircle(p + new Vector2(18, 4), 4, new Color(live ? "f3d673" : "283d43")); break;
            case "fuel": case "water":
                DrawCircle(p + new Vector2(3, 4), 27, new Color(0, 0, 0, .16f)); DrawCircle(p, 26, color);
                DrawArc(p, 21, 0, Mathf.Tau, 32, new Color(capability == "fuel" ? "8d8059" : "6f9b9e"), 5, true); DrawLine(p + new Vector2(21, 8), p + new Vector2(37, 21), new Color("43575a"), 5); break;
            case "lodging":
                DrawColoredPolygon([p + new Vector2(-31, 21), p + new Vector2(0, -27), p + new Vector2(31, 21)], color);
                DrawLine(p + new Vector2(0, -27), p + new Vector2(0, 21), new Color("8b8d73"), 3);
                DrawColoredPolygon([p + new Vector2(-8, 21), p + new Vector2(0, 0), p + new Vector2(9, 21)], new Color("3b5354")); break;
            case "food": case "foodService":
                Box(p + new Vector2(0, 6), new Vector2(58, 32), color); DrawColoredPolygon([p + new Vector2(-36, -10), p + new Vector2(-24, -27), p + new Vector2(24, -27), p + new Vector2(36, -10)], new Color("647f6c")); break;
            default: Box(p, new Vector2(45, 45), color); DrawLine(p + new Vector2(-20, -8), p + new Vector2(20, -8), new Color("809184"), 4); break;
        }
    }
    private void DrawVehicle(Vector2 point, string mode, Color color)
    {
        if (mode is "aircraft" or "airlift")
            DrawColoredPolygon([point + new Vector2(35, 0), point + new Vector2(2, -8), point + new Vector2(-12, -30), point + new Vector2(-22, -30), point + new Vector2(-15, -5), point + new Vector2(-34, -10), point + new Vector2(-30, 0), point + new Vector2(-34, 10), point + new Vector2(-15, 5), point + new Vector2(-22, 30), point + new Vector2(-12, 30), point + new Vector2(2, 8)], color);
        else if (mode == "truck")
        {
            Box(point, new Vector2(54, 25), color); Box(point + new Vector2(27, 0), new Vector2(20, 23), new Color("79948b"));
            DrawCircle(point + new Vector2(-16, 15), 6, new Color("263d45")); DrawCircle(point + new Vector2(25, 15), 6, new Color("263d45"));
        }
        else
        {
            DrawColoredPolygon([point + new Vector2(-40, -19), point + new Vector2(32, -19), point + new Vector2(45, 0), point + new Vector2(32, 19), point + new Vector2(-40, 19)], new Color("354e59"));
            Box(point, new Vector2(52, 25), color); DrawLine(point + new Vector2(-48, 0), point + new Vector2(-70, 0), new Color("b2d1d9"), 3);
        }
    }
}
