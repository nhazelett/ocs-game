using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DisruptionSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        var passed = false;
        try
        {
            shell = Shell(this);
            await DrainFrames();
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            var survey = new SurveyAction { Day = 1, Island = "spokeC" };
            Require(expected.Apply(survey).Accepted && shell.SendAction(survey).Accepted, "Survey fixture accepted.");
            shell.NavigateBoardItem(new BoardItem("test", 0, BoardCategory.Report, BoardSeverity.Info, BoardLinkKind.SiteCapability, "spokeCSite.water", "", []));
            var forecastSeen = false; var arrivalSeen = false; var differenceSeen = false;
            while (!expected.Finished)
            {
                expected.AdvanceDay(); Step(shell);
                var labels = shell.MorningItems.GetChildren().OfType<Button>().Select(b => b.Text).ToArray();
                foreach (var record in expected.View.MorningBoard().Items)
                {
                    if (record.LinkId == "ferryWeather" && record.Text.StartsWith("Forecast"))
                    {
                        Require(labels.Any(t => t.Contains(record.Text)), "Morning board shows the exact ferry forecast."); forecastSeen = true;
                    }
                    if (expected.Records.Any(r => r.Id == record.RecordId && r.Kind == RecordKind.Event && r.Subject == "spokeCSite"))
                    {
                        Require(labels.Any(t => t.Contains(record.Text)), "Morning board shows the exact arrival notice."); arrivalSeen = true;
                    }
                }
                var day = Math.Min(expected.Day, expected.LastDay);
                var actual = expected.View.Population("spokeCSite", day);
                var planned = expected.View.PlannedPopulation("spokeCSite", day);
                Require(shell.BoardFocus.Text.Contains($"Population day {day}: {actual}"), "Selected site population refreshes each day from Sim.");
                Require(shell.BoardFocus.Text.Contains($"· planned {planned}") == (actual != planned), "Planned value is shown beside actual exactly when they differ.");
                differenceSeen |= actual != planned;
            }
            Require(forecastSeen && arrivalSeen && differenceSeen, "Fixture exercises both notices and the population jump.");
            Require(shell.Session.StateHash == expected.StateHash(), "Disruption displays do not change the run.");
            // A population-caused sizing gap requires arrangements sized to the old plan (with the team, DESK-12, approach 2
            // carries the Spoke C food gap that approach 3 used to).
            Scenario(shell, "spoke-b"); Replay(shell, "approach2 fix scope and bridge");
            while (!shell.Session.View!.Finished) Step(shell);
            var gap = shell.DisplayedDebrief!.Gaps.First(g => g.Factors.Any(f => f.Kind == GapFactorKind.PopulationJump));
            shell.DebriefPanel.GetChildren().OfType<Button>().Single(b => b.HasMeta("gap") && b.GetMeta("gap").AsInt32() == gap.Index).EmitSignal(Button.SignalName.Pressed);
            Require(shell.DebriefPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains(DebriefWords.Factor(GapFactorKind.PopulationJump))), "Expanded gap uses the readable PopulationJump label.");
            var args = OS.GetCmdlineUserArgs(); var screenshot = Array.IndexOf(args, "--screenshot");
            if (screenshot >= 0)
            {
                await DrainFrames(); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[screenshot + 1]) == Error.Ok, "The unchanged population-gap screen is captured.");
            }
            passed = true;
        }
        catch (Exception error) { GD.PushError($"DISRUPTION_SMOKE_FAIL: {error}"); }

        // Release the complete shell before Mono/RenderingServer shutdown, on success and failure.
        // Otherwise the synchronous 120-day fixture quits with queued map/UI frees and a live shell.
        var audio = GodotObject.IsInstanceValid(shell) ? StopAudio(shell!) : [];
        if (GodotObject.IsInstanceValid(shell)) shell!.QueueFree();
        shell = null;
        await DrainFrames();
        GC.Collect(); GC.WaitForPendingFinalizers();
        await DrainFrames();
        // Stop() queues the mixer-side playback retirement; process frames alone do not prove it finished.
        // DESK-200 reproduced leaked Ogg streams/playbacks after the old child-count check passed.
        var deadline = Time.GetTicksMsec() + 2000;
        while (audio.Any(GodotObject.IsInstanceIdValid) && Time.GetTicksMsec() < deadline) await DrainFrames();
        var clean = GetChildCount() == 0 && !audio.Any(GodotObject.IsInstanceIdValid);
        if (!clean) GD.PushError("DISRUPTION_SMOKE_FAIL: scene children or tracked audio streams/playbacks remain after teardown.");
        if (passed && clean)
        {
            GD.Print("DISRUPTION_TEARDOWN_PASS: test-owned shell freed, audio streams/playbacks retired and deferred cleanup drained before quit.");
            GD.Print("DISRUPTION_SMOKE_PASS: ferry forecast, arrival notice, refreshed actual/planned population and gap label.");
        }
        GetTree().Quit(passed && clean ? 0 : 1);
    }

    private static ulong[] StopAudio(GameShell shell)
    {
        // Keep real music/cues throughout the fixture. Prevent a new track during teardown, then stop every
        // player and release its stream. Observe native instance ids without holding new resource references.
        shell.Music.SetProcess(false);
        var instances = new HashSet<ulong>();
        foreach (var player in PlayScreen.Descendants(shell).OfType<AudioStreamPlayer>())
        {
            if (player.HasStreamPlayback())
            {
                using var playback = player.GetStreamPlayback(); instances.Add(playback.GetInstanceId());
            }
            using (var stream = player.Stream) if (stream is not null) instances.Add(stream.GetInstanceId());
            player.Stop(); player.Stream = null;
        }
        return instances.ToArray();
    }

    private async Task DrainFrames()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
