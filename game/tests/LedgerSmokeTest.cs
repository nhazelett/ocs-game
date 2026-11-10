using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class LedgerSmokeTest : Node
{
    public override void _Ready()
    {
        try
        {
            var shell = Shell(this);
            Scenario(shell, "spoke-b-90"); Replay(shell, "approach3b uca never definitized");
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b-90"));
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach3b-uca-never-definitized.json"));
            var sawUnpaid = false; var sawUnusable = false; var sawCoordination = false;
            while (true)
            {
                var view = shell.Session.View!;
                var ledger = shell.GetNode<TabContainer>("%DeskTabs").GetNode("Ledger/LedgerContext/Contents");
                var funds = ledger.GetChildren().OfType<Label>().Where(l => l.HasMeta("funding")).ToArray();
                Require(funds.Length == view.Funding.Count, "Every funding line is shown exactly once.");
                foreach (var funding in view.Funding)
                {
                    var row = funds.Single(l => l.GetMeta("funding").AsString() == funding.Id);
                    Require(row.Text.Contains(DeskUi.Money(funding.RemainingCents)) && row.Text.Contains($"expires day {funding.ExpiresDay}"), "Remaining funds and expiry come from Sim.");
                    Require(row.Text.Contains(funding.UsableOn(view.Day) ? "· usable today" : "· not usable today"), "Funding usability refreshes with the day.");
                    sawUnusable |= !funding.UsableOn(view.Day);
                }
                var arrangements = ledger.GetChildren().OfType<Button>().Where(b => b.HasMeta("arrangement")).ToArray();
                Require(arrangements.Length == view.Arrangements.Count, "Every arrangement has one ledger row.");
                foreach (var arrangement in view.Arrangements)
                {
                    var row = arrangements.Single(b => b.GetMeta("arrangement").AsString() == arrangement.Id);
                    Require(row.Text.Contains(arrangement.Status switch { ArrangementStatus.Pending => "Being arranged", ArrangementStatus.Active => "Providing support", ArrangementStatus.Suspended => "Support paused", ArrangementStatus.Ended => "Ended", _ => "Declined" }) && row.Text.Contains($"Money committed {DeskUi.Money(arrangement.ObligatedCents)}") && row.Text.Contains($"cost of support received {DeskUi.Money(arrangement.IncurredCents)}") && row.Text.Contains($"Received but not covered by committed funds {DeskUi.Money(arrangement.UnobligatedIncurredCents)}"), "All arrangement amounts and status bind to Sim, including Q11.");
                    sawUnpaid |= arrangement.UnobligatedIncurredCents > 0;
                    sawCoordination |= arrangement.IsCoordination;
                }
                if (view.Finished) break;
                Step(shell);
            }
            Require(sawUnpaid && sawUnusable && sawCoordination, "Replay covers unpaid UCA, expired funds and coordination rows.");
            var expected = LinesOfSupport.Sim.Replay.Run(bundle, log);
            Require(shell.Session.StateHash == expected.StateHash(), "Inspecting every row leaves the replay unchanged.");
            Scenario(shell, "spoke-b"); Replay(shell, "approach1 refuse"); Step(shell, 14);
            var offers = shell.Session.View!.ActionEditor("spokeBSite", "power")!.Vendors;
            var directory = shell.GetNode<TabContainer>("%DeskTabs").GetNode("Directory/BpaDirectory").GetChild(0).GetNode("Vendors/Contents");
            foreach (var vendor in offers)
                Require(directory.GetChildren().OfType<Button>().Any(b => b.Text.Contains(vendor.Name, StringComparison.OrdinalIgnoreCase) && (vendor.Standing is null || b.Text.Contains(vendor.Standing))), "Directory preserves Sim vendor standing.");
            GD.Print("LEDGER_SMOKE_PASS: 90-day costs, coordination, funding expiry, vendor standing and pure bindings."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"LEDGER_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
