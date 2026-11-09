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
        shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
    }

    internal static void Replay(GameShell shell, string name = "approach1 as written")
    {
        var picker = shell.GetNode<OptionButton>("%ReplayPicker");
        picker.Select(Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemText(i) == name));
        shell.GetNode<Button>("%ReplayButton").EmitSignal(Button.SignalName.Pressed);
    }

    internal static void Step(GameShell shell, int days = 1)
    {
        for (var i = 0; i < days; i++) shell.GetNode<Button>("%NextDay").EmitSignal(Button.SignalName.Pressed);
    }

    internal static void Pick(OptionButton picker, string id)
    {
        var index = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemMetadata(i).AsString() == id);
        picker.Select(index); picker.EmitSignal(OptionButton.SignalName.ItemSelected, index);
    }
}
