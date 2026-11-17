using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal VBoxContainer MorningBrief { get; private set; } = null!;
    internal Label MorningHeading => morningHeading;

    private void BuildMorningBrief(VBoxContainer board)
    {
        morningHeading = DeskUi.Label();
        board.AddChild(morningHeading);
        MorningBrief = new VBoxContainer { Name = "NewSinceYesterday" };
        MorningBrief.AddThemeConstantOverride("separation", 14); board.AddChild(MorningBrief);
    }

    private void RefreshMorningBrief(MorningBoardView? board)
    {
        DeskUi.Clear(MorningBrief);
        MorningBrief.AddChild(DeskUi.Heading("New since yesterday"));
        if (board is null) return;
        if (board.New.Count == 0) MorningBrief.AddChild(DeskUi.Label("Nothing new since yesterday."));
        foreach (var item in board.New) AddNewsItem(MorningBrief, item, "morning");
    }

    // The Sim supplies both the order and the full words. The same answer is readable on the results page and Today.
    private void AddNewsItem(VBoxContainer parent, NewItem item, string surface)
    {
        var panel = new PanelContainer(); parent.AddChild(panel);
        panel.SetMeta("new_kind", item.Kind.ToString());
        panel.SetMeta("news_record", item.RecordId ?? "");
        var row = new VBoxContainer(); row.AddThemeConstantOverride("separation", 6); panel.AddChild(row);
        var label = DeskUi.Label(item.Text); label.SetMeta("news_surface", surface); row.AddChild(label);
        if (surface == "summary") label.SetMeta("summary_kind", item.Kind == NewKind.Answer ? "said" : "news");
        if (item.AskedBy is { } asker)
        {
            var asked = DeskUi.Label("Asked by " + DeskUi.ListLabel(asker) + ".");
            asked.SetMeta("asked_by", asker); row.AddChild(asked);
        }
        if (surface != "morning" || item.Kind != NewKind.Request || item.LinkKind != BoardLinkKind.Card) return;
        var hash = session.StateHash;
        var open = DeskUi.Button("Open request", () =>
        {
            if (hash != session.StateHash || PlayingDay || ReadingDaySummary || AtFrontDoor || ReadingTurnover) return;
            NavigateBoardItem(new BoardItem(item.RecordId ?? "", session.View!.Day, BoardCategory.Report,
                BoardSeverity.Info, item.LinkKind, item.LinkId, item.Text, [], item.Person));
        });
        open.SetMeta("news_link", item.LinkId); row.AddChild(open);
    }

    private static string TileLevel(CapabilityTile tile)
        => tile.OpensDay is { } day ? $"opens day {day}" : $"{tile.TodayPermille / 10m:0.#}% · {StatusWords(tile.Status)}";
}
