using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game.Tests;

public partial class PlainBoardSmokeTest : Node
{
    [Export] public bool VendorNews { get; set; }

    public override async void _Ready()
    {
        var marker = VendorNews ? "VENDOR_NEWS" : "PLAIN_BOARD";
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var packed = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = packed.Instantiate<GameShell>();
            shell.AutosavePathOverride = Path.Combine(Path.GetTempPath(), "ocs-plain-news-" + Guid.NewGuid() + ".json");
            AddChild(shell); // The first fixture loads directly; there is no unrelated deployment to set up.
            await PlainBoardSmokeChecks.Run(shell, this, VendorNews ? BoardCategory.VendorNotice : BoardCategory.HigherHq);
            shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print($"{marker}_SMOKE_PASS: Today and Inbox use plain category names on authored news. {elapsed.Elapsed.TotalSeconds:0.0} seconds.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"{marker}_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
