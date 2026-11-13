using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class TitleArtSmokeTest : Node
{
    public override async void _Ready()
    {
        var folder = Path.Combine(Path.GetTempPath(), "ocs-title-" + Guid.NewGuid());
        Directory.CreateDirectory(folder); GameShell? shell = null;
        try
        {
            var args = OS.GetCmdlineUserArgs(); var baseline = args.Contains("--baseline");
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell = scene.Instantiate<GameShell>();
            shell.AutosavePathOverride = Path.Combine(folder, "save.json");
            shell.LookSettingsPathOverride = Path.Combine(folder, "look.cfg");
            shell.TeamSettingsPathOverride = Path.Combine(folder, "team.cfg");
            shell.AudioSettingsPathOverride = Path.Combine(folder, "sound.cfg");
            shell.MusicSettingsPathOverride = Path.Combine(folder, "music.cfg");
            File.WriteAllText(shell.AudioSettingsPathOverride, "[sound]\nmuted=true\n");
            File.WriteAllText(shell.MusicSettingsPathOverride, "[music]\nmuted=true\n");
            AddChild(shell);
            Require(shell.AtFrontDoor && shell.Session.View is null, "The title creates no deployment.");
            Require(shell.ContinueGameButton.Disabled, "No save can be loaded at cold start.");
            if (!baseline) Require(!shell.ContinueGameButton.IsVisibleInTree(), "Continue is absent without a save.");
            await Shot("cold");
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); await Shot("deployments");
            shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed); DeployWithoutPrep(shell); Step(shell);
            var hash = shell.Session.StateHash;
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            Require(!shell.ContinueGameButton.Disabled && shell.ContinueGameButton.IsVisibleInTree(), "A real autosave reveals Continue.");
            if (!baseline) Require(shell.NewGameButton.ThemeTypeVariation == "TabletopPrimary"
                && shell.ContinueGameButton.ThemeTypeVariation != "TabletopPrimary", "New game leads and Continue is secondary.");
            await Shot("saved");
            foreach (var direction in new[] { 0, 1, 2 })
            {
                shell.LookPicker.Select(direction); shell.LookPicker.EmitSignal(OptionButton.SignalName.ItemSelected, direction);
                await Shot("look-" + direction);
                Require(shell.Session.StateHash == hash, "Title art and settings never change a deployment.");
            }
            shell.SoundButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.SoundPanel.Visible && shell.SoundMute.HasFocus(), "Small Sound control opens settings with keyboard focus.");
            shell.SoundPanel.Hide(); shell.LookButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.LookPanel.Visible && shell.LookPicker.HasFocus(), "Small Look control opens settings with keyboard focus.");
            shell.LookPanel.Hide(); shell.ContinueGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(!shell.AtFrontDoor && shell.Session.StateHash == hash, "Secondary Continue restores the exact autosave.");
            if (!baseline)
            {
                var art = shell.FindChildren("TitleHarbor", "", true, false).Cast<Control>().Single();
                Require(!art.IsVisibleInTree(), "The harbor is hidden during play.");
            }
            shell.Free(); shell = null;
            for (var i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("TITLE_ART_SMOKE_PASS: cold/saved title, real Continue, new-game routes, all looks, secondary settings, idle render and unchanged Sim.");
            GetTree().Quit();

            async Task Shot(string state)
            {
                for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (DisplayServer.GetName() != "headless")
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (!baseline)
                {
                    var screen = GetViewport().GetVisibleRect();
                    if (state != "deployments")
                        Require(shell.GetNode<Control>("FrontDoor").FindChildren("*", "Button", true, false)
                            .Cast<Button>().Count(b => b.IsVisibleInTree() && b.ThemeTypeVariation == "TabletopPrimary") == 1,
                            "New game remains the only primary action even with a live deployment.");
                    foreach (var button in new[] { shell.NewGameButton, shell.SoundButton, shell.LookButton })
                        if (GodotObject.IsInstanceValid(button) && button.IsVisibleInTree())
                            Require(screen.Encloses(button.GetGlobalRect()), "Title actions stay in the viewport.");
                    Require(shell.SoundButton.CustomMinimumSize.Y < 50 && shell.LookButton.CustomMinimumSize.Y < 50,
                        "Settings are visually smaller than the deployment actions.");
                    var art = shell.FindChildren("TitleHarbor", "", true, false).Cast<Control>().Single();
                    Require(screen.Encloses(art.GetGlobalRect()) && art.Size.X >= 400 && art.Size.Y >= 400,
                        "The key image occupies a substantial visible panel.");
                    var view = art.GetNode<SubViewport>("HarborViewport");
                    Require(view.GetChild<Node3D>(0).FindChildren("TitleKit*", "", true, false).Count == 9,
                        "The title uses the actual board kit models.");
                    if (DisplayServer.GetName() != "headless")
                    {
                        using var screenImage = GetViewport().GetTexture().GetImage();
                        var bounds = art.GetGlobalRect(); var samples = 0; var foreground = 0;
                        var sea = screenImage.GetPixel((int)bounds.End.X - 4, (int)bounds.End.Y - 4);
                        for (var y = (int)bounds.Position.Y + 12; y < bounds.End.Y; y += 24)
                            for (var x = (int)bounds.Position.X + 12; x < bounds.End.X; x += 24)
                            {
                                var pixel = screenImage.GetPixel(x, y); samples++;
                                if (Math.Abs(pixel.R - sea.R) + Math.Abs(pixel.G - sea.G) + Math.Abs(pixel.B - sea.B) > .12f) foreground++;
                            }
                        Require(foreground > samples / 8, "The displayed title contains the harbor, rather than just its sea background.");
                        using var artImage = view.GetTexture().GetImage();
                        var colors = new HashSet<Color>();
                        for (var y = 0; y < artImage.GetHeight(); y += 24)
                            for (var x = 0; x < artImage.GetWidth(); x += 24) colors.Add(artImage.GetPixel(x, y));
                        Require(colors.Count > 20, "The harbor remains a rendered image after every navigation and look change.");
                        Require(RenderingServer.ViewportGetUpdateMode(view.GetViewportRid()) == RenderingServer.ViewportUpdateMode.Disabled,
                            "The rendered key art has stopped all continuous 3D updates.");
                    }
                }
                var at = Array.IndexOf(args, "--screenshots"); if (at < 0) return;
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.GetWidth() == 1280 && image.GetHeight() == 800, "Review uses the release viewport.");
                Require(image.SavePng(args[at + 1] + "-" + state + ".png") == Error.Ok, "Native title screenshot saved.");
            }
        }
        catch (Exception error) { GD.PushError("TITLE_ART_SMOKE_FAIL: " + error); GetTree().Quit(1); }
        finally { shell?.QueueFree(); Directory.Delete(folder, true); TabletopTheme.Select(2); }
    }
}
