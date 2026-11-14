using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private const int RecentDecisionCount = 3;
    private VBoxContainer decisionsPanel = null!;
    private bool allDecisions;
    internal VBoxContainer DecisionRows { get; private set; } = null!;
    internal Button AllDecisionsButton { get; private set; } = null!;
    internal VBoxContainer? RequestDecisionRows { get; private set; }

    private void BuildDecisions(VBoxContainer board)
    {
        decisionsPanel = new VBoxContainer { Name = "Decisions" };
        decisionsPanel.AddThemeConstantOverride("separation", 10); board.AddChild(decisionsPanel);
        decisionsPanel.AddChild(DeskUi.Heading("Your decisions"));
        DecisionRows = new VBoxContainer(); DecisionRows.AddThemeConstantOverride("separation", 10);
        decisionsPanel.AddChild(DecisionRows);
        AllDecisionsButton = DeskUi.Button("All decisions", () =>
        {
            if (AtFrontDoor || ReadingTurnover || PlayingDay || ReadingDaySummary || session.View is null) return;
            allDecisions = !allDecisions; RefreshDecisions(session.View);
        });
        decisionsPanel.AddChild(AllDecisionsButton);
    }

    private void RefreshDecisions(SessionView? view)
    {
        var decisions = view?.Decisions() ?? [];
        DeskUi.Clear(DecisionRows);
        RenderDecisionRows(DecisionRows, allDecisions ? decisions : decisions.Take(RecentDecisionCount), true);
        AllDecisionsButton.Visible = decisions.Count > RecentDecisionCount;
        AllDecisionsButton.Text = allDecisions ? "Recent decisions" : "All decisions";
        decisionsPanel.Visible = view is not null;
    }

    private void RenderRequestDecisions(SessionView view, string card)
    {
        linkedRequirement.AddChild(DeskUi.Heading("Your decisions for this request"));
        RequestDecisionRows = new VBoxContainer { Name = "RequestDecisions" };
        RequestDecisionRows.AddThemeConstantOverride("separation", 10);
        linkedRequirement.AddChild(RequestDecisionRows);
        RenderDecisionRows(RequestDecisionRows, view.Decisions(card), false);
    }

    private void RenderDecisionRows(VBoxContainer parent, IEnumerable<DecisionView> decisions, bool openRequest)
    {
        var hash = session.StateHash;
        var any = false;
        foreach (var decision in decisions)
        {
            any = true;
            var row = new VBoxContainer(); row.SetMeta("decision_sentence", decision.Sentence); parent.AddChild(row);
            var now = decision.Now + (decision.Sentence.Contains(decision.Teammate, StringComparison.OrdinalIgnoreCase)
                ? "" : " With " + decision.Teammate + ".");
            if (openRequest && decision.CardId is { } card)
            {
                var button = DeskUi.Button(decision.Sentence + "\n" + now, () =>
                {
                    if (hash != session.StateHash || AtFrontDoor || ReadingTurnover || PlayingDay || ReadingDaySummary) return;
                    NavigateBoardItem(new BoardItem("", session.View!.Day, BoardCategory.Request, BoardSeverity.Info,
                        BoardLinkKind.Card, card, "", []));
                });
                button.TooltipText = "Open " + decision.Request;
                button.SetMeta("decision_card", card); row.AddChild(button);
            }
            else
            {
                // Office-wide decisions have no request destination; they remain readable entries.
                row.AddChild(DeskUi.Label(decision.Sentence)); row.AddChild(DeskUi.Label(now));
            }
            row.SetMeta("decision_now", decision.Now);
            row.SetMeta("decision_teammate", decision.Teammate);
        }
        if (!any) parent.AddChild(DeskUi.Label("No decisions committed yet."));
    }
}
