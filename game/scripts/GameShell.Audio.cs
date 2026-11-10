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
