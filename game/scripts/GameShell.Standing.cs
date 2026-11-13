using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer standingPanel = null!;
    internal VBoxContainer StandingRows { get; private set; } = null!;
    internal Dictionary<string, Button> StandingOpenButtons { get; } = new(StringComparer.Ordinal);

    private void BuildStandings(VBoxContainer board)
    {
        standingPanel = new VBoxContainer { Name = "WhereThingsStand" }; board.AddChild(standingPanel);
        standingPanel.AddThemeConstantOverride("separation", 10);
        var heading = DeskUi.Heading("Where things stand");
        standingPanel.AddChild(heading);
        StandingRows = new VBoxContainer { Name = "Requests" }; standingPanel.AddChild(StandingRows);
        StandingRows.AddThemeConstantOverride("separation", 10);
    }

    private void RefreshStandings(SessionView? view)
    {
        DeskUi.Clear(StandingRows); StandingOpenButtons.Clear();
        var entries = view?.WhereThingsStand() ?? [];
        standingPanel.Visible = entries.Count > 0;
        var observedHash = session.StateHash;
        foreach (var entry in entries)
        {
            var row = new HBoxContainer(); row.SetMeta("standing_card", entry.CardId); StandingRows.AddChild(row);
            var portrait = new PersonPortrait { SizeFlagsVertical = SizeFlags.ShrinkBegin };
            portrait.SetPerson(entry.From); row.AddChild(portrait);
            portrait.CustomMinimumSize = new Vector2(40, 50);
            var open = DeskUi.Button(entry.Title + (entry.From is null ? "" : " · " + entry.From.DisplayName)
                + "\n" + entry.Sentence, () =>
            {
                if (observedHash != session.StateHash || AtFrontDoor || ReadingTurnover || PlayingDay) return;
                NavigateBoardItem(new BoardItem("", session.View!.Day, BoardCategory.Request, BoardSeverity.Info,
                    BoardLinkKind.Card, entry.CardId, "", [], entry.From));
            });
            open.TooltipText = "Open request"; open.SetMeta("standing_state", entry.State);
            row.AddChild(open); StandingOpenButtons.Add(entry.CardId, open);
        }
    }
}
