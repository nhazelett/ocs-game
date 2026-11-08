using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DisruptionSmokeTest : Node
{
    public override void _Ready()
    {
        try
        {
            var shell = Shell(this);
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
            // A population-caused sizing gap requires arrangements sized to the old plan.
            Scenario(shell, "spoke-b"); Replay(shell, "approach3 uca turnkey with bridge");
            while (!shell.Session.View!.Finished) Step(shell);
            var gap = shell.DisplayedDebrief!.Gaps.First(g => g.Factors.Any(f => f.Kind == GapFactorKind.PopulationJump));
            shell.DebriefPanel.GetChildren().OfType<Button>().Single(b => b.HasMeta("gap") && b.GetMeta("gap").AsInt32() == gap.Index).EmitSignal(Button.SignalName.Pressed);
            Require(shell.DebriefPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains("Unplanned population arrival")), "Expanded gap uses the readable PopulationJump label.");
            GD.Print("DISRUPTION_SMOKE_PASS: ferry forecast, arrival notice, refreshed actual/planned population and gap label."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"DISRUPTION_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
