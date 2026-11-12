using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class GoingToPeopleSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-going-to-people-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell = scene.Instantiate<GameShell>(); shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.TutorialGameButton.Disabled, "The real tutorial is available from the normal front door.");
            shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed); DeployWithoutPrep(shell);
            Require(shell.Session.View!.Scenario.Tutorial && !shell.DeveloperToggle.ButtonPressed, "Tutorial starts without developer controls.");
            var tutorial = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var expectedTutorial = Session.Create(tutorial, 1);
            for (var day = 1; day <= 3; day++)
            {
                Step(shell); expectedTutorial.AdvanceDay();
                var notes = expectedTutorial.Records.Where(r => r.Day == day && expectedTutorial.View.RecordPerson(r)?.Role == "outgoingCco").ToArray();
                Require(notes.Length > 0 && shell.MentorMessages.Visible, "The authored mentor note has its own visible card each of the first three days.");
                foreach (var note in notes) Require(Labels(shell.MentorMessages).Any(l => l.HasMeta("mentor_record") && l.GetMeta("mentor_record").AsString() == note.Id && l.Text == note.Text), "Mentor words and record link bind exactly to Sim.");
                Require(Labels(shell.MentorMessages).Any(l => l.Text == expectedTutorial.View.OutgoingCco!.WithUnit), "The outgoing CCO is the named mentor.");
                Require(shell.Session.StateHash == expectedTutorial.StateHash(), "Mentor presentation changes no rules or replay state.");
                if (day == 1) await Screenshot(shell, "--mentor-screenshot");
            }
            Step(shell); Require(!shell.MentorMessages.Visible, "The mentor leaves after the authored third day.");
            // The taught path covers the first need; Continue must still stop at a mentor note, even without a lead.
            Scenario(shell, "tutorial"); Replay(shell, "approach1 taught");
            var startingDay = shell.Session.View!.Day;
            shell.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.View!.Day == startingDay + 1 && shell.MentorMessages.Visible, "Continue stops for mentor news rather than skipping it.");

            Scenario(shell, "spoke-b"); shell.DeveloperToggle.ButtonPressed = false; shell.PeopleToggle.ButtonPressed = true;
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var expected = Session.Create(bundle, 1);
            void Submit(string key)
            {
                var (action, preview) = shell.VisitOptions[key];
                var hash = shell.Session.StateHash; var realPreview = expected.View.PreviewVisit(action);
                Require(preview.Accepted == realPreview.Accepted && preview.ShopHoursMin == realPreview.ShopHoursMin && preview.EarliestStartMin == realPreview.EarliestStartMin && preview.EarliestStartMax == realPreview.EarliestStartMax && preview.QueueAheadHours == realPreview.QueueAheadHours && preview.HardStops.SequenceEqual(realPreview.HardStops), "Visit cost, timing and stops come unchanged from Sim.");
                Require(shell.Session.StateHash == hash && expected.Apply(action).Accepted && !shell.VisitButtons[key].Disabled, "Preview is pure and action fixture is accepted.");
                shell.VisitButtons[key].EmitSignal(Button.SignalName.Pressed);
                Require(shell.Session.StateHash == expected.StateHash(), "Contact control sends the exact previewed action.");
            }
            void Day() { Step(shell); expected.AdvanceDay(); Require(shell.Session.StateHash == expected.StateHash(), "Visits progress through the real day loop."); }
            Pick(shell.PersonContactInput, "finance"); await Screenshot(shell, "--people-screenshot"); Submit("finance");
            Require(shell.VisitButtons["finance"].Disabled, "A pending finance visit cannot be duplicated.");
            while (expected.View.FinanceFoundDay is null) Day();
            Require(shell.VisitButtons["finance"].Disabled && Labels(shell.VisitResults).Any(l => l.Text.Contains(expected.Records.Single(r => r.Kind == RecordKind.Visit).Text)), "Finance completes with exact returned facts and stays found.");
            var personItem = shell.Session.View!.MorningBoard().Items.Single(i => i.LinkKind == BoardLinkKind.Person);
            var beforeNavigation = shell.Session.StateHash; shell.PeopleToggle.ButtonPressed = false; shell.NavigateBoardItem(personItem);
            Require(shell.PeopleToggle.ButtonPressed && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.TodayTab && shell.Session.StateHash == beforeNavigation, "Person-linked news opens the board contact without spending time.");
            Pick(shell.PersonContactInput, "meeting"); Submit("meeting");
            while (!expected.View.StaffMeeting.Attending) Day();
            var next = expected.View.StaffMeeting.NextDay!.Value;
            while (expected.Day <= next) Day();
            Require(expected.View.StaffMeeting.MeetingDays.Count >= 2, "Staff meeting recurs with the Sim's weekly cost.");
            Pick(shell.PersonContactInput, "meeting"); Submit("leave-meeting"); Require(!expected.View.StaffMeeting.Attending, "Player can stop attending.");
            Pick(shell.PersonContactInput, "sco"); Pick(shell.ScoQuestionInput!, "fuel"); Submit("sco");
            Require(shell.VisitButtons["sco"].Disabled, "Pending question is gated by its real preview.");
            var sentDay = expected.Day;
            while (expected.View.ScoQuestions.Single(q => q.Id == "fuel").Pending) Day();
            var answer = expected.Records.Last(r => r.Kind == RecordKind.Visit && expected.View.RecordPerson(r)?.Id == expected.View.ScoContact!.Id);
            Require(answer.Day > sentDay && Labels(shell.VisitResults).Any(l => l.Text.Contains(answer.Text)), "SCO answer returns after its actual lag and is displayed verbatim.");
            Require(!answer.Text.Contains(expected.Rules.GoingToPeople.AskSco.Questions.Single(q => q.Id == "fuel").TutorialDirection!), "A scenario answer contains facts without tutorial directions.");
            var vendorPerson = expected.Scenario.People.First(p => p.Role == "vendorRep" && p.Vendor is not null);
            Pick(shell.PersonContactInput, "vendor:" + vendorPerson.Id); Submit("vendor");
            while (expected.View.VendorMetDay(vendorPerson.Vendor!) is null) Day();
            Require(shell.VisitButtons["vendor"].Disabled && shell.Session.View!.VendorCards().Single(c => c.Id == vendorPerson.Vendor).KnownFrom is not null, "Meeting fills in its vendor card and prevents duplicate meetings.");
            Require(!shell.MentorMessages.Visible, "Scenario play never acquires tutorial mentor directions.");

            Scenario(shell, "spoke-b"); expected = Session.Create(bundle, 1); shell.PeopleToggle.ButtonPressed = true;
            Pick(shell.PersonContactInput, "j4"); Pick(shell.J4SiteInput!, "spokeBSite");
            var quantityControl = shell.J4QuantityInput!;
            shell.J4QuantityInput!.Value = 1000000;
            Require(ReferenceEquals(quantityControl, shell.J4QuantityInput), "Typing a quantity preserves the focused input while the preview updates.");
            Require(!shell.VisitOptions["j4"].Preview.Accepted && shell.VisitButtons["j4"].Disabled, "Partner stock ceiling comes from Sim and blocks the request.");
            shell.J4QuantityInput!.Value = 1300;
            var j4 = (EngageJ4Action)shell.VisitOptions["j4"].Action; Submit("j4");
            Require(((EngageJ4Action)shell.VisitOptions["j4"].Action).Id != j4.Id, "A second request cannot reuse the pending internal id.");
            while (!expected.Arrangements.Any(a => a.Id == j4.Id)) Day();
            Require(shell.Session.View!.Arrangements.Any(a => a.Id == j4.Id && a.SiteId == "spokeBSite" && a.Instrument == "acsa"), "The J4 request creates the real ACSA support, not a local award.");
            // Continue stops on returned finance news even in an otherwise quiet game.
            Scenario(shell, "tutorial"); Replay(shell, "approach1 taught"); Step(shell, 5);
            Pick(shell.PersonContactInput, "finance"); var prior = shell.Session.View!.Day;
            shell.VisitButtons["finance"].EmitSignal(Button.SignalName.Pressed);
            shell.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.View!.Day == prior + 1 && shell.Session.View.MorningBoard().Items.Any(i => shell.Session.View.Records.Any(r => r.Id == i.RecordId && r.Kind == RecordKind.Visit)), "Continue stops on returned visit facts.");
            while (!shell.Session.View.Finished) Step(shell);
            Require(shell.VisitButtons.Values.All(b => b.Disabled), "Finished games gate every visit submission.");
            shell.QueueFree(); shell = null;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("GOING_TO_PEOPLE_SMOKE_PASS: cold tutorial, exact mentor days/words, finance, recurring meeting, SCO lag, vendor confidence, J4 stock and support, pure previews, person navigation and Continue stops."); GetTree().Quit();
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError($"GOING_TO_PEOPLE_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }

    private static IEnumerable<Label> Labels(Node node) => node.GetChildren().SelectMany(child => (child is Label label ? new[] { label } : Enumerable.Empty<Label>()).Concat(Labels(child)));

    private async Task Screenshot(GameShell shell, string flag)
    {
        var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, flag); if (at < 0) return;
        shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab; shell.DeveloperToggle.ButtonPressed = false;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (flag == "--people-screenshot")
        {
            var board = shell.GetNode<TabContainer>("%DeskTabs").GetNode<ScrollContainer>("Today/TodayPane/Body/Body");
            board.EnsureControlVisible(shell.VisitButtons["finance"].GetParent().GetChildren().OfType<Label>().Last());
        }
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Require(GetViewport().GetTexture().GetImage().SavePng(args[at + 1]) == Error.Ok, "Screenshot saved.");
    }
}
