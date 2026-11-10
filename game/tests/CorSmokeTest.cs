using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class CorSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-cor-ui-" + Guid.NewGuid().ToString("N"));
        try
        {
            var shell = Shell(this);
            shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json");
            Require(shell.CorInput is null, "Scenario without a roster has no COR picker.");
            Scenario(shell, "spoke-b-90");
            Pick(shell.VendorInput, "vendorA"); Pick(shell.InstrumentInput, "purchaseOrder");
            shell.QuantityInput.Value = 60; shell.DurationInput.Value = 10;
            Require(shell.CorInput is not null && shell.PendingAward!.Cor is null, "Written award with a roster offers None by default.");
            Require(shell.AwardPreview!.SoftGaps.Any(g => g.StartsWith("No COR named")), "None preserves Sim's no-COR warning.");
            var unchanged = shell.Session.StateHash;
            foreach (var cor in shell.Session.View!.Cors)
            {
                var index = Enumerable.Range(0, shell.CorInput!.ItemCount).Single(i => shell.CorInput.GetItemMetadata(i).AsString() == cor.Id);
                Require(shell.CorInput.GetItemText(index).Contains(cor.Name) && shell.CorInput.GetItemText(index).Contains(shell.Session.View.CorSummary(cor)), "COR choice shows roster name and exact reporting summary.");
            }
            Pick(shell.CorInput!, "corOkafor");
            Require(shell.PendingAward!.Cor == "corOkafor" && shell.AwardPreview!.SoftGaps.Any(g => g.StartsWith("COR SGT Okafor is fair")), "Fair COR updates both action and real soft gap.");
            Pick(shell.CorInput!, "corReyes");
            Require(shell.PendingAward!.Cor == "corReyes" && !shell.AwardPreview!.SoftGaps.Any(g => g.Contains("COR")), "Good COR clears the reporting soft gap.");
            Require(shell.Session.StateHash == unchanged, "COR choice only previews.");
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b-90"));
            var expected = Session.Create(bundle, 1);
            var candidate = shell.PendingAward!;
            Require(expected.Apply(candidate).Accepted && !shell.AwardButton.Disabled, "Real written award fixture is accepted.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "Award sends the exact COR-bearing action.");
            while (!shell.Session.View!.Arrangements.Any(a => a.Id == candidate.Id)) { Step(shell); expected.AdvanceDay(); }
            var controls = shell.CorDesignations[candidate.Id];
            Require(controls.Submit.Disabled, "Already designated COR cannot be submitted again.");
            var original = shell.Session.StateHash;
            Pick(controls.Picker, "corOkafor");
            Require(!controls.Submit.Disabled && shell.Session.StateHash == original, "New ledger selection enables designation without mutating state.");
            Require(expected.Apply(new DesignateCorAction { Day = expected.Day, Arrangement = candidate.Id, Cor = "corOkafor" }).Accepted, "Sim accepts the designation fixture.");
            controls.Submit.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.GetNode<Label>("%BuildStatus").Text.Contains("designated"), "Ledger submits the exact designation and shows its result.");
            var ledger = shell.GetNode<TabContainer>("%DeskTabs").GetNode("Ledger/LedgerContext/Contents");
            Require(ledger.GetChildren().OfType<Label>().Any(l => l.HasMeta("cor_arrangement") && l.GetMeta("cor_arrangement").AsString() == candidate.Id && l.Text.Contains("SGT Okafor") && l.Text.Contains(expected.View.CorSummary(expected.View.Cors.Single(c => c.Id == "corOkafor")))), "Ledger shows the assigned COR and reporting quality.");
            Require(shell.CorDesignations[candidate.Id].Submit.Disabled, "Designation refresh disables the now-current choice.");
            if(!shell.CurrentAwardChoices!.Instruments.Single(i=>i.Id=="sf44").Available)
            {
                Require(shell.Session.Apply(new FindFinanceAction()).Accepted,"Establish the paying agent before testing a selectable cash instrument.");Step(shell,6);
            }
            Pick(shell.InstrumentInput, "sf44");
            Require(shell.CorInput is null && shell.PendingAward!.Cor is null, "Cash instrument hides and clears the COR field.");
            Pick(shell.InstrumentInput, "purchaseOrder");
            Pick(shell.FundingInput,shell.CurrentAwardChoices!.Funding.First(f=>f.Available).Id);
            Require(shell.CorInput is not null && shell.PendingAward!.Cor is null, "Returning to written terms starts at None without a hidden stale designation.");

            // Reporting fidelity is exercised on the authored no-COR replay, including the rotation.
            Scenario(shell, "spoke-b-90"); Replay(shell, "approach1 no cor");
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-no-cor.json"));
            var reportSeen = false; var hiddenSeen = false; var rotationSeen = false;
            while (true)
            {
                var view = shell.Session.View!; var hash = shell.Session.StateHash;
                var visible = shell.MorningItems.GetChildren().OfType<Button>().Select(b => b.Text)
                    .Concat(shell.PerformanceRows.FindChildren("*", "Label", true, false).OfType<Label>().Select(l => l.Text))
                    .Append(shell.GetNode<RichTextLabel>("%InboxRecords").Text).ToArray();
                foreach (var hidden in view.Records.Where(r => !view.Known(r)))
                {
                    hiddenSeen = true;
                    Require(!visible.Any(t => t.Contains(hidden.Text)), "Raw unreported vendor failures stay out of the board, inbox and performance record lists.");
                }
                foreach (var item in view.MorningBoard().Items.Where(i => view.Records.Any(r => r.Id == i.RecordId && r.Kind == RecordKind.CorReport)))
                {
                    var record = view.Records.Single(r => r.Id == item.RecordId);
                    var button = shell.MorningItems.GetChildren().OfType<Button>().Single(b => b.Text.EndsWith(item.Text));
                    if (view.Arrangements.Any(a => a.Id == record.Subject))
                    {
                        Require(item.LinkKind == BoardLinkKind.Arrangement && !button.Disabled, "COR or unit failure report links to its arrangement.");
                        button.EmitSignal(Button.SignalName.Pressed);
                        Require(shell.SelectedArrangement == record.Subject && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 5, "Report button opens the arrangement's ledger row.");
                        reportSeen = true;
                    }
                    else Require(button.Disabled, "Roster rotation notice remains informational per the Sim contract.");
                }
                var rotated = view.Cors.FirstOrDefault(c => c.RotatedDay is not null);
                if (rotated is not null)
                {
                    rotationSeen = true; Pick(shell.InstrumentInput, "purchaseOrder");
                    Require(shell.CorInput is not null && Enumerable.Range(0, shell.CorInput.ItemCount).Any(i => shell.CorInput.GetItemText(i).Contains(rotated.Name) && shell.CorInput.GetItemText(i).Contains(view.CorSummary(rotated))), "COR picker refreshes to the relief's current name and reporting quality.");
                }
                Require(shell.Session.StateHash == hash, "Inspecting reports and roster never changes replay state.");
                if (view.Finished) break;
                Step(shell);
            }
            Require(reportSeen && hiddenSeen && rotationSeen, "Replay covers reports, hidden failures and rotation.");
            Require(shell.Session.StateHash == LinesOfSupport.Sim.Replay.Run(bundle, log).StateHash(), "Reporting displays leave the authored replay unchanged.");
            foreach (var button in shell.DebriefPanel.GetChildren().OfType<Button>().Where(b => b.HasMeta("gap"))) button.EmitSignal(Button.SignalName.Pressed);
            foreach (var trace in shell.DebriefPanel.FindChildren("*", "Button", true, false).OfType<Button>().Where(b => b.HasMeta("gap_trace")).ToArray()) trace.EmitSignal(Button.SignalName.Pressed);
            var debriefText = shell.DebriefPanel.FindChildren("*", "Label", true, false).OfType<Label>().Select(l => l.Text).ToArray();
            var final = shell.Session.View!;
            foreach (var hidden in final.Records.Where(r => !final.Known(r)))
            {
                Require(!debriefText.Any(t => t.Contains(hidden.Text)), "Debrief timeline and expanded cause lists also filter unreported raw records.");
                Require(!shell.DebriefPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.HasMeta("event_record") && l.GetMeta("event_record").AsString() == hidden.Id), "Hidden raw failure is not listed as a timeline event.");
            }
            var knownEvents = shell.DisplayedDebrief!.Events.Where(e => final.Records.Any(r => r.Id == e.RecordId && final.Known(r))).ToArray();
            Require(knownEvents.Length > 0, "Fixture has known annotated events.");
            foreach (var item in knownEvents)
                Require(debriefText.Any(t => t.Contains(item.Text)) && shell.DebriefPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.HasMeta("event_record") && l.GetMeta("event_record").AsString() == item.RecordId), "Every known event selected by Sim remains in the debrief timeline.");
            Require(shell.CorDesignations.Values.All(c => c.Submit.Disabled), "Finished games offer no enabled designation action.");
            Scenario(shell, "spoke-b");
            Require(shell.CorInput is null && shell.PendingAward!.Cor is null && shell.CorDesignations.Count == 0, "Scenario switch clears the COR controls and field.");
            // M2R-39: release the shell's theme and portrait resources and let two frames pass before quitting, as the
            // other full-shell tests do. Quitting with the shell alive crashed Godot at process exit on CI (signal 4 or 11
            // after the pass line), which failed the job under pipefail.
            shell.QueueFree(); shell = null!; await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            // The crash is the engine finalizing managed StyleBoxFlat wrappers after it has begun shutting down ("Leaked unsafe
            // reference", csharp_script.cpp finalize). Collect them while the engine is still up.
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("COR_SMOKE_PASS: award and designation actions, roster summaries, report links, rotation and known-record filtering.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"COR_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }
}
