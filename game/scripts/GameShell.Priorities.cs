using Godot;
using LinesOfSupport.Sim;

namespace LinesOfSupport.Game;

// DESK-26 (D7). Presentation only; the Sim owns every cost and consequence. Remove this file and the
// DESK-26 calls in Actions to remove the choice from the game without affecting other editors.
public partial class GameShell
{
    private static readonly string[] PriorityOrder = ["fast", "cheap", "good"];
    private readonly HashSet<string> awardPriorities = [];
    internal Dictionary<string, CheckBox> PriorityInputs { get; } = [];
    internal Label? AwardTrade { get; private set; }
    // DESK-65 (D27): only open-market written buys pick two; a BPA call or an IDIQ order is priced by its vehicle.
    private bool WrittenAward => session.View?.Rules.Instruments.TryGetValue(awardInstrument ?? "", out var instrument) == true
        && instrument.PicksTwo;

    private void BuildAwardPriorities(SessionView view)
    {
        PriorityInputs.Clear(); AwardTrade = null;
        if (!WrittenAward) return;
        priorityHelp = DeskUi.Label("Choose the two that matter most. To change a pair, turn one off first.");
        var row = new HBoxContainer { Name = "PickTwo" }; awardPanel.AddChild(row);
        var caption = DeskUi.Label("Pick two"); caption.CustomMinimumSize = new Vector2(150, 0); caption.SizeFlagsVertical = SizeFlags.ShrinkCenter; row.AddChild(caption);
        row.AddThemeConstantOverride("separation", 12);
        var formHash = session.StateHash;
        var formVersion = awardFormVersion;
        foreach (var priority in PriorityOrder)
        {
            var toggle = new CheckBox { Text = char.ToUpperInvariant(priority[0]) + priority[1..],
                CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            toggle.SetPressedNoSignal(awardPriorities.Contains(priority));
            row.AddChild(toggle); PriorityInputs[priority] = toggle;
            toggle.Toggled += chosen =>
            {
                if (formVersion != awardFormVersion || formHash != session.StateHash || AtFrontDoor || ReadingTurnover || PlayingDay
                    || session.View is not { Finished: false })
                { toggle.SetPressedNoSignal(awardPriorities.Contains(priority)); return; }
                if (chosen && awardPriorities.Count >= 2 && !awardPriorities.Contains(priority))
                { toggle.SetPressedNoSignal(false); return; }
                if (chosen) awardPriorities.Add(priority); else awardPriorities.Remove(priority);
                RefreshPriorityToggles(view.Finished);
                UpdateAwardPreview();
                if (awardPriorities.Count == 2) FocusAwardMonitor(formVersion, formHash);
            };
        }
        AwardTrade = DeskUi.Label(); AwardTrade.Hide(); awardPanel.AddChild(AwardTrade);
        RefreshPriorityToggles(view.Finished);
    }

    private async void FocusAwardMonitor(int version, string? hash)
    {
        var tree = GetTree();
        // The chosen pair changes the pinned summary and writer dates, then the scroll body's height.
        for (var frame = 0; frame < 4; frame++) await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (!IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion() || version != awardFormVersion
            || hash != session.StateHash || awardPriorities.Count != 2 || !RequestWorkspace.IsVisibleInTree()
            || CorInput is null || !CorInput.IsVisibleInTree()) return;
        var row = CorInput.GetParent<Control>();
        AwardScroll.ScrollVertical += Mathf.RoundToInt(row.GlobalPosition.Y - AwardScroll.GlobalPosition.Y);
    }

    private void RefreshPriorityToggles(bool finished)
    {
        foreach (var (priority, toggle) in PriorityInputs)
            toggle.Disabled = finished || (awardPriorities.Count == 2 && !awardPriorities.Contains(priority));
    }
}
