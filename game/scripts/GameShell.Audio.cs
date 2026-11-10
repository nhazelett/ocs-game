using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal string? AudioSettingsPathOverride { get; set; }
    internal string? MusicSettingsPathOverride { get; set; }
    internal string? MusicRootOverride { get; set; }
    internal GameMusic Music { get; private set; } = null!;
    internal HSlider MusicVolume { get; private set; } = null!;
    internal CheckBox MusicMute { get; private set; } = null!;
    internal GameAudio Sound { get; private set; } = null!;
    internal Button SoundButton { get; private set; } = null!;
    internal HSlider SoundVolume { get; private set; } = null!;
    internal CheckBox SoundMute { get; private set; } = null!;
    internal PanelContainer SoundPanel { get; private set; } = null!;

    private void RefreshMusicContext()
    {
        if (Music is null) return;
        var seesMap = !AtFrontDoor && session.View is not null && !ReadingTurnover
            && !ContractView.Visible && GetNode<Control>("Margin/Layout/WorkArea/BoardPanel").IsVisibleInTree();
        Music.SetContext(AtFrontDoor, seesMap);
    }

    private void BuildSoundControls(VBoxContainer menu)
    {
        Sound = new GameAudio { Name = "GameAudio" }; AddChild(Sound);
        Sound.Initialize(AudioSettingsPathOverride ?? ProjectSettings.GlobalizePath("user://sound.cfg"));
        Music = new GameMusic { Name = "GameMusic" }; AddChild(Music);
        Music.Initialize(MusicSettingsPathOverride ?? ProjectSettings.GlobalizePath("user://music.cfg"),
            MusicRootOverride ?? Path.Combine(GamePaths.AssetRoot, "audio", "music"), () => Sound.AnyCuePlaying);
        session.DayAdvanced += Sound.CompletedDay;
        session.ActionAccepted += Sound.AcceptedAction;
        session.DeploymentStarted += Sound.BeginDeployment;
        SoundButton = FrontButton("Sound", () => { SoundPanel.Show(); SoundMute.GrabFocus(); });
        menu.AddChild(SoundButton);
        SoundPanel = new PanelContainer { Name = "SoundSettings", ZIndex = 30, Visible = false };
        AddChild(SoundPanel); SoundPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center = new CenterContainer(); SoundPanel.AddChild(center);
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(480, 0) };
        column.AddThemeConstantOverride("separation", 12); center.AddChild(column);
        var title = DeskUi.Label("Sound and music"); title.AddThemeFontSizeOverride("font_size", 32); column.AddChild(title);
        column.AddChild(DeskUi.Label("Event cues stay clear while music and harbor sounds play quietly underneath."));
        SoundMute = new CheckBox { Text = "Mute sound", ButtonPressed = Sound.Muted }; column.AddChild(SoundMute);
        var volumeLabel = DeskUi.Label(); column.AddChild(volumeLabel);
        SoundVolume = new HSlider { MinValue = 0, MaxValue = 100, Step = 1, Value = Sound.Volume * 100,
            CustomMinimumSize = new Vector2(480, 40), FocusMode = FocusModeEnum.All, TooltipText = "Sound volume" };
        column.AddChild(SoundVolume);
        MusicMute = new CheckBox { Text = "Mute music and ambience", ButtonPressed = Music.Muted }; column.AddChild(MusicMute);
        var musicLabel = DeskUi.Label($"Music: {Music.Volume * 100:0}%"); column.AddChild(musicLabel);
        MusicVolume = new HSlider { MinValue = 0, MaxValue = 100, Step = 1, Value = Music.Volume * 100,
            CustomMinimumSize = new Vector2(480, 40), FocusMode = FocusModeEnum.All, TooltipText = "Music and ambience volume" };
        column.AddChild(MusicVolume);
        var saved = DeskUi.Label("Settings are remembered on this computer."); column.AddChild(saved);
        void Change()
        {
            Sound.ChangeSettings((float)SoundVolume.Value / 100, SoundMute.ButtonPressed);
            volumeLabel.Text = $"Sound: {SoundVolume.Value:0}%";
            saved.Text = Sound.SaveError ?? "Settings are remembered on this computer.";
        }
        volumeLabel.Text = $"Sound: {SoundVolume.Value:0}%";
        SoundVolume.ValueChanged += _ => Change(); SoundMute.Toggled += _ => Change();
        void ChangeMusic()
        {
            Music.ChangeSettings((float)MusicVolume.Value / 100, MusicMute.ButtonPressed);
            musicLabel.Text = $"Music: {MusicVolume.Value:0}%";
            saved.Text = Music.SaveError ?? "Settings are remembered on this computer.";
        }
        MusicVolume.ValueChanged += _ => ChangeMusic(); MusicMute.Toggled += _ => ChangeMusic();
        column.AddChild(DeskUi.Button("Try a cue", () => Sound.PlayCue("new-day")));
        column.AddChild(DeskUi.Button("Back", () => { SoundPanel.Hide(); SoundButton.GrabFocus(); }));
    }
}
