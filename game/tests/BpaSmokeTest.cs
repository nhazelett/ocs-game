using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class BpaSmokeTest : Node
{
    public override void _Ready()
    {
        try
        {
            var shell = Shell(this);
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            var hash = shell.Session.StateHash;
            shell.EstablishVendorButtons["vendorD"].EmitSignal(Button.SignalName.Pressed);
            Pick(shell.BpaCapabilityInput, "fuel");
            Require(shell.EstablishButton.Disabled && shell.EstablishPreview!.HardStops.Count > 0, "Empty scope is a Sim hard stop.");
            shell.BpaScopeInputs["delivery"].ButtonPressed = true;
            Require(!shell.EstablishButton.Disabled, "Offered delivery scope is accepted.");
            shell.CustomBpaLimit.ButtonPressed = true;
            shell.BpaLimitInput.Value = 3000000;
            Require(shell.EstablishButton.Disabled && shell.EstablishPreview!.HardStops.Any(s => s.Contains("threshold")), "Sim's limit stop is displayed and gates submission.");
            shell.BpaLimitInput.Value = 50000;
            var candidate = shell.PendingEstablish!;
            Require(candidate.CallLimitCents == 5000000, "Currency input converts to exact action cents.");
            var preview = expected.View.PreviewEstablish(candidate);
            Require(preview.Accepted && preview.ShopHoursMin == shell.EstablishPreview!.ShopHoursMin && preview.EarliestStartMax == shell.EstablishPreview.EarliestStartMax, "Setup timing is unchanged from Sim.");
            Require(shell.Session.StateHash == hash, "Every form change only previews.");
            var result = expected.Apply(candidate); Require(result.Accepted, "Sim accepts fixture.");
            shell.EstablishButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.GetNode<Label>("%BuildStatus").Text.Contains(result.Message), "Submit sends the exact previewed action and shows result.");
            Require(shell.Session.View!.Vehicles.All(v => v.Id != candidate.Id), "BPA is not ready before setup work finishes.");
            while (expected.View.Vehicles.All(v => v.Id != candidate.Id) && !expected.Finished) { expected.AdvanceDay(); Step(shell); }
            Require(shell.Session.StateHash == expected.StateHash(), "Setup completes through normal Sim day advance.");
            var rows = shell.VendorCardContents["vendorD"].GetChildren().OfType<Label>();
            Require(rows.Any(l => l.HasMeta("vehicle") && l.GetMeta("vehicle").AsString() == candidate.Id && l.Text.Contains("$50,000.00")), "Ready BPA appears on its vendor card with the actual limit.");
            Require(shell.Session.View.ActionEditor("spokeBSite", "fuel")!.Vehicles.Any(v => v.Id == candidate.Id && v.OpenToday), "The normal award editor can use the completed BPA.");
            shell.EstablishVendorButtons["vendorD"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.PendingEstablish!.Id != candidate.Id, "Next command cannot reuse an existing or in-work ID.");
            GD.Print("BPA_SMOKE_PASS: offered scope, pure timing preview, limit gating, exact submit and completed vehicle."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"BPA_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
