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
            await CheckUnwrittenBuy();
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

        async Task CheckUnwrittenBuy()
        {
            var eleventh = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-11", "input-log-2.json"));
            var buy = eleventh.Actions.OfType<AwardAction>().Single(a => a.Card is null);
            var expected = LinesOfSupport.Sim.Replay.Run(bundle, eleventh, stopAfterDay: 1);
            await Load(expected); shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed);
            Pick(shell.InitiativeTargetInput!, "hubSite.mhe"); Pick(shell.InstrumentInput, buy.Instrument); Pick(shell.VendorInput, buy.Vendor);
            Pick(shell.VehicleInput!, buy.Vehicle!); Pick(shell.FundingInput!, buy.FundingLine!);
            shell.QuantityInput.Value = buy.Quantity; shell.StartInput.Value = buy.StartDay!.Value; shell.DurationInput.Value = buy.DurationDays;
            shell.AwardWriterPicker!.ChoiceButtons[buy.Assignee!].EmitSignal(Button.SignalName.Pressed);
            Require(shell.PendingAward == buy && !shell.AwardButton.Disabled, "The eleventh play's day-two own-initiative BPA call is ready through the real form.");
            var applied = expected.Apply(buy); Require(applied.Accepted, "Independent Sim accepts the eleventh play's buy.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            var record = applied.RecordId!;
            CheckEntry(expected, record, signed: false);
            var receipt = PlayScreen.Descendants(shell.RequestWorkspace).OfType<Label>().Single(l => l.HasMeta("arranged_award"));
            Require(receipt.Text == expected.View.Decision(record)!.Sentence + "\n" + expected.View.Decision(record)!.Now,
                "Receipt and list show the same accepted decision while no contract exists.");
            await Capture("initiative-day2-in-work");
            shell.RequestBackButton.EmitSignal(Button.SignalName.Pressed); shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            CheckEntry(expected, record, signed: false);
            var work = expected.View.Work.Single(w => w.CauseId == record);
            var handover = new AssignAction { Day = expected.Day, Work = work.Id, To = "you" };
            Require(expected.Apply(handover).Accepted && shell.SendAction(handover).Accepted, "The pending own buy can change writers.");
            await Frames(); CheckEntry(expected, record, signed: false);
            // A stopped buy remains a truthful historical entry and never acquires a contract link.
            var stopped = LinesOfSupport.Sim.Replay.Resume(bundle, expected.ToSave());
            Require(stopped.Apply(new StopWorkAction { Day = stopped.Day, Work = work.Id }).Accepted, "The own buy can be stopped before signing.");
            await Load(stopped); shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            CheckEntry(stopped, record, signed: false);
            await Load(expected); shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            while (!expected.Arrangements.Any(a => a.CauseId == record)) { expected.AdvanceDay(); Step(shell); await Frames(); }
            shell.GoToOwnInitiative(); await Frames();
            CheckEntry(expected, record, signed: true);
            var contract = OwnBuys().Single(); var hash = shell.Session.StateHash;
            contract.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.ContractView.Visible && shell.SelectedArrangement == buy.Id && shell.Session.StateHash == hash,
                "The same entry opens the exact contract once signed without changing Sim.");
            GD.Print("OWN_INITIATIVE_PENDING_SMOKE_PASS: eleventh-play day-two commit/receipt/list, live hand-over, stopped/reloaded work and signed contract link.");
        }
        void CheckEntry(Session expected, string record, bool signed)
        {
            var entries = PlayScreen.Descendants(shell.RequestContents).OfType<Control>().Where(c => c.HasMeta("initiative_decision")).ToArray();
            var decision = expected.View.Decision(record)!;
            Require(entries.Length == 1 && entries[0].GetMeta("initiative_decision").AsString() == record,
                "One accepted own-initiative buy is listed once, excluding the request buy, inherited arrangements and hand-over decisions.");
            var text = entries[0] switch { Label l => l.Text, Button b => b.Text, _ => "" };
            Require(text == decision.Sentence + "\n" + decision.Now + (signed ? "\nOpen its contract page →" : "")
                && (entries[0] is Button) == signed && shell.Session.StateHash == expected.StateHash(),
                "The own-buy entry uses the live Sim sentence/status, and only signed contracts have links.");
            Require(!PlayScreen.Descendants(shell.RequestContents).OfType<Label>().Any(l => l.Text == "Nothing bought on your own initiative yet."),
                "A buy in work never sits beside an empty-list claim.");
        }

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
