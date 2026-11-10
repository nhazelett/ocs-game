using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game.Art;

// Read-only comparison tool. All three studies share one exact, real day-eleven snapshot.
public partial class ArtDirections : Control
{
    private readonly GameSessionBridge session = new();
    private Control surface = null!;
    private ScenarioBoardData board = null!;
    internal SessionView View => session.View!;
    internal string StateHash => session.StateHash!;
    internal int Direction { get; private set; }
    internal LeadItem Lead => View.MorningBoard().Lead!;
    internal Label LeadWords { get; private set; } = null!;
    internal Label Morning { get; private set; } = null!;
    internal Action? ExitStudy { get; set; }
    internal ArtDiorama? Diorama => surface.GetChildren().OfType<ArtDiorama>().FirstOrDefault();

    public override async void _Ready()
    {
        try
        {
            var folder = Path.Combine(GamePaths.ScenarioRoot, "spoke-b");
            session.LoadScenario(Path.Combine(folder, "scenario.json"));
            session.LoadReplay(Path.Combine(folder, "golden", "approach1-as-written.json"));
            for (var i = 0; i < 10; i++) session.RequestNextDay();
            board = ScenarioBoardData.Load(Path.Combine(folder, "scenario.json"));
            if (View.Day != 11 || Lead.Person?.DisplayName != "Capt Ana Ruiz" || View.MorningBoard().Tiles.Single(t => t.SiteId == "spokeBSite" && t.CapabilityId == "power").Status != "down")
                throw new InvalidDataException("ART-01 requires the authored day-eleven Ruiz/power-down state.");
            ShowDirection(0);
            var args = OS.GetCmdlineUserArgs(); var render = Array.IndexOf(args, "--art-render-dir");
            if (render < 0) return;
            var hash = StateHash;
            Directory.CreateDirectory(args[render + 1]);
            for (var i = 0; i < 3; i++)
            {
                ShowDirection(i);
                for (var frame = 0; frame < 12; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                var path = Path.Combine(args[render + 1], $"2026-11-09-codex-art-01-{i + 1}.png");
                if (image.GetSize() != new Vector2I(1280, 800) || image.SavePng(path) != Error.Ok) throw new IOException("Expected a real 1280x800 Godot capture.");
                if (hash != StateHash || LeadWords.Text != Lead.Text) throw new InvalidOperationException("Art changed Sim state or lead text.");
                GD.Print($"ART_RENDERED: {i + 1} {ArtStudyStyle.All[i].Name}, {new FileInfo(path).Length} bytes, same Sim hash {hash}");
            }
            RemoveChild(surface); surface.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("ART_RENDER_PASS: three native renders of one real morning."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"ART_RENDER_FAIL: {error}"); GetTree().Quit(1); }
    }

    internal void ShowDirection(int index)
    {
        Direction = index;
        if (surface is not null) { RemoveChild(surface); surface.QueueFree(); }
        var s = ArtStudyStyle.All[index];
        surface = new Control { Name = "StudySurface" }; AddChild(surface); surface.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var font = ArtStudyStyle.LoadFont(s.Font); var heading = ArtStudyStyle.LoadFont(s.HeadingFont);
        var theme = new Theme { DefaultFont = font, DefaultFontSize = 18 }; theme.SetFont("study_heading", "Label", heading); surface.Theme = theme;
        Panel(surface, new Rect2(0, 0, 1280, 800), s.Background);
        Label(surface, "OCS / LINES OF SUPPORT", new Rect2(30, 20, 540, 34), 24, s.Ink, heading);
        Label(surface, "Spoke B · operating picture", new Rect2(32, 53, 530, 26), 18, s.Muted);
        Label(surface, index == 0 ? "FIELD NOTES / 11" : index == 1 ? "MORNING WATCH / 11" : "A NEW MORNING", new Rect2(922, 29, 326, 30), 18, s.Muted, heading, HorizontalAlignment.Right);
        var strip = View.TopStrip();
        Panel(surface, new Rect2(24, 91, 1232, 72), s.Panel, s.Muted, index == 2 ? 12 : 0);
        Morning = Label(surface, $"Morning of day {View.Day}", new Rect2(44, 109, 260, 38), 25, s.Ink, heading);
        Label(surface, $"{strip.ShopHoursQueued:0.#} of {strip.ShopHoursPerDay:0.#} shop hours spoken for", new Rect2(310, 112, 320, 30), 18, s.Ink);
        Label(surface, $"Cash {DeskUi.Money(strip.CashCents)}", new Rect2(675, 112, 255, 30), 20, s.Ink, heading);
        Button(surface, "Next day", new Rect2(1076, 105, 160, 44), s.Accent, index == 1 ? s.Background : s.Panel, () => ShowReadOnlyNotice());
        var mapRect = new Rect2(24, 187, 786, 516);
        if (index == 2)
        {
            var diorama = new ArtDiorama(); surface.AddChild(diorama); diorama.Position = mapRect.Position; diorama.Size = mapRect.Size;
            diorama.Configure(board, View.MorningBoard().Tiles, s, font);
        }
        else
        {
            var map = new ArtMap2D { Board = board, Tiles = View.MorningBoard().Tiles, Routes = View.MapDay().Links, Study = index };
            surface.AddChild(map); map.Position = mapRect.Position; map.Size = mapRect.Size;
            map.Theme = theme;
        }
        Panel(surface, new Rect2(834, 187, 422, 516), s.Panel, index == 1 ? s.Accent : s.Muted, index == 2 ? 14 : 0);
        var portrait = new ArtPortrait { Study = index }; surface.AddChild(portrait); portrait.Position = new Vector2(855, 210); portrait.Size = new Vector2(92, 110);
        Label(surface, Lead.Person!.DisplayName, new Rect2(965, 210, 265, 70), 24, s.Ink, heading);
        Label(surface, Lead.Person.Unit ?? "", new Rect2(965, 280, 265, 30), 18, s.Muted);
        Panel(surface, new Rect2(855, 342, 5, 179), s.Alert);
        LeadWords = Label(surface, Lead.Text, new Rect2(879, 336, 351, 196), 27, s.Ink, heading);
        Button(surface, "Open message", new Rect2(856, 548, 378, 52), s.Accent, index == 1 ? s.Background : s.Panel, ShowReadOnlyNotice);
        Button(surface, "People to reach", new Rect2(856, 620, 181, 46), s.Background, s.Ink, ShowReadOnlyNotice);
        Button(surface, "Other updates", new Rect2(1048, 620, 186, 46), s.Background, s.Ink, ShowReadOnlyNotice);
        Label(surface, "Drag to move · scroll to zoom", new Rect2(34, 712, 730, 25), 16, s.Muted);
        Label(surface, $"{index + 1:00} / {s.Name}", new Rect2(30, 754, 260, 28), 21, s.Ink, heading);
        Label(surface, "Developer art study · same day, same facts", new Rect2(288, 757, 480, 25), 16, s.Muted);
        for (var i = 0; i < 3; i++) { var chosen = i; Button(surface, $"{i + 1}", new Rect2(868 + i * 50, 747, 42, 38), i == index ? s.Accent : s.Panel, i == index && index == 1 ? s.Background : s.Ink, () => ShowDirection(chosen)); }
        Button(surface, "Back to game", new Rect2(1042, 747, 214, 38), s.Panel, s.Ink, () => { if (ExitStudy is not null) ExitStudy(); else GetTree().ChangeSceneToFile("res://scenes/Main.tscn"); });
    }

    private void ShowReadOnlyNotice()
    {
        var dialog = new AcceptDialog { Title = "ART-01 · read-only study", DialogText = Lead.Text + "\n\nThis comparison does not send game commands. Use Back to game to play." };
        surface.AddChild(dialog); dialog.Confirmed += dialog.QueueFree; dialog.PopupCentered(new Vector2I(520, 240));
    }
    internal static Panel Panel(Control parent, Rect2 rect, string color, string? border = null, int radius = 0)
    {
        var panel = new Panel { Position = rect.Position, Size = rect.Size, MouseFilter = MouseFilterEnum.Ignore };
        var style = new StyleBoxFlat { BgColor = new Color(color), CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius };
        if (border is not null) { style.BorderColor = new Color(border); style.SetBorderWidthAll(1); }
        panel.AddThemeStyleboxOverride("panel", style); parent.AddChild(panel); return panel;
    }
    internal static Label Label(Control parent, string text, Rect2 rect, int size, string color, Font? font = null, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Text = text, HorizontalAlignment = align, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", new Color(color));
        if (font is not null) label.AddThemeFontOverride("font", font); parent.AddChild(label); label.Position = rect.Position; label.Size = rect.Size; return label;
    }
    private static void Button(Control parent, string text, Rect2 rect, string bg, string ink, Action pressed)
    {
        var button = new Button { Text = text, Position = rect.Position, Size = rect.Size, MouseDefaultCursorShape = CursorShape.PointingHand };
        var style = new StyleBoxFlat { BgColor = new Color(bg), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        button.AddThemeStyleboxOverride("normal", style); var hover = (StyleBoxFlat)style.Duplicate(); hover.BgColor = new Color(bg).Lightened(.12f); button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeColorOverride("font_color", new Color(ink)); button.AddThemeColorOverride("font_hover_color", new Color(ink)); button.AddThemeFontSizeOverride("font_size", 18);
        button.Pressed += pressed; parent.AddChild(button);
    }
}
