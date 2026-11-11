using Godot;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class SupportSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this); Scenario(shell, "sustainment");
            while (!shell.Session.View!.Finished) Step(shell);
            var hash = shell.Session.StateHash;
            var gap = shell.DisplayedDebrief!.Gaps.First(g => g.Factors.Any(f => f.Kind == GapFactorKind.AcsaWithdrawn));
            var button = shell.DebriefPanel.GetChildren().OfType<Button>()
                .Single(b => b.HasMeta("gap") && b.GetMeta("gap").AsInt32() == gap.Index);
            var detail = shell.DebriefPanel.GetChildren().OfType<VBoxContainer>().Single(v => v.HasMeta("gap_detail") && v.GetMeta("gap_detail").AsInt32() == gap.Index);
            if (!detail.Visible) button.EmitSignal(Button.SignalName.Pressed);
            var evidence = shell.DebriefPanel.FindChildren("*", "Label", true, false).OfType<Label>()
                .Single(l => l.Text.StartsWith(DebriefWords.Factor(GapFactorKind.AcsaWithdrawn)));
            Require(evidence.Text.Contains(gap.Factors.Single(f => f.Kind == GapFactorKind.AcsaWithdrawn).Text),
                "Readable support label preserves actual Sim evidence.");
            shell.DeveloperToggle.ButtonPressed = false;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            ((ScrollContainer)shell.DebriefPanel.GetParent()).EnsureControlVisible(evidence);
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Screenshot saved.");
            }
            Require(shell.Session.StateHash == hash, "Opening actual support-withdrawal evidence leaves Sim unchanged.");
            // Release the full shell and finalize managed resources while Godot is still running.
            // CI previously crashed during native shutdown after this suite's pass marker.
            shell.QueueFree(); shell = null!;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(!GetChildren().OfType<GameShell>().Any(), "The full shell is gone before process shutdown.");
            GD.Print("SUPPORT_SMOKE_PASS: actual ACSA withdrawal label, unchanged evidence and read-only debrief."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"SUPPORT_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
