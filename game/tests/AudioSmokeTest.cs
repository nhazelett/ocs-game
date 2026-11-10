using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class AudioSmokeTest : Node
{
    public override async void _Ready()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ocs-audio-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        GameShell? shell = null;
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell = scene.Instantiate<GameShell>();
            shell.AutosavePathOverride = Path.Combine(directory, "save.json");
            shell.AudioSettingsPathOverride = Path.Combine(directory, "sound.cfg");
            AddChild(shell);
            Require(shell.Sound.LoadedCueCount == 4 && shell.Sound.PlayedCount == 0, "Title loads four real streams without announcing a deployment.");
            shell.SoundButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.SoundPanel.Visible && shell.SoundMute.HasFocus(), "Sound controls are reachable from the title with keyboard focus.");
            shell.SoundVolume.Value = 37; shell.SoundMute.ButtonPressed = true;
            Require(shell.Sound.Muted && Math.Abs(shell.Sound.Volume - .37f) < .001, "Actual controls set volume and mute.");
            var count = shell.Sound.PlayedCount; shell.Sound.PlayCue("new-day");
            Require(shell.Sound.PlayedCount == count, "Mute prevents sound playback.");
            var restored = new GameAudio(); AddChild(restored); restored.Initialize(shell.AudioSettingsPathOverride);
            Require(restored.Muted && Math.Abs(restored.Volume - .37f) < .001, "A new audio instance restores both settings from disk.");
            restored.Free(); shell.SoundMute.ButtonPressed = false; shell.SoundVolume.Value = 0;
            shell.Sound.PlayCue("new-day"); Require(shell.Sound.PlayedCount == count, "Zero volume is silent too.");
            shell.SoundVolume.Value = 37;
            shell.SoundPanel.Hide(); DeveloperWorkspace(shell); Scenario(shell, "spoke-b");
            var hash = shell.Session.StateHash; count = shell.Sound.PlayedCount;
            foreach (var cue in GameAudio.CueNames) shell.Sound.PlayCue(cue);
            Require(shell.Sound.PlayedCount == count + 4 && shell.Session.StateHash == hash, "Every file really plays; presentation leaves Sim unchanged.");
            Pick(shell.VendorInput, "vendorC"); Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "bosBpa"); shell.QuantityInput.Value = 360;
            count = shell.Sound.PlayedCount;
            Require(!shell.SendAction(shell.PendingAward! with { Quantity = 100000 }).Accepted && shell.Sound.PlayedCount == count, "A refused award adds no cue.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Sound.PlayedCount == count + 1 && shell.Sound.LastCue == "award-made", "Accepted real Award alone triggers its cue.");
            Replay(shell); count = shell.Sound.PlayedCount;
            Step(shell); Require(shell.Sound.PlayedCount > count, "Advancing a real day announces it.");
            var after = shell.Sound.PlayedCount; var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            for (var i = 0; i < tabs.GetTabCount(); i++) tabs.CurrentTab = i;
            Require(shell.Sound.PlayedCount == after, "Browsing and redraws do not repeat event cues.");
            Step(shell, 9);
            Require(shell.Session.View!.MapDay().Changes.Any(c => c.Known && c.Kind == "shortfall"), "Day ten exercises known shortfall projection.");
            Require(shell.Sound.LastCue is "support-down" or "at-the-door", "Actual shortfall emits its cue, possibly followed by its messenger.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
                shell.SoundButton.EmitSignal(Button.SignalName.Pressed);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1]) == Error.Ok, "Sound controls screenshot saved.");
            }
            shell.Free(); shell = null;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("AUDIO_SMOKE_PASS: actual WAV playback, accepted-event cues, silent mute/zero, disk persistence, keyboard controls and unchanged Sim.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("AUDIO_SMOKE_FAIL: " + error); GetTree().Quit(1); }
        finally { shell?.QueueFree(); Directory.Delete(directory, true); }
    }
}
