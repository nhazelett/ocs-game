using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class PlainBoardSmokeChecks
{
    internal static async Task Run(GameShell shell, Node scene)
    {
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
        var input = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json"));
        var expected = LinesOfSupport.Sim.Replay.Run(bundle, input, stopAfterDay: 0);
        var path = Path.Combine(Path.GetTempPath(), "ocs-plain-board-" + Guid.NewGuid() + ".json");
        var seen = new HashSet<BoardCategory>();
        try
        {
            while (!expected.Finished && seen.Count < 2)
            {
                var board = expected.View.MorningBoard();
                foreach (var category in new[] { BoardCategory.VendorNotice, BoardCategory.HigherHq })
                {
                    var item = board.Items.FirstOrDefault(i => i.Category == category);
                    if (item is null || !seen.Add(category)) continue;
                    // Sim finds the authored news; the real saved-game path renders only those mornings.
                    File.WriteAllText(path, expected.ToSave().ToJson());
                    shell.LoadSavedGame(path);
                    Require(shell.Session.StateHash == expected.StateHash(), "The captured morning is the exact authored replay state.");
                    var label = category == BoardCategory.VendorNotice ? "Vendor notice" : "Higher headquarters";
                    var button = shell.MorningItems.GetChildren().OfType<Button>().Single(b => b.Text.Contains(item.Text));
                    var inbox = shell.GetNode<RichTextLabel>("%InboxRecords");
                    Require(button.Text.StartsWith(item.Severity + " · " + label + "\n") && inbox.Text.Contains("[" + label + " · " + item.Severity + "] " + item.Text),
                        "Today and Inbox use the same plain category on real authored news.");
                    Require(!inbox.Text.Contains("VendorNotice") && !inbox.Text.Contains("HigherHq"), "Inbox exposes no category enum names.");
                    shell.DeveloperToggle.ButtonPressed = false;
                    shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab;
                    shell.BoardDetailsToggle.ButtonPressed = true;
                    await Frames();
                    shell.GetNode<ScrollContainer>("%DeskTabs/Today/TodayPane/Body/Body").EnsureControlVisible(button);
                    await Capture(category + "-today");
                    shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.InboxTab;
                    await Frames();
                    var recordsScroll = shell.GetNode<ScrollContainer>("%DeskTabs/Inbox/Contents");
                    recordsScroll.ScrollVertical += (int)(inbox.GlobalPosition.Y - recordsScroll.GlobalPosition.Y);
                    await Capture(category + "-inbox");
                    shell.DeveloperToggle.ButtonPressed = true;
                }
                if (seen.Count < 2)
                {
                    foreach (var action in input.Actions.Where(a => a.Day == expected.Day)) expected.Apply(action);
                    expected.AdvanceDay();
                }
            }
            Require(seen.Count == 2, "The replay exercises both compound category names.");
        }
        finally { File.Delete(path); }
        async Task Frames() { for (var i = 0; i < 8; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string suffix)
        {
            await Frames();
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--polish-screenshots");
            if (at < 0) return;
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Plain category screenshot saved.");
        }
    }
}
