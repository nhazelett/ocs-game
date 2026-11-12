using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ShopVehicleSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed);
            foreach (var (id, toggle) in shell.PrepChoices) toggle.ButtonPressed = id == "gpcAccounts";
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed); FinishOpening(shell);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var expected = LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave());
            void Matches() => Require(shell.Session.StateHash == expected.StateHash(), "Office vehicle play equals independent Sim state.");
            var model = shell.Session.View!.OwnInitiative.Single();
            Require(!model.Have && !shell.Session.View.HasShopVehicle && shell.ShopVehicleStatus!.Text == "Office vehicle: none running today.", "The visits page starts with the real no-vehicle status.");
            var rep = shell.Session.View.Scenario.People.Single(p => p.Vendor == "sanitation");
            void VendorVisit()
            {
                shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab; shell.PeopleToggle.ButtonPressed = true;
                Pick(shell.PersonContactInput, "vendor:" + rep.Id);
            }
            VendorVisit(); var before = shell.VisitOptions["vendor"].Preview;
            Label Timing() => shell.ShopVehicleStatus!.GetParent().GetChildren().OfType<Label>()
                .Single(l => l.HasMeta("visit_timing") && l.GetMeta("visit_timing").AsString() == "vendor");
            Require(Timing().Text.StartsWith($"This takes {before.ShopHoursMin} shop hours.") && !Timing().Text.Contains("down from"),
                "An ordinary vendor meeting uses plural hours and claims no vehicle saving.");
            var oldRequests = shell.AwardRequestInput; var oldRequestIndex = oldRequests.Selected;
            Pick(oldRequests, "own-initiative");
            oldRequests.EmitSignal(OptionButton.SignalName.ItemSelected, oldRequestIndex);
            var key = model.SiteId + "." + model.CapabilityId;
            Require(shell.InitiativeTargetInput!.GetItemMetadata(shell.InitiativeTargetInput.Selected).AsString() == key, "The office's own purchase is offered first; an obsolete request control cannot replace it.");
            Require(shell.InitiativeDetails!.Text.Contains(model.Why) && model.Vendors.All(v => shell.InitiativeDetails.Text.Contains(shell.Session.View.NameOf(v))), "The reason and actual seller names come from the Sim model.");
            Require(shell.CurrentActionEditor!.CardId is null && shell.CurrentActionEditor.SiteId == model.SiteId && shell.CurrentActionEditor.CapabilityId == model.CapabilityId
                && shell.PendingAward is null && shell.AwardButton.Disabled, "The model opens its ordinary site/capability award without stale terms.");
            var hash = shell.Session.StateHash;
            await Capture(shell.InitiativeDetails, "office-purchase"); Matches();
            Pick(shell.VendorInput, model.Vendors.Single()); Pick(shell.InstrumentInput, "gpc");
            shell.QuantityInput.Value = 1; shell.StartInput.Value = 2; shell.DurationInput.Value = 5;
            var action = shell.PendingAward!; var independent = expected.View.Preview(action);
            Require(action.Card is null && action.Site == model.SiteId && action.Capability == model.CapabilityId && action.Instrument == "gpc"
                && action.Priorities is null && action.Cor is null, "Buying uses an ordinary AwardAction with no hidden request or written terms.");
            Require(shell.Session.StateHash == hash && shell.AwardPreview!.TotalCents == independent.TotalCents
                && shell.AwardPreview.ShopHoursMin == independent.ShopHoursMin && shell.AwardPreview.HardStops.SequenceEqual(independent.HardStops), "Quote inspection is pure and exactly Sim's.");
            Require(expected.Apply(action).Accepted && !shell.AwardButton.Disabled, "Independent Sim accepts the office vehicle purchase.");
            var submit = shell.AwardButton; submit.EmitSignal(Button.SignalName.Pressed); submit.EmitSignal(Button.SignalName.Pressed); Matches();
            while (!shell.Session.View!.HasShopVehicle && shell.Session.View.Day < 7) { Step(shell); expected.AdvanceDay(); Matches(); }
            Require(shell.Session.View.HasShopVehicle && shell.Session.View.OwnInitiative.Single().Have
                && shell.InitiativeDetails!.Text.Contains("The office has one running today."), "The own-initiative model refreshes only when the vehicle actually runs.");
            VendorVisit();
            var after = shell.VisitOptions["vendor"].Preview;
            var real = expected.View.PreviewVisit(shell.VisitOptions["vendor"].Action);
            Require(after.ShopHoursMin < before.ShopHoursMin && after.ShopHoursMin == real.ShopHoursMin && after.ShopHoursMax == real.ShopHoursMax,
                "The running vehicle's lower visit hours come from Sim, not Game arithmetic.");
            var timing = Timing();
            Require(timing.Text.StartsWith($"This takes {after.ShopHoursMin} shop hour with the office vehicle, down from the usual {before.ShopHoursMin} shop hours.")
                && shell.ShopVehicleStatus!.Text == "Office vehicle: running today.",
                "The singular hour, actual saving and running status are visible together; the baseline is the independent pre-purchase preview.");
            await Capture(timing, "running-visit"); Matches();
            shell.SaveButton.EmitSignal(Button.SignalName.Pressed); var saved = GameSaveStore.Read(shell.AutosavePath);
            Require(LinesOfSupport.Sim.Replay.Resume(bundle, saved).StateHash() == expected.StateHash(), "The actual saved vehicle reproduces the independent state.");
            shell.LoadSavedGame(shell.AutosavePath); VendorVisit(); Matches();
            Require(shell.ShopVehicleStatus!.Text == "Office vehicle: running today.", "Running status survives actual load and refresh.");
            var meet = shell.VisitOptions["vendor"].Action;
            Require(expected.Apply(meet).Accepted, "Independent Sim accepts the cheaper meeting.");
            shell.VisitButtons["vendor"].EmitSignal(Button.SignalName.Pressed); Matches();
            while (shell.Session.View!.HasShopVehicle) { Step(shell); expected.AdvanceDay(); Matches(); }
            Require(!shell.Session.View.OwnInitiative.Single().Have && shell.ShopVehicleStatus!.Text == "Office vehicle: none running today.", "Expiry clears the status without caching the benefit.");
            VendorVisit();
            Require(shell.VisitOptions["vendor"].Preview.ShopHoursMin == before.ShopHoursMin && !Timing().Text.Contains("down from"),
                "Expiry restores the original visit hours and removes the saving sentence.");
            Scenario(shell, "spoke-b");
            Require(shell.ShopVehicleStatus is null && shell.InitiativeDetails is null, "A scenario with no office offer inherits no vehicle status or explanation.");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            GD.Print("SHOP_VEHICLE_SMOKE_PASS: explicit model and seller, pure exact award, duplicate guard, real start/expiry, cheaper visit, save/load, exact meeting and scenario clearing.");
            GetTree().Quit();

            async Task Capture(Control focus, string suffix)
            {
                var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots"); if (shot < 0) return;
                shell.DeveloperToggle.ButtonPressed = false;
                if (suffix == "office-purchase") shell.ShowMarket();
                else shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab;
                for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var scroll = suffix == "office-purchase" ? shell.AwardScroll
                    : shell.GetNode<TabContainer>("%DeskTabs").GetNode<ScrollContainer>("Today/TodayPane/Body/Body");
                if (suffix == "office-purchase") scroll.EnsureControlVisible(focus);
                else scroll.EnsureControlVisible(focus);
                for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(GetViewport().GetVisibleRect().Encloses(scroll.GetGlobalRect()), "The actual scroll pane stays within the window.");
                Require(focus.GetGlobalRect().Position.X >= scroll.GetGlobalRect().Position.X && focus.GetGlobalRect().End.X <= scroll.GetGlobalRect().End.X,
                    "The complete office/visit explanation fits the pane horizontally.");
                Require(scroll.GetGlobalRect().Encloses(focus.GetGlobalRect()), "The complete explanation or visit timing is visible.");
                if (suffix == "running-visit") Require(scroll.GetGlobalRect().Encloses(shell.ShopVehicleStatus!.GetGlobalRect()), "Running status and the lower visit hours fit together.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native vehicle screenshot saved.");
                shell.DeveloperToggle.ButtonPressed = true;
            }
        }
        catch (Exception error) { GD.PushError("SHOP_VEHICLE_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
