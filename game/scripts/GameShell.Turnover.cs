using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private PanelContainer openingSurface = null!;
    private VBoxContainer turnoverPage = null!;
    private Label turnoverSpeaker = null!, turnoverWords = null!;
    private ScrollContainer turnoverScroll = null!;
    private int turnoverIndex;
    private PersonPortrait turnoverPortrait=null!;
    internal TurnoverView? DisplayedTurnover { get; private set; }
    internal TurnoverSectionKind? CurrentTurnoverSection => DisplayedTurnover?.Sections[turnoverIndex].Kind;
    internal Button TurnoverNext { get; private set; } = null!;
    internal Button TurnoverBack { get; private set; } = null!;
    internal Label TurnoverWords => turnoverWords;
    internal bool ReadingTurnover => DisplayedTurnover is not null;

    private Control BuildOpeningSurface()
    {
        openingSurface = new PanelContainer { Name = "Opening", ZIndex = 10 };
        using var panelStyle = new StyleBoxFlat { BgColor = TabletopTheme.Background,
            ContentMarginLeft = 70, ContentMarginRight = 70, ContentMarginTop = 70, ContentMarginBottom = 50 };
        openingSurface.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(openingSurface); openingSurface.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var pages = new Control(); openingSurface.AddChild(pages); return pages;
    }

    private void BuildTurnoverPage(Control pages)
    {
        turnoverPage = new VBoxContainer { Name = "Turnover", Visible = false };
        turnoverPage.AddThemeConstantOverride("separation", 24); pages.AddChild(turnoverPage);
        turnoverPage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var sender = new HBoxContainer(); sender.AddThemeConstantOverride("separation", 20); turnoverPage.AddChild(sender);
        turnoverPortrait=new PersonPortrait{SizeFlagsVertical=SizeFlags.ShrinkBegin};sender.AddChild(turnoverPortrait);
        turnoverSpeaker = DeskUi.Label(); turnoverSpeaker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        turnoverSpeaker.SizeFlagsVertical = SizeFlags.ShrinkCenter; turnoverSpeaker.AddThemeFontSizeOverride("font_size", 22);
        sender.AddChild(turnoverSpeaker);
        turnoverScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        turnoverPage.AddChild(turnoverScroll);
        turnoverWords = DeskUi.Label(); turnoverWords.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        turnoverWords.AddThemeFontSizeOverride("font_size", 23); turnoverScroll.AddChild(turnoverWords);
        var buttons = new HBoxContainer(); buttons.AddThemeConstantOverride("separation", 18); turnoverPage.AddChild(buttons);
        TurnoverBack = DeskUi.Button("Back", () => { if (turnoverIndex > 0) { turnoverIndex--; RenderTurnover(); } }); buttons.AddChild(TurnoverBack);
        TurnoverNext = DeskUi.Button("Continue", () =>
        {
            if (DisplayedTurnover is null) return;
            if (++turnoverIndex < DisplayedTurnover.Sections.Count) RenderTurnover();
            else FinishTurnover();
        });
        TurnoverNext.CustomMinimumSize = new Vector2(240, 48); buttons.AddChild(TurnoverNext);
    }

    private void BeginTurnover()
    {
        DisplayedTurnover = session.View!.Turnover(); turnoverIndex = 0; RenderTurnover();
    }

    private void RenderTurnover()
    {
        if (DisplayedTurnover is not { } handover) return;
        turnoverPortrait.SetPerson(handover.From);
        turnoverSpeaker.Text = handover.From?.WithUnit ?? "The outgoing contracting officer";
        var paragraphs = new List<string?>();
        if (turnoverIndex == 0)
        {
            paragraphs.Add(handover.Greeting); paragraphs.Add(handover.Briefing?.Situation); paragraphs.Add(handover.Briefing?.Due);
        }
        paragraphs.Add(handover.Sections[turnoverIndex].Text);
        if (turnoverIndex == handover.Sections.Count - 1) paragraphs.Add(handover.LastLine);
        turnoverWords.Text = string.Join("\n\n", paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)));
        TurnoverBack.Disabled = turnoverIndex == 0;
        TurnoverNext.Text = turnoverIndex == handover.Sections.Count - 1 ? "Let's meet them" : "Continue";
        turnoverScroll.ScrollVertical = 0; TurnoverNext.GrabFocus();
    }

    private void FinishTurnover()
    {
        DisplayedTurnover = null; turnoverIndex = 0; RefreshOpeningVisibility();
        GetNode<TabContainer>("%DeskTabs").CurrentTab = InboxTab;
        var first = session.View!.Cards.FirstOrDefault(c => RequestOpenButtons.ContainsKey(c.Id));
        if (first is not null)
        {
            var button = RequestOpenButtons[first.Id];
            GetNode<TabContainer>("%DeskTabs").GetNode<ScrollContainer>("Inbox/Contents").EnsureControlVisible(button);
            button.GrabFocus();
        }
    }

    private void ResetOpening()
    {
        DisplayedTurnover = null; turnoverIndex = 0;
    }

    private void RefreshOpeningVisibility()
    {
        var preparing = session.Prep is not null;
        var visible = !AtFrontDoor && (preparing || ReadingTurnover);
        openingSurface.Visible = visible;
        prepPanel.Visible = preparing; turnoverPage.Visible = ReadingTurnover;
        GetNode<Control>("Margin").Visible = !AtFrontDoor && !visible;
        menuButton.Visible = !AtFrontDoor && !visible;
    }
}
