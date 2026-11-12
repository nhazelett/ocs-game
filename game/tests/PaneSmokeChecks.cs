using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class PaneSmokeChecks
{
    internal static async Task Run(Node parent)
    {
        var shell = Shell(parent);
        try
        {
            Scenario(shell, "tutorial"); Step(shell, 3);
            shell.DeveloperToggle.ButtonPressed = false;
            var tabs = shell.GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = 2;
            PickAwardTarget(shell, "tutForklift");
            var hash = shell.Session.StateHash;
            Blocked("Choose an available supplier.");
            await Frames(); await Capture("greyed", false);
            Pick(shell.VendorInput, "harborEquipment"); Blocked("Choose an available arrangement.");
            Pick(shell.InstrumentInput, "bpaCall"); Blocked("Choose an available vehicle.");
            Pick(shell.VehicleInput, "equipBpa");
            Require(shell.AwardButton.Disabled && shell.AwardBlockedReason.Text == shell.AwardPreview!.HardStops[0], "Missing priorities use the first exact Sim stop.");
            PickPriorities(shell, "fast", "good");
            Require(!shell.AwardButton.Disabled && !shell.AwardBlockedReason.Visible, "A ready award has no stale disabled reason.");
            Require(hash == shell.Session.StateHash, "Choosing terms and reading reasons remain read only.");
            await Frames(); await Capture("ready", true);
            shell.QuantityInput.Value = 6;
            Require(shell.AwardButton.Disabled && shell.AwardBlockedReason.Text == shell.AwardPreview!.HardStops[0], "The first hard stop appears directly below the action.");
            await Capture("refused", true);
            var stopped = shell.Session.StateHash;
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == stopped, "Forcing a refused award cannot submit.");
            foreach (var i in Enumerable.Range(0, tabs.GetTabCount()))
            {
                tabs.CurrentTab = i; await Frames();
                var scrolls = Walk(tabs.GetCurrentTabControl()).OfType<ScrollContainer>().Where(s => s.IsVisibleInTree()).ToArray();
                Require(scrolls.Length <= 1, "A desk pane has at most one visible scroll, including the full debrief.");
            }
            tabs.CurrentTab = 0; shell.PeopleToggle.ButtonPressed = true; await Frames();
            CheckFooter(shell.VisitButtons["finance"]);
            var today = Walk(tabs.GetCurrentTabControl()).OfType<ScrollContainer>().Single(s => s.IsVisibleInTree());
            today.ScrollVertical = 100000; await Frames(); CheckFooter(shell.VisitButtons["finance"]);
            tabs.CurrentTab = 1; await Frames(); CheckFooter(shell.RequirementMarketButton);
            var request = Walk(tabs.GetCurrentTabControl()).OfType<ScrollContainer>().Single(s => s.IsVisibleInTree());
            request.ScrollVertical = 100000; await Frames(); CheckFooter(shell.RequirementMarketButton);
            tabs.CurrentTab = 3;
            shell.EstablishVendorButtons["harborEquipment"].EmitSignal(Button.SignalName.Pressed);
            await Frames();
            Require(shell.EstablishButton.Disabled, "A vehicle with no scope is refused.");
            CheckFooter(shell.EstablishButton);
            var setupScroll = Walk(tabs.GetCurrentTabControl()).OfType<ScrollContainer>().Single(s => s.IsVisibleInTree());
            setupScroll.ScrollVertical = 100000; await Frames(); CheckFooter(shell.EstablishButton);
            shell.VendorSetupBack.EmitSignal(Button.SignalName.Pressed);
            GD.Print("PANE_SMOKE_PASS: single scroll, pinned award and setup actions, missing choices, exact hard stops, pure previews and refused-submit guard.");

            void Blocked(string text)
            {
                Require(shell.AwardButton.Disabled && shell.AwardBlockedReason.Visible && shell.AwardBlockedReason.Text == text,
                    "The disabled button states the next missing choice: " + text);
            }
            void CheckFooter(Button button)
            {
                Require(parent.GetViewport().GetVisibleRect().Encloses(button.GetGlobalRect()), "The primary action stays in the window.");
                var reason = button.GetParent().GetChild<Label>(button.GetIndex() + 1);
                if (button.Disabled)
                    Require(reason.IsVisibleInTree() && reason.Text.Length > 0
                        && reason.GetGlobalRect().Position.Y >= button.GetGlobalRect().End.Y
                        && parent.GetViewport().GetVisibleRect().Encloses(reason.GetGlobalRect()), "The complete reason is immediately under the button and visible.");
            }
            async Task Capture(string suffix, bool bottom)
            {
                shell.AwardScroll.ScrollVertical = bottom ? 100000 : 0; await Frames();
                CheckFooter(shell.AwardButton);
                var rect = shell.AwardButton.GetGlobalRect();
                shell.AwardScroll.ScrollVertical = bottom ? 0 : 100000; await Frames();
                Require(shell.AwardButton.GetGlobalRect() == rect, "Scrolling the form never moves its primary action.");
                shell.AwardScroll.ScrollVertical = bottom ? (int)shell.AwardTrade!.GetParent<Control>().Position.Y : 0; await Frames();
                var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--screenshots");
                if (index < 0) return;
                await parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = parent.GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[index + 1] + "-pane-" + suffix + ".png") == Error.Ok, "Saved the actual pane screenshot.");
            }
        }
        finally
        {
            shell.QueueFree(); await Frames();
        }
        async Task Frames()
        {
            for (var i = 0; i < 5; i++) await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static IEnumerable<Node> Walk(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren()) foreach (var descendant in Walk(child)) yield return descendant;
    }
}
