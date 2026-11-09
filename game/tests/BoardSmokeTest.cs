using Godot;

namespace LinesOfSupport.Game.Tests;

public partial class BoardSmokeTest : Node
{
    private const string Fixture = """
        {"id":"first","title":"First map","islands":[
          {"id":"alpha","name":"Alpha island","isHub":true},
          {"id":"beta","name":"Beta island","isHub":false}],
         "transportLinks":[{"from":"alpha","to":"beta","mode":"barge"},
                           {"from":"alpha","to":"beta","mode":"aircraft"}],
         "sites":[{"id":"beta-site","name":"Beta site","island":"beta","opensDay":7}]}
        """;

    public override async void _Ready()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "los-board-" + Guid.NewGuid().ToString("N")));
        try
        {
            var first = Write(root, "_stub", Fixture);
            Write(root, "second", Fixture.Replace("\"first\"", "\"second\"").Replace("First map", "Second map"));
            Write(root, "invalid", "{");
            Directory.CreateDirectory(Path.Combine(root, "not-a-scenario"));
            var catalog = ScenarioCatalog.Discover(root);
            Require(catalog.Count == 3 && catalog[0].Name == "_stub", "Picker lists scenario folders in stable order only.");
            var data = ScenarioBoardData.Load(first);
            Require(data.Islands.Count == 2 && data.Links.Select(x => x.Mode).SequenceEqual(["barge", "aircraft"]), "Authored islands and parallel modes load.");
            Require(data.Sites.Single().Name == "Beta site" && data.Sites.Single().OpensDay == 7, "Site label fields load unchanged.");
            File.WriteAllText(first, Fixture.Replace("\"island\":\"beta\"", "\"island\":\"missing\""));
            var rejected = false;
            try { ScenarioBoardData.Load(first); }
            catch (InvalidDataException) { rejected = true; }
            Require(rejected, "Dangling island reference is rejected.");
            File.WriteAllText(first, Fixture);

            var shell = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<GameShell>();
            shell.ScenarioRootOverride = root;
            AddChild(shell);
            DeskSmokeSupport.DeveloperWorkspace(shell);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            Require(map.Data?.Id == "first", "The initial _stub is displayed.");
            var picker = shell.GetNode<OptionButton>("%ScenarioPicker");
            var second = catalog.Select(x => x.Name).ToList().IndexOf("second");
            picker.Select(second);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, second);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(map.Data?.Id == "second" && map.Data.Title == "Second map" && !shell.GetNode<Label>("%BoardCaption").Visible, "Picker switches the displayed map without inventing a capability selection.");
            var invalid = catalog.Select(x => x.Name).ToList().IndexOf("invalid");
            picker.Select(invalid);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, invalid);
            Require(map.Data is null && shell.GetNode<Label>("%BuildStatus").Text.Contains("Cannot load"), "Bad JSON clears old map and reports an error.");

            var missing = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<GameShell>();
            missing.ScenarioRootOverride = Path.Combine(root, "absent");
            AddChild(missing);
            Require(missing.GetNode<OptionButton>("%ScenarioPicker").Disabled, "Missing scenario folder is handled.");
            GD.Print("BOARD_SMOKE_PASS: catalog, data labels, parallel links, switching, invalid data, missing folder.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError($"BOARD_SMOKE_FAIL: {error}");
            GetTree().Quit(1);
        }
        finally
        {
            if (Directory.Exists(root) && root.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                Directory.Delete(root, recursive: true);
        }
    }

    private static string Write(string root, string folder, string json)
    {
        var directory = Path.Combine(root, folder);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "scenario.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
