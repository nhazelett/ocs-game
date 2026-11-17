using Godot;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class RequestWorkspaceSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            shell = Shell(this); Scenario(shell, "tutorial"); shell.DeveloperToggle.ButtonPressed = false;
            var tabs = shell.GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = GameShell.RequestsTab;
            await Frames();
            var cards = shell.Session.View!.Cards.Where(c => shell.Session.View.RequirementEditor(c.Id) is not null).ToArray();
            Require(shell.RequestListButtons.Keys.SequenceEqual(cards.Select(c => c.Id)), "The desk lists every arrived request once, without its detail or form.");
            Require(!shell.RequestWorkspace.Visible && !PlayScreen.Descendants(tabs.GetCurrentTabControl()).OfType<SpinBox>().Any(), "The Requests tab is a list, not a hidden buy form.");
            var listScroll = tabs.GetNode<ScrollContainer>("Requests/RequestList/Body/Body");
            listScroll.ScrollVertical = 10000; await Frames(); var position = listScroll.ScrollVertical;
            var hash = shell.Session.StateHash;
            shell.RequestListButtons["tutWater"].EmitSignal(Button.SignalName.Pressed); await Frames();
            Page("tutWater");
            Pick(shell.VendorInput, "islandMart"); Pick(shell.InstrumentInput, "gpc"); await Frames();
            Require(shell.PendingAward!.Card == "tutWater" && shell.AwardSummaryLabel.Text == shell.Session.View!.AwardSummary(shell.PendingAward)!.Sentence, "The moved form previews the exact selected request and complete Sim summary.");
            var pinned = shell.AwardButton.GetGlobalRect(); shell.AwardScroll.ScrollVertical = 10000; await Frames();
            Require(shell.AwardButton.GetGlobalRect() == pinned && shell.RequestBuyColumn.GetGlobalRect().Encloses(pinned), "The buy action and summary are pinned independently of the scrolling form.");
            shell.AwardScroll.ScrollVertical = 0; await Capture("water");
            shell.RequestBackButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(!shell.RequestWorkspace.Visible && tabs.CurrentTab == GameShell.RequestsTab && listScroll.ScrollVertical == position, "Back returns to the exact originating desk tab and scroll position.");
            Require(shell.Session.StateHash == hash, "Opening, choosing terms and returning spend no hours or money.");
            await Capture("desk-after-back");
            tabs.CurrentTab = GameShell.TodayTab;
            shell.StandingOpenButtons["tutWater"].EmitSignal(Button.SignalName.Pressed); await Frames(); Page("tutWater");
            using (var escape = new InputEventKey { Keycode = Key.Escape, Pressed = true }) GetViewport().PushInput(escape, true);
            await Frames(); Require(!shell.RequestWorkspace.Visible && tabs.CurrentTab == GameShell.TodayTab, "Escape returns to Today through real viewport input.");
            Step(shell, 3);
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            Require(map.Tabletop!.ToggleShortfall("hubSite"), "The map's known shortfall card opens.");
            await Frames();
            var mapRequest = map.Tabletop.RequestButtons.Values.First();
            var mapCard = mapRequest.GetMeta("map_request").AsString();
            await Capture("map-request");
            var hit = mapRequest.GetGlobalRect().GetCenter();
            using (var down = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = hit }) map._Input(down);
            using (var up = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = hit }) map._Input(up);
            await Frames(); Page(mapCard);
            shell.CloseRequestWorkspace(); Open("tutForklift");
            shell.RequirementAskButtons["dunn"].EmitSignal(Button.SignalName.Pressed); Step(shell); Open("tutForklift");
            await Frames(); Page("tutForklift");
            var learned = shell.RequestContents.GetChildren().OfType<Label>().Single(l => l.HasMeta("learned_line") && l.GetMeta("learned_line").AsString() == "operator");
            var line = shell.RequestContents.GetChildren().OfType<Label>().Single(l => l.HasMeta("requirement_line") && l.GetMeta("requirement_line").AsString() == "operator");
            Require(line.GetIndex() < learned.GetIndex() && learned.Text.Contains(shell.Session.View!.RequirementEditor("tutForklift")!.Lines.Single(l => l.Id == "operator").Learned!), "Dunn's exact warning stays beside its known part.");
            shell.RequestScroll.EnsureControlVisible(shell.RequirementAddButtons["operator"]); await Capture("forklifts");
            shell.RequirementAddButtons["operator"].EmitSignal(Button.SignalName.Pressed); await Frames(); Page("tutForklift");
            Require(!shell.RequirementAddButtons.ContainsKey("operator") && shell.CurrentActionEditor!.SelectedLines.Contains("operator"), "Adding the learned part refreshes both columns in place.");
            shell.CloseRequestWorkspace(); tabs.CurrentTab = GameShell.RequestsTab; shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.CurrentActionEditor!.CardId is null && shell.RequestContents.GetChildren().OfType<Label>().Any(l => l.Text == "Buy on your own initiative"), "Own-initiative buying uses the same page and explains what it is for on the left.");
            shell.CloseRequestWorkspace(); await Frames();
            Open("tutForklift"); await Frames(); shell.RequestBackButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            var decision = PlayScreen.Descendants(shell.DecisionRows).OfType<Button>().FirstOrDefault(b => b.HasMeta("decision_card"));
            Require(decision is not null, "The committed ask/add has a request-linked decision.");
            decision!.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.RequestWorkspace.Visible && shell.SelectedCard == decision.GetMeta("decision_card").AsString(), "A decisions entry opens the same request page.");
            shell.QueueFree(); shell = null; await Frames();
            GD.Print("REQUEST_WORKSPACE_SMOKE_PASS: compact list, exact request/ask/add/form, two independent scrolls, pinned commit, strip, Back/Esc return, decisions and own initiative."); GetTree().Quit();

            void Open(string card) => shell!.NavigateBoardItem(new BoardItem("", shell.Session.View!.Day, BoardCategory.Request, BoardSeverity.Info, BoardLinkKind.Card, card, "", []));
            void Page(string card)
            {
                Require(shell!.RequestWorkspace.Visible && shell.SelectedCard == card && shell.CurrentActionEditor!.CardId == card, "The selected request and its form share one full page.");
                var viewport = GetViewport().GetVisibleRect(); var page = shell.RequestWorkspace.GetGlobalRect();
                var strip = shell.GetNode<Control>("Margin/Layout/TopStrip").GetGlobalRect();
                Require(viewport.Encloses(page) && page.Size.X > 1200 && page.Position.Y >= strip.End.Y, "The page covers map and pane below the intact strip.");
                Require(page.Encloses(shell.RequestBackButton.GetGlobalRect()) && shell.RequestColumn.Size.X >= page.Size.X / 3, "Back stays at the top left and the request has at least one third of the width.");
                Require(shell.RequestBackButton.Size.Y <= 50 && shell.RequestBackButton.Size.X > 150 && shell.RequestColumn.Size.Y > 400, "The Back button keeps one line and gives the columns their reading height.");
                var scrolls = PlayScreen.Descendants(shell.RequestWorkspace).OfType<ScrollContainer>().Where(s => s.IsVisibleInTree()).ToArray();
                Require(scrolls.Length == 2 && !scrolls.Any(s => PlayScreen.Descendants(s).OfType<ScrollContainer>().Any(n => n.IsVisibleInTree())), "Each column has one scroll and no nested scroll.");
                Require(shell.RequestScroll.GetGlobalRect().End.X < shell.AwardScroll.GlobalPosition.X, "The two reading columns sit side by side.");
            }
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Capture(string suffix)
            {
                await Frames(); var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots"); if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage(); Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native request workspace capture saved.");
            }
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError("REQUEST_WORKSPACE_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
