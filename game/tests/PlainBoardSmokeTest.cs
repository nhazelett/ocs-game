using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PlainBoardSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            await PlainBoardSmokeChecks.Run(shell, this);
            shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("PLAIN_BOARD_SMOKE_PASS: Today and Inbox use plain category names on authored news.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PLAIN_BOARD_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
