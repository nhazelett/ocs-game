using Godot;

namespace LinesOfSupport.Game.Tests;

// Run as a separate scene; the normal player scene has no test flags or test controls.
public partial class ShellSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Control>();
            AddChild(shell);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            var expected = new[] { "Board", "Requirements", "Actions", "Directory", "Vehicles", "Ledger", "Inbox", "Reports", "Debrief" };
            Require(tabs.GetTabCount() == expected.Length, "All nine desk tabs exist.");
            for (var i = 0; i < expected.Length; i++)
            {
                Require(tabs.GetTabTitle(i) == expected[i], $"Tab {i}: {expected[i]}.");
                Require(tabs.GetTabControl(i).GetChildCount() == 0, "Desk panels remain empty.");
                tabs.CurrentTab = i;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(tabs.GetCurrentTabControl().Visible, "Selected tab is visible.");
            }
            tabs.CurrentTab = 0;
            var viewport = shell.GetNode<SubViewport>("%BoardViewport");
            Require(viewport.Size.X > 1 && viewport.Size.Y > 1, "Board viewport has a valid size.");
            Require(viewport.GetNode<Node2D>("SchematicMap") is not null, "The board contains its 2D schematic.");
            Require(shell.GetNode<ProgressBar>("%ShopHoursGauge") is not null, "Shop-hours gauge exists.");
            Require(shell.GetNode<ProgressBar>("%CashGauge") is not null, "Cash gauge exists.");
            var day = shell.GetNode<Label>("%DayCounter");
            var before = day.Text;
            var openingStatus = shell.GetNode<Label>("%BuildStatus").Text;
            shell.GetNode<Button>("%NextDay").EmitSignal(Button.SignalName.Pressed);
            Require(day.Text == before, "No day is invented before Sim is connected.");
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains("unavailable"), "Button feedback explains the no-op.");

            var arguments = OS.GetCmdlineUserArgs();
            var screenshotIndex = Array.IndexOf(arguments, "--screenshot");
            if (screenshotIndex >= 0)
            {
                Require(screenshotIndex + 1 < arguments.Length, "Screenshot needs an output path.");
                Require(DisplayServer.GetName() != "headless", "Screenshots require a rendering display.");
                // Restore the normal opening state for the handoff screenshot.
                shell.GetNode<Label>("%BuildStatus").Text = openingStatus;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var path = arguments[screenshotIndex + 1];
                Require(GetViewport().GetTexture().GetImage().SavePng(path) == Error.Ok, "Screenshot saved.");
                GD.Print($"SHELL_SCREENSHOT: {path}");
            }
            GD.Print("SHELL_SMOKE_PASS: nine tabs, board viewport, gauges, next-day no-op.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"SHELL_SMOKE_FAIL: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
