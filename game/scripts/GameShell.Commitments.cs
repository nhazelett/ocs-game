using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer commitmentRows = null!;
    private readonly Dictionary<(string Id, string Choice), Button> commitmentChoices = [];
    internal IReadOnlyDictionary<(string Id, string Choice), Button> CommitmentChoices => commitmentChoices;

    private void BuildCommitmentDesk()
    {
        commitmentRows = new VBoxContainer { Name = "Decisions" };
        GetNode<TabContainer>("%DeskTabs").GetNode("Inbox/Contents/Items").AddChild(commitmentRows);
    }

    internal ActionResult SendAction(PlayerAction action)
    {
        if (ReadingDaySummary) return ActionResult.Stop("Close the day’s results before giving new work.");
        var wasReplay = session.IsReplay;
        var result = session.Apply(action);
        var message = result.Accepted && action is DesignateCorAction && session.View?.Records.FirstOrDefault(r => r.Id == result.RecordId) is { } designation
            ? designation.Text
            : result.Accepted && action is (AwardAction or AssignAction or CoordinateAction or StopWorkAction) && session.View?.Decisions().FirstOrDefault() is { } decision
            ? decision.Sentence + " " + decision.Now : result.Message;
        RefreshDisplay(message + (wasReplay && result.Accepted ? " You are now in control; future replay actions are stopped." : ""));
        return result;
    }

    private void RefreshCommitments(SessionView? view)
    {
        DeskUi.Clear(commitmentRows); commitmentChoices.Clear();
        var all = view?.Commitments.OrderByDescending(c => c.Unresolved).ThenBy(c => c.OpenedDay).ThenBy(c => c.Id, StringComparer.Ordinal).ToArray() ?? [];
        commitmentRows.AddChild(DeskUi.Label($"Unauthorized commitments to decide · {all.Count(c => c.Unresolved)} unresolved"));
        if (session.IsReplay) commitmentRows.AddChild(DeskUi.Label("Making a decision takes control and stops future replay actions."));
        foreach (var commitment in all)
        {
            var row = new VBoxContainer(); row.SetMeta("commitment", commitment.Id); commitmentRows.AddChild(row);
            var site = view!.Scenario.Sites.Single(s => s.Id == commitment.SiteId).Name;
            var kind = commitment.Kind == CommitmentKind.Handshake ? "Handshake" : "Out-of-scope work";
            row.AddChild(DeskUi.Label($"{kind} · {view.NameOf(commitment.Id)}\nDecision: {commitment.Decision}" + (commitment.DecidedDay is { } day ? $" on day {day}" : "")));
            var record = view.Records.FirstOrDefault(r => r.Id == commitment.CauseId && view.Known(r));
            if (record is not null) row.AddChild(DeskUi.Label(record.Text));
            if (commitment.Kind == CommitmentKind.OutOfScope) row.AddChild(DeskUi.Label($"Invoice: {DeskUi.Money(commitment.AmountCents)}"));
            if (!commitment.Unresolved) continue;
            var choices = commitment.Kind == CommitmentKind.Handshake ? new[] { "ratify", "park", "refuse" } : ["deny", "ratify"];
            var buttons = new VBoxContainer(); row.AddChild(buttons);
            foreach (var choice in choices)
            {
                var action = new ResolveCommitmentAction { Day = view.Day, Commitment = commitment.Id, Choice = choice };
                var preview = view.PreviewAny(action);
                var button = DeskUi.Button(char.ToUpperInvariant(choice[0]) + choice[1..], () => SendAction(action));
                button.Disabled = view.Finished;
                var choiceBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; buttons.AddChild(choiceBox);
                var reason = DeskUi.ActionReason(choiceBox, button);
                DeskUi.Gate(button, reason, view.Finished ? DeskUi.FinishedReason : preview.HardStops.FirstOrDefault());
                AddActionCost(choiceBox, view, action, choice switch
                {
                    "ratify" => "Puts ratification of this commitment in work.",
                    "deny" => "Denies payment for the out-of-scope work.",
                    "refuse" => "Refuses the handshake and stops its support.",
                    _ => "Leaves the commitment unresolved."
                }, preview);
                commitmentChoices.Add((commitment.Id, choice), button);
            }
        }
    }
}
