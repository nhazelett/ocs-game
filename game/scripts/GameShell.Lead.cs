using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer boardDetails = null!;
    private Label leadSpeaker = null!, leadWords = null!;
    private PersonPortrait leadPortrait=null!;
    internal LeadItem? DisplayedLead { get; private set; }
    internal Button LeadOpen { get; private set; } = null!;
    internal Button BoardDetailsToggle { get; private set; } = null!;
    internal bool BoardDetailsVisible => boardDetails.Visible;
    internal Label LeadWords => leadWords;

    private void BuildLead(VBoxContainer board)
    {
        var card = new PanelContainer();
        using var panelStyle = new StyleBoxFlat { BgColor = TabletopTheme.Paper, ContentMarginLeft = 18,
            ContentMarginRight = 18, ContentMarginTop = 18, ContentMarginBottom = 18, CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5 };
        card.AddThemeStyleboxOverride("panel", panelStyle);
        board.AddChild(card);
        var body = new VBoxContainer(); body.AddThemeConstantOverride("separation", 16); card.AddChild(body);
        var sender=new HBoxContainer();body.AddChild(sender);
        leadPortrait=new PersonPortrait{SizeFlagsVertical=SizeFlags.ShrinkBegin};sender.AddChild(leadPortrait);
        leadSpeaker = DeskUi.Label();leadSpeaker.SizeFlagsHorizontal=SizeFlags.ExpandFill;leadSpeaker.SizeFlagsVertical=SizeFlags.ShrinkCenter;sender.AddChild(leadSpeaker);
        leadWords = DeskUi.Label(); leadWords.ThemeTypeVariation="TabletopHeading"; leadWords.AddThemeFontSizeOverride("font_size", 24); body.AddChild(leadWords);
        LeadOpen = DeskUi.Button("Open", OpenLead); LeadOpen.CustomMinimumSize = new Vector2(0, 44); body.AddChild(LeadOpen);
        LeadOpen.ThemeTypeVariation="TabletopPrimary";
        BoardDetailsToggle = new Button { Text = "Other updates and sites", ToggleMode = true, CustomMinimumSize = new Vector2(0, 42) };
        BoardDetailsToggle.Toggled += open => boardDetails.Visible = open;
        board.AddChild(BoardDetailsToggle);
        boardDetails = new VBoxContainer { Name = "OtherUpdates", Visible = false }; boardDetails.AddThemeConstantOverride("separation", 10); board.AddChild(boardDetails);
    }

    private void RefreshLead(SessionView? view, MorningBoardView? board)
    {
        DisplayedLead = board?.Lead;
        leadPortrait.Visible=DisplayedLead?.Person is not null;if(leadPortrait.Visible)leadPortrait.SetPerson(DisplayedLead!.Person);
        leadSpeaker.Text = DisplayedLead?.Person?.WithUnit ?? ""; leadSpeaker.Visible = leadSpeaker.Text.Length > 0;
        leadWords.Text = DisplayedLead?.Text ?? (view is null ? "Your deployment has not started." : view.Finished ? "The deployment is complete." : "There is no priority message this morning.");
        LeadOpen.Visible = DisplayedLead is not null;
        LeadOpen.Disabled = DisplayedLead is null || DisplayedLead.LinkKind == BoardLinkKind.None;
    }

    private void OpenLead()
    {
        if (DisplayedLead is not { } lead || session.View is not { } view) return;
        // Lead ordering is entirely Sim-owned; these are UI destinations for existing controls.
        if (lead.Kind == LeadKind.Decision && lead.LinkKind == BoardLinkKind.Arrangement)
        {
            var commitment = view.Commitments.FirstOrDefault(c => c.ArrangementId == lead.LinkId && c.Decision == LinesOfSupport.Sim.Core.CommitmentDecision.Open);
            var issue = view.PerformanceIssues.FirstOrDefault(i => i.ArrangementId == lead.LinkId);
            Button? target = null;
            if (commitment is not null) target = commitmentChoices.FirstOrDefault(kv => kv.Key.Id == commitment.Id).Value;
            else if (issue is not null && PerformanceChoices.TryGetValue((issue.Id, "resource"), out var resource)) target = resource;
            if (target is not null)
            {
                GetNode<TabContainer>("%DeskTabs").CurrentTab = 6;
                GetNode<TabContainer>("%DeskTabs").GetNode<ScrollContainer>("Inbox/Contents").EnsureControlVisible(target);
                target.GrabFocus(); return;
            }
        }
        NavigateBoardItem(new BoardItem(lead.RecordId ?? "", view.Day, BoardCategory.Report, BoardSeverity.Info,
            lead.LinkKind, lead.LinkId, lead.Text, [], lead.Person));
    }

    private void FocusNewMorning()
    {
        BoardDetailsToggle.ButtonPressed = false;
        GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
    }

    private static string StatusWords(string status) => status switch
    {
        "ok" => "fully supported", "degraded" => "partly supported", "down" => "support interrupted",
        "notOpen" => "not open yet", _ => "not assessed"
    };
}
