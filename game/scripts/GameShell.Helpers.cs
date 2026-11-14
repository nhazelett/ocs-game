using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal VBoxContainer RequestHelperRows { get; private set; } = null!;
    internal Dictionary<string, Button> RequestHelperButtons { get; } = [];

    private static string HelperKey(HelperView helper) => helper.Kind + ":" + (helper.Person?.Id ?? helper.CapabilityId);

    private void RenderRequestHelpers(SessionView view, RequirementEditorView card)
    {
        RequestHelperButtons.Clear();
        linkedRequirement.AddChild(DeskUi.Heading("Who can help"));
        RequestHelperRows = new VBoxContainer { Name = "RequestHelpers" };
        RequestHelperRows.AddThemeConstantOverride("separation", 14); linkedRequirement.AddChild(RequestHelperRows);
        var hash = session.StateHash;
        foreach (var helper in card.WhoCanHelp ?? [])
        {
            var key = HelperKey(helper);
            var panel = new PanelContainer(); RequestHelperRows.AddChild(panel);
            var row = new VBoxContainer(); row.AddThemeConstantOverride("separation", 8); panel.AddChild(row);
            row.SetMeta("helper", key); row.SetMeta("helper_state", helper.State);
            var header = new HBoxContainer(); row.AddChild(header);
            if (helper.Person is { } person)
            {
                var face = new PersonPortrait { SizeFlagsVertical = SizeFlags.ShrinkBegin };
                face.SetPerson(person); header.AddChild(face);
                face.CustomMinimumSize = new Vector2(48, 60);
            }
            var name = DeskUi.Label(helper.Name); name.Name = "HelperName";
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill; header.AddChild(name);
            row.AddChild(DeskUi.Label(helper.Does));
            row.AddChild(DeskUi.Label(helper.Informs ? "Informs this request." : helper.Kind == "j4" ? "Opens support through the J4." : "Opens the vendor’s terms."));
            var state = DeskUi.Label(helper.Sentence); state.SetMeta("helper_sentence", key); row.AddChild(state);
            // The answer is supplied only after the visit, never inferred from what someone could tell us.
            if (helper.Answer is { } answer)
            {
                var said = DeskUi.Label(answer); said.SetMeta("helper_answer", key); row.AddChild(said);
            }
            if (helper.State == HelperView.Asking)
                row.AddChild(DeskUi.Label($"This takes about {helper.Hours} shop {(helper.Hours == 1 ? "hour" : "hours")}."));
            if (helper.Start is not { } action) continue;
            var preview = action is EngageJ4Action ? null : view.PreviewAny(action);
            if (preview is not null)
            {
                var timing = DeskUi.Label($"The earliest result is day {preview.EarliestStartMin}" +
                    (preview.EarliestStartMin == preview.EarliestStartMax ? "" : $"–{preview.EarliestStartMax}") + DeskUi.QueueClause(preview.QueueAheadHours) + ".");
                timing.SetMeta("ask_preview", helper.Person?.Id ?? key); row.AddChild(timing);
            }
            var text = action is MeetVendorAction ? "Meet " + helper.Person!.DisplayName : action is EngageJ4Action ? "Ask the J4" : "Ask " + helper.Person!.DisplayName;
            var button = DeskUi.Button(text, () =>
            {
                if (hash != session.StateHash || SelectedCard != card.CardId || PlayingDay || ReadingDaySummary
                    || session.View is not { Finished: false } current) return;
                // Re-read the typed target so a stale callback cannot repeat or redirect a visit.
                var latest = current.RequirementEditor(card.CardId)?.WhoCanHelp?.FirstOrDefault(h => HelperKey(h) == key);
                if (latest?.Start is not { } start) return;
                if (start is EngageJ4Action j4) OpenJ4Support(j4);
                else if (current.PreviewAny(start).Accepted) SendAction(start);
            });
            button.SetMeta("helper_start", key);
            var reason = DeskUi.ActionReason(row, button);
            DeskUi.Gate(button, reason, view.Finished ? DeskUi.FinishedReason : preview?.HardStops.FirstOrDefault());
            RequestHelperButtons[key] = button;
            if (action is AskAboutAction ask) RequirementAskButtons[ask.Person] = button;
        }
    }
}
