using Godot;
using System.Text.Json;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class MapScenerySmokeTest : Node
{
    public override async void _Ready()
    {
        var folder=Path.Combine(Path.GetTempPath(),"ocs-scenery-"+Guid.NewGuid());
        Directory.CreateDirectory(folder);GameShell? shell=null;
        try
        {
            using var scene=GD.Load<PackedScene>("res://scenes/Main.tscn");shell=scene.Instantiate<GameShell>();
            shell.AutosavePathOverride=Path.Combine(folder,"save.json");
            shell.LookSettingsPathOverride=Path.Combine(folder,"look.cfg");
            shell.AudioSettingsPathOverride=Path.Combine(folder,"sound.cfg");
            shell.MusicSettingsPathOverride=Path.Combine(folder,"music.cfg");
            shell.TeamSettingsPathOverride=Path.Combine(folder,"team.cfg");
            AddChild(shell);DeveloperWorkspace(shell);
            var picker=shell.GetNode<OptionButton>("%ScenarioPicker");
            var scenarios=Enumerable.Range(0,picker.ItemCount).Select(picker.GetItemText).Where(id=>!id.StartsWith("_")).ToArray();
            foreach(var id in scenarios)
            {
                DeveloperWorkspace(shell);Scenario(shell,id);shell.DeveloperToggle.ButtonPressed=false;
                shell.GetNode<TabContainer>("%DeskTabs").CurrentTab=GameShell.TodayTab;
                using var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(GamePaths.ScenarioRoot,id,"scenario.json")));
                Require(shell.Session.View!.Scenario.Id==data.RootElement.GetProperty("id").GetString(),"The fixture loaded the actual requested scenario.");
                var map=shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");map.FitMap();
                var hash=shell.Session.StateHash;
                for(var i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Require(map.Tabletop!.ModelsHaveGround,"Every actual model foot is inside its island's ground in "+id);
                Require(map.Tabletop.SceneryTriangles is >0 and <8000,"The sea/coast/contact geometry stays within its small presentation budget.");
                MapCalloutSmokeTest.Check(map);
                Require(shell.Session.StateHash==hash,"Scenery changes no Sim state in "+id);
                GD.Print($"SCENERY_BUDGET {id}: {map.Tabletop.SceneryTriangles} sea/coast/contact triangles.");
            }
            if(DisplayServer.GetName()!="headless")
            {
                DeveloperWorkspace(shell);Scenario(shell,"tutorial");shell.DeveloperToggle.ButtonPressed=false;
                var map=shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");map.FitMap();
                var hash=shell.Session.StateHash;
                for(var i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var texture=map.Tabletop!.SceneryViewport.GetTexture();using var before=texture.GetImage();
                await ToSignal(GetTree().CreateTimer(1.2),SceneTreeTimer.SignalName.Timeout);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var after=texture.GetImage();var changed=0;var sampled=0;
                for(var y=4;y<12;y++)for(var x=1;x<12;x++)
                {
                    var p=new Vector2(before.GetWidth()*x/12,before.GetHeight()*y/12);
                    if(map.Tabletop.ProjectedSolids.Any(s=>Geometry2D.IsPointInPolygon(p,s)))continue;
                    var a=before.GetPixel((int)p.X,(int)p.Y);var b=after.GetPixel((int)p.X,(int)p.Y);sampled++;
                    if(Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B)>.004f)changed++;
                }
                Require(sampled>10&&changed>4,"Native sea pixels move slowly without advancing a day.");
                Require(shell.Session.StateHash==hash,"Actual graphical water animation leaves Sim untouched.");
                GD.Print($"SCENERY_WATER_PIXELS: {changed}/{sampled} sampled sea pixels changed over 1.2 seconds.");
            }
            shell.QueueFree();shell=null;
            for(var i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("MAP_SCENERY_SMOKE_PASS: actual model feet, scenery budget and facts across authored scenarios; "+(DisplayServer.GetName()=="headless"?"headless geometry checks.":"native animated-water purity checked."));GetTree().Quit();
        }
        catch(Exception error){shell?.QueueFree();GD.PushError("MAP_SCENERY_SMOKE_FAIL: "+error);GetTree().Quit(1);}
        finally{Directory.Delete(folder,true);}
    }
}
