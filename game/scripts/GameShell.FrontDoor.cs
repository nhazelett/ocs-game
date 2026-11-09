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
    internal Button NewGameButton { get; private set; } = null!;
    internal Button ContinueGameButton { get; private set; } = null!;
    internal Button SpokeBGameButton { get; private set; } = null!;
    internal Button TutorialGameButton { get; private set; } = null!;
    internal bool AtFrontDoor => frontDoor.Visible;
    private bool DeveloperTools => DeveloperToggle?.ButtonPressed == true;

    private void BuildFrontDoor()
    {
        frontDoor = new PanelContainer { Name = "FrontDoor" };
        var background = new StyleBoxFlat { BgColor = new Color("111f29") };
        frontDoor.AddThemeStyleboxOverride("panel", background);
        AddChild(frontDoor); frontDoor.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center = new CenterContainer(); frontDoor.AddChild(center);
        var margin = new MarginContainer(); center.AddChild(margin);
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 30);
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(500, 0) }; column.AddThemeConstantOverride("separation", 22); margin.AddChild(column);
        var imprint = new Label { Text = "KTHQ", Modulate = new Color("9ab8be") }; imprint.AddThemeFontSizeOverride("font_size", 18); column.AddChild(imprint);
        var title = new Label { Text = "OCS\nLines of Support" }; title.AddThemeFontSizeOverride("font_size", 46); column.AddChild(title);
        var accent = new ColorRect { Color = new Color("e2b475"), CustomMinimumSize = new Vector2(0, 3), MouseFilter = MouseFilterEnum.Ignore }; column.AddChild(accent);
        frontMessage = DeskUi.Label("Take over the contracting shop. Keep the deployment supported as the days unfold.");
        frontMessage.CustomMinimumSize = new Vector2(500, 0); column.AddChild(frontMessage);
        frontChoices = new VBoxContainer(); frontChoices.AddThemeConstantOverride("separation", 12); column.AddChild(frontChoices);
        DeveloperWorkspaceButton = FrontButton("Open developer workspace", ShowGameSurface); column.AddChild(DeveloperWorkspaceButton);
        var chrome = new HBoxContainer { Name = "Navigation", ZIndex = 20 };
        AddChild(chrome); chrome.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        chrome.OffsetLeft = -310; chrome.OffsetRight = -20; chrome.OffsetTop = 16; chrome.OffsetBottom = 48;
        menuButton = DeskUi.Button("Menu", ShowFrontDoor); menuButton.Name = "Menu"; chrome.AddChild(menuButton);
        DeveloperToggle = new CheckBox { Text = "Developer tools" }; chrome.AddChild(DeveloperToggle);
        DeveloperToggle.Toggled += _ => RefreshDeveloperAccess();
        ShowFrontDoor();
    }

    private static Button FrontButton(string text, Action pressed)
    {
        var button = DeskUi.Button(text, pressed); button.CustomMinimumSize = new Vector2(0, 50);
        button.AddThemeFontSizeOverride("font_size", 22);
        var normal = new StyleBoxFlat { BgColor = new Color("243d4b"), ContentMarginLeft = 20, ContentMarginRight = 20,
            ContentMarginTop = 10, ContentMarginBottom = 10, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        button.AddThemeStyleboxOverride("normal", normal);
        var hover = (StyleBoxFlat)normal.Duplicate(); hover.BgColor = new Color("345768"); button.AddThemeStyleboxOverride("hover", hover);
        var focus = (StyleBoxFlat)normal.Duplicate(); focus.BorderColor = new Color("e2b475"); focus.SetBorderWidthAll(2); button.AddThemeStyleboxOverride("focus", focus);
        return button;
    }

    private void ShowFrontDoor()
    {
        frontDoor.Show(); GetNode<Control>("Margin").Hide(); menuButton?.Hide();
        frontMessage.Text = "Take over the contracting shop. Keep the deployment supported as the days unfold.";
        DeskUi.Clear(frontChoices);
        NewGameButton = FrontButton("New game", ShowNewGames); frontChoices.AddChild(NewGameButton);
        ContinueGameButton = FrontButton("Continue", () => LoadSavedGame(AutosavePath));
        ContinueGameButton.Disabled = !File.Exists(AutosavePath); frontChoices.AddChild(ContinueGameButton);
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
        frontChoices.AddChild(FrontButton("Back", ShowFrontDoor)); SpokeBGameButton.GrabFocus();
    }

    private void BeginNewGame(string folder)
    {
        var index = scenarios.Select(s => s.Name).ToList().IndexOf(folder);
        if (index < 0) return;
        GetNode<OptionButton>("%ScenarioPicker").Select(index); LoadScenario(index);
        if (session.Prep is null) { frontMessage.Text = "We couldn't open this deployment. Please choose another."; return; }
        GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
        ShowGameSurface();
    }

    private void ShowGameSurface()
    {
        frontDoor.Hide(); GetNode<Control>("Margin").Show(); menuButton.Show(); RefreshDeveloperAccess();
        if (session.Prep is not null) StartGameButton.GrabFocus();
        else if (!GetNode<Button>("%NextDay").Disabled) GetNode<Button>("%NextDay").GrabFocus();
    }

    private void RefreshDeveloperAccess()
    {
        var board = GetNode<VBoxContainer>("Margin/Layout/WorkArea/BoardPanel/BoardLayout");
        foreach (var name in new[] { "ScenarioRow", "ReplayRow", "SaveRow" }) board.GetNode<Control>(name).Visible = DeveloperTools;
        DeveloperWorkspaceButton.Visible = DeveloperTools && AtFrontDoor;
        if (!DeveloperTools) SaveFilePicker.Hide();
    }
}
