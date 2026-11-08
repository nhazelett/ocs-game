using Godot;

namespace LinesOfSupport.Game.Tests;

public partial class KitExperiment : Node3D
{
    public override async void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            var root = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), ".."));
            var assets = Path.Combine(root, "assets", "kit");
            if (args.Contains("--generate-fallback-kit"))
            {
                Directory.CreateDirectory(assets);
                foreach (var name in KitPrimitives.Names)
                {
                    var piece = KitPrimitives.Create(name);
                    AddChild(piece);
                    using var document = new GltfDocument();
                    using var state = new GltfState();
                    Require(document.AppendFromScene(piece, state) == Error.Ok, "Fallback scene converted to glTF.");
                    Require(document.WriteToFilesystem(state, Path.Combine(assets, name + ".glb")) == Error.Ok, "Fallback glTF saved.");
                    RemoveChild(piece);
                    piece.Free();
                }
                GD.Print("KIT_FALLBACK_GENERATED: Godot primitives, not Blender output. Update PROVENANCE before committing.");
            }

            var positions = new[] { new Vector3(-4, 0, -2), new Vector3(0, 0, -2), new Vector3(4, 0, -2),
                new Vector3(-2.4f, 0, 2.5f), new Vector3(2.4f, 0, 2.5f) };
            var loaded = 0;
            for (var i = 0; i < KitPrimitives.Names.Length; i++)
            {
                var name = KitPrimitives.Names[i];
                var path = Path.Combine(assets, name + ".glb");
                Node3D piece;
                if (File.Exists(path))
                {
                    using var document = new GltfDocument();
                    using var state = new GltfState();
                    Require(document.AppendFromFile(path, state) == Error.Ok, $"Load {name} glTF.");
                    piece = (Node3D)document.GenerateScene(state);
                    Require(HasVertexColors(piece), $"{name} keeps vertex colors after glTF round trip.");
                    loaded++;
                }
                else
                    piece = KitPrimitives.Create(name);
                piece.Position = positions[i];
                AddChild(piece);
                AddChild(new Label3D { Text = name, Position = positions[i] + new Vector3(0, 2.8f, 0),
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, FontSize = 48, PixelSize = .005f });
            }
            var floor = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(16, 12) }, Position = new Vector3(0, -.03f, 0),
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(.42f, .41f, .36f), Roughness = 1 } };
            AddChild(floor);
            var light = new DirectionalLight3D { RotationDegrees = new Vector3(-55, -30, 0), ShadowEnabled = true, LightEnergy = 1.2f };
            AddChild(light);
            var environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(.16f, .17f, .18f), AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = Colors.White, AmbientLightEnergy = .45f };
            AddChild(new WorldEnvironment { Environment = environment });
            var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 15, Current = true };
            AddChild(camera);
            camera.LookAtFromPosition(new Vector3(11, 10, 14), new Vector3(0, .8f, 0));
            var overlay = new CanvasLayer();
            AddChild(overlay);
            overlay.AddChild(new Label { Text = "M0 kit experiment · generated primitive fallback · Blender unavailable", Position = new Vector2(20, 16) });
            Require(camera.Projection == Camera3D.ProjectionType.Orthogonal && GetChildren().OfType<DirectionalLight3D>().Count() == 1,
                "One directional light and orthographic camera.");
            var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                Require(shot + 1 < args.Length && DisplayServer.GetName() != "headless", "Screenshot needs a display and path.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Kit screenshot saved.");
            }
            GD.Print($"KIT_SMOKE_PASS: five pieces, {loaded} glTF files loaded, vertex colors, orthographic camera, one directional light.");
            if (DisplayServer.GetName() == "headless" || args.Contains("--smoke-test") || shot >= 0)
                GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError($"KIT_SMOKE_FAIL: {error}");
            GetTree().Quit(1);
        }
    }

    private static bool HasVertexColors(Node node)
    {
        if (node is MeshInstance3D { Mesh: not null } mesh)
            for (var i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                var colors = mesh.Mesh.SurfaceGetArrays(i)[(int)Mesh.ArrayType.Color];
                if (colors.VariantType != Variant.Type.Nil && colors.AsColorArray().Length > 0) return true;
            }
        return node.GetChildren().Any(HasVertexColors);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
