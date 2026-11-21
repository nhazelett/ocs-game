using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class VendorsSmokeTest : Node
{
    private async Task CheckNotice(GameShell shell)
    {
        Scenario(shell, "tutorial", preview: false); shell.DeveloperToggle.ButtonPressed = false;
        var view = shell.Session.View!; var hash = shell.Session.StateHash;
        var offer = view.VendorCards().Single(c => c.Id == "islandMart").Sells.Single(s => s.CapabilityId == "water");
        Require(offer.NoticeSays is not null, "Island Mart's day-one notice is known.");
        shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.VendorsTab;
        Pick(shell.VendorCardPicker, "islandMart");
        var facts = shell.VendorCardContents["islandMart"].GetChildren().OfType<Label>().Single(l => l.Text.Contains(offer.NoticeSays!));
        Check(facts); await Capture("vendor", facts, shell.VendorCardPicker.GetParent().GetParent<ScrollContainer>());
        PickAwardTarget(shell, "tutWater"); Pick(shell.VendorInput, "islandMart"); Pick(shell.InstrumentInput, "gpc"); shell.ShowMarket();
        Check(shell.SupplierNotice!); await Capture("supplier", shell.SupplierNotice!, shell.AwardScroll);
        Require(shell.Session.StateHash == hash, "Reading both notice explanations changes no simulation state.");
        void Check(Label label)
        {
            Require(label.Text.EndsWith("after a signed order\n" + offer.NoticeSays)
                && label.GetThemeColor("font_color").IsEqualApprox(TabletopTheme.Ink),
                "The exact Sim notice explanation follows the notice in ordinary ink.");
        }
        async Task Capture(string suffix, Control facts, ScrollContainer scroll)
        {
            for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            scroll.EnsureControlVisible(facts);
            for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(scroll.GetGlobalRect().Encloses(facts.GetGlobalRect()), "The full notice and explanation can be read together.");
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--notice-screenshots"); if (at < 0) return;
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Native notice capture saved.");
        }
    }

    private async Task CheckHeard(GameShell shell)
    {
        var input = InputLog.Load(Path.Combine(Path.GetDirectoryName(GamePaths.ScenarioRoot)!, "docs", "reviews", "plays", "2026-11-19", "input-log.json"));
        var path = Path.Combine(Path.GetTempPath(), "ocs-vendor-heard-" + Guid.NewGuid() + ".json");
        string? previous = null;
        try
        {
            foreach (var day in new[] { 4, 5, 8, 9 })
            {
                File.WriteAllText(path, (input with { SavedDay = day, Actions = input.Actions.Where(a => a.Day < day).ToList() }).ToJson());
                shell.DeveloperToggle.ButtonPressed = false; shell.LoadSavedGame(path);
                var view = shell.Session.View!; var hash = shell.Session.StateHash;
                shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.VendorsTab;
                Pick(shell.VendorCardPicker, "harborEquipment"); await Frames();
                var card = view.VendorCards().Single(c => c.Id == "harborEquipment");
                var text = string.Join("\n", shell.VendorCardContents[card.Id].GetChildren().OfType<Label>().Select(l => l.Text));
                Check(text, "Usual capacity per day:", card.Heard);
                Require(view.VendorCards().Where(c => c.Id != card.Id && c.Heard is null).All(c =>
                    !shell.VendorCardContents[c.Id].GetChildren().OfType<Label>().Any(l => previous is not null && l.Text.Contains(previous))),
                    "A different vendor never inherits Harbor's news.");
                if (day == 5)
                {
                    var facts = shell.VendorCardContents[card.Id].GetChildren().OfType<Label>().Single(l => card.Heard is not null && l.Text.Contains(card.Heard));
                    var scroll = shell.VendorCardPicker.GetParent().GetParent<ScrollContainer>();
                    scroll.EnsureControlVisible(facts); await Frames();
                    Require(scroll.GetGlobalRect().Encloses(facts.GetGlobalRect()), "The vendor's full capacity and news sentence can be read together.");
                    await Capture("day5-card");
                }
                PickAwardTarget(shell, "tutForklift"); Pick(shell.VendorInput, "harborEquipment");
                Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "equipBpa"); shell.ShowMarket(); await Frames();
                var option = shell.CurrentActionEditor!.Vendors.Single(v => v.Id == card.Id);
                Require(option.Heard == card.Heard, "The two surfaces bind the same current Sim news.");
                Check(shell.SupplierKnowledge!.Text, "Usual capacity per day:", option.Heard);
                if (day == 5)
                {
                    shell.AwardScroll.EnsureControlVisible(shell.SupplierKnowledge); await Frames();
                    Require(shell.AwardScroll.GetGlobalRect().Encloses(shell.SupplierKnowledge.GetGlobalRect()),
                        "The full supplier capacity and news sentence fit together above the pinned footer.");
                    await Capture("day5-supplier");
                }
                Require(shell.Session.StateHash == hash, "Reading cards and quote facts spends no hours or money.");
                previous = card.Heard ?? previous;
            }
        }
        finally { File.Delete(path); }
        void Check(string text, string capacityPrefix, string? heard)
        {
            var lines = text.Split('\n'); var capacity = Array.FindIndex(lines, l => l.StartsWith(capacityPrefix, StringComparison.Ordinal));
            Require(capacity >= 0, "The existing capacity facts remain visible.");
            if (heard is not null)
                Require(lines[capacity + 1] == heard && lines.Count(l => l == heard) == 1,
                    "The Sim's heard sentence appears exactly once, directly under capacity.");
            else Require(previous is null || !text.Contains(previous), "Before the news and after it expires there is no stale heard sentence.");
        }
        async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string name)
        {
            var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--heard-screenshots"); if (index < 0) return;
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[index + 1] + "-" + name + ".png") == Error.Ok, "Native heard-news review capture saved.");
        }
    }

    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            await CheckNotice(shell);
            await CheckHeard(shell);
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
            string SupplierText() => string.Join("\n", shell.SupplierKnowledge!.GetParent().GetChildren().OfType<Label>().Select(l => l.Text));
            string CardText(string vendor) => string.Join("\n", shell.VendorCardContents[vendor].GetChildren().OfType<Label>().Select(l => l.Text));
            var pickerHash = shell.Session.StateHash; Pick(shell.VendorCardPicker, "campServices"); Pick(shell.VendorCardPicker, "harborBottling");
            Require(shell.Session.StateHash == pickerHash, "Choosing whose card to read never changes play.");
            PickAwardTarget(shell, "tutWater"); Pick(shell.VendorInput, "harborBottling");
            Require(SupplierText().Contains("Cash: — · Card: —") && shell.SupplierKnowledge.Text.Contains("Usual capacity per day: — · Uncommitted: —"),
                "The award editor also blanks unknown payment and capacity instead of disclosing its raw numbers.");
            var blank = CardText("harborBottling");
            Require(!blank.Contains("Counted from the signed order") && !SupplierText().Contains("Counted from the signed order"),
                "An unknown notice reveals no explanation on either surface.");
            Require(blank.Contains("Cash: — · Card: —") && blank.Contains("Islands reached: —")
                && blank.Contains("Usual capacity per day: —") && blank.Contains("Price per gal: —\nTime after a signed order: —"), "An unmet vendor's unknown facts are visible blanks, not hidden or invented numbers.");
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
            Require(sale.NoticeSays is not null && text.Contains("after a signed order\n" + sale.NoticeSays)
                && shell.SupplierNotice!.Text.EndsWith(sale.NoticeSays),
                "Meeting the vendor reveals the same authoritative notice explanation on both surfaces.");
            Require(shell.Session.StateHash == expected.StateHash() && text.Contains("Cash: yes · Card: no")
                && text.Contains(DeskUi.Money(sale.UnitPriceCents!.Value)) && text.Contains(sale.CapacityPerDay!.Value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " " + sale.Unit)
                && text.Contains(known.KnownFrom!), "A completed meeting fills in the exact price, supply, payment and source supplied by Sim.");
            Require(SupplierText().Contains("Cash: yes · Card: no") && !shell.SupplierKnowledge.Text.Contains("Usual capacity per day: —"),
                "The same award summary fills in after the engine reports learned terms and capacity.");
            Require(text.Contains("Usual capacity per day: 5,000 gal") && text.Contains("Price per gal: " + DeskUi.Money(sale.UnitPriceCents!.Value)),
                "A consumable quote names its engine unit and groups thousands without adding another per-day charge.");
            Require(CardText("campServices").Contains("Price per bed per day:"), "An equipment quote retains its daily basis and the engine unit.");
            await Capture("known");
            Pick(shell.VendorCardPicker, "campServices"); await Capture("bed-price");
            Require(CardText("campServices").Contains(expected.View.VehicleName("campBpa")), "An existing BPA appears on its vendor's card.");
            shell.VendorMeetButtons["harborBottling"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "A completed meeting remains guarded.");

            Scenario(shell, "tutorial"); Step(shell, 3); PickAwardTarget(shell, "tutForklift");
            Pick(shell.VendorInput, "harborEquipment"); shell.ShowMarket();
            Require(shell.SupplierKnowledge!.Text.Contains("Usual capacity per day:") && shell.SupplierKnowledge.Text.Contains("Uncommitted:")
                && !shell.SupplierKnowledge.Text.Contains("Available today"), "Capacity is labeled as usual supply, not a promise for today.");
            shell.DeveloperToggle.ButtonPressed = false;
            for (var i = 0; i < 6; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            shell.AwardScroll.EnsureControlVisible(shell.SupplierKnowledge); await Capture("capacity");

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
