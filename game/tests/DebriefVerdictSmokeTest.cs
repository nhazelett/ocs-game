using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DebriefVerdictSmokeTest : Node
{
    public override async void _Ready()
    {
        var path = Path.Combine(Path.GetTempPath(), "ocs-gap-verdict-" + Guid.NewGuid() + ".json");
        try
        {
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var input = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-20-3", "input-log.json"));
            var expected = LinesOfSupport.Sim.Replay.Run(bundle, input);
            using var packed = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = packed.Instantiate<GameShell>(); shell.AutosavePathOverride = path + ".autosave"; AddChild(shell);
            File.WriteAllText(path, expected.ToSave().ToJson()); shell.LoadSavedGame(path); shell.DeveloperToggle.ButtonPressed = false;
            var report = expected.View.Debrief(); var hash = expected.StateHash();
            Require(shell.Session.StateHash == hash && shell.Session.View!.Finished, "The third play's exact completed save loads through the game boundary.");
            Require(report.Gaps.Any(g => g.Factors.Any(f => f.Tag != g.Tag)), "The real play exercises conflicting factor tags under one gap verdict.");
            Require(shell.DebriefSummaryText.Text == report.Summary!.Text, "The Sim's summary and its quoted evidence stay verbatim.");
            foreach (var gap in report.Gaps)
            {
                var button = shell.DebriefPanel.GetChildren().OfType<Button>().Single(b => b.HasMeta("gap") && b.GetMeta("gap").AsInt32() == gap.Index);
                Require(button.Text.Split('\n').Last() == (gap.TagLine ?? DebriefWords.TagMeaning(gap.Tag)), "Each gap button retains its one Sim verdict.");
                var detail = Detail(gap.Index);
                var labels = detail.GetChildren().OfType<Label>().Where(l => l.HasMeta("factor_index")).ToArray();
                Require(labels.Length == gap.Factors.Count, "Every factor remains present on every gap.");
                for (var i = 0; i < gap.Factors.Count; i++)
                {
                    var factor = gap.Factors[i]; var label = labels.Single(l => l.GetMeta("factor_index").AsInt32() == i);
                    var quoted = gap.Index == report.Summary.LeadGap && report.Summary.Quoted.Contains(i);
                    Require(label.Text.Split('\n')[0] == DebriefWords.Factor(factor.Kind) + ".", "Lead and other factor labels carry no second verdict.");
                    Require(quoted ? label.Text == DebriefWords.Factor(factor.Kind) + "." && shell.DebriefSummaryText.Text.Contains(factor.Text)
                        : label.Text.Contains(factor.Text), "Each exact reason remains in its factor row or the summary that already quotes it.");
                }
            }
            shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            var score = PlayScreen.Descendants(shell.DebriefFullRecord).OfType<Label>().Single(l => l.HasMeta("scorecard"));
            Require(score.Text.Contains($"{report.Scorecard.GapCount} gaps: {report.Scorecard.AvoidableGaps} avoidable · {report.Scorecard.DiscoverableGaps} discoverable · {report.Scorecard.UnforeseeableGaps} unforeseeable"), "The full record keeps all tag counts.");
            shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed);
            var fork = report.Gaps.Single(g => g.CapabilityId == "mhe"); var forkDetail = Detail(fork.Index); forkDetail.Show();
            var late = fork.Factors.Single(f => f.Text.Contains("was quoted to start on day 8 or 9 and started on day 9."));
            var lateIndex = fork.Factors.ToList().IndexOf(late);
            var lateLabel = fork.Index == report.Summary.LeadGap && report.Summary.Quoted.Contains(lateIndex)
                ? shell.DebriefSummaryText : forkDetail.GetChildren().OfType<Label>().Single(l => l.HasMeta("factor_index") && l.Text.Contains(late.Text));
            await Frames(); var scroll = shell.DebriefPanel.GetParent<ScrollContainer>(); scroll.EnsureControlVisible(forkDetail); await Frames();
            Require(scroll.GetGlobalRect().Encloses(lateLabel.GetGlobalRect()), "The quoted late-start reason is readable in the actual viewport.");
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--screenshots");
            if (at >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage(); Require(image.SavePng(args[at + 1] + "-forklift.png") == Error.Ok, "The third-play forklift screenshot is saved.");
            }
            Require(shell.Session.StateHash == hash, "Reading verdicts, evidence and the full record changes no Sim state.");

            // DESK-204 (D54): the 22 Nov first play's confusion days on card buys read as a trade taken knowingly.
            var trade = LinesOfSupport.Sim.Replay.Run(bundle, InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-22", "input-log-1.json")));
            File.WriteAllText(path, trade.ToSave().ToJson()); shell.LoadSavedGame(path); await Frames();
            var tradeReport = trade.View.Debrief();
            Require(shell.Session.StateHash == trade.StateHash() && shell.Session.View!.Finished, "The 22 Nov first play's completed save loads.");
            var tradeButtons = tradeReport.Gaps.Select(g => (Gap: g, Button: shell.DebriefPanel.GetChildren().OfType<Button>().Single(b => b.HasMeta("gap") && b.GetMeta("gap").AsInt32() == g.Index))).ToList();
            Require(tradeButtons.All(x => x.Button.Text.Split('\n').Last() == x.Gap.TagLine), "Each gap button shows the Sim's own line.");
            // DESK-215 (D56): water's written route could not have started by day 3, so it reads as worth taking; the two
            // forklift days, where a purchase order would have kept the line, keep D54's line.
            Require(tradeButtons.Count(x => x.Button.Text.EndsWith(DebriefWords.NoTermsTagLine, StringComparison.Ordinal)) == 2, "The two forklift confusion days read as a risk taken.");
            Require(tradeButtons.Single(x => x.Gap.CapabilityId == "water").Button.Text.EndsWith("A risk worth taking: no written order could have started by day 3.", StringComparison.Ordinal), "Water's confusion day reads as a risk worth taking.");
            if (at >= 0)
            {
                var waterGap = tradeReport.Gaps.Single(g => g.CapabilityId == "water"); Detail(waterGap.Index).Show(); await Frames();
                var tradeScroll = shell.DebriefPanel.GetParent<ScrollContainer>(); tradeScroll.EnsureControlVisible(Detail(waterGap.Index)); await Frames();
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage(); Require(image.SavePng(args[at + 1] + "-no-terms.png") == Error.Ok, "The 22 Nov no-terms screenshot is saved.");
            }
            shell.QueueFree(); await Frames(); GD.Print("DEBRIEF_VERDICT_SMOKE_PASS: one verdict per gap, every factor and exact reason, quoted evidence, full-record counts, third-play late start and pure navigation."); GetTree().Quit();
            VBoxContainer Detail(int index) => shell.DebriefPanel.GetChildren().OfType<VBoxContainer>().Single(v => v.HasMeta("gap_detail") && v.GetMeta("gap_detail").AsInt32() == index);
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        }
        catch (Exception e) { GD.PushError($"DEBRIEF_VERDICT_SMOKE_FAIL: {e}"); GetTree().Quit(1); }
        finally { File.Delete(path); }
    }
}
