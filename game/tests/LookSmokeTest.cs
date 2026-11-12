using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class LookSmokeTest : Node
{
    public override async void _Ready()
    {
        var folder = Path.Combine(Path.GetTempPath(), "ocs-look-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        GameShell? shell = null;
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            GameShell Open()
            {
                var value = scene.Instantiate<GameShell>();
                value.LookSettingsPathOverride = Path.Combine(folder, "look.cfg");
                value.AudioSettingsPathOverride = Path.Combine(folder, "sound.cfg");
                value.AutosavePathOverride = Path.Combine(folder, "save.json");
                AddChild(value); return value;
            }
            async Task Frames()
            {
                for (var i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string suffix)
            {
                await Frames();
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot + 1] + "-" + suffix + ".png") == Error.Ok, "Native screenshot saved.");
            }
            void Choose(int direction)
            {
                shell!.LookPicker.Select(direction);
                shell.LookPicker.EmitSignal(OptionButton.SignalName.ItemSelected, direction);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                using var theme = shell.Theme;
                using var panel = theme.GetStylebox("panel", "Panel");
                using var arrow = theme.GetIcon("arrow", "OptionButton");
                Require(panel is StyleBoxFlat paper && paper.BgColor.IsEqualApprox(TabletopTheme.Paper)
                    && arrow.GetSize().X > 0, "Each native theme retains its styles and icons after managed wrappers are released.");
                foreach (var (surface, color) in new[] { (shell.GetNode<PanelContainer>("FrontDoor"), TabletopTheme.Background),
                    (shell.GetNode<PanelContainer>("Opening"), TabletopTheme.Background),
                    ((PanelContainer)shell.LeadWords.GetParent().GetParent(), TabletopTheme.Paper) })
                {
                    using var retained = surface.GetThemeStylebox("panel");
                    Require(retained is StyleBoxFlat panelStyle && panelStyle.BgColor.IsEqualApprox(color),
                        "The title, opening and lead panels retain their exact native colors after managed collection.");
                }
            }
            shell = Open();
            Require(shell.LookDirection == 2 && shell.LookPicker.Selected == 2, "Tabletop is the fresh default.");
            shell.LookButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.LookPanel.Visible && shell.LookPicker.HasFocus(), "Look is reachable from the title with keyboard focus.");
            await Capture("setting");
            shell.LookPanel.Hide(); shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed); DeployWithoutPrep(shell);
            shell.DeveloperToggle.ButtonPressed = true; Replay(shell); Step(shell, 10);
            shell.DeveloperToggle.ButtonPressed = false;
            var hash = shell.Session.StateHash;
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            map.ZoomAt(new Vector2(220, 180), 1.2f); map.PanBy(new Vector2(30, -12));
            var pan = map.Tabletop!.Pan; var zoom = map.Tabletop.CameraSize;
            var facts = map.Day; var tiles = map.Tiles;
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            foreach (var direction in new[] { 0, 1, 2, 1, 0, 2 })
            {
                var instrument = shell.InstrumentInput.Selected;
                var vendor = shell.VendorInput.Selected;
                Choose(direction); await Frames();
                var style = Art.ArtStudyStyle.All[direction];
                Require(shell.Session.StateHash == hash && shell.InstrumentInput.Selected == instrument && shell.VendorInput.Selected == vendor,
                    "Changing look neither changes the deployment nor clears the current assembly.");
                Require(map.Day == facts && map.Tiles.SequenceEqual(tiles) && map.Tabletop!.Pan == pan && map.Tabletop.CameraSize == zoom,
                    "Look retains exact MapDay facts and the player's camera.");
                Require(map.Tabletop!.SiteStatuses["spokeBSite"].Text.Contains("down"), "Every look keeps the outage in words.");
                Require(shell.GetThemeDefaultFont().GetFontName().Contains(direction == 2 ? "Atkinson" : "Barlow"),
                    "The established body font is applied immediately.");
                Require(shell.GetThemeColor("font_color", "Label").IsEqualApprox(new Color(style.Ink)),
                    "Every screen inherits the selected ink.");
                Require(shell.GetNode<Label>("%GameTitle").GetThemeColor("font_color").IsEqualApprox(TabletopTheme.Ink)
                    && shell.GetNode<Button>("Navigation/Menu").GetThemeColor("font_color").IsEqualApprox(TabletopTheme.Ink),
                    "Existing labels and ordinary buttons keep legible inherited ink after switching.");
                Require(shell.LookButton.GetThemeColor("font_color").IsEqualApprox(TabletopTheme.PrimaryInk),
                    "Primary buttons use legible ink against the accent.");
                for (var i = 0; i < tabs.GetTabCount(); i++)
                {
                    tabs.CurrentTab = i;
                    Require(tabs.GetCurrentTabControl().GetThemeDefaultFont() == shell.GetThemeDefaultFont(), "All desks inherit the selected font.");
                }
                tabs.CurrentTab = GameShell.TodayTab; map.FitMap();
                await Capture("morning-" + direction);
                map.ZoomAt(new Vector2(220, 180), 1.2f); map.PanBy(new Vector2(30, -12));
                pan = map.Tabletop!.Pan; zoom = map.Tabletop.CameraSize;
            }
            // Changing look also updates screens built after the setting changed.
            Choose(1); shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            await Capture("title-night");
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            await Capture("prep-night"); DeployWithoutPrep(shell);
            shell.DeveloperToggle.ButtonPressed = true; Replay(shell);
            while (!shell.Session.View!.Finished) Step(shell);
            shell.DeveloperToggle.ButtonPressed = false;
            var endHash = shell.Session.StateHash; var summary = shell.DebriefSummaryText.Text;
            foreach (var direction in new[] { 0, 1, 2 })
            {
                Choose(direction); await Capture("debrief-" + direction);
                Require(shell.Session.StateHash == endHash && shell.DebriefSummaryText.Text == summary, "After-action wording and outcome survive each look.");
                Require(shell.DebriefSummaryText.GetThemeDefaultFont() == shell.GetThemeDefaultFont(), "After-action uses the current theme.");
            }
            Choose(1); shell.Free(); shell = null; await Frames();
            shell = Open(); Require(shell.LookDirection == 1 && shell.LookPicker.Selected == 1, "A new shell restores the selected look from disk.");
            shell.Free(); shell = null; await Frames();
            using (var invalid = new ConfigFile())
            {
                invalid.SetValue("look", "direction", "Night watch"); invalid.Save(Path.Combine(folder, "look.cfg"));
            }
            shell = Open(); Require(shell.LookDirection == 2, "A malformed setting safely uses Tabletop.");
            shell.LookSettingsPathOverride = Path.Combine(folder, "missing", "look.cfg");
            Choose(0); Require(shell.LookDirection == 0, "An unwritable preference still applies for this visit.");
            Require(shell.LookPanel.GetChildren().Count > 0, "Settings remain usable after a save failure.");
            shell.Free(); shell = null; await Frames();
            GD.Print("LOOK_SMOKE_PASS: all three looks, title/prep/all desks/debrief, live state and camera preserved, keyboard access, disk restore and invalid/unwritable settings.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("LOOK_SMOKE_FAIL: " + error); GetTree().Quit(1); }
        finally { shell?.QueueFree(); Directory.Delete(folder, true); TabletopTheme.Select(2); }
    }
}
