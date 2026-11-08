using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class OtherActionsSmokeTest : Node
{
    public override void _Ready()
    {
        try
        {
            var shell = Shell(this);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var expected = Session.Create(bundle, 1);
            var result = expected.Apply(new SurveyAction { Day = 1, Island = "spokeB" });
            Require(result.Accepted && !shell.SurveyButtons["spokeB"].Disabled, "Survey is offered for the unsurveyed island.");
            shell.SurveyButtons["spokeB"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.SurveyButtons["spokeB"].Disabled, "Survey sends the exact action and marks it pending.");

            Scenario(shell, "spoke-b"); expected = Session.Create(bundle, 1);
            shell.NavigateBoardItem(new BoardItem("test", 0, BoardCategory.Report, BoardSeverity.Info, BoardLinkKind.SiteCapability, "spokeBSite.fuel", "", []));
            Require(shell.PendingCoordination is not null && shell.CoordinateButton is not null, "Selected capability offers support coordination.");
            result = expected.Apply(shell.PendingCoordination!); Require(result.Accepted, "The fixture accepts coordination.");
            shell.CoordinateButton!.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.GetNode<Label>("%BuildStatus").Text.Contains(result.Message), "Coordination and its result match Sim.");

            Scenario(shell, "spoke-b"); Replay(shell); Step(shell, 8);
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json"));
            expected = LinesOfSupport.Sim.Replay.Run(bundle, log, 8);
            var running = expected.Arrangements.First(a => !a.IsCoordination && a.Status == ArrangementStatus.Active);
            result = expected.Apply(new TerminateAction { Day = expected.Day, Arrangement = running.Id }); Require(result.Accepted, "The fixture can terminate the contract.");
            shell.TerminateButtons[running.Id].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.TerminateButtons[running.Id].Disabled, "Termination queues exactly once and shows its result.");

            Scenario(shell, "spoke-b-90"); Replay(shell, "approach3b uca never definitized");
            while (!shell.Session.View!.Finished && shell.DefinitizeButtons.Count == 0) Step(shell);
            var id = shell.DefinitizeButtons.Keys.Single();
            var longBundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b-90"));
            log = InputLog.Load(Path.Combine(longBundle.Directory, "golden", "approach3b-uca-never-definitized.json"));
            expected = LinesOfSupport.Sim.Replay.Run(longBundle, log, shell.Session.CompletedDay);
            var line = expected.View.ActionEditor("spokeBSite", "power")!.Funding.First(f => f.UsableToday).Id;
            result = expected.Apply(new DefinitizeAction { Day = expected.Day, Arrangement = id, FundingLine = line }); Require(result.Accepted, "The proposal can be definitized.");
            shell.DefinitizeButtons[id].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && !shell.DefinitizeButtons.ContainsKey(id), "Definitization uses the chosen funding and no duplicate command is offered.");
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(result.Message), "Definitization result is visible.");
            GD.Print("OTHER_ACTIONS_SMOKE_PASS: survey, coordination, termination and proposal-gated definitization."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"OTHER_ACTIONS_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
