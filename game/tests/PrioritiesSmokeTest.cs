using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PrioritiesSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var expected = Session.Create(bundle, 1);
            void Written()
            {
                PickAwardTarget(shell, "req0007");
                Pick(shell.VendorInput, "vendorA"); Pick(shell.InstrumentInput, "purchaseOrder");
                shell.QuantityInput.Value = 60; shell.StartInput.Value = 1; shell.DurationInput.Value = 10;
            }
            Written();
            var hash = shell.Session.StateHash;
            Require(shell.PriorityInputs.Keys.Order().SequenceEqual(new[] { "cheap", "fast", "good" }), "Three named priority toggles are offered on a written contract.");
            Require(shell.PriorityInputs.Values.All(t => !t.ButtonPressed && !t.Disabled) && shell.AwardButton.Disabled
                && shell.PendingAward!.Priorities is { Count: 0 }, "No pair is picked for the player, and an empty pair cannot be awarded.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash, "A forced award with no pair does nothing.");
            shell.PriorityInputs["fast"].ButtonPressed = true;
            Require(shell.AwardButton.Disabled && shell.AwardPreview!.HardStops.Contains("Pick two of fast, cheap and good"), "One priority keeps the engine's exact hard stop.");
            foreach (var pair in new[] { new[] { "fast", "cheap" }, new[] { "fast", "good" }, new[] { "cheap", "good" } })
            {
                PickPriorities(shell, pair);
                var action = shell.PendingAward!;
                Require(action.Priorities!.Order().SequenceEqual(pair.Order()) && action.Priorities!.Count == 2, "The typed action has exactly the chosen pair.");
                var preview = expected.View.Preview(action);
                Require(preview.Accepted && !shell.AwardButton.Disabled && shell.AwardTrade!.Visible && shell.AwardTrade!.Text == preview.Trade
                    && System.Text.Json.JsonSerializer.Serialize(shell.AwardPreview) == System.Text.Json.JsonSerializer.Serialize(preview), "All preview costs, timing, stops, gaps and the trade come directly from Sim.");
                var third = shell.PriorityInputs.Single(t => !pair.Contains(t.Key)).Value;
                Require(third.Disabled && shell.PriorityInputs.Where(t => pair.Contains(t.Key)).All(t => !t.Value.Disabled), "Only the unchosen third is disabled; either chosen toggle can be changed.");
                third.ButtonPressed = true;
                Require(!third.ButtonPressed && shell.PendingAward!.Priorities!.Order().SequenceEqual(pair.Order()), "A forced third signal cannot sneak a third priority into the action.");
                Require(shell.Session.StateHash == hash, "Trying every pair and the blocked third leaves the game unchanged.");
                await Capture(string.Join("-", pair));
            }
            // Instrument metadata, rather than a list of ids in Game, controls the UI on written orders.
            Pick(shell.VendorInput, "vendorC"); Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "bosBpa");
            PickPriorities(shell, "fast", "good");
            Require(shell.PendingAward!.Priorities is { Count: 2 } && shell.AwardTrade!.Text == expected.View.Preview(shell.PendingAward).Trade,
                "A written BPA call uses the same typed pair and engine trade.");
            Written(); Pick(shell.InstrumentInput, "uca"); PickPriorities(shell, "cheap", "good");
            Require(shell.PendingAward!.Priorities is { Count: 2 } && shell.AwardTrade!.Text == expected.View.Preview(shell.PendingAward).Trade,
                "A written UCA uses the same typed pair and engine trade.");
            Written();
            shell.DurationInput.Value = 12;
            PickPriorities(shell, "fast", "good");
            Require(shell.AwardTrade!.Text == expected.View.Preview(shell.PendingAward!).Trade, "Changing the price reruns the premium's exact sentence.");
            shell.QuantityInput.Value = 100000;
            Require(shell.AwardButton.Disabled && shell.AwardPreview!.HardStops.Count > 0, "Two priorities cannot bypass a threshold or funds stop.");
            shell.QuantityInput.Value = 60; shell.DurationInput.Value = 10;
            var obsoleteToggle = shell.PriorityInputs["fast"];
            var obsoleteAward = shell.AwardButton;
            PickAwardTarget(shell, "spokeBSite.power");
            Pick(shell.VendorInput, "vendorA"); Pick(shell.InstrumentInput, "purchaseOrder");
            obsoleteToggle.EmitSignal(BaseButton.SignalName.Toggled, false); obsoleteAward.EmitSignal(Button.SignalName.Pressed);
            Require(shell.PendingAward!.Priorities is { Count: 0 } && shell.Session.StateHash == hash, "Changing target clears the pair and obsolete controls cannot affect the new form.");
            Written(); PickPriorities(shell, "fast", "good");
            var menuToggle = shell.PriorityInputs["fast"]; var menuAward = shell.AwardButton;
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            menuToggle.ButtonPressed = false; menuAward.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash && menuToggle.ButtonPressed, "Hidden controls on the Menu cannot edit or award a pair.");
            shell.ContinueGameButton.EmitSignal(Button.SignalName.Pressed);
            Scenario(shell, "spoke-b");
            Require(shell.PriorityInputs.Values.All(t => !t.ButtonPressed) && (shell.PendingAward?.Priorities is null or { Count: 0 }), "A new deployment has no hidden priority choice.");
            Require(shell.Session.Apply(new FindFinanceAction()).Accepted, "Finance fixture accepted."); Step(shell, 6);
            PickAwardTarget(shell, "req0007"); Pick(shell.VendorInput, "vendorA"); Pick(shell.InstrumentInput, "purchaseOrder");
            PickPriorities(shell, "fast", "good");
            Pick(shell.InstrumentInput, "sf44");
            Require(shell.PriorityInputs.Count == 0 && shell.PendingAward!.Priorities is null && shell.AwardTrade is { Visible: false, Text: "" }, "Switching to cash removes the pair and its trade.");
            Pick(shell.InstrumentInput, "purchaseOrder");
            Require(shell.PriorityInputs.Count == 3 && shell.PriorityInputs.Values.All(t => !t.ButtonPressed) && shell.AwardButton.Disabled,
                "Returning to written terms requires a fresh pair.");
            Scenario(shell, "spoke-b"); Written(); PickPriorities(shell, "fast", "good");
            hash = shell.Session.StateHash;
            var changedDayToggle = shell.PriorityInputs["fast"]; var changedDayAward = shell.AwardButton;
            Require(shell.Session.Apply(new FindFinanceAction()).Accepted, "External state change fixture accepted.");
            var changed = shell.Session.StateHash;
            changedDayToggle.ButtonPressed = false; changedDayAward.EmitSignal(Button.SignalName.Pressed);
            Require(changed != hash && shell.Session.StateHash == changed && changedDayToggle.ButtonPressed, "An outdated state hash invalidates both old actions and priority edits.");
            Scenario(shell, "spoke-b"); Written(); PickPriorities(shell, "fast", "good");
            var candidate = shell.PendingAward!; expected = Session.Create(bundle, 1);
            Require(expected.Apply(candidate).Accepted, "Independent Sim accepts the chosen written contract.");
            var submit = shell.AwardButton; submit.EmitSignal(Button.SignalName.Pressed); submit.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.Session.CreateSave().Actions.OfType<AwardAction>().Count() == 1,
                "The real button sends the exact chosen action once, even if its old signal repeats.");
            Require(shell.PendingAward!.Priorities is { Count: 0 } && shell.AwardButton.Disabled, "The next contract asks for a new pair.");
            shell.SaveButton.EmitSignal(Button.SignalName.Pressed);
            var saved = GameSaveStore.Read(shell.AutosavePath);
            Require(saved.Actions.OfType<AwardAction>().Single().Priorities!.SequenceEqual(new[] { "fast", "good" }), "The pair survives actual save serialization.");
            var resumed = LinesOfSupport.Sim.Replay.Resume(bundle, saved);
            Require(resumed.StateHash() == expected.StateHash(), "Loading the saved pair reproduces the exact state.");
            shell.LoadSavedGame(shell.AutosavePath); Step(shell); expected.AdvanceDay(); resumed.AdvanceDay();
            Require(shell.Session.StateHash == expected.StateHash() && resumed.StateHash() == expected.StateHash(), "Resumed play and the independent next day stay identical.");
            Scenario(shell, "spoke-b"); Replay(shell); Written(); PickPriorities(shell, "cheap", "good");
            hash = shell.Session.StateHash;
            Require(shell.Session.IsReplay, "Inspecting priorities keeps replay mode.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.Session.IsReplay && shell.Session.StateHash != hash && shell.Session.CreateSave().Actions.OfType<AwardAction>().Last().Priorities!.SequenceEqual(new[] { "cheap", "good" }),
                "A real chosen award takes control and logs the pair; preview did not.");
            while (!shell.Session.View!.Finished) Step(shell);
            Written(); PickPriorities(shell, "fast", "good");
            hash = shell.Session.StateHash;
            Require(shell.PriorityInputs.Values.All(t => t.Disabled) && shell.AwardButton.Disabled, "A finished game has no enabled priority or award action.");
            foreach (var toggle in shell.PriorityInputs.Values) toggle.EmitSignal(BaseButton.SignalName.Toggled, true);
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash, "Finished-game signals cannot mutate state.");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            GD.Print("PRIORITIES_SMOKE_PASS: all three exact Sim trades, required pairs, price updates, pure edits, stale/duplicate guards, cash clearing, save/resume, replay takeover and finished games.");
            GetTree().Quit();

            async Task Capture(string suffix)
            {
                var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--screenshots");
                if (index < 0) return;
                shell.DeveloperToggle.ButtonPressed = false; shell.ShowMarket();
                for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var toggle = shell.PriorityInputs["fast"];
                var scroll = shell.AwardScroll;
                scroll.EnsureControlVisible(toggle);
                for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(shell.PriorityInputs.Values.All(t => t.GetGlobalRect().Position.X >= scroll.GetGlobalRect().Position.X
                    && t.GetGlobalRect().End.X <= scroll.GetGlobalRect().End.X), "The three toggles fit the award form.");
                Require(shell.AwardTrade!.GetGlobalRect().End.X <= GetViewport().GetVisibleRect().End.X, "The wrapped trade stays in the preview pane.");
                var previewScroll = shell.AwardScroll;
                previewScroll.ScrollVertical = (int)shell.AwardTrade.GetParent<Control>().Position.Y;
                for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(previewScroll.GetGlobalRect().Encloses(shell.AwardTrade.GetGlobalRect())
                    && previewScroll.GetGlobalRect().Encloses(shell.AwardTiming.GetGlobalRect()),
                    "The complete trade and shop-hours cost fit together above the preview fold.");
                Require(previewScroll.GetGlobalRect().Encloses(shell.AwardCost.GetGlobalRect())
                    && shell.AwardCost.GetGlobalRect().End.Y + 8 <= previewScroll.GetGlobalRect().End.Y,
                    $"The complete quote, including its punctuation, clears the preview edge above the award button ({suffix}: quote {shell.AwardCost.GetGlobalRect()}, preview {previewScroll.GetGlobalRect()}).");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[index + 1] + "-" + suffix + ".png") == Error.Ok, "Native pair screenshot saved.");
                shell.DeveloperToggle.ButtonPressed = true;
            }
        }
        catch (Exception error) { GD.PushError("PRIORITIES_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
