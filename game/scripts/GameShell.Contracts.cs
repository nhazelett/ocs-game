using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal PanelContainer ContractView { get; private set; } = null!;
    internal ConfirmationDialog EndContractDialog { get; private set; } = null!;
    private VBoxContainer contractContents = null!, contractFooter = null!;
    private string? contractToEnd;
    internal Dictionary<(string Id, string Choice), Button> ContractPerformanceChoices { get; } = [];

    private void BuildContractView()
    {
        ContractView = new PanelContainer { Name = "ContractDetails", ZIndex = 14, Visible = false };
        AddChild(ContractView); ContractView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ContractView.OffsetTop = 64;
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
            var id = contractToEnd; contractToEnd = null; EndContractDialog.Hide();
            if (id is null || session.View is not { Finished: false } view) return;
            var arrangement = view.Arrangements.FirstOrDefault(a => a.Id == id);
            if (arrangement is null || !CanEndContract(view, arrangement)) return;
            SendAction(new TerminateAction { Day = view.Day, Arrangement = id });
        };
        GetNode<TabContainer>("%DeskTabs").TabChanged += _ => CloseContractView();
    }

    private static bool CanEndContract(SessionView view, Arrangement arrangement)
        => !view.Finished && !arrangement.IsCoordination && view.TerminateBlocked(arrangement.Id) is null
            && arrangement.Status is not (ArrangementStatus.Ended or ArrangementStatus.Declined)
            && !view.Work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == arrangement.Id);

    internal void CloseContractView()
    {
        ContractView?.Hide(); EndContractDialog?.Hide(); contractToEnd = null; terminations.Clear(); ContractPerformanceChoices.Clear();
    }

    internal void OpenContractView(string id)
    {
        if (session.View?.Arrangements.Any(a => a.Id == id) != true) return;
        CloseRequestWorkspace(restore: false);
        GetNode<TabContainer>("%DeskTabs").CurrentTab = PaymentsTab;
        SelectedArrangement = id; SelectedFunding = null; RenderLedgerContext(session.View);
        ContractView.Show(); RefreshContractView(session.View);
    }

    private void RefreshContractView(SessionView? view)
    {
        terminations.Clear(); ContractPerformanceChoices.Clear(); DeskUi.Clear(contractContents); DeskUi.Clear(contractFooter);
        var arrangement = view?.Arrangements.FirstOrDefault(a => a.Id == SelectedArrangement);
        if (view is null || arrangement is null) { CloseContractView(); return; }
        contractContents.AddChild(DeskUi.Button("Back to what we are paying for", CloseContractView));
        var title = DeskUi.Label(DeskUi.ListLabel(view.ArrangementName(arrangement.Id)));
        title.AddThemeFontSizeOverride("font_size", 28); contractContents.AddChild(title);
        var site = view.NameOf(arrangement.SiteId);
        var unit = view.ActionEditor(arrangement.SiteId, arrangement.CapabilityId)?.Unit ?? "units";
        var supplies = $"{DeskUi.Quantity(arrangement.QuantityMilli / 1000m)} {unit} a day at {site}, days {arrangement.StartDay} to {arrangement.EndDay}";
        var scope = string.Join(", ", view.Rules.Model.Capabilities.Single(c => c.Id == arrangement.CapabilityId).Lines
            .Where(line => arrangement.Lines.Contains(line.Id)).Select(line => line.Label?.ToLowerInvariant() ?? "support"));
        contractContents.AddChild(DeskUi.Label($"{ArrangementStatusText(view, arrangement)}. Supplies {supplies}."));
        if (scope.Length > 0) contractContents.AddChild(DeskUi.Label("Includes " + scope + "."));
        contractContents.AddChild(DeskUi.Label(ArrangementPaymentText(view, arrangement)));
        if (arrangement.NoticeDays is { } days)
            contractContents.AddChild(DeskUi.Label($"The contract requires {days} days of notice."));
        foreach (var issue in view.PerformanceIssues.Where(i => i.ArrangementId == arrangement.Id && i.Outcome != CureOutcome.Cured))
        {
            contractContents.AddChild(DeskUi.Heading(DeskUi.ListLabel(view.NameOf(issue.Id))));
            AddPerformanceRecord(contractContents, view, issue);
            AddPerformanceActions(contractContents, view, issue, ContractPerformanceChoices, includeContractLink: false);
        }
        var blocked = view.TerminateBlocked(arrangement.Id);
        if (blocked is not null) { contractContents.AddChild(DeskUi.Label(blocked)); return; }
        if (arrangement.IsCoordination) return;
        var ending = view.Work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == arrangement.Id);
        var end = DeskUi.Button(ending ? "Ending this contract is in work" : "End this contract…", () =>
        {
            var current = session.View;
            var target = current?.Arrangements.FirstOrDefault(a => a.Id == arrangement.Id);
            if (current is null || target is null || !CanEndContract(current, target)) return;
            contractToEnd = target.Id;
            EndContractDialog.DialogText = $"End this contract? Here is what stops.\n\n{DeskUi.ListLabel(current.ArrangementName(target.Id))} supplies {supplies}.\nIncludes {scope}.\n\nEnding it uses shop time. Its support stops when that work is complete.";
            EndContractDialog.PopupCentered(new Vector2I(620, 300));
            EndContractDialog.GetCancelButton().GrabFocus();
        });
        var action = new TerminateAction { Day = view.Day, Arrangement = arrangement.Id };
        var preview = view.PreviewAny(action);
        var endPanel = ContractPerformanceChoices.Count > 0 ? contractContents : contractFooter;
        var reason = DeskUi.ActionReason(endPanel, end);
        AddActionCost(endPanel, view, action, "Ends this contract's support when the work and any notice are complete.", preview);
        DeskUi.Gate(end, reason, view.Finished ? DeskUi.FinishedReason : ending ? "Ending this contract is already in work."
            : arrangement.Status is ArrangementStatus.Ended or ArrangementStatus.Declined ? "This contract is no longer providing support." : preview.HardStops.FirstOrDefault()); terminations[arrangement.Id] = end;
    }

    private static string ArrangementPaymentText(SessionView view, Arrangement arrangement)
    {
        if (view.TerminateBlocked(arrangement.Id) is not null
            && arrangement.ObligatedCents == 0 && arrangement.IncurredCents == 0 && arrangement.UnobligatedIncurredCents == 0)
            return arrangement.Instrument == "hostNation"
                ? "Provided under the host nation agreement; this office has no recorded costs."
                : "Provided by the J4 under the ACSA; this office has no recorded costs.";
        return (arrangement.Instrument == "gpc" ? "Paid from the card program.\n" : "") + $"Money committed {DeskUi.Money(arrangement.ObligatedCents)} · cost of support received {DeskUi.Money(arrangement.IncurredCents)}\nReceived but not covered by committed funds {DeskUi.Money(arrangement.UnobligatedIncurredCents)}";
    }

    private static string ArrangementStatusText(SessionView view, Arrangement arrangement) => arrangement.Status switch
    {
        ArrangementStatus.Pending => arrangement.StartDay == view.Day ? "Starts today" : $"Starts on day {arrangement.StartDay}", ArrangementStatus.Active => "Providing support",
        ArrangementStatus.Suspended => "Support paused", ArrangementStatus.Ended => "Ended",
        ArrangementStatus.Declined => "Declined", _ => arrangement.Status.ToString()
    };
}
