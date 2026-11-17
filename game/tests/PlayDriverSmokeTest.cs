using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

// The oracle may inspect Sim; the driver under test cannot.
public partial class PlayDriverSmokeTest : Node
{
    public override async void _Ready()
    {
        var scratch = Path.Combine(Path.GetTempPath(), "ocs-play-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var driver = new PlayDriver { TestMode = true }; AddChild(driver);
            await driver.Initialize("tutorial", 20261116, scratch);
            var shell = driver.Shell;
            var screen = new PlayScreen(shell);
            async Task Click(string label, int occurrence = 1)
            {
                var result = await driver.Execute(new("test", "click", label, Occurrence: occurrence));
                GD.Print(result);
            }
            async Task Review(string suffix)
            {
                var args = OS.GetCmdlineUserArgs();
                var index = Array.IndexOf(args, "--screenshots");
                if (index < 0) return;
                var paths = await driver.Capture("Native play-driver review: " + suffix);
                File.Copy(paths.png, args[index + 1] + "-" + suffix + ".png", true);
                File.Copy(paths.txt, args[index + 1] + "-" + suffix + ".txt", true);
            }
            Require(!shell.DeveloperToggle.ButtonPressed && shell.Session.Prep is not null, "Normal New game, no developer load.");
            Require(!screen.Describe("").Contains("End the day"), "The covered desk is absent during preparations.");
            var before = screen.Describe("");
            Require((await driver.Execute(new("test", "click", "End the day"))).Contains("Nothing changed"), "Covered controls cannot be clicked.");
            Require(before == screen.Describe(""), "A missing label leaves the screen unchanged.");
            await Click("Your team · choose names and faces");
            await Click("Face 1");
            await driver.Capture("Picker opened");
            GD.Print($"Picker scratch: {scratch}");
            Require(screen.Read().Any(e => e.Kind == "choice" && e.Text == "No portrait"), "An opened picker lists its visible choices.");
            await Click("No portrait");
            Require(shell.TeamFaceInputs.Values.First().Selected == 0, "A popup choice uses the real menu input path.");
            await driver.Execute(new("test", "type", "Name", "Morgan Test", 2));
            Require(shell.TeamNameInputs.Values.ElementAt(1).Text == "Morgan Test", "Duplicate fields select the requested visible occurrence.");
            await Click("Use these names and faces");
            await Click("I'm ready to leave");
            Require(shell.Session.CreateSave().Seed == 20261116, "The requested seed reaches the ordinary New game session.");
            for (var i = 0; i < 10 && shell.ReadingTurnover; i++)
                await Click(screen.Read().Any(e => e.Text == "Open the desk and meet people") ? "Open the desk and meet people" : "Continue");
            Require(!shell.ReadingTurnover, "Turnover uses only the visible Continue choices.");
            await Click("Requests");
            await Click(shell.RequestListButtons["tutWater"].Text);
            for (var i = 0; i < 12 && !screen.Read().Any(e => e.Kind == "button" && e.Text == "Ask MSgt Carla Ortiz"); i++)
                await driver.Execute(new("test", "scroll", "Request", "down"));
            Require(screen.Read().Any(e => e.Text == "Ask MSgt Carla Ortiz"), "The helper becomes reachable through visible scrolling.");
            await Click("Ask MSgt Carla Ortiz");
            Require(shell.Session.CreateSave().Actions.Count > 0, "A real player decision is recorded before the day is played.");
            await Click("Back to the desk");
            await Click("Today");
            await Click("End the day");
            Require(shell.EndDayDialog.Visible && shell.Session.CompletedDay == 0, "The plan opens before advancing.");
            await Review("plan");
            Require(!screen.Read().Any(e => e.Text == "Menu"), "The modal excludes controls behind it.");
            await driver.Execute(new("test", "key", "Escape"));
            Require(!shell.EndDayDialog.Visible && shell.Session.CompletedDay == 0, "Escape cancels the real dialog.");
            await Click("End the day"); await Click("End the day");
            await driver.Capture("After day confirmation");
            GD.Print($"After confirmation: completed={shell.Session.CompletedDay}, plan={shell.EndDayDialog.Visible}, playback={shell.PlayingDay}, results={shell.ReadingDaySummary}; scratch={scratch}");
            Require(shell.Session.CompletedDay == 1 && shell.ReadingDaySummary, "One confirmation plays one day and settles on results.");
            await Review("results");
            await Click("Back to Today");
            Require(screen.Read().Any(e => e.Pane == "Map" && e.Text.Contains("Hub")), "Map words come from the rendered map controls.");
            var paths = await driver.Capture("PLAY_DRIVER_SMOKE_PASS");
            using var image = Image.LoadFromFile(paths.png);
            Require(image.GetWidth() == 1280 && image.GetHeight() == 800, "The native screen is 1280 by 800.");
            var log = InputLog.Load(Path.Combine(scratch, "input-log.json"));
            var replay = LinesOfSupport.Sim.Replay.Resume(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial")), log);
            Require(replay.StateHash() == shell.Session.StateHash, "The game's own input log replays the exact reached state, seed, prep and team.");
            // A small synthetic scroll proves off-screen lines cannot leak to the player.
            var cover = new PanelContainer { ZIndex = 50 }; shell.AddChild(cover);
            cover.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(300, 40), SizeFlagsVertical = Control.SizeFlags.ShrinkBegin };
            var fixture = new VBoxContainer(); cover.AddChild(fixture); fixture.AddChild(scroll);
            var label = new Label { Text = "Visible line\nVisible second\nHIDDEN SENTINEL\nStill hidden", CustomMinimumSize = new Vector2(300, 140) };
            scroll.AddChild(label);
            var invoked = false;
            var grey = new Button { Text = "Unavailable action", Disabled = true };
            grey.Pressed += () => invoked = true; fixture.AddChild(grey);
            fixture.AddChild(new Label { Text = "You need more hours for this task." });
            fixture.AddChild(new Label { Text = "Test quantity" });
            var number = new SpinBox { MinValue = 1, MaxValue = 20, Value = 3 }; fixture.AddChild(number);
            for (var i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(!screen.Describe("").Contains("HIDDEN SENTINEL"), "Clipped label lines never enter the transcript.");
            Require(screen.Describe("").Contains("You need more hours for this task."), "The visible reason remains under its greyed control.");
            Require((await driver.Execute(new("test", "click", "Unavailable action"))).Contains("greyed") && !invoked,
                "A greyed control cannot invoke its handler.");
            await driver.Execute(new("test", "type", "Test quantity", "7"));
            Require(number.Value == 7, "Typing a numeric field uses its visible caption and commits the value.");
            cover.QueueFree();
            GD.Print("PLAY_DRIVER_SMOKE_PASS: visible New game, seed, duplicate/numeric fields, popup, clipped text, greyed/missing controls, modal Escape, one real day, native screenshot and exact replay.");
            Directory.Delete(scratch, true);
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PLAY_DRIVER_SMOKE_FAIL: {error}"); GetTree().Quit(1); }

    }
}
