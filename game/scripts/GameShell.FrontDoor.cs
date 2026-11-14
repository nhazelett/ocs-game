using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private PanelContainer frontDoor = null!;
    private VBoxContainer frontChoices = null!;
    private Label frontMessage = null!;
    private Button menuButton = null!;
    internal CheckBox DeveloperToggle { get; private set; } = null!;
    internal Button DeveloperWorkspaceButton { get; private set; } = null!;
    internal Button ArtStudiesButton { get; private set; } = null!;
    internal Button NewGameButton { get; private set; } = null!;
    internal Button ContinueGameButton { get; private set; } = null!;
    internal Button SpokeBGameButton { get; private set; } = null!;
    internal Button TutorialGameButton { get; private set; } = null!;
    internal bool AtFrontDoor => frontDoor.Visible;
    internal TitleHarbor TitleArt { get; private set; } = null!;
    private bool DeveloperTools => DeveloperToggle?.ButtonPressed == true;

    private void BuildFrontDoor()
    {
        frontDoor = new PanelContainer { Name = "FrontDoor", ZIndex = 15 };
        using var background = new StyleBoxFlat { BgColor = TabletopTheme.Background };
        frontDoor.AddThemeStyleboxOverride("panel", background);
        AddChild(frontDoor); frontDoor.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var margin = new MarginContainer(); frontDoor.AddChild(margin);
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, side == "top" ? 76 : 48);
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 48); margin.AddChild(row);
        var column = new VBoxContainer { Name = "TitleChoices", CustomMinimumSize = new Vector2(390, 0),
            SizeFlagsVertical = SizeFlags.ShrinkCenter };
        column.AddThemeConstantOverride("separation", 24); row.AddChild(column);
        var imprint = new Label { Text = "KTHQ   /   OCS", ThemeTypeVariation = "TabletopHeading" };
        imprint.AddThemeFontSizeOverride("font_size", 18); imprint.AddThemeColorOverride("font_color", TabletopTheme.Moss); column.AddChild(imprint);
        var title = new Label { Name = "Title", Text = "Lines of\nSupport", ThemeTypeVariation = "TabletopHeading" };
        title.AddThemeFontSizeOverride("font_size", 64); column.AddChild(title);
        var accent = new ColorRect { Color = TabletopTheme.Moss, CustomMinimumSize = new Vector2(56, 3),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin, MouseFilter = MouseFilterEnum.Ignore }; column.AddChild(accent);
        frontMessage = DeskUi.Label("Take over the contracting shop. Keep the deployment supported as the days unfold.");
        frontMessage.CustomMinimumSize = new Vector2(390, 0); column.AddChild(frontMessage);
        frontChoices = new VBoxContainer(); frontChoices.AddThemeConstantOverride("separation", 12); column.AddChild(frontChoices);
        TitleArt = new TitleHarbor { Name = "TitleHarbor", SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill };
        row.AddChild(TitleArt);
        DeveloperWorkspaceButton = FrontButton("Open developer workspace", ShowGameSurface); column.AddChild(DeveloperWorkspaceButton);
        var skipQuietDays = GetNode<Button>("%Continue");
        var settings = new VBoxContainer(); column.AddChild(settings);
        BuildSoundControls(settings);
        BuildLookControls(settings);
        var settingsRow = new HBoxContainer(); settingsRow.AddThemeConstantOverride("separation", 24); settings.AddChild(settingsRow);
        foreach (var button in new[] { SoundButton, LookButton, skipQuietDays })
        {
            button.Reparent(settingsRow, false); button.ThemeTypeVariation = "TabletopSetting";
            button.Flat = false; button.CustomMinimumSize = new Vector2(64, 38);
            button.AddThemeFontSizeOverride("font_size", 18);
        }
        ArtStudiesButton = FrontButton("ART-01 · compare art directions", () =>
        {
            var study = new Art.ArtDirections { ZIndex = 50 };
            study.ExitStudy = study.QueueFree; AddChild(study); study.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        });
        column.AddChild(ArtStudiesButton);
        var chrome = new HBoxContainer { Name = "Navigation", ZIndex = 20, Alignment = BoxContainer.AlignmentMode.End };
        AddChild(chrome); chrome.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        chrome.OffsetLeft = -310; chrome.OffsetRight = -20; chrome.OffsetTop = 16; chrome.OffsetBottom = 60;
        menuButton = DeskUi.Button("Menu", ShowFrontDoor); menuButton.Name = "Menu"; chrome.AddChild(menuButton);
        DeveloperToggle = new CheckBox { Text = "Developer tools" }; chrome.AddChild(DeveloperToggle);
        DeveloperToggle.Toggled += _ => RefreshDeveloperAccess();
        ShowFrontDoor();
    }

    private static Button FrontButton(string text, Action pressed)
    {
        var button = DeskUi.Button(text, pressed); button.CustomMinimumSize = new Vector2(0, 50);
        button.AddThemeFontSizeOverride("font_size", 22);
        button.ThemeTypeVariation = "TabletopPrimary";
        return button;
    }

    private void ShowFrontDoor()
    {
        CancelEndDayCheck();
        FinishDayPlayback(false);
        CloseDaySummary(false);
        GetNode<Control>("Navigation").OffsetRight = -48;
        frontDoor.Show(); GetNode<Control>("Margin").Hide(); menuButton?.Hide();
        openingSurface.Hide();
        frontMessage.Text = "Take over the contracting shop. Keep the deployment supported as the days unfold.";
        DeskUi.Clear(frontChoices);
        NewGameButton = FrontButton("New game", ShowNewGames); frontChoices.AddChild(NewGameButton);
        ContinueGameButton = FrontButton("Continue saved game", () => LoadSavedGame(AutosavePath));
        ContinueGameButton.Disabled = !File.Exists(AutosavePath);
        ContinueGameButton.Visible = !ContinueGameButton.Disabled;
        ContinueGameButton.ThemeTypeVariation = ""; frontChoices.AddChild(ContinueGameButton);
        ContinueGameButton.TooltipText = ContinueGameButton.Disabled ? "A saved deployment will appear here." : "Return to your saved deployment.";
        RefreshDeveloperAccess(); NewGameButton.GrabFocus();
    }

    private void ShowNewGames()
    {
        DeskUi.Clear(frontChoices);
        frontMessage.Text = "Choose a deployment. Your preparations come next.";
        SpokeBGameButton = FrontButton("Spoke B", () => BeginNewGame("spoke-b"));
        SpokeBGameButton.Disabled = !scenarios.Any(s => s.Name == "spoke-b"); frontChoices.AddChild(SpokeBGameButton);
        TutorialGameButton = FrontButton("Tutorial", () => BeginNewGame("tutorial"));
        TutorialGameButton.Disabled = !scenarios.Any(s => s.Name == "tutorial");
        TutorialGameButton.TooltipText = TutorialGameButton.Disabled ? "The training deployment is not available yet." : "Learn alongside the outgoing contracting officer.";
        frontChoices.AddChild(TutorialGameButton);
        var back = FrontButton("Back", ShowFrontDoor); back.ThemeTypeVariation = "";
        frontChoices.AddChild(back); SpokeBGameButton.GrabFocus();
    }

    private void BeginNewGame(string folder)
    {
        var index = scenarios.Select(s => s.Name).ToList().IndexOf(folder);
        if (index < 0) return;
        GetNode<OptionButton>("%ScenarioPicker").Select(index); LoadScenario(index);
        if (session.Prep is null) { frontMessage.Text = "We couldn't open this deployment. Please choose another."; return; }
        GetNode<TabContainer>("%DeskTabs").CurrentTab = TodayTab;
        ShowGameSurface();
    }

    private void ShowGameSurface()
    {
        GetNode<Control>("Navigation").OffsetRight = -20;
        frontDoor.Hide(); GetNode<Control>("Margin").Show(); menuButton.Show(); RefreshDeveloperAccess();
        RefreshOpeningVisibility();
        if (session.Prep is not null) StartGameButton.GrabFocus();
        else if (!GetNode<Button>("%NextDay").Disabled) GetNode<Button>("%NextDay").GrabFocus();
    }

    private void RefreshDeveloperAccess()
    {
        var board = GetNode<VBoxContainer>("Margin/Layout/WorkArea/BoardPanel/BoardLayout");
        foreach (var name in new[] { "ScenarioRow", "ReplayRow", "SaveRow" }) board.GetNode<Control>(name).Visible = DeveloperTools;
        GetNode<Button>("%Continue").Visible = AtFrontDoor && session.View is { Finished: false } && !ReadingTurnover;
        DeveloperWorkspaceButton.Visible = DeveloperTools && AtFrontDoor;
        ArtStudiesButton.Visible = DeveloperTools && AtFrontDoor && OS.HasFeature("editor");
        if (!DeveloperTools) SaveFilePicker.Hide();
        RefreshBuildStatus();
    }
}
