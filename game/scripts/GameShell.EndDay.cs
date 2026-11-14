using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal ConfirmationDialog EndDayDialog { get; private set; } = null!;
    private string? endDayCheckHash;
    internal VBoxContainer DayPlanContents { get; private set; } = null!;
    internal ScrollContainer DayPlanScroll { get; private set; } = null!;

    private void BuildEndDayCheck()
    {
        EndDayDialog = new ConfirmationDialog { Title = "", Borderless = true, DialogAutowrap = true, Exclusive = true };
        AddChild(EndDayDialog);
        EndDayDialog.GetLabel().Hide();
        EndDayDialog.GetOkButton().Text = "End the day";
        EndDayDialog.GetCancelButton().Text = "Back";
        var holder = new Control { CustomMinimumSize = new Vector2(580, 320) };
        EndDayDialog.AddChild(holder);
        var page = new VBoxContainer(); holder.AddChild(page);
        page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        page.AddThemeConstantOverride("separation", 12);
        page.AddChild(DeskUi.Heading("The day's plan"));
        page.AddChild(DeskUi.Label("This is a forecast. Work should finish as shown; events and deliveries can change what happens. Go back to the desk to change assignments."));
        DayPlanScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        page.AddChild(DayPlanScroll);
        DayPlanContents = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        DayPlanContents.AddThemeConstantOverride("separation", 16); DayPlanScroll.AddChild(DayPlanContents);
        EndDayDialog.Canceled += CancelEndDayCheck;
        EndDayDialog.CloseRequested += CancelEndDayCheck;
        EndDayDialog.Confirmed += () =>
        {
            var checkedHash = endDayCheckHash;
            CancelEndDayCheck();
            if (checkedHash is null || checkedHash != session.StateHash || ReadingTurnover || PlayingDay
                || AtFrontDoor || session.View is not { Finished: false }) return;
            AdvanceDayFromDesk();
        };
    }

    private void ShowDayPlan(SessionView view)
    {
        var plan = view.DayPlan();
        endDayCheckHash = session.StateHash;
        DeskUi.Clear(DayPlanContents);
        DayPlanContents.AddChild(DeskUi.Heading($"Day {plan.Day}"));
        foreach (var person in plan.People)
        {
            var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 14); DayPlanContents.AddChild(row);
            row.SetMeta("plan_person", person.MemberId);
            var face = new PersonPortrait { SizeFlagsVertical = SizeFlags.ShrinkBegin };
            face.SetPerson(TeamPerson(person.MemberId, person.Name, "", person.Portrait)); row.AddChild(face);
            face.CustomMinimumSize = new Vector2(48, 60);
            var words = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddChild(words);
            words.AddChild(DeskUi.Heading(person.Name)); words.AddChild(DeskUi.Label(person.Sentence));
            foreach (var work in person.Work)
            {
                var line = DeskUi.Label(work.Sentence); line.SetMeta("plan_work", person.MemberId);
                words.AddChild(line);
            }
        }
        Lines("Deliveries and support due today", plan.Due, "plan_due", "No deliveries or support starts or ends are due today.");
        Lines("Still open", plan.Open, "plan_open", "Nothing needs your attention before ending the day.");
        DayPlanScroll.ScrollVertical = 0;
        var viewport = GetViewportRect().Size;
        EndDayDialog.PopupCentered(new Vector2I((int)Math.Min(960, viewport.X - 32), (int)Math.Min(650, viewport.Y - 32)));
        EndDayDialog.GetCancelButton().GrabFocus();

        void Lines(string heading, IReadOnlyList<string> sentences, string key, string empty)
        {
            DayPlanContents.AddChild(DeskUi.Heading(heading));
            if (sentences.Count == 0) DayPlanContents.AddChild(DeskUi.Label(empty));
            foreach (var sentence in sentences)
            {
                var label = DeskUi.Label(sentence); label.SetMeta(key, true); DayPlanContents.AddChild(label);
            }
        }
    }

    private void CancelEndDayCheck()
    {
        endDayCheckHash = null;
        EndDayDialog?.Hide();
    }
}
