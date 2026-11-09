using Godot;

namespace LinesOfSupport.Game;

internal static class GamePaths
{
    internal static string AssetRoot => OS.HasFeature("editor")
        ? Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "assets"))
        : Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "assets");
    internal static string ScenarioRoot => OS.HasFeature("editor")
        ? Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "scenarios"))
        : Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "scenarios");
}
