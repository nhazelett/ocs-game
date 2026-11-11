using Godot;

namespace LinesOfSupport.Game;

// Presentation only. Uses the approved files; never sends a Sim command or generates audio.
public partial class GameMusic : Node
{
    private readonly Dictionary<string, AudioStreamOggVorbis> streams = [];
    private readonly List<string> deskTracks = [];
    private readonly List<string> history = [];
    private AudioStreamPlayer music = null!;
    private AudioStreamPlayer[] harbor = [];
    private string settingsPath = "";
    private Func<bool> cuePlaying = () => false;
    private bool titleWanted = true, titlePlaying = true, titleDone, mapVisible;
    private int nextDesk, harborCurrent;
    private double trackAge, harborCrossfade;
    private float surfaceFade, harborGain;
    private bool crossfading;
    internal float Volume { get; private set; } = .35f;
    internal bool Muted { get; private set; }
    internal float DuckFactor { get; private set; } = 1;
    internal double QuietGap { get; private set; }
    internal string? CurrentTrack { get; private set; }
    internal string? SaveError { get; private set; }
    internal int LoadedStreamCount => streams.Count;
    internal AudioStreamPlayer MusicPlayer => music;
    internal IReadOnlyList<AudioStreamPlayer> HarborPlayers => harbor;
    internal IReadOnlyList<string> TrackHistory => history;
    internal bool HarborCrossfading => crossfading;
    internal double HarborLength => streams.TryGetValue("harbor-ambience", out var stream) ? stream.GetLength() : 0;
    internal bool UsesNativeTrackLoops => streams.Values.Any(s => s.Loop);

    internal void Initialize(string path, string audioRoot, Func<bool> cues)
    {
        settingsPath = path; cuePlaying = cues;
        using var config = new ConfigFile();
        if (config.Load(path) == Error.Ok)
        {
            var stored = config.GetValue("music", "volume", .35f);
            var value = stored.VariantType is Variant.Type.Float or Variant.Type.Int ? stored.AsDouble() : .35;
            Volume = double.IsFinite(value) ? (float)Math.Clamp(value, 0, 1) : .35f;
            var muted = config.GetValue("music", "muted", false);
            Muted = muted.VariantType == Variant.Type.Bool && muted.AsBool();
        }
        foreach (var name in new[] { "title", "desk-morning", "desk-long-day", "harbor-ambience" })
        {
            var file = Path.Combine(audioRoot, name + ".ogg");
            if (!File.Exists(file)) continue;
            var stream = AudioStreamOggVorbis.LoadFromFile(file);
            if (stream is null) continue;
            stream.Loop = false; streams.Add(name, stream);
            if (name.StartsWith("desk-", StringComparison.Ordinal)) deskTracks.Add(name);
        }
        music = new AudioStreamPlayer { Name = "Music", VolumeLinear = 0 }; AddChild(music);
        harbor = [new() { Name = "HarborA", VolumeLinear = 0 }, new() { Name = "HarborB", VolumeLinear = 0 }];
        foreach (var player in harbor)
        {
            if (streams.TryGetValue("harbor-ambience", out var ambient)) player.Stream = ambient;
            AddChild(player);
        }
        music.Finished += TrackFinished;
        if (!Muted && Volume > 0) StartTitle();
    }

    internal void SetContext(bool atTitle, bool seesMap)
    {
        titleWanted = atTitle; mapVisible = seesMap;
    }

    internal void ChangeSettings(float volume, bool muted)
    {
        Volume = float.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : .35f; Muted = muted;
        if (Muted || Volume == 0) StopPlayers();
        using var config = new ConfigFile();
        config.SetValue("music", "volume", Volume); config.SetValue("music", "muted", Muted);
        SaveError = config.Save(settingsPath) == Error.Ok ? null : "Your music setting works for this visit, but could not be saved.";
    }

    private void StartTrack(string name)
    {
        music.Stop(); music.Stream = streams[name]; music.VolumeLinear = 0;
        music.Play(); CurrentTrack = name; history.Add(name); trackAge = 0;
    }

