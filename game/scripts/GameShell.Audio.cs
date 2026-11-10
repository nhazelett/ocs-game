using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal string? AudioSettingsPathOverride { get; set; }
    internal GameAudio Sound { get; private set; } = null!;
    internal Button SoundButton { get; private set; } = null!;
    internal HSlider SoundVolume { get; private set; } = null!;
    internal CheckBox SoundMute { get; private set; } = null!;
    internal PanelContainer SoundPanel { get; private set; } = null!;

    private void BuildSoundControls(VBoxContainer menu)
    {
        Sound = new GameAudio { Name = "GameAudio" }; AddChild(Sound);
        Sound.Initialize(AudioSettingsPathOverride ?? ProjectSettings.GlobalizePath("user://sound.cfg"));
        session.DayAdvanced += Sound.CompletedDay;
        session.ActionAccepted += Sound.AcceptedAction;
        session.DeploymentStarted += Sound.BeginDeployment;
        SoundButton = FrontButton("Sound", () => { SoundPanel.Show(); SoundMute.GrabFocus(); });
        menu.AddChild(SoundButton);
        SoundPanel = new PanelContainer { Name = "SoundSettings", ZIndex = 30, Visible = false };
        AddChild(SoundPanel); SoundPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center = new CenterContainer(); SoundPanel.AddChild(center);
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(480, 0) };
        column.AddThemeConstantOverride("separation", 24); center.AddChild(column);
        var title = DeskUi.Label("Sound"); title.AddThemeFontSizeOverride("font_size", 32); column.AddChild(title);
        column.AddChild(DeskUi.Label("Quiet cues for a new day, someone at the door, support going short, and an award made."));
        SoundMute = new CheckBox { Text = "Mute sound", ButtonPressed = Sound.Muted }; column.AddChild(SoundMute);
        var volumeLabel = DeskUi.Label(); column.AddChild(volumeLabel);
        SoundVolume = new HSlider { MinValue = 0, MaxValue = 100, Step = 1, Value = Sound.Volume * 100,
            CustomMinimumSize = new Vector2(480, 40), FocusMode = FocusModeEnum.All, TooltipText = "Sound volume" };
        using var track = new StyleBoxFlat { BgColor = new Color("b8b0a0"), ContentMarginTop = 4, ContentMarginBottom = 4 };
        track.SetCornerRadiusAll(4); SoundVolume.AddThemeStyleboxOverride("slider", track);
        using var fill = new StyleBoxFlat { BgColor = new Color("536c5a"), ContentMarginTop = 4, ContentMarginBottom = 4 };
        fill.SetCornerRadiusAll(4); SoundVolume.AddThemeStyleboxOverride("grabber_area", fill);
        SoundVolume.AddThemeStyleboxOverride("grabber_area_highlight", fill);
        using var knob = Image.CreateEmpty(18, 18, false, Image.Format.Rgba8); knob.Fill(Colors.Transparent);
        for (var y = 0; y < 18; y++) for (var x = 0; x < 18; x++)
            if (new Vector2(x - 8.5f, y - 8.5f).Length() <= 8) knob.SetPixel(x, y, new Color("536c5a"));
        using var knobTexture = ImageTexture.CreateFromImage(knob);
        SoundVolume.AddThemeIconOverride("grabber", knobTexture); SoundVolume.AddThemeIconOverride("grabber_highlight", knobTexture);
        column.AddChild(SoundVolume);
        var saved = DeskUi.Label("Settings are remembered on this computer."); column.AddChild(saved);
        void Change()
        {
            Sound.ChangeSettings((float)SoundVolume.Value / 100, SoundMute.ButtonPressed);
            volumeLabel.Text = $"Volume: {SoundVolume.Value:0}%";
            saved.Text = Sound.SaveError ?? "Settings are remembered on this computer.";
        }
        volumeLabel.Text = $"Volume: {SoundVolume.Value:0}%";
        SoundVolume.ValueChanged += _ => Change(); SoundMute.Toggled += _ => Change();
        column.AddChild(DeskUi.Button("Try a cue", () => Sound.PlayCue("new-day")));
        column.AddChild(DeskUi.Button("Back", () => { SoundPanel.Hide(); SoundButton.GrabFocus(); }));
    }
}
