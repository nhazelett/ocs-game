using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class CardCancellationSmokeTest : Node
{
    public override async void _Ready()
    {
        var path = Path.Combine(Path.GetTempPath(), "ocs-card-cancel-" + Guid.NewGuid() + ".json");
        try
        {
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var shell = Shell(this);
            foreach (var day in new[] { 1, 4 })
            {
                var expected = Fresh();
                Require(expected.Apply(new AwardAction { Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500,
                    StartDay = 2, DurationDays = 7, FundingLine = "om1" }).Accepted, "The tutorial card water buy is accepted.");
                while (expected.Day < day) expected.AdvanceDay();
                await Load(expected, "water");
                var hash = shell.Session.StateHash;
                var paid = expected.Arrangements.Single(a => a.Id == "water").ObligatedCents;
                var words = expected.View.EndWords("water")!;
                var button = shell.TerminateButtons["water"];
                Require(button.Text == words.Button + "…" && button.Text.StartsWith("Cancel this card buy"), "A card buy's page uses the Sim's cancellation words.");
                Require(Cost(button).Text.Contains(words.Says), "The page has the exact before/after-delivery explanation, including the card credit.");
                var picker = button.GetParent().GetChildren().OfType<AssigneePicker>().Single();
                var action = new TerminateAction { Day = expected.Day, Arrangement = "water", Assignee = "novak" };
                Require(picker.Selected == "you" && picker.Choices.SequenceEqual(expected.View.AssigneeChoices(action with { Assignee = null })), "Cancellation keeps the exact D17 picker and defaults to you.");
                picker.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
                Require(shell.Session.StateHash == hash && Cost(button).Text.Contains("Novak") && Cost(button).Text.Contains(words.Says), "Selecting the caller is pure and keeps the Sim explanation.");
                var preview = expected.View.PreviewAny(action);
                Require(Cost(button).Text.Contains($"Earliest result: day {preview.EarliestStartMin}") && Cost(button).Text.Contains(preview.ShopHoursMax.ToString()), "Hours and result day come from the selected caller's preview.");
                await Capture(day == 1 ? "before-first" : "day4", Cost(button));
                button.EmitSignal(Button.SignalName.Pressed);
                Require(shell.EndContractDialog.Visible && shell.EndContractDialog.Title == "Cancel this card buy?"
                    && shell.EndContractDialog.GetOkButton().Text == words.Button && shell.EndContractDialog.GetCancelButton().Text == "Keep this card buy"
                    && shell.EndContractDialog.DialogText.Contains(words.Says) && shell.EndContractDialog.DialogText.Contains("Novak")
                    && !shell.EndContractDialog.DialogText.Contains("End this contract") && shell.Session.StateHash == hash,
                    "Confirmation uses cancellation words, exact explanation and caller, without acting.");
                await Capture(day == 1 ? "confirm-first" : "confirm-day4", null);
                shell.EndContractDialog.Hide(); shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
                Require(shell.Session.StateHash == hash, "Keep this card buy sends nothing.");
                button.EmitSignal(Button.SignalName.Pressed);
                Require(expected.Apply(action).Accepted, "The independent cancellation is accepted.");
                shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                Require(shell.Session.StateHash == expected.StateHash() && shell.TerminateButtons["water"].Disabled,
                    "Confirmation queues the exact selected caller and prevents another cancellation.");
                shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed); button.EmitSignal(Button.SignalName.Pressed);
                Require(shell.Session.StateHash == expected.StateHash(), "Repeated confirmation and stale page clicks cannot duplicate the call.");
                while (expected.Work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null)) { expected.AdvanceDay(); Step(shell); }
                var cancelled = shell.Session.View!.Arrangements.Single(a => a.Id == "water");
                Require(shell.Session.StateHash == expected.StateHash() && cancelled.EndDay == day && cancelled.ObligatedCents < paid && expected.Claims.Count == 0,
                    "The call finishes on the intended day, credits the unperformed portion and matches Sim without a settlement.");
                Require(day != 1 || cancelled.Status == ArrangementStatus.Ended && cancelled.ObligatedCents == 0,
                    "Before first delivery the full charge comes back and the buy never starts.");
                Require(LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave()).StateHash() == expected.StateHash(), "The cancellation and caller survive save/replay.");
            }
            var contract = Fresh(); while (contract.Day < 4) contract.AdvanceDay();
            Require(contract.Apply(new AwardAction { Id = "fork", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3,
                StartDay = 8, DurationDays = 13, FundingLine = "om1" }).Accepted, "The written comparison contract is accepted.");
            while (contract.Day < 9) contract.AdvanceDay(); await Load(contract, "fork");
            var end = shell.TerminateButtons["fork"]; var before = shell.Session.StateHash;
            Require(end.Text == "End this contract…" && Cost(end).Text.Contains(contract.View.EndWords("fork")!.Says), "A written contract keeps its end-contract page words and preview.");
            await Capture("contract", Cost(end));
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.EndContractDialog.Title == "End this contract?" && shell.EndContractDialog.GetOkButton().Text == "End this contract"
                && shell.EndContractDialog.GetCancelButton().Text == "Keep this contract" && shell.EndContractDialog.DialogText.StartsWith("End this contract? Here is what stops."),
                "The reused confirmation resets all card-specific words for a written contract.");
            shell.EndContractDialog.Hide(); shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            Require(shell.Session.StateHash == before, "Cancelling the comparison confirmation is read only.");
            shell.QueueFree(); await Frames();
            GD.Print("CARD_CANCELLATION_SMOKE_PASS: before/after delivery words, exact caller quotes, confirmation reset, pure cancel, single execution, credit and save/replay."); GetTree().Quit();

            Session Fresh() => Session.Create(bundle, 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
            Label Cost(Button b) => b.GetParent().GetChildren().OfType<Label>().Single(l => l.HasMeta("action_cost"));
            async Task Frames() { for (var f = 0; f < 8; f++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Load(Session expected, string id)
            {
                File.WriteAllText(path, expected.ToSave().ToJson()); shell.LoadSavedGame(path); shell.DeveloperToggle.ButtonPressed = false; shell.OpenContractView(id); await Frames();
                Require(shell.Session.StateHash == expected.StateHash(), "The card/contract fixture loads through the real save boundary.");
            }
            async Task Capture(string suffix, Control? target)
            {
                await Frames();
                if (target is not null)
                    Require(GetViewport().GetVisibleRect().Encloses(target.GetGlobalRect()) && target.IsVisibleInTree(), "The committing explanation is visible in the actual viewport.");
                var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--screenshots"); if (at < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage(); Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Card cancellation screenshot saved.");
            }
        }
        catch (Exception error) { GD.PushError($"CARD_CANCELLATION_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { File.Delete(path); }
    }
}
