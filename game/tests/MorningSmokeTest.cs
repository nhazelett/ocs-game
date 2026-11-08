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
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var expected = Session.Create(bundle, 1);
            var days = 0;
            do
            {
                expected.AdvanceDay(); days++;
            } while (days < 7 && !expected.Finished && !expected.View.MorningBoard().Items.Any(i => i.Severity == BoardSeverity.Alert || i.Category == BoardCategory.Request));
            shell.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.Session.CompletedDay == days, "Continue stops exactly at the Sim's first alert/request or seven-day bound.");

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
            var quiet = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<GameShell>();
            quiet.ScenarioRootOverride = temporary; AddChild(quiet);
            quiet.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            Require(quiet.Session.CompletedDay == 7, "A quiet scenario stops after exactly seven days.");
            GD.Print("MORNING_SMOKE_PASS: alert-first board, linked targets, event stop and seven-day limit.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"MORNING_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true); }
    }
}
