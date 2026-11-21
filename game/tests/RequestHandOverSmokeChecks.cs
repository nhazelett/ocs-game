using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

// Eighth play's second brief, exercised by the existing Decisions CI scene.
internal static class RequestHandOverSmokeChecks
{
    internal static async Task Run(Node scene, GameShell shell)
    {
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var log = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-8", "input-log-2.json"));
        var expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 6);
        foreach (var action in log.Actions.Where(a => a.Day == 7).TakeWhile(a => a is not AssignAction))
            Require(expected.Apply(action).Accepted, "The actual day-seven buy is accepted before its hand-over.");
        Load(expected); shell.GoToMarket("tutWaste"); await Frames();
        var assign = log.Actions.OfType<AssignAction>().Single(a => a.Day == 7);
        var picker = Controls<AssigneePicker>().Single(p => Meta(p, assign.Work));
        var give = Controls<Button>().Single(b => Meta(b, assign.Work));
        Require(picker.Selected == "you" && give.Disabled && picker.Choices.SequenceEqual(expected.View.AssigneeChoices(assign)),
            "The request hand-over starts with the real holder and the team's exact authoritative done-day forecasts.");
        Require(Controls<Button>().Count(b => b.HasMeta("stop_work") && b.GetMeta("stop_work").AsString() == assign.Work) == 1,
            "The hand-over sits with the single existing Stop control, without duplicating it in the decision history.");
        var hash = shell.Session.StateHash;
        picker.ChoiceButtons[assign.To].EmitSignal(Button.SignalName.Pressed); await Frames();
        Require(!give.Disabled && hash == shell.Session.StateHash, "Choosing the replacement previews without spending hours.");
        await Capture("request", shell.RequestScroll, give);
        Require(expected.Apply(assign).Accepted, "Independent Sim accepts the original hand-over.");
        give.EmitSignal(Button.SignalName.Pressed);
        Require(shell.Session.StateHash == expected.StateHash() && shell.RequestWorkspace.Visible && shell.SelectedCard == "tutWaste",
            "The request sends the exact assignment and remains on the request.");
        var after = shell.Session.StateHash; give.EmitSignal(Button.SignalName.Pressed);
        picker.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
        Require(shell.Session.StateHash == after, "Old request controls cannot assign twice or change the refreshed work.");
        Require(Controls<AssigneePicker>().Single(p => Meta(p, assign.Work)).Selected == assign.To,
            "After giving the work, the request names its new holder.");
        Require(LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave()).StateHash() == after, "Request assignment survives save/replay.");
        Step(shell); expected.AdvanceDay();
        Require(shell.Session.StateHash == expected.StateHash(), "The next day uses the handed-over work and its actual pace.");

        var day13 = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 12);
        Load(day13);
        var tabs = shell.GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = GameShell.InboxTab; await Frames();
        var issue = day13.View.PerformanceIssues.Single(i => i.CapabilityId == "food");
        var link = shell.PerformanceChoices[(issue.Id, "terminate")];
        Require(link.Text == "Open its contract page →" && link.GetMeta("performance_contract").AsString() == issue.ArrangementId,
            "The vendor decision links to the same arrangement that cure and re-source concern.");
        var inbox = tabs.GetNode<ScrollContainer>("Inbox/Contents");
        await Capture("inbox", inbox, link);
        hash = shell.Session.StateHash; link.EmitSignal(Button.SignalName.Pressed); await Frames();
        Require(shell.ContractView.Visible && shell.SelectedArrangement == issue.ArrangementId
            && shell.TerminateButtons[issue.ArrangementId].Text.StartsWith("End this contract") && shell.Session.StateHash == hash,
            "The Inbox link reveals End this contract without ending anything or spending hours.");
        shell.CloseContractView(); Step(shell);
        hash = shell.Session.StateHash; link.EmitSignal(Button.SignalName.Pressed);
        Require(!shell.ContractView.Visible && shell.Session.StateHash == hash, "Yesterday's Inbox link cannot navigate into a changed decision.");
        GD.Print("REQUEST_HANDOVER_SMOKE_PASS: eighth-play day-seven assignment, exact previews and execution, single Stop, refreshed holder, stale guards, save/replay and day-thirteen Inbox contract navigation.");

        IEnumerable<T> Controls<T>() where T : Node => PlayScreen.Descendants(shell.RequestContents).OfType<T>();
        static bool Meta(Node n, string id) => n.HasMeta("handover_work") && n.GetMeta("handover_work").AsString() == id;
        void Load(Session fixture)
        {
            var path = shell.AutosavePathOverride + ".handover";
            File.WriteAllText(path, fixture.ToSave().ToJson()); shell.LoadSavedGame(path); File.Delete(path);
            shell.DeveloperToggle.ButtonPressed = false;
        }
        async Task Frames() { for (var i = 0; i < 8; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string suffix, ScrollContainer scroll, Control focus)
        {
            await Frames(); scroll.EnsureControlVisible(focus); await Frames();
            Require(scroll.GetGlobalRect().Encloses(focus.GetGlobalRect()), "The real hand-over or contract link fits its existing scroll.");
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--handover-screenshots"); if (at < 0) return;
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "The exact eighth-play screen is captured.");
        }
    }
}
