using Godot;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer prepPanel = null!;
    private Label prepCheckLabel = null!;
    private readonly Dictionary<string, Label> prepChoiceReasons = new(StringComparer.Ordinal);
    internal ScrollContainer PrepScroll { get; private set; } = null!;
    internal Dictionary<string, CheckBox> PrepChoices { get; } = new(StringComparer.Ordinal);
    internal Button StartGameButton { get; private set; } = null!;
    private IEnumerable<string> ChosenPrep => PrepChoices.Where(p => p.Value.ButtonPressed).Select(p => p.Key);

    private void BuildPrepDesk()
    {
        var pages = BuildOpeningSurface();
        prepPanel = new VBoxContainer { Name = "PrepScreen" };
        pages.AddChild(prepPanel);
        prepPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        // Hidden rebuilds can measure wrapping before the opening page has its final width.
        void Refit()
        {
            if (!GodotObject.IsInstanceValid(prepPanel) || !prepPanel.IsInsideTree() || !prepPanel.IsVisibleInTree()) return;
            prepPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            prepPanel.Size = pages.Size;
        }
        void QueueRefit() => Callable.From(Refit).CallDeferred();
        prepPanel.VisibilityChanged += QueueRefit;
        prepPanel.MinimumSizeChanged += QueueRefit;
        pages.Resized += QueueRefit;
        prepPanel.AddThemeConstantOverride("separation", 10);
        BuildTurnoverPage(pages);
    }

    private void ShowPrepChoices()
    {
        DeskUi.Clear(prepPanel); PrepChoices.Clear(); prepChoiceReasons.Clear();
        if (session.Prep is not { } prep) return;
        var sender=new HBoxContainer();prepPanel.AddChild(sender);
        if(prep.From is not null){var portrait=new PersonPortrait{SizeFlagsVertical=SizeFlags.ShrinkBegin};portrait.SetPerson(prep.From);sender.AddChild(portrait);}
        var name=DeskUi.Label(prep.From?.WithUnit ?? "Before you leave");name.SizeFlagsHorizontal=SizeFlags.ExpandFill;name.SizeFlagsVertical=SizeFlags.ShrinkCenter;sender.AddChild(name);
        if (prep.Where is { Length: > 0 } where)
        {
            var place = DeskUi.Label(where); place.SetMeta("prep_where", true); prepPanel.AddChild(place);
        }
        var intro = DeskUi.Label(prep.Intro ?? $"You have {prep.BudgetDays} days before you leave. Choose what you want to put in place before you go.");
        intro.AddThemeFontSizeOverride("font_size", 22); prepPanel.AddChild(intro);
        prepCheckLabel = DeskUi.Label();
        StartGameButton = DeskUi.Button("I'm ready to leave", StartPreparedGame);
        StartGameButton.ThemeTypeVariation="TabletopPrimary";
        // The holder bounds the scrolling body independently of its children's minimum sizes.
        // Choice wrapping and refusal lines must never push the departure controls off screen.
        var holder = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        prepPanel.AddChild(holder);
        var choices = DeskUi.Panel(holder, "PreparationChoices");
        PrepScroll = (ScrollContainer)choices.GetParent();
        choices.AddThemeConstantOverride("separation", 12);
        foreach (var choice in prep.Offered)
        {
            var toggle = new CheckBox { Text = $"{choice.Says ?? choice.Label} ({choice.PrepDays} {(choice.PrepDays == 1 ? "day" : "days")})", ButtonPressed = choice.Default,
                AutowrapMode = TextServer.AutowrapMode.WordSmart, ThemeTypeVariation = "TabletopPrepChoice",
                SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var row = new VBoxContainer(); row.AddThemeConstantOverride("separation", 3);
            choices.AddChild(row);
            PrepChoices.Add(choice.Id, toggle); row.AddChild(toggle);
            var changes = DeskUi.Label(choice.Changes ?? ""); changes.SetMeta("prep_changes", choice.Id); row.AddChild(changes);
            var reason = DeskUi.Label(); reason.SetMeta("prep_reason", choice.Id); reason.Hide();
            reason.AddThemeFontSizeOverride("font_size", 18);
            reason.AddThemeColorOverride("font_color", TabletopTheme.Muted);
            row.AddChild(reason); prepChoiceReasons.Add(choice.Id, reason);
            toggle.Toggled += _ => UpdatePrepCheck();
        }
        prepPanel.AddChild(prepCheckLabel);
        AddTeamPreparation(prepPanel);
        var buttons = new HBoxContainer(); prepPanel.AddChild(buttons);
        buttons.AddChild(DeskUi.Button("Back", () => { ShowFrontDoor(); ShowNewGames(); })); buttons.AddChild(StartGameButton);
        StartGameButton.CustomMinimumSize = new Vector2(220, 46); StartGameButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        UpdatePrepCheck();
    }

    private void UpdatePrepCheck()
    {
        var check = session.CheckPrep(ChosenPrep);
        prepCheckLabel.Text = $"You've planned {check.UsedDays} of your {check.BudgetDays} days." + (check.Accepted ? " You can leave days unused." : "\n" + string.Join("\n", check.Stops));
        prepCheckLabel.AddThemeColorOverride("font_color", check.Accepted ? TabletopTheme.Ink : TabletopTheme.Alert);
        StartGameButton.Disabled = !check.Accepted;
        if (session.Prep is not { } prep) return;
        var chosen = ChosenPrep.ToArray();
        foreach (var choice in prep.Offered)
        {
            var toggle = PrepChoices[choice.Id];
            toggle.Disabled = !toggle.ButtonPressed && !session.CheckPrep(chosen.Append(choice.Id)).Accepted;
            var reason = prepChoiceReasons[choice.Id];
            reason.Text = toggle.Disabled
                ? $"{choice.PrepDays} {(choice.PrepDays == 1 ? "day" : "days")}; you have {Math.Max(0, check.BudgetDays - check.UsedDays)} left."
                : "";
            reason.Visible = toggle.Disabled;
        }
    }

    private void StartPreparedGame()
    {
        if (session.Prep is null) return;
        UpdatePrepCheck();
        if (StartGameButton.Disabled) return;
        session.StartScenario(ChosenPrep, teamPicks.Values.ToArray());
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
            tabs.CurrentTab = TodayTab;
            GetNode<Label>("%DayCounter").Text = "Pre-deployment";
        }
        RefreshOpeningVisibility();
    }
}
