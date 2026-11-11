using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal List<Label> CorReasonLabels { get; } = [];
    internal Dictionary<string, (OptionButton Picker, Button Submit)> CorDesignations { get; } = [];

    private static string CorText(SessionView view, CorState cor)
        => $"{cor.Name}{(string.IsNullOrEmpty(cor.Unit) ? "" : " · " + cor.Unit)} · {view.CorSummary(cor)}";

    private void AddCorDesignation(VBoxContainer parent, Arrangement arrangement, SessionView view)
    {
        if (!view.CorModeled || !view.Rules.Instruments.TryGetValue(arrangement.Instrument, out var instrument) || !instrument.WrittenTerms) return;
        var current = view.Cors.FirstOrDefault(c => c.Id == arrangement.CorId);
        var label = DeskUi.Label($"{view.ArrangementName(arrangement.Id)} · COR: {(current is null ? "None" : CorText(view, current))}");
        label.SetMeta("cor_arrangement", arrangement.Id); parent.AddChild(label);
        // These controls are offered on running, office-awarded contracts. Sim validates the submitted action.
        if (arrangement.CreatedDay <= 0 || arrangement.IsCoordination || arrangement.Unauthorized
            || arrangement.Status is not (ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended)) return;
        var selected = current?.Id ?? view.Cors.First().Id;
        var submit = DeskUi.Button("Designate COR · " + view.ArrangementName(arrangement.Id), () =>
            SendAction(new DesignateCorAction { Day = session.View!.Day, Arrangement = arrangement.Id, Cor = selected }));
        void Gate() => submit.Disabled = view.Finished || selected == arrangement.CorId;
        var picker = AddPick(parent, "Designate COR", view.Cors.Select(c => new Pick(c.Id, CorText(view, c))).ToArray(), selected,
            id => { selected = id; Gate(); });
        picker.Disabled = view.Finished;
        Gate(); parent.AddChild(submit); CorDesignations.Add(arrangement.Id, (picker, submit));
    }
}
