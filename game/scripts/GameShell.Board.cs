using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer morningItems = null!, morningTiles = null!, linkedRequirement = null!, linkedLedger = null!;
    private VBoxContainer requirementFooter = null!, boardContents = null!, boardFooter = null!;
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
        tabs.TabChanged += tab =>
        {
            if (tab == TodayTab) GetNode<ScrollContainer>("%DeskTabs/Today/TodayPane/Body/Body").ScrollVertical = 0;
            else if (tab == InboxTab) GetNode<ScrollContainer>("%DeskTabs/Inbox/Contents").ScrollVertical = 0;
        };
        var board = boardContents = DeskUi.Pane(tabs.GetNode<Control>("Today"), "TodayPane", out boardFooter);
        BuildLead(board);
        boardFocus = DeskUi.Label(); boardDetails.AddChild(boardFocus);
        morningItems = new VBoxContainer { Name = "Items" }; boardDetails.AddChild(morningItems);
        morningTiles = new VBoxContainer { Name = "Tiles" }; boardDetails.AddChild(morningTiles);
        var requests = tabs.GetNode<Control>("Requests");
        BuildRequestWorkspace();
        requestList = DeskUi.Pane(requests, "RequestList", out requestListFooter);
        requests.GetNode<Control>("Market").Hide();
        linkedRequirement = DeskUi.Pane(RequestColumn, "RequirementPane", out requirementFooter);
        var requestPane = RequestColumn.GetNode<Control>("RequirementPane");
        // Rebuilding wrapped content can enlarge this Control before its width settles.
        // Keep the pane's offsets tied to its tab, as for the preparation page.
        void RefitRequestPane()
        {
            if (!GodotObject.IsInstanceValid(requestPane) || !requestPane.IsInsideTree() || !requestPane.IsVisibleInTree()) return;
            requestPane.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            requestPane.Size = RequestColumn.Size;
        }
        void QueueRequestRefit() => Callable.From(RefitRequestPane).CallDeferred();
        requestPane.VisibilityChanged += QueueRequestRefit;
        requestPane.MinimumSizeChanged += QueueRequestRefit;
        RequestColumn.Resized += QueueRequestRefit;
        linkedLedger = DeskUi.Panel(tabs.GetNode<Control>("Payments"), "LedgerContext");
    }

    private void ResetDeskSelection()
    {
        CancelEndDayCheck();
        ResetDaySummary();
        allDecisions = false;
        ResetOpening();
        TeamPanel?.Hide();
        CloseContractView(); CloseRequestWorkspace(restore: false);
        debriefHash = null;
        ResetVisits();
        BoardDetailsToggle.ButtonPressed = false;
        SelectedCard = SelectedCapability = SelectedArrangement = SelectedFunding = null;
        boardFocus.Text = "";
        Map.SelectSite(null);
        awardDrafts.Clear(); awardDraftDay = 0; ResetAwardForm();
        RenderRequirementContext(session.View); RenderRequestList(session.View);
        ResetBpaForm();
        coordinationQuantity = 1; coordinationTarget = null;
    }

    private void RefreshMorningDesk(SessionView? view, MorningBoardView? board)
    {
        DeskUi.Clear(morningItems); DeskUi.Clear(morningTiles);
        RefreshMorningBrief(board);
        RenderBar(TodayBar, view?.Bar(), compact: true);
        RefreshLead(view, board);
        RefreshStandings(view);
        RefreshDecisions(view);
        morningHeading.Text = board is null ? "No scenario loaded" :
            $"Morning of day {Math.Min(board.Day, board.Strip.LastDay)} · {DeskUi.Count(MorningAlerts.GetChildCount(), "alert")} · {DeskUi.Count(board.OpenRequests, "request")}";
        if (board?.Items.Count is null or 0) morningItems.AddChild(DeskUi.Label("No overnight changes."));
        foreach (var item in board?.Items ?? [])
        {
            var button = DeskUi.Button($"{item.Severity} · {BoardCategoryText(item.Category)}\n{item.Text}", () => NavigateBoardItem(item));
            button.Disabled = item.LinkKind == BoardLinkKind.None;
            button.SetMeta("link_kind", item.LinkKind.ToString()); button.SetMeta("link_id", item.LinkId);
            var reason = DeskUi.ActionReason(morningItems, button);
            DeskUi.Gate(button, reason, item.LinkKind == BoardLinkKind.None ? "This update has no further detail to open." : null);
        }
        foreach (var tile in board?.Tiles ?? [])
        {
            var key = tile.SiteId + "." + tile.CapabilityId;
            var name = view!.Scenario.Sites.Single(s => s.Id == tile.SiteId).Name;
            var button = DeskUi.Button($"{name} · {tile.Label}: {TileLevel(tile)}{(view.LostConfidenceSites.Contains(tile.SiteId) ? " · commander confidence lost" : "")}", () => SelectCapability(key));
            button.ToggleMode = true; button.ButtonPressed = SelectedCapability == key;
            button.SetMeta("capability", key); morningTiles.AddChild(button);
            if (tile.WhyShort is { } why)
            {
                var reason = DeskUi.Label(why); reason.SetMeta("tile_why_short", key); morningTiles.AddChild(reason);
            }
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
                boardFocus.Text = $"Selected: {tile.Label} at {name} · {TileLevel(tile)}\nPopulation day {day}: {population}" + (population == planned ? "" : $" · planned {planned}");
            }
        }
        RenderRequirementContext(view); RenderRequestList(view);
        RenderLedgerContext(view);
        RefreshBoardActions(view);
    }

    private static string BoardCategoryText(BoardCategory category) => category switch
    {
        BoardCategory.Delivery => "Delivery", BoardCategory.Failure => "Failure",
        BoardCategory.Report => "Report", BoardCategory.Request => "Request",
        BoardCategory.VendorNotice => "Vendor notice", BoardCategory.HigherHq => "Higher headquarters",
        _ => "Event"
    };

    internal void NavigateBoardItem(BoardItem item)
    {
        var tabs = GetNode<TabContainer>("%DeskTabs");
        switch (item.LinkKind)
        {
            case BoardLinkKind.Card:
                SelectedCard = item.LinkId; GoToMarket(item.LinkId);
                linkedRequirement.GetParent<ScrollContainer>().ScrollVertical = 0;
                break;
            case BoardLinkKind.SiteCapability: SelectCapability(item.LinkId); break;
            case BoardLinkKind.Arrangement:
                SelectedArrangement = item.LinkId; SelectedFunding = null; tabs.CurrentTab = PaymentsTab;
                RenderLedgerContext(session.View); break;
            case BoardLinkKind.Funding:
                SelectedArrangement = null;
                SelectedFunding = session.View?.Funding.Any(f => f.Id == item.LinkId) == true ? item.LinkId : null;
                tabs.CurrentTab = PaymentsTab; RenderLedgerContext(session.View);
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
                tabs.CurrentTab = PaymentsTab; RenderLedgerContext(session.View);
                linkedLedger.AddChild(DeskUi.Label(item.Text)); break;
            case BoardLinkKind.Person: FocusPerson(item); break;
            case BoardLinkKind.Island:
            case BoardLinkKind.Event:
                tabs.CurrentTab = TodayTab; boardFocus.Text = item.Text; break;
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
        GetNode<TabContainer>("%DeskTabs").CurrentTab = TodayTab;
        RefreshMorningDesk(view, view.MorningBoard());
    }

    private void RenderLedgerContext(SessionView? view)
    {
        DeskUi.Clear(linkedLedger);
        definitizations.Clear(); terminations.Clear(); CorDesignations.Clear();
        if (view is null) { linkedLedger.AddChild(DeskUi.Label("No scenario loaded.")); return; }
        linkedLedger.AddChild(DeskUi.Label($"Cash: {DeskUi.Money(view.TopStrip().CashCents)}"));
        linkedLedger.AddChild(DeskUi.Heading("Funding lines"));
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
        linkedLedger.AddChild(DeskUi.Heading("What we are paying for"));
        foreach (var arrangement in view.Arrangements)
        {
            var button = DeskUi.Button($"{DeskUi.ListLabel(view.ArrangementName(arrangement.Id))} · {ArrangementStatusText(view, arrangement)}\n{ArrangementPaymentText(view, arrangement)}", () =>
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

}
