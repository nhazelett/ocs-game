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
    internal string? SelectedFunding { get; private set; }
    internal VBoxContainer MorningItems => morningItems;
    internal VBoxContainer MorningTiles => morningTiles;
    internal Label BoardFocus => boardFocus;

    private void BuildMorningDesk()
    {
        var tabs = GetNode<TabContainer>("%DeskTabs");
        var board = DeskUi.Panel(tabs.GetNode<Control>("Board"), "MorningBoard");
        BuildLead(board);
        morningHeading = DeskUi.Label(); boardDetails.AddChild(morningHeading);
        boardFocus = DeskUi.Label(); boardDetails.AddChild(boardFocus);
        morningItems = new VBoxContainer { Name = "Items" }; boardDetails.AddChild(morningItems);
        morningTiles = new VBoxContainer { Name = "Tiles" }; boardDetails.AddChild(morningTiles);
        linkedRequirement = DeskUi.Panel(tabs.GetNode<Control>("Requirements"), "RequirementContext");
        linkedLedger = DeskUi.Panel(tabs.GetNode<Control>("Ledger"), "LedgerContext");
    }

    private void ResetDeskSelection()
    {
        ResetOpening();
        TeamPanel?.Hide();
        CloseContractView();
        debriefHash = null;
        ResetVisits();
        BoardDetailsToggle.ButtonPressed = false;
        SelectedCard = SelectedCapability = SelectedArrangement = SelectedFunding = null;
        boardFocus.Text = "";
        Map.SelectSite(null);
        ResetAwardForm();
        ResetBpaForm();
        coordinationQuantity = 1;
    }

    private void RefreshMorningDesk(SessionView? view, MorningBoardView? board)
    {
        DeskUi.Clear(morningItems); DeskUi.Clear(morningTiles);
        RefreshLead(view, board);
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
            var button = DeskUi.Button($"{name} · {tile.Label}: {tile.TodayPermille / 10m:0.#}% · {StatusWords(tile.Status)}{(view.LostConfidenceSites.Contains(tile.SiteId) ? " · commander confidence lost" : "")}", () => SelectCapability(key));
            button.ToggleMode = true; button.ButtonPressed = SelectedCapability == key;
            button.SetMeta("capability", key); morningTiles.AddChild(button);
        }
        var caption = GetNode<Label>("%BoardCaption");
        caption.Text = ""; caption.Visible = false;
        if (view is not null && SelectedCapability is { } selected)
        {
            var tile = board?.Tiles.SingleOrDefault(t => t.SiteId + "." + t.CapabilityId == selected);
            if (tile is not null)
            {
                var name = view.Scenario.Sites.Single(s => s.Id == tile.SiteId).Name;
                var day = Math.Min(view.Day, view.LastDay);
                var population = view.Population(tile.SiteId, day);
                var planned = view.PlannedPopulation(tile.SiteId, day);
                caption.Text = $"Selected: {tile.Label} at {name}"; caption.Visible = true;
                boardFocus.Text = $"Selected: {tile.Label} at {name} · {StatusWords(tile.Status)} · {tile.TodayPermille / 10m:0.#}%\nPopulation day {day}: {population}" + (population == planned ? "" : $" · planned {planned}");
            }
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
                SelectedArrangement = item.LinkId; SelectedFunding = null; tabs.CurrentTab = 5;
                RenderLedgerContext(session.View); break;
            case BoardLinkKind.Funding:
                SelectedArrangement = null;
                SelectedFunding = session.View?.Funding.Any(f => f.Id == item.LinkId) == true ? item.LinkId : null;
                tabs.CurrentTab = 5; RenderLedgerContext(session.View);
                var focus = linkedLedger.GetChildren().OfType<Label>()
                    .FirstOrDefault(l => l.HasMeta("funding") && l.GetMeta("funding").AsString() == SelectedFunding);
                if (focus is null) linkedLedger.AddChild(DeskUi.Label(item.Text));
                else Callable.From(() =>
                {
                    if (!GodotObject.IsInstanceValid(focus) || !focus.IsInsideTree()) return;
                    focus.GrabFocus(); ((ScrollContainer)linkedLedger.GetParent()).EnsureControlVisible(focus);
                }).CallDeferred();
                break;
            case BoardLinkKind.Cash:
            case BoardLinkKind.Work:
                tabs.CurrentTab = 5; RenderLedgerContext(session.View);
                linkedLedger.AddChild(DeskUi.Label(item.Text)); break;
            case BoardLinkKind.Person: FocusPerson(item); break;
            case BoardLinkKind.Island:
            case BoardLinkKind.Event:
                tabs.CurrentTab = 0; boardFocus.Text = item.Text; break;
        }
    }

    private void SelectCapability(string key)
    {
        BoardDetailsToggle.ButtonPressed = true;
        if (session.View is not { } view) return;
        var tile = view.MorningBoard().Tiles.SingleOrDefault(t => t.SiteId + "." + t.CapabilityId == key);
        if (tile is null) return;
        SelectedCapability = key;
        Map.SelectSite(tile.SiteId);
        GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
        RefreshMorningDesk(view, view.MorningBoard());
    }

    private void RenderLedgerContext(SessionView? view)
    {
        DeskUi.Clear(linkedLedger);
        definitizations.Clear(); terminations.Clear(); CorDesignations.Clear();
        if (view is null) { linkedLedger.AddChild(DeskUi.Label("No scenario loaded.")); return; }
        linkedLedger.AddChild(DeskUi.Label($"Cash: {DeskUi.Money(view.TopStrip().CashCents)}"));
        linkedLedger.AddChild(DeskUi.Label("Funding lines"));
        foreach (var funding in view.Funding)
        {
            var heading = SelectedFunding == funding.Id
                ? "Selected funding line: " + view.FundingName(funding.Id) : DeskUi.ListLabel(view.FundingName(funding.Id));
            var row = DeskUi.Label($"{heading} · remaining {DeskUi.Money(funding.RemainingCents)}\nAvailable day {funding.AvailableDay} · expires day {funding.ExpiresDay} · {(funding.UsableOn(view.Day) ? "usable today" : "not usable today")}");
            var purpose = funding.Capabilities.Count == 0 ? "any" : string.Join(", ", funding.Capabilities.Select(id => CapabilityName(view, id).ToLowerInvariant()));
            row.Text += $"\nPurpose: {purpose}";
            if (funding.AvailableDay > funding.PlannedAvailableDay)
                row.Text += $"\nRelease: planned day {funding.PlannedAvailableDay}, now day {funding.AvailableDay}";
            row.FocusMode = FocusModeEnum.All;
            row.SetMeta("funding", funding.Id); linkedLedger.AddChild(row);
        }
        if (view.Funding.Count == 0) linkedLedger.AddChild(DeskUi.Label("No funding lines."));
        linkedLedger.AddChild(DeskUi.Label("What we are paying for"));
        foreach (var arrangement in view.Arrangements)
        {
            var button = DeskUi.Button($"{DeskUi.ListLabel(view.ArrangementName(arrangement.Id))} · {ArrangementStatusText(arrangement.Status)}\n{ArrangementPaymentText(view, arrangement)}", () =>
            {
                OpenContractView(arrangement.Id);
            });
            button.ToggleMode = true; button.ButtonPressed = SelectedArrangement == arrangement.Id;
            button.SetMeta("arrangement", arrangement.Id); linkedLedger.AddChild(button);
            AddCorDesignation(linkedLedger, arrangement, view);
            AddArrangementActions(linkedLedger, arrangement, view);
        }
        if (view.Arrangements.Count == 0) linkedLedger.AddChild(DeskUi.Label("No support to pay for yet."));
        if (ContractView?.Visible == true) RefreshContractView(view);
    }

    private void OnContinue()
    {
        if (ReadingTurnover || PlayingDay) return;
        ShowGameSurface();
        var count = 0;
        var reason = "deployment complete";
        while (AdvanceAndAutosave())
        {
            count++;
            var board = session.View!.MorningBoard();
            if (HasPeopleNews(session.View!, board)) { reason = "new messages have arrived"; break; }
            if (board.Lead is not null) { reason = "the next priority item is ready"; break; }
        }
        if (session.View?.Finished == true) reason = "scenario complete";
        else if (count > 0) FocusNewMorning();
        RefreshDisplay($"Continued {count} days: {reason}. Results through day {session.CompletedDay}.", developerOnly: true);
        if (count > 0) BeginDayPlayback();
    }
}
