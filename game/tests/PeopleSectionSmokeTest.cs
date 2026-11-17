using Godot;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PeopleSectionSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); AddChild(shell);
            shell.AutosavePathOverride = Path.Combine(Path.GetTempPath(), "ocs-people-section-" + Guid.NewGuid() + ".json");
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed); FinishOpening(shell);
            var tabs = shell.GetNode<TabContainer>("%DeskTabs"); tabs.CurrentTab = GameShell.TodayTab;
            var section = PlayScreen.Descendants(shell).Single(n => n.Name == "ReachPeople");
            Require(shell.Session.View!.MorningBoard().OpenRequests == 1 && shell.MorningHeading.Text.EndsWith("1 open request"),
                "The tutorial's first open request uses the singular count.");
            shell.PeopleToggle.ButtonPressed = true;
            Require(shell.PersonContactInput.Selected == -1 && shell.PersonContactInput.Text == "Choose someone to reach…"
                && shell.VisitButtons.Count == 0, "People to reach opens with an explicit prompt and no unchosen action.");
            await Frames();
            tabs.GetNode<ScrollContainer>("Today/TodayPane/Body/Body").EnsureControlVisible(shell.PersonContactInput);
            await Capture("people-prompt");
            var contacts = Enumerable.Range(0, shell.PersonContactInput.ItemCount)
                .Select(i => shell.PersonContactInput.GetItemMetadata(i).AsString()).ToArray();
            foreach (var contact in contacts)
            {
                Pick(shell.PersonContactInput, contact); shell.PeopleToggle.ButtonPressed = true;
                foreach (var button in shell.VisitButtons.Values)
                {
                    Require(Within(button, section) && button.IsVisibleInTree(), "Every contact's action belongs to its open section.");
                }
                shell.PeopleToggle.ButtonPressed = false;
                Require(shell.VisitButtons.Values.All(b => !b.IsVisibleInTree()), "Closing the section hides every contact's buttons.");
            }
            // Repeated full refreshes used to leave Find finance pinned below a closed section.
            for (var i = 0; i < 8; i++)
            {
                Step(shell);
                Require(!shell.PeopleToggle.ButtonPressed && shell.VisitButtons.Values.All(b => !b.IsVisibleInTree()),
                    "Daily refreshes cannot expose a closed section's controls.");
            }
            var missed = shell.Session.View!.Cards.First(c => c.Heard is not null && c.RaisedInRoomDay is null);
            shell.NavigateBoardItem(new BoardItem("", shell.Session.View.Day, BoardCategory.Request, BoardSeverity.Info,
                BoardLinkKind.Card, missed.Id, "", []));
            var link = PlayScreen.Descendants(tabs.GetNode("Requests/RequirementPane")).OfType<Button>()
                .Single(b => b.HasMeta("staff_meeting_link"));
            Require(link.GetMeta("staff_meeting_link").AsString() == missed.Id, "The missed meeting's request carries the exact link.");
            await Frames(); tabs.GetNode<ScrollContainer>("Requests/RequirementPane/Body/Body").EnsureControlVisible(link);
            await Capture("request-link");
            var hash = shell.Session.StateHash;
            link.EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.Session.StateHash == hash && tabs.CurrentTab == GameShell.TodayTab && shell.PeopleToggle.ButtonPressed
                && shell.PersonContactInput.GetItemMetadata(shell.PersonContactInput.Selected).AsString() == "meeting",
                "The link opens the staff meeting in People to reach without starting work or changing Sim.");
            var join = shell.VisitButtons["meeting"];
            var today = tabs.GetNode<ScrollContainer>("Today/TodayPane/Body/Body");
            Require(Within(join, section) && today.GetGlobalRect().Encloses(join.GetGlobalRect()), "The actual meeting action is visible inside the section.");
            await Capture("meeting-open");
            shell.PeopleToggle.ButtonPressed = false; shell.PeopleToggle.GrabFocus(); await Frames();
            Require(shell.VisitButtons.Values.All(b => !b.IsVisibleInTree()), "The linked section still closes with all its controls.");
            await Capture("people-closed");
            GD.Print("PEOPLE_SECTION_SMOKE_PASS: all contacts inside section, refresh-safe hiding, typed missed-meeting link, pure navigation and visible meeting action.");
            GetTree().Quit();

            static bool Within(Node node, Node ancestor)
            {
                for (Node? parent = node.GetParent(); parent is not null; parent = parent.GetParent()) if (parent == ancestor) return true;
                return false;
            }
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Capture(string suffix)
            {
                await Frames();
                var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--screenshots");
                if (index < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[index + 1] + "-" + suffix + ".png") == Error.Ok, "Native section screenshot saved.");
            }
        }
        catch (Exception error) { GD.PushError("PEOPLE_SECTION_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
