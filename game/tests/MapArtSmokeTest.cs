using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class MapArtSmokeTest : Node
{
    public override async void _Ready()
    {
        var folder=Path.Combine(Path.GetTempPath(),"ocs-map-art-"+Guid.NewGuid());
        Directory.CreateDirectory(folder);
        GameShell? shell=null;
        try
        {
            using var scene=GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell=scene.Instantiate<GameShell>();
            shell.AutosavePathOverride=Path.Combine(folder,"save.json");
            shell.AudioSettingsPathOverride=Path.Combine(folder,"sound.cfg");
            shell.MusicSettingsPathOverride=Path.Combine(folder,"music.cfg");
            shell.LookSettingsPathOverride=Path.Combine(folder,"look.cfg");
            shell.TeamSettingsPathOverride=Path.Combine(folder,"team.cfg");
            File.WriteAllText(shell.AudioSettingsPathOverride,"[sound]\nmuted=true\n");
            File.WriteAllText(shell.MusicSettingsPathOverride,"[music]\nmuted=true\n");
            AddChild(shell); DeveloperWorkspace(shell);
            Scenario(shell, "tutorial");
            await Capture("tutorial-day1", "hubSite");
            Step(shell, 3);
            await Capture("tutorial-day4", "hubSite");
            Scenario(shell, "spoke-b"); Replay(shell); Step(shell, 10);
            await Capture("spoke-b-day11", "spokeBSite");
            File.Delete(shell.AutosavePath); shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("MAP_ART_SMOKE_PASS: tutorial days 1/4 and Spoke B day 11; Fit map and zoom; presentation leaves Sim unchanged.");
            GetTree().Quit();

            async Task Capture(string name, string site)
            {
                shell.DeveloperToggle.ButtonPressed = false;
                shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab;
                var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
                var hash = shell.Session.StateHash;
                map.FitMap();
                CheckModels(map);
                await Shot(name + "-fit");
                map.ZoomAt(map.SiteScreenPoint(site), 1.65f);
                map.PanBy(map.GetViewportRect().Size / 2 - map.SiteScreenPoint(site));
                await Shot(name + "-zoom");
                Require(shell.Session.StateHash == hash, "Model rendering and camera inspection leave Sim unchanged.");
                map.FitMap(); shell.DeveloperToggle.ButtonPressed = true;
            }
            async Task Shot(string name)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--screenshots");
                if (at < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[at + 1] + "-" + name + ".png") == Error.Ok, "Map art screenshot saved.");
            }
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError($"MAP_ART_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { Directory.Delete(folder,true); }
    }
    internal static void CheckModels(SchematicMap map)
    {
        var tabletop=map.Tabletop!;
        tabletop._Process(0);
        Require(tabletop.EquipmentModels.Count==map.Tiles.Count,"Every capability has a loaded mesh model.");
        foreach(var model in tabletop.EquipmentModels)
        {
            var bounds=MapEquipmentModels.Bounds(model);
            Require(Math.Abs(bounds.Position.Y-.77f)<.001f,"Every model rests on the island's ground plane.");
            Require(bounds.Size.X<=.93f && bounds.Size.Z<=.93f && bounds.Size.Y<=1.06f,
                "Kit models fit their cells without touching neighbouring equipment.");
        }
        Require(tabletop.DownMarkers.Count==map.Tiles.Count(t=>t.Status=="down"),"Only Sim-reported down capabilities get crosses.");
        Require(tabletop.DownMarkers.All(m=>m.Marker.Size.DistanceTo(new Vector2(24,24))<.01f && m.Model.GetMeta("capability_status").AsString()=="down"),
            "Down markers remain at least 16px at Fit map and belong to their actual down model.");
        var rectangles=tabletop.ModelScreenBounds.ToArray();
        foreach(var item in tabletop.DownMarkers)
        {
            var bounds=rectangles[tabletop.EquipmentModels.IndexOf(item.Model)];
            Require(item.Marker.GetRect().Intersects(bounds),
                "A down badge touches its own model's footprint rather than floating over a different row.");
        }
        if(map.Tiles.Select(t=>t.CapabilityId).Distinct().Count()==7)
            Require(tabletop.EquipmentModels.Select(m=>m.GetMeta("kit_model").AsString()).Distinct().Count()==7,
                "The tutorial's seven capabilities have seven distinct real-model silhouettes.");
    }

}
