using Godot;

namespace LinesOfSupport.Game;

internal enum SiteCondition { Unknown, NotOpen, Full, Degraded, Down }

public partial class SchematicMap : Node2D
{
    internal ScenarioBoardData? Data { get; private set; }
    internal string? SelectedSite { get; private set; }
    internal void SelectSite(string? id) { SelectedSite = id; QueueRedraw(); }
    internal IReadOnlyDictionary<string, SiteCondition> Conditions { get; private set; } = new Dictionary<string, SiteCondition>();

    internal void ShowConditions(IReadOnlyDictionary<string, SiteCondition> conditions)
    {
        Conditions = conditions;
        QueueRedraw();
    }

    internal void ShowScenario(ScenarioBoardData? data)
    {
        Data = data;
        Conditions = new Dictionary<string, SiteCondition>();
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
                var condition = Conditions.GetValueOrDefault(sites[i].Id, SiteCondition.Unknown);
                var color = condition switch
                {
                    SiteCondition.Full => new Color(.3f, .8f, .55f), SiteCondition.Degraded => new Color(1, .75f, .25f),
                    SiteCondition.Down => new Color(.95f, .3f, .3f), _ => new Color(.7f, .7f, .7f)
                };
                DrawCircle(marker, 8, color);
                if (SelectedSite == sites[i].Id) DrawArc(marker, 13, 0, Mathf.Tau, 24, Colors.White, 2, true);
                var label = condition == SiteCondition.NotOpen ? "not open" : condition.ToString().ToLowerInvariant();
                Label(point + new Vector2(0, 65 + i * 32), $"{sites[i].Name} · {label} · opens day {sites[i].OpensDay}", 300, 15);
            }
        }
    }

    private void Label(Vector2 baseline, string text, float width, int size = 17)
    {
        DrawString(ThemeDB.FallbackFont, baseline - new Vector2(width / 2, 0), text,
            HorizontalAlignment.Center, width, size, new Color(0.85f, 0.85f, 0.85f));
    }
}
