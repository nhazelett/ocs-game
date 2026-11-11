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
            foreach(var style in Art.ArtStudyStyle.All)
            {
                using var body=Art.ArtStudyStyle.LoadFont(style.Font);
                using var heading=Art.ArtStudyStyle.LoadFont(style.HeadingFont);
                Require(body.GetFontName().Length>0 && heading.GetFontName().Length>0,"Every selectable look loads its packaged fonts outside the checkout.");
            }
            foreach(var license in new[]{"Atkinson-OFL.txt","Barlow-OFL.txt","Bitter-OFL.txt"})
                Require(File.Exists(Path.Combine(GamePaths.AssetRoot,"art-studies","fonts",license)),"Every packaged typeface has its redistribution license.");
            Require(File.Exists(Path.Combine(GamePaths.AssetRoot,"portraits","fallback.svg")),"Packaged cast fallback is present.");
            Require(shell.Sound.LoadedCueCount == 4 && File.Exists(Path.Combine(GamePaths.AssetRoot,"audio","LICENSE.txt")), "Packaged sound cues and license load outside the checkout.");
            Require(shell.Music.LoadedStreamCount == 4, "Packaged approved music and ambience load outside the checkout.");
            DeskSmokeSupport.DeveloperWorkspace(shell);
            var catalog = ScenarioCatalog.Discover(GamePaths.ScenarioRoot);
            Require(catalog.All(x => !x.Name.StartsWith("_", StringComparison.Ordinal)), "Development scenarios are not packaged.");
            Require(catalog.Any(x => x.Name == "spoke-b-90"), "Packaged 90-day Spoke B is present.");
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
            var spoke = catalog.Select(x => x.Name).ToList().IndexOf("spoke-b");
            Require(spoke >= 0, "Packaged Spoke B is present.");
            picker.Select(spoke);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, spoke);
            var replay = shell.GetNode<Button>("%ReplayButton");
            Require(!replay.Disabled, "Packaged approach logs enable Replay.");
            shell.GetNode<OptionButton>("%ReplayPicker").Select(0);
            replay.EmitSignal(Button.SignalName.Pressed);
            DeskSmokeSupport.Step(shell,10);
            Require(shell.Session.CompletedDay == 10 && shell.Session.View!.Capabilities.Any(c => c.SiteId == "spokeBSite" && c.CapabilityId == "power" && c.TodayPermille == 0), "Packaged approach 1 reaches day-10 power down.");
            GD.Print($"EXPORT_SMOKE_PASS: {catalog.Count} scenarios, referenced rules, real session and packaged replay.");
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
