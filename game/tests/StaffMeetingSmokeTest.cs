using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class StaffMeetingSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-meeting-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell = scene.Instantiate<GameShell>();
            shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json");
            shell.TeamSettingsPathOverride = Path.Combine(temporary, "team.cfg");
            shell.LookSettingsPathOverride = Path.Combine(temporary, "look.cfg");
            AddChild(shell); DeveloperWorkspace(shell); Scenario(shell, "tutorial");
            shell.DeveloperToggle.ButtonPressed = false;
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab;
            shell.PeopleToggle.ButtonPressed = true; Pick(shell.PersonContactInput, "meeting");
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var expected = LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave());
            var pane = shell.PersonContactInput.GetParent();
            IEnumerable<Label> Labels(Node node) => node.GetChildren().SelectMany(n =>
                (n is Label label ? new[] { label } : Enumerable.Empty<Label>()).Concat(Labels(n)));
            void Check(string state, string? action)
            {
                pane = shell.PersonContactInput.GetParent();
                var labels = Labels(pane).ToArray();
                Require(labels.Count(l => l.HasMeta("meeting_state")) == 1
                    && labels.Single(l => l.HasMeta("meeting_state")).GetMeta("meeting_state").AsString() == state,
                    "One exact meeting state is visible.");
                Require(labels.Count(l => l.HasMeta("meeting_cost")) == 1
                    && labels.Single(l => l.HasMeta("meeting_cost")).Text == "Joining takes 2 shop hours once. Each daily meeting takes 1 shop hour.",
                    "The real rules' joining and daily costs appear once, with singular hours.");
                Require(!labels.Any(l => l.Text.Contains("This takes") || l.Text.Contains("already getting")
                    || l.Text.Contains("0 shop hours")), "No duplicate refused preview or zero-hour quote is shown.");
                Require(shell.VisitButtons.Keys.SequenceEqual(action is null ? Array.Empty<string>() : new[] { action }),
                    "Only the action appropriate to the current meeting state is reachable.");
                Require(shell.VisitOptions.Keys.SequenceEqual(shell.VisitButtons.Keys), "No stale action or preview survives a state change.");
                Require(shell.Session.StateHash == expected.StateHash(), "Meeting presentation preserves exact Sim state.");
            }
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string state)
            {
                for (var i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var scroll = shell.GetNode<TabContainer>("%DeskTabs").GetNode<ScrollContainer>("Today/TodayPane/Body/Body");
                scroll.EnsureControlVisible(Labels(shell.PersonContactInput.GetParent()).Single(l => l.HasMeta("meeting_cost")));
                for (var i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + state + ".png") == Error.Ok, "Native meeting screenshot saved.");
            }
            Check("not-attending", "meeting"); await Capture("not-attending");
            var join = shell.VisitOptions["meeting"].Action;
            Require(expected.Apply(join).Accepted, "Joining is accepted by Sim.");
            shell.VisitButtons["meeting"].EmitSignal(Button.SignalName.Pressed);
            Check("joining", null); await Capture("joining");
            // Leaving and returning to the contact cannot resubmit the pending enrollment.
            Pick(shell.PersonContactInput, "finance"); Pick(shell.PersonContactInput, "meeting");
            Check("joining", null);
            while (!expected.View.StaffMeeting.Attending)
            {
                Require(expected.Day < 5, "Joining finishes within the fixture's actual queue.");
                Step(shell); expected.AdvanceDay();
            }
            Check("attending", "leave-meeting"); await Capture("attending");
            var stop = shell.VisitOptions["leave-meeting"].Action;
            Require(expected.Apply(stop).Accepted, "Stopping is accepted by Sim.");
            shell.VisitButtons["leave-meeting"].EmitSignal(Button.SignalName.Pressed);
            Check("not-attending", "meeting");
            shell.QueueFree(); shell = null;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GC.Collect(); GC.WaitForPendingFinalizers();
            GD.Print("STAFF_MEETING_SMOKE_PASS: all three states, one applicable action, one cost, pending navigation, exact join/stop and day-loop state.");
            GetTree().Quit();
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError("STAFF_MEETING_SMOKE_FAIL: " + error); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }
}
