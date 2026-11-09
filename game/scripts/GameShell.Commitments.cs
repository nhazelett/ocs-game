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
        var wasReplay = session.IsReplay;
        var result = session.Apply(action);
        RefreshDisplay(result.Message + (wasReplay && result.Accepted ? " You are now in control; future replay actions are stopped." : ""));
        return result;
    }

    private void RefreshCommitments(SessionView? view)
    {
        DeskUi.Clear(commitmentRows); commitmentChoices.Clear();
        var all = view?.Commitments.OrderByDescending(c => c.Unresolved).ThenBy(c => c.OpenedDay).ThenBy(c => c.Id, StringComparer.Ordinal).ToArray() ?? [];
        commitmentRows.AddChild(DeskUi.Label($"Decisions · {all.Count(c => c.Unresolved)} unresolved"));
        if (session.IsReplay) commitmentRows.AddChild(DeskUi.Label("Making a decision takes control and stops future replay actions."));
        foreach (var commitment in all)
        {
            var row = new VBoxContainer(); commitmentRows.AddChild(row);
            var site = view!.Scenario.Sites.Single(s => s.Id == commitment.SiteId).Name;
            var kind = commitment.Kind == CommitmentKind.Handshake ? "Handshake" : "Out-of-scope work";
            row.AddChild(DeskUi.Label($"{kind} · {view.NameOf(commitment.Id)}\nDecision: {commitment.Decision}" + (commitment.DecidedDay is { } day ? $" on day {day}" : "")));
            var record = view.Records.FirstOrDefault(r => r.Id == commitment.CauseId && view.Known(r));
            if (record is not null) row.AddChild(DeskUi.Label(record.Text));
            if (commitment.Kind == CommitmentKind.OutOfScope) row.AddChild(DeskUi.Label($"Invoice: {DeskUi.Money(commitment.AmountCents)}"));
            if (!commitment.Unresolved) continue;
            var choices = commitment.Kind == CommitmentKind.Handshake ? new[] { "ratify", "park", "refuse" } : ["deny", "ratify"];
            var buttons = new HBoxContainer(); row.AddChild(buttons);
            foreach (var choice in choices)
            {
                var button = DeskUi.Button(char.ToUpperInvariant(choice[0]) + choice[1..], () =>
                    SendAction(new ResolveCommitmentAction { Day = session.View!.Day, Commitment = commitment.Id, Choice = choice }));
                button.Disabled = view.Finished;
                buttons.AddChild(button); commitmentChoices.Add((commitment.Id, choice), button);
            }
        }
    }
}
