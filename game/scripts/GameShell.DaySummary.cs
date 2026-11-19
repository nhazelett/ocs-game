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
    private IReadOnlyList<DaySummaryView> skippedDaySummaries = [];
    internal IReadOnlyList<DaySummaryView> DisplayedDaySummaries { get; private set; } = [];
    private Label daySummaryTitle = null!;
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
        daySummaryTitle = DeskUi.Heading("What happened today"); page.AddChild(daySummaryTitle);
        DaySummaryScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        page.AddChild(DaySummaryScroll);
        DeskUi.ShowScrollTrack(DaySummaryScroll);
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
        YesterdayButton.Text = skippedDaySummaries.Count > 1 ? "Last results" : "Yesterday";
    }

    private void ShowDaySummary()
    {
        if (session.CompletedSummary is not { } summary || ReadingDaySummary || PlayingDay || AtFrontDoor
            || ReadingTurnover || EndDayDialog.Visible) return;
        DisplayedDaySummary = summary;
        DeskUi.Clear(DaySummaryContents);
        DisplayedDaySummaries = skippedDaySummaries.Count > 0 ? skippedDaySummaries : [summary];
        daySummaryTitle.Text = DisplayedDaySummaries.Count > 1
            ? $"What happened over days {DisplayedDaySummaries[0].Day}–{DisplayedDaySummaries[^1].Day}" : "What happened today";
        foreach (var day in DisplayedDaySummaries)
        {
            var heading = DeskUi.Heading(day.Sentence); heading.SetMeta("summary_day", day.Day); DaySummaryContents.AddChild(heading);
            News("What people told you", day.Said, "said");
            News("The day's news", day.News, "news");
            People("Work finished", day.Finished, "finished");
            Lines("Support started", day.Started, "started");
            Lines("Support ended", day.Ended, "ended");
            Lines("Short or missed", day.Short, "short");
            People("Slipped against the plan", day.Slipped, "slipped");
            People("Carried into tomorrow", day.CarriedOver, "carried");
        }
        DaySummaryScroll.ScrollVertical = 0;
        DaySummaryDialog.GetOkButton().Text = session.View?.Finished == true ? "Back to the debrief" : $"On to the morning of day {session.View!.Day}";
        var viewport = GetViewportRect().Size;
        DaySummaryDialog.PopupCentered(new Vector2I((int)Math.Min(960, viewport.X - 32), (int)Math.Min(650, viewport.Y - 32)));
        RefreshDayAdvanceControls();
        DaySummaryDialog.GetOkButton().GrabFocus();

        void News(string title, IReadOnlyList<NewItem> items, string kind)
        {
            if (items.Count == 0) return;
            var heading = DeskUi.Heading(title); heading.SetMeta("summary_group", kind); DaySummaryContents.AddChild(heading);
            foreach (var item in items) AddNewsItem(DaySummaryContents, item, "summary");
        }
        void Lines(string title, IReadOnlyList<string> lines, string kind)
        {
            if (lines.Count == 0) return;
            var heading = DeskUi.Heading(title); heading.SetMeta("summary_group", kind);
            DaySummaryContents.AddChild(heading);
            foreach (var sentence in lines) AddLine(DaySummaryContents, sentence, kind);
        }
        void People(string title, IReadOnlyList<DaySummaryLine> lines, string kind)
        {
            if (lines.Count == 0) return;
            var heading = DeskUi.Heading(title); heading.SetMeta("summary_group", kind);
            DaySummaryContents.AddChild(heading);
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
        DaySummaryDialog?.Hide(); DisplayedDaySummary = null; DisplayedDaySummaries = [];
        if (!wasReading) return;
        RefreshDayAdvanceControls();
        if (!returnToMorning || AtFrontDoor || ReadingTurnover) return;
        if (session.View?.Finished == true) GetNode<TabContainer>("%DeskTabs").CurrentTab = DebriefTab;
        else
        {
            FocusNewMorning();
        }
    }

    private void ResetDaySummary()
    {
        CloseDaySummary(false);
        skippedDaySummaries = [];
        YesterdayButton.Visible = false;
    }

    private void RefreshDayAdvanceControls()
    {
        var blocked = PlayingDay || ReadingDaySummary || session.View is null || session.View.Finished;
        GetNode<Button>("%NextDay").Disabled = blocked;
    }
}
