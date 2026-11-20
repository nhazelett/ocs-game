using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

// Runs in DecisionsSmokeTest, including in the existing CI smoke suite.
internal static class StopWorkSmokeChecks
{
    internal static async Task Run(Node scene, GameShell shell)
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-stop-work-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var root = Path.GetDirectoryName(GamePaths.ScenarioRoot)!;
            var log = InputLog.Load(Path.Combine(root, "docs/reviews/plays/2026-11-18/input-log.json"));
            var fixture = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 4);
            var call = fixture.Work.Single(w => w.SubjectId == "player-award-2");
            var workId = call.Id;
            var sentence = fixture.View.StopWorkSentence(workId)!;
            var savePath = Path.Combine(temporary, "day5.json");
            File.WriteAllText(savePath, fixture.ToSave().ToJson());
            shell.DeveloperToggle.ButtonPressed = false;
            shell.LoadSavedGame(savePath);
            OpenRequest(); await Frames();
            Require(shell.Session.StateHash == fixture.StateHash(), "Reading stop controls changes no Sim state.");
            MatchRequest(fixture);
            var requestStop = StopButton(shell.RequestDecisionRows!, workId);
            MatchSentence(requestStop, sentence);
            Require(StopButtons(shell.RequestDecisionRows!).Count() == 1,
                "Only the unsigned buy has a request stop button; asks and the finished card buy do not.");
            Require(!StopButtons(shell.DecisionRows).Any(), "Today keeps its existing decision links.");
            await Capture("request-before", requestStop);

            shell.TeamButtons["avery"].EmitSignal(Button.SignalName.Pressed); await Frames();
            var teamStop = StopButton(shell.TeamPanel, workId);
            MatchSentence(teamStop, sentence);
            Require(StopButtons(shell.TeamPanel).Count() == fixture.View.Team.SelectMany(m => m.Work).Count(w => w.StopSentence is not null),
                "Team stop buttons follow exactly the Sim's nullable sentence, never other tasks.");
            await Capture("team-before", teamStop);
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);

            requestStop.EmitSignal(Button.SignalName.Pressed);
            Require(fixture.Apply(new StopWorkAction { Day = 5, Work = workId }).Accepted, "The independent Sim stops the call.");
            Require(shell.Session.StateHash == fixture.StateHash(), "The request button submits exactly the Sim action.");
            Require(shell.RequestWorkspace.Visible && shell.SelectedCard == "tutForklift", "Stopping keeps the request page open.");
            MatchRequest(fixture);
            Require(!StopButtons(shell.RequestDecisionRows!).Any(), "The stopped buy loses its button immediately.");
            Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(fixture.View.Decisions()[0].Sentence),
                "Stopping confirms the exact new decision in a full sentence.");
            var afterStop = shell.Session.StateHash;
            requestStop.EmitSignal(Button.SignalName.Pressed); teamStop.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == afterStop, "Duplicate and stale controls cannot stop work again.");
            var original = shell.RequestDecisionRows!.GetChildren().OfType<Control>()
                .Single(c => c.GetMeta("decision_now").AsString() == "Stopped on day 5 before it was signed; nothing was bought.");
            await Capture("request-after", original);
            shell.TeamButtons["avery"].EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(!StopButtons(shell.TeamPanel).Any(), "The stopped task leaves the team page immediately.");
            await Capture("team-after", shell.TeamBackButton);
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);
            shell.SaveButton.EmitSignal(Button.SignalName.Pressed);
            shell.LoadSavedGame(shell.AutosavePath); OpenRequest();
            Require(shell.Session.StateHash == afterStop && !StopButtons(shell.RequestDecisionRows!).Any(),
                "Save and resume retain the stopped decision and restored request standing.");

            // DESK-118: a stopped buy keeps its name even after it leaves the active queue.
            Require(fixture.View.AwardIdTaken("player-award-2") && !fixture.View.InWork("player-award-2"),
                "The stopped call is a reserved name outside active work.");
            await BuyAfterStop(fixture, "gpc", "same-day");
            Step(shell); fixture.AdvanceDay();
            Require(shell.Session.StateHash == fixture.StateHash(), "The day loop preserves the independent post-stop state.");
            await BuyAfterStop(fixture, "purchaseOrder", "next-day");

            // The team button performs the same change without requiring the request page to be open.
            shell.CloseRequestWorkspace(); shell.LoadSavedGame(savePath);
            shell.TeamButtons["avery"].EmitSignal(Button.SignalName.Pressed);
            StopButton(shell.TeamPanel, workId).EmitSignal(Button.SignalName.Pressed);
            Require(shell.TeamPanel.Visible && shell.Session.StateHash == afterStop && !StopButtons(shell.TeamPanel).Any(),
                "The team button stops the same call, refreshes its task list, and keeps the team page open.");
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);

            // Previously spent hours are explained verbatim; advancing invalidates the old button.
            var later = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 9);
            var order = later.Work.Single(w => w.SubjectId == "player-award-4");
            File.WriteAllText(savePath, later.ToSave().ToJson()); shell.LoadSavedGame(savePath);
            shell.TeamButtons["avery"].EmitSignal(Button.SignalName.Pressed);
            var spentStop = StopButton(shell.TeamPanel, order.Id);
            MatchSentence(spentStop, later.View.StopWorkSentence(order.Id)!);
            Require(later.View.StopWorkSentence(order.Id)!.Contains("The 8 hours"), "The fixture has eight hours already spent.");
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);
            Step(shell); var nextDayHash = shell.Session.StateHash;
            spentStop.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == nextDayHash, "A stop button from yesterday cannot commit today.");

            // Once the call is finished it has no button in either place.
            var finished = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 8);
            File.WriteAllText(savePath, finished.ToSave().ToJson()); shell.LoadSavedGame(savePath); OpenRequest();
            Require(!StopButtons(shell.RequestDecisionRows!).Any(b => b.GetMeta("stop_work").AsString() == workId),
                "A finished call has no request stop button.");
            shell.TeamButtons["avery"].EmitSignal(Button.SignalName.Pressed);
            Require(!StopButtons(shell.TeamPanel).Any(b => b.GetMeta("stop_work").AsString() == workId),
                "A finished call has no team stop button.");
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed);
            GD.Print("STOP_WORK_SMOKE_PASS: day-five request and team controls, exact sentences, independent state hashes, standing and queue refresh, save/resume, spent hours, finished buys and stale/duplicate guards.");
        }
        finally { Directory.Delete(temporary, true); }

        async Task BuyAfterStop(Session expected, string instrument, string name)
        {
            PickAwardTarget(shell, "tutWater"); shell.ShowMarket();
            Pick(shell.InstrumentInput, instrument); Pick(shell.VendorInput, "islandMart");
            shell.QuantityInput.Value = 500; shell.StartInput.Value = expected.Day; shell.DurationInput.Value = 2;
            PickPriorities(shell, "fast", "good");
            var buy = shell.PendingAward!;
            Require(!expected.View.AwardIdTaken(buy.Id) && buy.Id != "player-award-2",
                "The real buy form skips the stopped call's reserved name.");
            Require(shell.AwardPreview?.Accepted == true && !shell.AwardButton.Disabled,
                "A different request has an enabled quote after a stop: " + shell.AwardBlockedReason.Text);
            await Frames();
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--buy-after-stop-screenshots");
            if (at >= 0)
            {
                await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = scene.GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[at + 1] + "-" + name + ".png") == Error.Ok, "Saved the enabled quote after stopping work.");
            }
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(expected.Apply(buy).Accepted && shell.Session.StateHash == expected.StateHash(),
                "Arrange support submits the exact new buy after a stop on the " + name + ".");
        }

        void OpenRequest() => shell.NavigateBoardItem(new BoardItem("", shell.Session.View!.Day, BoardCategory.Request,
            BoardSeverity.Info, BoardLinkKind.Card, "tutForklift", "", []));
        void MatchRequest(Session expected)
        {
            var standing = expected.View.WhereThingsStand().Single(e => e.CardId == "tutForklift");
            var actual = PlayScreen.Descendants(shell.RequestContents).OfType<Label>()
                .Single(l => l.HasMeta("request_standing"));
            Require(actual.Text == standing.Sentence, "The request displays the exact current standing from Sim.");
            foreach (var d in expected.View.Decisions("tutForklift"))
                Require(PlayScreen.Descendants(shell.RequestDecisionRows!).OfType<Label>().Any(l => l.Text.Contains(d.Sentence) && l.Text.Contains(d.Now)),
                    "The request shows every current decision and its state from Sim.");
        }
        async Task Frames() { for (var i = 0; i < 8; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string name, Control target)
        {
            await Frames();
            var scroll = target.FindParent("Team") is not null
                ? PlayScreen.Descendants(shell.TeamPanel).OfType<ScrollContainer>().Single() : shell.RequestScroll;
            // Show the task near the top, with the explanation fully in view.
            if (name == "team-after") scroll.ScrollVertical = 0;
            else scroll.ScrollVertical += (int)(target.GlobalPosition.Y - scroll.GlobalPosition.Y - 140);
            await Frames();
            if (name.EndsWith("before"))
            {
                var label = target.GetParent().GetChildren().OfType<Label>().Single(l => l.HasMeta("stop_work_sentence"));
                Require(scroll.GetGlobalRect().Encloses(target.GetGlobalRect()) && scroll.GetGlobalRect().Encloses(label.GetGlobalRect()),
                    "The stop button and its whole explanation fit together inside the page's scroll.");
            }
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--stop-screenshots");
            if (at < 0) return;
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + name + ".png") == Error.Ok, "Saved the actual day-five stop-work screen.");
        }
    }

    private static IEnumerable<Button> StopButtons(Node node) => PlayScreen.Descendants(node).OfType<Button>().Where(b => b.HasMeta("stop_work"));
    private static Button StopButton(Node node, string id) => StopButtons(node).Single(b => b.GetMeta("stop_work").AsString() == id);
    private static void MatchSentence(Button button, string sentence)
    {
        var siblings = button.GetParent().GetChildren();
        Require(button.Text == "Stop this work" && !button.Disabled && siblings[button.GetIndex() + 1] is Label label && label.Text == sentence,
            "The exact Sim sentence sits directly below the enabled Stop this work button.");
    }
}
