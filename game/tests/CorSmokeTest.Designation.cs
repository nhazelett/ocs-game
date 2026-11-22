using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class CorSmokeTest
{
    private async Task CheckDesignationWords(GameShell shell, string temporary)
    {
        // Seventh 22 Nov play, second player: Lane on the waste order, morning fourteen.
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var log = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-22-7", "input-log-2.json"));
        var expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 13);
        Require(expected.Day == 14, "The original play reaches the morning of the COR change.");
        var path = Path.Combine(temporary, "day14.json");
        File.WriteAllText(path, expected.ToSave().ToJson()); shell.LoadSavedGame(path);
        shell.DeveloperToggle.ButtonPressed = false;
        var tabs = shell.GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = GameShell.PaymentsTab;
        await Frames();
        var ledger = tabs.GetNode<VBoxContainer>("Payments/LedgerContext/Contents");
        var waste = expected.Arrangements.Single(a => a.CreatedDay > 0 && a.CapabilityId == "waste");
        var controls = shell.CorDesignations[waste.Id];
        var explanation = ledger.GetChildren().OfType<Label>().Single(l => l.HasMeta("cor_designation_why") && l.GetMeta("cor_designation_why").AsString() == waste.Id);
        var caption = ledger.GetChildren().OfType<Label>().Single(l => l.Text == "COR for " + expected.View.ArrangementName(waste.Id));
        Require(caption.GetIndex() + 1 == controls.Picker.GetIndex(), "Each COR caption names the contract immediately above its own picker.");
        Require(controls.Submit.Disabled && !explanation.Visible && explanation.Text.Length == 0, "The current COR has only the already-assigned reason, with no stale load explanation.");
        var unchanged = shell.Session.StateHash;
        foreach (var cor in expected.Cors)
        {
            Pick(controls.Picker, cor.Id);
            var text = controls.Picker.GetItemText(controls.Picker.Selected);
            Require(text.Contains(cor.Name) && text.EndsWith(expected.View.CorSummary(cor, waste)), "Every contract choice carries the authoritative fit, rate and reporting lag.");
            Require(explanation.Text == (expected.View.CorDesignationWhy(cor.Id, waste.Id) ?? "") && explanation.Visible == (explanation.Text.Length > 0), "Changing the COR rereads the exact contract-specific fit and load sentence.");
            Require(shell.Session.StateHash == unchanged, "Reading the COR's effect never changes the play.");
        }
        Pick(controls.Picker, "corLane"); await Frames();
        Require(!controls.Submit.Disabled && explanation.Text.Contains("Naming SSgt Lane here also lowers the watch on"), "Lane's effect on the contracts she already watches is visible before the swap.");
        Require(explanation.GetIndex() > controls.Submit.GetIndex(), "The explanation sits beneath Designate COR.");
        var scroll = ledger.GetParent<ScrollContainer>();
        scroll.ScrollVertical += Mathf.RoundToInt(caption.GlobalPosition.Y - scroll.GlobalPosition.Y);
        await Frames();
        Require(scroll.GetGlobalRect().Encloses(caption.GetGlobalRect()) && scroll.GetGlobalRect().Encloses(controls.Picker.GetGlobalRect())
            && scroll.GetGlobalRect().Encloses(controls.Submit.GetGlobalRect()) && scroll.GetGlobalRect().Encloses(explanation.GetGlobalRect()), "The contract caption, Lane's choice, button and explanation fit together in Payments at the normal viewport.");
        var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--designation-screenshot");
        if (shot >= 0)
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[shot + 1]) == Error.Ok, "Native day-fourteen Payments capture saved.");
        }
        var action = log.Actions.OfType<DesignateCorAction>().Single(a => a.Day == 14);
        var result = expected.Apply(action); Require(result.Accepted, "The recorded Lane swap is accepted.");
        controls.Submit.EmitSignal(Button.SignalName.Pressed);
        Require(shell.Session.StateHash == expected.StateHash(), "Payments submits precisely the recorded COR change.");
        var record = expected.View.Records.Single(r => r.Id == result.RecordId);
        Require(shell.GetNode<Label>("%BuildStatus").Text == record.Text && record.Text.Contains("replacing TSgt Pratt"), "The visible result is the full designation record, including the replaced COR and the actual rate.");
        controls.Submit.EmitSignal(Button.SignalName.Pressed);
        Require(shell.Session.StateHash == expected.StateHash(), "The retired control cannot submit a duplicate designation.");
        Require(shell.CorDesignations[waste.Id].Submit.Disabled, "After the change, Lane is the disabled current selection.");
        var refreshed = ledger.GetChildren().OfType<Label>().Single(l => l.HasMeta("cor_designation_why") && l.GetMeta("cor_designation_why").AsString() == waste.Id);
        Require(!refreshed.Visible && refreshed.Text.Length == 0, "The completed change clears the old proposed-load explanation.");
        shell.SaveButton.EmitSignal(Button.SignalName.Pressed); shell.LoadSavedGame(shell.AutosavePath);
        Require(shell.Session.StateHash == expected.StateHash(), "The COR change survives the actual Save control and replay.");
        GD.Print("COR_DESIGNATION_SMOKE_PASS: original day-fourteen play, contract captions, exact fit/load choices, full result, stale guard and save/replay.");

        async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    }
}
