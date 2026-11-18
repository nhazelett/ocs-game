using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class BarSmokeChecks
{
    internal static async Task Run(Node parent)
    {
        using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
        var shell = scene.Instantiate<GameShell>(); parent.AddChild(shell);
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-bar-" + Guid.NewGuid() + ".json");
        shell.AutosavePathOverride = temporary + ".autosave";
        var root = Path.GetDirectoryName(GamePaths.ScenarioRoot)!;
        var logs = Path.Combine(root, "docs", "reviews", "plays", "2026-11-17-review");
        var careful = InputLog.Load(Path.Combine(logs, "tutorial-careful-input-log.json"));
        var hasty = InputLog.Load(Path.Combine(logs, "tutorial-hasty-input-log.json"));
        try
        {
            Load(careful, 1); Check(shell.TodayBar, shell.Session.View!.Bar());
            await Capture("day1", shell.TodayBar);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var atRisk = Enumerable.Range(2, bundle.Scenario.Days - 1).First(day =>
                LinesOfSupport.Sim.Replay.Run(bundle, hasty, day - 1).View.Bar().Lines.Any(l => l.State == BarState.AtRisk));
            Load(hasty, atRisk); Check(shell.TodayBar, shell.Session.View!.Bar());
            Require(shell.Session.View!.Bar().Lines.Any(l => l.State == BarState.AtRisk), "A real review day exercises the amber state.");
            await Capture("at-risk", shell.TodayBar);
            Load(careful, null); var pass = shell.DisplayedDebrief!.Bar!;
            Require(pass.Final && pass.Lines.All(l => l.State == BarState.Met), "The careful review log passes every real line.");
            Check(shell.DebriefBar!, pass); await Capture("pass", shell.DebriefBar!);
            var play = InputLog.Load(Path.Combine(root, "docs", "reviews", "plays", "2026-11-18", "input-log.json"));
            Load(play, null); Check(shell.DebriefBar!, shell.DisplayedDebrief!.Bar!);
            Require(shell.DisplayedDebrief.Carried.Single(c => c.LineId == "mheUp").Sentence.Contains("ordered on day 5")
                && shell.DisplayedDebrief.Carried.Single(c => c.LineId == "mheUp").Sentence.Contains("SMSgt Ray Dunn"),
                "The recorded 18 Nov play exercises the decision and the person who named the missing operators.");
            await Capture("play18", shell.DebriefBar!);
            Load(hasty, null); var miss = shell.DisplayedDebrief!.Bar!;
            Require(miss.Final && miss.Lines.Any(l => l.State == BarState.Missed), "The hasty review log has real misses.");
            Require(miss.Lines.Single(l => l.Id == "mheUp").State == BarState.Missed
                && shell.DisplayedDebrief!.Carried.Single(c => c.LineId == "mheUp").Sentence.StartsWith("Out on days "),
                "The hasty review log misses the forklift line and supplies its actual days and cause.");
            Check(shell.DebriefBar!, miss); await Capture("miss", shell.DebriefBar!);
            var barIndex = shell.DebriefBar!.GetIndex();
            Require(shell.DebriefPanel.GetChildren().OfType<Button>().Where(b => b.HasMeta("gap")).All(b => b.GetIndex() > barIndex),
                "Every gap follows the commander's verdict.");
            shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.DebriefRecordButton.Text.Contains(miss.Days + " days"), "The record title states the real length.");
            var labels = PlayScreen.Descendants(shell.DebriefFullRecord).OfType<Label>().ToArray();
            Require(labels.Where(l => l.HasMeta("record_morning")).Select(l => l.GetMeta("record_morning").AsInt32())
                .SequenceEqual(Enumerable.Range(1, miss.Days)), "Even quiet mornings run to the last day.");
            Require(!labels.Any(l => l.Text.StartsWith("Known then:")), "The obsolete repeated information-tier line is absent.");
            var hash = shell.Session.StateHash;
            shell.DebriefPlayAgain.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.Prep is not null && shell.Session.PrepScenarioId == "tutorial" && !shell.AtFrontDoor,
                "Play again opens this scenario's preparations.");
            Load(hasty, null); hash = shell.Session.StateHash;
            shell.DebriefChooseScenario.EmitSignal(Button.SignalName.Pressed);
            Require(shell.AtFrontDoor && shell.TutorialGameButton.IsVisibleInTree() && shell.SpokeBGameButton.IsVisibleInTree()
                && hash == shell.Session.StateHash, "Choose a scenario opens the real choice without changing the finished game.");
            var spoke = InputLog.Load(Path.Combine(logs, "spoke-b-input-log.json"));
            Load(spoke, null); shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed);
            var picker = PlayScreen.Descendants(shell.DebriefFullRecord).OfType<OptionButton>().Single();
            picker.Select(13); picker.EmitSignal(OptionButton.SignalName.ItemSelected, 13);
            var calendar = PlayScreen.Descendants(shell.DebriefFullRecord).OfType<Label>().Where(l => l.HasMeta("event_record")).ToArray();
            Require(calendar.Any(l => l.Text.Contains("64") && l.Text.Contains("Spoke C")),
                "The real Spoke C notice is under Morning of day 13, when the player saw it, rather than day 12.");
            GD.Print("BAR_SMOKE_PASS: exact commander words/counts/states, real amber and pass/miss logs, visible native bar, every morning, population notice date and restart/scenario navigation.");
        }
        finally
        {
            shell.QueueFree(); await Frames(); File.Delete(temporary); File.Delete(temporary + ".autosave");
        }

        void Load(InputLog input, int? day)
        {
            var save = input with { SavedDay = day, Actions = day is { } d ? input.Actions.Where(a => a.Day < d).ToList() : input.Actions };
            File.WriteAllText(temporary, save.ToJson()); shell.LoadSavedGame(temporary);
        }
        void Check(VBoxContainer panel, BarView bar)
        {
            var hash = shell.Session.StateHash;
            var labels = PlayScreen.Descendants(panel).OfType<Label>().Where(l => l.HasMeta("bar_line")).ToArray();
            Require(panel.GetChildren().OfType<Label>().First().ThemeTypeVariation == "TabletopHeading", "The bar title is a pane heading.");
            Require(labels.Length == bar.Lines.Count, "Every pass criterion is shown exactly once.");
            for (var i = 0; i < labels.Length; i++)
            {
                var line = bar.Lines[i]; var label = labels[i];
                var word = line.State == BarState.AtRisk ? "At risk" : line.State == BarState.Missed ? "Missed" : bar.Final ? "Met" : "Met so far";
                Require(label.Text == (panel == shell.TodayBar ? "" : line.Says + " ") + line.Text + " " + word + ".", "Today shows counts and state only; the verdict also keeps the commander's words.");
                var color = line.State == BarState.AtRisk ? TabletopTheme.Warning : line.State == BarState.Missed ? TabletopTheme.Alert : TabletopTheme.Ink;
                Require(label.GetThemeColor("font_color") == color, "The Sim's at-risk and missed states are amber and red, with ordinary ink for met.");
            }
            var causes = PlayScreen.Descendants(panel).OfType<Label>().Where(l => l.HasMeta("bar_carried")).ToArray();
            if (panel == shell.TodayBar)
                Require(causes.Length == 0, "Today keeps the current bar without final-debrief explanations.");
            else
            {
                var carried = shell.DisplayedDebrief!.Carried;
                Require(causes.Select(l => l.GetMeta("bar_carried").AsString()).SequenceEqual(carried.Select(c => c.LineId)),
                    "Exactly the Sim's carried sentences appear, in verdict order, without extra lines.");
                foreach (var cause in carried)
                {
                    var label = causes.Single(l => l.GetMeta("bar_carried").AsString() == cause.LineId);
                    var verdict = labels.Single(l => l.GetMeta("bar_line").AsString() == cause.LineId);
                    Require(label.Text == cause.Sentence && label.GetParent() == verdict.GetParent()
                        && label.GetIndex() == verdict.GetIndex() + 1,
                        "The exact Sim sentence sits directly under its matching verdict in the same group.");
                    Require(label.GetThemeFontSize("font_size") == verdict.GetThemeFontSize("font_size"),
                        "What carried the line uses the verdict's normal readable text size.");
                }
            }
            Require(hash == shell.Session.StateHash, "Reading the bar is pure.");
        }
        async Task Frames() { for (var i = 0; i < 8; i++) await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string suffix, Control focus)
        {
            await Frames();
            ScrollContainer scroll;
            if (focus == shell.TodayBar) scroll = shell.GetNode<ScrollContainer>("%DeskTabs/Today/TodayPane/Body/Body");
            else scroll = (ScrollContainer)shell.DebriefPanel.GetParent();
            if (focus == shell.TodayBar) scroll.EnsureControlVisible(focus);
            else
            {
                foreach (var row in focus.GetChildren().OfType<VBoxContainer>())
                {
                    scroll.EnsureControlVisible(row); await Frames();
                    Require(scroll.GetGlobalRect().Encloses(row.GetGlobalRect()), "Each verdict and its carried sentence fit together in the native reading area.");
                }
                // EnsureControlVisible queues scroll adjustments; let them settle before returning to the heading.
                await Frames(); scroll.ScrollVertical = 0;
            }
            await Frames();
            if (focus != shell.TodayBar) { scroll.ScrollVertical = 0; await Frames(); }
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--bar-screenshots");
            if (at < 0) return;
            Require(focus == shell.TodayBar ? scroll.GetGlobalRect().Encloses(focus.GetGlobalRect())
                : scroll.GetGlobalRect().Encloses(focus.GetChildren().OfType<Label>().First().GetGlobalRect()),
                "The current bar or the final verdict heading is visible at the native scroll position.");
            await parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = parent.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Native commander's bar saved.");
        }
    }
}
