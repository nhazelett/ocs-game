using Godot;
using System.Text.Json;

namespace LinesOfSupport.Game.Tests;

// Explicit command-line check of the real release main scene; no alternate scene or UI controls.
internal static class ExportSmokeCheck
{
    internal static void Run(GameShell shell)
    {
        try
        {
            Require(!OS.HasFeature("editor"), "Run this check with the exported executable.");
            var catalog = ScenarioCatalog.Discover(GamePaths.ScenarioRoot);
            Require(catalog.Any(x => x.Name == "_stub"), "Packaged _stub is present.");
            foreach (var folder in catalog)
            {
                ScenarioBoardData.Load(folder.ScenarioPath);
                using var json = JsonDocument.Parse(File.ReadAllText(folder.ScenarioPath));
                var version = json.RootElement.GetProperty("rulesVersion").GetString();
                Require(File.Exists(Path.Combine(GamePaths.ScenarioRoot, "rules", $"rules-{version}.json")), "Referenced rules are packaged.");
            }

            var picker = shell.GetNode<OptionButton>("%ScenarioPicker");
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            Require(!picker.Disabled && picker.ItemCount == catalog.Count && map.Data is not null, "Exported shell discovers and displays its packaged data.");
            GD.Print($"EXPORT_SMOKE_PASS: {catalog.Count} scenarios, referenced rules, main scene and C# runtime.");
            shell.GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError($"EXPORT_SMOKE_FAIL: {error}");
            shell.GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
