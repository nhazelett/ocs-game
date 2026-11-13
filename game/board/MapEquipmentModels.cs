using Godot;

namespace LinesOfSupport.Game;

// The same embedded, vertex-colour glTF kit is read in the editor and Windows package.
// Packed scenes share meshes/materials across sites and morning refreshes.
internal static class MapEquipmentModels
{
    private static readonly Dictionary<string, PackedScene> scenes = [];
    private static bool cleanupRegistered;
    private static GltfDocumentExtensionConvertImporterMesh? converter;

    private static void Clear()
    {
        foreach(var scene in scenes.Values) scene.Dispose();
        scenes.Clear();
        if(converter is not null)
        {
            GltfDocument.UnregisterGltfDocumentExtension(converter);
            converter.Dispose();
            converter=null;
        }
    }

    internal static string ModelFor(string capability) => capability switch
    {
        "power" => "generator",
        "fuel" => "fuel-bladder",
        "water" => "water-buffalo",
        "lodging" => "chu",
        "food" => "field-kitchen",
        "waste" => "waste-dumpster",
        "mhe" => "forklift",
        "transport" => "light-truck",
        "comms" => "antenna-mast",
        _ => "warehouse"
    };

    internal static Node3D Instantiate(string capability)
    {
        if(!cleanupRegistered)
        {
            // Export templates need the runtime importer-mesh conversion explicitly.
            converter=new GltfDocumentExtensionConvertImporterMesh();
            GltfDocument.RegisterGltfDocumentExtension(converter,true);
            ((SceneTree)Engine.GetMainLoop()).Root.TreeExiting += Clear;
            cleanupRegistered=true;
        }
        var name = ModelFor(capability);
        if (!scenes.TryGetValue(name, out var packed))
        {
            var path = Path.Combine(GamePaths.AssetRoot, "kit", name + ".glb");
            using var document = new GltfDocument();
            using var state = new GltfState();
            if (document.AppendFromFile(path, state) != Error.Ok)
                throw new InvalidDataException($"Cannot read map kit model {path}.");
            var source = document.GenerateScene(state);
            // Preserve the kit silhouette and running gear; separate these two tanks
            // from olive ground with rubber-black fuel and a pale blue-grey water shell.
            void Finish(Node node)
            {
                if(node is MeshInstance3D mesh&&(name=="fuel-bladder"||name=="water-buffalo"&&node.Name=="water-buffalo-1"))
                {
                    using var material=new StandardMaterial3D{AlbedoColor=new Color(name=="fuel-bladder"?"202c2c":"b8ced0"),
                        VertexColorUseAsAlbedo=false,Roughness=1,CullMode=BaseMaterial3D.CullModeEnum.Disabled};
                    mesh.MaterialOverride=material;mesh.SetMeta("tank_finish",true);
                }
                foreach(var child in node.GetChildren())Finish(child);
            }
            Finish(source);
            try
            {
                packed = new PackedScene();
                if (packed.Pack(source) != Error.Ok)
                    throw new InvalidDataException($"Cannot pack map kit model {path}.");
                scenes.Add(name, packed);
            }
            finally { source.Free(); }
        }
        var root = new Node3D { Name = "CapabilityModel" };
        root.SetMeta("kit_model", name);
        var model = packed.Instantiate<Node3D>(); root.AddChild(model);
        model.RotationDegrees = new Vector3(0, -35, 0);
        var bounds = Bounds(model);
        var scale = .92f / Math.Max(bounds.Size.X, bounds.Size.Z);
        // Keep the mast legible without towering over its neighbours.
        scale = Math.Min(scale, 1.05f / bounds.Size.Y);
        model.Scale *= scale;
        model.Position -= new Vector3(bounds.GetCenter().X, bounds.Position.Y, bounds.GetCenter().Z) * scale;
        return root;
    }

    internal static Vector3[] Vertices(Node3D root)
    {
        var points=new List<Vector3>();
        void Visit(Node3D node,Transform3D parent)
        {
            var transform=parent*node.Transform;
            if(node is MeshInstance3D{Mesh:not null} mesh)
                points.AddRange(mesh.Mesh.GetFaces().Select(p=>transform*p));
            foreach(var child in node.GetChildren().OfType<Node3D>())Visit(child,transform);
        }
        Visit(root,Transform3D.Identity);return points.ToArray();
    }
    internal static Aabb Bounds(Node3D root)
    {
        Aabb? result = null;
        void Visit(Node3D node, Transform3D parent)
        {
            var transform = parent * node.Transform;
            if (node is MeshInstance3D { Mesh: not null } mesh)
            {
                var bounds = transform * mesh.GetAabb();
                result = result is null ? bounds : result.Value.Merge(bounds);
            }
            foreach (var child in node.GetChildren().OfType<Node3D>()) Visit(child, transform);
        }
        Visit(root, Transform3D.Identity);
        return result ?? throw new InvalidDataException("Map kit model has no meshes.");
    }
}
