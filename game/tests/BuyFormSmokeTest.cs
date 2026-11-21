using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class BuyFormSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            shell = Shell(this); Scenario(shell, "tutorial");
            shell.DeveloperToggle.ButtonPressed = false;
            PickAwardTarget(shell, "tutWater"); shell.ShowMarket();
            var hash = shell.Session.StateHash;
            Pick(shell.VendorInput, "harborBottling");
            Require(CardReason().Contains("Harbor Bottling does not accept the card"), "The disabled card choice explains the supplier's refusal in the arrangement picker.");
            Pick(shell.VendorInput, "islandMart"); Pick(shell.InstrumentInput, "gpc");
            Require(!CardReason().Contains("Harbor Bottling does not accept the card") && !shell.AwardBlockedReason.Text.Contains("Harbor Bottling does not accept the card"), "Changing suppliers clears the previous supplier's refusal line.");
            Order();
            Require(shell.PendingAward!.Quantity == shell.CurrentActionEditor!.PeakDemandUnits, "The buy starts at actual demand.");
            shell.QuantityInput.Value = 4500; shell.StartInput.Value = 2; shell.DurationInput.Value = 7;
            Require(shell.PendingAward!.Quantity == 4500 && shell.PendingAward.StartDay == 2 && shell.PendingAward.DurationDays == 7, "The early controls still edit the exact pending buy.");
            var expected = shell.Session.View!.Preview(shell.PendingAward);
            Require(expected.TotalCents == shell.AwardPreview!.TotalCents && !shell.AwardButton.Disabled, "The Sim quotes the edited water buy.");
            await Layout("water", shell.DurationInput);
            Pick(shell.InstrumentInput, "purchaseOrder"); Order();
            Require(shell.QuantityInput.Value == 4500 && shell.StartInput.Value == 2 && shell.DurationInput.Value == 7, "Arrangement changes keep the chosen amount and timing.");
            Require(shell.AwardBlockedReason.Text == "Pick two of fast, cheap and good below.", "A pick-two stop says where the choices are.");
            PickPriorities(shell, "fast", "good");
            Require(shell.CorInput is not null && shell.DurationInput.GetParent().GetParent().GetIndex() < shell.CorInput.GetParent().GetIndex()
                && shell.PriorityInputs.Values.All(p => shell.DurationInput.GetParent().GetParent().GetIndex() < p.GetParent().GetIndex()), "Amount and timing precede priorities and the COR.");
            await Layout("written-water", shell.DurationInput);
            Require(shell.Session.StateHash == hash, "Reading and editing every term leaves Sim state unchanged.");
            shell.ShowRequests(); shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed);
            PickAwardTarget(shell, "hubSite.water"); Pick(shell.VendorInput, "islandMart"); Pick(shell.InstrumentInput, "gpc"); Order();
            shell.QuantityInput.Value = 4500; shell.StartInput.Value = 2; shell.DurationInput.Value = 7;
            Require(!Caption(shell.StartInput).Contains("Asked:") && !Caption(shell.DurationInput).Contains("Asked:"), "Own-initiative timing has its own labels without a customer's dates.");
            await Layout("initiative", shell.DurationInput);
            Scenario(shell, "tutorial"); Step(shell, 4);
            PickAwardTarget(shell, "tutForklift"); shell.ShowMarket();
            Pick(shell.VendorInput, "harborEquipment"); Pick(shell.InstrumentInput, "purchaseOrder"); PickPriorities(shell, "fast", "good");
            await Layout("forklifts", shell.DurationInput);
            shell.QueueFree(); await Frames(); shell = null;
            GD.Print("BUY_FORM_SMOKE_PASS: early amount and timing, correct labels, supplier refusal refresh, pure exact previews, preserved terms and half-pane space with pinned summaries.");
            GetTree().Quit();

            string CardReason() => shell.InstrumentInput.GetItemTooltip(Enumerable.Range(1, shell.InstrumentInput.ItemCount - 1)
                .Single(i => shell.InstrumentInput.GetItemMetadata(i).AsString() == "gpc"));
            string Caption(SpinBox input) => input.GetParent().GetChild<Label>(input.GetIndex() - 1).Text;
            void Order()
            {
                var amounts = shell.QuantityInput.GetParent().GetParent();
                Require(amounts.Name == "BuyAmounts" && shell.StartInput.GetParent().GetParent() == amounts
                    && shell.DurationInput.GetParent().GetParent() == amounts
                    && amounts.GetIndex() > shell.InstrumentInput.GetParent().GetIndex()
                    && amounts.GetIndex() < shell.VendorInput.GetParent().GetIndex(), "The sizing row sits between arrangement and supplier.");
                Require(Caption(shell.StartInput).StartsWith("Start day") && Caption(shell.DurationInput).StartsWith("Duration (days)"), "Timing fields carry their own visible names.");
            }
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Layout(string suffix, Control focus)
            {
                shell.ShowMarket(); await Frames();
                var scroll = shell.AwardScroll;
                var pane = shell.RequestBuyColumn.GetGlobalRect();
                GD.Print($"BUY_FORM_LAYOUT {suffix}: scroll={scroll.Size.Y}, pane={pane.Size.Y}, summary={shell.AwardSummaryLabel.Size.Y}");
                if (shell.AwardWriterPicker is { } writer)
                    GD.Print($"WRITER_LAYOUT: picker={writer.Size}, rows={string.Join(",", writer.ChoiceButtons.Values.Select(b => b.Size.Y))}, summary={shell.AwardSummaryLabel.Size}, button={shell.AwardButton.Size}, reason={shell.AwardBlockedReason.Size} visible={shell.AwardBlockedReason.Visible}");
                Require(scroll.Size.Y >= pane.Size.Y / 2, "The form keeps at least half the pane, including the longer forklift summary.");
                Require(shell.AwardSummaryLabel.Text == shell.Session.View!.AwardSummary(shell.PendingAward!)!.Sentence, "The pinned summary retains the complete Sim sentence.");
                var button = shell.AwardButton.GetGlobalRect();
                scroll.EnsureControlVisible(focus); await Frames();
                Require(scroll.GetGlobalRect().Encloses(focus.GetGlobalRect()) && button == shell.AwardButton.GetGlobalRect()
                    && pane.Encloses(button) && pane.Encloses(shell.AwardSummaryLabel.GetGlobalRect()), "The sizing controls scroll into view while the complete summary and action stay pinned.");
                var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--screenshots");
                if (index < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[index + 1] + "-" + suffix + ".png") == Error.Ok, "The actual buy form is captured.");
            }
        }
        catch (Exception error) { GD.PushError("BUY_FORM_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
