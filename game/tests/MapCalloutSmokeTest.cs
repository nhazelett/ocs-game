using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class MapCalloutSmokeTest : Node
{
    public override async void _Ready()
    {
        var folder=Path.Combine(Path.GetTempPath(),"ocs-callouts-"+Guid.NewGuid());
        Directory.CreateDirectory(folder);
        GameShell? shell=null;
        try
        {
            using var scene=GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell=scene.Instantiate<GameShell>();
            shell.AutosavePathOverride=Path.Combine(folder,"save.json");
            shell.LookSettingsPathOverride=Path.Combine(folder,"look.cfg");
            shell.AudioSettingsPathOverride=Path.Combine(folder,"sound.cfg");
            shell.MusicSettingsPathOverride=Path.Combine(folder,"music.cfg");
            shell.TeamSettingsPathOverride=Path.Combine(folder,"team.cfg");
            AddChild(shell);shell.SetProcess(false);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed);DeployWithoutPrep(shell);
            var map=shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");map.SetProcess(false);
            var args=OS.GetCmdlineUserArgs();var shot=Array.IndexOf(args,"--screenshots");
            async Task Capture(string suffix)
            {
                shell.DeveloperToggle.ButtonPressed=false;map.FitMap();map.SetMovementProgress(.36f);
                for(var i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var hash=shell.Session.StateHash;
                Check(map);
                Require(hash==shell.Session.StateHash,"Callout placement never changes the simulation.");
                if(shot>=0)
                {
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    using var image=GetViewport().GetTexture().GetImage();
                    Require(image.GetWidth()==1280&&image.GetHeight()==800,"Matched screenshots use the release viewport size.");
                    Require(image.SavePng(args[shot+1]+"-"+suffix+".png")==Error.Ok,"Native map screenshot saved.");
                }
            }
            await Capture("tutorial-day1");Step(shell,3);await Capture("tutorial-day4");
            DeveloperWorkspace(shell);Scenario(shell,"spoke-b");Replay(shell);Step(shell,10);
            Require(map.Tiles.Any(t=>t.Status=="down"),"Real replay contains a down site.");await Capture("spoke-day11");
            Step(shell,6);EndDay(shell);
            Require(shell.PlayingDay&&map.Day!.Held.Any(h=>h.Reason=="closed"),"Actual closed barge lane holds cargo during playback.");
            await Capture("held-playback");shell.FinishDayPlayback();await Capture("closed-lane");
            foreach(var direction in new[]{0,1,2})
            {
                shell.LookPicker.Select(direction);shell.LookPicker.EmitSignal(OptionButton.SignalName.ItemSelected,direction);
                map.FitMap();
                for(var i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check(map);
                map.ZoomAt(map.GetViewportRect().Size/2,1.15f);map.PanBy(new Vector2(12,8));
                for(var i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check(map);
            }
            shell.QueueFree();shell=null;
            for(var i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("MAP_CALLOUT_SMOKE_PASS: exact tutorial/down/held/closed snapshots and pure native map layout.");GetTree().Quit();
        }
        catch(Exception error){shell?.QueueFree();GD.PushError($"MAP_CALLOUT_SMOKE_FAIL: {error}");GetTree().Quit(1);}
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
    internal static void Check(SchematicMap map)
    {
        map.Tabletop!._Process(0);
        var cards=map.Tabletop.CalloutCards.ToArray();
        foreach(var card in cards)
        {
            Require(new Rect2(Vector2.Zero,map.Tabletop.Size-new Vector2(0,map.Tabletop.BottomOcclusion)).Encloses(card.GetRect()),"Callout stays inside the map: "+card.GetRect());
            foreach(var other in cards.Where(c=>c!=card))
                Require(!card.GetRect().Intersects(other.GetRect()),"Callout collision: "+card.GetRect()+" / "+other.GetRect());
            Require(!map.Tabletop.ProjectedSolids.Any(p=>TabletopMap3D.IntersectsSolid(card.GetRect(),p)),"Callout covers solid scenery: "+card.GetRect());
        }
        Require(map.Tabletop!.CalloutLayoutFits,"Visible cards have a complete collision-free placement.");
        Require(map.Tabletop.CalloutPointers.All(p=>p.Points.Length==2&&p.Points[0].DistanceTo(p.Points[1])>0),"Every visible card has a pointer to its map target.");
        Require(map.Tabletop.ClosedLaneBarriers.All(p=>p.Points.Length==2&&p.Points[0].DistanceTo(p.Points[1])>=19),"Closed-lane crosses keep a readable fixed screen size.");
    }
}
