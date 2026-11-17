using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal PanelContainer RequestWorkspace { get; private set; } = null!;
    internal Control RequestColumn { get; private set; } = null!;
    internal Control RequestBuyColumn { get; private set; } = null!;
    internal VBoxContainer RequestContents => linkedRequirement;
    internal ScrollContainer RequestScroll => linkedRequirement.GetParent<ScrollContainer>();
    internal Button RequestBackButton { get; private set; } = null!;
    internal Dictionary<string, Button> RequestListButtons { get; } = [];
    private VBoxContainer requestList = null!, requestListFooter = null!;
    private int requestReturnTab;
    private Control? requestReturnFocus;
    private readonly Dictionary<ScrollContainer, int> requestReturnScrolls = [];

    private void BuildRequestWorkspace()
    {
        RequestWorkspace = new PanelContainer { Name = "RequestWorkspace", ZIndex = 14, Visible = false, ClipContents = true };
        AddChild(RequestWorkspace);
        var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 12); RequestWorkspace.AddChild(layout);
        RequestBackButton = DeskUi.Button("Back to the desk", () => CloseRequestWorkspace());
        RequestBackButton.AutowrapMode = TextServer.AutowrapMode.Off;
        RequestBackButton.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; layout.AddChild(RequestBackButton);
        var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 24); layout.AddChild(columns);
        RequestColumn = new Control { Name = "Request", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        RequestBuyColumn = new Control { Name = "Buy", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        columns.AddChild(RequestColumn); columns.AddChild(RequestBuyColumn);
        void Fit()
        {
            RequestWorkspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            RequestWorkspace.OffsetLeft = 24; RequestWorkspace.OffsetRight = -24; RequestWorkspace.OffsetBottom = -16;
            RequestWorkspace.OffsetTop = GetNode<Control>("Margin/Layout/WorkArea").GlobalPosition.Y - GlobalPosition.Y;
            foreach (var host in new[] { RequestColumn, RequestBuyColumn })
                foreach (var pane in host.GetChildren().OfType<Control>())
                { pane.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); }
        }
        void QueueFit() => Callable.From(Fit).CallDeferred();
        GetNode<Control>("Margin/Layout/WorkArea").Resized += QueueFit;
        RequestWorkspace.VisibilityChanged += QueueFit;
        RequestColumn.Resized += QueueFit; RequestBuyColumn.Resized += QueueFit;
        GetNode<TabContainer>("%DeskTabs").TabChanged += _ => CloseRequestWorkspace(restore: false);
        QueueFit();
    }

    private void OpenRequestWorkspace()
    {
        if (session.View is null || AtFrontDoor || ReadingTurnover || PlayingDay || ReadingDaySummary) return;
        if (!RequestWorkspace.Visible)
        {
            var tabs = GetNode<TabContainer>("%DeskTabs"); requestReturnTab = tabs.CurrentTab;
            requestReturnFocus = GetViewport().GuiGetFocusOwner(); requestReturnScrolls.Clear();
            foreach (var scroll in tabs.FindChildren("*", "ScrollContainer", true, false).OfType<ScrollContainer>())
                requestReturnScrolls[scroll] = scroll.ScrollVertical;
        }
        CloseContractView(); Map.Tabletop?.CloseShortfall(); RequestWorkspace.Show();
    }

    internal void CloseRequestWorkspace(bool restore = true)
    {
        if (RequestWorkspace?.Visible != true) return;
        RequestWorkspace.Hide();
        if (!restore) { requestReturnScrolls.Clear(); return; }
        GetNode<TabContainer>("%DeskTabs").CurrentTab = requestReturnTab;
        var positions = requestReturnScrolls.ToArray(); requestReturnScrolls.Clear();
        var focus = requestReturnFocus;
        Callable.From(() =>
        {
            if (RequestWorkspace.Visible) return;
            if (GodotObject.IsInstanceValid(focus) && focus!.IsInsideTree() && focus.IsVisibleInTree()) focus.GrabFocus();
            foreach (var (scroll, value) in positions)
                if (GodotObject.IsInstanceValid(scroll) && scroll.IsInsideTree()) scroll.ScrollVertical = value;
        }).CallDeferred();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape } && RequestWorkspace.Visible
            && !EndDayDialog.Visible && !LeaveGameDialog.Visible)
        { CloseRequestWorkspace(); GetViewport().SetInputAsHandled(); }
    }

    private void RenderRequestList(SessionView? view)
    {
        DeskUi.Clear(requestList); DeskUi.Clear(requestListFooter); RequestListButtons.Clear();
        requestList.AddChild(DeskUi.Heading("Requests"));
        var standings = view?.WhereThingsStand() ?? [];
        var hash = session.StateHash;
        foreach (var card in view?.Cards ?? [])
        {
            var request = view!.RequirementEditor(card.Id);
            if (request is null) continue;
            var standing = standings.FirstOrDefault(s => s.CardId == card.Id);
            var text = request.Title + " · " + request.SiteName + "\n" + (standing?.Sentence ?? "Open this request to review its support.");
            if (standing?.OrderBy is { } order) text += "\n" + order.Sentence;
            var button = DeskUi.Button(text, () =>
            {
                if (hash == session.StateHash) NavigateBoardItem(new BoardItem("", view.Day, BoardCategory.Request,
                    BoardSeverity.Info, BoardLinkKind.Card, card.Id, "", []));
            });
            button.SetMeta("request_card", card.Id); requestList.AddChild(button); RequestListButtons.Add(card.Id, button);
        }
        if (RequestListButtons.Count == 0) requestList.AddChild(DeskUi.Label("No requests have arrived."));
        var initiative = DeskUi.Button("Buy on your own initiative", GoToOwnInitiative);
        var reason = DeskUi.ActionReason(requestListFooter, initiative);
        DeskUi.Gate(initiative, reason, view is null ? "Start a deployment to buy support." : view.Finished ? DeskUi.FinishedReason : null);
        requestListFooter.AddChild(DeskUi.Label("Arrange support nobody has requested."));
    }
}
