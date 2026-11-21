using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class AssignedHelpSmokeChecks
{
    internal static async Task Run(GameShell shell, Node scene)
    {
        var sent = new List<PlayerAction>(); shell.Session.ActionAccepted += sent.Add;
        void Open(string card) => shell.NavigateBoardItem(new BoardItem("", shell.Session.View!.Day, BoardCategory.Request,
            BoardSeverity.Info, BoardLinkKind.Card, card, "", []));
        void Match(AssigneePicker picker, PlayerAction action)
        {
            var choices = shell.Session.View!.AssigneeChoices(action);
            Require(picker.Choices.SequenceEqual(choices) && choices.All(c => picker.ChoiceButtons[c.MemberId].Text == c.Sentence
                && picker.ChoiceButtons[c.MemberId].Icon is not null), "Every teammate has Sim's exact load, pace, result day and a face.");
            Require(picker.GetChildren().OfType<Label>().Single().Text == "Who does it", "The action uses the requested picker caption.");
        }
        Label Cost(Button button, PlayerAction action, string member)
        {
            var quote = shell.Session.View!.PreviewAny(action);
            var label = button.GetParent().GetChildren().OfType<Label>().First(l => l.GetIndex() > button.GetIndex() && l.HasMeta("action_cost"));
            var hours = GameShell.PreviewHours(quote);
            Require(label.Text.StartsWith(hours + " of ")
                && label.Text.Contains(member) && label.Text.Contains("Earliest result: day " + quote.EarliestStartMin),
                "The cost under the button names the chosen person's actual preview hours and result day.");
            return label;
        }
        void Commit(Button button, PlayerAction action, string member)
        {
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, shell.Session.View!.Scenario.Id));
            var expected = LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave());
            Require(expected.Apply(action).Accepted, "The selected action is accepted by Sim.");
            button.EmitSignal(Button.SignalName.Pressed);
            Require(sent.Last() == action && shell.Session.StateHash == expected.StateHash(), "The exact selected action is sent once.");
            Require(shell.Session.View!.Team.Single(m => m.Id == member).Work.Count > 0, "The chosen teammate receives the real work.");
            var count = sent.Count; button.EmitSignal(Button.SignalName.Pressed);
            Require(sent.Count == count, "A stale accepted button cannot repeat work.");
            Require(LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave()).StateHash() == expected.StateHash(),
                "The assignee survives save and replay.");
            Step(shell); expected.AdvanceDay();
            Require(shell.Session.StateHash == expected.StateHash(), "The real day loop uses that teammate's pace and queue.");
        }
        async Task Frames() { for (var i = 0; i < 6; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string name, AssigneePicker picker, Label cost)
        {
            shell.DeveloperToggle.ButtonPressed = false; await Frames();
            Node? node = picker.GetParent(); while (node is not null && node is not ScrollContainer) node = node.GetParent();
            var scroll = (ScrollContainer)node!; scroll.EnsureControlVisible(picker); await Frames();
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--assigned-screenshots"); if (at < 0) return;
            Require(scroll.GetGlobalRect().Encloses(picker.GetGlobalRect()), "All three complete choices fit in the review viewport.");
            scroll.EnsureControlVisible(cost); await Frames();
            Require(scroll.GetGlobalRect().Encloses(cost.GetGlobalRect())
                && scroll.GetGlobalRect().Encloses(picker.ChoiceButtons[picker.Selected].GetGlobalRect()),
                "The selected teammate and complete committing cost are visible together.");
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + name + ".png") == Error.Ok, "Native assigned-action screenshot saved.");
            shell.DeveloperToggle.ButtonPressed = true;
        }

        Scenario(shell, "tutorial"); Step(shell, 2);
        shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab;
        shell.PeopleToggle.ButtonPressed = true; Pick(shell.PersonContactInput, "meeting");
        var picker = shell.VisitAssignees["meeting"]; var action = shell.VisitOptions["meeting"].Action;
        Require(shell.Session.View!.Day == 3 && picker.Selected == "you", "Day-three meeting starts with you selected."); Match(picker, action);
        var hash = shell.Session.StateHash; picker.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
        action = shell.VisitOptions["meeting"].Action;
        Require(action is JoinStaffMeetingAction { Assignee: "novak", Attend: true } && shell.Session.StateHash == hash,
            "Picking Novak changes only the proposal.");
        await Capture("meeting", picker, Cost(shell.VisitButtons["meeting"], action, "Novak"));
        Commit(shell.VisitButtons["meeting"], action, "novak");
        Require(!shell.VisitAssignees.ContainsKey("meeting"), "Pending or attended meetings have no duplicate joining picker.");

        Scenario(shell, "tutorial"); Step(shell, 3); Open("tutForklift");
        var helper = shell.Session.View!.RequirementEditor("tutForklift")!.WhoCanHelp!.First(h => h.Person?.Id == "dunn");
        picker = shell.RequestHelperAssignees["ask:dunn"]; Match(picker, helper.Start!);
        Require(shell.Session.View.Day == 4 && picker.Selected == "you", "Day-four asks default to you.");
        hash = shell.Session.StateHash; picker.ChoiceButtons["avery"].EmitSignal(Button.SignalName.Pressed);
        action = helper.Start! with { Assignee = "avery" };
        Require(hash == shell.Session.StateHash, "Picking an asker does not spend hours or reveal an answer.");
        await Capture("ask", picker, Cost(shell.RequestHelperButtons["ask:dunn"], action, "Avery"));
        Commit(shell.RequestHelperButtons["ask:dunn"], action, "avery"); Open("tutForklift");
        Require(!shell.RequestHelperAssignees.ContainsKey("ask:dunn"), "An ask in work or answered cannot be started again.");
        var vendor = shell.Session.View!.RequirementEditor("tutForklift")!.WhoCanHelp!.First(h => h.Kind == "vendor");
        var key = "vendor:" + vendor.Person!.Id; picker = shell.RequestHelperAssignees[key]; Match(picker, vendor.Start!);
        picker.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed); action = vendor.Start! with { Assignee = "novak" };
        Cost(shell.RequestHelperButtons[key], action, "Novak"); Commit(shell.RequestHelperButtons[key], action, "novak");

        Scenario(shell, "spoke-b"); Open("req0007");
        helper = shell.Session.View!.RequirementEditor("req0007")!.WhoCanHelp!.First(h => h.Kind == "j4");
        shell.RequestHelperButtons["j4:" + helper.CapabilityId].EmitSignal(Button.SignalName.Pressed);
        picker = shell.VisitAssignees["j4"]; Match(picker, shell.VisitOptions["j4"].Action);
        picker.ChoiceButtons["avery"].EmitSignal(Button.SignalName.Pressed); shell.J4QuantityInput!.Value -= 1;
        Require(shell.VisitAssignees["j4"].Selected == "avery", "Changing the J4 quantity keeps the chosen teammate.");
        action = shell.VisitOptions["j4"].Action; Cost(shell.VisitButtons["j4"], action, "Avery"); Commit(shell.VisitButtons["j4"], action, "avery");

        Scenario(shell, "tutorial"); Step(shell, 3); Open("tutForklift");
        var stale = shell.RequestHelperButtons["ask:dunn"]; picker = shell.RequestHelperAssignees["ask:dunn"];
        Open("tutWater"); var sentCount = sent.Count; hash = shell.Session.StateHash;
        picker.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed); stale.EmitSignal(Button.SignalName.Pressed);
        Require(sent.Count == sentCount && hash == shell.Session.StateHash, "A replaced helper picker and action cannot send work for another request.");
        GD.Print("ASSIGNED_HELP_SMOKE_PASS: exact forecasts, named hours, pure selections, meeting/request/vendor/J4 assignees, actual day loop, save/replay, quantity retention and stale guards.");
    }
}
