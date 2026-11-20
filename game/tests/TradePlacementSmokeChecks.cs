using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class TradePlacementSmokeChecks
{
    // Tutorial acceptance cases for DESK-124; called by PrioritiesSmokeTest in CI.
    internal static async Task Run(Node scene)
    {
        var shell = Shell(scene); Scenario(shell, "tutorial", preview: false); Step(shell, 3);
        shell.DeveloperToggle.ButtonPressed = false;
        PickAwardTarget(shell, "tutForklift"); shell.ShowMarket();
        Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VendorInput, "harborEquipment");
        var hash = shell.Session.StateHash;
        Require(shell.CurrentAwardChoices!.Vehicles.Count(v => v.Available) == 1
            && shell.PendingAward?.Vehicle == "equipBpa" && !shell.AwardButton.Disabled,
            "The tutorial's single compatible BPA is already selected and quoted.");
        Require(shell.PriorityInputs.Count == 0 && shell.PendingAward!.Priorities is null
            && shell.AwardTrade!.Text == shell.Session.View!.Preview(shell.PendingAward).Trade,
            "The call keeps its vehicle's own exact price/time sentence with no pick-two controls.");
        Pick(shell.VehicleInput, "");
        Require(shell.PendingAward!.Vehicle == "equipBpa", "Clearing a sole vehicle returns to its one usable entry.");
        await Capture("call", shell.VehicleInput.GetParent<Control>());
        Require(shell.Session.StateHash == hash, "The auto-selection and quote never act on the Sim.");
        Step(shell, 2); PickAwardTarget(shell, "tutForklift"); shell.ShowMarket();
        Pick(shell.InstrumentInput, "purchaseOrder"); Pick(shell.VendorInput, "coastalRentals"); PickPriorities(shell, "fast", "good");
        hash = shell.Session.StateHash;
        var trade = shell.AwardTrade!; var row = shell.PriorityInputs["fast"].GetParent<Control>();
        Require(trade.Text == shell.Session.View!.Preview(shell.PendingAward!).Trade && trade.Text.Contains("15%")
            && trade.GetParent() == row.GetParent() && trade.GetIndex() == row.GetIndex() + 1,
            "Day-six Fast and Good shows the exact trade and premium immediately after the toggles.");
        shell.DurationInput.Value = 8;
        Require(trade.Text == shell.Session.View.Preview(shell.PendingAward!).Trade && shell.Session.StateHash == hash,
            "Changing the price updates the inline premium without acting.");
        await Capture("written", trade, row);
        Pick(shell.InstrumentInput, "gpc");
        Require(shell.PriorityInputs.Count == 0 && (shell.AwardTrade is null || !shell.AwardTrade.Visible), "An immediate buy retains no stale written trade.");
        shell.QueueFree(); await Frames();
        GD.Print("TRADE_PLACEMENT_SMOKE_PASS: tutorial day-four single BPA, exact call terms, pure auto-selection, day-six inline Fast/Good trade and premium updates, immediate-buy clearing.");

        async Task Frames() { for (var i = 0; i < 5; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string suffix, Control focus, Control? above = null)
        {
            await Frames(); shell.AwardScroll.EnsureControlVisible(focus); await Frames();
            var clip = shell.AwardScroll.GetGlobalRect();
            Require(clip.Encloses(focus.GetGlobalRect()) && (above is null || clip.Encloses(above.GetGlobalRect())),
                "The complete choice and its explanation fit in the existing single buy scroll.");
            var pane = shell.RequestBuyColumn.GetGlobalRect();
            Require(pane.Encloses(shell.AwardSummaryLabel.GetGlobalRect()) && pane.Encloses(shell.AwardButton.GetGlobalRect()),
                $"The complete exact summary and action stay pinned inside the buy column ({suffix}: pane {pane}, summary {shell.AwardSummaryLabel.GetGlobalRect()}, action {shell.AwardButton.GetGlobalRect()}, text={shell.AwardSummaryLabel.Text}).");
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--trade-screenshots"); if (at < 0) return;
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Native trade screenshot saved.");
        }
    }
}
