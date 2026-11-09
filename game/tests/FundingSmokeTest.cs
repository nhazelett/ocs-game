using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using System.Text.Json.Nodes;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class FundingSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-funding-ui-" + Guid.NewGuid().ToString("N"));
        try
        {
            var shell = Shell(this);
            shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json");
            Scenario(shell, "spoke-b-90");
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b-90"));
            var expected = Session.Create(bundle, 1);
            var lateSeen = false; var expirySeen = false; var releaseSeen = false; var lapseSeen = false;
            while (true)
            {
                var view = shell.Session.View!;
                var hash = shell.Session.StateHash;
                var ledger = shell.GetNode<TabContainer>("%DeskTabs").GetNode("Ledger/LedgerContext/Contents");
                foreach (var line in view.Funding)
                {
                    var row = ledger.GetChildren().OfType<Label>().Single(l => l.HasMeta("funding") && l.GetMeta("funding").AsString() == line.Id);
                    Require(row.Text.Contains("Purpose: any"), "An unrestricted line shows purpose any.");
                    var late = line.AvailableDay > line.PlannedAvailableDay;
                    Require(row.Text.Contains($"planned day {line.PlannedAvailableDay}, now day {line.AvailableDay}") == late, "Delayed release dates bind to Sim and appear only after the delay.");
                    lateSeen |= late;
                }
                foreach (var option in shell.CurrentActionEditor!.Funding)
                {
                    var index = Index(shell.FundingInput, option.Id);
                    Require(shell.FundingInput.IsItemDisabled(index) == (!option.UsableToday || !option.PaysForThis), "Funding disabled state follows the current view.");
                    if (!option.UsableToday)
                        Require(shell.FundingInput.GetItemTooltip(index).Contains(view.Day < option.AvailableDay ? $"Available day {option.AvailableDay}" : $"Expired after day {option.ExpiresDay}"), "Unusable line explains release or expiry.");
                }
                foreach (var item in view.MorningBoard().Items.Where(i => i.LinkKind == BoardLinkKind.Funding))
                {
                    var button = shell.MorningItems.GetChildren().OfType<Button>().Single(b => b.Text.EndsWith(item.Text));
                    Require(!button.Disabled, "Funding notice is clickable.");
                    button.EmitSignal(Button.SignalName.Pressed);
                    Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 5, "Notice opens Ledger.");
                    if (view.Funding.Any(f => f.Id == item.LinkId))
                    {
                        Require(shell.SelectedFunding == item.LinkId, "Notice selects its actual funding line.");
                        var selected = ledger.GetChildren().OfType<Label>().Single(l => l.HasMeta("funding") && l.GetMeta("funding").AsString() == item.LinkId);
                        Require(selected.Text.StartsWith("Selected funding line: " + view.FundingName(item.LinkId)), "Linked line has a visible selection marker.");
                        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                        Require(selected.HasFocus(), "Linked line receives keyboard focus after layout.");
                    }
                    else
                    {
                        Require(shell.SelectedFunding is null && ledger.GetChildren().OfType<Label>().Any(l => l.Text == item.Text), "General no-funds notice is shown without a stale selected line.");
                        lapseSeen = true;
                    }
                    expirySeen |= item.Text.Contains("reached its fiscal-year end");
                    releaseSeen |= item.Text.StartsWith("Comptroller released");
                }
                Require(shell.Session.StateHash == hash, "Reading and following funding notices is pure.");
                if (view.Day == 77)
                {
                    Require(Enumerable.Range(0, shell.FundingInput.ItemCount).All(shell.FundingInput.IsItemDisabled), "Both expired and unreleased lines are disabled during the lapse.");
                    Require(!shell.AwardPreview!.Accepted && shell.AwardButton.Disabled, "No-funds window blocks Award using Sim's preview.");
                }
                if (view.Finished) break;
                Step(shell); expected.AdvanceDay();
            }
            Require(lateSeen && expirySeen && releaseSeen && lapseSeen, "Fixture covers delay, expiry, release and the no-funds window.");
            Require(shell.Session.StateHash == expected.StateHash(), "Ninety-day funding inspection leaves the run unchanged.");
            var gap = shell.DisplayedDebrief!.Gaps.First(g => g.Factors.Any(f => f.Kind == GapFactorKind.FundingLapse));
            shell.DebriefPanel.GetChildren().OfType<Button>().Single(b => b.HasMeta("gap") && b.GetMeta("gap").AsInt32() == gap.Index).EmitSignal(Button.SignalName.Pressed);
            Require(shell.DebriefPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.StartsWith("Funding lapse ·")), "Expanded debrief uses a readable funding label.");

            // A temporary data fixture tests purpose restrictions without changing authored scenarios.
            var data = JsonNode.Parse(File.ReadAllText(Path.Combine(bundle.Directory, "scenario.json")))!;
            data["fundingLines"]![0]!["capabilities"] = new JsonArray("fuel");
            var fixture = Path.Combine(temporary, "purpose"); Directory.CreateDirectory(fixture);
            var rules = Path.Combine(temporary, "rules"); Directory.CreateDirectory(rules);
            File.WriteAllText(Path.Combine(fixture, "scenario.json"), data.ToJsonString());
            File.Copy(Path.Combine(GamePaths.ScenarioRoot, "rules", "rules-2025-10.json"), Path.Combine(rules, "rules-2025-10.json"));
            var purposeShell = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<GameShell>();
            purposeShell.ScenarioRootOverride = temporary;
            purposeShell.AutosavePathOverride = Path.Combine(temporary, "purpose-save.json");
            AddChild(purposeShell); Scenario(purposeShell, "purpose");
            var purposeView = purposeShell.CurrentActionEditor!;
            Require(purposeView.CapabilityId == "power" && !purposeView.Funding.Single(f => f.Id == "om1").PaysForThis, "Power editor uses a real wrong-purpose fixture.");
            var wrong = Index(purposeShell.FundingInput, "om1");
            Require(purposeShell.FundingInput.IsItemDisabled(wrong) && purposeShell.FundingInput.GetItemTooltip(wrong).Contains("Certified for fuel only"), "Wrong-purpose line is greyed with its purpose reason even while usable.");
            Require(purposeShell.AwardButton.Disabled, "All unavailable funding leaves a Sim hard stop.");
            var purposeLedger = purposeShell.GetNode<TabContainer>("%DeskTabs").GetNode("Ledger/LedgerContext/Contents");
            Require(purposeLedger.GetChildren().OfType<Label>().Any(l => l.HasMeta("funding") && l.GetMeta("funding").AsString() == "om1" && l.Text.Contains("Purpose: fuel")), "Ledger shows the restricted purpose.");
            var before = purposeShell.Session.StateHash;
            var targets = purposeShell.GetNode<TabContainer>("%DeskTabs").GetNode("Actions/AwardEditor").FindChildren("*", "OptionButton", true, false).OfType<OptionButton>().First();
            Pick(targets, "hubSite.fuel");
            ChoosePreviewFixture(purposeShell);
            var right = Index(purposeShell.FundingInput, "om1");
            Require(!purposeShell.FundingInput.IsItemDisabled(right) && purposeShell.FundingInput.GetItemTooltip(right) == "", "Changing capability re-enables purpose-matching funds and clears the reason.");
            Require(purposeShell.Session.StateHash == before, "Purpose editing changes presentation only.");
            Scenario(shell, "spoke-b");
            Require(shell.SelectedFunding is null, "Scenario switch clears funding selection.");
            GD.Print("FUNDING_SMOKE_PASS: purposes, late release, expiry window, clickable notices, focus, debrief and pure bindings.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"FUNDING_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }

    private static int Index(OptionButton picker, string id)
        => Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemMetadata(i).AsString() == id);
}
