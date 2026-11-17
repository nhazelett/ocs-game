using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class CommitPreviewSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            shell = Shell(this);
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            Label Cost(Button button, PlayerAction action)
            {
                var label = button.GetParent().GetChildren().OfType<Label>().First(l => l.GetIndex() > button.GetIndex() && l.HasMeta("action_cost") && l.GetMeta("action_cost").AsString() == action.GetType().Name);
                var p = shell.Session.View!.PreviewAny(action);
                var hours = p.ShopHoursMin == p.ShopHoursMax ? p.ShopHoursMin.ToString() : $"{p.ShopHoursMin} to {p.ShopHoursMax}";
                Require(label.Text.StartsWith(p.ShopHoursMax == 0 ? "No shop hours." : hours + (p.ShopHoursMax == 1 ? " hour" : " hours") + " of your time."), "The line under the committing button has the exact preview hours and whose time.");
                Require(label.GetIndex() > button.GetIndex() && label.Text.Length > 30, "The cost and effect are under the button before it is pressed.");
                return label;
            }
            async Task Frames() { for (var i = 0; i < 6; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Capture(string suffix, Control focus)
            {
                if (shot < 0) return;
                shell.DeveloperToggle.ButtonPressed = false; await Frames();
                var ancestor = focus.GetParent();
                while (ancestor is not null && ancestor is not ScrollContainer) ancestor = ancestor.GetParent();
                if (ancestor is ScrollContainer scroll) { scroll.EnsureControlVisible(focus); await Frames(); }
                Require(GetViewport().GetVisibleRect().Encloses(focus.GetGlobalRect()), "The complete review sentence is inside the window.");
                if (ancestor is ScrollContainer clip) Require(clip.GetGlobalRect().Encloses(focus.GetGlobalRect()), "The complete cost/effect sentence fits its scroll viewport.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native review capture saved.");
                shell.DeveloperToggle.ButtonPressed = true;
            }
            var before = shell.Session.StateHash;
            foreach (var contact in new[] { "finance", "meeting", "sco" })
            {
                tabs.CurrentTab = GameShell.TodayTab; shell.PeopleToggle.ButtonPressed = true; Pick(shell.PersonContactInput, contact);
                var key = contact;
                var item = shell.VisitOptions[key];
                await Capture(contact, Cost(shell.VisitButtons[key], item.Action));
            }
            Require(shell.Session.StateHash == before, "Contact previews spend neither hours nor money.");
            shell.NavigateBoardItem(new BoardItem("test", 0, BoardCategory.Report, BoardSeverity.Info, BoardLinkKind.SiteCapability, "spokeBSite.fuel", "", []));
            var coordinate = shell.PendingCoordination!;
            var coordinateCost = Cost(shell.CoordinateButton!, coordinate);
            Require(coordinateCost.Text.Contains(coordinate.Quantity.ToString()) && coordinateCost.Text.Contains("withdrawn"), "Coordination names quantity and its consequence in a sentence.");
            await Capture("coordinate", coordinateCost);
            tabs.CurrentTab = GameShell.TodayTab; shell.PeopleToggle.ButtonPressed = true; Pick(shell.PersonContactInput, "j4");
            await Capture("j4", Cost(shell.VisitButtons["j4"], shell.VisitOptions["j4"].Action));
            shell.VisitButtons["j4"].EmitSignal(Button.SignalName.Pressed);
            Require(!shell.PeopleToggle.ButtonPressed && shell.J4QuantityInput is null, "An accepted J4 request closes its form.");

            Scenario(shell, "tutorial"); Step(shell, 3); PickAwardTarget(shell, "tutForklift"); shell.ShowRequests();
            var helper = shell.Session.View!.RequirementEditor("tutForklift")!.WhoCanHelp!.First(h => h.Person?.Id == "dunn");
            await Capture("ask", Cost(shell.RequestHelperButtons["ask:dunn"], helper.Start!));
            shell.RequestHelperButtons["ask:dunn"].EmitSignal(Button.SignalName.Pressed); Step(shell);
            shell.RequirementMarketButton.EmitSignal(Button.SignalName.Pressed);
            Pick(shell.VendorInput, "harborEquipment"); Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "equipBpa");
            Require(shell.AwardAddButtons.ContainsKey("operator"), "The learned missing-part warning carries Add what you learned in the buying form.");
            await Capture("learned", shell.AwardAddButtons["operator"]);
            var learnedHash = shell.Session.StateHash; shell.AwardAddButtons["operator"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash != learnedHash && shell.Session.View!.RequirementEditor("tutForklift")!.Lines.Single(l => l.Id == "operator").On, "The quote's inline fix adds the exact learned part.");
            Require(!shell.AwardAddButtons.ContainsKey("operator"), "The inline fix disappears after the part is included.");
            Pick(shell.InstrumentInput, "purchaseOrder"); PickPriorities(shell, "fast", "good");
            Pick(shell.CorInput!, shell.Session.View!.CorChoices("mhe")[0].Id);
            var original = shell.PendingAward!;
            Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "equipBpa"); Pick(shell.InstrumentInput, "purchaseOrder");
            Require(shell.PendingAward!.FundingLine == original.FundingLine && shell.PendingAward.Cor == original.Cor && shell.PendingAward.Priorities!.SequenceEqual(original.Priorities!), "An arrangement round trip keeps applicable funding, pick and COR.");
            var hash = shell.Session.StateHash;
            var summary = shell.Session.View!.AwardSummary(shell.PendingAward!)!;
            Require(shell.AwardSummaryLabel.Visible && shell.AwardSummaryLabel.Text == summary.Sentence, "The fixed summary binds the Sim sentence verbatim.");
            await Capture("buy", shell.AwardSummaryLabel);
            var need = shell.Session.View.RequirementEditor(shell.CurrentActionEditor!.CardId!)!;
            shell.StartInput.Value = need.RequestedStartDay + need.DurationDays + 1;
            summary = shell.Session.View.AwardSummary(shell.PendingAward!)!;
            Require(summary.Attention && shell.AwardSummaryLabel.GetThemeColor("font_color").IsEqualApprox(TabletopTheme.Warning) && shell.AwardSummaryLabel.Text == summary.Sentence, "A start after the need ends uses Sim's amber attention state.");
            await Capture("late-buy", shell.AwardSummaryLabel);
            shell.QuantityInput.Value = 1000000;
            Require(!shell.AwardSummaryLabel.Visible && shell.AwardButton.Disabled, "A refused buy clears the previous summary.");
            Require(shell.Session.StateHash == hash, "Quote and attention checks are pure.");
            shell.ShowRequests(); shell.RequirementMarketButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.AwardScroll.ScrollVertical == 0, "Every opening of the buying form starts at its top.");

            Scenario(shell, "tutorial"); Replay(shell, "approach1 taught");
            void Water(long amount)
            {
                PickAwardTarget(shell, "tutWater"); shell.ShowMarket();
                Pick(shell.VendorInput, "harborBottling"); Pick(shell.InstrumentInput, "sf44");
                shell.QuantityInput.Value = amount; shell.StartInput.Value = 2; shell.DurationInput.Value = 7;
            }
            Water(4500); Require(!shell.AwardButton.Disabled, "The first water buy is accepted.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed); Water(500);
            summary = shell.Session.View!.AwardSummary(shell.PendingAward!)!;
            Require(summary.Attention && shell.AwardSummaryLabel.Text == summary.Sentence
                && shell.AwardSummaryLabel.GetThemeColor("font_color").IsEqualApprox(TabletopTheme.Warning), "A second buy on already covered days uses the exact Sim summary and amber state.");
            await Capture("covered-buy", shell.AwardSummaryLabel);
            Pick(shell.InstrumentInput, "purchaseOrder"); PickPriorities(shell, "fast", "good");
            Pick(shell.CorInput!, shell.Session.View.CorChoices("water")[0].Id); original = shell.PendingAward!;
            Pick(shell.InstrumentInput, "sf44");
            Require(shell.PendingAward!.Cor is null && shell.PendingAward.FundingLine is null && shell.PendingAward.Priorities is null, "A cash buy sends no dormant written terms.");
            Pick(shell.InstrumentInput, "purchaseOrder");
            Require(shell.PendingAward!.FundingLine == original.FundingLine && shell.PendingAward.Cor == original.Cor
                && shell.PendingAward.Priorities!.SequenceEqual(original.Priorities!), "A cash round trip restores applicable funding, COR and pick.");

            foreach (var (kind, replay, choice) in new[] { (CommitmentKind.Handshake, "approach1 as written", "ratify"), (CommitmentKind.OutOfScope, "approach3 uca turnkey with bridge", "deny") })
            {
                Scenario(shell, "spoke-b"); Replay(shell, replay);
                while (!shell.Session.View!.Finished && !shell.Session.View.Commitments.Any(c => c.Kind == kind && c.Unresolved)) Step(shell);
                var commitment = shell.Session.View.Commitments.First(c => c.Kind == kind && c.Unresolved);
                tabs.CurrentTab = GameShell.InboxTab;
                await Capture(choice, Cost(shell.CommitmentChoices[(commitment.Id, choice)], new ResolveCommitmentAction { Day = shell.Session.View.Day, Commitment = commitment.Id, Choice = choice }));
            }
            Scenario(shell, "spoke-b"); Replay(shell); Step(shell, 8);
            var running = shell.Session.View!.Arrangements.First(a => !a.IsCoordination && a.Status == ArrangementStatus.Active && shell.Session.View.TerminateBlocked(a.Id) is null);
            shell.OpenContractView(running.Id);
            await Capture("end-contract", Cost(shell.TerminateButtons[running.Id], new TerminateAction { Day = shell.Session.View.Day, Arrangement = running.Id }));
            shell.QueueFree(); shell = null; await Frames();
            GD.Print("COMMIT_PREVIEW_SMOKE_PASS: exact hours/whose and effects, visible costs, coordination quantity, accepted J4 closes, inline learned fix, preserved terms, pure buy summary and amber state, refused summary cleared, form opens at top."); GetTree().Quit();
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError("COMMIT_PREVIEW_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
