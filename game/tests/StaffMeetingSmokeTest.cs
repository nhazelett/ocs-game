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
                    && labels.Single(l => l.HasMeta("meeting_cost")).Text == shell.Session.View!.StaffMeeting.CostSays,
                    "The real rules' joining and daily costs appear once, with singular hours.");
                Require(!labels.Any(l => l.Text.Contains("This takes") || l.Text.Contains("already getting")
                    || l.Text.Contains("0 shop hours")), "No duplicate refused preview or zero-hour quote is shown.");
                Require(shell.VisitButtons.Keys.SequenceEqual(action is null ? Array.Empty<string>() : new[] { action }),
                    "Only the action appropriate to the current meeting state is reachable.");
                Require(shell.VisitOptions.Keys.SequenceEqual(shell.VisitButtons.Keys), "No stale action or preview survives a state change.");
                if (action == "meeting")
                {
                    var effect = shell.VisitButtons[action].GetParent().GetChildren().OfType<Label>().Single(l => l.HasMeta("action_cost"));
                    Require(effect.Text.Contains(shell.Session.View!.StaffMeeting.JoinSays, StringComparison.Ordinal)
                        && effect.Text.Contains("2 hours of your time", StringComparison.Ordinal),
                        "Joining keeps the exact Sim daily-cost effect and the separate enrollment hours beneath its button.");
                }
                Require(shell.Session.StateHash == expected.StateHash(), "Meeting presentation preserves exact Sim state.");
            }
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string state)
            {
                for (var i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var scroll = shell.GetNode<TabContainer>("%DeskTabs").GetNode<ScrollContainer>("Today/TodayPane/Body/Body");
                Control focus = state == "not-attending"
                    ? shell.VisitButtons["meeting"].GetParent().GetChildren().OfType<Label>().Single(l => l.HasMeta("action_cost"))
                    : shell.VisitButtons.TryGetValue("leave-meeting",out var leave) ? leave : Labels(shell.PersonContactInput.GetParent()).Single(l => l.HasMeta("meeting_cost"));
                scroll.EnsureControlVisible(focus);
                for (var i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                if(state=="novak-attending")
                {
                    Require(scroll.GetGlobalRect().Encloses(focus.GetGlobalRect()),"Stop attending is visible beside the actual attendee/cost.");
                    Require(scroll.GetGlobalRect().Encloses(Labels(shell.PersonContactInput.GetParent()).Single(l=>l.HasMeta("meeting_state")).GetGlobalRect()),"The attendee remains visible when the action is brought into view.");
                }
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
            // DESK-229: the actual eighth play gives the standing seat to Novak.
            var recorded = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot,"..","docs","reviews","plays","2026-11-22-8","input-log-2.json"));
            expected = LinesOfSupport.Sim.Replay.Run(bundle,recorded,stopAfterDay:14);
            var saved = Path.Combine(temporary,"day15.json"); File.WriteAllText(saved,expected.ToSave().ToJson());shell.LoadSavedGame(saved);
            shell.PeopleToggle.ButtonPressed=true;Pick(shell.PersonContactInput,"meeting");
            Check("attending","leave-meeting");
            var actualLabels=Labels(shell.PersonContactInput.GetParent()).ToArray();
            Require(actualLabels.Single(l=>l.HasMeta("meeting_state")).Text=="SrA Eli Novak is attending the staff meeting. The next is on day 15.","Committed attendance names Novak rather than the player.");
            Require(actualLabels.Single(l=>l.HasMeta("meeting_cost")).Text.Contains("1.5 hours of SrA Eli Novak's time"),"Actual standing cost matches the team bar.");
            await Capture("novak-attending");
            shell.VisitButtons["leave-meeting"].EmitSignal(Button.SignalName.Pressed);
            var selectionHash=shell.Session.StateHash;
            shell.VisitAssignees["meeting"].ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
            actualLabels=Labels(shell.PersonContactInput.GetParent()).ToArray();
            Require(actualLabels.Single(l=>l.HasMeta("meeting_cost")).Text==shell.Session.View!.StaffMeetingFor("novak").CostSays,"Changing the joiner refreshes the explanatory personal hours.");
            Require(Labels(shell.VisitButtons["meeting"].GetParent()).Any(l=>l.HasMeta("action_cost") && l.Text.Contains("Novak then spends 1.5 hours")),"The action's standing cost updates with its joining-hours quote.");
            Require(shell.Session.StateHash==selectionHash,"Changing the selected joiner is read-only.");
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
