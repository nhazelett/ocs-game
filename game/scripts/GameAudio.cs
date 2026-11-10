using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

// Presentation only: cues subscribe to accepted commands and the completed-day projection.
public partial class GameAudio : Node
{
    internal static readonly string[] CueNames = ["new-day", "at-the-door", "support-down", "award-made"];
    private readonly Dictionary<string, AudioStreamPlayer> players = [];
    private string settingsPath = "";
    private string? lastVisitor;
    internal float Volume { get; private set; } = .5f;
    internal bool Muted { get; private set; }
    internal string? SaveError { get; private set; }
    internal int PlayedCount { get; private set; }
    internal string? LastCue { get; private set; }
    internal int LoadedCueCount => players.Count;

    internal void Initialize(string path)
    {
        settingsPath = path;
        using var config = new ConfigFile();
        if (config.Load(path) == Error.Ok)
        {
            var stored = config.GetValue("sound", "volume", .5f);
            var volume = stored.VariantType is Variant.Type.Float or Variant.Type.Int ? stored.AsDouble() : .5;
            Volume = double.IsFinite(volume) ? (float)Math.Clamp(volume, 0, 1) : .5f;
            var muted = config.GetValue("sound", "muted", false);
            Muted = muted.VariantType == Variant.Type.Bool && muted.AsBool();
        }
        foreach (var name in CueNames)
        {
            using var stream = AudioStreamWav.LoadFromFile(Path.Combine(GamePaths.AssetRoot, "audio", name + ".wav"));
            if (stream is null) { GD.PushWarning("Sound unavailable: " + name); continue; }
            var player = new AudioStreamPlayer { Stream = stream, MaxPolyphony = 1 };
            AddChild(player); players.Add(name, player);
        }
        ApplyVolume();
    }

    internal void ChangeSettings(float volume, bool muted)
    {
        Volume = float.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : .5f;
        Muted = muted; ApplyVolume();
        using var config = new ConfigFile();
        config.SetValue("sound", "volume", Volume); config.SetValue("sound", "muted", Muted);
        var error = config.Save(settingsPath);
        SaveError = error == Error.Ok ? null : "Your sound setting works for this visit, but could not be saved.";
    }

    private void ApplyVolume()
    {
        foreach (var player in players.Values)
        {
            player.VolumeLinear = Muted ? 0 : Volume;
            if (Muted || Volume == 0) player.Stop();
        }
    }

    internal void PlayCue(string cue)
    {
        if (Muted || Volume == 0 || !players.TryGetValue(cue, out var player)) return;
        player.Play(); LastCue = cue; PlayedCount++;
    }

    internal void BeginDeployment(SessionView view)
    {
        lastVisitor = null;
        AnnounceVisitor(view);
    }

    internal void CompletedDay(SessionView view)
    {
        PlayCue("new-day");
        if (view.MapDay().Changes.Any(c => c.Known && c.Kind == "shortfall")) PlayCue("support-down");
        AnnounceVisitor(view);
    }

    private void AnnounceVisitor(SessionView view)
    {
        var lead = view.MorningBoard().Lead;
        if (view.Finished || lead?.Person is null) return;
        var key = lead.RecordId ?? lead.LinkId;
        if (key == lastVisitor) return;
        lastVisitor = key; PlayCue("at-the-door");
    }

    internal void AcceptedAction(PlayerAction action)
    {
        if (action is AwardAction) PlayCue("award-made");
    }
}
