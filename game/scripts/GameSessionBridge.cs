using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Game;

// Orchestrates the accepted Sim API. No rules, policy decisions or authoritative state live in Game.
internal sealed class GameSessionBridge
{
    private ScenarioBundle? bundle;
    private Session? session;
    private InputLog? log;
    internal SessionView? View => session?.View;
    internal string? StateHash => session?.StateHash();
    internal int CompletedDay => session is null ? 0 : (int)session.Metrics["day"];

    internal void Clear() { bundle = null; session = null; log = null; }

    internal void LoadScenario(string path)
    {
        Clear();
        var loaded = ScenarioBundle.Load(path);
        var created = Session.Create(loaded, 1);
        bundle = loaded;
        session = created;
    }

    internal void LoadReplay(string path)
    {
        if (bundle is null) throw new InvalidDataException("Load a scenario before choosing a replay.");
        session = null;
        log = null;
        InputLog loaded;
        try { loaded = InputLog.Load(path); }
        catch (NotSupportedException error) { throw new InvalidDataException("The replay action format is not supported.", error); }
        // Sim checks scenario/rules identity and applies the log's seed/prep, before any day is played.
        session = Replay.Run(bundle, loaded, stopAfterDay: 0);
        log = loaded;
    }

    internal bool RequestNextDay()
    {
        if (session is null || session.Finished) return false;
        if (log is not null)
            foreach (var action in log.Actions.Where(a => a.Day == session.Day)) session.Apply(action);
        session.AdvanceDay();
        return true;
    }
}
