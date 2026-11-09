using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer performanceRows = null!;
    internal Dictionary<(string Id, string Choice), Button> PerformanceChoices { get; } = [];
    internal VBoxContainer PerformanceRows => performanceRows;

    private void BuildPerformanceDesk()
    {
        performanceRows = new VBoxContainer { Name = "PerformanceDecisions" };
        GetNode<TabContainer>("%DeskTabs").GetNode("Inbox/Contents/Items").AddChild(performanceRows);
    }

    private void RefreshPerformance(SessionView? view)
    {
        DeskUi.Clear(performanceRows); PerformanceChoices.Clear();
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
            var cure = DeskUi.Button(curing ? "Cure notice in work" : "Cure notice", () =>
                SendAction(new CureNoticeAction { Day = session.View!.Day, Issue = issue.Id }));
            cure.Disabled = view.Finished || !running || curing || terminating || issue.CureNoticeDay is not null;
            var resource = DeskUi.Button("Re-source…", () =>
            {
                ResetAwardForm(); awardTarget = issue.SiteId + "." + issue.CapabilityId;
                RefreshActionDesk(session.View); GetNode<TabContainer>("%DeskTabs").CurrentTab = 2;
            });
            resource.Disabled = view.Finished || !running;
            var terminate = DeskUi.Button(terminating ? "Termination in work" : "Terminate", () =>
                SendAction(new TerminateAction { Day = session.View!.Day, Arrangement = issue.ArrangementId }));
            terminate.Disabled = view.Finished || !running || terminating;
            row.AddChild(cure); row.AddChild(resource); row.AddChild(terminate);
            PerformanceChoices.Add((issue.Id, "cure"), cure);
            PerformanceChoices.Add((issue.Id, "resource"), resource);
            PerformanceChoices.Add((issue.Id, "terminate"), terminate);
        }
    }
}
