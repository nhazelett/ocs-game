using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ActionSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            var original = shell.Session.StateHash;
            Pick(shell.VendorInput, "vendorC"); Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "bosBpa");
            shell.QuantityInput.Value = 360;
            Require(shell.Session.StateHash == original, "Changing every picker/quantity only previews, never mutates Sim.");
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            var candidate = shell.PendingAward!;
            var preview = expected.View.Preview(candidate);
            Require(preview.Accepted && shell.AwardPreview!.Accepted && !shell.AwardButton.Disabled, "The real BPA fixture can be awarded.");
            Require(preview.TotalCents == shell.AwardPreview!.TotalCents && preview.SoftGaps.SequenceEqual(shell.AwardPreview.SoftGaps), "Preview cost and soft gaps are unchanged from Sim.");
            shell.DurationInput.Value = 30;
            Require(shell.AwardPreview!.TotalCents != preview.TotalCents && shell.AwardPreview.TotalCents == expected.View.Preview(shell.PendingAward!).TotalCents, "Duration changes rerun the actual quote preview.");
            Require(shell.Session.StateHash == original, "Live duration previews do not mutate the session.");
            shell.DurationInput.Value = 60;
            shell.QuantityInput.Value = 100000;
            Require(!shell.AwardPreview!.Accepted && shell.AwardButton.Disabled && shell.AwardPreview.HardStops.Count > 0, "A hard stop disables Award live.");
            shell.QuantityInput.Value = 360;
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 2;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Screenshot saved.");
            }
            candidate = shell.PendingAward!;
            var result = expected.Apply(candidate); Require(result.Accepted, "Sim accepts the candidate.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "Award button sends the exact previewed action.");
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(result.Message), "ActionResult is shown.");
            var instruments = shell.CurrentActionEditor!.Instruments;
            foreach (var blocked in instruments.Where(i => i.UnavailableReason is not null))
            {
                var index = Enumerable.Range(0, shell.InstrumentInput.ItemCount).Single(i => shell.InstrumentInput.GetItemMetadata(i).AsString() == blocked.Id);
                Require(shell.InstrumentInput.IsItemDisabled(index) && shell.InstrumentInput.GetItemText(index).Contains(blocked.UnavailableReason!), "Unavailable instrument is greyed with the actual reason.");
            }
            GD.Print("ACTION_SMOKE_PASS: live pure previews, hard-stop gating, quote facts and exact award."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"ACTION_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
