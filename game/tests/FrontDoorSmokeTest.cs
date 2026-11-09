using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class FrontDoorSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-front-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json"); AddChild(shell);
            Require(shell.AtFrontDoor && !shell.DeveloperToggle.ButtonPressed && shell.Session.View is null, "Cold launch is title, developer tools off, no active session.");
            Require(shell.ContinueGameButton.Disabled, "Continue is unavailable without an autosave.");
            Require(!shell.GetNode<OptionButton>("%ScenarioPicker").IsVisibleInTree() && !shell.LoadSaveButton.IsVisibleInTree(), "No file or scenario controls on the title.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1] + suffix + ".png") == Error.Ok, "Screenshot saved.");
            }
            await Capture("-title");
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.SpokeBGameButton.Disabled, "Named Spoke B is offered without a file picker.");
            shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.AtFrontDoor && shell.Session.Prep is not null && shell.Session.View is null, "New game opens Spoke B preparations before play.");
            Require(!shell.GetNode<OptionButton>("%ScenarioPicker").IsVisibleInTree() && !shell.GetNode<Button>("%ReplayButton").IsVisibleInTree() && !shell.LoadSaveButton.IsVisibleInTree(), "Test controls remain hidden during normal play.");
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed); Step(shell);
            var hash = shell.Session.StateHash;
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            Require(!shell.ContinueGameButton.Disabled, "A deployment saved through normal play enables Continue.");
            shell.ContinueGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.AtFrontDoor && shell.Session.StateHash == hash && !shell.Session.IsReplay, "Continue resumes the exact deployment without future replay actions.");
            shell.DeveloperToggle.ButtonPressed = true;
            Require(shell.GetNode<OptionButton>("%ScenarioPicker").IsVisibleInTree() && shell.LoadSaveButton.IsVisibleInTree(), "Explicit developer opt-in reveals file/test controls.");
            await Capture("-developer");
            shell.LoadSaveButton.EmitSignal(Button.SignalName.Pressed);
            shell.DeveloperToggle.ButtonPressed = false;
            Require(!shell.SaveFilePicker.Visible && !shell.LoadSaveButton.IsVisibleInTree(), "Turning developer tools off closes the file picker too.");
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            File.WriteAllText(shell.AutosavePath, "{");
            shell.ContinueGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.AtFrontDoor && shell.Session.StateHash == hash, "Malformed saved deployment is refused without losing the open session.");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("FRONT_DOOR_SMOKE_PASS: cold title, named new game, exact Continue, developer-only controls, opt-out and invalid save."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"FRONT_DOOR_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true); }
    }
}
