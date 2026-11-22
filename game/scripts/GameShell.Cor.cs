using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal List<Label> CorReasonLabels { get; } = [];
    internal Dictionary<string, (OptionButton Picker, Button Submit)> CorDesignations { get; } = [];

    private static string CorText(SessionView view, CorState cor, Arrangement? arrangement = null)
        => $"{cor.Name}{(string.IsNullOrEmpty(cor.Unit) ? "" : " · " + cor.Unit)} · {(arrangement is null ? view.CorSummary(cor) : view.CorSummary(cor, arrangement))}";

    private void AddCorDesignation(VBoxContainer parent, Arrangement arrangement, SessionView view)
    {
        if (!view.CorModeled || !view.Rules.Instruments.TryGetValue(arrangement.Instrument, out var instrument) || !instrument.WrittenTerms) return;
        var current = view.Cors.FirstOrDefault(c => c.Id == arrangement.CorId);
        var label = DeskUi.Label($"{view.ArrangementName(arrangement.Id)} · COR: {(current is null ? "None" : CorText(view, current, arrangement))}");
        label.SetMeta("cor_arrangement", arrangement.Id); parent.AddChild(label);
        // These controls are offered on running, office-awarded contracts. Sim validates the submitted action.
        if (arrangement.CreatedDay <= 0 || arrangement.IsCoordination || arrangement.Unauthorized
            || arrangement.Status is not (ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended)) return;
        var selected = current?.Id ?? view.Cors.First().Id;
        var formHash = session.StateHash;
        var submit = DeskUi.Button("Designate COR · " + view.ArrangementName(arrangement.Id), () =>
        {
            if (formHash != session.StateHash || view.Finished || selected == arrangement.CorId) return;
            SendAction(new DesignateCorAction { Day = session.View!.Day, Arrangement = arrangement.Id, Cor = selected });
        });
        Label reason = null!;
        var explanation = DeskUi.Label(); explanation.SetMeta("cor_designation_why", arrangement.Id);
        void Gate()
        {
            DeskUi.Gate(submit, reason, view.Finished ? DeskUi.FinishedReason : selected == arrangement.CorId ? "This COR is already assigned to the contract." : null);
            explanation.Text = view.CorDesignationWhy(selected, arrangement.Id) ?? "";
            explanation.Visible = explanation.Text.Length > 0;
        }
        var picker = AddPick(parent, "COR for " + view.ArrangementName(arrangement.Id), view.Cors.Select(c => new Pick(c.Id, CorText(view, c, arrangement))).ToArray(), selected,
            id => { if (formHash != session.StateHash) return; selected = id; Gate(); });
        picker.Disabled = view.Finished;
        reason = DeskUi.ActionReason(parent, submit); parent.AddChild(explanation);
        Gate(); CorDesignations.Add(arrangement.Id, (picker, submit));
    }
}
