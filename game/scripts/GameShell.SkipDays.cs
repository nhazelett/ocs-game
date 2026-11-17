using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal Button SkipAheadButton { get; private set; } = null!;
    internal Label SkipAheadReason { get; private set; } = null!;
    private VBoxContainer skipAheadChoice = null!;

    private void BuildSkipAhead(VBoxContainer page)
    {
        // Keep wrapped stop text inside the bounded page; native dialog minima measure it before wrapping.
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 16); page.AddChild(row);
        var back = EndDayDialog.GetCancelButton(); back.Reparent(row); back.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var end = EndDayDialog.GetOkButton(); end.Reparent(row); end.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        skipAheadChoice = new VBoxContainer { Visible = false, CustomMinimumSize = new Vector2(380, 0) };
        row.AddChild(skipAheadChoice);
        SkipAheadButton = DeskUi.Button("Skip ahead", SkipAheadFromPlan); skipAheadChoice.AddChild(SkipAheadButton);
        SkipAheadReason = DeskUi.Label(); skipAheadChoice.AddChild(SkipAheadReason);
    }

    private void RefreshSkipAhead(SessionView view)
    {
        var stop = view.QuietDay() ? view.SkipStops() : null;
        skipAheadChoice.Visible = stop is not null;
        SkipAheadReason.Text = stop?.Sentence ?? "";
    }

    private void SkipAheadFromPlan()
    {
        if (!EndDayDialog.Visible || endDayCheckHash is null || endDayCheckHash != session.StateHash
            || AtFrontDoor || ReadingTurnover || PlayingDay || ReadingDaySummary
            || session.View is not { Finished: false } view || !view.QuietDay()
            || view.SkipStops() is not { } stop) return;
        CancelEndDayCheck();
        var days = new List<DaySummaryView>();
        // The Sim owns quietness and known stops. Keep the original horizon for the whole click.
        while (days.Count < 7 && session.View is { Finished: false } current
            && current.Day < stop.UntilDay && current.QuietDay())
        {
            if (!AdvanceAndAutosave()) break;
            days.Add(session.CompletedSummary!);
            if (saveError is not null) break;
        }
        if (days.Count == 0) return;
        skippedDaySummaries = days;
        FocusNewMorning();
        RefreshDisplay($"Results for {DeskUi.DaySpan(days[0].Day, days[^1].Day)}.", developerOnly: true);
        Map.SetMovementProgress(1);
        ShowDaySummary();
    }
}
