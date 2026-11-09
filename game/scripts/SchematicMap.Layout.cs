using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

internal sealed record MapTileLayout(CapabilityTile Tile, Rect2 Bounds, Vector2 Symbol, IReadOnlyList<string> LabelLines, float LabelY, float PercentY);
internal sealed record MapSiteLayout(Rect2 Bounds, IReadOnlyList<MapTileLayout> Tiles)
{
    internal float NameY => Bounds.End.Y + 45;
    internal float StateY => NameY + 33;
    internal Vector2 IslandRadius => new(Math.Max(225, Bounds.Size.X / 2 + 60), Math.Max(180, Bounds.Size.Y / 2 + 60));
}

public partial class SchematicMap
{
    internal MapSiteLayout SiteLayout(string site)
    {
        var tiles = Tiles.Where(t => t.SiteId == site).ToArray();
        var labels = tiles.Select(t => MapLabel(t.Label)).ToArray();
        var font = ThemeDB.FallbackFont;
        float Width(string text) => font.GetStringSize(text, fontSize: 18).X;
        var cellWidth = Math.Max(124, labels.SelectMany(l => l.Split(' ')).Select(Width).DefaultIfEmpty(0).Max() + 16);
        string[] Wrap(string label)
        {
            var lines = new List<string>(); var line = "";
            foreach (var word in label.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var next = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && Width(next) > cellWidth - 12) { lines.Add(line); line = word; }
                else line = next;
            }
            if (line.Length > 0) lines.Add(line);
            return lines.ToArray();
        }
        var wrapped = labels.Select(Wrap).ToArray();
        var cellHeight = 94 + wrapped.Select(l => l.Length).DefaultIfEmpty(1).Max() * 22;
        var columns = Math.Min(3, Math.Max(1, tiles.Length));
        var rows = Math.Max(1, (tiles.Length + columns - 1) / columns);
        var size = new Vector2(columns * cellWidth + 24, rows * cellHeight + 24);
        var bounds = new Rect2(-size / 2, size);
        var result = tiles.Select((tile, i) =>
        {
            var cell = new Rect2(bounds.Position + new Vector2(12 + i % columns * cellWidth, 12 + i / columns * cellHeight), new Vector2(cellWidth, cellHeight));
            return new MapTileLayout(tile, cell, new Vector2(cell.GetCenter().X, cell.Position.Y + 34), wrapped[i],
                cell.Position.Y + 86, cell.Position.Y + 86 + wrapped[i].Length * 22);
        }).ToArray();
        return new MapSiteLayout(bounds, result);
    }

    internal static string MapLabel(string label) => label switch
    {
        "Food service" => "Food", "Waste removal" => "Waste", "Ground transport" => "Transport", _ => label
    };
}
