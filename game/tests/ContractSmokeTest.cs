using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ContractSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            var tabs = shell.GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = GameShell.PaymentsTab;
            shell.DeveloperToggle.ButtonPressed = false;
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                for (var i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Screenshot saved.");
            }
            var ledger = tabs.GetNode("Payments/LedgerContext/Contents");
            Require(shell.TerminateButtons.Count == 0 && !ledger.GetChildren().OfType<Button>().Any(b => b.Text.StartsWith("Terminate") || b.Text.StartsWith("End this")),
                "The payment list has no termination controls.");
            var hash = shell.Session.StateHash;
            foreach (var inherited in shell.Session.View!.Arrangements.Where(a => shell.Session.View.TerminateBlocked(a.Id) is not null))
            {
                var inheritedRow = ledger.GetChildren().OfType<Button>().Single(b => b.HasMeta("arrangement") && b.GetMeta("arrangement").AsString() == inherited.Id);
                Require(inheritedRow.Text.Contains("no recorded costs") && !inheritedRow.Text.Contains('$'), "Zero-cost inherited support uses one sentence.");
                shell.OpenContractView(inherited.Id);
                Require(shell.ContractView.Visible && shell.TerminateButtons.Count == 0, "Foreign support never offers an end-contract action.");
                Require(shell.ContractView.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == shell.Session.View.TerminateBlocked(inherited.Id)),
                    "The exact Sim ownership reason is shown.");
                Require(shell.Session.StateHash == hash, "Opening inherited support is read only.");
                await Capture("inherited");
                var back = shell.ContractView.FindChildren("*", "Button", true, false).OfType<Button>().Single(b => b.Text == "Back to what we are paying for");
                Require(back.GetGlobalRect().Position.Y >= shell.GetNode<Control>("Navigation").GetGlobalRect().End.Y,
                    "Contract navigation does not run underneath Menu or Developer tools.");
            }
            shell.CloseContractView(); shell.DeveloperToggle.ButtonPressed = true; Replay(shell); Step(shell, 8);
            shell.DeveloperToggle.ButtonPressed = false; tabs.CurrentTab = GameShell.PaymentsTab;
            var own = shell.Session.View!.Arrangements.First(a => !a.IsCoordination && a.Status == ArrangementStatus.Active && shell.Session.View.TerminateBlocked(a.Id) is null);
            await Capture("ledger");
            var row = ledger.GetChildren().OfType<Button>().Single(b => b.HasMeta("arrangement") && b.GetMeta("arrangement").AsString() == own.Id);
            Require(!row.Text.Contains("Unobligated incurred") && row.Text.Contains("Money committed"), "Payment amounts use plain labels.");
            row.EmitSignal(Button.SignalName.Pressed); hash = shell.Session.StateHash;
            Require(shell.ContractView.Visible && shell.TerminateButtons.Keys.Single() == own.Id, "Only the selected office contract offers the end action.");
            await Capture("contract");
            shell.TerminateButtons[own.Id].EmitSignal(Button.SignalName.Pressed);
            Require(shell.EndContractDialog.Visible && shell.Session.StateHash == hash, "Opening the confirmation sends no action.");
            Require(shell.EndContractDialog.DialogText.Contains("End this contract? Here is what stops.")
                && shell.EndContractDialog.DialogText.Contains(DeskUi.ListLabel(shell.Session.View.ArrangementName(own.Id))), "Confirmation names the real support being ended.");
            using (var panel = shell.EndContractDialog.GetThemeStylebox("panel", "AcceptDialog") as StyleBoxFlat)
                Require(panel is not null && panel.BgColor.IsEqualApprox(TabletopTheme.Paper), "Confirmation has the readable shared paper background.");
            await Capture("confirm");
            shell.EndContractDialog.Hide(); shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            Require(shell.Session.StateHash == hash, "Cancel keeps the contract unchanged.");
            shell.TerminateButtons[own.Id].EmitSignal(Button.SignalName.Pressed);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var expected = LinesOfSupport.Sim.Replay.Run(bundle, InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json")), 8);
            Require(expected.Apply(new TerminateAction { Day = expected.Day, Arrangement = own.Id }).Accepted, "Own contract termination is accepted by Sim.");
            shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.StateHash == expected.StateHash() && shell.TerminateButtons[own.Id].Disabled, "Confirm queues exactly the expected action once.");
            shell.EndContractDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.StateHash == expected.StateHash(), "Stale repeated confirmation cannot duplicate work.");
            shell.CloseContractView(); shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("CONTRACT_SMOKE_PASS: plain payment list, inherited support protected, own contract detail, cancel and exact confirmed termination.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("CONTRACT_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
