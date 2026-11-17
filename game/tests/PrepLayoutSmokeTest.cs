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
            Button Footer()
            {
                var page = PlayScreen.Descendants(shell).OfType<Control>().Single(c => c.Name == "PrepScreen");
                var back = PlayScreen.Descendants(page).OfType<Button>().Single(b => b.Text == "Back");
                var viewport = GetViewport().GetVisibleRect();
                if (!viewport.Encloses(back.GetGlobalRect()))
                    for (Node? node = back; node is not null; node = node.GetParent())
                        if (node is Control c) GD.Print($"LAYOUT {c.GetPath()}: rect={c.GetGlobalRect()} min={c.GetCombinedMinimumSize()} offset={c.OffsetTop},{c.OffsetBottom}");
                foreach (var button in new[] { back, shell.StartGameButton }.Concat(
                    shell.Session.PrepTeam.Count > 0 ? new[] { shell.TeamCustomizeButton } : Array.Empty<Button>()))
                    Require(button.IsVisibleInTree() && viewport.Encloses(button.GetGlobalRect()),
                        $"Preparation footer stays wholly on screen: {button.Text}, {button.GetGlobalRect()}.");
                var count = PlayScreen.Descendants(page).OfType<Label>().Single(l => l.Text.StartsWith("You've planned "));
                Require(count.IsVisibleInTree() && viewport.Encloses(count.GetGlobalRect()), "The day count stays in view on every visit.");
                Require(shell.PrepScroll.GetGlobalRect().End.Y <= back.GlobalPosition.Y,
                    "The scrolling body cannot cover departure controls.");
                var where = PlayScreen.Descendants(page).OfType<Label>().SingleOrDefault(l => l.HasMeta("prep_where"));
                Require(where is not null && where.Text == shell.Session.Prep!.Where && viewport.Encloses(where.GetGlobalRect()),
                    "The place briefing stays wholly in view before the choices.");
                return back;
            }
            void Choose(int days, string? exclude = null)
            {
                var options = shell.Session.Prep!.Offered;
                var mask = Enumerable.Range(0, 1 << options.Count).First(m =>
                    options.Where((_, i) => (m & (1 << i)) != 0).Sum(o => o.PrepDays) == days && options.Where((_, i) => (m & (1 << i)) != 0).All(o => o.Id != exclude));
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
            foreach (var days in Enumerable.Range(0, Math.Min(shell.Session.Prep!.BudgetDays, shell.Session.Prep.OfferedDays) + 1)) { Choose(days); await Settle(); Footer(); }
            Choose(shell.Session.Prep!.OfferedDays); shell.PrepScroll.ScrollVertical = 0; await Capture("tutorial");
            shell.PrepScroll.ScrollVertical = 10000; await Settle();
            var templates = shell.PrepChoices["templatesAndKit"];
            Input.ParseInputEvent(new InputEventMouseMotion { Position = templates.GetGlobalRect().GetCenter() });
            Require(!templates.Disabled && templates.ButtonPressed, "Every offered tutorial choice fits its nine-day total.");
            Require(templates.GetThemeColor("font_hover_pressed_color") == TabletopTheme.Ink
                && templates.GetThemeStylebox("hover_pressed") is StyleBoxFlat { BorderWidthLeft: > 0 },
                "A checked and hovered row retains ink and its frame.");
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.View is not null && shell.ReadingTurnover, "The visible departure action starts the selected deployment.");
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            foreach (var days in Enumerable.Range(0, 15)) { Choose(days); await Settle(); Footer(); }
            Choose(13, "cashAndPayingAgent"); await Settle();
            var payingAgent = shell.PrepChoices["cashAndPayingAgent"];
            shell.PrepScroll.EnsureControlVisible(payingAgent); await Settle();
            var reason = payingAgent.GetParent().GetChildren().OfType<Label>().Single(l => l.HasMeta("prep_reason"));
            Require(payingAgent.Disabled && reason.Visible && reason.Text == "2 days; you have 1 left.", "Spoke B's paying agent carries its exact visible refusal at thirteen days.");
            var point = payingAgent.GetGlobalRect().GetCenter();
            Input.ParseInputEvent(new InputEventMouseMotion { Position = point });
            Input.ParseInputEvent(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true });
            await Settle();
            Input.ParseInputEvent(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = false });
            await Settle();
            Require(!payingAgent.ButtonPressed, "Real mouse input cannot tick the disabled over-budget choice.");
            await Capture("spoke-b");
            shell.TeamCustomizeButton.EmitSignal(Button.SignalName.Pressed); await Settle();
            Require(shell.TeamPanel.Visible, "Team customization remains reachable at thirteen days.");
            shell.TeamBackButton.EmitSignal(Button.SignalName.Pressed); await Settle(); Footer();
            foreach (var tutorial in new[] { false, true })
            {
                if (tutorial)
                {
                    Footer().EmitSignal(Button.SignalName.Pressed); await Settle();
                    shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed); await Settle();
                }
                Choose(Math.Min(14, shell.Session.Prep!.OfferedDays)); await Settle(); Footer();
                Footer().EmitSignal(Button.SignalName.Pressed); await Settle();
                Require(shell.AtFrontDoor && shell.TutorialGameButton.IsVisibleInTree() && shell.SpokeBGameButton.IsVisibleInTree(),
                    "Preparation Back returns to scenario choice.");
                (tutorial ? shell.TutorialGameButton : shell.SpokeBGameButton).EmitSignal(Button.SignalName.Pressed);
                await Capture(tutorial ? "tutorial-second-back" : "spoke-b-second-back");
                foreach (var days in Enumerable.Range(0, Math.Min(shell.Session.Prep!.BudgetDays, shell.Session.Prep.OfferedDays) + 1)) { Choose(days); await Settle(); Footer(); }
                shell.StartGameButton.EmitSignal(Button.SignalName.Pressed); FinishOpening(shell); Step(shell);
                shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
                shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
                (tutorial ? shell.TutorialGameButton : shell.SpokeBGameButton).EmitSignal(Button.SignalName.Pressed);
                await Capture(tutorial ? "tutorial-second-menu" : "spoke-b-second-menu");
                foreach (var days in Enumerable.Range(0, Math.Min(shell.Session.Prep!.BudgetDays, shell.Session.Prep.OfferedDays) + 1)) { Choose(days); await Settle(); Footer(); }
            }
            shell.QueueFree(); await Settle();
            GD.Print("PREP_LAYOUT_SMOKE_PASS: all day totals, fixed footer, choice reasons, checked hover, refused input, team, departure and both same-scenario re-entry routes.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PREP_LAYOUT_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
