using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class OtherActionsSmokeTest : Node
{
    public override async void _Ready()
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
            var view = shell.Session.View!;
            var demand = view.ActionEditor("spokeBSite", "fuel")!.PeakDemandUnits;
            Require(demand == 1170 && shell.PendingCoordination!.Quantity == demand && shell.CoordinationQuantityInput!.Value == demand,
                "Spoke B fuel coordination starts at its Sim-owned peak demand, 1,170 gallons per day.");
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            shell.DeveloperToggle.ButtonPressed = false; await Frames();
            var pane = shell.GetNode<Control>("%DeskTabs/Today/TodayPane");
            var body = pane.GetNode<Control>("Body");
            var quantity = shell.CoordinationQuantityInput!;
            var source = shell.CoordinationSourceInput!;
            Require(Math.Abs(quantity.GlobalPosition.Y - source.GlobalPosition.Y) < 1 && quantity.GlobalPosition.X > source.GlobalPosition.X,
                "From site and Quantity share one row.");
            Require(body.Size.Y >= pane.Size.Y / 2 && pane.GetGlobalRect().Encloses(quantity.GetGlobalRect()),
                "Today retains at least half the pane and both coordination fields fit.");
            async Task Capture(string suffix)
            {
                if (shot < 0) return;
                await Frames(); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native review capture saved.");
            }
            var cost = shell.CoordinateButton!.GetParent().GetChildren().OfType<Label>().Single(l => l.HasMeta("action_cost"));
            Require(cost.Text.Contains("Requests 1,170 gal a day"), "The actual coordination sentence groups thousands.");
            quantity.Value = 3600;
            Require(cost.Text.Contains("Requests 3,600 gal a day") && shell.PendingCoordination!.Quantity == 3600, "Formatting the changed quantity never changes the action amount.");
            quantity.Value = demand;
            await Capture("today");
            shell.ShowRequests(); await Frames(); await Capture("requests");
            var hash = shell.Session.StateHash;
            quantity.Value = 800;
            Require(shell.PendingCoordination!.Quantity == 800 && shell.Session.StateHash == hash, "Editing quantity updates only the pending action.");
            shell.NavigateBoardItem(new BoardItem("test", 0, BoardCategory.Report, BoardSeverity.Info, BoardLinkKind.SiteCapability, "spokeBSite.fuel", "", []));
            Require(shell.PendingCoordination!.Quantity == 800, "Reopening the same capability keeps the player's quantity.");
            shell.NavigateBoardItem(new BoardItem("test", 0, BoardCategory.Report, BoardSeverity.Info, BoardLinkKind.SiteCapability, "hubSite.fuel", "", []));
            Require(shell.PendingCoordination!.Quantity == view.ActionEditor("hubSite", "fuel")!.PeakDemandUnits, "Another capability starts at its own need.");
            shell.NavigateBoardItem(new BoardItem("test", 0, BoardCategory.Report, BoardSeverity.Info, BoardLinkKind.SiteCapability, "spokeBSite.fuel", "", []));
            Require(shell.PendingCoordination!.Quantity == demand && shell.Session.StateHash == hash, "Returning to fuel resets its need without spending resources.");
            shell.DeveloperToggle.ButtonPressed = true;
            result = expected.Apply(shell.PendingCoordination!); Require(result.Accepted, "The fixture accepts coordination.");
            shell.CoordinateButton!.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.GetNode<Label>("%BuildStatus").Text.Contains(expected.View.Decisions().First().Sentence), "Coordination and its result match Sim.");

            Scenario(shell, "spoke-b"); Replay(shell); Step(shell, 8);
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json"));
            expected = LinesOfSupport.Sim.Replay.Run(bundle, log, 8);
            // DESK-01: host nation and ACSA support are not this office's to end; the fixture terminates its own contract.
            var running = expected.Arrangements.First(a => !a.IsCoordination && a.Status == ArrangementStatus.Active && expected.View.TerminateBlocked(a.Id) is null);
            result = expected.Apply(new TerminateAction { Day = expected.Day, Arrangement = running.Id }); Require(result.Accepted, "The fixture can terminate the contract.");
            shell.OpenContractView(running.Id);
            shell.TerminateButtons[running.Id].EmitSignal(Button.SignalName.Pressed);
            shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
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
            GD.Print("OTHER_ACTIONS_SMOKE_PASS: survey, peak-demand coordination, side-by-side fields and half-pane layout, termination and proposal-gated definitization."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"OTHER_ACTIONS_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
