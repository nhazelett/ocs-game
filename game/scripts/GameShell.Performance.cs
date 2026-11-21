using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer performanceRows = null!;
    internal Dictionary<(string Id, string Choice), Button> PerformanceChoices { get; } = [];
    internal VBoxContainer PerformanceRows => performanceRows;
    internal VBoxContainer TodayPerformanceRows { get; private set; } = null!;
    internal Dictionary<string, Button> TodayPerformanceLinks { get; } = [];

    private void BuildPerformanceDesk()
    {
        TodayPerformanceRows = new VBoxContainer { Name = "VendorProblems", Visible = false };
        TodayPerformanceRows.AddThemeConstantOverride("separation", 14);
        boardContents.AddChild(TodayPerformanceRows);
        boardContents.MoveChild(TodayPerformanceRows, MorningBrief.GetIndex() + 1);
        performanceRows = new VBoxContainer { Name = "PerformanceDecisions" };
        performanceRows.SortChildren += performanceRows.UpdateMinimumSize;
        var inboxBody = GetNode<VBoxContainer>("%DeskTabs/Inbox/Contents/Items");
        inboxBody.SortChildren += inboxBody.UpdateMinimumSize;
        GetNode<TabContainer>("%DeskTabs").GetNode("Inbox/Contents/Items").AddChild(performanceRows);
    }

    private void RefreshPerformance(SessionView? view)
    {
        DeskUi.Clear(performanceRows); PerformanceChoices.Clear();
        DeskUi.Clear(TodayPerformanceRows); TodayPerformanceRows.Hide(); TodayPerformanceLinks.Clear();
        performanceRows.AddChild(DeskUi.Label("Vendor performance"));
        if (view is null || view.PerformanceIssues.Count == 0)
        {
            var reports = view?.Records.Where(view.Known).Where(r => r.Kind == RecordKind.CorReport).OrderByDescending(r => r.Day).ToArray() ?? [];
            if (reports.Length == 0) performanceRows.AddChild(DeskUi.Label("No performance issues reported."));
            else
            {
                performanceRows.AddChild(DeskUi.Label("Reports on file"));
                foreach (var report in reports)
                {
                    var line = DeskUi.Label($"Day {report.Day} · {report.Text}");
                    line.SetMeta("performance_report", report.Id); performanceRows.AddChild(line);
                }
            }
            return;
        }
        foreach (var issue in view.PerformanceIssues.OrderByDescending(i => i.OpenedDay))
        {
            var row = new VBoxContainer(); row.SetMeta("issue", issue.Id); performanceRows.AddChild(row);
            var site = view.Scenario.Sites.Single(s => s.Id == issue.SiteId).Name;
            var decision = view.PerformanceDecision(issue);
            row.AddChild(DeskUi.Label(DeskUi.ListLabel(view.NameOf(issue.Id)) + "\n"
                + (decision == "no decision" ? "No decision yet." : DeskUi.ListLabel(decision) + ".")));
            AddPerformanceRecord(row, view, issue);
            if (issue.CureEndsDay is { } end)
                row.AddChild(DeskUi.Label($"Cure period ends day {end} · outcome: {issue.Outcome}"));
            foreach (var outcome in view.Records.Where(view.Known).Where(r => r.Subject == issue.Id && r.Kind is RecordKind.Compliance or RecordKind.WorkDone))
                row.AddChild(DeskUi.Label($"Day {outcome.Day} · {outcome.Text}"));
            AddPerformanceActions(row, view, issue, PerformanceChoices, includeContractLink: true);
            // Reuse the Inbox's existing action availability; opening this link spends nothing.
            if (!PerformanceChoices[(issue.Id, "cure")].Disabled) AddTodayPerformanceLink(view, issue);

        }
    }

    private static void AddPerformanceRecord(VBoxContainer row, SessionView view, PerformanceIssueState issue)
    {
        var record = view.Records.FirstOrDefault(r => r.Id == issue.CauseId && view.Known(r));
        if (record is null) return;
        var text = DeskUi.Label(record.Text); text.SetMeta("issue_record", issue.Id); row.AddChild(text);
    }

    private void AddPerformanceActions(VBoxContainer row, SessionView view, PerformanceIssueState issue,
        Dictionary<(string Id, string Choice), Button> choices, bool includeContractLink)
    {
        var arrangement = view.Arrangements.Single(a => a.Id == issue.ArrangementId);
        var running = arrangement.Status is ArrangementStatus.Active or ArrangementStatus.Suspended;
        var curing = view.Work.Any(w => w.Kind == WorkKind.CureNotice && w.CompletedDay is null && (string?)w.Payload == issue.Id);
        var terminating = view.Work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == issue.ArrangementId);
        var cureAction = new CureNoticeAction { Day = view.Day, Issue = issue.Id };
        var cure = AddAssignedAction(row, row, view, cureAction, curing ? "Cure notice in work" : "Cure notice",
            "Issues a cure notice to put this vendor problem right.",
            blocked: () => terminating ? "Ending this contract is already in work." : null, inWork: curing).Button;
        var resource = DeskUi.Button("Re-source…", () =>
        {
            CloseContractView(); SwitchAwardTarget(issue.SiteId + "." + issue.CapabilityId);
            RefreshActionDesk(session.View); ShowMarket();
        });
        resource.Disabled = view.Finished || !running;
        var resourceReason = DeskUi.ActionReason(row, resource);
        DeskUi.Gate(resource, resourceReason, view.Finished ? DeskUi.FinishedReason : !running ? "This contract is no longer running." : null);
        if (includeContractLink)
        {
            row.AddChild(DeskUi.Label("To end this contract, open its contract page."));
            var terminate = DeskUi.Button("View contract", () => OpenContractView(issue.ArrangementId));
            var contractReason = DeskUi.ActionReason(row, terminate);
            DeskUi.Gate(terminate, contractReason, view.Finished ? DeskUi.FinishedReason : !running ? "This contract is no longer running."
                : terminating ? "Ending this contract is already in work." : null);
            choices.Add((issue.Id, "terminate"), terminate);
        }
        choices.Add((issue.Id, "cure"), cure);
        choices.Add((issue.Id, "resource"), resource);
    }

    private void AddTodayPerformanceLink(SessionView view, PerformanceIssueState issue)
    {
        var issueId = issue.Id; var causeId = issue.CauseId;
        var alert = MorningAlerts.GetChildren().OfType<PanelContainer>()
            .FirstOrDefault(panel => panel.HasMeta("morning_shortfall")
                && panel.GetMeta("morning_shortfall").AsString() == issue.SiteId + "." + issue.CapabilityId);
        var news = MorningBrief.GetChildren().OfType<PanelContainer>()
            .FirstOrDefault(panel => panel.HasMeta("news_record") && panel.GetMeta("news_record").AsString() == causeId);
        VBoxContainer parent;
        if (alert is not null) parent = alert.GetChild<VBoxContainer>(0);
        else if (news is not null) parent = news.GetChild<VBoxContainer>(0);
        else
        {
            if (!TodayPerformanceRows.Visible) TodayPerformanceRows.AddChild(DeskUi.Heading("Vendor problems"));
            TodayPerformanceRows.Show();
            var panel = new PanelContainer(); panel.SetMeta("today_issue", issueId); TodayPerformanceRows.AddChild(panel);
            parent = new VBoxContainer(); parent.AddThemeConstantOverride("separation", 6); panel.AddChild(parent);
            parent.AddChild(DeskUi.Label(DeskUi.ListLabel(view.NameOf(issueId))));
        }
        var hash = session.StateHash;
        var open = DeskUi.Button("Open vendor decision", () => OpenPerformanceDecision(issueId, hash));
        open.SetMeta("performance_link", issueId); parent.AddChild(open); TodayPerformanceLinks[issueId] = open;
    }

    private void OpenPerformanceDecision(string issueId, string? hash)
    {
        if (hash != session.StateHash || PlayingDay || ReadingDaySummary || ReadingTurnover || AtFrontDoor
            || !PerformanceChoices.TryGetValue((issueId, "cure"), out var cure)) return;
        FocusInboxDecision(cure.GetParent().GetChild<Control>(0), hash);
    }

    private async void FocusInboxDecision(Control heading, string? hash)
    {
        if (hash != session.StateHash || PlayingDay || ReadingDaySummary || ReadingTurnover || AtFrontDoor) return;
        var tabs = GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = InboxTab;
        var tree = GetTree();
        // The first visit wraps and sizes the record list before coordinates are reliable.
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()
            || hash != session.StateHash || tabs.CurrentTab != InboxTab
            || !GodotObject.IsInstanceValid(heading) || !heading.IsInsideTree() || heading.IsQueuedForDeletion()) return;
        heading.FocusMode = Control.FocusModeEnum.All; heading.GrabFocus();
        var inbox = tabs.GetNode<ScrollContainer>("Inbox/Contents");
        // EnsureControlVisible only exposes the heading at the nearest edge. Align its
        // top instead, so the context and the choices below it open together.
        inbox.ScrollVertical += Mathf.RoundToInt(heading.GlobalPosition.Y - inbox.GlobalPosition.Y);
    }
}
