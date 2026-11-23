using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class BuyFormSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            shell = Shell(this); Scenario(shell, "tutorial");
            shell.DeveloperToggle.ButtonPressed = false;
            PickAwardTarget(shell, "tutWater"); shell.ShowMarket();
            var hash = shell.Session.StateHash;
            Pick(shell.VendorInput, "harborBottling");
            Require(CardReason().Contains("Harbor Bottling does not accept the card"), "The disabled card choice explains the supplier's refusal in the arrangement picker.");
            await Frames(); shell.AwardScroll.ScrollVertical = 100000; await Frames();
            Require(shell.AwardBlockedControl == shell.InstrumentInput, "A missing arrangement links to the arrangement picker.");
            shell.AwardBlockedReason.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.AwardScroll.GetGlobalRect().Encloses(shell.InstrumentInput.GetGlobalRect()), "The arrangement reason reveals its offscreen picker.");
            await Layout("arrangement-link", shell.InstrumentInput);
            Pick(shell.VendorInput, "islandMart"); Pick(shell.InstrumentInput, "gpc");
            Require(!CardReason().Contains("Harbor Bottling does not accept the card") && !shell.AwardBlockedReason.Text.Contains("Harbor Bottling does not accept the card"), "Changing suppliers clears the previous supplier's refusal line.");
            Order();
            Require(shell.PendingAward!.Quantity == shell.CurrentActionEditor!.PeakDemandUnits, "The buy starts at actual demand.");
            shell.QuantityInput.Value = 4500; shell.StartInput.Value = 2; shell.DurationInput.Value = 7;
            Require(shell.PendingAward!.Quantity == 4500 && shell.PendingAward.StartDay == 2 && shell.PendingAward.DurationDays == 7, "The early controls still edit the exact pending buy.");
            var expected = shell.Session.View!.Preview(shell.PendingAward);
            Require(expected.TotalCents == shell.AwardPreview!.TotalCents && !shell.AwardButton.Disabled, "The Sim quotes the edited water buy.");
            await Layout("water", shell.DurationInput);
            Pick(shell.InstrumentInput, "purchaseOrder"); Order();
            Require(shell.QuantityInput.Value == 4500 && shell.StartInput.Value == 2 && shell.DurationInput.Value == 7, "Arrangement changes keep the chosen amount and timing.");
            Require(shell.AwardBlockedReason.Text == "Pick two of fast, cheap and good", "The pick-two reason does not point below the pinned footer.");
            Require(shell.AwardWriterPicker!.ChoiceButtons.Values.All(b => b.Text.EndsWith("date: pick two of Fast/Cheap/Good")),
                "Every writer names the choices needed for a date without promising the day after selection.");
            // The ninth play had a capacity refusal ahead of the missing-priority reason. The writer still has to
            // name the choices, and the full wording must be presented rather than clipped off every button.
            shell.QuantityInput.Value = 10000; await Frames();
            Require(shell.AwardBlockedReason.Text != "Pick two of fast, cheap and good" && shell.AwardButton.Disabled,
                "Another authoritative refusal occupies the reason line while priorities are missing.");
            var presented = new PlayScreen(shell).Read();
            Require(shell.AwardWriterPicker!.ChoiceButtons.Values.All(b => b.Text.EndsWith("date: pick two of Fast/Cheap/Good")
                && presented.Any(e => e.Control == b && e.Text == b.Text)),
                "Every complete writer row names the priority pair even when the reason line says something else.");
            await Layout("writer-other-stop", shell.DurationInput);
            shell.QuantityInput.Value = 4500;
            var beforeJump = shell.Session.StateHash;
            await Frames(); shell.AwardScroll.ScrollVertical = 100000; await Frames();
            var pickRow = shell.PriorityInputs["fast"].GetParent<Control>();
            Require(shell.AwardBlockedControl == pickRow && !shell.AwardBlockedReason.Disabled, "The reason links to the exact missing-pair control.");
            shell.AwardBlockedReason.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.AwardScroll.GetGlobalRect().Encloses(pickRow.GetGlobalRect()) && beforeJump == shell.Session.StateHash,
                "The blocking reason brings pick two into view without changing any terms or Sim state.");
            await Layout("pick-two-link", pickRow);
            PickPriorities(shell, "fast", "good");
            Require(shell.AwardWriterPicker!.ChoiceButtons.Values.All(b => !b.Text.Contains("date: pick two")), "Picking a pair restores authoritative done dates.");
            Require(PlayScreen.Descendants(shell.CorInput!.GetParent()).OfType<Label>().Any(l => l.Text == "Contract monitor (COR)"), "The buy form uses the same COR name as Mills and Payments.");
            Require(shell.CorInput is not null && shell.DurationInput.GetParent().GetParent().GetIndex() < shell.CorInput.GetParent().GetIndex()
                && shell.PriorityInputs.Values.All(p => shell.DurationInput.GetParent().GetParent().GetIndex() < p.GetParent().GetIndex()), "Amount and timing precede priorities and the COR.");
            await Layout("written-water", shell.DurationInput);
            Require(shell.Session.StateHash == hash, "Reading and editing every term leaves Sim state unchanged.");
            shell.ShowRequests(); shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed);
            PickAwardTarget(shell, "hubSite.water"); Pick(shell.VendorInput, "islandMart"); Pick(shell.InstrumentInput, "gpc"); Order();
            shell.QuantityInput.Value = 4500; shell.StartInput.Value = 2; shell.DurationInput.Value = 7;
            Require(!Caption(shell.StartInput).Contains("Asked:") && !Caption(shell.DurationInput).Contains("Asked:"), "Own-initiative timing has its own labels without a customer's dates.");
            await Layout("initiative", shell.DurationInput);
            Scenario(shell, "tutorial"); Step(shell, 4);
            PickAwardTarget(shell, "tutForklift"); shell.ShowMarket();
            Pick(shell.VendorInput, "harborEquipment"); Pick(shell.InstrumentInput, "purchaseOrder"); PickPriorities(shell, "fast", "good");
            await Layout("forklifts", shell.DurationInput);
            var input = LinesOfSupport.Sim.InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-6", "input-log.json"));
            var bundle = LinesOfSupport.Sim.Data.ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var morning = LinesOfSupport.Sim.Replay.Run(bundle, input, stopAfterDay: 5);
            var save = shell.AutosavePathOverride + ".day6";
            File.WriteAllText(save, morning.ToSave().ToJson()); shell.LoadSavedGame(save); File.Delete(save);
            shell.DeveloperToggle.ButtonPressed = false;
            PickAwardTarget(shell, "tutForklift"); shell.ShowMarket();
            Pick(shell.VendorInput, "coastalRentals"); Pick(shell.InstrumentInput, "gpc");
            Require(shell.AwardPreview is not null && !shell.SupplierNotice!.Visible
                && shell.AwardTiming.Text.Contains("The earliest start is day "), "The exact sixth-play card buy has one start forecast, without the signed-order notice.");
            await Layout("day6-card", shell.SupplierKnowledge!);
            Pick(shell.InstrumentInput, "purchaseOrder");
            Require(shell.AwardButton.Disabled && shell.AwardBlockedReason.Text == "Pick two of fast, cheap and good"
                && !shell.SupplierNotice!.Visible, "The written buy keeps its actual quote and plain pick-two reason.");
            await Layout("day6-pick-two", shell.PriorityInputs["fast"]);
            await CheckRecordedNavigation(shell);
            await CheckCorCandidates(shell);
            await CheckBpaAcceptanceWait(shell);
            shell.QueueFree(); await Frames(); shell = null;
            GD.Print("BUY_FORM_SMOKE_PASS: early amount and timing, correct labels, supplier refusal refresh, pure exact previews, preserved terms and half-pane space with pinned summaries.");
            GetTree().Quit();

            string CardReason() => shell.InstrumentInput.GetItemTooltip(Enumerable.Range(1, shell.InstrumentInput.ItemCount - 1)
                .Single(i => shell.InstrumentInput.GetItemMetadata(i).AsString() == "gpc"));
            string Caption(SpinBox input) => input.GetParent().GetChild<Label>(input.GetIndex() - 1).Text;
            void Order()
            {
                var amounts = shell.QuantityInput.GetParent().GetParent();
                Require(amounts.Name == "BuyAmounts" && shell.StartInput.GetParent().GetParent() == amounts
                    && shell.DurationInput.GetParent().GetParent() == amounts
                    && amounts.GetIndex() > shell.InstrumentInput.GetParent().GetIndex()
                    && amounts.GetIndex() < shell.VendorInput.GetParent().GetIndex(), "The sizing row sits between arrangement and supplier.");
                Require(Caption(shell.StartInput).StartsWith("Start day") && Caption(shell.DurationInput).StartsWith("Duration (days)"), "Timing fields carry their own visible names.");
            }
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Layout(string suffix, Control focus)
            {
                shell.ShowMarket(); await Frames();
                var scroll = shell.AwardScroll;
                var pane = shell.RequestBuyColumn.GetGlobalRect();
                GD.Print($"BUY_FORM_LAYOUT {suffix}: scroll={scroll.Size.Y}, pane={pane.Size.Y}, summary={shell.AwardSummaryLabel.Size.Y}");
                if (shell.AwardWriterPicker is { } writer)
                    GD.Print($"WRITER_LAYOUT: picker={writer.Size}, rows={string.Join(",", writer.ChoiceButtons.Values.Select(b => b.Size.Y))}, summary={shell.AwardSummaryLabel.Size}, button={shell.AwardButton.Size}, reason={shell.AwardBlockedReason.Size} visible={shell.AwardBlockedReason.Visible}");
                Require(scroll.Size.Y >= pane.Size.Y / 2, "The form keeps at least half the pane, including the longer forklift summary.");
                Require(shell.AwardSummaryLabel.Text == ((shell.PendingAward is { } pending ? shell.Session.View!.AwardSummary(pending)?.Sentence : null) ?? ""), "The pinned summary retains the complete Sim sentence.");
                var button = shell.AwardButton.GetGlobalRect();
                scroll.EnsureControlVisible(focus); await Frames();
                Require(scroll.GetGlobalRect().Encloses(focus.GetGlobalRect()) && button == shell.AwardButton.GetGlobalRect()
                    && pane.Encloses(button) && pane.Encloses(shell.AwardSummaryLabel.GetGlobalRect()), "The sizing controls scroll into view while the complete summary and action stay pinned.");
                var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--screenshots");
                if (index < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[index + 1] + "-" + suffix + ".png") == Error.Ok, "The actual buy form is captured.");
            }
        }
        catch (Exception error) { GD.PushError("BUY_FORM_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
    private async Task CheckRecordedNavigation(GameShell shell)
    {
        var bundle = LinesOfSupport.Sim.Data.ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var root = Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-9");
        var save = shell.AutosavePathOverride + ".navigation";
        var tabs = shell.GetNode<TabContainer>("%DeskTabs");
        var payments = tabs.GetNode<ScrollContainer>("Payments/LedgerContext");
        try
        {
            var first = LinesOfSupport.Sim.InputLog.Load(Path.Combine(root, "input-log-1.json"));
            var expected = LinesOfSupport.Sim.Replay.Run(bundle, first, stopAfterDay: 14);
            Load(); await Frames();
            var view = shell.Session.View!;
            var failure = view.MorningBoard().Items.First(i => i.Severity == LinesOfSupport.Sim.Views.BoardSeverity.Warning
                && view.Arrangements.Any(a => a.Id == i.LinkId && a.CapabilityId == "waste"));
            var failed = view.Arrangements.Single(a => a.Id == failure.LinkId);
            Require(failed.Instrument == "sf44" && failure.Text.Contains("equipment down"), "The recorded warning belongs to the SF 44 bridge, from its authoritative arrangement link.");
            shell.BoardDetailsToggle.ButtonPressed = true;
            var open = shell.MorningItems.GetChildren().OfType<Button>().Single(b => b.Text.Contains(failure.Text));
            foreach (var oldScroll in new[] { 0, 100000 })
            {
                tabs.CurrentTab = GameShell.PaymentsTab; await Frames(); payments.ScrollVertical = oldScroll; await Frames();
                tabs.CurrentTab = GameShell.TodayTab; open.EmitSignal(Button.SignalName.Pressed); await Frames();
                var row = tabs.GetNode<VBoxContainer>("Payments/LedgerContext/Contents").GetChildren().OfType<Button>()
                    .Single(b => b.HasMeta("arrangement") && b.GetMeta("arrangement").AsString() == failed.Id);
                Require(tabs.CurrentTab == GameShell.PaymentsTab && row.ButtonPressed
                    && payments.GetGlobalRect().Encloses(row.GetGlobalRect())
                    && Math.Abs(row.GlobalPosition.Y - payments.GlobalPosition.Y) <= 2,
                    $"The exact ninth-play day-fifteen failure opens its own complete entry at Payments' top after either old scroll: old={oldScroll}, now={payments.ScrollVertical}, tab={tabs.CurrentTab}, selected={row.ButtonPressed}, row={row.GetGlobalRect()}, scroll={payments.GetGlobalRect()}.");
                Require(shell.Session.StateHash == expected.StateHash(), "Opening the failed contract's payment entry is pure navigation.");
            }
            await Capture("failure-payments");
            open.EmitSignal(Button.SignalName.Pressed); tabs.CurrentTab = GameShell.TodayTab; await Frames();
            Require(tabs.CurrentTab == GameShell.TodayTab, "Deferred payment focus respects a player who immediately leaves Payments.");

            var second = LinesOfSupport.Sim.InputLog.Load(Path.Combine(root, "input-log-2.json"));
            expected = LinesOfSupport.Sim.Replay.Run(bundle, second, stopAfterDay: 4);
            foreach (var action in second.Actions.Where(a => a.Day == 5).TakeWhile(a => a is not LinesOfSupport.Sim.Core.AwardAction))
                Require(expected.Apply(action).Accepted, "The exact day-five add-part and stop actions are accepted.");
            Load(); shell.GoToMarket("tutForklift");
            Pick(shell.InstrumentInput, "purchaseOrder"); Pick(shell.VendorInput, "coastalRentals"); Pick(shell.FundingInput, "om1");
            shell.QuantityInput.Value = 3; shell.StartInput.Value = 8; shell.DurationInput.Value = 13; await Frames();
            shell.AwardScroll.ScrollVertical = 0;
            shell.AwardBlockedReason.EmitSignal(Button.SignalName.Pressed); await Frames();
            shell.PriorityInputs["fast"].ButtonPressed = true; shell.PriorityInputs["good"].ButtonPressed = true; await Frames();
            var monitorRow = shell.CorInput!.GetParent<Control>();
            var pairRow = shell.PriorityInputs["fast"].GetParent<Control>();
            var pane = shell.RequestBuyColumn.GetGlobalRect(); var scroll = shell.AwardScroll.GetGlobalRect();
            Require(scroll.Encloses(monitorRow.GetGlobalRect()) && scroll.Encloses(pairRow.GetGlobalRect()),
                "After choosing pick two, the full COR caption/picker and both chosen priorities fit above the pinned footer without manual scrolling.");
            Require(scroll.Size.Y >= pane.Size.Y / 2 && pane.Encloses(shell.AwardSummaryLabel.GetGlobalRect())
                && shell.AwardSummaryLabel.Text == shell.Session.View!.AwardSummary(shell.PendingAward!)!.Sentence,
                "The long forklift quote keeps its complete summary and at least half the column for the form.");
            Require(shell.PendingAward!.Priorities!.Order().SequenceEqual(new[] { "fast", "good" })
                && shell.Session.StateHash == expected.StateHash(), "Auto-scrolling preserves the exact chosen terms and Sim state.");
            await Capture("cor-pick-two");
            shell.PriorityInputs["good"].ButtonPressed = false; shell.PriorityInputs["good"].ButtonPressed = true;
            shell.CloseRequestWorkspace(); await Frames();
            Require(!shell.RequestWorkspace.Visible && shell.Session.StateHash == expected.StateHash(), "Deferred monitor scrolling respects closing the request.");
            GD.Print("BUY_NAVIGATION_SMOKE_PASS: ninth-play day-fifteen failure opens its exact Payments entry at the top after old scrolls; day-five full COR/pick-two rows, complete half-pane footer, pure navigation and stale navigation guards.");

            void Load()
            {
                File.WriteAllText(save, expected.ToSave().ToJson()); shell.LoadSavedGame(save); shell.DeveloperToggle.ButtonPressed = false;
                Require(shell.Session.StateHash == expected.StateHash(), "The ninth-play moment loads exactly through the save boundary.");
            }
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Capture(string suffix)
            {
                var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--navigation-screenshots"); if (at < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "The exact ninth-play navigation screenshot is saved.");
            }
        }
        finally { File.Delete(save); }
    }

}
