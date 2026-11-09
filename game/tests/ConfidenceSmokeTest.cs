using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ConfidenceSmokeTest : Node
{
    public override void _Ready()
    {
        try
        {
            var shell = Shell(this);
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            void Apply(PlayerAction action)
            {
                Require(expected.Apply(action).Accepted && shell.SendAction(action).Accepted, "Both sessions accept the fixture action.");
                Require(shell.Session.StateHash == expected.StateHash(), "UI actions match Sim exactly.");
            }
            void Advance() { expected.AdvanceDay(); Step(shell); }
            Apply(new AwardAction { Day = 1, Id = "gens", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Card = "req0007", Quantity = 360, StartDay = 7, DurationDays = 60, FundingLine = "om1" });
            while (expected.Day < 12) Advance();
            Apply(new ResolveCommitmentAction { Day = expected.Day, Commitment = "uc1", Choice = "refuse" });
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            Require(map.LostConfidenceSites.SetEquals(expected.View.LostConfidenceSites), "Map marks exactly Sim's lost-confidence sites.");
            foreach (var tile in shell.MorningTiles.GetChildren().OfType<Button>())
                Require(tile.Text.Contains("commander confidence lost") == (tile.GetMeta("capability").AsString().StartsWith("spokeBSite.")), "Only the refused site's capability rows are marked.");
            Apply(new AwardAction { Day = expected.Day, Id = "turnkey", Instrument = "uca", Vendor = "vendorB", Card = "req0007", Quantity = 470, StartDay = 20, DurationDays = 60, FundingLine = "om1" });
            var award = expected.Work.Single(w => w.Kind == WorkKind.Award && w.SubjectId == "turnkey");
            while (expected.Day < award.WaitUntilDay) Advance();
            Apply(new SurveyAction { Day = expected.Day, Island = "spokeB" });
            Apply(new EstablishBpaAction { Day = expected.Day, Id = "fuelBpa", Vendor = "vendorD", Capability = "fuel", ScopeLines = ["delivery"], CallLimitCents = 5000000 });
            Advance();
            var record = expected.Records.Single(r => r.Kind == RecordKind.Compliance && r.Subject == award.Id);
            Require(record.Text.StartsWith("Shop hours short") && shell.GetNode<RichTextLabel>("%InboxRecords").Text.Contains(record.Text), "Inbox shows the unchanged shop-hours Compliance record.");
            while (!expected.Finished) Advance();
            Require(shell.Session.StateHash == expected.StateHash(), "Presentation leaves final state unchanged.");
            var gap = shell.DisplayedDebrief!.Gaps.First(g => g.Factors.Any(f => f.Kind == GapFactorKind.PriorityDeferred));
            shell.DebriefPanel.GetChildren().OfType<Button>().Single(b => b.HasMeta("gap") && b.GetMeta("gap").AsInt32() == gap.Index).EmitSignal(Button.SignalName.Pressed);
            Require(shell.DebriefPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains(DebriefWords.Factor(GapFactorKind.PriorityDeferred))), "Expanded debrief uses the readable factor label.");
            Scenario(shell, "spoke-b");
            Require(map.LostConfidenceSites.Count == 0 && shell.MorningTiles.GetChildren().OfType<Button>().All(b => !b.Text.Contains("confidence lost")), "A new game clears all confidence marks.");
            GD.Print("CONFIDENCE_SMOKE_PASS: site marks, Compliance inbox, debrief label and reset with unchanged Sim."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"CONFIDENCE_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
