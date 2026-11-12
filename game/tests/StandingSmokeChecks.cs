using Godot;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class StandingSmokeChecks
{
    internal static async Task Run(Node parent)
    {
        var shell = Shell(parent);
        try
        {
            Scenario(shell, "tutorial"); Replay(shell, "approach1 taught");
            shell.DeveloperToggle.ButtonPressed = false;
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            var scroll = tabs.GetNode<ScrollContainer>("Today/TodayPane/Body/Body");
            var seen = new HashSet<string>();
            for (var morning = 1; morning <= shell.Session.View!.LastDay; morning++)
            {
                tabs.CurrentTab = GameShell.TodayTab; await Frames();
                var hash = shell.Session.StateHash;
                var entries = shell.Session.View.WhereThingsStand();
                var rows = shell.StandingRows.GetChildren().OfType<HBoxContainer>().ToArray();
                Require(rows.Select(r => r.GetMeta("standing_card").AsString()).SequenceEqual(entries.Select(e => e.CardId)),
                    "Every request appears once, in the Sim's attention-first order.");
                Require(shell.StandingRows.GetParent().GetIndex() < shell.PeopleToggle.GetIndex(), "Standings sit under the lead and above optional visits.");
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i]; seen.Add(entry.State);
                    var button = shell.StandingOpenButtons[entry.CardId];
                    Require(button.Text.EndsWith("\n" + entry.Sentence) && button.Text.StartsWith(entry.Title)
                        && (entry.From is null || button.Text.Contains(entry.From.DisplayName)), "Each row keeps its exact request, asker and Sim sentence.");
                    Require(button.GetMeta("standing_state").AsString() == entry.State, "Status comes directly from Sim.");
                    var portrait = rows[i].GetChildren().OfType<PersonPortrait>().Single();
                    Require(portrait.PortraitKey == (entry.From?.Portrait ?? "fallback"), "Each row uses the asker's declared face, with a neutral fallback.");
                    Require(button.GetGlobalRect().End.X <= scroll.GetGlobalRect().End.X, "Long sentences wrap inside the main scroll.");
                    button.EmitSignal(Button.SignalName.Pressed);
                    Require(tabs.CurrentTab == GameShell.RequestsTab && shell.SelectedCard == entry.CardId && !shell.MarketOpen,
                        "Each standing opens its own request, including after market editing.");
                    tabs.CurrentTab = GameShell.TodayTab;
                }
                Require(shell.Session.StateHash == hash, "Rendering and opening every standing never changes Sim state.");
                if (morning is 1 or 4 or 9) await Capture(morning);
                if (morning == 4)
                {
                    var stale = shell.StandingOpenButtons.Values.First();
                    Step(shell); tabs.CurrentTab = GameShell.TodayTab;
                    stale.EmitSignal(Button.SignalName.Pressed);
                    Require(tabs.CurrentTab == GameShell.TodayTab, "A removed row cannot navigate after the day changes.");
                }
                else Step(shell);
            }
            Require(seen.Contains(RequestStanding.Nothing) && seen.Contains(RequestStanding.Buying)
                && seen.Contains(RequestStanding.Running) && seen.Contains(RequestStanding.Ended), "Tutorial exercises untouched, being-written, running and ended readings.");
            DeveloperWorkspace(shell); Scenario(shell, "spoke-b");
            var newIds = shell.Session.View!.WhereThingsStand().Select(e => e.CardId);
            Require(shell.StandingOpenButtons.Keys.SequenceEqual(newIds) && !shell.StandingOpenButtons.ContainsKey("tutWater"), "Changing deployments clears old request rows.");
            GD.Print("STANDING_SMOKE_PASS: exact ordered sentences and asker faces, every tutorial morning, pure request links, stale-row guard, wrap geometry and scenario reset.");

            async Task Capture(int day)
            {
                var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--standing-screenshots");
                if (shot < 0) return;
                Require(DisplayServer.GetName() != "headless", "Standing captures need a real display.");
                scroll.ScrollVertical = 0; await Frames();
                if (day > 1)
                {
                    var panel = shell.StandingRows.GetParent<Control>();
                    scroll.ScrollVertical = (int)(panel.GlobalPosition.Y - scroll.GlobalPosition.Y);
                    await Frames();
                }
                await parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var texture = parent.GetViewport().GetTexture(); using var image = texture.GetImage();
                Require(image.SavePng(args[shot + 1] + "-day" + day + ".png") == Error.Ok, "Standing capture saved.");
            }
        }
        finally { shell.QueueFree(); await Frames(); }
        async Task Frames()
        {
            for (var i = 0; i < 4; i++) await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
