using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal AcceptDialog DaySummaryDialog { get; private set; } = null!;
    internal VBoxContainer DaySummaryContents { get; private set; } = null!;
    internal ScrollContainer DaySummaryScroll { get; private set; } = null!;
    internal Button YesterdayButton { get; private set; } = null!;
    internal DaySummaryView? DisplayedDaySummary { get; private set; }
    internal bool ReadingDaySummary => DaySummaryDialog?.Visible == true;

    private void BuildDaySummary()
    {
        DaySummaryDialog = new AcceptDialog { Title = "", Borderless = true, Exclusive = true };
        AddChild(DaySummaryDialog);
        DaySummaryDialog.GetLabel().Hide();
        DaySummaryDialog.GetOkButton().Text = "Back to Today";
        var holder = new Control { CustomMinimumSize = new Vector2(580, 320) };
        DaySummaryDialog.AddChild(holder);
        var page = new VBoxContainer(); holder.AddChild(page);
        page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        page.AddThemeConstantOverride("separation", 12);
        page.AddChild(DeskUi.Heading("What happened today"));
        DaySummaryScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        page.AddChild(DaySummaryScroll);
        DaySummaryContents = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        DaySummaryContents.AddThemeConstantOverride("separation", 16);
        DaySummaryScroll.AddChild(DaySummaryContents);
        DaySummaryDialog.Confirmed += () => CloseDaySummary();
        DaySummaryDialog.Canceled += () => CloseDaySummary();
        DaySummaryDialog.CloseRequested += () => CloseDaySummary();
    }

    private void BuildYesterday(VBoxContainer board)
    {
        YesterdayButton = DeskUi.Button("Yesterday", ShowDaySummary);
        YesterdayButton.TooltipText = "Read the results of the last day played.";
        YesterdayButton.Visible = false;
        board.AddChild(YesterdayButton);
    }

    private void RefreshYesterday()
    {
        YesterdayButton.Visible = session.CompletedSummary is not null;
    }

    private void ShowDaySummary()
    {
        if (session.CompletedSummary is not { } summary || ReadingDaySummary || PlayingDay || AtFrontDoor
            || ReadingTurnover || EndDayDialog.Visible) return;
        DisplayedDaySummary = summary;
        DeskUi.Clear(DaySummaryContents);
        DaySummaryContents.AddChild(DeskUi.Heading(summary.Sentence));
        People("Work finished", summary.Finished, "finished", "No team jobs finished today.");
        Lines("Support started", summary.Started, "started", "No support started today.");
        Lines("Support ended", summary.Ended, "ended", "No support ended today.");
        Lines("Short or missed", summary.Short, "short", "Everything was supplied in full today.");
        People("Slipped against the plan", summary.Slipped, "slipped", "Nothing slipped against the morning's plan.");
        People("Carried into tomorrow", summary.CarriedOver, "carried", "No work in progress carried over today.");
        DaySummaryScroll.ScrollVertical = 0;
        DaySummaryDialog.GetOkButton().Text = session.View?.Finished == true ? "Back to the debrief" : "Back to Today";
        var viewport = GetViewportRect().Size;
        DaySummaryDialog.PopupCentered(new Vector2I((int)Math.Min(960, viewport.X - 32), (int)Math.Min(650, viewport.Y - 32)));
        RefreshDayAdvanceControls();
        DaySummaryDialog.GetOkButton().GrabFocus();

        void Lines(string title, IReadOnlyList<string> lines, string kind, string empty)
        {
            DaySummaryContents.AddChild(DeskUi.Heading(title));
            if (lines.Count == 0) DaySummaryContents.AddChild(DeskUi.Label(empty));
            foreach (var sentence in lines) AddLine(DaySummaryContents, sentence, kind);
        }
        void People(string title, IReadOnlyList<DaySummaryLine> lines, string kind, string empty)
        {
            DaySummaryContents.AddChild(DeskUi.Heading(title));
            if (lines.Count == 0) DaySummaryContents.AddChild(DeskUi.Label(empty));
            foreach (var line in lines)
            {
                var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 14);
                DaySummaryContents.AddChild(row);
                if (session.View?.Team.FirstOrDefault(p => p.Id == line.TeammateId) is { } member)
                {
                    var face = new PersonPortrait { CustomMinimumSize = new Vector2(40, 50),
                        SizeFlagsVertical = SizeFlags.ShrinkBegin };
                    face.SetPerson(TeamPerson(member.Id, TeamName(member), member.Rank, member.Portrait));
                    row.AddChild(face);
                }
                AddLine(row, line.Sentence, kind);
            }
        }
        static void AddLine(Container parent, string sentence, string kind)
        {
            var label = DeskUi.Label(sentence); label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            label.SetMeta("summary_kind", kind); parent.AddChild(label);
        }
    }

    internal void CloseDaySummary(bool returnToMorning = true)
    {
        var wasReading = DisplayedDaySummary is not null;
        DaySummaryDialog?.Hide(); DisplayedDaySummary = null;
        if (!wasReading) return;
        RefreshDayAdvanceControls();
        if (!returnToMorning || AtFrontDoor || ReadingTurnover) return;
        if (session.View?.Finished == true) GetNode<TabContainer>("%DeskTabs").CurrentTab = DebriefTab;
        else
        {
            FocusNewMorning();
            if (YesterdayButton.IsVisibleInTree()) YesterdayButton.GrabFocus();
        }
    }

    private void ResetDaySummary()
    {
        CloseDaySummary(false);
        YesterdayButton.Visible = false;
    }

    private void RefreshDayAdvanceControls()
    {
        var blocked = PlayingDay || ReadingDaySummary || session.View is null || session.View.Finished;
        GetNode<Button>("%NextDay").Disabled = blocked;
        GetNode<Button>("%Continue").Disabled = blocked;
    }
}
