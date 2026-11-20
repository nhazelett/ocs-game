using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal VBoxContainer RequestHelperRows { get; private set; } = null!;
    internal Dictionary<string, Button> RequestHelperButtons { get; } = [];

    internal Dictionary<string, AssigneePicker> RequestHelperAssignees { get; } = [];

    private static string HelperKey(HelperView helper) => helper.Kind + ":" + (helper.Person?.Id ?? helper.CapabilityId);

    private void RenderRequestHelpers(SessionView view, RequirementEditorView card)
    {
        RequestHelperButtons.Clear(); RequestHelperAssignees.Clear();
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
            bool CurrentHelper() => SelectedCard == card.CardId
                && session.View?.RequirementEditor(card.CardId)?.WhoCanHelp?.FirstOrDefault(h => HelperKey(h) == key)?.Start is not null;
            Button button;
            if (action is EngageJ4Action j4)
            {
                // This opens a quantity form; its committing Ask button has the assignee picker.
                button = DeskUi.Button("Ask the J4", () =>
                {
                    if (hash == session.StateHash && CurrentHelper() && !PlayingDay && !ReadingDaySummary
                        && session.View is { Finished: false }) OpenJ4Support(j4);
                });
                var reason = DeskUi.ActionReason(row, button);
                DeskUi.Gate(button, reason, view.Finished ? DeskUi.FinishedReason : null);
                row.AddChild(DeskUi.Label("Opens the support quantity form to choose who does it; no work is sent yet."));
            }
            else
            {
                var assigned = AddAssignedAction(row, row, view, action,
                    action is MeetVendorAction ? "Meet " + helper.Person!.DisplayName : "Ask " + helper.Person!.DisplayName,
                    action is MeetVendorAction meet ? VendorMeetingEffect(view, meet.Vendor) : "Asks " + helper.Person!.DisplayName + " about " + card.Title + ".",
                    stillCurrent: CurrentHelper);
                button = assigned.Button; RequestHelperAssignees[key] = assigned.Picker;
                assigned.Cost.SetMeta("ask_preview", helper.Person?.Id ?? key);
            }
            button.SetMeta("helper_start", key); RequestHelperButtons[key] = button;
            if (action is AskAboutAction ask) RequirementAskButtons[ask.Person] = button;
        }
    }
}
