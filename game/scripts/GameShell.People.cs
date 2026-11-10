using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer customerMessages = null!;
    internal Dictionary<string, Button> RequestOpenButtons { get; } = new(StringComparer.Ordinal);
    internal VBoxContainer CustomerMessages => customerMessages;

    private void BuildPeopleDesk()
    {
        customerMessages = new VBoxContainer { Name = "CustomerMessages" };
        customerMessages.AddThemeConstantOverride("separation", 16);
        var inbox = GetNode<TabContainer>("%DeskTabs").GetNode<VBoxContainer>("Inbox/Contents/Items");
        inbox.AddChild(customerMessages); inbox.MoveChild(customerMessages, 0);
    }

    private void RefreshPeople(SessionView? view)
    {
        DeskUi.Clear(customerMessages); RequestOpenButtons.Clear();
        if (view is null) return;
        var requests = view.Cards.Select(c => view.RequirementEditor(c.Id)).OfType<RequirementEditorView>()
            .OrderByDescending(r => r.SubmittedDay).ThenBy(r => r.CardId, StringComparer.Ordinal);
        foreach (var request in requests)
        {
            var card = new PanelContainer();
            using var cardStyle = new StyleBoxFlat { BgColor = TabletopTheme.Paper, ContentMarginLeft = 14,
                ContentMarginRight = 14, ContentMarginTop = 14, ContentMarginBottom = 14, CornerRadiusTopLeft = 5,
                CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5 };
            card.AddThemeStyleboxOverride("panel", cardStyle);
            customerMessages.AddChild(card);
            var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 14); card.AddChild(row);
            var portrait = new PersonPortrait { TooltipText = request.Person?.DisplayName ?? "Customer", SizeFlagsVertical = SizeFlags.ShrinkBegin };
            portrait.SetPerson(request.Person);
            row.AddChild(portrait);
            var words = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; words.AddThemeConstantOverride("separation", 10); row.AddChild(words);
            words.AddChild(DeskUi.Label(request.Person?.WithUnit ?? request.SubmittedBy));
            words.AddChild(DeskUi.Label(request.Message ?? "A request is waiting for you."));
            var when = DeskUi.Label($"Arrived on day {request.SubmittedDay}."); when.SelfModulate = Colors.White; words.AddChild(when);
            var open = DeskUi.Button("Open", () => NavigateBoardItem(new BoardItem("", request.SubmittedDay, BoardCategory.Request,
                BoardSeverity.Info, BoardLinkKind.Card, request.CardId, "", [], request.Person)));
            open.TooltipText = request.Title.Length > 0 ? request.Title : "Open the requirement";
            open.CustomMinimumSize = new Vector2(0, 38); words.AddChild(open); RequestOpenButtons.Add(request.CardId, open);
        }
    }
}
