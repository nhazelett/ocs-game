using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using System.Text.Json;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class SkipDaysSmokeChecks
{
    internal static async Task Run(Node parent)
    {
        using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
        var shell = scene.Instantiate<GameShell>(); parent.AddChild(shell);
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-skip-" + Guid.NewGuid() + ".json");
        shell.AutosavePathOverride = temporary + ".autosave";
        // DESK-80 (D31): the tutorial now ends on day 16 and its dining hall slips on days 11 and 12, so the careful
        // play's long quiet stretch is gone. Its day 10 skip is stopped by the slip's news; Spoke B's review play
        // (days 8 to 11) carries the multi-day skip that ends on a known support start.
        var plays = Path.Combine(Path.GetDirectoryName(GamePaths.ScenarioRoot)!, "docs", "reviews", "plays", "2026-11-17-review");
        var bundles = new Dictionary<string, ScenarioBundle>
        {
            ["tutorial"] = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial")),
            ["spoke-b"] = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")),
        };
        var logs = new Dictionary<string, InputLog>
        {
            ["tutorial"] = InputLog.Load(Path.Combine(plays, "tutorial-careful-input-log.json")),
            ["spoke-b"] = InputLog.Load(Path.Combine(plays, "spoke-b-input-log.json")),
        };
        try
        {
            Load(1); OpenPlan();
            Require(!shell.Session.View!.QuietDay() && !shell.SkipAheadButton.IsVisibleInTree(), "An open request leaves no Skip ahead on the busy plan.");
            await Capture("busy");
            var hash = shell.Session.StateHash;
            shell.SkipAheadButton.EmitSignal(Button.SignalName.Pressed);
            Require(hash == shell.Session.StateHash && shell.EndDayDialog.Visible, "A forced hidden skip cannot run a busy day.");

            var expected = Load(10); OpenPlan();
            Require(shell.Session.View!.StaffMeeting.Attending, "The real careful play attends the staff meeting.");
            Require(shell.SkipAheadButton.IsVisibleInTree() && shell.SkipAheadReason.Text == expected.View.SkipStops()!.Sentence,
                "The quiet plan offers Skip ahead with the Sim's exact stop sentence, even while attending uninformative meetings.");
            await Capture("quiet");
            var stop = expected.View.SkipStops()!.UntilDay;
            var summaries = AdvanceExpected(expected, stop);
            Require(summaries.Count > 1 && expected.Day < stop && expected.View.MorningBoard().New.Count > 0,
                "The real review play exercises a multi-day skip that the dining slips' news stops before its forecast.");
            shell.SkipAheadButton.EmitSignal(Button.SignalName.Pressed);
            CheckResults(expected, summaries);
            await Capture("combined");
            hash = shell.Session.StateHash;
            var saved = File.ReadAllBytes(shell.AutosavePath);
            shell.SkipAheadButton.EmitSignal(Button.SignalName.Pressed);
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(hash == shell.Session.StateHash && saved.SequenceEqual(File.ReadAllBytes(shell.AutosavePath)), "Duplicate skip and confirmation cannot run behind results.");
            shell.CloseDaySummary(); shell.YesterdayButton.EmitSignal(Button.SignalName.Pressed);
            CheckResults(expected, summaries);
            Require(hash == shell.Session.StateHash, "Last results reopens the complete batch without changing state.");
            shell.CloseDaySummary(); Step(shell);
            shell.YesterdayButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.DisplayedDaySummaries.Count == 1, "A later single day replaces the previous batch with its own results.");

            // The seven-day cap is the Sim's UntilDay (QuietDayTests); the game stops on UntilDay the same way for a cap
            // or a support start, which this checks.
            expected = Load(8, "spoke-b"); OpenPlan(); stop = expected.View.SkipStops()!.UntilDay;
            summaries = AdvanceExpected(expected, stop);
            Require(summaries.Count > 1 && expected.Day == stop, "The real Spoke B review play exercises a multi-day skip to a known support start.");
            shell.SkipAheadButton.EmitSignal(Button.SignalName.Pressed); CheckResults(expected, summaries);
            shell.CloseDaySummary();
            Load(16); OpenPlan();
            Require(!shell.SkipAheadButton.IsVisibleInTree(), "The last day is not offered for skipping.");

            expected = Load(10); OpenPlan();
            var finance = new FindFinanceAction { Day = 10 };
            Require(expected.Apply(finance).Accepted && shell.SendAction(finance).Accepted, "A new finance visit creates genuine upcoming news.");
            hash = shell.Session.StateHash;
            shell.SkipAheadButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.EndDayDialog.Visible && hash == shell.Session.StateHash, "Changed work invalidates the previous skip check.");
            OpenPlan(); stop = expected.View.SkipStops()!.UntilDay;
            summaries = AdvanceExpected(expected, stop);
            Require(expected.Day < stop && expected.View.MorningBoard().New.Count > 0, "Returned news stops the skip before its forecast horizon.");
            shell.SkipAheadButton.EmitSignal(Button.SignalName.Pressed); CheckResults(expected, summaries);
            Require(shell.DisplayedDaySummaries.SelectMany(d => d.Said).Any(), "The results include the returned answer that stopped the skip.");

            Load(10); OpenPlan(); shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            hash = shell.Session.StateHash; shell.SkipAheadButton.EmitSignal(Button.SignalName.Pressed);
            Require(hash == shell.Session.StateHash, "A canceled plan's hidden skip cannot act later.");
            Load(10); OpenPlan(); shell.AutosavePathOverride = Path.Combine(temporary, "blocked.json");
            shell.SkipAheadButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.View!.Day == 11 && shell.DisplayedDaySummaries.Count == 1,
                "A failed autosave stops the batch after the first day rather than silently continuing.");
            shell.AutosavePathOverride = temporary + ".autosave";
            GD.Print("SKIP_DAYS_SMOKE_PASS: busy absence, exact quiet forecast, support/news stops, last day, whole grouped results, autosaves, stale/duplicate/cancel guards and later-day reset.");
        }
        finally
        {
            shell.QueueFree(); await Frames(); File.Delete(temporary); File.Delete(temporary + ".autosave");
        }

        Session Load(int day, string scenario = "tutorial")
        {
            var log = logs[scenario];
            var save = log with { SavedDay = day, Actions = log.Actions.Where(a => a.Day < day).ToList() };
            File.WriteAllText(temporary, save.ToJson()); shell.LoadSavedGame(temporary);
            return LinesOfSupport.Sim.Replay.Resume(bundles[scenario], save);
        }
        void OpenPlan() => shell.GetNode<Button>("%NextDay").EmitSignal(Button.SignalName.Pressed);
        static List<DaySummaryView> AdvanceExpected(Session expected, int until)
        {
            var summaries = new List<DaySummaryView>();
            while (expected.Day < until && expected.View.QuietDay())
            {
                expected.AdvanceDay(); summaries.Add(expected.View.DaySummary()!);
            }
            return summaries;
        }
        void CheckResults(Session expected, List<DaySummaryView> summaries)
        {
            Require(shell.ReadingDaySummary && !shell.PlayingDay && !shell.EndDayDialog.Visible
                && shell.Session.StateHash == expected.StateHash(), "One results page opens at exactly the independently simulated stopping morning.");
            Require(JsonSerializer.Serialize(shell.DisplayedDaySummaries) == JsonSerializer.Serialize(summaries), "Every skipped day's actual results, answers and news are retained whole and in order.");
            var labels = PlayScreen.Descendants(shell.DaySummaryContents).OfType<Label>().ToArray();
            Require(labels.Where(l => l.HasMeta("summary_day")).Select(l => l.GetMeta("summary_day").AsInt32())
                .SequenceEqual(summaries.Select(d => d.Day)), "The combined page includes every completed day, including quiet ones.");
            Require(GameSaveStore.Read(shell.AutosavePath).ToJson() == shell.Session.CreateSave().ToJson(), "The final skipped morning and its input history are autosaved exactly.");
        }
        async Task Frames() { for (var i = 0; i < 8; i++) await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string suffix)
        {
            await Frames(); var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--skip-screenshots");
            if (at < 0) return;
            await parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = parent.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Native skip screenshot saved.");
            if (shell.EndDayDialog.Visible && shell.Session.View!.QuietDay())
            {
                var row = shell.SkipAheadButton.GetParent<Control>().GetGlobalRect();
                Require(shell.EndDayDialog.GetVisibleRect().Size.Y <= 800 && row.End.Y <= shell.EndDayDialog.Size.Y,
                    $"The quiet-day action and its stop sentence fit the native plan. Row {row}, dialog {shell.EndDayDialog.Size}, visible {shell.EndDayDialog.GetVisibleRect()}.");
            }
        }
    }
}
