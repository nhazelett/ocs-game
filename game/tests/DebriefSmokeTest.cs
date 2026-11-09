using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DebriefSmokeTest : Node
{
    public override void _Ready()
    {
        try
        {
            var shell = Shell(this); Replay(shell);
            Require(shell.DisplayedDebrief is null, "Debrief waits for the final day.");
            while (!shell.Session.View!.Finished) Step(shell);
            var hash = shell.Session.StateHash;
            var expected = shell.Session.View.Debrief();
            Require(shell.DisplayedDebrief!.Scorecard == expected.Scorecard, "Every scorecard value comes from Sim.");
            Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 8, "Final day opens Debrief automatically.");
            var buttons = shell.DebriefPanel.GetChildren().OfType<Button>().Where(b => b.HasMeta("gap")).ToArray();
            Require(buttons.Length == expected.Gaps.Count && buttons.Length > 0, "Each actual gap has an evidence disclosure.");
            foreach (var gap in expected.Gaps)
            {
                var button = buttons.Single(b => b.GetMeta("gap").AsInt32() == gap.Index);
                Require(button.Text.Contains(gap.Tag.ToString()), "Gap tags are unchanged from Sim.");
                button.EmitSignal(Button.SignalName.Pressed);
            }
            static IEnumerable<Label> Labels(Node node) => node.GetChildren().SelectMany(child => (child is Label label ? new[] { label } : Enumerable.Empty<Label>()).Concat(Labels(child)));
            var text = string.Join("\n", Labels(shell.DebriefPanel).Select(l => l.Text));
            foreach (var item in expected.Events) Require(text.Contains(item.Text) && Labels(shell.DebriefPanel).Any(l => l.HasMeta("event_record") && l.GetMeta("event_record").AsString() == item.RecordId), "Timeline retains Sim event text and internal record links.");
            foreach (var gap in expected.Gaps)
            {
                foreach (var factor in gap.Factors) Require(text.Contains(factor.Text), "Evidence text is displayed verbatim.");
                foreach (var id in gap.ShortfallRecordIds) Require(Labels(shell.DebriefPanel).Any(l => l.HasMeta("cause_record") && l.GetMeta("cause_record").AsString() == id), "Each shortfall cause chain is available through its internal link.");
            }
            foreach (var commitment in expected.Commitments) Require(text.Contains(shell.Session.View.NameOf(commitment.Id)) && text.Contains(commitment.Decision), "Commitment decisions are displayed.");
            Require(shell.Session.StateHash == hash, "Rendering and expanding the debrief are read-only.");
            Scenario(shell, "_stub");
            Require(shell.DisplayedDebrief is null, "Changing scenarios clears the completed debrief.");
            GD.Print("DEBRIEF_SMOKE_PASS: final-day screen, unchanged scores/tags/evidence, causes, commitments and read-only render."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"DEBRIEF_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
