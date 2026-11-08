using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer morningItems = null!, morningTiles = null!, linkedRequirement = null!, linkedLedger = null!;
    private Label morningHeading = null!, boardFocus = null!;
    internal string? SelectedCard { get; private set; }
    internal string? SelectedCapability { get; private set; }
    internal string? SelectedArrangement { get; private set; }
    internal VBoxContainer MorningItems => morningItems;
    internal VBoxContainer MorningTiles => morningTiles;

    private void BuildMorningDesk()
    {
        var tabs = GetNode<TabContainer>("%DeskTabs");
        var board = DeskUi.Panel(tabs.GetNode<Control>("Board"), "MorningBoard");
        morningHeading = DeskUi.Label(); board.AddChild(morningHeading);
        boardFocus = DeskUi.Label(); board.AddChild(boardFocus);
        board.AddChild(DeskUi.Label("Overnight changes · choose a linked item to inspect it"));
        morningItems = new VBoxContainer { Name = "Items" }; board.AddChild(morningItems);
        board.AddChild(DeskUi.Label("Site capabilities"));
        morningTiles = new VBoxContainer { Name = "Tiles" }; board.AddChild(morningTiles);
        linkedRequirement = DeskUi.Panel(tabs.GetNode<Control>("Requirements"), "RequirementContext");
        linkedLedger = DeskUi.Panel(tabs.GetNode<Control>("Ledger"), "LedgerContext");
    }

    private void ResetDeskSelection()
    {
        SelectedCard = SelectedCapability = SelectedArrangement = null;
        boardFocus.Text = "";
        Map.SelectSite(null);
        ResetAwardForm();
        ResetBpaForm();
        coordinationQuantity = 1;
    }

    private void RefreshMorningDesk(SessionView? view, MorningBoardView? board)
    {
        DeskUi.Clear(morningItems); DeskUi.Clear(morningTiles);
        morningHeading.Text = board is null ? "No scenario loaded" :
            $"Morning of day {Math.Min(board.Day, board.Strip.LastDay)} · {board.Items.Count(x => x.Severity == BoardSeverity.Alert)} alerts · {board.Items.Count(x => x.Category == BoardCategory.Request)} requests";
        if (board?.Items.Count is null or 0) morningItems.AddChild(DeskUi.Label("No overnight changes."));
        foreach (var item in board?.Items ?? [])
        {
            var button = DeskUi.Button($"{item.Severity} · {item.Category}\n{item.Text}", () => NavigateBoardItem(item));
            button.Disabled = item.LinkKind == BoardLinkKind.None;
            button.SetMeta("link_kind", item.LinkKind.ToString()); button.SetMeta("link_id", item.LinkId);
            morningItems.AddChild(button);
        }
        foreach (var tile in board?.Tiles ?? [])
        {
            var key = tile.SiteId + "." + tile.CapabilityId;
            var name = view!.Scenario.Sites.Single(s => s.Id == tile.SiteId).Name;
            var button = DeskUi.Button($"{name} · {tile.Label}: {tile.TodayPermille / 10m:0.#}% · {tile.Status}", () => SelectCapability(key));
            button.ToggleMode = true; button.ButtonPressed = SelectedCapability == key;
            button.SetMeta("capability", key); morningTiles.AddChild(button);
        }
        RenderRequirementContext(view);
        RenderLedgerContext(view);
        RefreshBoardActions(view);
    }

    internal void NavigateBoardItem(BoardItem item)
    {
        var tabs = GetNode<TabContainer>("%DeskTabs");
        switch (item.LinkKind)
        {
            case BoardLinkKind.Card:
                SelectedCard = item.LinkId; tabs.CurrentTab = 1;
                RenderRequirementContext(session.View); break;
            case BoardLinkKind.SiteCapability: SelectCapability(item.LinkId); break;
            case BoardLinkKind.Arrangement:
                SelectedArrangement = item.LinkId; tabs.CurrentTab = 5;
                RenderLedgerContext(session.View); break;
            case BoardLinkKind.Cash:
            case BoardLinkKind.Work:
                tabs.CurrentTab = 5; RenderLedgerContext(session.View);
                linkedLedger.AddChild(DeskUi.Label(item.Text)); break;
            case BoardLinkKind.Island:
            case BoardLinkKind.Event:
                tabs.CurrentTab = 0; boardFocus.Text = item.Text; break;
        }
    }

    private void SelectCapability(string key)
    {
        var tile = session.View?.MorningBoard().Tiles.SingleOrDefault(t => t.SiteId + "." + t.CapabilityId == key);
        if (tile is null) return;
        SelectedCapability = key;
        Map.SelectSite(tile.SiteId);
        boardFocus.Text = $"Selected: {tile.Label} at {session.View!.Scenario.Sites.Single(s => s.Id == tile.SiteId).Name} · {tile.Status} · {tile.TodayPermille / 10m:0.#}%";
        GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
        RefreshMorningDesk(session.View, session.View.MorningBoard());
    }

    private void RenderLedgerContext(SessionView? view)
    {
        DeskUi.Clear(linkedLedger);
        definitizations.Clear(); terminations.Clear();
        if (view is null) { linkedLedger.AddChild(DeskUi.Label("No scenario loaded.")); return; }
        linkedLedger.AddChild(DeskUi.Label($"Cash: {DeskUi.Money(view.TopStrip().CashCents)}"));
        linkedLedger.AddChild(DeskUi.Label("Funding lines"));
        foreach (var funding in view.Funding)
        {
            var row = DeskUi.Label($"{funding.Id} · remaining {DeskUi.Money(funding.RemainingCents)}\nAvailable day {funding.AvailableDay} · expires day {funding.ExpiresDay} · {(funding.UsableOn(view.Day) ? "usable today" : "not usable today")}");
            row.SetMeta("funding", funding.Id); linkedLedger.AddChild(row);
        }
        if (view.Funding.Count == 0) linkedLedger.AddChild(DeskUi.Label("No funding lines."));
        linkedLedger.AddChild(DeskUi.Label("Arrangements"));
        foreach (var arrangement in view.Arrangements)
        {
            var button = DeskUi.Button($"{arrangement.Id} · {arrangement.Status}\nObligated {DeskUi.Money(arrangement.ObligatedCents)} · incurred {DeskUi.Money(arrangement.IncurredCents)}\nUnobligated incurred {DeskUi.Money(arrangement.UnobligatedIncurredCents)}", () =>
            {
                SelectedArrangement = arrangement.Id; RenderLedgerContext(session.View);
            });
            button.ToggleMode = true; button.ButtonPressed = SelectedArrangement == arrangement.Id;
            button.SetMeta("arrangement", arrangement.Id); linkedLedger.AddChild(button);
            AddArrangementActions(linkedLedger, arrangement, view);
        }
        if (view.Arrangements.Count == 0) linkedLedger.AddChild(DeskUi.Label("No arrangements yet."));
    }

    private void OnContinue()
    {
        var count = 0;
        var reason = "seven-day limit";
        while (count < 7 && session.RequestNextDay())
        {
            count++;
            var board = session.View!.MorningBoard();
            if (board.Items.Any(i => i.Severity == BoardSeverity.Alert || i.Category == BoardCategory.Request))
            { reason = "an alert or request arrived"; break; }
        }
        if (session.View?.Finished == true) reason = "scenario complete";
        RefreshDisplay($"Continued {count} days: {reason}. Results through day {session.CompletedDay}.");
    }
}
