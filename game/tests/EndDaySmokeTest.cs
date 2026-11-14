using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class EndDaySmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            shell.DeveloperToggle.ButtonPressed = false;
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.TodayTab;
            Require(!shell.GetNode<Label>("%BuildStatus").Visible, "Developer diagnostics stay off the player desk.");
            var end = shell.GetNode<Button>("%NextDay");
            var skip = shell.GetNode<Button>("%Continue");
            Require(end.Text == "End the day", "The day control says what it does.");
            Require(skip.Text == "Skip quiet days" && !skip.IsVisibleInTree()
                && skip.GetParent() != end.GetParent(), "Quiet-day skipping is absent from the desk strip.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                for (var i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Screenshot saved.");
            }
            IEnumerable<Label> Labels(Node node) => node.GetChildren().SelectMany(child =>
                (child is Label label ? new[] { label } : Enumerable.Empty<Label>()).Concat(Labels(child)));
            string AllText() => string.Join("\n",Labels(shell.DayPlanContents).Select(l=>l.Text));
            IEnumerable<string> PlanOpen() => Labels(shell.DayPlanContents).Where(l=>l.HasMeta("plan_open")).Select(l=>l.Text);
            void CheckPlan(LinesOfSupport.Sim.Views.DayPlanView plan)
            {
                var rows=shell.DayPlanContents.GetChildren().OfType<HBoxContainer>().ToArray();
                Require(rows.Select(r=>r.GetMeta("plan_person").AsString()).SequenceEqual(plan.People.Select(p=>p.MemberId)),
                    "The whole team is shown in the exact Sim order.");
                foreach(var person in plan.People)
                {
                    var row=rows.Single(r=>r.GetMeta("plan_person").AsString()==person.MemberId);
                    Require(Labels(row).Select(l=>l.Text).SequenceEqual(new[]{person.Name,person.Sentence}.Concat(person.Work.Select(w=>w.Sentence))),
                        "Every person's forecast and their tasks keep Sim wording and queue order.");
                    Require(row.GetChildren().OfType<PersonPortrait>().Single().PortraitKey==(person.Portrait??"fallback"),
                        "The forecast uses the teammate's declared face.");
                }
                Require(Labels(shell.DayPlanContents).Where(l=>l.HasMeta("plan_due")).Select(l=>l.Text).SequenceEqual(plan.Due)
                    && PlanOpen().SequenceEqual(plan.Open),"Due deliveries/starts/ends and open items bind verbatim to DayPlan.");
            }
            await Capture("desk");
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            expected.AdvanceDay();
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.EndDayDialog.Visible && shell.Session.CompletedDay == 0, "Even a quiet day opens its plan before it runs.");
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.CompletedDay == 1 && shell.Session.StateHash == expected.StateHash(), "End the day advances exactly one real day.");
            var hash = shell.Session.StateHash;
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash, "Playback prevents a second hidden advance.");
            shell.FinishDayPlayback(); shell.CloseDaySummary();
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            Require(skip.IsVisibleInTree() && !skip.Disabled, "The explicit skip command is available from Menu during a deployment.");
            Require(skip.Alignment == shell.NewGameButton.Alignment
                && shell.ContinueGameButton.Text == "Continue saved game", "Menu commands align consistently and resuming names the saved game.");
            await Capture("menu");
            Require(skip.GetGlobalRect().End.Y <= 800, "The Menu shortcut stays on screen at 1280 by 800.");
            skip.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.AtFrontDoor && shell.Session.CompletedDay > 1, "Menu skipping returns to the live deployment.");
            shell.FinishDayPlayback(); shell.CloseDaySummary();

            // The tutorial's idle teammates and due water request are the cold-start check.
            Scenario(shell, "tutorial");
            var tutorial = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial")), 1);
            hash = shell.Session.StateHash;
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.EndDayDialog.Visible && PlanOpen().SequenceEqual(tutorial.View.DayPlan().Open)
                && shell.Session.StateHash == hash && shell.Session.CompletedDay == 0 && !shell.PlayingDay,
                "The exact Sim sentences appear before any state change, autosave or playback.");
            Require(string.Join(" ",PlanOpen()).Contains("Water for Hub") && AllText().Contains("MSgt Joan Avery")
                && AllText().Contains("SrA Eli Novak"), "The check names the due request and both free teammates.");
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash, "Repeated clicks cannot advance behind the check.");
            await Capture("check");
            CheckPlan(tutorial.View.DayPlan());
            Require(shell.EndDayDialog.Size.Y <= 800 && shell.EndDayDialog.Size.X <= 1280,
                "The plan stays inside the release viewport.");
            Require(shell.EndDayDialog.GetCancelButton().HasFocus(), "Returning to the desk is the focused choice.");
            Require(shell.DayPlanScroll.GetChildCount() == 1, "The plan uses one scrolling body.");
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            Require(!shell.EndDayDialog.Visible && shell.Session.StateHash == hash, "Cancel closes the check without ending the day.");
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.StateHash == hash, "A canceled confirmation cannot act later.");
            end.EmitSignal(Button.SignalName.Pressed);
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed); tutorial.AdvanceDay();
            Require(shell.Session.StateHash == tutorial.StateHash() && shell.Session.CompletedDay == 1 && shell.PlayingDay
                && !shell.EndDayDialog.Visible, "Confirm advances exactly one independently simulated day and starts its playback.");
            hash = shell.Session.StateHash;
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(shell.Session.StateHash == hash, "A duplicate confirmation cannot advance again.");
            shell.FinishDayPlayback(); shell.CloseDaySummary();

            end.EmitSignal(Button.SignalName.Pressed);
            Pick(shell.PersonContactInput, "finance"); shell.VisitButtons["finance"].EmitSignal(Button.SignalName.Pressed);
            hash = shell.Session.StateHash;
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(!shell.EndDayDialog.Visible && shell.Session.StateHash == hash && shell.Session.CompletedDay == 1,
                "Changing the day's work invalidates its old check.");
            end.EmitSignal(Button.SignalName.Pressed);
            Scenario(shell, "spoke-b"); hash = shell.Session.StateHash;
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(!shell.EndDayDialog.Visible && shell.Session.StateHash == hash && shell.Session.CompletedDay == 0,
                "Starting another deployment invalidates the old confirmation.");
            Step(shell, 5); hash = shell.Session.StateHash;
            end.EmitSignal(Button.SignalName.Pressed);
            Require(shell.EndDayDialog.Visible && PlanOpen().SequenceEqual(shell.Session.View!.DayPlan().Open)
                && string.Join(" ",PlanOpen()).Contains("was wanted from day 5"), "An overdue request retains the exact late wording.");
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require(!shell.EndDayDialog.Visible && shell.AtFrontDoor && shell.Session.StateHash == hash,
                "Leaving the desk invalidates the check without advancing.");
            DeveloperWorkspace(shell); Scenario(shell,"tutorial");
            shell.DeveloperToggle.ButtonPressed=false;
            while (shell.Session.View!.Day<9) Step(shell);
            Require(shell.SendAction(new AwardAction { Day=9, Id="plan-order", Instrument="purchaseOrder", Vendor="sanitation",
                Card="tutWaste", Quantity=3600, StartDay=13, DurationDays=8, FundingLine="om1" }).Accepted,
                "A real order supplies a carry-over fixture.");
            Step(shell);
            Require(shell.SendAction(new MeetVendorAction { Day=10, Vendor="sanitation", Assignee="avery" }).Accepted,
                "A free teammate has a small job that should finish today.");
            hash=shell.Session.StateHash;
            end.EmitSignal(Button.SignalName.Pressed);
            var loadedPlan=shell.Session.View!.DayPlan();
            Require(loadedPlan.People.SelectMany(p=>p.Work).Any(w=>w.FinishesToday)
                && loadedPlan.People.SelectMany(p=>p.Work).Any(w=>!w.FinishesToday),
                "The plan includes actual finishes and carry-overs together.");
            CheckPlan(loadedPlan);
            Require(shell.Session.StateHash==hash, "Reading every planned person and task is pure.");
            await Capture("plan-work");
            shell.DayPlanScroll.ScrollVertical=(int)shell.DayPlanScroll.GetVScrollBar().MaxValue;
            await Capture("plan-due");
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            Require(shell.Session.StateHash==hash, "Backing out of a loaded plan does not run it.");
            DeveloperWorkspace(shell); Scenario(shell,"spoke-b"); Replay(shell);
            shell.DeveloperToggle.ButtonPressed=false;
            while (shell.Session.View!.DayPlan().Due.Count==0 && shell.Session.View.Day<20) Step(shell);
            var deliveryPlan=shell.Session.View!.DayPlan();
            Require(deliveryPlan.Due.Count>0, "An authored replay has actual support or deliveries due today.");
            hash=shell.Session.StateHash;
            end.EmitSignal(Button.SignalName.Pressed); CheckPlan(deliveryPlan);
            shell.DayPlanScroll.ScrollVertical=(int)shell.DayPlanScroll.GetVScrollBar().MaxValue;
            await Capture("plan-deliveries");
            Require(shell.Session.StateHash==hash, "Reading delivery forecasts also preserves exact state.");
            shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("END_DAY_SMOKE_PASS: exact Sim check, named free teammates, overdue request, cancel-first focus, stale/duplicate guards, one-day hash, playback and Menu skip.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("END_DAY_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
