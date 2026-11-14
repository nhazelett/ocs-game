using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class MapLayoutSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell = scene.Instantiate<GameShell>();
            shell.AutosavePathOverride = Path.Combine(Path.GetTempPath(), "ocs-map-layout-" + Guid.NewGuid() + ".json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed); DeployWithoutPrep(shell);
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            void Check(string site)
            {
                Require(map.Tabletop is not null && map.Tabletop.SiteCount==map.Data!.Sites.Count && map.Tabletop.CapabilityObjects==map.Tiles.Count,"Actual 3D renderer includes all sites and capability rows.");
                MapArtSmokeTest.CheckModels(map);
                var layout = map.SiteLayout(site);
                Require(layout.Tiles.Select(t => t.Tile).SequenceEqual(map.Tiles.Where(t => t.SiteId == site)), "Every actual capability has a rendered tile.");
                foreach (var tile in layout.Tiles)
                {
                    Require(layout.Bounds.Encloses(tile.Bounds), "Every tile is inside the site card, including all rows after the second.");
                    Require(string.Join(" ", tile.LabelLines) == SchematicMap.MapLabel(tile.Tile.Label), "Wrapping keeps the entire label.");
                    foreach (var line in tile.LabelLines) Require(ThemeDB.FallbackFont.GetStringSize(line, fontSize: 18).X <= tile.Bounds.Size.X - 12, "No line is clipped by DrawString's width.");
                    Require(tile.PercentY + 5 < tile.Bounds.End.Y && tile.LabelY - 18 > tile.Symbol.Y + 30, "Label and percentage have space below the equipment, inside the tile.");
                    foreach (var other in layout.Tiles.Where(t => !ReferenceEquals(t, tile))) Require(!tile.Bounds.Intersects(other.Bounds), "Capability tiles do not overlap.");
                }
                Require(layout.NameY - 32 > layout.Bounds.End.Y, "Site name and status sit below the complete card.");
            }
            var hash = shell.Session.StateHash;
            Require(map.Tiles.Count == 7, "The cold tutorial exercises the reported seven-capability hub."); Check("hubSite");
            var tutorialLayout = map.SiteLayout("hubSite");
            Require(tutorialLayout.Tiles.Single(t => t.Tile.CapabilityId == "mhe").LabelLines.SequenceEqual(new[] { "Material", "handling", "equipment" }), "Material handling equipment is readable in full on three lines.");
            var point = map.SiteScreenPoint("hubSite");
            Require(map.GetViewportRect().HasPoint(point), "Cold fit includes the tutorial's actual 3D site.");
            await Screenshot("--map-screenshot");
            // Larger future site inventories use the same drawing layout, without changing scenario data or the Sim.
            var original = map.Tiles;
            for (var count = 8; count <= 15; count++)
            {
                map.ShowCapabilities(Enumerable.Range(0, count).Select(i => original[i % original.Count] with { CapabilityId = "layout-" + i }).ToArray()); Check("hubSite");
            }
            map.ShowCapabilities(original); Require(shell.Session.StateHash == hash, "Layout inspection changes no simulation state.");
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab;
            shell.PeopleToggle.ButtonPressed = true; Pick(shell.PersonContactInput, "finance");
            var visitPanel = shell.GetNode<TabContainer>("%DeskTabs").GetNode("Today/TodayPane/Body/Body");
            string VisitText() => Labels(visitPanel).Single(l => l.Text.StartsWith("This takes ")).Text;
            Require(shell.VisitOptions["finance"].Preview.QueueAheadHours == 0 && !VisitText().Contains("queue"), "An empty visit queue adds no clause.");
            var visitHash = shell.Session.StateHash;
            Require(VisitText().Contains("earliest result is day ") && visitHash == shell.Session.StateHash, "Visit preview still shows the real timing without spending time.");
            await Screenshot("--visit-screenshot", shell.GetNode<TabContainer>("%DeskTabs").GetNode<ScrollContainer>("Today/TodayPane/Body/Body"), Labels(visitPanel).Single(l => l.Text.StartsWith("This takes ")));
            shell.VisitButtons["finance"].EmitSignal(Button.SignalName.Pressed);
            var hours = shell.VisitOptions["finance"].Preview.QueueAheadHours;
            Require(hours > 0 && VisitText().Contains($", after the {hours} hours already queued."), "A queued visit states the exact pending hours.");
            foreach (var scenario in new[] { "spoke-b", "spoke-b-90", "sustainment" })
            {
                DeveloperWorkspace(shell); Scenario(shell, scenario);
                foreach (var site in map.Data!.Sites) Check(site.Id);
            }
            shell.QueueFree(); shell = null;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("MAP_LAYOUT_SMOKE_PASS: cold seven-tile tutorial, complete wrapped labels, card/viewport containment, 8–15 tiles, all scenarios, pure layout and exact zero/nonzero visit queue prose."); GetTree().Quit();
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError($"MAP_LAYOUT_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }

    private static IEnumerable<Label> Labels(Node node) => node.GetChildren().SelectMany(child => (child is Label label ? new[] { label } : Enumerable.Empty<Label>()).Concat(Labels(child)));
    private async Task Screenshot(string flag, ScrollContainer? scroll = null, Control? target = null)
    {
        var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, flag); if (at < 0) return;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (scroll is not null && target is not null) scroll.EnsureControlVisible(target);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Require(GetViewport().GetTexture().GetImage().SavePng(args[at + 1]) == Error.Ok, "Screenshot saved.");
    }
}