    private void StartTitle()
    {
        if (streams.ContainsKey("title")) StartTrack("title");
        else titleDone = true;
    }

    private void TrackFinished()
    {
        music.Stop(); CurrentTrack = null;
        if (titlePlaying) titleDone = true;
        else
        {
            QuietGap = 3;
            if (deskTracks.Count > 0) nextDesk = (nextDesk + 1) % deskTracks.Count;
        }
    }

    public override void _Process(double delta) => Tick(delta);

    internal void Tick(double delta)
    {
        if (music is null) return;
        var step = (float)Math.Max(0, delta);
        if (Muted || Volume == 0) { StopPlayers(); return; }
        DuckFactor = Mathf.MoveToward(DuckFactor, cuePlaying() ? .22f : 1, step * (cuePlaying() ? 8 : 1.5f));
        surfaceFade = Mathf.MoveToward(surfaceFade, titleWanted == titlePlaying ? 1 : 0, step * 3);
        if (titleWanted != titlePlaying && surfaceFade <= .001f)
        {
            music.Stop(); CurrentTrack = null; titlePlaying = titleWanted;
            titleDone = false; QuietGap = 0;
        }
        if (!music.Playing && titleWanted == titlePlaying)
        {
            if (titlePlaying && !titleDone) StartTitle();
            else if (!titlePlaying && deskTracks.Count > 0)
            {
                QuietGap = Math.Max(0, QuietGap - delta);
                if (QuietGap <= 0) StartTrack(deskTracks[nextDesk]);
            }
        }
        trackAge += delta;
        var trackFade = Math.Clamp((float)(trackAge / 1.5), 0, 1);
        if (CurrentTrack is { } name && streams.TryGetValue(name, out var track))
            trackFade = Math.Min(trackFade, Math.Clamp((float)((track.GetLength() - music.GetPlaybackPosition()) / 1.5), 0, 1));
        music.VolumeLinear = Volume * .25f * DuckFactor * surfaceFade * trackFade;
        TickHarbor(step);
    }

    private void TickHarbor(float delta)
    {
        var wanted = !titleWanted && mapVisible && streams.ContainsKey("harbor-ambience");
        harborGain = Mathf.MoveToward(harborGain, wanted ? Volume * .18f : 0, delta * .3f);
        if (harborGain <= .0001f && !wanted)
        {
            foreach (var player in harbor) { player.Stop(); player.VolumeLinear = 0; }
            crossfading = false; harborCrossfade = 0; return;
        }
        if (!streams.TryGetValue("harbor-ambience", out var stream)) return;
        var current = harbor[harborCurrent]; var other = harbor[1 - harborCurrent];
        if (!current.Playing && !crossfading) current.Play();
        if (!crossfading && current.GetPlaybackPosition() >= stream.GetLength() - .7)
        {
            other.VolumeLinear = 0; other.Play(); crossfading = true; harborCrossfade = 0;
        }
        if (crossfading)
        {
            harborCrossfade += delta;
            var mix = Math.Clamp((float)(harborCrossfade / .7), 0, 1);
            current.VolumeLinear = harborGain * DuckFactor * Math.Max(0, Mathf.Cos(mix * Mathf.Pi / 2));
            other.VolumeLinear = harborGain * DuckFactor * Mathf.Sin(mix * Mathf.Pi / 2);
            if (mix >= 1) { current.Stop(); harborCurrent = 1 - harborCurrent; crossfading = false; harborCrossfade = 0; }
        }
        else { current.VolumeLinear = harborGain * DuckFactor; other.VolumeLinear = 0; }
    }

    private void StopPlayers()
    {
        music.Stop(); music.VolumeLinear = 0; CurrentTrack = null;
        foreach (var player in harbor) { player.Stop(); player.VolumeLinear = 0; }
        surfaceFade = harborGain = 0; crossfading = false; harborCrossfade = 0;
    }

    public override void _ExitTree()
    {
        if (music is null) return;
        StopPlayers(); music.Stream = null;
        foreach (var player in harbor) player.Stream = null;
        foreach (var stream in streams.Values) stream.Dispose();
        streams.Clear();
    }
}
