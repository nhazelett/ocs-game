using Godot;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class MapSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>();
            shell.AutosavePathOverride = Path.Combine(Path.GetTempPath(), "ocs-map-" + Guid.NewGuid().ToString("N") + ".json");
            AddChild(shell); shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed); DeployWithoutPrep(shell);
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string name)
            {
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1] + "-" + name + ".png") == Error.Ok, "Screenshot saved.");
            }
            var hash = shell.Session.StateHash;
            Require(map.Day?.Day == 0 && map.Day.Runs.Count == 0, "No fictional movements before any day is played.");
            Require(map.Tiles.SequenceEqual(shell.Session.View!.MorningBoard().Tiles), "Every equipment symbol reads its actual capability tile.");
            await Capture("day1");
            var oldZoom = map.CameraZoom; var oldOffset = map.CameraOffset;
            map.ZoomAt(new Vector2(250, 200), 1.3f); map.PanBy(new Vector2(40, -20));
            Require(map.CameraZoom > oldZoom && map.CameraOffset != oldOffset && shell.Session.StateHash == hash, "Pan/zoom are real camera changes without Sim mutation.");
            map.FitMap(); var point = map.SiteScreenPoint("hubSite");
            map._Input(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true });
            map._Input(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = false });
            Require(map.SelectedSite == "hubSite" && shell.SelectedCapability?.StartsWith("hubSite.") == true, "Clicking a site reaches its actual support details.");
            Require(shell.Session.StateHash == hash, "Site inspection is read-only.");
            shell.DeveloperToggle.ButtonPressed = true; Replay(shell);
            var capturedRun = false; var capturedClosure = false;
            for (var day = 1; day <= 20; day++)
            {
                Step(shell); hash = shell.Session.StateHash;
                var facts = shell.Session.View!.MapDay();
                Require(map.Day!.Day == facts.Day && map.Day.Runs.SequenceEqual(facts.Runs) && map.Day.Held.SequenceEqual(facts.Held)
                    && map.Day.Links.SequenceEqual(facts.Links) && map.Day.Shipments.SequenceEqual(facts.Shipments), "Map receives exact engine routes, deliveries, holds and shipments.");
                Require(map.KnownChanges.SequenceEqual(facts.Changes.Where(c => c.Known)), "Unreported causes are excluded from visible change markers.");
                foreach (var link in facts.Links.Where(l => !l.Open)) Require(!map.Day.Runs.Any(r => r.LinkId == link.Id), "Closed routes do not invent a trip.");
                if (day == 10)
                {
                    Require(map.Tiles.Single(t => t.SiteId == "spokeBSite" && t.CapabilityId == "power").Status == "down", "Day-ten generator support goes dark using Sim's status.");
                    shell.DeveloperToggle.ButtonPressed = false; await Capture("day10"); shell.DeveloperToggle.ButtonPressed = true;
                }
                if (!capturedRun && facts.Runs.Any(r => r.SiteId == "spokeBSite"))
                {
                    capturedRun = true; map.ReplayMovement();
                    if (shot >= 0) await ToSignal(GetTree().CreateTimer(1.8), SceneTreeTimer.SignalName.Timeout);
                    shell.DeveloperToggle.ButtonPressed = false; await Capture("delivery"); shell.DeveloperToggle.ButtonPressed = true;
                }
                if (!capturedClosure && facts.Held.Any(h => h.Reason == "closed"))
                {
                    capturedClosure = true; shell.DeveloperToggle.ButtonPressed = false; await Capture("closed"); shell.DeveloperToggle.ButtonPressed = true;
                }
                Require(shell.Session.StateHash == hash, "Animation, rendering and snapshot queries never mutate Sim.");
            }
            Require(capturedRun && capturedClosure, "Real authored approach exercises a delivery and a closed route holding cargo.");
            var actual = shell.Session.View!.MapDay();
            map.ShowMapDay(actual with { Changes = [.. actual.Changes, new MapChange("hubSite", "power", "vendorFailure", 0, null, "unreported-test", false)] });
            Require(map.KnownChanges.All(c => c.RecordId != "unreported-test"), "An explicitly unreported change is never drawn.");
            map.ShowMapDay(actual);
            File.Delete(shell.AutosavePath);
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("MAP_SMOKE_PASS: illustrated tiles, camera/site input, exact runs/closures/cargo, known filtering and pure animation."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"MAP_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
