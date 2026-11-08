using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class RequirementSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var expected = Session.Create(bundle, 1);
            var before = expected.View.RequirementEditor("req0007")!;
            Require(shell.SelectedCard == "req0007" && !shell.RequirementToggles["fuelSupply"].ButtonPressed, "The authored card and missing fuel line are shown.");
            var panel = shell.GetNode<TabContainer>("%DeskTabs").GetNode("Requirements/RequirementContext/Contents");
            var gap = before.Lines.Single(l => l.Id == "fuelSupply").GapIfOff!;
            Require(panel.GetChildren().OfType<Label>().Any(l => l.Text == gap), "The exact off-line consequence is visible.");
            var action = new SetLineAction { Day = 1, Card = "req0007", Line = "fuelSupply", On = true };
            Require(expected.Apply(action).Accepted, "The fixture accepts the line edit.");
            shell.RequirementToggles["fuelSupply"].EmitSignal(BaseButton.SignalName.Toggled, true);
            Require(shell.Session.StateHash == expected.StateHash() && shell.RequirementToggles["fuelSupply"].ButtonPressed, "Toggle sends SetLineAction without advancing a day.");
            Require(panel.GetChildren().OfType<Label>().Any(l => l.HasMeta("dependency") && l.GetMeta("dependency").AsString() == "fuel" && l.Text.Contains("in requirement")), "Dependency coverage refreshes from Sim.");
            expected.Apply(action with { On = false });
            shell.RequirementToggles["fuelSupply"].EmitSignal(BaseButton.SignalName.Toggled, false);
            Require(shell.Session.StateHash == expected.StateHash() && panel.GetChildren().OfType<Label>().Any(l => l.Text == gap), "Turning the line off restores the authored warning.");
            Require(panel.GetChildren().OfType<Label>().Any(l => l.Text.Contains($"Peak demand: {before.PeakDemandUnits} {before.Unit}")), "Peak demand and units are shown unchanged.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 1;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Screenshot saved.");
            }
            GD.Print("REQUIREMENT_SMOKE_PASS: submitted cards, line actions, exact gaps and dependency refresh.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"REQUIREMENT_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
