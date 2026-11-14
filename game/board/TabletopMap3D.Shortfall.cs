using Godot;

namespace LinesOfSupport.Game;

public partial class TabletopMap3D
{
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
    }

    private void LayoutShortfall()
    {
        if (ShortfallCard is not { } card || OpenShortfallSite is not { } site) return;
        var resting = callouts.Single(c => c.Words == SiteStatuses[site]);
        card.Size = new Vector2(card.CustomMinimumSize.X, card.GetCombinedMinimumSize().Y);
        var available = Size - new Vector2(6, BottomOcclusion + 6);
        card.Position = new Vector2(Math.Clamp(resting.Card.Position.X, 6, Math.Max(6, available.X - card.Size.X)),
            Math.Clamp(resting.Card.Position.Y, 6, Math.Max(6, available.Y - card.Size.Y)));
        ShortfallTether!.Points = [resting.PlacedAnchor, PointerEnd(resting.PlacedAnchor, card.GetRect())];
    }
}
