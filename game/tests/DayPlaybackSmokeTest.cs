using Godot;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DayPlaybackSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell=Shell(this);Replay(shell);Step(shell,9);
            var map=shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            var next=shell.GetNode<Button>("%NextDay");next.EmitSignal(Button.SignalName.Pressed);
            Require(shell.PlayingDay && shell.PlaybackFacts!.Day==10 && next.Disabled,"Next day automatically plays exactly the completed day and holds further input.");
            var hash=shell.Session.StateHash;
            next.EmitSignal(Button.SignalName.Pressed);shell.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            Require(hash==shell.Session.StateHash,"Day/Continue cannot advance behind playback.");
            Require(map.Tabletop!.ReportedChangeLabels.Count==shell.PlaybackFacts!.Changes.Where(c=>c.Known).Select(c=>c.SiteId).Distinct().Count(),"Every changed site has a visible marker for its reported changes.");
            Require(new[]{"fuel","power","lodging"}.All(shell.PlaybackText.Text.Contains) && !shell.PlaybackText.Text.Contains("more in"),"Playback names all three reported shortfalls rather than hiding two behind a count.");
            await Capture("shortfall");
            shell.SkipPlaybackButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.PlayingDay && !next.Disabled && hash==shell.Session.StateHash,"Skip finishes only the presentation and restores controls.");
            var sawRun=false;var sawClosure=false;
            for(var i=0;i<15;i++)
            {
                next.EmitSignal(Button.SignalName.Pressed);hash=shell.Session.StateHash;
                var facts=shell.Session.View!.MapDay();
                Require(shell.PlaybackFacts!.Day==facts.Day && shell.PlaybackFacts.Runs.SequenceEqual(facts.Runs) && shell.PlaybackFacts.Held.SequenceEqual(facts.Held),"Playback uses the exact current snapshot.");
                if(!sawRun && facts.Runs.Count>0){sawRun=true;shell.TickDayPlayback(1.8);await Capture("delivery");}
                if(!sawClosure && facts.Held.Any(h=>h.Reason=="closed")){sawClosure=true;Require(shell.PlaybackText.Text.Contains("closed route"),"Closure text is attached to real held cargo.");await Capture("held");}
                map.ShowMapDay(facts with{Changes=[..facts.Changes,new MapChange("hubSite","power","vendorFailure",0,null,"hidden-test",false)]});
                Require(map.Tabletop!.ReportedChangeLabels.Count==facts.Changes.Where(c=>c.Known).Select(c=>c.SiteId).Distinct().Count(),"Unreported failures produce no marker.");map.ShowMapDay(facts);
                shell.TickDayPlayback(6);
                Require(!shell.PlayingDay && hash==shell.Session.StateHash,"Clock completion does not simulate another day.");
            }
            Require(sawRun && sawClosure,"Real authored replay exercises delivery and closed-route hold.");
            File.Delete(shell.AutosavePath);shell.QueueFree();
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("DAY_PLAYBACK_SMOKE_PASS: automatic exact-day playback, guarded advance, skip/timer purity, real runs/holds, reported-only change markers.");GetTree().Quit();

            async Task Capture(string name)
            {
                var args=OS.GetCmdlineUserArgs();var index=Array.IndexOf(args,"--screenshots");if(index<0)return;
                shell.DeveloperToggle.ButtonPressed=false;
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[index+1]+"-"+name+".png")==Error.Ok,"Native playback render saved.");
                shell.DeveloperToggle.ButtonPressed=true;
            }
        }
        catch(Exception error){GD.PushError($"DAY_PLAYBACK_SMOKE_FAIL: {error}");GetTree().Quit(1);}
    }
}
