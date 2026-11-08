using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Game.Tests;

public partial class SessionSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<GameShell>();
            AddChild(shell);
            var picker = shell.GetNode<OptionButton>("%ScenarioPicker");
            var spoke = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemText(i) == "spoke-b");
            picker.Select(spoke);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, spoke);
            Require(shell.Session.View is null, "Scenario selection waits for prep before creating a session.");
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            Require(shell.Session.StateHash == Session.Create(bundle, 1).StateHash(), "Deploy without picks starts seed 1 with no prep.");
            var approach = shell.GetNode<OptionButton>("%ReplayPicker");
            var first = Enumerable.Range(0, approach.ItemCount).Single(i => approach.GetItemText(i) == "approach1 as written");
            approach.Select(first);
            shell.GetNode<Button>("%ReplayButton").EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.View!.Day == 1 && shell.Session.CompletedDay == 0, "Replay resets to before day 1.");
            var next = shell.GetNode<Button>("%NextDay");
            for (var i = 0; i < 10; i++) next.EmitSignal(Button.SignalName.Pressed);
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json"));
            var expected = Replay.Run(bundle, log, stopAfterDay: 10);
            Require(shell.Session.StateHash == expected.StateHash(), "Ten UI steps equal the Sim replay, including all dated actions.");
            var power = shell.Session.View!.Capabilities.Single(c => c.SiteId == "spokeBSite" && c.CapabilityId == "power");
            Require(shell.Session.CompletedDay == 10 && power.TodayPermille == 0, "Spoke B power is down on day 10 under approach 1.");
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            Require(map.Conditions["spokeBSite"] == SiteCondition.Down, "Board marks the lowest capability down.");
            Require(shell.GetNode<ProgressBar>("%CashGauge").Value == expected.View.CashCents, "Cash gauge reads the session.");
            var strip = expected.View.TopStrip();
            Require(shell.GetNode<ProgressBar>("%CashGauge").MaxValue == strip.CashFullCents, "Cash maximum comes from TopStrip.");
            Require(shell.GetNode<ProgressBar>("%ShopHoursGauge").MaxValue == strip.ShopHoursPerDay, "Shop maximum comes from TopStrip.");
            Require(shell.GetNode<Label>("%DayCounter").Text == "Day 11 (next)", "Day label honors the Sim's next-day convention.");
            Require(shell.GetNode<Label>("%InboxDay").Text.Contains("Day 10 results"), "Inbox names the day just played.");
            var inbox = shell.GetNode<RichTextLabel>("%InboxRecords");
            Require(!inbox.BbcodeEnabled, "Record text cannot become markup.");
            foreach (var record in expected.View.MorningBoard().Items)
                Require(inbox.Text.Contains($"[{record.Category} · {record.Severity}] {record.Text}"), "Inbox renders the Sim-classified morning board items.");

            var args = OS.GetCmdlineUserArgs();
            var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                Require(shot + 1 < args.Length && DisplayServer.GetName() != "headless", "Screenshot needs a display and path.");
                shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 6;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Screenshot saved.");
            }
            while (!shell.Session.View!.Finished) next.EmitSignal(Button.SignalName.Pressed);
            var finished = shell.Session.StateHash;
            next.EmitSignal(Button.SignalName.Pressed);
            Require(next.Disabled && shell.Session.StateHash == finished, "Finished sessions cannot advance again.");
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, spoke);
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == Session.Create(bundle, 1).StateHash(), "Switching scenario clears replay state.");
            var ninety = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemText(i) == "spoke-b-90");
            picker.Select(ninety);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, ninety);
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            var longBundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b-90"));
            Require(shell.Session.StateHash == Session.Create(longBundle, 1).StateHash() && shell.Session.View!.LastDay == 90, "The 90-day scenario starts a fresh seeded session.");
            next.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.CompletedDay == 1, "The 90-day scenario advances normally.");

            var empty = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<GameShell>();
            empty.ScenarioRootOverride = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            AddChild(empty);
            Require(empty.Session.View is null && empty.GetNode<Button>("%NextDay").Disabled, "No scenario holds no Session and disables advance.");
            GD.Print("SESSION_SMOKE_PASS: real session, replay, day-10 power down, board, cash, inbox, finish and reset.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError($"SESSION_SMOKE_FAIL: {error}");
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
