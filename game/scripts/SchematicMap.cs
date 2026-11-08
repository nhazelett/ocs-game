using Godot;

namespace LinesOfSupport.Game;

public partial class SchematicMap : Node2D
{
    internal ScenarioBoardData? Data { get; private set; }

    internal void ShowScenario(ScenarioBoardData? data)
    {
        Data = data;
        QueueRedraw();
    }

    public override void _Ready() => GetViewport().SizeChanged += QueueRedraw;

    public override void _Draw()
    {
        if (Data is null)
            return;
        var size = GetViewportRect().Size;
        var positions = new System.Collections.Generic.Dictionary<string, Vector2>(StringComparer.Ordinal);
        for (var i = 0; i < Data.Islands.Count; i++)
        {
            var angle = Mathf.Pi + Mathf.Tau * i / Data.Islands.Count;
            positions[Data.Islands[i].Id] = Data.Islands.Count == 1 ? size / 2 :
                size / 2 + new Vector2(Mathf.Cos(angle) * size.X * 0.28f, Mathf.Sin(angle) * size.Y * 0.27f);
        }
        var line = new Color(0.45f, 0.45f, 0.45f);
        foreach (var group in Data.Links.GroupBy(x => string.CompareOrdinal(x.From, x.To) <= 0 ? (x.From, x.To) : (x.To, x.From)))
        {
            var links = group.ToArray();
            for (var i = 0; i < links.Length; i++)
            {
                var link = links[i];
                var from = positions[link.From];
                var to = positions[link.To];
                var direction = (positions[group.Key.Item2] - positions[group.Key.Item1]).Normalized();
                var offset = direction.Orthogonal() * ((i - (links.Length - 1) / 2f) * 44);
                if (from.DistanceTo(to) < 1)
                {
                    DrawArc(from + Vector2.Up * 55, 35 + i * 20, 0, Mathf.Tau, 32, line, 2, true);
                    Label(from + Vector2.Up * (95 + i * 20), link.Mode, 180);
                }
                else
                {
                    DrawLine(from + offset, to + offset, line, 2, true);
                    Label((from + to) / 2 + offset + Vector2.Up * 6, link.Mode, 180);
                }
            }
        }
        foreach (var island in Data.Islands)
        {
            var point = positions[island.Id];
            DrawCircle(point, 42, new Color(island.IsHub ? 0.35f : 0.28f, 0.28f, 0.28f));
            Label(point + Vector2.Up * 54, island.Name, 230);
            var sites = Data.Sites.Where(x => x.Island == island.Id).ToArray();
            for (var i = 0; i < sites.Length; i++)
            {
                var marker = point + new Vector2(0, i * 32);
                DrawCircle(marker, 5, new Color(0.85f, 0.85f, 0.85f));
                Label(point + new Vector2(0, 65 + i * 32), $"{sites[i].Name} · opens day {sites[i].OpensDay}", 250, 15);
            }
        }
    }

    private void Label(Vector2 baseline, string text, float width, int size = 17)
    {
        DrawString(ThemeDB.FallbackFont, baseline - new Vector2(width / 2, 0), text,
            HorizontalAlignment.Center, width, size, new Color(0.85f, 0.85f, 0.85f));
    }
}
