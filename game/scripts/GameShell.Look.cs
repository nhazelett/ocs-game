using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal string? LookSettingsPathOverride { get; set; }
    internal Button LookButton { get; private set; } = null!;
    internal OptionButton LookPicker { get; private set; } = null!;
    internal PanelContainer LookPanel { get; private set; } = null!;
    internal int LookDirection => TabletopTheme.Direction;
    private Label lookSaved = null!;
    private string LookSettingsPath => LookSettingsPathOverride ?? ProjectSettings.GlobalizePath("user://look.cfg");

    private void LoadLook()
    {
        using var config = new ConfigFile();
        var index = 2;
        if (config.Load(LookSettingsPath) == Error.Ok)
        {
            var stored = config.GetValue("look", "direction", 2);
            if (stored.VariantType == Variant.Type.Int && stored.AsInt64() is >= 0 and < 3)
                index = stored.AsInt32();
        }
        TabletopTheme.Select(index);
    }

    private void BuildLookControls(VBoxContainer menu)
    {
        LookButton = FrontButton("Look", () => { LookPanel.Show(); LookPicker.GrabFocus(); });
        menu.AddChild(LookButton);
        LookPanel = new PanelContainer { Name = "LookSettings", ZIndex = 30, Visible = false };
        AddChild(LookPanel); LookPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center = new CenterContainer(); LookPanel.AddChild(center);
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(480, 0) };
        column.AddThemeConstantOverride("separation", 24); center.AddChild(column);
        var title = DeskUi.Label("Look"); title.AddThemeFontSizeOverride("font_size", 32); column.AddChild(title);
        column.AddChild(DeskUi.Label("Choose how the desk and map look. Tabletop is the default."));
        LookPicker = new OptionButton { FocusMode = FocusModeEnum.All, CustomMinimumSize = new Vector2(480, 48) };
        foreach (var style in Art.ArtStudyStyle.All) LookPicker.AddItem(style.Name);
        LookPicker.Select(LookDirection); column.AddChild(LookPicker);
        lookSaved = DeskUi.Label("Settings are remembered on this computer."); column.AddChild(lookSaved);
        LookPicker.ItemSelected += index => ChangeLook((int)index);
        column.AddChild(DeskUi.Button("Back", () => { LookPanel.Hide(); LookButton.GrabFocus(); }));
    }

    private void ChangeLook(int direction)
    {
        if (direction is < 0 or >= 3) return;
        var previous = TabletopTheme.ColorsByRole();
        TabletopTheme.Select(direction);
        using var theme = TabletopTheme.Create();
        Theme = theme;
        RestyleLook(this, previous, TabletopTheme.ColorsByRole());
        Map.RefreshLook();
        // Rebuild interpolated scorecard colors as well as the inherited theme.
        if (session.View?.Finished == true) { debriefHash = null; RefreshDebrief(session.View); }
        using var config = new ConfigFile();
        config.SetValue("look", "direction", direction);
        lookSaved.Text = config.Save(LookSettingsPath) == Error.Ok
            ? "Settings are remembered on this computer."
            : "This look works for this visit, but could not be saved.";
    }

    private static void RestyleLook(Node node, Color[] previous, Color[] next)
    {
        if (node is Art.ArtDirections) return; // The independent comparison owns its three palettes.
        Color Remap(Color value)
        {
            for (var i = 0; i < previous.Length; i++)
                if (value.IsEqualApprox(previous[i])) return next[i];
            return value;
        }
        if (node is ColorRect rect) rect.Color = Remap(rect.Color);
        if (node is Control control)
        {
            foreach (var property in control.GetPropertyList())
            {
                var name = property["name"].AsString();
                if (name.StartsWith("theme_override_colors/", StringComparison.Ordinal) && control.HasThemeColorOverride(name["theme_override_colors/".Length..]))
                    control.Set(name, Remap(control.Get(name).AsColor()));
                else if (name.StartsWith("theme_override_styles/", StringComparison.Ordinal) && control.HasThemeStyleboxOverride(name["theme_override_styles/".Length..]))
                {
                    using var box = control.Get(name).AsGodotObject() as StyleBoxFlat;
                    if (box is null) continue;
                    using var updated = (StyleBoxFlat)box.Duplicate();
                    updated.BgColor = Remap(box.BgColor); updated.BorderColor = Remap(box.BorderColor);
                    control.Set(name, updated);
                }
            }
        }
        foreach (var child in node.GetChildren()) RestyleLook(child, previous, next);
    }
}
