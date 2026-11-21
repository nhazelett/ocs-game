using Godot;
using LinesOfSupport.Sim.Views;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal VBoxContainer MorningBrief { get; private set; } = null!;
    internal Label MorningHeading => morningHeading;
    internal VBoxContainer MorningAlerts { get; private set; } = null!;

    private void BuildMorningBrief(VBoxContainer board)
    {
        morningHeading = DeskUi.Label();
        board.AddChild(morningHeading);
        MorningAlerts = new VBoxContainer { Name = "MorningAlerts" };
        MorningAlerts.AddThemeConstantOverride("separation", 10); board.AddChild(MorningAlerts);
        MorningBrief = new VBoxContainer { Name = "NewSinceYesterday" };
        MorningBrief.AddThemeConstantOverride("separation", 14); board.AddChild(MorningBrief);
    }

    private void RefreshMorningBrief(MorningBoardView? board)
    {
        DeskUi.Clear(MorningBrief); DeskUi.Clear(MorningAlerts);
        var view = session.View;
        var alertRecords = new HashSet<string>();
        if (board is not null && view is not null)
        {
            // The current Sim tiles supply both state and explanation, including a continuing shortfall.
            foreach (var tile in board.Tiles.Where(t => t.Status is "down" or "degraded"))
            {
                var key = tile.SiteId + "." + tile.CapabilityId;
                var panel = new PanelContainer(); panel.SetMeta("morning_shortfall", key); MorningAlerts.AddChild(panel);
                var row = new VBoxContainer(); panel.AddChild(row);
                var open = DeskUi.Button(view.NameOf(tile.SiteId) + " · " + tile.Label + ": " + TileLevel(tile) + "\nOpen site details →", () => SelectCapability(key));
                row.AddChild(open);
                if (tile.WhyShort is { } why) row.AddChild(DeskUi.Label(why));
            }
            foreach (var item in board.Items.Where(i => i.Severity == BoardSeverity.Alert
                && !board.Tiles.Any(t => t.SiteId + "." + t.CapabilityId == i.LinkId && t.Status is "down" or "degraded")))
            {
                alertRecords.Add(item.RecordId);
                var row = new VBoxContainer(); row.SetMeta("alert_record", item.RecordId); MorningAlerts.AddChild(row);
                var open = DeskUi.Button(item.Text, () => NavigateBoardItem(item));
                var reason = DeskUi.ActionReason(row, open);
                DeskUi.Gate(open, reason, item.LinkKind == BoardLinkKind.None ? "This update has no further detail to open." : null);
            }
        }
        MorningAlerts.Visible = MorningAlerts.GetChildCount() > 0;
        MorningBrief.AddChild(DeskUi.Heading("New since yesterday"));
        if (board is null) return;
        var news = board.New.Where(item => item.RecordId is null || !alertRecords.Contains(item.RecordId)).ToArray();
        MorningBrief.Visible = news.Length > 0 || board.New.Count == 0;
        if (news.Length == 0 && board.New.Count == 0) MorningBrief.AddChild(DeskUi.Label("Nothing new since yesterday."));
        foreach (var item in news) AddNewsItem(MorningBrief, item, "morning");
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
        if (surface == "morning" && item.Person?.Role == "outgoingCco"
            && item.Text.Contains("staff meeting", StringComparison.OrdinalIgnoreCase)
            && session.View is { Finished: false } view && !view.StaffMeeting.Attending && !view.StaffMeeting.Joining)
        {
            var action = new JoinStaffMeetingAction { Day = view.Day, Attend = true };
            var assigned = AddAssignedAction(row, row, view, action, "Join the staff meeting",
                view.StaffMeeting.JoinSays);
            assigned.Button.SetMeta("news_staff_meeting", true);
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
