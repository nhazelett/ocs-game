using Godot;

namespace LinesOfSupport.Game;

public partial class TabletopMap3D
{
    private string? shortfallLayoutKey;

    internal string? OpenShortfallSite { get; private set; }
    internal PanelContainer? ShortfallCard { get; private set; }
    internal Line2D? ShortfallTether { get; private set; }

    internal string? SiteAt(Vector2 point)
    {
        if (ShortfallCard?.GetRect().HasPoint(point) == true) return OpenShortfallSite;
        foreach (var entry in SiteStatuses)
            if (callouts.Single(c => c.Words == entry.Value).Card is { Visible: true } card && card.GetRect().HasPoint(point))
                return entry.Key;
        // Use the actual island silhouette, not a zoom-dependent circle around its name.
        foreach (var island in board.Islands)
        {
            var index = solidIslands.IndexOf(island.Id);
            if (index >= 0 && Geometry2D.IsPointInPolygon(point, ProjectSolid(index)))
                return board.Sites.Where(s => s.Island == island.Id).MinBy(s => SitePoint(s.Id).DistanceSquaredTo(point))?.Id;
        }
        return null;
    }

    internal bool ToggleShortfall(string site)
    {
        if (OpenShortfallSite == site) { CloseShortfall(); return true; }
        CloseShortfall();
        var shortTiles = capabilityTiles.Where(t => t.SiteId == site && t.WhyShort is not null).ToArray();
        if (shortTiles.Length == 0) return false;
        OpenShortfallSite = site;
        ShortfallTether = new Line2D { ZIndex = 99, Width = 1.5f, DefaultColor = TabletopTheme.Alert, Antialiased = true };
        AddChild(ShortfallTether);
        var width = Math.Min(360, Size.X - 12);
        ShortfallCard = new PanelContainer { ZIndex = 100, MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(width, 0), Size = new Vector2(width, 0) };
        using var paper = TabletopTheme.Box(TabletopTheme.Paper, 10, 7);
        paper.BorderColor = TabletopTheme.Alert; paper.BorderWidthLeft = 3;
        ShortfallCard.AddThemeStyleboxOverride("panel", paper); AddChild(ShortfallCard);
        var body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore }; body.AddThemeConstantOverride("separation", 7);
        ShortfallCard.AddChild(body);
        Label Words(string text, bool heading = false, string? capability = null)
        {
            var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(width - 20, 0), MouseFilter = MouseFilterEnum.Ignore };
            label.AddThemeFontSizeOverride("font_size", heading ? 20 : 17);
            label.AddThemeColorOverride("font_color", TabletopTheme.Ink);
            if (capability is not null) label.SetMeta("why_short", capability);
            body.AddChild(label); return label;
        }
        Words(board.Sites.Single(s => s.Id == site).Name, true);
        foreach (var tile in shortTiles)
        {
            var capability = Words($"{tile.Label} · {tile.TodayPermille / 10m:0.#}% supplied");
            capability.AddThemeColorOverride("font_color", TabletopTheme.Alert);
            capability.SetMeta("short_capability", tile.CapabilityId);
            Words(tile.WhyShort!, capability: tile.CapabilityId);
        }
        LayoutShortfall(); return true;
    }

    internal void CloseShortfall()
    {
        if (ShortfallCard is not null) { RemoveChild(ShortfallCard); ShortfallCard.QueueFree(); ShortfallCard = null; }
        if (ShortfallTether is not null) { RemoveChild(ShortfallTether); ShortfallTether.QueueFree(); ShortfallTether = null; }
        OpenShortfallSite = null;
        shortfallLayoutKey = null;
    }

    private void LayoutShortfall()
    {
        if (ShortfallCard is not { } card || OpenShortfallSite is not { } site) return;
        var resting = callouts.Single(c => c.Words == SiteStatuses[site]);
        var key = $"{Size}|{BottomOcclusion}|{world.Position}|{camera.Size}|{resting.Heading!.GetGlobalRect()}";
        if (key == shortfallLayoutKey) return;
        var available = new Rect2(new Vector2(6, 6), Size - new Vector2(12, BottomOcclusion + 12));
        var island = board.Sites.Single(s => s.Id == site).Island;
        var outline = SolidInventory.Where(s => s.Island == island).SelectMany(s => s.Polygon).ToArray();
        var min = new Vector2(outline.Min(p => p.X), outline.Min(p => p.Y));
        var max = new Vector2(outline.Max(p => p.X), outline.Max(p => p.Y));
        // Keep the selected shore, ring and name together in view. The resting
        // callouts keep their own placement; only this expanded card moves.
        var ground = new Rect2(min, max - min);
        if (!available.Encloses(ground)) { MakeRoomForShortfall(); return; }
        var name = new Rect2(resting.Heading!.GlobalPosition - GlobalPosition, resting.Heading.Size);
        var protectedArea = ground.Merge(name).Grow(10);
        var regions = new[]
        {
            new Rect2(available.Position, new Vector2(protectedArea.Position.X - available.Position.X, available.Size.Y)),
            new Rect2(new Vector2(protectedArea.End.X, available.Position.Y), new Vector2(available.End.X - protectedArea.End.X, available.Size.Y)),
            new Rect2(available.Position, new Vector2(available.Size.X, protectedArea.Position.Y - available.Position.Y)),
            new Rect2(new Vector2(available.Position.X, protectedArea.End.Y), new Vector2(available.Size.X, available.End.Y - protectedArea.End.Y))
        };
        var body = card.GetChild<VBoxContainer>(0);
        var labels = body.GetChildren().OfType<Label>().ToArray();
        using var paper = card.GetThemeStylebox("panel");
        var padding = paper.GetMinimumSize();
        var anchor = SitePoint(site);
        var candidates = new List<Rect2>();
        foreach (var raw in regions.Where(r => r.Size.X >= 160 && r.Size.Y > 0))
        {
            var region = raw.Intersection(available);
            var width = Math.Min(360, Mathf.Floor(region.Size.X));
            if (width < 160) continue;
            var height = padding.Y + (labels.Length - 1) * body.GetThemeConstant("separation");
            foreach (var label in labels)
            {
                using var font = label.GetThemeFont("font");
                height += Mathf.Ceil(font.GetMultilineStringSize(label.Text, width: width - padding.X,
                    fontSize: label.GetThemeFontSize("font_size"),
                    brkFlags: TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound
                        | TextServer.LineBreakFlag.Adaptive).Y);
            }
            if (height > region.Size.Y) continue;
            var position = new Vector2(Math.Clamp(anchor.X - width / 2, region.Position.X, region.End.X - width),
                Math.Clamp(anchor.Y - height / 2, region.Position.Y, region.End.Y - height));
            candidates.Add(new Rect2(position, new Vector2(width, height)));
        }
        if (candidates.Count == 0)
        {
            // A close zoom can leave no side wide enough for readable type.
            // Make room in the presentation camera rather than covering the site.
            MakeRoomForShortfall(); return;
        }
        shortfallLayoutKey = key;
        // Prefer less wrapping, then the shortest tether to the actual site.
        var chosen = candidates.OrderByDescending(r => r.Size.X)
            .ThenBy(r => anchor.DistanceSquaredTo(PointerEnd(anchor, r))).First();
        foreach (var label in labels)
        {
            label.CustomMinimumSize = new Vector2(chosen.Size.X - padding.X, 0);
            label.Size = new Vector2(chosen.Size.X - padding.X, 0);
        }
        card.CustomMinimumSize = new Vector2(chosen.Size.X, 0);
        card.Size = chosen.Size;
        card.Position = chosen.Position;
        ShortfallTether!.Points = [anchor, PointerEnd(anchor, card.GetRect())];
    }

    private void MakeRoomForShortfall()
    {
        world.Position = Vector3.Zero;
        camera.Size = Math.Min(50, camera.Size * 1.12f);
    }
}
