using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DaySummarySmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            shell.DeveloperToggle.ButtonPressed = false;
            var end = shell.GetNode<Button>("%NextDay");
            Require(!shell.YesterdayButton.Visible && shell.Session.View!.DaySummary() is null,
                "There are no results or Yesterday link before the first day.");
            EndDay(shell);
            Require(shell.PlayingDay && !shell.ReadingDaySummary, "The map plays before its results appear.");
            var hash = shell.Session.StateHash;
            var saved = File.ReadAllBytes(shell.AutosavePath);
            shell.SkipPlaybackButton.EmitSignal(Button.SignalName.Pressed);
            CheckSummary();
            Require(end.Disabled && shell.DaySummaryDialog.GetOkButton().HasFocus(), "Results hold day input and focus their close control.");
            end.EmitSignal(Button.SignalName.Pressed);
            shell.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            Require(!shell.SendAction(new FindFinanceAction { Day = shell.Session.View!.Day }).Accepted,
                "A stale action cannot change the morning behind the results.");
            Require(hash == shell.Session.StateHash && saved.SequenceEqual(File.ReadAllBytes(shell.AutosavePath)),
                "Reading, duplicate advances and rejected background actions change neither state nor save.");
            Require(shell.DisplayedDaySummary!.Short.Count == 0, "The quiet fixture was fully supplied.");
            await Capture("quiet");
            shell.DaySummaryDialog.EmitSignal(AcceptDialog.SignalName.Confirmed);
            Require(!shell.ReadingDaySummary && !end.Disabled && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.TodayTab,
                "Closing results returns to the next morning's Today tab.");
            shell.FinishDayPlayback();
            Require(!shell.ReadingDaySummary, "A duplicate playback finish cannot reopen the results.");
            shell.YesterdayButton.EmitSignal(Button.SignalName.Pressed); CheckSummary();
            Require(shell.DisplayedDaySummary!.Day == 1 && hash == shell.Session.StateHash, "Yesterday reopens the same completed day without simulating.");
            shell.DaySummaryDialog.EmitSignal(AcceptDialog.SignalName.Canceled);
            Require(!shell.ReadingDaySummary && !end.Disabled, "Escape/cancel returns to the desk too.");
            await Capture("today");

            DeveloperWorkspace(shell); Scenario(shell, "tutorial");
            Require(!shell.YesterdayButton.Visible && !shell.ReadingDaySummary, "A fresh deployment clears the previous results.");
            Step(shell, 2);
            Require(shell.SendAction(new MeetVendorAction { Day = 3, Vendor = "sanitation", Assignee = "avery" }).Accepted,
                "A real teammate has work that finishes on the bad day.");
            shell.DeveloperToggle.ButtonPressed = false;
            EndDay(shell); hash = shell.Session.StateHash;
            shell.TickDayPlayback(6); CheckSummary();
            var bad = shell.DisplayedDaySummary!;
            Require(bad.Day == 3 && bad.Finished.Any(l => l.TeammateId == "avery")
                && bad.Short.Any(l => l.Contains("Nothing supplies water at Hub"))
                && bad.Short.Any(l => l.Contains("No water, so no food service")),
                "The bad day shows the actual named finish and both Sim shortfall reasons.");
            await Capture("bad");
            shell.CloseDaySummary();
            Require(shell.SendAction(new FindFinanceAction { Day = 4 }).Accepted,
                "Today's work can change after the results are closed.");
            hash = shell.Session.StateHash;
            shell.YesterdayButton.EmitSignal(Button.SignalName.Pressed);
            Require(ReferenceEquals(shell.DisplayedDaySummary, bad) && hash == shell.Session.StateHash,
                "Yesterday preserves the completed day's snapshot after a new morning action.");
            shell.CloseDaySummary();
            while (shell.Session.View!.Day < 9) Step(shell);
            Require(shell.SendAction(new AwardAction { Day = 9, Id = "summary-order", Instrument = "purchaseOrder", Vendor = "sanitation",
                Card = "tutWaste", Quantity = 3600, StartDay = 13, DurationDays = 8, FundingLine = "om1" }).Accepted,
                "A real written order supplies carry-over work.");
            Step(shell); EndDay(shell); shell.FinishDayPlayback(); CheckSummary();
            Require(shell.DisplayedDaySummary!.CarriedOver.Count > 0, "Unfinished real work says who has it and what remains.");
            shell.DaySummaryScroll.ScrollVertical = (int)shell.DaySummaryScroll.GetVScrollBar().MaxValue;
            await Capture("carried");
            var carried = shell.DisplayedDaySummary!;
            shell.CloseDaySummary();
            var order = shell.Session.View!.Team.SelectMany(p => p.Work).Single(w => w.Label.Contains("Main Island Sanitation"));
            Require(shell.SendAction(new AssignAction { Day = shell.Session.View.Day, Work = order.WorkId, To = "avery" }).Accepted,
                "Today's hand-over changes a real carry-over item's current holder.");
            shell.SaveButton.EmitSignal(Button.SignalName.Pressed);
            var reassigned = shell.Session.StateHash;
            shell.LoadSavedGame(shell.AutosavePath);
            shell.YesterdayButton.EmitSignal(Button.SignalName.Pressed); CheckSummary(carried);
            Require(shell.Session.StateHash == reassigned && shell.DisplayedDaySummary!.CarriedOver.SequenceEqual(carried.CarriedOver),
                "Resuming after a morning hand-over preserves yesterday's original holder and hours.");
            shell.CloseDaySummary();

            // Check all categories against real authored records, including starts and ends.
            DeveloperWorkspace(shell); Scenario(shell, "spoke-b"); Replay(shell);
            var sawStarted = false; var sawEnded = false;
            while (!shell.Session.View!.Finished)
            {
                EndDay(shell); shell.FinishDayPlayback(); CheckSummary();
                sawStarted |= shell.DisplayedDaySummary!.Started.Count > 0;
                sawEnded |= shell.DisplayedDaySummary.Ended.Count > 0;
                shell.CloseDaySummary();
            }
            Require(sawStarted && sawEnded, "The real replay exercises actual support starts and ends.");
            Require(end.Disabled && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.DebriefTab,
                "The final day's results close into the debrief without enabling more days.");
            shell.YesterdayButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.ReadingDaySummary && shell.DisplayedDaySummary!.Day == shell.Session.CompletedDay,
                "The final day's results can still be read.");
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            Require(shell.AtFrontDoor && !shell.ReadingDaySummary, "Menu cleanup dismisses results without opening another page.");
            DeveloperWorkspace(shell); Scenario(shell, "tutorial"); Step(shell, 2);
            var beforeLoad = shell.Session.StateHash;
            shell.LoadSavedGame(shell.AutosavePath);
            Require(!shell.ReadingDaySummary && shell.Session.StateHash == beforeLoad && shell.YesterdayButton.Visible,
                "Resuming reconstructs Yesterday and does not automatically replay old results.");
            shell.YesterdayButton.EmitSignal(Button.SignalName.Pressed); CheckSummary();
            Require(shell.DisplayedDaySummary!.Day == 2, "Resumed Yesterday is the saved completed day.");
            shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("DAY_SUMMARY_SMOKE_PASS: exact actual results, skip/timer order, modal and save purity, Yesterday snapshot, reset/resume, final debrief and one scroll.");
            GetTree().Quit();

            void CheckSummary(DaySummaryView? expected = null)
            {
                Require(shell.ReadingDaySummary && !shell.PlayingDay, "One results page opens only after playback.");
                expected ??= shell.Session.View!.DaySummary()!;
                Require(shell.DisplayedDaySummary!.Day == expected.Day, "Results name the exact completed day.");
                var labels = Labels(shell.DaySummaryContents).ToArray();
                Require(labels.First().Text == expected.Sentence, "The result summary comes verbatim from Sim.");
                Check("finished", expected.Finished.Select(l => l.Sentence));
                Check("started", expected.Started); Check("ended", expected.Ended); Check("short", expected.Short);
                Check("slipped", expected.Slipped.Select(l => l.Sentence)); Check("carried", expected.CarriedOver.Select(l => l.Sentence));
                Require(shell.DaySummaryScroll.GetChildCount() == 1
                    && shell.DaySummaryDialog.Size.X <= 1280 && shell.DaySummaryDialog.Size.Y <= 800,
                    "One scrolling body and its close control fit the release viewport.");
                void Check(string kind, IEnumerable<string> sentences) => Require(labels.Where(l => l.HasMeta("summary_kind")
                    && l.GetMeta("summary_kind").AsString() == kind).Select(l => l.Text).SequenceEqual(sentences),
                    "Every actual " + kind + " result keeps Sim wording and order.");
            }
            async Task Capture(string name)
            {
                var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--screenshots");
                for (var i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (at < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (shell.ReadingDaySummary)
                {
                    Require(shell.DaySummaryDialog.GetOkButton().GetGlobalRect().End.Y <= shell.DaySummaryDialog.Size.Y,
                        "The close control stays on screen.");
                    Require(shell.DaySummaryContents.Size.X <= shell.DaySummaryScroll.Size.X,
                        "Wrapped result text stays inside the scroll width.");
                }
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[at + 1] + "-" + name + ".png") == Error.Ok, "Native result screenshot saved.");
            }
        }
        catch (Exception error) { GD.PushError("DAY_SUMMARY_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }

    private static IEnumerable<Label> Labels(Node node) => node.GetChildren().SelectMany(child =>
        (child is Label label ? new[] { label } : Enumerable.Empty<Label>()).Concat(Labels(child)));
}
