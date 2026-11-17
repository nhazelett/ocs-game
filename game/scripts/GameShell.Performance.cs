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
            performanceRows.AddChild(DeskUi.Label("No performance issues reported.")); return;
        }
        foreach (var issue in view.PerformanceIssues.OrderByDescending(i => i.OpenedDay))
        {
            var row = new VBoxContainer(); row.SetMeta("issue", issue.Id); performanceRows.AddChild(row);
            var site = view.Scenario.Sites.Single(s => s.Id == issue.SiteId).Name;
            row.AddChild(DeskUi.Label($"{view.NameOf(issue.Id)}\nDecision: {view.PerformanceDecision(issue)}"));
            var record = view.Records.FirstOrDefault(r => r.Id == issue.CauseId && view.Known(r));
            if (record is not null) row.AddChild(DeskUi.Label(record.Text));
            if (issue.CureEndsDay is { } end)
                row.AddChild(DeskUi.Label($"Cure period ends day {end} · outcome: {issue.Outcome}"));
            foreach (var outcome in view.Records.Where(view.Known).Where(r => r.Subject == issue.Id && r.Kind is RecordKind.Compliance or RecordKind.WorkDone))
                row.AddChild(DeskUi.Label($"Day {outcome.Day} · {outcome.Text}"));
            var arrangement = view.Arrangements.Single(a => a.Id == issue.ArrangementId);
            var running = arrangement.Status is ArrangementStatus.Active or ArrangementStatus.Suspended;
            var curing = view.Work.Any(w => w.Kind == WorkKind.CureNotice && w.CompletedDay is null && (string?)w.Payload == issue.Id);
            var terminating = view.Work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == issue.ArrangementId);
            var cureAction = new CureNoticeAction { Day = view.Day, Issue = issue.Id };
            var cure = DeskUi.Button(curing ? "Cure notice in work" : "Cure notice", () => SendAction(cureAction));
            cure.Disabled = view.Finished || !running || curing || terminating || issue.CureNoticeDay is not null;
            var resource = DeskUi.Button("Re-source…", () =>
            {
                ResetAwardForm(); awardTarget = issue.SiteId + "." + issue.CapabilityId;
                RefreshActionDesk(session.View); ShowMarket();
            });
            resource.Disabled = view.Finished || !running;
            var terminate = DeskUi.Button("View contract", () => OpenContractView(issue.ArrangementId));
            terminate.Disabled = view.Finished || !running || terminating;
            var cureReason = DeskUi.ActionReason(row, cure);
            DeskUi.Gate(cure, cureReason, view.Finished ? DeskUi.FinishedReason : !running ? "This contract is no longer running."
                : terminating ? "Ending this contract is already in work." : curing ? "The cure notice is already in work."
                : issue.CureNoticeDay is not null ? "A cure notice has already been issued." : null);
            AddActionCost(row, view, cureAction, "Issues a cure notice to put this vendor problem right.");
            var resourceReason = DeskUi.ActionReason(row, resource);
            DeskUi.Gate(resource, resourceReason, view.Finished ? DeskUi.FinishedReason : !running ? "This contract is no longer running." : null);
            var contractReason = DeskUi.ActionReason(row, terminate);
            DeskUi.Gate(terminate, contractReason, view.Finished ? DeskUi.FinishedReason : !running ? "This contract is no longer running."
                : terminating ? "Ending this contract is already in work." : null);
            PerformanceChoices.Add((issue.Id, "cure"), cure);
            PerformanceChoices.Add((issue.Id, "resource"), resource);
            PerformanceChoices.Add((issue.Id, "terminate"), terminate);
            // Reuse the Inbox's existing action availability; opening this link spends nothing.
            if (!cure.Disabled) AddTodayPerformanceLink(view, issue.Id, issue.CauseId);

        }
    }

    private void AddTodayPerformanceLink(SessionView view, string issueId, string causeId)
    {
        var news = MorningBrief.GetChildren().OfType<PanelContainer>()
            .FirstOrDefault(panel => panel.HasMeta("news_record") && panel.GetMeta("news_record").AsString() == causeId);
        VBoxContainer parent;
        if (news is not null) parent = news.GetChild<VBoxContainer>(0);
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

    private async void OpenPerformanceDecision(string issueId, string? hash)
    {
        if (hash != session.StateHash || PlayingDay || ReadingDaySummary || ReadingTurnover || AtFrontDoor) return;
        var tabs = GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = InboxTab;
        var tree = GetTree();
        // The first visit to Inbox must wrap and size its long record list before it can scroll.
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()
            || hash != session.StateHash || tabs.CurrentTab != InboxTab
            || !PerformanceChoices.TryGetValue((issueId, "cure"), out var cure)
            || !PerformanceChoices.TryGetValue((issueId, "resource"), out var resource)) return;
        cure.GrabFocus();
        tabs.GetNode<ScrollContainer>("Inbox/Contents").EnsureControlVisible(resource);
    }
}
