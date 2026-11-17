using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class AwardFeedbackSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); AddChild(shell);
            shell.AutosavePathOverride = Path.Combine(Path.GetTempPath(), "ocs-award-feedback-" + Guid.NewGuid() + ".json");
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed); FinishOpening(shell);
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            var water = shell.Session.View!.Cards.First(c => c.CapabilityId == "water").Id;
            shell.NavigateBoardItem(new BoardItem("", 1, BoardCategory.Request, BoardSeverity.Info, BoardLinkKind.Card, water, "", []));
            var pane = tabs.GetNode<Control>("Requests/RequirementPane");
            Require(PlayScreen.Descendants(pane).OfType<Label>().Any(l => l.Text.Contains("nobody has requested"))
                && PlayScreen.Descendants(pane).OfType<Label>().Any(l => l.Text.Contains("for this request")), "Both routes explain what they arrange.");
            await Capture("request-before");
            shell.RequirementMarketButton.EmitSignal(Button.SignalName.Pressed);
            Pick(shell.VendorInput, "islandMart"); Pick(shell.InstrumentInput, "gpc");
            shell.StartInput.Value = 2; shell.DurationInput.Value = 7;
            var award = shell.PendingAward!;
            Require(!shell.AwardButton.Disabled && shell.AwardPreview!.Accepted, "The tutorial's real water card buy is valid.");
            var before = shell.Session.CreateSave().Actions.Count;
            var stale = shell.AwardButton;
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.MarketOpen && tabs.CurrentTab == GameShell.RequestsTab && shell.SelectedCard == water,
                "A successful arrangement returns to the exact request instead of quoting its own committed capacity.");
            Require(shell.Session.CreateSave().Actions.Count == before + 1, "Exactly one award was sent.");
            var decision = shell.Session.View!.Decisions(water).First();
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(decision.Sentence), "The acknowledgement uses the actual committed decision sentence.");
            var rows = shell.RequestDecisionRows!;
            Require(rows.GetChildren().First().GetMeta("decision_sentence").AsString() == decision.Sentence,
                "The new decision is first on its request.");
            var requestScroll = tabs.GetNode<ScrollContainer>("Requests/RequirementPane/Body/Body");
            stale.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.CreateSave().Actions.Count == before + 1, "A stale button cannot submit a duplicate arrangement.");
            await Frames();
            await Capture("arranged");
            Require(requestScroll.ScrollVertical == 0 && requestScroll.GetGlobalRect().Encloses(rows.GetChild<Control>(0).GetGlobalRect()),
                "The new arrangement and its actual state are on screen at the top.");
            // A fresh form still offers real structural refusals, with their explanations in the list itself.
            shell.RequirementMarketButton.EmitSignal(Button.SignalName.Pressed);
            Pick(shell.InstrumentInput, "bpaCall");
            var blocked = shell.CurrentAwardChoices!.Vendors.First(v => !v.Available && v.Reason is not null);
            var index = Enumerable.Range(1, shell.VendorInput.ItemCount - 1)
                .Single(i => shell.VendorInput.GetItemMetadata(i).AsString() == blocked.Id);
            Require(shell.VendorInput.IsItemDisabled(index) && shell.VendorInput.GetItemText(index).Contains(" — ")
                && !shell.VendorInput.GetItemText(index).Contains("(unavailable)")
                && shell.VendorInput.GetItemTooltip(index) == blocked.Reason,
                "The disabled supplier row states the Sim's actual refusal.");
            Require(PlayScreen.Descendants(tabs.GetNode("Requests/Market")).OfType<Label>().Any(l => l.Text == "Contract vehicle"),
                "The ordering vehicle cannot be mistaken for the office's truck.");
            await Frames(); shell.AwardScroll.EnsureControlVisible(shell.VendorInput); await Frames();
            var args = OS.GetCmdlineUserArgs();
            if (Array.IndexOf(args, "--screenshots") >= 0)
            {
                shell.VendorInput.ShowPopup(); await Frames();
                var popup = shell.VendorInput.GetPopup();
                Require(new Rect2I(0, 0, 1280, 800).Encloses(new Rect2I(popup.Position, popup.Size)),
                    "The supplier explanations fit inside the native window.");
                await Capture("supplier-reasons"); shell.VendorInput.GetPopup().Hide();
            }
            await Frames(); shell.AwardScroll.EnsureControlVisible(shell.VehicleInput); await Capture("contract-vehicle");
            Require(DeskUi.DaySpan(17, 17) == "day 17" && DeskUi.DaySpan(17, 18) == "days 17–18", "One-day and multi-day gaps use readable dates.");
            tabs.CurrentTab = GameShell.InboxTab;
            var countHeading = PlayScreen.Descendants(tabs.GetNode("Inbox")).OfType<Label>().Single(l => l.Text.StartsWith("Unauthorized commitments to decide"));
            await Frames(); tabs.GetNode<ScrollContainer>("Inbox/Contents").EnsureControlVisible(countHeading);
            await Capture("commitment-count");
            GD.Print("AWARD_FEEDBACK_SMOKE_PASS: exact return, visible committed support, sentence acknowledgement, no duplicate, reasons, route hints and labels.");
            GetTree().Quit();

            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Capture(string suffix)
            {
                await Frames();
                var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--screenshots");
                if (index < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[index + 1] + "-" + suffix + ".png") == Error.Ok, "Native feedback screenshot saved.");
            }
        }
        catch (Exception error) { GD.PushError("AWARD_FEEDBACK_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
