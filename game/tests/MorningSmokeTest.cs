using Godot;
using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class MorningSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "los-quiet-" + Guid.NewGuid().ToString("N"));
        try
        {
            var shell = Shell(this);
            Require(shell.DisplayedLead is not null && shell.LeadOpen.Visible && !shell.LeadOpen.Disabled, "An actual priority item keeps its working Open button.");
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var expected = Session.Create(bundle, 1);
            var days = 0;
            do
            {
                expected.AdvanceDay(); days++;
            } while (!expected.Finished && expected.View.MorningBoard().Lead is null);
            shell.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.Session.CompletedDay == days, "Continue stops exactly at the next Sim-projected lead or completion.");

            Replay(shell); Step(shell, 10);
            var board = shell.Session.View!.MorningBoard();
            var buttons = shell.MorningItems.GetChildren().OfType<Button>().ToArray();
            Require(buttons.Length == board.Items.Count, "Every morning item is rendered.");
            for (var i = 0; i < buttons.Length; i++) Require(buttons[i].Text.Contains(board.Items[i].Text), "The Sim's alert-first order is retained.");
            var tile = board.Tiles.Single(t => t.SiteId == "spokeBSite" && t.CapabilityId == "power");
            shell.NavigateBoardItem(new BoardItem("test", 10, BoardCategory.Failure, BoardSeverity.Alert, BoardLinkKind.SiteCapability, tile.SiteId + "." + tile.CapabilityId, "", []));
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            Require(map.SelectedSite == tile.SiteId && shell.SelectedCapability == "spokeBSite.power", "Capability link selects the actual tile and highlights its site.");
            var card = shell.Session.View.Cards.First();
            shell.NavigateBoardItem(new BoardItem("test", 10, BoardCategory.Request, BoardSeverity.Info, BoardLinkKind.Card, card.Id, "", []));
            Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 1 && shell.SelectedCard == card.Id, "Card link opens the selected requirement.");
            var arrangement = shell.Session.View.Arrangements.First();
            shell.NavigateBoardItem(new BoardItem("test", 10, BoardCategory.Delivery, BoardSeverity.Info, BoardLinkKind.Arrangement, arrangement.Id, "", []));
            var rows = shell.GetNode<TabContainer>("%DeskTabs").GetNode("Ledger/LedgerContext/Contents").GetChildren().OfType<Button>();
            Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 5 && rows.Single(b => b.HasMeta("arrangement") && b.GetMeta("arrangement").AsString() == arrangement.Id).ButtonPressed, "Arrangement link opens and selects its real ledger row.");
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Screenshot saved.");
            }

            // Author a quiet temporary fixture using only existing scenario fields; production data is untouched.
            var dir = Path.Combine(temporary, "quiet"); Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(temporary, "rules"));
            File.Copy(Path.Combine(GamePaths.ScenarioRoot, "rules", "rules-2025-10.json"), Path.Combine(temporary, "rules", "rules-2025-10.json"));
            var data = JsonNode.Parse(File.ReadAllText(Path.Combine(bundle.Directory, "scenario.json")))!;
            foreach (var key in new[] { "events", "variation", "requirementCards", "inheritedArrangements" }) data[key] = new JsonArray();
            foreach (var site in data["sites"]!.AsArray()) site!["capabilities"] = new JsonArray();
            File.WriteAllText(Path.Combine(dir, "scenario.json"), data.ToJsonString());
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var quiet = scene.Instantiate<GameShell>();
            quiet.ScenarioRootOverride = temporary; AddChild(quiet);
            DeveloperWorkspace(quiet);
            DeployWithoutPrep(quiet);
            quiet.GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
            quiet.DeveloperToggle.ButtonPressed = false;
            shell.Hide();
            var quietHash = quiet.Session.StateHash;
            Require(quiet.Session.View!.MorningBoard().Lead is null && quiet.DisplayedLead is null && !quiet.LeadOpen.Visible,
                "A real quiet morning omits Open while keeping the no-priority sentence.");
            Require(quiet.LeadWords.Text == "There is no priority message this morning.", "The quiet morning still explains why there is no lead.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(quiet.PeopleToggle.IsVisibleInTree() && quiet.BoardDetailsToggle.IsVisibleInTree()
                && quiet.BoardDetailsToggle.GetGlobalRect().End.Y < GetViewport().GetVisibleRect().End.Y,
                "People and other updates remain reachable in the compact quiet board.");
            var quietShot = Array.IndexOf(args, "--quiet-screenshot");
            if (quietShot >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[quietShot + 1]) == Error.Ok, "Quiet morning screenshot saved.");
            }
            Require(quiet.Session.StateHash == quietHash, "The quiet layout and capture leave Sim state unchanged.");
            quiet.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            Require(quiet.Session.View!.Finished && quiet.Session.CompletedDay == quiet.Session.View.LastDay, "A quiet scenario continues to completion without an arbitrary week stop.");
            Require(!quiet.LeadOpen.Visible, "Open stays hidden at completion without a lead.");
            GD.Print("MORNING_SMOKE_PASS: quiet morning without Open, alert-first details, linked targets, Sim lead stop and quiet completion.");
            shell.QueueFree(); quiet.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"MORNING_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true); }
    }
}
