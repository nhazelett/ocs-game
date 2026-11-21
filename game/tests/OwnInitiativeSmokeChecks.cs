using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class OwnInitiativeSmokeChecks
{
    internal static async Task Run(Node host, GameShell shell)
    {
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var input = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-10", "input-log-2.json"));
        var office = input.Actions.OfType<AwardAction>().Single(a => a.Card is null);
        var save = shell.AutosavePathOverride + ".initiative";
        try
        {
            var expected = LinesOfSupport.Sim.Replay.Run(bundle, input, stopAfterDay: 2);
            await Load(expected);
            shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed);
            Pick(shell.InitiativeTargetInput!, "hubSite.shopVehicle");
            Pick(shell.VendorInput, office.Vendor); Pick(shell.InstrumentInput, office.Instrument);
            shell.QuantityInput.Value = office.Quantity; shell.StartInput.Value = office.StartDay!.Value; shell.DurationInput.Value = office.DurationDays;
            Require(shell.PendingAward == office && !shell.AwardButton.Disabled, "The tenth play's exact day-three office buy is ready through the real form.");
            var commit = shell.AwardButton;
            Require(expected.Apply(office).Accepted, "The independent exact office buy is accepted.");
            commit.EmitSignal(Button.SignalName.Pressed);
            commit.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "A stale commit cannot buy again.");
            await Frames();
            var decision = expected.View.Decision(expected.Arrangements.Single(a => a.Id == office.Id).CauseId)!;
            var receipt = PlayScreen.Descendants(shell.RequestWorkspace).OfType<Label>().Single(l => l.HasMeta("arranged_award"));
            Require(shell.Session.StateHash == expected.StateHash() && shell.CurrentActionEditor!.CardId is null && shell.SelectedCard is null
                && shell.InitiativeTargetInput!.GetItemMetadata(shell.InitiativeTargetInput.Selected).AsString() == "hubSite.shopVehicle"
                && receipt.Text == decision.Sentence + "\n" + decision.Now && shell.AwardScroll.GetGlobalRect().Encloses(receipt.GetGlobalRect()),
                "Arrange support keeps the own-initiative page and shows the accepted decision visibly, without switching to a request.");
            Require(OwnBuys().Select(b => b.GetMeta("initiative_contract").AsString()).SequenceEqual(new[] { office.Id }),
                "Only the office's own buy appears, excluding inherited arrangements and the request's water buy.");
            await Capture("initiative-receipt");
            shell.RequestBackButton.EmitSignal(Button.SignalName.Pressed); shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(OwnBuys().Single().Text.Contains(decision.Sentence), "Returning to own initiative keeps the bought item and its decision sentence.");

            expected = LinesOfSupport.Sim.Replay.Run(bundle, input, stopAfterDay: 9);
            await Load(expected); shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            var hash = shell.Session.StateHash;
            var own = OwnBuys().Single();
            Require(own.Text.Contains(expected.View.Decision(expected.Arrangements.Single(a => a.Id == office.Id).CauseId)!.Now),
                "The day-ten entry reads the decision's current running status, not its old receipt.");
            await Capture("initiative-day10");
            shell.QuantityInput.Value = 2; var draftQuantity = shell.QuantityInput.Value;
            shell.RequestScroll.ScrollVertical = 100000; await Frames(); var scroll = shell.RequestScroll.ScrollVertical;
            own.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.ContractView.Visible && shell.SelectedArrangement == office.Id && shell.TerminateButtons.ContainsKey(office.Id)
                && shell.Session.StateHash == hash, "The own-buy entry opens its exact cancellable contract without changing Sim.");
            await Capture("initiative-contract");
            Back().EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(!shell.ContractView.Visible && shell.RequestWorkspace.Visible && shell.CurrentActionEditor!.CardId is null
                && shell.QuantityInput.Value == draftQuantity && shell.RequestScroll.ScrollVertical == scroll && shell.Session.StateHash == hash,
                "Back to your own buys preserves the page, draft, scroll and Sim state.");
            OwnBuys().Single().EmitSignal(Button.SignalName.Pressed); await Frames();
            var end = shell.TerminateButtons[office.Id];
            end.GetParent().GetChildren().OfType<AssigneePicker>().Single().ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.EndContractDialog.Visible && shell.Session.StateHash == hash, "The own-buy cancellation uses the ordinary confirmation.");
            var cancel = input.Actions.OfType<TerminateAction>().Single(a => a.Day == 10);
            Require(expected.Apply(cancel).Accepted, "The independent day-ten cancellation is accepted.");
            shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed); await Frames();
            Require(shell.Session.StateHash == expected.StateHash(), "The own-buy route commits the exact tenth-play cancellation and caller.");
            Back().EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(OwnBuys().Length == 1 && shell.CurrentActionEditor!.CardId is null, "Cancelling still returns to the own-purchase list.");
            expected.AdvanceDay(); Step(shell); await Frames();
            var after = OwnBuys().Single();
            Require(shell.Session.StateHash == expected.StateHash() && after.Text.Contains(expected.View.Decision(expected.Arrangements.Single(a => a.Id == office.Id).CauseId)!.Now),
                "The entry refreshes after the cancellation finishes and remains a route to the ended contract.");
            var stale = after;
            shell.RequestBackButton.EmitSignal(Button.SignalName.Pressed); stale.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.ContractView.Visible && shell.Session.StateHash == expected.StateHash(), "A hidden old own-buy link cannot navigate.");
            GD.Print("OWN_INITIATIVE_SMOKE_PASS: exact day-three receipt, persistent own-only list, day-ten contract/Back/cancellation, live ended status and stale controls.");
        }
        finally { File.Delete(save); }

        Button[] OwnBuys() => PlayScreen.Descendants(shell.RequestContents).OfType<Button>().Where(b => b.HasMeta("initiative_contract")).ToArray();
        Button Back() => PlayScreen.Descendants(shell.ContractView).OfType<Button>().Single(b => b.Text == "Back to your own buys");
        async Task Load(Session state)
        {
            File.WriteAllText(save, state.ToSave().ToJson()); shell.LoadSavedGame(save);
            shell.DeveloperToggle.ButtonPressed = false;
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.RequestsTab; await Frames();
        }
        async Task Frames() { for (var i = 0; i < 8; i++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string suffix)
        {
            var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--screenshots"); if (index < 0) return;
            await Frames(); await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = host.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[index + 1] + "-" + suffix + ".png") == Error.Ok, "The own-buy player screen is captured.");
        }
    }
}
