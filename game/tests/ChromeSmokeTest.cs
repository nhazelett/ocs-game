using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
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
            var cashLabel = shell.GetNode<Label>("%CashLabel");
            Require(cashLabel.Text == "Cash: $50,000 of $50,000", "The day-one cash comparison uses readable whole dollars.");
            var caption = shell.GetNode<Label>("%BoardCaption");
            Require(shell.SelectedCapability is null && !caption.Visible && caption.Text == "", "A fresh deployment has no invented selected capability.");
            await Capture("day1");
            Require(cashLabel.GetLineCount() == 1, "The whole-dollar cash line fits one line at the default viewport.");
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
            DeveloperWorkspace(shell); Scenario(shell, "spoke-b"); Replay(shell); Step(shell, 10);
            shell.DeveloperToggle.ButtonPressed = false;
            Require(shell.Session.View!.Day == 11 && shell.SelectedCapability is null && !caption.Visible,
                "Replaying a new deployment clears the old map selection.");
            var selectionHash = shell.Session.StateHash;
            shell.NavigateBoardItem(new BoardItem("", 11, BoardCategory.Request, BoardSeverity.Info, BoardLinkKind.SiteCapability, "spokeBSite.power", "", []));
            Require(caption.Visible && caption.Text == "Selected: Power at Spoke B", "The map caption names the actual selected capability.");
            await Capture("day11");
            Require(cashLabel.GetLineCount() == 1 && cash.Value == shell.Session.View.TopStrip().CashCents,
                "Day eleven keeps cash on one line while the gauge retains exact cents.");
            Require(shell.Session.StateHash == selectionHash, "Map selection and rendering change no Sim state.");
            PickAwardTarget(shell, "hubSite.fuel");
            var partner = Enumerable.Range(0, shell.VendorInput.ItemCount).Single(i => shell.VendorInput.GetItemMetadata(i).AsString() == "partnerForce");
            Require(shell.CurrentActionEditor!.Vendors.Single(v => v.Id == "partnerForce").Name == "the partner force"
                && shell.VendorInput.GetItemText(partner).StartsWith("The partner force")
                && !shell.VendorCardContents.ContainsKey("partnerForce"),
                "Market capitalises the partner list label; partner support has no buyable vendor card.");
            Require(shell.Session.StateHash == selectionHash, "Vendor label formatting changes no Sim state.");
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed); shell.ContinueGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.SelectedCapability is null && !caption.Visible && caption.Text == "", "Continue starts with no stale map selection.");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("CHROME_SMOKE_PASS: normal preparation/morning, whole-dollar cash on one line, actual map selection and reset, capitalised partner labels and unchanged Sim actions."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"CHROME_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }
}
