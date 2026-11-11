using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer prepPanel = null!;
    private Label prepCheckLabel = null!;
    internal Dictionary<string, CheckBox> PrepChoices { get; } = new(StringComparer.Ordinal);
    internal Button StartGameButton { get; private set; } = null!;
    private IEnumerable<string> ChosenPrep => PrepChoices.Where(p => p.Value.ButtonPressed).Select(p => p.Key);

    private void BuildPrepDesk()
    {
        var pages = BuildOpeningSurface();
        prepPanel = new VBoxContainer { Name = "PrepScreen" };
        pages.AddChild(prepPanel);
        prepPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        prepPanel.AddThemeConstantOverride("separation", 10);
        BuildTurnoverPage(pages);
    }

    private void ShowPrepChoices()
    {
        DeskUi.Clear(prepPanel); PrepChoices.Clear();
        if (session.Prep is not { } prep) return;
        var sender=new HBoxContainer();prepPanel.AddChild(sender);
        if(prep.From is not null){var portrait=new PersonPortrait{SizeFlagsVertical=SizeFlags.ShrinkBegin};portrait.SetPerson(prep.From);sender.AddChild(portrait);}
        var name=DeskUi.Label(prep.From?.WithUnit ?? "Before you leave");name.SizeFlagsHorizontal=SizeFlags.ExpandFill;name.SizeFlagsVertical=SizeFlags.ShrinkCenter;sender.AddChild(name);
        var intro = DeskUi.Label(prep.Intro ?? $"You have {prep.BudgetDays} days before you leave. Choose what you want to put in place before you go.");
        intro.AddThemeFontSizeOverride("font_size", 22); prepPanel.AddChild(intro);
        prepCheckLabel = DeskUi.Label(); prepPanel.AddChild(prepCheckLabel);
        StartGameButton = DeskUi.Button("I'm ready to leave", StartPreparedGame);
        StartGameButton.ThemeTypeVariation="TabletopPrimary";
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        choices.AddThemeConstantOverride("separation", 12);
        prepPanel.AddChild(scroll); scroll.AddChild(choices);
        foreach (var choice in prep.Options)
        {
            var toggle = new CheckBox { Text = $"{choice.Says ?? choice.Label} ({choice.PrepDays} {(choice.PrepDays == 1 ? "day" : "days")})", ButtonPressed = choice.Default,
                AutowrapMode = TextServer.AutowrapMode.WordSmart };
            PrepChoices.Add(choice.Id, toggle); choices.AddChild(toggle);
            toggle.TooltipText = string.Join("\n\n", choice.Effects.Select(effect => effect.With + " " + effect.Without +
                (effect.HasEffect ? "" : " This preparation is recorded in your debrief; it has no modeled effect yet.")));
            toggle.Toggled += _ => UpdatePrepCheck();
        }
        var buttons = new HBoxContainer(); prepPanel.AddChild(buttons);
        buttons.AddChild(DeskUi.Button("Back", ShowFrontDoor)); buttons.AddChild(StartGameButton);
        StartGameButton.CustomMinimumSize = new Vector2(220, 46); StartGameButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        UpdatePrepCheck();
    }

    private void UpdatePrepCheck()
    {
        var check = session.CheckPrep(ChosenPrep);
        prepCheckLabel.Text = $"You've planned {check.UsedDays} of your {check.BudgetDays} days." + (check.Accepted ? " You can leave days unused." : "\n" + string.Join("\n", check.Stops));
        prepCheckLabel.AddThemeColorOverride("font_color", check.Accepted ? TabletopTheme.Ink : TabletopTheme.Alert);
        StartGameButton.Disabled = !check.Accepted;
    }

    private void StartPreparedGame()
    {
        if (session.Prep is null) return;
        UpdatePrepCheck();
        if (StartGameButton.Disabled) return;
        session.StartScenario(ChosenPrep);
        TryAutosave();
        BeginTurnover();
        RefreshDisplay(DeveloperTools ? "Seed 1 · preparations applied. End the day plays day 1." : "Your preparations are in place. You're ready for your first day.", developerOnly: DeveloperTools);
    }

    private void RefreshPrepVisibility()
    {
        var preparing = session.Prep is not null;
        var tabs = GetNode<TabContainer>("%DeskTabs");
        prepPanel.Visible = preparing;
        for (var i = 1; i < tabs.GetTabCount(); i++) tabs.SetTabDisabled(i, preparing);
        if (preparing)
        {
            tabs.CurrentTab = 0;
            GetNode<Label>("%DayCounter").Text = "Pre-deployment";
        }
        RefreshOpeningVisibility();
    }
}
