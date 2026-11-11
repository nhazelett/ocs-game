using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

// Orchestrates the accepted Sim API. No rules, policy decisions or authoritative state live in Game.
internal sealed class GameSessionBridge
{
    private ScenarioBundle? bundle;
    private Session? session;
    private InputLog? log;
    internal SessionView? View => session?.View;
    internal event Action<SessionView>? DayAdvanced;
    internal event Action<SessionView>? DeploymentStarted;
    internal event Action<PlayerAction>? ActionAccepted;
    internal bool IsReplay => log is not null;
    internal PrepView? Prep => bundle is not null && session is null ? PrepScreen.View(bundle) : null;
    internal IReadOnlyList<TeamMemberData> PrepTeam => bundle?.Scenario.Shop.Team ?? [];
    internal string? PrepScenarioId => bundle?.Scenario.Id;
    internal PrepCheck CheckPrep(IEnumerable<string> picks) => PrepScreen.Check(bundle ?? throw new InvalidOperationException("Load a scenario first."), picks);
    internal ActionResult Apply(PlayerAction action)
    {
        if (session is null) return ActionResult.Stop("Load a scenario before making a decision.");
        var result = session.Apply(action);
        // A successful player command takes control from the authored input source; the Sim state is retained.
        if (result.Accepted) { log = null; ActionAccepted?.Invoke(action); }
        return result;
    }
    internal string? StateHash => session?.StateHash();
    internal InputLog CreateSave() => session?.ToSave() ?? throw new InvalidOperationException("Deploy before saving.");

    internal void ResumeScenario(string path, InputLog save)
    {
        // Validate and replay before replacing the current game: a refused save leaves it intact.
        var loaded = ScenarioBundle.Load(path);
        var resumed = Replay.Resume(loaded, save);
        bundle = loaded;
        session = resumed;
        log = null;
    }
    internal int CompletedDay => session is null ? 0 : (int)session.Metrics["day"];

    internal void Clear() { bundle = null; session = null; log = null; }

    internal void LoadScenario(string path)
    {
        Clear();
        var loaded = ScenarioBundle.Load(path);
        bundle = loaded;
    }

    internal void StartScenario(IEnumerable<string> picks, IReadOnlyList<TeamPick>? team = null)
    {
        var chosen = picks.ToArray();
        var check = CheckPrep(chosen);
        if (!check.Accepted) throw new InvalidDataException(string.Join("; ", check.Stops));
        session = Session.Create(bundle!, 1, chosen, team);
        log = null;
        DeploymentStarted?.Invoke(session.View);
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
        DayAdvanced?.Invoke(session.View);
        return true;
    }
}
