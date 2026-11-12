using Godot;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

// Runs the real desk, including tab geometry and its request/market navigation.
public partial class ShellSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        var exitCode = 0;
        try
        {
            shell = Shell(this); Scenario(shell, "tutorial");
            shell.DeveloperToggle.ButtonPressed = false;
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            var expected = new[] { "Today", "Requests", "Vendors", "Payments", "Inbox", "Debrief" };
            Require(tabs.GetTabCount() == expected.Length, "Only the five desk pages and the finished-game Debrief exist.");
            Require(tabs.IsTabHidden(GameShell.DebriefTab), "Debrief is absent during play.");
            var hash = shell.Session.StateHash;
            for (var i = 0; i < expected.Length; i++)
            {
                Require(tabs.GetTabTitle(i) == expected[i], $"Tab {i}: {expected[i]}.");
                if (tabs.IsTabHidden(i)) continue;
                tabs.CurrentTab = i; await Frames();
                Require(tabs.GetCurrentTabControl().Visible, "Every live tab opens its page.");
                CheckTabs();
            }
            tabs.CurrentTab = GameShell.TodayTab; await Capture("today");
            var card = shell.Session.View!.Cards.First().Id;
            var request = new BoardItem("", 1, BoardCategory.Request, BoardSeverity.Info, BoardLinkKind.Card, card, "", []);
            shell.NavigateBoardItem(request);
            Require(tabs.CurrentTab == GameShell.RequestsTab && !shell.MarketOpen, "A request link opens the request inside Requests.");
            await Capture("request");
            shell.RequirementMarketButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(tabs.CurrentTab == GameShell.RequestsTab && shell.MarketOpen && shell.CurrentActionEditor!.CardId == card,
                "Go to market opens this request's market inside the same tab.");
            Require(shell.AwardButton.IsVisibleInTree(), "The market action remains reachable.");
            Require(Scrolls(tabs.GetCurrentTabControl()).Count(s => s.IsVisibleInTree()) == 1, "Request and market never show competing scroll panes.");
            await Capture("market");
            shell.AwardBackButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(!shell.MarketOpen && shell.SelectedCard == card && shell.RequirementMarketButton.IsVisibleInTree(), "Back restores the same request.");
            shell.RequestInitiativeButton.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(tabs.CurrentTab == GameShell.RequestsTab && shell.MarketOpen && shell.CurrentActionEditor!.CardId is null
                && shell.InitiativeTargetInput!.IsVisibleInTree(), "Own-initiative buying is reachable from Requests without an Actions tab.");
            shell.NavigateBoardItem(request); await Frames();
            Require(!shell.MarketOpen && shell.SelectedCard == card, "A new request link closes a previously open market.");
            Require(shell.Session.StateHash == hash, "Visiting tabs, requests and markets changes no Sim state.");
            var viewport = shell.GetNode<SubViewport>("%BoardViewport");
            Require(viewport.Size.X > 1 && viewport.Size.Y > 1, "The board keeps a valid viewport.");
            var day = shell.Session.View.Day;
            Step(shell); Require(shell.Session.View.Day == day + 1, "End the day still advances exactly one real day.");
            while (!shell.Session.View.Finished) Step(shell);
            Require(!tabs.IsTabHidden(GameShell.DebriefTab) && tabs.CurrentTab == GameShell.DebriefTab, "Completion reveals and opens Debrief.");
            shell.DebriefPanel.FindChildren("*", "Button", true, false).OfType<Button>()
                .Single(b => b.Text == "Back to the map").EmitSignal(Button.SignalName.Pressed);
            await Frames(); CheckTabs(); await Capture("finished");
            DeveloperWorkspace(shell); Scenario(shell, "tutorial"); await Frames();
            Require(tabs.IsTabHidden(GameShell.DebriefTab) && !shell.MarketOpen && tabs.TabsVisible,
                "A new deployment hides Debrief and resets the market pane.");
            GD.Print("SHELL_SMOKE_PASS: five fitting tabs, pure request/market/initiative navigation, one scroll, real day advance and finished-only Debrief.");

            void CheckTabs()
            {
                var bar = tabs.GetTabBar();
                Require(!bar.GetOffsetButtonsVisible(), "All tab titles fit without arrows at the default viewport.");
                for (var i = 0; i < tabs.GetTabCount(); i++)
                    if (!tabs.IsTabHidden(i)) Require(bar.GetRect().Size.X >= bar.GetTabRect(i).End.X, "Every visible tab title fits inside the bar.");
            }
            async Task Capture(string suffix)
            {
                await Frames(); CheckTabs();
                var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
                if (shot < 0) return;
                Require(DisplayServer.GetName() != "headless", "Screenshots require a rendering display.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native desk capture saved.");
            }
        }
        catch (Exception error) { GD.PushError($"SHELL_SMOKE_FAIL: {error}"); exitCode = 1; }
        finally
        {
            if (shell is not null) { shell.QueueFree(); await Frames(); }
        }
        GetTree().Quit(exitCode);
    }

    private async Task Frames()
    {
        for (var i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static IEnumerable<ScrollContainer> Scrolls(Node node)
    {
        if (node is ScrollContainer scroll) yield return scroll;
        foreach (var child in node.GetChildren()) foreach (var descendant in Scrolls(child)) yield return descendant;
    }
}
