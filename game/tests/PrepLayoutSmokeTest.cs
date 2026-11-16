using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PrepLayoutSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); AddChild(shell);
            shell.AutosavePathOverride = Path.Combine(Path.GetTempPath(), "ocs-prep-layout-" + Guid.NewGuid() + ".json");
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed);
            async Task Settle()
            {
                for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            void Footer()
            {
                var page = PlayScreen.Descendants(shell).OfType<Control>().Single(c => c.Name == "PrepScreen");
                var back = PlayScreen.Descendants(page).OfType<Button>().Single(b => b.Text == "Back");
                var viewport = GetViewport().GetVisibleRect();
                foreach (var button in new[] { back, shell.StartGameButton }.Concat(
                    shell.Session.PrepTeam.Count > 0 ? new[] { shell.TeamCustomizeButton } : Array.Empty<Button>()))
                    Require(button.IsVisibleInTree() && viewport.Encloses(button.GetGlobalRect()),
                        $"Preparation footer stays wholly on screen: {button.Text}, {button.GetGlobalRect()}.");
                Require(shell.PrepScroll.GetGlobalRect().End.Y <= back.GlobalPosition.Y,
                    "The scrolling body cannot cover departure controls.");
            }
            void Choose(int days)
            {
                var options = shell.Session.Prep!.Options;
                var mask = Enumerable.Range(0, 1 << options.Count).First(m =>
                    options.Where((_, i) => (m & (1 << i)) != 0).Sum(o => o.PrepDays) == days);
                foreach (var toggle in shell.PrepChoices.Values) toggle.ButtonPressed = false;
                for (var i = 0; i < options.Count; i++)
                    if ((mask & (1 << i)) != 0) shell.PrepChoices[options[i].Id].ButtonPressed = true;
                Require(shell.Session.CheckPrep(shell.PrepChoices.Where(p => p.Value.ButtonPressed).Select(p => p.Key)).UsedDays == days,
                    "Fixture spends the requested days through the real choice handlers.");
                foreach (var option in options)
                {
                    var toggle = shell.PrepChoices[option.Id];
                    Require(toggle.Disabled == (!toggle.ButtonPressed && option.PrepDays > shell.Session.Prep.BudgetDays - days),
                        "Only unselected choices that exceed the days left are disabled.");
                }
                Require(!shell.StartGameButton.Disabled && shell.Session.View is null, "Affordable choices stay before deployment and allow leaving.");
            }
            async Task Capture(string suffix)
            {
                await Settle(); Footer();
                var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--screenshots");
                if (index < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[index + 1] + "-" + suffix + ".png") == Error.Ok, "Native prep screenshot saved.");
            }
            // Every affordable total, including the previously stuck full-budget state.
            foreach (var days in Enumerable.Range(0, 15)) { Choose(days); await Settle(); Footer(); }
            Choose(10); shell.PrepScroll.ScrollVertical = 0; await Capture("10-days");
            Choose(14); await Settle(); shell.PrepScroll.ScrollVertical = 10000; await Settle();
            var templates = shell.PrepChoices["templatesAndKit"];
            Input.ParseInputEvent(new InputEventMouseMotion { Position = templates.GetGlobalRect().GetCenter() });
            await Capture("14-days");
            Require(!templates.Disabled && templates.ButtonPressed, "A checked item can still be removed at the full budget.");
            Require(templates.GetThemeColor("font_hover_pressed_color") == TabletopTheme.Ink
                && templates.GetThemeStylebox("hover_pressed") is StyleBoxFlat { BorderWidthLeft: > 0 },
                "A checked and hovered row retains ink and its frame.");
            templates.ButtonPressed = false; await Settle(); Footer();
            var warrant = shell.PrepChoices["warrantOrThreshold"];
            var reason = warrant.GetParent().GetChildren().OfType<Label>().Single();
            Require(warrant.Disabled && reason.Visible && reason.Text == "2 days; you have 1 left.", "The over-budget item carries its exact visible reason at thirteen days.");
            var point = warrant.GetGlobalRect().GetCenter();
            Input.ParseInputEvent(new InputEventMouseMotion { Position = point });
            Input.ParseInputEvent(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true });
            await Settle();
            Input.ParseInputEvent(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = false });
            await Settle();
            Require(!warrant.ButtonPressed, "Real mouse input cannot tick the disabled over-budget choice.");
            await Capture("13-days");
            shell.TeamCustomizeButton.EmitSignal(Button.SignalName.Pressed); await Settle();
            Require(shell.TeamPanel.Visible, "Team customization remains reachable at thirteen days.");
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed); await Settle(); Footer();
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.View is not null && shell.ReadingTurnover, "The visible departure action starts the selected deployment.");
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            foreach (var days in Enumerable.Range(0, 15)) { Choose(days); await Settle(); Footer(); }
            GD.Print("PREP_LAYOUT_SMOKE_PASS: all day totals, fixed footer, choice reasons, checked hover, refused input, team and departure.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PREP_LAYOUT_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
