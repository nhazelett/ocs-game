using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class EndDaySmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            shell.DeveloperToggle.ButtonPressed = false;
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
            Require(!shell.GetNode<Label>("%BuildStatus").Visible, "Developer diagnostics stay off the player desk.");
            var end = shell.GetNode<Button>("%NextDay");
            var skip = shell.GetNode<Button>("%Continue");
            Require(end.Text == "End the day", "The day control says what it does.");
            Require(skip.Text == "Skip quiet days" && !skip.IsVisibleInTree()
                && skip.GetParent() != end.GetParent(), "Quiet-day skipping is absent from the desk strip.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                for (var i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Screenshot saved.");
            }
            await Capture("desk");
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            expected.AdvanceDay();
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.CompletedDay == 1 && shell.Session.StateHash == expected.StateHash(), "End the day advances exactly one real day.");
            var hash = shell.Session.StateHash;
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash, "Playback prevents a second hidden advance.");
            shell.FinishDayPlayback();
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            Require(skip.IsVisibleInTree() && !skip.Disabled, "The explicit skip command is available from Menu during a deployment.");
            Require(skip.Alignment == shell.NewGameButton.Alignment
                && shell.ContinueGameButton.Text == "Continue saved game", "Menu commands align consistently and resuming names the saved game.");
            await Capture("menu");
            Require(skip.GetGlobalRect().End.Y <= 800, "The Menu shortcut stays on screen at 1280 by 800.");
            skip.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.AtFrontDoor && shell.Session.CompletedDay > 1, "Menu skipping returns to the live deployment.");
            shell.FinishDayPlayback();

            // The tutorial's idle teammates and due water request are the cold-start check.
            Scenario(shell, "tutorial");
            var tutorial = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial")), 1);
            hash = shell.Session.StateHash;
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.EndDayDialog.Visible && shell.EndDayDialog.DialogText == string.Join("\n\n", tutorial.View.EndOfDayCheck())
                && shell.Session.StateHash == hash && shell.Session.CompletedDay == 0 && !shell.PlayingDay,
                "The exact Sim sentences appear before any state change, autosave or playback.");
            Require(shell.EndDayDialog.DialogText.Contains("Water for Hub") && shell.EndDayDialog.DialogText.Contains("MSgt Joan Avery")
                && shell.EndDayDialog.DialogText.Contains("SrA Eli Novak"), "The check names the due request and both free teammates.");
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash, "Repeated clicks cannot advance behind the check.");
            await Capture("check");
            Require(shell.EndDayDialog.GetCancelButton().HasFocus(), "Returning to the desk is the focused choice.");
            Require(shell.EndDayDialog.GetLabel().GetRect().End.X <= shell.EndDayDialog.Size.X,
                "The wrapped check fits its dialog.");
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            Require(!shell.EndDayDialog.Visible && shell.Session.StateHash == hash, "Cancel closes the check without ending the day.");
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.StateHash == hash, "A canceled confirmation cannot act later.");
            end.EmitSignal(Button.SignalName.Pressed);
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed); tutorial.AdvanceDay();
            Require(shell.Session.StateHash == tutorial.StateHash() && shell.Session.CompletedDay == 1 && shell.PlayingDay
                && !shell.EndDayDialog.Visible, "Confirm advances exactly one independently simulated day and starts its playback.");
            hash = shell.Session.StateHash;
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.StateHash == hash, "A duplicate confirmation cannot advance again.");
            shell.FinishDayPlayback();

            end.EmitSignal(Button.SignalName.Pressed);
            Pick(shell.PersonContactInput, "finance"); shell.VisitButtons["finance"].EmitSignal(Button.SignalName.Pressed);
            hash = shell.Session.StateHash;
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(!shell.EndDayDialog.Visible && shell.Session.StateHash == hash && shell.Session.CompletedDay == 1,
                "Changing the day's work invalidates its old check.");
            end.EmitSignal(Button.SignalName.Pressed);
            Scenario(shell, "spoke-b"); hash = shell.Session.StateHash;
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(!shell.EndDayDialog.Visible && shell.Session.StateHash == hash && shell.Session.CompletedDay == 0,
                "Starting another deployment invalidates the old confirmation.");
            Step(shell, 5); hash = shell.Session.StateHash;
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.EndDayDialog.Visible && shell.EndDayDialog.DialogText == string.Join("\n\n", shell.Session.View!.EndOfDayCheck())
                && shell.EndDayDialog.DialogText.Contains("was wanted from day 5"), "An overdue request retains the exact late wording.");
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(!shell.EndDayDialog.Visible && shell.AtFrontDoor && shell.Session.StateHash == hash,
                "Leaving the desk invalidates the check without advancing.");
            shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("END_DAY_SMOKE_PASS: exact Sim check, named free teammates, overdue request, cancel-first focus, stale/duplicate guards, one-day hash, playback and Menu skip.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("END_DAY_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
