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

    internal AssigneePicker(IReadOnlyList<AssigneeChoice> choices, string selected, bool disabled, string caption = "Who should take this task?", bool compact = false)
    {
        Choices = choices; Selected = selected;
        AddThemeConstantOverride("separation", 6);
        AddChild(DeskUi.Label(caption));
        foreach (var choice in choices)
        {
            string CompactText() => (choice.MemberId == Selected ? "✓ " : "") + choice.Name
                + $" · {choice.FreeHoursToday} h free · " + (choice.DoneDayMin is { } lo && choice.DoneDayMax is { } hi
                    ? lo == hi ? $"done day {lo}" : $"done day {lo}–{hi}" : "done day unavailable");
            var button = DeskUi.Button(compact ? CompactText() : choice.Sentence, () => Select(choice.MemberId));
            if (compact)
            {
                button.AutowrapMode = TextServer.AutowrapMode.Off;
                button.TooltipText = choice.Sentence;
                button.SetMeta("compact_assignee", CompactText().TrimStart('✓', ' '));
            }
            button.ToggleMode = true; button.ButtonPressed = choice.MemberId == selected;
            button.Disabled = disabled; button.CustomMinimumSize = new Vector2(0, compact ? 36 : 88);
            button.ThemeTypeVariation = "TabletopPerson";
            button.TextureFilter = TextureFilterEnum.LinearWithMipmaps;
            button.ExpandIcon = true; button.AddThemeConstantOverride("icon_max_width", compact ? 28 : 48);
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
        foreach (var (member, button) in ChoiceButtons)
        {
            button.ButtonPressed = member == id;
            if (button.HasMeta("compact_assignee")) button.Text = (member == id ? "✓ " : "") + button.GetMeta("compact_assignee").AsString();
        }
        SelectionChanged?.Invoke();
    }
}
