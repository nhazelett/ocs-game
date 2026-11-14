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
            await Capture("held-playback");shell.FinishDayPlayback();shell.CloseDaySummary();await Capture("closed-lane");
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
            Step(shell,3);await Capture("partial-supply");
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
        Require(map.Tabletop!.CalloutLayoutFits,$"Day {map.Day?.Day}: visible cards have a complete collision-free placement: "+map.Tabletop.CalloutFailure);
        foreach(var site in map.Tiles.Where(t=>t.Status=="down").GroupBy(t=>t.SiteId))
        {
            var label=map.Tabletop.SiteStatuses[site.Key];
            var visible=string.Join("\n",label.GetParent().GetChildren().OfType<Label>()
                .Where(l=>l.IsVisibleInTree()).Select(l=>l.Text));
            foreach(var tile in site)
                Require(visible.Contains(tile.Label,StringComparison.OrdinalIgnoreCase)
                    &&(visible.Contains("down",StringComparison.OrdinalIgnoreCase)||visible.Contains("0% supplied")),
                    $"Every down site's visible text keeps its facts, including {tile.Label}: {visible}");
        }
        foreach(var panel in map.Tabletop.FactPanels)
        {
            using var background=panel.GetThemeStylebox("panel");
            Require(background is StyleBoxFlat paper&&paper.BgColor==TabletopTheme.Paper,
                "Every visible fact, including compact outage summaries, has opaque paper behind it.");
        }
        Require(map.Tabletop.FactPanels.Count()==map.Tabletop.CalloutPointers.Count(),
            "Every visible fact panel has a short tether to its own actual target.");
        Require(map.Tabletop.QuietLabels.All(l=>l.GetThemeConstant("outline_size")>=4),
            "Quiet site and lane names keep a broad contrasting halo over sea.");
        var cards=map.Tabletop.CalloutCards.ToArray();
        var badges=map.Tabletop.DownMarkers.Select(m=>m.Marker.GetRect()).ToArray();
        var stems=map.Tabletop.DownStems.SelectMany(s=>Enumerable.Range(1,s.Points.Length-1)
            .Select(i=>(From:s.Points[i-1],To:s.Points[i]))).ToArray();
        foreach(var card in cards)
        {
            Require(new Rect2(Vector2.Zero,map.Tabletop.Size-new Vector2(0,map.Tabletop.BottomOcclusion)).Encloses(card.GetRect()),"Callout stays inside the map: "+card.GetRect());
            Require(badges.All(b=>!b.Intersects(card.GetRect())),"A card or shore name never covers an outage badge.");
            Require(stems.All(s=>!TabletopMap3D.SegmentHits(s.From,s.To,TabletopMap3D.RectanglePolygon(card.GetRect()))),
                "A card or shore name never covers an outage badge stem.");
            foreach(var other in cards.Where(c=>c!=card))
                Require(!card.GetRect().Intersects(other.GetRect()),"Callout collision: "+card.GetRect()+" / "+other.GetRect());
            Require(!map.Tabletop.ProjectedSolids.Any(p=>TabletopMap3D.IntersectsSolid(card.GetRect(),p)),"Callout covers solid scenery: "+card.GetRect());
        }
        Require(map.Tabletop.VisibleRouteDots.All(p=>cards.All(c=>!c.GetRect().Grow(6).HasPoint(p))),
            "Route dots stop before map lettering and its halo.");
        var pointers=map.Tabletop.CalloutPointers.ToArray();
        foreach(var pointer in pointers)
        {
            var a=pointer.Points[0];var b=pointer.Points[1];
            Require(pointer.Points.Length==2&&a.DistanceTo(b)>0&&a.DistanceTo(b)<=120.01f,"Fact pointers stay within 120 px of their actual target.");
            Require(badges.All(r=>!TabletopMap3D.SegmentHits(a,b,TabletopMap3D.RectanglePolygon(r))),
                "A callout pointer never crosses an outage badge.");
            Require(stems.All(s=>Geometry2D.SegmentIntersectsSegment(a,b,s.From,s.To).VariantType==Variant.Type.Nil),
                "A callout pointer never crosses an outage badge stem.");
            foreach(var other in pointers.Where(p=>p!=pointer))
                Require(Geometry2D.SegmentIntersectsSegment(a,b,other.Points[0],other.Points[1]).VariantType==Variant.Type.Nil,"Map pointers never cross.");
            foreach(var card in cards.Where(c=>!c.GetRect().Grow(4).HasPoint(b)))
                Require(!TabletopMap3D.SegmentHits(a,b,TabletopMap3D.RectanglePolygon(card.GetRect())),"A pointer never crosses another card or shore name.");
        }
        foreach(var target in map.Tabletop.PointerTargets)
            foreach(var solid in map.Tabletop.SolidInventory.Where(s=>target.Island is null||s.Island!=target.Island))
                Require(!TabletopMap3D.SegmentHits(target.Pointer.Points[0],target.Pointer.Points[1],solid.Polygon),"A pointer never crosses another island or its equipment.");
        Require(map.Tabletop.FullCalloutText.All(t=>t!="Supported"&&t!="Air lane"&&t!="Barge lane"),"Supported sites and open lanes have only small unboxed names.");
        Require(map.Tabletop.FullCalloutWords.All(l=>l.GetLineCount()==l.Text.Split('\n').Length),"Every reported fact occupies one complete line without wrapping.");
        foreach(var pair in map.Tabletop.LanePoints.GroupBy(l=>(l.From,l.To)))
            if(pair.Any(l=>l.Mode=="aircraft")&&pair.Any(l=>l.Mode=="barge"))
                Require(pair.First(l=>l.Mode=="aircraft").Point.DistanceTo(pair.First(l=>l.Mode=="barge").Point)>24,"Air and barge paths have distinct visible targets.");
        Require(map.Tabletop.ClosedLaneBarriers.All(p=>p.Points.Length==2&&p.Points[0].DistanceTo(p.Points[1])>=19),"Closed-lane crosses keep a readable fixed screen size.");
    }
}
