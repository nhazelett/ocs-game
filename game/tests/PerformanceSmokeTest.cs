using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PerformanceSmokeTest : Node
{
    public override void _Ready()
    {
        try
        {
            var shell = Shell(this);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b-90"));
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json"));
            Session Issue()
            {
                Scenario(shell, "spoke-b-90"); Replay(shell);
                while (shell.Session.View!.PerformanceIssues.Count == 0 && !shell.Session.View.Finished) Step(shell);
                Require(shell.Session.View.PerformanceIssues.Count > 0, "Replay reaches a performance issue.");
                return LinesOfSupport.Sim.Replay.Run(bundle, log, shell.Session.CompletedDay);
            }
            var expected = Issue();
            var issue = expected.View.PerformanceIssues.First();
            var record = expected.Records.Single(r => r.Id == issue.CauseId);
            Require(shell.PerformanceRows.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == record.Text), "Persistent inbox row shows the original issue record.");
            var hash = shell.Session.StateHash;
            shell.PerformanceChoices[(issue.Id, "resource")].EmitSignal(Button.SignalName.Pressed);
            Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 2 && shell.CurrentActionEditor!.SiteId == issue.SiteId && shell.CurrentActionEditor.CapabilityId == issue.CapabilityId, "Re-source opens the matching action editor.");
            Require(shell.Session.StateHash == hash && shell.Session.IsReplay, "Opening re-source neither changes Sim nor takes control.");
            var result = expected.Apply(new CureNoticeAction { Day = expected.Day, Issue = issue.Id });
            Require(result.Accepted, "Cure fixture is accepted.");
            shell.PerformanceChoices[(issue.Id, "cure")].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.PerformanceChoices[(issue.Id, "cure")].Disabled, "Cure sends the exact action and prevents a duplicate while in work.");
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(result.Message), "Cure ActionResult is visible.");
            while (issue.Outcome == CureOutcome.None && !expected.Finished)
            {
                expected.AdvanceDay(); Step(shell);
                if (issue.CureEndsDay is { } end)
                    Require(shell.PerformanceRows.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains($"Cure period ends day {end}")), "Cure end day is read from Sim.");
            }
            Require(issue.Outcome != CureOutcome.None && shell.PerformanceRows.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains($"outcome: {issue.Outcome}")), "Cure outcome stays visible when it arrives.");
            Require(shell.Session.StateHash == expected.StateHash(), "Cure progression matches Sim.");
            expected = Issue(); issue = expected.View.PerformanceIssues.First();
            result = expected.Apply(new TerminateAction { Day = expected.Day, Arrangement = issue.ArrangementId });
            Require(result.Accepted, "Termination fixture is accepted.");
            shell.PerformanceChoices[(issue.Id, "terminate")].EmitSignal(Button.SignalName.Pressed);
            shell.TerminateButtons[issue.ArrangementId].EmitSignal(Button.SignalName.Pressed);
            shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.PerformanceChoices[(issue.Id, "terminate")].Disabled, "Terminate sends the exact action and prevents duplicate work.");
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(result.Message), "Terminate ActionResult is visible.");
            while (!shell.Session.View!.Finished) Step(shell);
            Require(shell.PerformanceChoices.Values.All(b => b.Disabled), "Finished-game performance controls are disabled.");
            Scenario(shell, "spoke-b"); Require(shell.PerformanceChoices.Count == 0, "Scenario reset clears performance choices.");
            GD.Print("PERFORMANCE_SMOKE_PASS: persistent issue, pure re-source navigation, cure timing/outcome and exact termination."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PERFORMANCE_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
