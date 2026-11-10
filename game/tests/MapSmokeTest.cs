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
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1] + "-" + name + ".png") == Error.Ok, "Screenshot saved.");
            }
            var hash = shell.Session.StateHash;
            Require(map.Day?.Day == 0 && map.Day.Runs.Count == 0, "No fictional movements before any day is played.");
            Require(map.Tiles.SequenceEqual(shell.Session.View!.MorningBoard().Tiles), "Every equipment symbol reads its actual capability tile.");
            Require(map.Tabletop is not null && map.Tabletop.SiteCount==map.Data!.Sites.Count && map.Tabletop.CapabilityObjects==map.Tiles.Count,"Native 3D board includes every authored site and actual capability tile.");
            await Capture("day1");
            var oldZoom = map.CameraZoom; var oldOffset = map.CameraOffset;
            var oldPoint=map.SiteScreenPoint("hubSite");var oldSize=map.Tabletop!.CameraSize;
            map.ZoomAt(new Vector2(250, 200), 1.3f); map.PanBy(new Vector2(40, -20));
            Require(map.CameraZoom > oldZoom && map.CameraOffset != oldOffset && shell.Session.StateHash == hash, "Pan/zoom are real camera changes without Sim mutation.");
            Require(map.Tabletop.CameraSize<oldSize && map.SiteScreenPoint("hubSite")!=oldPoint,"Real 3D camera and projected site position change.");
            map.FitMap(); var point = map.SiteScreenPoint("hubSite");
            map._Input(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true });
            map._Input(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = false });
            Require(map.SelectedSite == "hubSite" && shell.SelectedCapability?.StartsWith("hubSite.") == true, "Clicking a site reaches its actual support details.");
            Require(shell.Session.StateHash == hash, "Site inspection is read-only.");
            shell.DeveloperToggle.ButtonPressed = true; Replay(shell);
            var capturedRun = false; var capturedClosure = false; var capturedDay18 = false;
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
                    var status=map.Tabletop!.SiteStatuses["spokeBSite"].Text.ToLowerInvariant();
                    Require(new[]{"power","fuel","lodging","down"}.All(status.Contains),"The actual site caption names every down capability in words.");
                    map.Tabletop!._Process(0);
                    Require(map.Tabletop.FactLabels.Where(l=>l.Label.HasMeta("reported_change")).All(l=>
                        l.Label.GetRect().End.Y <= map.SiteScreenPoint("spokeBSite").Y-30),
                        "Reported shortfall captions sit above the site's equipment instead of covering it.");
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
                    map.Tabletop!._Process(0);
                    var held=map.Tabletop.FactLabels.Where(l=>l.Leader is not null).ToArray();
                    Require(held.Any(l=>l.Label.Text=="Fuel for Spoke B waiting: barge lane closed."),"Cargo caption names its actual capability, destination and route closure.");
                    var cargo=held.First(l=>l.Label.Text.Contains("Spoke B"));
                    var source=map.SiteScreenPoint("hubSite");var destination=map.SiteScreenPoint("spokeBSite");
                    Require(cargo.Leader!.Points.Length==2 && cargo.Leader.Points[0].DistanceTo(source)<cargo.Leader.Points[0].DistanceTo(destination)
                        && (cargo.Leader.Points[0]-source).Dot(destination-source)>0 && cargo.Leader.Points[0].DistanceTo(cargo.Label.GetRect().GetCenter())<180,
                        "Cargo stays at the hub end of the outgoing route with a nearby tethered label.");
                    var before=cargo.Leader!.Points[0];map.PanBy(new Vector2(12,8));map.Tabletop._Process(0);
                    Require(cargo.Leader.Points[0]!=before,"The cargo label tether follows the real map camera.");map.FitMap();
                    capturedClosure = true; shell.DeveloperToggle.ButtonPressed = false; await Capture("closed"); shell.DeveloperToggle.ButtonPressed = true;
                }
                // The morning of day 18 displays the movements completed on day 17.
                if (day == 17)
                {
                    shell.DeveloperToggle.ButtonPressed=false;
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    map.FitMap();map.Tabletop!._Process(0);
                    var cargo=map.Tabletop.FactLabels.Single(l=>l.Leader is not null && l.Label.Text=="Fuel for Spoke B waiting: barge lane closed.");
                    Require(map.Tabletop.CaptionLabels.All(l=>!l.GetGlobalRect().Intersects(cargo.Label.GetGlobalRect().Grow(4))),
                        "Day-eighteen held cargo leaves every route, site name and status caption readable at default zoom.");
                    capturedDay18=true;
                    shell.DeveloperToggle.ButtonPressed=false;await Capture("day18");shell.DeveloperToggle.ButtonPressed=true;
                }
                Require(shell.Session.StateHash == hash, "Animation, rendering and snapshot queries never mutate Sim.");
            }
            Require(capturedRun && capturedClosure && capturedDay18, "Real authored approach exercises a delivery and a closed route holding cargo.");
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
