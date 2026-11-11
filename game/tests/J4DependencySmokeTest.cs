using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class J4DependencySmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell = scene.Instantiate<GameShell>();
            shell.AutosavePathOverride = Path.Combine(Path.GetTempPath(), "ocs-j4-dependency-" + Guid.NewGuid() + ".json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed); DeployWithoutPrep(shell);
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            var panel = tabs.GetNode<VBoxContainer>("Requirements/RequirementContext/Contents");
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var expected = Session.Create(bundle, 1);
            Button? Shortcut() => panel.GetChildren().OfType<Button>().SingleOrDefault(b => b.HasMeta("dependency_visit"));
            void CheckOwner(bool engaged)
            {
                var dependency = expected.View.RequirementEditor("req0007")!.Dependencies.Single(d => d.CapabilityId == "fuel");
                var label = panel.GetChildren().OfType<Label>().Single(l => l.HasMeta("dependency_owner"));
                Require(dependency.OwnerEngaged == engaged && label.GetMeta("owner_engaged").AsBool() == engaged
                    && label.Text.Contains(dependency.UsualOwner!), "Checklist binds the authored usual owner and the Sim's engaged state.");
                Require(label.Text.Contains(engaged ? "owner is engaged" : "owner has not been engaged"), "The owner's status is readable in prose.");
            }
            CheckOwner(false); Require(Shortcut() is { Disabled: false }, "Uncovered fuel offers the J4 from its checklist.");
            tabs.CurrentTab = 1; await Screenshot(shell, "--checklist-screenshot", (ScrollContainer)panel.GetParent(), Shortcut()!);
            // A prior board request for another site/quantity must not carry into this requirement's request.
            shell.PeopleToggle.ButtonPressed = true; Pick(shell.PersonContactInput, "j4");
            Pick(shell.J4SiteInput!, "hubSite"); shell.J4QuantityInput!.Value = 1000000;
            var hash = shell.Session.StateHash;
            Shortcut()!.EmitSignal(Button.SignalName.Pressed);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var action = (EngageJ4Action)shell.VisitOptions["j4"].Action;
            var preview = shell.VisitOptions["j4"].Preview;
            var peak = expected.View.ActionEditor("spokeBSite", "fuel")!.PeakDemandUnits;
            Require(tabs.CurrentTab == 0 && shell.PeopleToggle.ButtonPressed && action.Site == "spokeBSite"
                && action.Capability == "fuel" && action.Quantity == peak, "Shortcut opens J4 for this site's fuel with its exact peak daily demand.");
            Require(shell.J4QuantityInput!.GetLineEdit().HasFocus(), "Keyboard focus follows the player to the request quantity.");
            var realPreview = expected.View.PreviewVisit(action);
            Require(hash == shell.Session.StateHash && preview == realPreview with
                { HardStops = preview.HardStops, SoftGaps = preview.SoftGaps }
                && preview.HardStops.SequenceEqual(realPreview.HardStops) && preview.SoftGaps.SequenceEqual(realPreview.SoftGaps),
                "Navigation and preview change no state; hours, timing, stock limits and caveats bind exactly to Sim.");
            await Screenshot(shell, "--ask-screenshot", tabs.GetNode<ScrollContainer>("Board/MorningBoard"), shell.VisitButtons["j4"].GetParent().GetChildren().OfType<Label>().Last());
            Require(expected.Apply(action).Accepted, "The default request is within actual partner stock.");
            shell.VisitButtons["j4"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.GetNode<Label>("%BuildStatus").Text == "in work", "Ask submits the exact previewed action and shows its ActionResult.");
            CheckOwner(true); Require(Shortcut() is null, "Pending J4 work counts as engaged and removes the duplicate checklist shortcut.");
            while (!expected.Arrangements.Any(a => a.Id == action.Id))
            {
                Step(shell); expected.AdvanceDay(); Require(shell.Session.StateHash == expected.StateHash(), "Request completes in the real engine.");
            }
            CheckOwner(true); Require(Shortcut() is null && expected.View.RequirementEditor("req0007")!.Dependencies.Single(d => d.CapabilityId == "fuel").Coverage == "arranged", "Completed ACSA support remains engaged and covered.");
            Scenario(shell, "spoke-b"); expected = Session.Create(bundle, 1);
            // An older save may already include fuel; exercise that supported state without restoring removed UI.
            Require(shell.SendAction(new SetLineAction { Card = "req0007", Line = "fuelSupply", On = true }).Accepted,
                "Legacy included-fuel fixture is accepted.");
            Require(Shortcut() is null, "Fuel included in the requirement has no uncovered-line shortcut.");
            Scenario(shell, "tutorial");
            Require(shell.Session.View!.Scenario.Actors.AcsaPartner is null && Shortcut() is null, "No ACSA contact is invented in a scenario without a partner.");
            Scenario(shell, "spoke-b"); while (!shell.Session.View!.Finished) Step(shell);
            Require(Shortcut() is null or { Disabled: true }, "A finished game cannot submit from the checklist.");
            shell.QueueFree(); shell = null;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("J4_DEPENDENCY_SMOKE_PASS: exact owner and engaged state, uncovered fuel shortcut, site/peak defaults, pure preview, focus, exact request/results and pending/completed coverage."); GetTree().Quit();
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError($"J4_DEPENDENCY_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }

    private async Task Screenshot(GameShell shell, string flag, ScrollContainer scroll, Control target)
    {
        var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, flag); if (at < 0) return;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        scroll.EnsureControlVisible(target);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Require(GetViewport().GetTexture().GetImage().SavePng(args[at + 1]) == Error.Ok, "Screenshot saved.");
    }
}
