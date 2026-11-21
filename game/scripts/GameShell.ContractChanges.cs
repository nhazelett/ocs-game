using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private bool AddContractModification(VBoxContainer parent, SessionView view, string id)
    {
        if (view.ModifyOptions(id) is not { } change) return false;
        foreach (var added in change.Added)
        {
            var line = DeskUi.Label(added); line.SetMeta("contract_added", id); parent.AddChild(line);
        }
        var row = new VBoxContainer(); parent.AddChild(row);
        if (change.Parts.Count == 0)
        {
            var button = DeskUi.Button(change.Button, () => { });
            var reason = DeskUi.ActionReason(row, button);
            DeskUi.Gate(button, reason, view.Finished ? DeskUi.FinishedReason : change.Why);
            button.SetMeta("contract_modify", id);
            return false;
        }
        var action = new ModifyAction { Day = view.Day, Arrangement = id, Lines = change.Parts.ToList() };
        var assigned = AddAssignedAction(row, row, view, action, change.Button, "",
            stillCurrent: () => ContractView.Visible && SelectedArrangement == id && !EndContractDialog.Visible,
            costText: (chosen, _) =>
            {
                var option = view.ModifyOptions(id, chosen.Assignee)!;
                if (option.Says is null) return "";
                var person = view.Team.Single(m => chosen.Assignee is null ? m.Role == "you" : m.Id == chosen.Assignee);
                var name = person.Role == "you" ? "You" : string.IsNullOrEmpty(person.Rank) ? person.Name : person.Rank + " " + person.Name;
                return name + ": " + option.Says;
            });
        assigned.Button.SetMeta("contract_modify", id);
        return true;
    }
}
