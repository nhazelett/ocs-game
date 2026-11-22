using Godot;
using LinesOfSupport.Sim;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer? awardWriters;
    internal AssigneePicker? AwardWriterPicker { get; private set; }

    private void ClearAwardWriter()
    {
        if (awardWriters is not null) DeskUi.Clear(awardWriters);
        AwardWriterPicker = null;
    }

    private void RefreshAwardWriter(SessionView view)
    {
        if (awardWriters is null) return;
        if (PendingAward is null || AwardPreview is not { ShopHoursMax: > 0 })
        { ClearAwardWriter(); return; }
        var choices = view.AssigneeChoices(PendingAward);
        if (AwardWriterPicker is { } current && current.Selected == awardAssignee && current.Choices.SequenceEqual(choices)) return;
        ClearAwardWriter();
        // The Sim owns the missing-pair gate and the date. Name the choices without promising a next-day finish.
        var unavailable = PickTwoReason(view) is not null
            ? "date: pick two of Fast/Cheap/Good"
            : "done day unavailable";
        var picker = new AssigneePicker(choices, awardAssignee!, view.Finished, "Who writes it", compact: true, unavailableDoneDay: unavailable);
        AwardWriterPicker = picker; awardWriters.AddChild(picker);
        var version = awardFormVersion; var hash = session.StateHash;
        picker.SelectionChanged = () =>
        {
            if (picker != AwardWriterPicker || version != awardFormVersion || hash != session.StateHash
                || AtFrontDoor || ReadingTurnover || PlayingDay || session.View is not { Finished: false }) return;
            awardAssignee = picker.Selected; awardFormTouched = true;
            UpdateAwardPreview();
        };
    }
}
