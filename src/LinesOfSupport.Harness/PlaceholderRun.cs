namespace LinesOfSupport.Harness;

// Deliberately internal to Harness. This is not a simulation contract or a substitute for Fable's day loop.
internal static class PlaceholderRun
{
    internal static PlaceholderResult Advance(LoadedScenario scenario, int seed, string policy)
    {
        var day = 0;
        while (day < scenario.Days)
            day++;
        return new PlaceholderResult(day);
    }
}

internal sealed record PlaceholderResult(int DaysAdvanced);
