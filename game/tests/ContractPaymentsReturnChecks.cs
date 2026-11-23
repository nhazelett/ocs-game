using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ContractSmokeTest
{
    private async Task CheckColdPaymentsReturn(GameShell shell)
    {
        var input = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-23", "input-log-2.json"));
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var expected = LinesOfSupport.Sim.Replay.Run(bundle, input, stopAfterDay: 9);
        var id = expected.View.Arrangements.Single(a => a.VendorId == "coastalRentals" && a.CapabilityId == "mhe").Id;
        var save = shell.AutosavePathOverride + ".payments-return";
        var tabs = shell.GetNode<TabContainer>("%DeskTabs");
        var scroll = tabs.GetNode<ScrollContainer>("Payments/LedgerContext");
        var contents = scroll.GetNode<VBoxContainer>("Contents");
        try
        {
            foreach (var oldScroll in new[] { 0, 100000 })
            {
                Scenario(shell, "tutorial");
                File.WriteAllText(save, expected.ToSave().ToJson()); shell.LoadSavedGame(save); shell.DeveloperToggle.ButtonPressed = false;
                tabs.CurrentTab = GameShell.PaymentsTab; await Frames();
                scroll.ScrollVertical = oldScroll; await Frames();
                scroll.EnsureControlVisible(Row()); await Frames();
                var hash = shell.Session.StateHash; var log = shell.Session.CreateSave().ToJson();
                Row().EmitSignal(Button.SignalName.Pressed); await Frames();
                Require(shell.ContractView.Visible && shell.SelectedArrangement == id, "The actual cold player's scrolled Payments row opens its own running contract.");
                Back().EmitSignal(Button.SignalName.Pressed); await Frames();
                var row = Row();
                Require(!shell.ContractView.Visible && tabs.CurrentTab == GameShell.PaymentsTab && row.ButtonPressed
                    && scroll.GetGlobalRect().Encloses(row.GetGlobalRect())
                    && Math.Abs(row.GlobalPosition.Y - scroll.GlobalPosition.Y) <= 2,
                    "Returning reveals the complete selected contract at the top after selected-first reordering, from either old scroll.");
                Require(shell.Session.StateHash == hash && shell.Session.CreateSave().ToJson() == log, "Opening and returning change neither Sim nor save.");
            }
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--payments-return-screenshot");
            if (at >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[at + 1]) == Error.Ok, "The actual cold day-ten return is captured.");
            }
            Row().EmitSignal(Button.SignalName.Pressed); await Frames(); Back().EmitSignal(Button.SignalName.Pressed);
            tabs.CurrentTab = GameShell.TodayTab; var before = scroll.ScrollVertical; await Frames();
            Require(tabs.CurrentTab == GameShell.TodayTab && scroll.ScrollVertical == before, "Deferred return focus respects immediately leaving Payments.");
            GD.Print("PAYMENTS_RETURN_SMOKE_PASS: actual day-ten log, both old scrolls, complete selected row, pure Sim/save and tab-leave guard.");
        }
        finally { File.Delete(save); }
        Button Row() => contents.GetChildren().OfType<Button>().Single(b => b.HasMeta("arrangement") && b.GetMeta("arrangement").AsString() == id);
        Button Back() => PlayScreen.Descendants(shell.ContractView).OfType<Button>().Single(b => b.Text == "Back to what we are paying for");
        async Task Frames() { for (var frame = 0; frame < 8; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    }
}
