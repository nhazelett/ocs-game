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
            var pane = shell.RequestColumn;
            Require(PlayScreen.Descendants(pane).OfType<Label>().Any(l => l.Text == shell.Session.View!.RequirementEditor(water)!.Title), "The left column identifies the same request as the buy.");
            var peak = PlayScreen.Descendants(pane).OfType<Label>().Single(l => l.HasMeta("request_peak"));
            Require(peak.Text.StartsWith("Peak demand: 4,500 gal/day"), "Peak demand uses the same daily-rate words as the Sim.");
            await Capture("request-before");
            shell.GoToMarket(shell.SelectedCard!);
            Pick(shell.VendorInput, "islandMart"); Pick(shell.InstrumentInput, "gpc");
            shell.StartInput.Value = 2; shell.DurationInput.Value = 7;
            var award = shell.PendingAward!;
            Require(!shell.AwardButton.Disabled && shell.AwardPreview!.Accepted, "The tutorial's real water card buy is valid.");
            var before = shell.Session.CreateSave().Actions.Count;
            var stale = shell.AwardButton;
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.RequestWorkspace.Visible && shell.SelectedCard == water,
                "A successful arrangement returns to the exact request instead of quoting its own committed capacity.");
            Require(shell.Session.CreateSave().Actions.Count == before + 1, "Exactly one award was sent.");
            var decision = shell.Session.View!.Decisions(water).First();
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(decision.Sentence), "The acknowledgement uses the actual committed decision sentence.");
            var rows = shell.RequestDecisionRows!;
            Require(rows.GetChildren().First().GetMeta("decision_sentence").AsString() == decision.Sentence,
                "The new decision is first on its request.");
            var decisionText = string.Join("\n", PlayScreen.Descendants(rows).Select(n => n is Button b ? b.Text : n is Label l ? l.Text : ""));
            Require(decisionText.Contains(decision.Now) && decisionText.Contains(decision.Teammate, StringComparison.OrdinalIgnoreCase)
                && !decisionText.Contains("With " + decision.Teammate + "."),
                "The completed immediate buy names its teammate without implying work is still waiting on them.");
            var requestScroll = shell.RequestScroll;
            stale.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.CreateSave().Actions.Count == before + 1, "A stale button cannot submit a duplicate arrangement.");
            await Frames();
            var receipt = PlayScreen.Descendants(shell.RequestBuyColumn).OfType<Label>().Single(l => l.HasMeta("arranged_award"));
            Require(receipt.Text == decision.Sentence + "\n" + decision.Now && shell.AwardScroll.GetGlobalRect().Encloses(receipt.GetGlobalRect()),
                "The fresh buy column opens with the exact committed sentence, fully visible.");
            await Capture("arranged");
            Require(requestScroll.ScrollVertical == 0 && requestScroll.GetGlobalRect().Encloses(rows.GetChild<Control>(0).GetGlobalRect()),
                "The new arrangement and its actual state are on screen at the top.");
            // A fresh form still offers real structural refusals, with their explanations in the list itself.
            shell.GoToMarket(shell.SelectedCard!);
            Pick(shell.InstrumentInput, "bpaCall");
            var blocked = shell.CurrentAwardChoices!.Vendors.First(v => !v.Available && v.Reason is not null);
            var index = Enumerable.Range(1, shell.VendorInput.ItemCount - 1)
                .Single(i => shell.VendorInput.GetItemMetadata(i).AsString() == blocked.Id);
            Require(shell.VendorInput.IsItemDisabled(index) && shell.VendorInput.GetItemText(index).Contains(" — ")
                && !shell.VendorInput.GetItemText(index).Contains("(unavailable)")
                && shell.VendorInput.GetItemTooltip(index) == blocked.Reason,
                "The disabled supplier row states the Sim's actual refusal.");
            foreach (var refusal in shell.CurrentAwardChoices.Vendors.Where(v => !v.Available))
            {
                var item = Enumerable.Range(1, shell.VendorInput.ItemCount - 1)
                    .Single(i => shell.VendorInput.GetItemMetadata(i).AsString() == refusal.Id);
                var reason = refusal.Reason ?? "Unavailable today.";
                if (reason.StartsWith(refusal.Label + " ", StringComparison.OrdinalIgnoreCase)) reason = reason[(refusal.Label.Length + 1)..];
                Require(shell.VendorInput.GetItemText(item).EndsWith(DeskUi.ListLabel(reason)), "Every supplier refusal is complete in the list.");
            }
            Require(!shell.VendorChoiceReason.Text.Contains("point at one"), "The count does not tell players to hover for an explanation.");
            Require(shell.CurrentAwardChoices.Vendors.Any(v => v.Reason?.Contains("This office cannot buy from it or add to that support.") == true),
                "The real host-nation refusal includes the sentence that was lost.");
            Require(PlayScreen.Descendants(shell.RequestBuyColumn).OfType<Label>().Any(l => l.Text == "Contract vehicle"),
                "The ordering vehicle cannot be mistaken for the office's truck.");
            await Frames(); shell.AwardScroll.EnsureControlVisible(shell.VendorInput); await Frames();
            var args = OS.GetCmdlineUserArgs();
            if (Array.IndexOf(args, "--screenshots") >= 0)
            {
                shell.VendorInput.ShowPopup(); await Frames();
                var popup = shell.VendorInput.GetPopup();
                Require(new Rect2I(0, 0, 1280, 800).Encloses(new Rect2I(popup.Position, popup.Size)),
                    $"The supplier explanations fit inside the native window: {popup.Position}, {popup.Size}.");
                await Capture("supplier-reasons"); shell.VendorInput.GetPopup().Hide();
            }
            await Frames(); shell.AwardScroll.EnsureControlVisible(shell.VehicleInput); await Capture("contract-vehicle");
            Require(DeskUi.DaySpan(17, 17) == "day 17" && DeskUi.DaySpan(17, 18) == "days 17–18", "One-day and multi-day gaps use readable dates.");
            PickAwardTarget(shell, "hubSite.food");
            Require(!PlayScreen.Descendants(shell.RequestBuyColumn).Any(n => n.HasMeta("arranged_award")), "A different target inherits no receipt.");
            PickAwardTarget(shell, water);
            Require(PlayScreen.Descendants(shell.RequestBuyColumn).OfType<Label>().Any(l => l.HasMeta("arranged_award") && l.Text == decision.Sentence + "\n" + decision.Now), "Returning to the same request retains its receipt for this morning.");
            Step(shell);
            Require(!PlayScreen.Descendants(shell.RequestBuyColumn).Any(n => n.HasMeta("arranged_award")), "A new morning clears the old receipt.");
            // A receipt follows the exact accepted record, even after newer decisions and a hand-over.
            var bundle = LinesOfSupport.Sim.Data.ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var input = LinesOfSupport.Sim.InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-3", "input-log.json"));
            var morning = LinesOfSupport.Sim.Replay.Run(bundle, input, stopAfterDay: 3);
            var save = shell.AutosavePathOverride + ".day4";
            File.WriteAllText(save, morning.ToSave().ToJson()); shell.LoadSavedGame(save); File.Delete(save);
            shell.DeveloperToggle.ButtonPressed = false;
            PickAwardTarget(shell, "tutForklift"); shell.GoToMarket("tutForklift"); Pick(shell.InstrumentInput, "bpaCall");
            Require(!shell.AwardButton.Disabled, "The day-four Harbor call is valid.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            var accepted = shell.Session.View!.Decisions("tutForklift").First();
            Require(accepted.StopWorkId is not null, "The call is in work for a hand-over.");
            shell.TeamButtons[accepted.TeammateId].EmitSignal(Button.SignalName.Pressed);
            shell.WorkAssignees[accepted.StopWorkId!].ChoiceButtons["avery"].EmitSignal(Button.SignalName.Pressed);
            shell.WorkAssignButtons[accepted.StopWorkId!].EmitSignal(Button.SignalName.Pressed);
            var current = shell.Session.View!.Decision(accepted.RecordId)!;
            Require(current.TeammateId == "avery" && current.RecordId == accepted.RecordId, "The decision keeps its identity through a real team hand-over.");
            var give = shell.WorkAssignButtons[accepted.StopWorkId!];
            var pickReason = give.GetParent().GetChildren().OfType<Label>().First(l => l.GetIndex() > give.GetIndex());
            var handoverHash = shell.Session.StateHash;
            Require(give.Disabled && pickReason.Text == "Pick who should take it." && pickReason.Visible
                && pickReason.GetThemeColor("font_color").IsEqualApprox(TabletopTheme.Ink), "The current holder prompts a pick in ordinary ink.");
            var teamScroll = PlayScreen.Descendants(shell.TeamPanel).OfType<ScrollContainer>().Single();
            await Frames(); teamScroll.EnsureControlVisible(pickReason); await Capture("day4-handover-prompt");
            var handoverPicker = shell.WorkAssignees[accepted.StopWorkId!];
            handoverPicker.ChoiceButtons["you"].EmitSignal(Button.SignalName.Pressed);
            Require(!give.Disabled && !pickReason.Visible && pickReason.GetThemeColor("font_color").IsEqualApprox(TabletopTheme.Alert),
                "A real candidate enables the action with ordinary refusal colour restored for any Sim stop.");
            handoverPicker.ChoiceButtons["avery"].EmitSignal(Button.SignalName.Pressed);
            Require(give.Disabled && pickReason.Text == "Pick who should take it." && pickReason.GetThemeColor("font_color").IsEqualApprox(TabletopTheme.Ink)
                && shell.Session.StateHash == handoverHash, "Returning to the holder restores the neutral prompt without acting.");
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);
            // An unrelated, newer decision must never become this receipt.
            Require(shell.SendAction(new LinesOfSupport.Sim.Core.AskAboutAction { Day = 4, Card = "tutWater", Person = "silva", Assignee = "novak" }).Accepted, "A newer unrelated ask is accepted.");
            PickAwardTarget(shell, "tutForklift"); shell.ShowRequests(); shell.GoToMarket("tutForklift"); await Frames();
            current = shell.Session.View!.Decision(accepted.RecordId)!;
            var liveReceipt = PlayScreen.Descendants(shell.RequestBuyColumn).OfType<Label>().Single(l => l.HasMeta("arranged_award"));
            Require(liveReceipt.Text == current.Sentence + "\n" + current.Now && !liveReceipt.Text.StartsWith("Arranged:") && liveReceipt.Text.Contains("Avery"),
                "The receipt rereads its own decision and Now line once, naming the new holder.");
            Require(shell.AwardScroll.GetGlobalRect().Encloses(liveReceipt.GetGlobalRect()), "The updated receipt is fully visible.");
            await Capture("day4-handover");
            shell.SendAction(new StopWorkAction { Day = 4, Work = accepted.StopWorkId! });
            current = shell.Session.View!.Decision(accepted.RecordId)!;
            Require(PlayScreen.Descendants(shell.RequestBuyColumn).OfType<Label>().Single(l => l.HasMeta("arranged_award")).Text == current.Sentence + "\n" + current.Now
                && current.Now.Contains("Stopped"), "The same receipt refreshes after stopping the work.");
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
