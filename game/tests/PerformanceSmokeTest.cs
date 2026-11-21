using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PerformanceSmokeTest : Node
{
    private async Task CheckDaySeven(GameShell shell)
    {
        var input = InputLog.Load(Path.Combine(Path.GetDirectoryName(GamePaths.ScenarioRoot)!, "docs", "reviews", "plays", "2026-11-19", "input-log.json"));
        var path = Path.Combine(Path.GetTempPath(), "ocs-desk113-" + Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, (input with { SavedDay = 7, Actions = input.Actions.Where(a => a.Day < 7).ToList() }).ToJson());
            shell.DeveloperToggle.ButtonPressed = false; shell.LoadSavedGame(path);
            for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var issue = shell.Session.View!.PerformanceIssues.Single(i => i.CapabilityId == "lodging");
            var alert = shell.MorningAlerts.GetChildren().OfType<PanelContainer>().Single(p => p.HasMeta("morning_shortfall")
                && p.GetMeta("morning_shortfall").AsString() == issue.SiteId + "." + issue.CapabilityId);
            var open = shell.TodayPerformanceLinks[issue.Id];
            Require(alert.IsAncestorOf(open) && open.Text == "Open vendor decision",
                "The recorded day-seven lodging alert carries its existing vendor decision directly below the reason.");
            var hash = shell.Session.StateHash;
            Require(shell.MorningHeading.Text.Contains(DeskUi.Count(shell.TodayPerformanceLinks.Count, "decision waiting", "decisions waiting"))
                && !shell.MorningHeading.Text.Contains("0 alerts"), "The morning counts the decisions actually waiting on Today.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            if (shot >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-day7-alert.png") == Error.Ok, "The recorded day-seven alert is captured.");
            }
            open.EmitSignal(Button.SignalName.Pressed);
            for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var cure = shell.PerformanceChoices[(issue.Id, "cure")];
            var heading = cure.GetParent().GetChild<Control>(0);
            var decisionScroll = shell.GetNode<ScrollContainer>("%DeskTabs/Inbox/Contents");
            Require(decisionScroll.GetGlobalRect().Encloses(heading.GetGlobalRect()), "The vendor decision opens at its context heading.");
            decisionScroll.EnsureControlVisible(cure);
            for (var i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.InboxTab
                && shell.GetNode<ScrollContainer>("%DeskTabs/Inbox/Contents").GetGlobalRect().Encloses(cure.GetGlobalRect())
                && shell.Session.StateHash == hash, "The alert opens the exact pending decision without spending hours.");
            cure.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.TodayPerformanceLinks.ContainsKey(issue.Id), "Starting the cure removes the pending decision link.");
            var after = shell.Session.StateHash; open.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == after, "A stale alert button cannot act on a changed game.");
        }
        finally { File.Delete(path); }
    }

    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            await CheckDaySeven(shell);
            Scenario(shell, "tutorial");
            while (shell.Session.View!.PerformanceIssues.Count == 0 && shell.Session.View.Day < 13) Step(shell);
            var firstIssue = shell.Session.View!.PerformanceIssues.First();
            Require(shell.Session.View.MorningBoard().New.Any(n => n.RecordId == firstIssue.CauseId)
                && (shell.MorningBrief.IsAncestorOf(shell.TodayPerformanceLinks[firstIssue.Id])
                    || shell.MorningAlerts.IsAncestorOf(shell.TodayPerformanceLinks[firstIssue.Id])),
                "The first vendor report carries its decision link beside its alert or news.");
            Step(shell, 13 - shell.Session.View.Day); shell.DeveloperToggle.ButtonPressed = false;
            var tutorialIssue = shell.Session.View!.PerformanceIssues.First(i => i.CureNoticeDay is null);
            var tutorialHash = shell.Session.StateHash;
            Require(shell.Session.View.Day == 13 && shell.TodayPerformanceLinks.ContainsKey(tutorialIssue.Id),
                "Tutorial morning 13 carries its actual vendor decision on Today.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                for (var frame = 0; frame < 10; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Vendor-decision screenshot saved.");
            }
            async Task CaptureAssigned(Button button, AssigneePicker picker, string name, string suffix)
            {
                var cost = button.GetParent().GetChildren().OfType<Label>().First(l => l.GetIndex() > button.GetIndex() && l.HasMeta("action_cost"));
                Require(cost.Text.Contains(name) && cost.Text.Contains("Earliest result: day"), "The cost line names the chosen teammate and result day.");
                Node? parent = button.GetParent(); while (parent is not null && parent is not ScrollContainer) parent = parent.GetParent();
                var scroll = (ScrollContainer)parent!;
                for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                scroll.EnsureControlVisible(cost);
                await ToSignal(GetTree().CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
                await Capture(suffix);
                Require(GetViewport().GetVisibleRect().Encloses(button.GetGlobalRect()) && GetViewport().GetVisibleRect().Encloses(cost.GetGlobalRect()), $"Assigned decision is inside the actual release viewport: {suffix}, button {button.GetGlobalRect()}, cost {cost.GetGlobalRect()}, scroll {scroll.GetGlobalRect()}.");
                Require(scroll.GetGlobalRect().Encloses(picker.ChoiceButtons[picker.Selected].GetGlobalRect()) && scroll.GetGlobalRect().Encloses(cost.GetGlobalRect()), "The chosen teammate and complete cost fit together in the viewport.");
            }
            var link = shell.TodayPerformanceLinks[tutorialIssue.Id];
            var todayScroll = shell.GetNode<ScrollContainer>("%DeskTabs/Today/TodayPane/Body/Body");
            await Capture("day13-before"); todayScroll.EnsureControlVisible(link); await Capture("day13-today");
            Require(todayScroll.GetGlobalRect().Encloses(link.GetGlobalRect()), "Today's vendor decision is reachable inside its scroll.");
            link.EmitSignal(Button.SignalName.Pressed); await Capture("day13-inbox");
            var inbox = shell.GetNode<ScrollContainer>("%DeskTabs/Inbox/Contents");
            var cureChoice = shell.PerformanceChoices[(tutorialIssue.Id, "cure")];
            var resourceChoice = shell.PerformanceChoices[(tutorialIssue.Id, "resource")];
            Require(inbox.GetGlobalRect().Encloses(cureChoice.GetParent().GetChild<Control>(0).GetGlobalRect()), "The tutorial vendor decision opens at the top of its context.");
            inbox.EnsureControlVisible(resourceChoice); await Capture("day13-actions");
            Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.InboxTab
                && inbox.GetGlobalRect().Encloses(cureChoice.GetGlobalRect())
                && inbox.GetGlobalRect().Encloses(resourceChoice.GetGlobalRect()), $"The link opens Inbox scrolled to both actual decisions: scroll {inbox.ScrollVertical}/{inbox.GetVScrollBar().MaxValue}; viewport {inbox.GetGlobalRect()}, cure {cureChoice.GetGlobalRect()}, re-source {resourceChoice.GetGlobalRect()}.");
            Require(shell.Session.StateHash == tutorialHash, "Opening the vendor decision is pure navigation.");
            var issueLabels = shell.PerformanceRows.FindChildren("*", "Label", true, false).OfType<Label>();
            Require(issueLabels.Any(l => l.Text.StartsWith(DeskUi.ListLabel(shell.Session.View.NameOf(tutorialIssue.Id))) && l.Text.EndsWith("No decision yet."))
                && !issueLabels.Any(l => l.Text.Contains("Decision: no decision")), "The issue heading and decision read as sentences.");
            shell.OpenContractView(tutorialIssue.ArrangementId);
            Require(shell.ContractPerformanceChoices[(tutorialIssue.Id, "cure")].Disabled == cureChoice.Disabled
                && shell.ContractPerformanceChoices[(tutorialIssue.Id, "resource")].Disabled == resourceChoice.Disabled,
                "The contract page offers the same cure and re-source decisions as Inbox.");
            var cause = shell.Session.View.Records.Single(r => r.Id == tutorialIssue.CauseId && shell.Session.View.Known(r));
            var reportText = PlayScreen.Descendants(shell.ContractView).OfType<Label>().Single(l => l.HasMeta("issue_record") && l.GetMeta("issue_record").AsString() == tutorialIssue.Id);
            var contractCure = shell.ContractPerformanceChoices[(tutorialIssue.Id, "cure")];
            var contractResource = shell.ContractPerformanceChoices[(tutorialIssue.Id, "resource")];
            var contractEnd = shell.TerminateButtons[tutorialIssue.ArrangementId];
            Require(reportText.Text == cause.Text && reportText.GetIndex() < contractCure.GetIndex(), "The contract shows the same known performance record as Inbox, before the cure decision.");
            for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(contractEnd.GetParent() == contractResource.GetParent()
                && contractEnd.GetIndex() > contractResource.GetIndex()
                && contractEnd.GetParent().GetChildren().OfType<AssigneePicker>().Any(p => p.GetIndex() > contractResource.GetIndex() && p.GetIndex() < contractEnd.GetIndex())
                && GetViewport().GetVisibleRect().Encloses(reportText.GetGlobalRect()), "The reason is visible and termination follows re-source with its teammate picker.");
            await Capture("contract-decisions");
            shell.CloseContractView();
            var tabs = shell.GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = GameShell.VendorsTab;
            var vendor = shell.Session.View!.VendorCards().Single(c => c.ArrangementIds.Contains(tutorialIssue.ArrangementId));
            Pick(shell.VendorCardPicker, vendor.Id); await Capture("vendor-card");
            foreach (var offer in vendor.Sells.Where(o => o.LeadTimeDays is not null))
                Require(shell.VendorCardContents[vendor.Id].GetChildren().OfType<Label>().Any(l => l.Text.Contains("Starts " + DeskUi.Count(offer.LeadTimeDays!.Value, "day") + " after a signed order")), "The vendor's lead time says what starts it.");
            var vendorScroll = shell.VendorCardPicker.GetParent().GetParent<ScrollContainer>();
            var scrollPosition = vendorScroll.ScrollVertical;
            shell.VendorCardContents[vendor.Id].GetChildren().OfType<Button>().Single(b => b.HasMeta("vendor_contract") && b.GetMeta("vendor_contract").AsString() == tutorialIssue.ArrangementId).EmitSignal(Button.SignalName.Pressed);
            await Capture("vendor-contract");
            var back = PlayScreen.Descendants(shell.ContractView).OfType<Button>().Single(b => b.Text == "Back to " + vendor.Name);
            back.EmitSignal(Button.SignalName.Pressed); await Capture("vendor-return");
            Require(!shell.ContractView.Visible && tabs.CurrentTab == GameShell.VendorsTab && shell.VendorCardPicker.GetItemMetadata(shell.VendorCardPicker.Selected).AsString() == vendor.Id
                && vendorScroll.ScrollVertical == scrollPosition && shell.Session.StateHash == tutorialHash, "Back returns to the same vendor card and scroll without changing Sim.");
            shell.OpenContractView(tutorialIssue.ArrangementId);
            Require(PlayScreen.Descendants(shell.ContractView).OfType<Button>().Any(b => b.Text == "Back to what we are paying for"), "An ordinary contract opening resets its return destination.");

            shell.ContractPerformanceChoices[(tutorialIssue.Id, "resource")].EmitSignal(Button.SignalName.Pressed);
            Require(!shell.ContractView.Visible && shell.MarketOpen && shell.CurrentActionEditor!.SiteId == tutorialIssue.SiteId
                && shell.CurrentActionEditor.CapabilityId == tutorialIssue.CapabilityId && shell.Session.StateHash == tutorialHash,
                "Contract re-source closes its page and opens the matching form without changing Sim.");
            Step(shell); Require(shell.TodayPerformanceLinks.ContainsKey(tutorialIssue.Id), "An undecided vendor problem remains on the next morning.");
            Require(shell.TodayPerformanceRows.Visible, "An older decision stays below New since yesterday.");
            var oldLink = shell.TodayPerformanceLinks[tutorialIssue.Id];
            await Capture("day14-before"); todayScroll.EnsureControlVisible(oldLink); await Capture("day14-today");
            cureChoice = shell.PerformanceChoices[(tutorialIssue.Id, "cure")];
            cureChoice.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.TodayPerformanceLinks.ContainsKey(tutorialIssue.Id), "A cure already in work no longer asks for the same decision on Today.");
            var afterCure = shell.Session.StateHash;
            oldLink.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == afterCure && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.TodayTab,
                "A stale Today link does not navigate or send another action.");
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b-90"));
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json"));
            Session Issue()
            {
                Scenario(shell, "spoke-b-90"); Replay(shell);
                while (shell.Session.View!.PerformanceIssues.Count == 0 && !shell.Session.View.Finished) Step(shell);
                Require(shell.Session.View.PerformanceIssues.Count > 0, "Replay reaches a performance issue.");
                return LinesOfSupport.Sim.Replay.Run(bundle, log, shell.Session.CompletedDay);
            }
            var expected = Issue();
            var issue = expected.View.PerformanceIssues.First();
            var record = expected.Records.Single(r => r.Id == issue.CauseId);
            Require(shell.PerformanceRows.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == record.Text), "Persistent inbox row shows the original issue record.");
            var hash = shell.Session.StateHash;
            shell.PerformanceChoices[(issue.Id, "resource")].EmitSignal(Button.SignalName.Pressed);
            Require(shell.RequestWorkspace.Visible && shell.CurrentActionEditor!.SiteId == issue.SiteId && shell.CurrentActionEditor.CapabilityId == issue.CapabilityId, "Re-source opens the matching action editor.");
            Require(shell.Session.StateHash == hash && shell.Session.IsReplay, "Opening re-source neither changes Sim nor takes control.");
            var result = expected.Apply(new CureNoticeAction { Day = expected.Day, Issue = issue.Id, Assignee = "avery" });
            Require(result.Accepted, "Cure fixture is accepted.");
            shell.OpenContractView(issue.ArrangementId);
            var selectedCure = shell.ContractPerformanceChoices[(issue.Id, "cure")];
            var curePicker = selectedCure.GetParent().GetChildren().OfType<AssigneePicker>().First();
            Require(curePicker.Choices.SequenceEqual(shell.Session.View!.AssigneeChoices(new CureNoticeAction { Day = expected.Day, Issue = issue.Id })), "Cure offers the exact Sim teammate forecasts.");
            curePicker.ChoiceButtons["avery"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash, "Selecting the cure writer spends no hours.");
            await CaptureAssigned(selectedCure, curePicker, "Avery", "cure-assignee");
            selectedCure.EmitSignal(Button.SignalName.Pressed);
            Require(shell.ContractPerformanceChoices[(issue.Id, "cure")].Disabled, "The contract cure action refreshes its pending state.");
            Require(shell.Session.StateHash == expected.StateHash() && shell.PerformanceChoices[(issue.Id, "cure")].Disabled, "Cure sends the exact action and prevents a duplicate while in work.");
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(result.Message), "Cure ActionResult is visible.");
            Require(shell.Session.View!.Team.Single(m => m.Id == "avery").Work.Any(w => w.Label.StartsWith("Cure notice")), "Avery actually receives the cure work.");
            Require(LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave()).StateHash() == expected.StateHash(), "The cure assignee survives save and replay.");
            selectedCure.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "The old cure button cannot duplicate work.");
            while (issue.Outcome == CureOutcome.None && !expected.Finished)
            {
                expected.AdvanceDay(); Step(shell);
                if (issue.CureEndsDay is { } end)
                    Require(shell.PerformanceRows.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains($"Cure period ends day {end}")), "Cure end day is read from Sim.");
            }
            Require(issue.Outcome != CureOutcome.None && shell.PerformanceRows.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains($"outcome: {issue.Outcome}")), "Cure outcome stays visible when it arrives.");
            Require(shell.Session.StateHash == expected.StateHash(), "Cure progression matches Sim.");
            shell.OpenContractView(issue.ArrangementId);
            if (issue.Outcome == CureOutcome.Default)
                Require(shell.ContractPerformanceChoices[(issue.Id, "cure")].Disabled
                    && shell.ContractPerformanceChoices[(issue.Id, "resource")].Disabled == shell.PerformanceChoices[(issue.Id, "resource")].Disabled,
                    "A failed cure keeps the contract's re-source decision available with Inbox's gate.");
            else Require(shell.ContractPerformanceChoices.Count == 0, "A cured issue no longer adds decisions to the contract page.");
            shell.CloseContractView();
            expected = Issue(); issue = expected.View.PerformanceIssues.First();
            result = expected.Apply(new TerminateAction { Day = expected.Day, Arrangement = issue.ArrangementId, Assignee = "novak" });
            Require(result.Accepted, "Termination fixture is accepted.");
            shell.PerformanceChoices[(issue.Id, "terminate")].EmitSignal(Button.SignalName.Pressed);
            var endChoice = shell.TerminateButtons[issue.ArrangementId];
            var endPicker = endChoice.GetParent().GetChildren().OfType<AssigneePicker>().Last();
            var beforeEnd = shell.Session.StateHash;
            endPicker.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == beforeEnd, "Selecting the termination writer is pure.");
            await CaptureAssigned(endChoice, endPicker, "Novak", "terminate-assignee");
            endChoice.EmitSignal(Button.SignalName.Pressed);
            Require(shell.EndContractDialog.Visible && shell.EndContractDialog.DialogText.Contains("Novak") && shell.Session.StateHash == beforeEnd, "Confirmation keeps the chosen writer and sends nothing.");
            shell.EndContractDialog.Hide(); shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            Require(shell.Session.StateHash == beforeEnd, "Cancel sends no termination.");
            endChoice.EmitSignal(Button.SignalName.Pressed);
            shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.PerformanceChoices[(issue.Id, "terminate")].Disabled, "Terminate sends the exact action and prevents duplicate work.");
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(result.Message), "Terminate ActionResult is visible.");
            Require(shell.Session.View!.Team.Single(m => m.Id == "novak").Work.Any(w => w.Label.StartsWith("Terminate")), "Novak receives the real termination work.");
            Require(LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave()).StateHash() == expected.StateHash(), "The termination assignee survives save and replay.");
            shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.StateHash == expected.StateHash(), "Repeated confirmation cannot duplicate assigned work.");
            while (!shell.Session.View!.Finished) Step(shell);
            Require(shell.PerformanceChoices.Values.All(b => b.Disabled), "Finished-game performance controls are disabled.");
            Scenario(shell, "spoke-b"); Require(shell.PerformanceChoices.Count == 0, "Scenario reset clears performance choices.");
            // Release the full shell and finalize managed resources while Godot is still running.
            // CI previously crashed during native shutdown after this suite's pass marker.
            shell.QueueFree(); shell = null!;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(!GetChildren().OfType<GameShell>().Any(), "The full shell is gone before process shutdown.");
            GD.Print("PERFORMANCE_SMOKE_PASS: new and persistent Today links, visible Inbox decisions, stale navigation guard, pure re-source, cure timing/outcome and exact termination."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PERFORMANCE_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
