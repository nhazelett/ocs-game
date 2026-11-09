using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ChromeSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-chrome-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Screenshot saved.");
            }
            Require(shell.PrepChoices.Values.Any(c => c.ButtonPressed) && shell.PrepChoices.Values.Any(c => !c.ButtonPressed),
                "Preparation screenshot exercises both real checked and unchecked choices.");
            Require(!shell.DeveloperToggle.ButtonPressed, "Normal entry keeps developer tools unchecked.");
            await Capture("prep");
            DeployWithoutPrep(shell); shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
            var hash = shell.Session.StateHash;
            var strip = shell.Session.View!.TopStrip();
            Require(shell.GetNode<Label>("%DayCounter").Text == "Morning of day 1" && shell.GetNode<Label>("%InboxDay").Text == "Your first morning", "Initial morning and inbox have no day-zero/results ambiguity.");
            Require(shell.GetNode<Label>("%ShopHoursLabel").Text.Contains("none spoken for") && strip.ShopHoursQueued == 0,
                "An empty work queue reads naturally without inventing available hours.");
            var cash = shell.GetNode<ProgressBar>("%CashGauge");
            Require(cash.Value == strip.CashCents && cash.MaxValue == strip.CashFullCents && cash.Value == cash.MaxValue,
                "The authored full balance fills the cash gauge using exact Sim values.");
            Require(shell.GetNode<Label>("%CashLabel").Text.Contains($"{DeskUi.Money(strip.CashCents)} of {DeskUi.Money(strip.CashFullCents)}"), "Visible wording explains the cash comparison.");
            await Capture("day1");
            Require(shell.GetNode<Button>("%NextDay").GetGlobalRect().End.X <= GetViewport().GetVisibleRect().End.X,
                "Longer top-strip wording leaves Next day on screen.");
            Require(shell.Session.StateHash == hash, "Rendering the new chrome changes no simulation state.");
            Pick(shell.PersonContactInput, "finance"); shell.VisitButtons["finance"].EmitSignal(Button.SignalName.Pressed);
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            Require(expected.Apply(new FindFinanceAction { Day = expected.Day }).Accepted, "Finance fixture is a valid actual action.");
            strip = expected.View.TopStrip();
            Require(strip.ShopHoursQueued > 0 && shell.Session.StateHash == expected.StateHash(), "Real visit changes only the same Sim state as direct play.");
            Require(shell.GetNode<Label>("%ShopHoursLabel").Text == $"{strip.ShopHoursQueued:0.#} of {strip.ShopHoursPerDay:0.#} hours spoken for",
                "Queued wording binds to actual committed shop hours, including fractions.");
            Step(shell); expected.AdvanceDay();
            Require(shell.GetNode<Label>("%DayCounter").Text == "Morning of day 2" && shell.GetNode<Label>("%InboxDay").Text == "What happened on day 1"
                && shell.Session.StateHash == expected.StateHash(), "Later mornings name the completed day without changing advance behavior.");
            while (!shell.Session.View!.Finished) Step(shell);
            Require(shell.GetNode<Label>("%DayCounter").Text == "Day 30 complete", "Finished deployment keeps the final-day wording.");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("CHROME_SMOKE_PASS: normal preparation/morning, actual queue/cash, layout and unchanged Sim actions."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"CHROME_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }
}
