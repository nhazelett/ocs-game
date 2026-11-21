namespace LinesOfSupport.Game.Tests;

// Keep every original replay check while giving each ten-day range its own CI budget.
public partial class MapReplaySmokeTest : MapSmokeTest
{
    protected override bool LaterDays => true;
}
