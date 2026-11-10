using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

internal enum SiteCondition { Unknown, NotOpen, Full, Degraded, Down }

// Layout, camera and animation are presentation-only. MapDay supplies movement facts.
public partial class SchematicMap : Node2D
{
    private Font MapFont => (Owner as Control)?.GetThemeDefaultFont() ?? ThemeDB.FallbackFont;
    internal ScenarioBoardData? Data { get; private set; }
    internal string? SelectedSite { get; private set; }
    internal IReadOnlyDictionary<string, SiteCondition> Conditions { get; private set; } = new Dictionary<string, SiteCondition>();
    internal IReadOnlySet<string> LostConfidenceSites { get; private set; } = new HashSet<string>();
    internal MapDayView? Day { get; private set; }
    internal IReadOnlyList<MapChange> KnownChanges { get; private set; } = [];
    internal IReadOnlyList<CapabilityTile> Tiles { get; private set; } = [];
    internal event Action<string>? SitePicked;
    internal float CameraZoom => zoom;
    internal Vector2 CameraOffset => offset;
    private readonly Dictionary<string, Vector2> islands = new(StringComparer.Ordinal), sites = new(StringComparer.Ordinal);
    private Vector2 worldSize = new(1500, 1000), offset, dragStart;
    private float zoom = .5f, animationSeconds;
    private bool dragging, cameraTouched;
    internal void SelectSite(string? id) { SelectedSite = id; Tabletop?.Select(id);QueueRedraw(); }
    internal void ShowConfidence(IEnumerable<string> values) { LostConfidenceSites = values.ToHashSet(StringComparer.Ordinal);RefreshTabletop(); QueueRedraw(); }
    internal void ShowConditions(IReadOnlyDictionary<string, SiteCondition> values) { Conditions = values; QueueRedraw(); }
    internal void ShowCapabilities(IReadOnlyList<CapabilityTile> values) { Tiles = values; RefreshTabletop();QueueRedraw(); }
    internal void ShowMapDay(MapDayView? value)
    {
        if (Day?.Day != value?.Day) animationSeconds = 0;
        Day = value; KnownChanges = value?.Changes.Where(c => c.Known).ToArray() ?? []; RefreshTabletop();Tabletop?.ShowFacts(Day,Math.Clamp(animationSeconds/5f,0,1));QueueRedraw();
    }
    internal void ReplayMovement() { animationSeconds = 0; QueueRedraw(); }
    internal Vector2 SiteScreenPoint(string id) => Tabletop?.SitePoint(id) ?? sites[id] * zoom + offset;
    internal void ShowScenario(ScenarioBoardData? data)
    {
        Data = data; Day = null; Tiles = []; KnownChanges = []; SelectedSite = null;
        if(Tabletop is not null){RemoveChild(Tabletop);Tabletop.QueueFree();Tabletop=null;}tabletopSignature=null;
        LostConfidenceSites = new HashSet<string>(); Conditions = new Dictionary<string, SiteCondition>(); islands.Clear(); sites.Clear();
        if (data is not null)
        {
            var hub = data.Islands.FirstOrDefault(i => i.IsHub) ?? data.Islands[0];
            var spokes = data.Islands.Where(i => i.Id != hub.Id).ToArray();
            worldSize = spokes.Length == 0 ? new Vector2(900, 850) : new Vector2(1500, Math.Max(1000, spokes.Length * 350 + 250));
            islands[hub.Id] = new Vector2(spokes.Length == 0 ? 450 : 350, worldSize.Y / 2);
            for (var i = 0; i < spokes.Length; i++) islands[spokes[i].Id] = new Vector2(1110, spokes.Length == 1 ? worldSize.Y / 2 : 220 + i * (worldSize.Y - 440) / (spokes.Length - 1));
            foreach (var group in data.Sites.GroupBy(s => s.Island))
            {
                var members = group.ToArray();
                for (var i = 0; i < members.Length; i++) sites[members[i].Id] = islands[group.Key] + new Vector2((i % 2) * 200 - (members.Length > 1 ? 100 : 0), (i / 2) * 180);
            }
        }
        cameraTouched = false; FitMap();
    }
    internal void FitMap()
    {
        var size = GetViewportRect().Size; if (size.X < 1 || size.Y < 1) return;
        zoom = Math.Min(size.X / worldSize.X, size.Y / worldSize.Y) * .94f;
        offset = size / 2 - worldSize * zoom / 2; cameraTouched = false; QueueRedraw();
        Tabletop?.Fit();
    }
    internal void PanBy(Vector2 amount) { offset += amount;Tabletop?.PanBy(amount);cameraTouched = true; QueueRedraw(); }
    internal void ZoomAt(Vector2 anchor, float factor)
    {
        var world = (anchor - offset) / zoom;
        Tabletop?.ZoomAt(anchor,factor);
        zoom = Math.Clamp(zoom * factor, .2f, 2f); offset = anchor - world * zoom; cameraTouched = true; QueueRedraw();
    }
    public override void _Ready() => GetViewport().SizeChanged += () => { if(Tabletop is not null)Tabletop.Size=GetViewportRect().Size; if (!cameraTouched) FitMap(); else QueueRedraw(); };
    public override void _Process(double delta) { if (Day is not null && animationSeconds < 6) { animationSeconds += (float)delta;Tabletop?.ShowFacts(Day,Math.Clamp(animationSeconds/5f,0,1)); QueueRedraw(); } }
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton mouse)
        {
            if (!GetViewportRect().HasPoint(mouse.Position)) return;
            if (mouse.Pressed && mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            { ZoomAt(mouse.Position, mouse.ButtonIndex == MouseButton.WheelUp ? 1.15f : 1 / 1.15f); GetViewport().SetInputAsHandled(); }
            if (mouse.ButtonIndex is MouseButton.Left or MouseButton.Middle)
            {
                if (mouse.Pressed) { dragging = true; dragStart = mouse.Position; }
                else
                {
                    var wasDragging = dragging; dragging = false;
                    if (wasDragging && sites.Count > 0 && mouse.ButtonIndex == MouseButton.Left && dragStart.DistanceTo(mouse.Position) < 7)
                    {
                        var target = sites.MinBy(s => SiteScreenPoint(s.Key).DistanceSquaredTo(mouse.Position));
                        if (target.Key is not null && SiteScreenPoint(target.Key).DistanceTo(mouse.Position) < 100 * zoom)
                        { SelectSite(target.Key); SitePicked?.Invoke(target.Key); }
                    }
                }
            }
        }
        if (input is InputEventMouseMotion motion && dragging && motion.ButtonMask != 0) PanBy(motion.Relative);
    }
    public override void _Draw()
    {
        if(Tabletop is not null)return;
        DrawRect(new Rect2(Vector2.Zero, GetViewportRect().Size), new Color("8eafb2")); if (Data is null) return;
        DrawSetTransform(offset, 0, Vector2.One * zoom);
        for (var y = 70; y < worldSize.Y; y += 95) for (var x = 35; x < worldSize.X; x += 120)
            DrawArc(new Vector2(x, y), 15, .12f, 1.2f, 8, new Color("80a3a4"), 2, true);
        foreach (var group in Data.Links.GroupBy(l => string.CompareOrdinal(l.From, l.To) <= 0 ? (l.From, l.To) : (l.To, l.From)))
        {
            var links = group.ToArray();
            for (var i = 0; i < links.Length; i++)
            {
                var link = links[i]; var from = islands[link.From]; var to = islands[link.To];
                var shift = (islands[group.Key.Item2] - islands[group.Key.Item1]).Normalized().Orthogonal() * ((i - (links.Length - 1) / 2f) * 62);
                var state = Day?.Links.FirstOrDefault(l => l.FromIslandId == link.From && l.ToIslandId == link.To && l.Mode == link.Mode);
                var closed = state?.Open == false; var color = new Color(closed ? "9f4937" : "486c70");
                DrawDashedLine(from + shift, to + shift, color, 3, 15, true);
                var label = link.Mode switch { "aircraft" or "airlift" => "Air route", "barge" or "sealift" => "Barge route", _ => "Transport route" };
                Text((from + to) / 2 + shift + new Vector2(0, -12), label + (closed ? $" · closed through day {state!.ClosedThroughDay}" : ""), 440, 25, color);
            }
        }
        foreach (var island in Data.Islands) DrawIsland(islands[island.Id], island);
        foreach (var site in Data.Sites) DrawSite(sites[site.Id], site);
        DrawMovement(); DrawSetTransform(Vector2.Zero);
    }
    private void DrawMovement()
    {
        if (Day is null) return;
        var progress = Math.Clamp(animationSeconds / 5f, 0, 1);
        foreach (var run in Day.Runs)
        {
            if (!islands.TryGetValue(run.FromIslandId, out var from) || !sites.TryGetValue(run.SiteId, out var site)) continue;
            var port = islands[run.ToIslandId] + new Vector2(-150, 120);
            var point = progress < .72f ? from.Lerp(port, progress / .72f) : port.Lerp(site, (progress - .72f) / .28f);
            DrawVehicle(point, progress >= .72f ? "truck" : run.Mode, new Color("e5c578"));
            var label = Tiles.FirstOrDefault(t => t.CapabilityId == run.CapabilityId)?.Label ?? "Supplies";
            Text(point + new Vector2(0, 60), $"{label} · {run.Quantity:N0} {run.Unit}", 440, 24, new Color("343d4b"));
        }
        foreach (var held in Day.Held.GroupBy(h => h.FromIslandId))
        {
            if (!islands.TryGetValue(held.Key, out var point)) continue;
            point += new Vector2(180, 130); Box(point, new Vector2(38, 28), new Color("dbb874"));
            Text(point + new Vector2(0, 56), "Cargo waiting", 270, 23, new Color("343d4b"));
        }
        foreach (var shipment in Day.Shipments)
        {
            if (!sites.TryGetValue(shipment.SiteId, out var point)) continue;
            point += new Vector2(-240, -40);
            // Transit is not an arrival. The icon never travels into the destination site.
            var drift = shipment.CustomsHeld ? 0 : Math.Min(animationSeconds, 5) * 9;
            DrawVehicle(point + new Vector2(drift, 0), shipment.Mode, new Color("b7d4de"));
            Text(point + new Vector2(0, 58), shipment.CustomsHeld ? "Held in customs" : $"On its way · due day {shipment.ArrivalDay}", 400, 23, new Color("343d4b"));
        }
    }
    private void Text(Vector2 baseline, string text, float width, int size, Color color)
        => DrawString(MapFont, baseline - new Vector2(width / 2, 0), text, HorizontalAlignment.Center, width, size, color);
    private void Box(Vector2 p, Vector2 size, Color color)
    {
        DrawRect(new Rect2(p - size / 2 + new Vector2(5, 7), size), new Color(0, 0, 0, .22f));
        DrawRect(new Rect2(p - size / 2, size), color); DrawRect(new Rect2(p - size / 2, size), new Color("30464a"), false, 2);
    }
}
