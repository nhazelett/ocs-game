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
                Require(button.Text.Split('\n').Last() == DebriefWords.TagMeaning(gap.Tag), "Each gap button retains its one Sim verdict.");
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
            shell.QueueFree(); await Frames(); GD.Print("DEBRIEF_VERDICT_SMOKE_PASS: one verdict per gap, every factor and exact reason, quoted evidence, full-record counts, third-play late start and pure navigation."); GetTree().Quit();
            VBoxContainer Detail(int index) => shell.DebriefPanel.GetChildren().OfType<VBoxContainer>().Single(v => v.HasMeta("gap_detail") && v.GetMeta("gap_detail").AsInt32() == index);
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        }
        catch (Exception e) { GD.PushError($"DEBRIEF_VERDICT_SMOKE_FAIL: {e}"); GetTree().Quit(1); }
        finally { File.Delete(path); }
    }
}
