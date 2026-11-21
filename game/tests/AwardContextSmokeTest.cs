using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class AwardContextSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            await PaneSmokeChecks.Run(this);
            await AwardWriterSmokeChecks.Run(this);
            var shell = Shell(this);
            Scenario(shell, "tutorial"); Step(shell, 3);
            PickAwardTarget(shell, "tutForklift");
            Pick(shell.VendorInput, "harborEquipment"); Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "equipBpa");
            PickPriorities(shell, "fast", "good");
            var view = shell.Session.View!; var hash = shell.Session.StateHash;
            var cors = view.CorChoices("mhe");
            Require(cors[0].Fits == true && cors[0].Name.Contains("Pratt"), "Independent Sim puts the unit that knows forklifts first.");
            Require(Enumerable.Range(1, shell.CorInput!.ItemCount - 1).Select(i => shell.CorInput.GetItemMetadata(i).AsString()).SequenceEqual(cors.Select(c => c.Id)), "The actual roster keeps Sim's best-first order.");
            Require(shell.CorReasonLabels.Select(l => l.Text).SequenceEqual(cors.Select(c => c.Why)), "Every COR's exact fit, load, odds and lag sentence is visible.");
            Require(shell.PendingAward!.Cor is null, "The player chooses; no COR is silently appointed.");
            foreach (var cor in cors)
            {
                Pick(shell.CorInput!, cor.Id);
                Require(shell.PendingAward!.Cor == cor.Id && shell.CorReasonLabels.Select(l => l.Text).SequenceEqual(cors.Select(c => c.Why)), "Choosing a named COR binds its id and preserves every reason.");
                Require(shell.AwardPreview!.SoftGaps.SequenceEqual(view.Preview(shell.PendingAward).SoftGaps), "Fit warnings are exactly Sim's preview.");
            }
            string Caption(SpinBox input) => input.GetParent().GetChild<Label>(input.GetIndex() - 1).Text;
            var editor = shell.CurrentActionEditor!;
            void Asked()
            {
                Require(Caption(shell.QuantityInput).EndsWith("Asked: " + editor.RequestedQuantity), "The customer's quantity stays beside the editable quantity.");
                Require(Caption(shell.StartInput).EndsWith("Asked: day " + editor.RequestedStartDay), "The customer's start stays beside the editable start.");
                Require(Caption(shell.DurationInput).EndsWith($"Asked: {editor.RequestedDurationDays} days"), "The customer's duration stays beside the editable duration.");
            }
            Asked();
            shell.QuantityInput.Value = 4; shell.StartInput.Value = 6; shell.DurationInput.Value = 12;
            Asked(); Require(shell.Session.StateHash == hash, "Inspecting reasons and changing the buy never changes the request or Sim state.");
            await Capture(shell.CorInput!, "cor-reasons");
            await Capture(shell.CorReasonLabels.Last(), "other-cor");
            await Capture(shell.QuantityInput, "customer-ask");
            var old = shell.CorInput!;
            PickAwardTarget(shell, "hubSite.food");
            Require(shell.CorReasonLabels.Count == 0, "A fresh target clears old COR reasons before terms are chosen.");
            Pick(shell.VendorInput, "campServices"); Pick(shell.InstrumentInput, "purchaseOrder");
            PickPriorities(shell, "fast", "good");
            Require(shell.CorInput!.GetItemMetadata(1).AsString() == shell.Session.View!.CorChoices("food")[0].Id
                && shell.CorReasonLabels[0].Text == shell.Session.View.CorChoices("food")[0].Why, "A different capability reorders the roster from the current Sim view.");
            Require(!Caption(shell.QuantityInput).Contains("Asked:") && !Caption(shell.DurationInput).Contains("Asked:"), "An own-initiative buy inherits no customer ask.");
            old.EmitSignal(OptionButton.SignalName.ItemSelected, 1);
            Require(shell.PendingAward!.Cor is null, "An obsolete COR control cannot change the new award.");
            Pick(shell.CorInput, shell.Session.View.CorChoices("food")[0].Id);
            shell.QuantityInput.Value = 10; shell.DurationInput.Value = 3;
            var candidate = shell.PendingAward!;
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var expected = LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave());
            var result = expected.Apply(candidate);
            Require(result.Accepted && !shell.AwardButton.Disabled, "Independent Sim accepts the exact chosen award: " + string.Join("; ", result.HardStops));
            var submit = shell.AwardButton; submit.EmitSignal(Button.SignalName.Pressed); submit.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "The real submit sends the COR-bearing award exactly once.");
            var saved = shell.Session.CreateSave();
            Require(LinesOfSupport.Sim.Replay.Resume(bundle, saved).StateHash() == expected.StateHash(), "The chosen COR survives save/resume unchanged.");
            Step(shell); expected.AdvanceDay();
            Require(shell.Session.StateHash == expected.StateHash(), "The next day agrees with independent Sim play.");
            await CheckMonitorChoices(shell);
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            GD.Print("AWARD_CONTEXT_SMOKE_PASS: best-first COR reasons, exact customer ask beside editable terms, pure changes, capability refresh, stale controls, exact award and save/resume.");
            GetTree().Quit();

            async Task Capture(Control focus, string suffix)
            {
                var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
                if (shot < 0) return;
                shell.DeveloperToggle.ButtonPressed = false; shell.ShowMarket();
                for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var scroll = shell.AwardScroll;
                if (suffix == "cor-reasons") scroll.ScrollVertical = Math.Max(0, (int)focus.Position.Y - 34);
                else scroll.EnsureControlVisible(suffix == "other-cor" ? focus : shell.DurationInput);
                for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var viewport = GetViewport().GetVisibleRect();
                Require(viewport.Encloses(scroll.GetGlobalRect()), "The input pane stays inside the window.");
                foreach (var item in focus.GetParent().GetChildren().OfType<Control>().Where(c => c.Visible))
                    Require(item.GetGlobalRect().Position.X >= scroll.GetGlobalRect().Position.X
                        && item.GetGlobalRect().End.X <= scroll.GetGlobalRect().End.X,
                        "Every input and complete wrapped sentence fits horizontally inside the pane.");
                Require(scroll.GetGlobalRect().Encloses(focus.GetGlobalRect()), "The selected control stays within the scroll pane.");
                if (suffix == "cor-reasons") Require(scroll.GetGlobalRect().Encloses(shell.CorReasonLabels[0].GetGlobalRect()), "The best COR and its complete reason fit together.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native context screenshot saved.");
                shell.DeveloperToggle.ButtonPressed = true;
            }
        }
        catch (Exception error) { GD.PushError("AWARD_CONTEXT_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
    private async Task CheckMonitorChoices(GameShell shell)
    {
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var log = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-9", "input-log-2.json"));
        var expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 6);
        var path = shell.AutosavePathOverride + ".monitor-day7";
        File.WriteAllText(path, expected.ToSave().ToJson()); shell.LoadSavedGame(path); File.Delete(path);
        shell.DeveloperToggle.ButtonPressed = false; shell.GoToMarket("tutWaste");
        var order = log.Actions.OfType<AwardAction>().Single(a => a.Day == 7);
        Pick(shell.VendorInput, order.Vendor); Pick(shell.InstrumentInput, order.Instrument);
        Pick(shell.FundingInput, order.FundingLine!); PickPriorities(shell, order.Priorities!.ToArray());
        shell.QuantityInput.Value = order.Quantity; shell.StartInput.Value = order.StartDay!.Value; shell.DurationInput.Value = order.DurationDays;
        var picker = shell.CorInput!;
        foreach (var c in expected.View.CorChoices("waste"))
        {
            var index = Enumerable.Range(1, picker.ItemCount - 1).Single(i => picker.GetItemMetadata(i).AsString() == c.Id);
            var odds = $"catches about {c.ReportPermille / 10m:0.#}%, reports " + (c.LagDays == 0 ? "the same day" : DeskUi.Count(c.LagDays, "day") + " late");
            Require(picker.GetItemText(index).StartsWith(c.Why.Split(" and ", 2, StringSplitOptions.None)[0] + " · ")
                && picker.GetItemText(index).Contains(c.Fits == true ? "knows waste removal" : "does not work with waste removal")
                && picker.GetItemText(index).EndsWith(odds) && picker.GetItemTooltip(index) == c.Why
                && shell.CorReasonLabels.Any(l => l.Text == c.Why), "Every monitor choice carries the Sim's fit words before its effective odds and lag; the complete sentence remains below.");
        }
        Require(shell.Session.StateHash == expected.StateHash() && shell.PendingAward!.Cor is null, "Reading monitor choices is pure and retains None until chosen.");
        for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        shell.AwardScroll.EnsureControlVisible(picker);
        for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--monitor-screenshots");
        if (at >= 0)
        {
            picker.ShowPopup(); for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var popup = picker.GetPopup();
            Require(new Rect2I(0, 0, 1280, 800).Encloses(new Rect2I(popup.Position, popup.Size)), "The complete monitor choices fit the native window.");
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-monitor.png") == Error.Ok, "Native open-monitor capture saved."); popup.Hide();
        }
        Pick(picker, order.Cor!);
        Require(shell.PendingAward!.Cor == order.Cor && shell.Session.StateHash == expected.StateHash(), "Choosing from the explained roster binds the exact monitor without committing.");
        GD.Print("MONITOR_CHOICES_SMOKE_PASS: exact ninth-play day-seven waste quote, every fit/odds/lag choice, full Sim reasons, None and exact selection.");
    }

}
