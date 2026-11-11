using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal ConfirmationDialog EndDayDialog { get; private set; } = null!;
    private string? endDayCheckHash;

    private void BuildEndDayCheck()
    {
        EndDayDialog = new ConfirmationDialog { Title = "", Borderless = true, DialogAutowrap = true, Exclusive = true };
        AddChild(EndDayDialog);
        EndDayDialog.GetLabel().AutowrapMode = TextServer.AutowrapMode.WordSmart;
        EndDayDialog.GetLabel().CustomMinimumSize = new Vector2(600, 0);
        EndDayDialog.GetOkButton().Text = "End the day anyway";
        EndDayDialog.GetCancelButton().Text = "Back to the desk";
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

    private void CancelEndDayCheck()
    {
        endDayCheckHash = null;
        EndDayDialog?.Hide();
    }
}
