using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

// The forecasts and wording belong to Sim. The game only selects who receives the work.
internal partial class AssigneePicker : VBoxContainer
{
    internal IReadOnlyList<AssigneeChoice> Choices { get; }
    internal Dictionary<string, Button> ChoiceButtons { get; } = [];
    internal string Selected { get; private set; }
    internal Action? SelectionChanged { get; set; }

    internal AssigneePicker(IReadOnlyList<AssigneeChoice> choices, string selected, bool disabled)
    {
        Choices = choices; Selected = selected;
        AddThemeConstantOverride("separation", 6);
        AddChild(DeskUi.Label("Who should take this task?"));
        foreach (var choice in choices)
        {
            var button = DeskUi.Button(choice.Sentence, () => Select(choice.MemberId));
            button.ToggleMode = true; button.ButtonPressed = choice.MemberId == selected;
            button.Disabled = disabled; button.CustomMinimumSize = new Vector2(0, 88);
            button.ThemeTypeVariation = "TabletopPerson";
            button.ExpandIcon = true; button.AddThemeConstantOverride("icon_max_width", 48);
            var portrait = new PersonPortrait();
            portrait.SetPerson(new PersonView(choice.MemberId, choice.Name, "", "Contracting shop", "team", choice.Portrait, new Dictionary<string, string>()));
            using (var icon = portrait.Texture) button.Icon = icon;
            portrait.Free();
            button.SetMeta("assignee", choice.MemberId);
            AddChild(button); ChoiceButtons.Add(choice.MemberId, button);
        }
    }

    private void Select(string id)
    {
        if (ChoiceButtons[id].Disabled) return;
        Selected = id;
        foreach (var (member, button) in ChoiceButtons) button.ButtonPressed = member == id;
        SelectionChanged?.Invoke();
    }
}
