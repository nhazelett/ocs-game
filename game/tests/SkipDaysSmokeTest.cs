using Godot;

namespace LinesOfSupport.Game.Tests;

public partial class SkipDaysSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            await SkipDaysSmokeChecks.Run(this);
            GD.Print("SKIP_DAYS_SMOKE_PASS: real quiet/busy plans, exact Sim stop sentence, support/news stops, seven-day cap, complete grouped results, autosave and stale-control guards.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("SKIP_DAYS_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
}
