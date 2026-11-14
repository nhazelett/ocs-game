using Godot;

namespace LinesOfSupport.Game.Tests;

internal static class DeskSmokeSupport
{
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static GameShell Shell(Node parent)
    {
        using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
        var shell = scene.Instantiate<GameShell>();
        parent.AddChild(shell);
        DeveloperWorkspace(shell);
        Scenario(shell, "spoke-b");
        return shell;
    }

    internal static void DeveloperWorkspace(GameShell shell)
    {
        // Automated play must never overwrite the player's deployment.
        shell.AutosavePathOverride ??= Path.Combine(Path.GetTempPath(), "ocs-smoke-" + System.Environment.ProcessId, Guid.NewGuid() + ".json");
        shell.DeveloperToggle.ButtonPressed = true;
        shell.DeveloperWorkspaceButton.EmitSignal(Button.SignalName.Pressed);
    }

    internal static void Scenario(GameShell shell, string name)
    {
        var picker = shell.GetNode<OptionButton>("%ScenarioPicker");
        var index = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemText(i) == name);
        picker.Select(index); picker.EmitSignal(OptionButton.SignalName.ItemSelected, index);
        DeployWithoutPrep(shell);
        ChoosePreviewFixture(shell);
    }

    internal static void DeployWithoutPrep(GameShell shell)
    {
        foreach (var choice in shell.PrepChoices.Values) choice.ButtonPressed = false;
        shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
        FinishOpening(shell);
    }

    internal static void FinishOpening(GameShell shell)
    {
        for (var i = 0; i < 8 && shell.ReadingTurnover; i++) shell.TurnoverNext.EmitSignal(Button.SignalName.Pressed);
        Require(!shell.ReadingTurnover, "Turnover finishes through its real Continue controls.");
    }

    internal static void ChoosePreviewFixture(GameShell shell)
    {
        // Older rule-binding checks need an assembled preview. Select explicitly through the new UI.
        var editor = shell.CurrentActionEditor;
        if (editor?.Vendors.FirstOrDefault(v => !v.VehicleOnly) is not { } vendor) return;
        Pick(shell.VendorInput, vendor.Id);
        if (editor.Instruments.FirstOrDefault(i => i.UnavailableReason is null) is { } instrument) Pick(shell.InstrumentInput, instrument.Id);
    }

    internal static void PickAwardTarget(GameShell shell, string id)
    {
        var requests = shell.AwardRequestInput;
        if (Enumerable.Range(0, requests.ItemCount).Any(i => requests.GetItemMetadata(i).AsString() == id))
            Pick(requests, id);
        else
        {
            Pick(requests, "own-initiative");
            Pick(shell.InitiativeTargetInput!, id);
        }
    }

    internal static void Replay(GameShell shell, string name = "approach1 as written")
    {
        var picker = shell.GetNode<OptionButton>("%ReplayPicker");
        picker.Select(Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemText(i) == name));
        shell.GetNode<Button>("%ReplayButton").EmitSignal(Button.SignalName.Pressed);
        ChoosePreviewFixture(shell);
    }

    internal static void Step(GameShell shell, int days = 1)
    {
        for (var i = 0; i < days; i++)
        {
            shell.FinishDayPlayback(); shell.CloseDaySummary(); EndDay(shell); shell.FinishDayPlayback(); shell.CloseDaySummary();
        }
    }

    internal static void EndDay(GameShell shell)
    {
        shell.GetNode<Button>("%NextDay").EmitSignal(Button.SignalName.Pressed);
        if (shell.EndDayDialog.Visible) shell.EndDayDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
    }

    internal static void PickPriorities(GameShell shell, params string[] priorities)
    {
        foreach (var toggle in shell.PriorityInputs.Values) toggle.ButtonPressed = false;
        foreach (var priority in priorities) shell.PriorityInputs[priority].ButtonPressed = true;
    }

    internal static void Pick(OptionButton picker, string id)
    {
        var index = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemMetadata(i).AsString() == id);
        picker.Select(index); picker.EmitSignal(OptionButton.SignalName.ItemSelected, index);
    }
}
