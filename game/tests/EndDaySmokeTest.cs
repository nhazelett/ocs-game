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
            shell.FinishDayPlayback(); shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("END_DAY_SMOKE_PASS: one-day control, playback guard, explicit Menu skip, native layout.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("END_DAY_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
