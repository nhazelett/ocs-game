using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class VendorsSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            Scenario(shell, "tutorial");
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var expected = Session.Create(bundle, 1);
            var tabs = shell.GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = GameShell.VendorsTab;
            shell.DeveloperToggle.ButtonPressed = false;
            Require(tabs.GetTabTitle(GameShell.VendorsTab) == "Vendors" && !tabs.HasNode("Vehicles"), "Vendors replaces Directory and the obsolete Vehicles page is removed.");
            Require(shell.VendorCardContents.Keys.Order().SequenceEqual(expected.View.VendorCards().Select(c => c.Id).Order()), "Exactly one card per engine vendor, excluding host-nation and partner support.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                for (var i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native vendor screenshot saved.");
            }
            string CardText(string vendor) => string.Join("\n", shell.VendorCardContents[vendor].GetChildren().OfType<Label>().Select(l => l.Text));
            var pickerHash = shell.Session.StateHash; Pick(shell.VendorCardPicker, "campServices"); Pick(shell.VendorCardPicker, "harborBottling");
            Require(shell.Session.StateHash == pickerHash, "Choosing whose card to read never changes play.");
            PickAwardTarget(shell, "tutWater"); Pick(shell.VendorInput, "harborBottling");
            Require(shell.SupplierKnowledge!.Text.Contains("Cash: — · Card: —") && shell.SupplierKnowledge.Text.Contains("Supply per day: — · Available today: —"),
                "The award editor also blanks unknown payment and capacity instead of disclosing its raw numbers.");
            var blank = CardText("harborBottling");
            Require(blank.Contains("Cash: — · Card: —") && blank.Contains("Islands reached: —")
                && blank.Contains("Supply per day: —") && blank.Contains("Price per gal: — · Notice: —"), "An unmet vendor's unknown facts are visible blanks, not hidden or invented numbers.");
            Require(blank.Contains(expected.View.VendorCards().Single(c => c.Id == "harborBottling").StillToLearn!), "The exact engine sentence explains how to fill the blanks.");
            var hash = shell.Session.StateHash;
            await Capture("blank");
            Require(shell.Session.StateHash == hash, "Reading every vendor card is read only.");
            var meet = shell.VendorMeetButtons["harborBottling"];
            meet.EmitSignal(Button.SignalName.Pressed);
            Require(expected.Apply(new MeetVendorAction { Day = expected.Day, Vendor = "harborBottling" }).Accepted
                && shell.Session.StateHash == expected.StateHash(), "The card's meeting sends the exact existing visit action.");
            hash = shell.Session.StateHash; meet.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash && shell.VendorMeetButtons["harborBottling"].Disabled, "A duplicate meeting cannot queue more work.");
            while (expected.View.VendorMetDay("harborBottling") is null) { expected.AdvanceDay(); Step(shell); }
            tabs.CurrentTab = GameShell.VendorsTab; Pick(shell.VendorCardPicker, "harborBottling");
            var known = expected.View.VendorCards().Single(c => c.Id == "harborBottling");
            var sale = known.Sells.Single(); var text = CardText(known.Id);
            Require(shell.Session.StateHash == expected.StateHash() && text.Contains("Cash: yes · Card: no")
                && text.Contains(DeskUi.Money(sale.UnitPriceCents!.Value)) && text.Contains(sale.CapacityPerDay!.Value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " " + sale.Unit)
                && text.Contains(known.KnownFrom!), "A completed meeting fills in the exact price, supply, payment and source supplied by Sim.");
            Require(shell.SupplierKnowledge!.Text.Contains("Cash: yes · Card: no") && !shell.SupplierKnowledge.Text.Contains("Supply per day: —"),
                "The same award summary fills in after the engine reports learned terms and capacity.");
            Require(text.Contains("Supply per day: 5,000 gal") && text.Contains("Price per gal: " + DeskUi.Money(sale.UnitPriceCents!.Value)),
                "A consumable quote names its engine unit and groups thousands without adding another per-day charge.");
            Require(CardText("campServices").Contains("Price per beds per day:"), "An equipment quote retains its daily basis and the engine unit.");
            await Capture("known");
            Require(CardText("campServices").Contains(expected.View.VehicleName("campBpa")), "An existing BPA appears on its vendor's card.");
            shell.VendorMeetButtons["harborBottling"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "A completed meeting remains guarded.");

            Scenario(shell, "spoke-b"); expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            tabs.CurrentTab = GameShell.VendorsTab;
            shell.IdiqVendorButtons["vendorD"].EmitSignal(Button.SignalName.Pressed);
            Pick(shell.BpaCapabilityInput, "fuel");
            Require(shell.EstablishButton.Disabled, "Empty scope and a zero ceiling retain engine hard stops.");
            shell.BpaScopeInputs["delivery"].ButtonPressed = true;
            shell.IdiqCeilingInput.Value = 100000;
            shell.CustomIdiqGuarantee.ButtonPressed = true; shell.IdiqGuaranteeInput.Value = 10000;
            shell.CustomIdiqPeriod.ButtonPressed = true; shell.IdiqPeriodInput.Value = 20;
            Pick(shell.IdiqFundingInput, "om1");
            var action = shell.PendingIdiq!; var preview = expected.View.PreviewEstablish(action);
            Require(action.CeilingCents == 10000000 && action.MinimumGuaranteeCents == 1000000 && action.OrderingPeriodDays == 20
                && action.ScopeLines.SequenceEqual(new[] { "delivery" }) && action.Vendor == "vendorD" && action.FundingLine == "om1",
                "IDIQ controls build the exact typed cents, scope, period, vendor and funding action.");
            Require(preview.Accepted && !shell.EstablishButton.Disabled && shell.EstablishPreview!.TotalCents == preview.TotalCents && shell.EstablishPreview.ShopHoursMin == preview.ShopHoursMin && shell.EstablishPreview.EarliestStartMax == preview.EarliestStartMax && shell.EstablishPreview.SoftGaps.SequenceEqual(preview.SoftGaps) && shell.Session.StateHash == expected.StateHash(),
                "IDIQ terms preview without changing state and use the engine's complete preview.");
            await Capture("idiq");
            shell.EstablishButton.GrabFocus(); await Capture("idiq-preview");
            var stale = shell.EstablishButton;
            shell.VendorSetupBack.EmitSignal(Button.SignalName.Pressed);
            stale.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "A canceled setup cannot submit later.");
            shell.IdiqVendorButtons["vendorD"].EmitSignal(Button.SignalName.Pressed);
            stale.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "An obsolete form cannot submit a different setup.");
            Pick(shell.BpaCapabilityInput, "fuel"); shell.BpaScopeInputs["delivery"].ButtonPressed = true;
            shell.IdiqCeilingInput.Value = 100000; Pick(shell.IdiqFundingInput, "om1");
            action = shell.PendingIdiq!;
            var submit = shell.EstablishButton;
            Require(expected.Apply(action).Accepted, "The independent Sim accepts the submitted IDIQ fixture.");
            submit.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.PendingIdiq is null, "Submission sends the exact previewed action and closes the form.");
            hash = shell.Session.StateHash; submit.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash, "A repeated submit cannot create another contract.");
            var save = shell.Session.CreateSave(); GameSaveStore.Write(shell.AutosavePath, save); shell.LoadSavedGame(shell.AutosavePath);
            Require(shell.Session.StateHash == expected.StateHash(), "Save/resume preserves the pending setup and exact state.");
            while (expected.View.Vehicles.All(v => v.Id != action.Id) && !expected.Finished) { expected.AdvanceDay(); Step(shell); }
            Require(shell.Session.StateHash == expected.StateHash() && shell.VendorCardContents["vendorD"].GetChildren().OfType<Label>().Any(l => l.HasMeta("vehicle") && l.GetMeta("vehicle").AsString() == action.Id),
                "The completed IDIQ moves from setup work onto its vendor's card through ordinary day advance.");
            shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("VENDORS_SMOKE_PASS: exact nullable cards, meeting fill-in and guards, held vehicles, typed IDIQ preview/submit, stale controls, save/resume and completion.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("VENDORS_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
