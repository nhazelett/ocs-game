using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal PanelContainer ContractView { get; private set; } = null!;
    internal TabBar ContractDeskTabs { get; private set; } = null!;
    internal ConfirmationDialog EndContractDialog { get; private set; } = null!;
    private VBoxContainer contractContents = null!, contractFooter = null!;
    private TerminateAction? contractToEnd;
    private string? contractToEndHash;
    private string? contractReturnVendor, contractReturnRequest, contractReturnInitiative;
    private int contractReturnScroll;
    internal Dictionary<(string Id, string Choice), Button> ContractPerformanceChoices { get; } = [];

    private void BuildContractView()
    {
        ContractView = new PanelContainer { Name = "ContractDetails", ZIndex = 14, Visible = false };
        AddChild(ContractView); ContractView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ContractView.OffsetTop = 64;
        // Rebuilding a hidden, wrapped picker can temporarily enlarge the page. Refit when its minimum settles.
        void Fit()
        {
            if (!GodotObject.IsInstanceValid(ContractView) || !ContractView.IsInsideTree() || !ContractView.Visible) return;
            ContractView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); ContractView.OffsetTop = 64;
            ContractView.Size = Size - new Vector2(0, 64);
        }
        void QueueFit() => Callable.From(Fit).CallDeferred();
        ContractView.MinimumSizeChanged += QueueFit; ContractView.VisibilityChanged += QueueFit; Resized += QueueFit;
        contractContents = DeskUi.Pane(ContractView, "ContractRecord", out contractFooter);
        EndContractDialog = new ConfirmationDialog { Title = "End this contract?", Exclusive = true };
        AddChild(EndContractDialog);
        EndContractDialog.GetLabel().AutowrapMode = TextServer.AutowrapMode.WordSmart;
        EndContractDialog.GetLabel().CustomMinimumSize = new Vector2(560, 0);
        EndContractDialog.GetOkButton().Text = "End this contract";
        EndContractDialog.GetCancelButton().Text = "Keep this contract";
        EndContractDialog.Canceled += () => contractToEnd = null;
        EndContractDialog.Confirmed += () =>
        {
            var action = contractToEnd; contractToEnd = null; EndContractDialog.Hide();
            if (action is null || contractToEndHash != session.StateHash || session.View is not { Finished: false } view) return;
            var arrangement = view.Arrangements.FirstOrDefault(a => a.Id == action.Arrangement);
            if (arrangement is null || !CanEndContract(view, arrangement) || !view.PreviewAny(action).Accepted) return;
            SendAction(action);
        };
        GetNode<TabContainer>("%DeskTabs").TabChanged += _ => CloseContractView();
    }

    private static bool CanEndContract(SessionView view, Arrangement arrangement)
        => !view.Finished && !arrangement.IsCoordination && view.TerminateBlocked(arrangement.Id) is null
            && arrangement.Status is not (ArrangementStatus.Ended or ArrangementStatus.Declined)
            && !view.Work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == arrangement.Id);

    internal void CloseContractView()
    {
        ContractView?.Hide(); EndContractDialog?.Hide(); contractToEnd = null; terminations.Clear(); ContractPerformanceChoices.Clear(); contractReturnVendor = contractReturnRequest = contractReturnInitiative = null;
    }

    internal void OpenContractView(string id, string? fromVendor = null, string? fromRequest = null, string? fromInitiative = null)
    {
        if (session.View?.Arrangements.Any(a => a.Id == id) != true) return;
        if (fromRequest is null && fromInitiative is null) CloseRequestWorkspace(restore: false);
        var vendorScroll = vendorCardsScroll.ScrollVertical;
        if (fromRequest is null && fromInitiative is null) GetNode<TabContainer>("%DeskTabs").CurrentTab = PaymentsTab;
        contractReturnRequest = fromRequest; contractReturnInitiative = fromInitiative; contractReturnVendor = fromVendor; contractReturnScroll = vendorScroll;
        SelectedArrangement = id; SelectedFunding = null; RenderLedgerContext(session.View);
        ContractView.Show(); RefreshContractView(session.View);
    }

    private void ReturnFromContract()
    {
        var vendor = contractReturnVendor; var request = contractReturnRequest; var initiative = contractReturnInitiative; var scroll = contractReturnScroll;
        CloseContractView();
        if (request is not null)
        {
            if (!RequestWorkspace.Visible || SelectedCard != request) GoToMarket(request);
            return;
        }
        if (initiative is not null)
        {
            if (awardTarget != initiative) SelectAwardTarget(initiative);
            OpenRequestWorkspace();
            return;
        }
        if (vendor is null) return;
        var changedVendor = selectedVendor != vendor; selectedVendor = vendor;
        GetNode<TabContainer>("%DeskTabs").CurrentTab = VendorsTab;
        if (changedVendor) RefreshBpaDesk(session.View);
        var hash = session.StateHash;
        Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(this) && IsInsideTree() && session.StateHash == hash
                && selectedVendor == vendor && GetNode<TabContainer>("%DeskTabs").CurrentTab == VendorsTab)
                vendorCardsScroll.ScrollVertical = scroll;
        }).CallDeferred();
    }

    private void RefreshContractView(SessionView? view)
    {
        terminations.Clear(); ContractPerformanceChoices.Clear(); DeskUi.Clear(contractContents); DeskUi.Clear(contractFooter);
        var arrangement = view?.Arrangements.FirstOrDefault(a => a.Id == SelectedArrangement);
        if (view is null || arrangement is null) { CloseContractView(); return; }
        var back = contractReturnInitiative is not null ? "Back to your own buys" : contractReturnRequest is not null ? "Back to the request" : contractReturnVendor is { } vendor ? "Back to " + view.VendorCards().Single(c => c.Id == vendor).Name : "Back to what we are paying for";
        var navigation = new HBoxContainer(); contractContents.AddChild(navigation);
        navigation.AddChild(DeskUi.Button(back, ReturnFromContract));
        var bar = ContractDeskTabs = new TabBar { Name = "DeskNavigation", ClipTabs = false }; navigation.AddChild(bar);
        var tabs = GetNode<TabContainer>("%DeskTabs");
        for (var i = 0; i < tabs.GetTabCount(); i++)
        {
            bar.AddTab(tabs.GetTabTitle(i));
            bar.SetTabHidden(i, tabs.IsTabHidden(i));
        }
        bar.CurrentTab = PaymentsTab;
        bar.TabSelected += index =>
        {
            if (!ContractView.Visible || !bar.IsInsideTree()) return;
            CloseContractView(); CloseRequestWorkspace(restore: false); tabs.CurrentTab = (int)index;
        };
        var title = DeskUi.Label(DeskUi.ListLabel(view.ArrangementName(arrangement.Id)));
        title.AddThemeFontSizeOverride("font_size", 28); contractContents.AddChild(title);
        var site = view.NameOf(arrangement.SiteId);
        var unit = view.ActionEditor(arrangement.SiteId, arrangement.CapabilityId)?.Unit ?? "units";
        var supplies = $"{DeskUi.Quantity(arrangement.QuantityMilli / 1000m)} {unit} a day at {site}, days {arrangement.StartDay} to {arrangement.EndDay}";
        var scope = string.Join(", ", view.Rules.Model.Capabilities.Single(c => c.Id == arrangement.CapabilityId).Lines
            .Where(line => arrangement.Lines.Contains(line.Id)).Select(line => line.Label?.ToLowerInvariant() ?? "support"));
        contractContents.AddChild(DeskUi.Label($"{ArrangementStatusText(view, arrangement)}. Supplies {supplies}."));
        if (scope.Length > 0) contractContents.AddChild(DeskUi.Label("Includes " + scope + "."));
        contractContents.AddChild(DeskUi.Label(ArrangementPaymentText(view, arrangement, shortDispute: false) + (view.DisputedDaysLine(arrangement.Id) is { } disputed ? "\n" + disputed : "")));
        if (arrangement.NoticeDays is { } days)
            contractContents.AddChild(DeskUi.Label($"The contract requires {days} days of notice."));
        var hasModification = AddContractModification(contractContents, view, arrangement.Id);
        foreach (var issue in view.PerformanceIssues.Where(i => i.ArrangementId == arrangement.Id && i.Outcome != CureOutcome.Cured))
        {
            contractContents.AddChild(DeskUi.Heading(DeskUi.ListLabel(view.NameOf(issue.Id))));
            AddPerformanceRecord(contractContents, view, issue);
            AddPerformanceActions(contractContents, view, issue, ContractPerformanceChoices, includeContractLink: false);
        }
        var blocked = view.TerminateBlocked(arrangement.Id);
        if (blocked is not null) { contractContents.AddChild(DeskUi.Label(blocked)); return; }
        if (arrangement.IsCoordination || view.EndWords(arrangement.Id) is not { } endWords) return;
        var endingWork = view.Work.FirstOrDefault(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == arrangement.Id);
        var ending = endingWork is not null;
        var endPanel = ContractPerformanceChoices.Count > 0 || hasModification ? contractContents : contractFooter;
        var action = new TerminateAction { Day = view.Day, Arrangement = arrangement.Id, Assignee = endingWork?.Assignee };
        var assigned = AddAssignedAction(endPanel, endPanel, view, action,
            ending ? endWords.Button + " (in work)" : endWords.Button + "…",
            endWords.Says,
            stillCurrent: () => ContractView.Visible && SelectedArrangement == arrangement.Id && !EndContractDialog.Visible,
            // DESK-196: what ending it costs depends on the day it is done, so the words follow who does it.
            costText: (chosen, preview) => ActionCostText(view, chosen, view.EndWords(arrangement.Id, (TerminateAction)chosen)?.Says ?? endWords.Says, preview),
            commit: chosen =>
            {
                var current = session.View!;
                var words = current.EndWords(arrangement.Id, (TerminateAction)chosen)!;
                contractToEnd = (TerminateAction)chosen; contractToEndHash = session.StateHash;
                EndContractDialog.Title = words.Button + "?";
                EndContractDialog.GetOkButton().Text = words.Button;
                EndContractDialog.GetCancelButton().Text = arrangement.Instrument == "gpc" ? "Keep this card buy" : "Keep this contract";
                EndContractDialog.DialogText = $"{words.Button}? Here is what stops.\n\n{DeskUi.ListLabel(current.ArrangementName(arrangement.Id))} supplies {supplies}.\nIncludes {scope}.\n\n"
                    + ActionCostText(current, chosen, words.Says);
                EndContractDialog.PopupCentered(new Vector2I(620, 300));
                EndContractDialog.GetCancelButton().GrabFocus();
            }, inWork: endingWork?.Id, showInWorkPicker: true);
        terminations[arrangement.Id] = assigned.Button;
    }

    private static string ArrangementPaymentText(SessionView view, Arrangement arrangement, bool shortDispute = true)
    {
        if (view.TerminateBlocked(arrangement.Id) is not null
            && arrangement.ObligatedCents == 0 && arrangement.IncurredCents == 0 && arrangement.UnobligatedIncurredCents == 0)
            return arrangement.Instrument == "hostNation"
                ? "Provided under the host nation agreement; this office has no recorded costs."
                : "Provided by the J4 under the ACSA; this office has no recorded costs.";
        return (arrangement.Instrument == "gpc" ? "Paid from the card program.\n" : "") + $"Money committed {DeskUi.Money(arrangement.ObligatedCents)} · cost incurred {DeskUi.Money(arrangement.IncurredCents)}\nCost not covered by committed funds {DeskUi.Money(arrangement.UnobligatedIncurredCents)}"
            + (shortDispute && view.DisputedDaysLine(arrangement.Id) is not null ? "\nDisputed supply was paid in full; open for details." : "");
    }

    private static string ArrangementStatusText(SessionView view, Arrangement arrangement) => view.StatusToday(arrangement.Id) switch
    {
        ArrangementStatus.Pending => arrangement.StartDay == view.Day ? "Starts today" : $"Starts on day {arrangement.StartDay}", ArrangementStatus.Active => "Providing support",
        ArrangementStatus.Suspended => "Support paused", ArrangementStatus.Ended => "Ended",
        ArrangementStatus.Declined => "Declined", _ => view.StatusToday(arrangement.Id)?.ToString() ?? ""
    };
}
