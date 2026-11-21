using Godot;
using System.Globalization;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
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
                var hours = GameShell.PreviewHours(p);
                Require(label.Text.StartsWith(p.ShopHoursMax == 0 ? "No shop hours." : hours + " of your time."), "The line under the committing button has the exact preview hours and whose time.");
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
                Require(GetViewport().GetVisibleRect().Encloses(focus.GetGlobalRect()), $"The complete review sentence ({suffix}) is inside the window.");
                if (ancestor is ScrollContainer clip) Require(clip.GetGlobalRect().Encloses(focus.GetGlobalRect()), "The complete cost/effect sentence fits its scroll viewport.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native review capture saved.");
                shell.DeveloperToggle.ButtonPressed = true;
            }
            // DESK-142: the actual third play's named assignees must read the same everywhere.
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var log = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-20-3", "input-log.json"));
            var saved = Path.Combine(Path.GetTempPath(), "ocs-hours-" + Guid.NewGuid() + ".json");
            var expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 0);
            void LoadMorning()
            {
                try { File.WriteAllText(saved, expected.ToSave().ToJson()); shell.LoadSavedGame(saved); }
                finally { File.Delete(saved); }
            }
            LoadMorning(); PickAwardTarget(shell, "tutWater"); shell.ShowRequests();
            shell.RequestHelperAssignees["ask:ortiz"].ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
            var askButton = shell.RequestHelperButtons["ask:ortiz"];
            var askCost = askButton.GetParent().GetChildren().OfType<Label>().Single(l => l.HasMeta("action_cost"));
            Require(askCost.Text.StartsWith("1.5 hours of SrA Eli Novak's time."), "Novak's real day-one ask costs 1.5 hours beneath its committing button.");
            Require(shell.RequestHelperAssignees["ask:ortiz"].ChoiceButtons["novak"].Text.Contains("This takes them 1.5 hours;"), "The selected picker agrees with the committing cost.");
            Require(shell.Session.StateHash == expected.StateHash(), "Selecting an assignee and reading exact hours spend nothing.");
            await Capture("novak-ask", askCost);
            var ask = (AskAboutAction)log.Actions.First();
            Require(expected.Apply(ask).Accepted, "The independent day-one ask is accepted.");
            askButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.Session.View!.Team.Single(m => m.Id == "novak").HoursSpokenForToday == 1.5,
                "Committing sends the selected assignee and the strip charges the displayed 1.5 hours.");

            expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 6);
            foreach (var earlier in log.Actions.Where(a => a.Day == 7 && a is not AwardAction)) Require(expected.Apply(earlier).Accepted, "Day-seven setup is accepted.");
            LoadMorning(); PickAwardTarget(shell, "tutWaste"); shell.ShowRequests(); shell.GoToMarket("tutWaste");
            var order = log.Actions.OfType<AwardAction>().Single(a => a.Day == 7);
            Pick(shell.VendorInput, order.Vendor); Pick(shell.InstrumentInput, order.Instrument);
            Pick(shell.FundingInput, order.FundingLine!); Pick(shell.CorInput!, order.Cor!); PickPriorities(shell, order.Priorities!.ToArray());
            shell.QuantityInput.Value = order.Quantity; shell.StartInput.Value = order.StartDay!.Value; shell.DurationInput.Value = order.DurationDays;
            shell.AwardWriterPicker!.ChoiceButtons["avery"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.AwardTiming.Text.StartsWith("It would take 6.5 to 13 shop hours."), "Avery's real day-seven order keeps the half hour in the buy timing line.");
            Require(shell.AwardWriterPicker!.ChoiceButtons["avery"].Text.Contains("This takes them 6.5 to 13 hours;"), "The buy timing and selected writer agree.");
            Require(shell.Session.StateHash == expected.StateHash(), "Changing buy terms and writer remains a pure preview.");
            var precise = shell.AwardPreview!;
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                Require(GameShell.PreviewHours(precise) == "6.5 to 13 hours", "Half hours keep the game's decimal notation in every locale.");
                Require(GameShell.PreviewHours(precise with { HoursMinMilli = 1001, HoursMaxMilli = 1499 }) == "1.5 hours", "Milli-hours round up to the half hour, never down.");
                Require(GameShell.PreviewHours(precise with { HoursMinMilli = -1, HoursMaxMilli = -1, ShopHoursMin = 1, ShopHoursMax = 1 }) == "1 hour", "Whole-hour-only previews keep their singular wording.");
                Require(GameShell.PreviewHours(precise with { HoursMinMilli = -1, HoursMaxMilli = -1, ShopHoursMin = 0, ShopHoursMax = 0 }, shop: true) == "0 shop hours", "Zero-cost previews keep their whole-hour fallback.");
            }
            finally { CultureInfo.CurrentCulture = culture; }
            await Capture("avery-order", shell.AwardTiming);
            shell.QueueFree(); await Frames(); shell = Shell(this); tabs = shell.GetNode<TabContainer>("%DeskTabs");
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
            Require(coordinate.Quantity == 1170 && coordinateCost.Text.Contains("Requests 1,170 gal a day") && coordinateCost.Text.Contains(coordinate.Quantity.ToString("N0", CultureInfo.InvariantCulture)) && coordinateCost.Text.Contains("withdrawn"), "Coordination names quantity and its consequence in a sentence.");
            await Capture("coordinate", coordinateCost);
            tabs.CurrentTab = GameShell.TodayTab; shell.PeopleToggle.ButtonPressed = true; Pick(shell.PersonContactInput, "j4");
            await Capture("j4", Cost(shell.VisitButtons["j4"], shell.VisitOptions["j4"].Action));
            shell.VisitButtons["j4"].EmitSignal(Button.SignalName.Pressed);
            Require(!shell.PeopleToggle.ButtonPressed && shell.J4QuantityInput is null, "An accepted J4 request closes its form.");

            Scenario(shell, "tutorial"); Step(shell, 3); PickAwardTarget(shell, "tutForklift"); shell.ShowRequests();
            var helper = shell.Session.View!.RequirementEditor("tutForklift")!.WhoCanHelp!.First(h => h.Person?.Id == "dunn");
            await Capture("ask", Cost(shell.RequestHelperButtons["ask:dunn"], helper.Start!));
            shell.RequestHelperButtons["ask:dunn"].EmitSignal(Button.SignalName.Pressed); Step(shell);
            shell.GoToMarket(shell.SelectedCard!);
            Pick(shell.VendorInput, "harborEquipment"); Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "equipBpa");
            Require(shell.AwardAddButtons.ContainsKey("operator"), "The learned missing-part warning carries Add what you learned in the buying form.");
            var add = shell.AwardAddButtons["operator"];
            var addCost = add.GetParent().GetChildren().OfType<Label>().Last(l => l.HasMeta("action_cost"));
            var requestCost = shell.RequirementAddButtons["operator"].GetParent().GetChildren().OfType<Label>()
                .First(l => l.HasMeta("action_cost") && l.GetIndex() > shell.RequirementAddButtons["operator"].GetIndex());
            Require(addCost.Text.StartsWith("No shop hours.") && addCost.Text == requestCost.Text,
                "The quote and request offer the same explicit learned-part action and cost.");
            await Capture("learned", addCost);
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
            shell.ShowRequests(); shell.GoToMarket(shell.SelectedCard!); await Frames();
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
