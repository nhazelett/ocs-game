using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

// DESK-122, exercised by the existing ShopVehicleSmokeTest CI scene.
internal static class BoatSmokeChecks
{
    internal static async Task Run(Node scene)
    {
        var shell = Shell(scene);
        ReturnToMenu(shell); shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
        shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
        foreach (var (id, toggle) in shell.PrepChoices) toggle.ButtonPressed = id is "gpcAccounts" or "systemAccess";
        shell.StartGameButton.EmitSignal(Button.SignalName.Pressed); FinishOpening(shell);
        shell.DeveloperToggle.ButtonPressed = false;
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
        var expected = LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave());
        var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
        var offers = shell.Session.View!.OwnInitiative.Where(o => o.CapabilityId == "portLift").ToArray();
        Require(offers.Select(o => o.SiteId).SequenceEqual(["spokeBSite", "spokeCSite"]), "Both off-island sites offer a boat, without a customer request.");
        shell.GoToOwnInitiative();
        var hash = shell.Session.StateHash;
        foreach (var offer in offers)
        {
            Pick(shell.InitiativeTargetInput!, offer.SiteId + "." + offer.CapabilityId);
            Require(shell.InitiativeDetails!.Text.StartsWith(offer.Why) && shell.InitiativeDetails.Text.Contains("Harbor Lighterage")
                && shell.CurrentActionEditor!.CardId is null && shell.PendingAward is null && shell.AwardButton.Disabled,
                "Each boat has the exact Sim explanation and seller and opens an uncommitted ordinary buy.");
        }
        var picker = shell.InitiativeTargetInput!;
        Require(Enumerable.Range(0, picker.ItemCount).Count(i => picker.GetItemText(i).StartsWith("Boat between islands")) == 2,
            "The initiative list names both boats in player language.");
        await Capture("list", picker);
        Pick(shell.InstrumentInput, "purchaseOrder"); Pick(shell.VendorInput, "harborLighterage");
        shell.QuantityInput.Value = 1; shell.StartInput.Value = 10; shell.DurationInput.Value = 21;
        PickPriorities(shell, "fast", "good");
        var boat = shell.PendingAward!;
        Require(boat is { Card: null, Site: "spokeCSite", Capability: "portLift", Vendor: "harborLighterage", Quantity: 1 },
            "The ordinary award carries exactly the boat destination and seller.");
        var quote = expected.View.Preview(boat);
        Require(quote.Accepted && !shell.AwardButton.Disabled && shell.AwardPreview!.TotalCents == quote.TotalCents
            && shell.AwardPreview.EarliestStartMax == quote.EarliestStartMax && shell.Session.StateHash == hash,
            "Selecting and quoting the boat is pure and displays the exact Sim quote.");
        await Capture("quote");
        Require(expected.Apply(boat).Accepted, "Independent Sim accepts the previewed boat.");
        var submit = shell.AwardButton; submit.EmitSignal(Button.SignalName.Pressed); submit.EmitSignal(Button.SignalName.Pressed); Match();
        To(3);
        PickAwardTarget(shell, "req0014"); shell.ShowMarket();
        Pick(shell.InstrumentInput, "purchaseOrder"); Pick(shell.VendorInput, "vendorG");
        shell.QuantityInput.Value = shell.Session.View!.RequirementEditor("req0014")!.PeakDemandUnits;
        shell.StartInput.Value = 12; shell.DurationInput.Value = 19; PickPriorities(shell, "fast", "good");
        var food = shell.PendingAward!;
        Require(expected.Apply(food).Accepted && !shell.AwardButton.Disabled, "The independent food purchase is valid.");
        shell.AwardButton.EmitSignal(Button.SignalName.Pressed); Match();
        var sawBoat = false;
        while (!shell.Session.View!.Finished)
        {
            Step(shell); expected.AdvanceDay(); Match();
            var facts = expected.View.MapDay();
            Require(map.Day!.Runs.SequenceEqual(facts.Runs) && map.Day.Held.SequenceEqual(facts.Held), "Map binds exact Sim deliveries and holds after each real day.");
            var run = facts.Runs.FirstOrDefault(r => r.ArrangementId == food.Id && r.LinkId == "lift:" + boat.Id);
            if (run is null || facts.Links.Any(l => l.FromIslandId == "hub" && l.ToIslandId == "spokeC" && l.Mode == "barge" && l.ScheduledToday)) continue;
            Require(run.FromIslandId == "hub" && run.ToIslandId == "spokeC" && run.Mode == "barge", "The bought boat carries Main Island food to Third Island off the ferry's schedule.");
            shell.CloseRequestWorkspace(); shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab;
            map.SetMovementProgress(.4f);
            Require(map.Tabletop!.FactLabels.Any(l => l.Node.Visible && l.Label.Text.StartsWith("Food service") && l.Node.GetChildCount() == 2),
                "The actual map renders the food crossing as a visible carrier with its load caption.");
            await Capture("map"); sawBoat = true; break;
        }
        Require(sawBoat, "The fixture reaches a real boat delivery on an unscheduled ferry day.");
        shell.GoToOwnInitiative(); Pick(shell.InitiativeTargetInput!, "spokeCSite.portLift");
        Require(shell.InitiativeDetails!.Text.Contains("A boat to Third Island is running today.") && !shell.Session.View!.HasShopVehicle,
            "The running boat names its island and is never called an office vehicle.");
        Require(LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave()).StateHash() == expected.StateHash(), "Boat and food survive save/replay exactly.");
        shell.QueueFree(); await Frames();
        GD.Print("BOAT_SMOKE_PASS: both offers, exact seller/reason/quote, once-only ordinary buy, food crossing off schedule, native map carrier, running destination and save/replay.");

        void Match() => Require(shell.Session.StateHash == expected.StateHash(), "Boat play matches independent Sim state.");
        void To(int day) { while (shell.Session.View!.Day < day) { Step(shell); expected.AdvanceDay(); Match(); } }
        async Task Frames() { for (var i = 0; i < 3; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string suffix, OptionButton? list = null)
        {
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--boat-screenshots");
            if (at < 0) return;
            await Frames();
            if (suffix == "quote") { shell.AwardScroll.EnsureControlVisible(shell.AwardCost); await Frames(); }
            list?.ShowPopup(); await Frames();
            if (suffix == "map") map!.SetMovementProgress(.4f);
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Native boat capture saved.");
            list?.GetPopup().Hide();
        }
    }
}
