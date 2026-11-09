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
        prepPanel = new VBoxContainer { Name = "PrepScreen" };
        GetNode<TabContainer>("%DeskTabs").GetNode<Control>("Board").AddChild(prepPanel);
        prepPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        prepPanel.AddThemeConstantOverride("separation", 10);
    }

    private void ShowPrepChoices()
    {
        DeskUi.Clear(prepPanel); PrepChoices.Clear();
        if (session.Prep is not { } prep) return;
        prepPanel.AddChild(DeskUi.Label($"Pre-deployment · {prep.ScenarioTitle}\nChoose preparations within {prep.BudgetDays} prep days. All choices together cost {prep.AllChoicesDays} days. You may deploy without spending them all."));
        prepCheckLabel = DeskUi.Label(); prepPanel.AddChild(prepCheckLabel);
        StartGameButton = DeskUi.Button("Deploy · start day 1", StartPreparedGame); prepPanel.AddChild(StartGameButton);
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        choices.AddThemeConstantOverride("separation", 12);
        prepPanel.AddChild(scroll); scroll.AddChild(choices);
        foreach (var choice in prep.Options)
        {
            var toggle = new CheckBox { Text = $"{choice.Label} · {choice.PrepDays} prep days", AutowrapMode = TextServer.AutowrapMode.WordSmart };
            PrepChoices.Add(choice.Id, toggle); choices.AddChild(toggle);
            foreach (var effect in choice.Effects)
                choices.AddChild(DeskUi.Label($"With: {effect.With}\nWithout: {effect.Without}" + (effect.HasEffect ? "" : "\nRecorded only; not modeled in this build.")));
            toggle.Toggled += _ => UpdatePrepCheck();
        }
        UpdatePrepCheck();
    }

    private void UpdatePrepCheck()
    {
        var check = session.CheckPrep(ChosenPrep);
        prepCheckLabel.Text = $"{check.UsedDays} / {check.BudgetDays} prep days selected" + (check.Accepted ? "" : "\n" + string.Join("\n", check.Stops));
        prepCheckLabel.Modulate = check.Accepted ? Colors.White : new Color(1, .45f, .4f);
        StartGameButton.Disabled = !check.Accepted;
    }

    private void StartPreparedGame()
    {
        if (session.Prep is null) return;
        UpdatePrepCheck();
        if (StartGameButton.Disabled) return;
        session.StartScenario(ChosenPrep);
        TryAutosave();
        RefreshDisplay(DeveloperTools ? "Seed 1 · preparations applied. Next day plays day 1." : "Your preparations are in place. You're ready for your first day.");
    }

    private void RefreshPrepVisibility()
    {
        var preparing = session.Prep is not null;
        var tabs = GetNode<TabContainer>("%DeskTabs");
        prepPanel.Visible = preparing;
        tabs.GetNode<Control>("Board/MorningBoard").Visible = !preparing;
        for (var i = 1; i < tabs.GetTabCount(); i++) tabs.SetTabDisabled(i, preparing);
        if (preparing)
        {
            tabs.CurrentTab = 0;
            GetNode<Label>("%DayCounter").Text = "Pre-deployment";
        }
    }
}
