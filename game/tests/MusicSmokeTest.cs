using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class MusicSmokeTest : Node
{
    public override async void _Ready()
    {
        var folder=Path.Combine(Path.GetTempPath(),"ocs-music-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        GameShell? shell=null;
        try
        {
            using var scene=GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell=scene.Instantiate<GameShell>();shell.AutosavePathOverride=Path.Combine(folder,"save.json");
            shell.AudioSettingsPathOverride=Path.Combine(folder,"sound.cfg");shell.MusicSettingsPathOverride=Path.Combine(folder,"music.cfg");
            shell.LookSettingsPathOverride=Path.Combine(folder,"look.cfg");AddChild(shell);
            var music=shell.Music;
            Require(music.LoadedStreamCount==4 && music.CurrentTrack=="title" && music.MusicPlayer.Playing,"Front door really loads and plays the approved title track.");
            Require(!music.UsesNativeTrackLoops,"The faded desk tracks are never native loops.");
            music.MusicPlayer.EmitSignal(AudioStreamPlayer.SignalName.Finished);music.Tick(10);
            Require(!music.MusicPlayer.Playing,"The resolved title ending can remain silent.");
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);DeployWithoutPrep(shell);
            var hash=shell.Session.StateHash;shell._Process(1);music.Tick(1);music.Tick(2);
            Require(music.CurrentTrack=="desk-morning" && music.MusicPlayer.Playing,"The real desk changes to the first approved bed.");
            Require(music.HarborPlayers.Any(p=>p.Playing && p.VolumeLinear>0),"The visible map has quiet harbor ambience.");
            var current=music.HarborPlayers.First(p=>p.Playing);
            current.Seek((float)(music.HarborLength-.4));music.Tick(.1);
            Require(music.HarborCrossfading && music.HarborPlayers.All(p=>p.Playing),"Harbor overlaps two real playbacks across its seam.");
            music.Tick(.7);
            Require(music.HarborPlayers.All(p=>float.IsFinite(p.VolumeLinear) && p.VolumeLinear>=0), "Crossfade gains stay finite and nonnegative at the endpoint.");
            Require(!music.HarborCrossfading && music.HarborPlayers.Count(p=>p.Playing)==1,"After the seam only the next harbor playback remains.");
            music.MusicPlayer.EmitSignal(AudioStreamPlayer.SignalName.Finished);music.Tick(1);
            Require(!music.MusicPlayer.Playing && music.QuietGap>0,"Desk tracks have a real quiet gap.");
            music.Tick(2.1);Require(music.CurrentTrack=="desk-long-day","The second bed follows the first.");
            music.MusicPlayer.EmitSignal(AudioStreamPlayer.SignalName.Finished);music.Tick(3.1);
            Require(music.CurrentTrack=="desk-morning","The playlist alternates back after the next quiet gap.");
            shell.SoundMute.ButtonPressed=true;shell.SoundMute.ButtonPressed=false;
            music.Tick(2);var before=music.MusicPlayer.VolumeLinear;
            shell.Sound.PlayCue("award-made");Require(shell.Sound.AnyCuePlaying,"The real event stream is playing.");
            music.Tick(.1);Require(music.DuckFactor<1 && music.MusicPlayer.VolumeLinear<before,"Real cue playback lowers the music.");
            shell.SoundMute.ButtonPressed=true;music.Tick(1);
            Require(music.DuckFactor==1 && music.MusicPlayer.Playing,"The mix recovers; muting sound does not mute music.");
            shell.SoundMute.ButtonPressed=false;shell.Sound.PlayCue("new-day");
            shell.MusicVolume.Value=42;shell.MusicMute.ButtonPressed=true;
            Require(music.Muted && !music.MusicPlayer.Playing && music.HarborPlayers.All(p=>!p.Playing)
                && shell.Sound.AnyCuePlaying,"Music mute stops all music/ambience without stopping event cues.");
            var mutedCopy=new GameMusic();AddChild(mutedCopy);mutedCopy.Initialize(shell.MusicSettingsPathOverride!,Path.Combine(GamePaths.AssetRoot,"audio","music"),()=>false);
            Require(mutedCopy.Muted && Math.Abs(mutedCopy.Volume-.42f)<.001 && !mutedCopy.MusicPlayer.Playing,"A remembered music mute prevents startup playback.");
            mutedCopy.Free();
            shell.MusicMute.ButtonPressed=false;shell.MusicVolume.Value=0;music.Tick(1);
            Require(!music.MusicPlayer.Playing && music.HarborPlayers.All(p=>!p.Playing),"Zero music volume is silent.");
            shell.MusicVolume.Value=42;music.Tick(2);
            var restored=new GameMusic();AddChild(restored);restored.Initialize(shell.MusicSettingsPathOverride,Path.Combine(GamePaths.AssetRoot,"audio","music"),()=>false);
            Require(!restored.Muted && Math.Abs(restored.Volume-.42f)<.001,"Music volume and mute restore independently from disk.");
            restored.Free();
            var empty=Path.Combine(folder,"empty");Directory.CreateDirectory(empty);
            var missing=new GameMusic();AddChild(missing);missing.Initialize(Path.Combine(folder,"empty.cfg"),empty,()=>false);missing.SetContext(false,true);missing.Tick(60);
            Require(missing.LoadedStreamCount==0 && !missing.MusicPlayer.Playing && missing.HarborPlayers.All(p=>!p.Playing),"Missing music files simply remain silent.");
            missing.Free();
            Require(shell.Session.StateHash==hash,"Playback, playlist, mix and settings never mutate the Sim.");
            shell.OpenContractView(shell.Session.View!.Arrangements.First().Id);shell._Process(1);music.Tick(2);
            Require(music.HarborPlayers.All(p=>!p.Playing),"A contract detail covering the map fades out harbor ambience.");
            shell.CloseContractView();shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);shell._Process(1);music.Tick(2);music.Tick(2);
            Require(music.CurrentTrack=="title" && music.HarborPlayers.All(p=>!p.Playing),"Menu returns to title music and no harbor.");
            shell.SoundButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.SoundMute.HasFocus(),"Combined settings keep keyboard entry focus.");
            for(var i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Require(shell.MusicVolume.GetThemeStylebox("slider").GetMinimumSize().Y>=8, "Volume sliders have a visible track as well as a knob.");
            Require(shell.MusicVolume.GetGlobalRect().End.Y<800,"Both independent sliders fit the settings screen.");
            var args=OS.GetCmdlineUserArgs();var shot=Array.IndexOf(args,"--screenshot");
            if(shot>=0)
            {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=GetViewport().GetTexture().GetImage();Require(image.SavePng(args[shot+1])==Error.Ok,"Native settings screenshot saved.");
            }
            shell.Free();shell=null;await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("MUSIC_SMOKE_PASS: real Ogg playback, title/desk contexts, alternating faded playlist and gaps, harbor crossfade/visibility, actual-cue ducking, independent mute/zero, restart settings and missing-file silence.");
            GetTree().Quit();
        }
        catch(Exception error){GD.PushError("MUSIC_SMOKE_FAIL: "+error);GetTree().Quit(1);}
        finally{shell?.QueueFree();Directory.Delete(folder,true);}
    }
}
