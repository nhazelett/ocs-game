using Godot;
using System.Text.Json;

namespace LinesOfSupport.Game.Tests;

// Explicit M0-10 primitive fallback when Blender is unavailable. Presentation geometry only.
internal static class KitPrimitives
{
    internal static readonly string[] Names = ["tent", "chu", "generator", "fuel-bladder", "light-truck"];
    internal static readonly string[] ExtensionNames = ["terrain-sand", "terrain-scrub", "terrain-coast",
        "road-straight", "road-corner", "route-straight", "route-corner", "warehouse", "town-block",
        "antenna-mast", "guard-tower", "barrier-set", "fuel-truck", "bus", "forklift", "pallet", "water-buffalo"];
    internal static readonly string[] AllNames = [.. Names, .. ExtensionNames];
    private static readonly Color Sand = new(0.64f, 0.58f, 0.43f);
    private static readonly Color Olive = new(0.42f, 0.43f, 0.31f);
    private static readonly Color Dark = new(0.16f, 0.18f, 0.17f);
    private static readonly Color Glass = new(0.26f, 0.32f, 0.34f);

    internal static Node3D Create(string name)
    {
        var root = new Node3D { Name = name };
        switch (name)
        {
            case "tent":
                Part(root, new PrismMesh { Size = new Vector3(2.8f, 1.8f, 3.2f) }, new Vector3(0, .9f, 0), Sand);
                break;
            case "chu":
                Box(root, new Vector3(2.4f, 2.2f, 3.5f), new Vector3(0, 1.1f, 0), Sand);
                Box(root, new Vector3(.75f, 1.7f, .03f), new Vector3(0, .85f, -1.76f), Dark);
                break;
            case "generator":
                Box(root, new Vector3(1.5f, 1.1f, 1), new Vector3(0, .55f, 0), Olive);
                Part(root, Cylinder(.08f, .8f), new Vector3(.45f, 1.4f, 0), Dark);
                break;
            case "fuel-bladder":
                var bladder = Part(root, new SphereMesh { Radius = 1, Height = 2, RadialSegments = 12, Rings = 6 }, new Vector3(0, .45f, 0), Olive);
                bladder.Scale = new Vector3(1.4f, .45f, 1);
                break;
            case "light-truck":
                Box(root, new Vector3(1.6f, .35f, 3), new Vector3(0, .65f, 0), Dark);
                Box(root, new Vector3(1.6f, .45f, 1.7f), new Vector3(0, 1, .55f), Sand);
                Box(root, new Vector3(1.6f, 1.1f, 1.15f), new Vector3(0, 1.25f, -.9f), Sand);
                Box(root, new Vector3(1.3f, .45f, .03f), new Vector3(0, 1.5f, -1.48f), Glass);
                foreach (var x in new[] { -.88f, .88f })
                    foreach (var z in new[] { -1f, 1f })
                        Part(root, Cylinder(.37f, .25f), new Vector3(x, .38f, z), Dark).Rotation = new Vector3(0, 0, Mathf.Pi / 2);
                break;
            default: FromRecipe(root, name); break;
        }
        return root;
    }

    private static void FromRecipe(Node3D root, string name)
    {
        if (!ExtensionNames.Contains(name)) throw new ArgumentException($"Unknown kit piece {name}.");
        var recipe = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "tools", "bpy", "kit_parts.json"));
        using var json = JsonDocument.Parse(File.ReadAllText(recipe));
        foreach (var part in json.RootElement.GetProperty(name).EnumerateArray())
        {
            var at = Vector(part.GetProperty("at"));
            var color = part.GetProperty("color").GetString() switch
            {
                "sand" => Sand, "olive" => Olive, "dark" => Dark, "glass" => Glass,
                var unknown => throw new InvalidDataException($"Unknown palette color {unknown}.")
            };
            if (part.GetProperty("shape").GetString() == "box")
                Box(root, Vector(part.GetProperty("size")), at, color);
            else if (part.GetProperty("shape").GetString() == "cylinder")
            {
                var instance = Part(root, Cylinder(part.GetProperty("radius").GetSingle(), part.GetProperty("length").GetSingle()), at, color);
                var axis = part.TryGetProperty("axis", out var value) ? value.GetString() : "y";
                instance.Rotation = axis switch
                {
                    "x" => new Vector3(0, 0, Mathf.Pi / 2), "z" => new Vector3(Mathf.Pi / 2, 0, 0),
                    "y" => Vector3.Zero, _ => throw new InvalidDataException($"Unknown axis {axis}.")
                };
            }
            else throw new InvalidDataException("Unknown recipe shape.");
        }
    }

    private static Vector3 Vector(JsonElement array) => new(array[0].GetSingle(), array[1].GetSingle(), array[2].GetSingle());

    private static CylinderMesh Cylinder(float radius, float length) =>
        new() { TopRadius = radius, BottomRadius = radius, Height = length, RadialSegments = 8, Rings = 1 };

    private static void Box(Node3D parent, Vector3 size, Vector3 at, Color color) =>
        Part(parent, new BoxMesh { Size = size }, at, color);

    private static MeshInstance3D Part(Node3D parent, PrimitiveMesh primitive, Vector3 at, Color color)
    {
        var arrays = primitive.GetMeshArrays();
        var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
        var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
        if (indices.Length == 0) indices = Enumerable.Range(0, vertices.Length).ToArray();
        using var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        surface.SetMaterial(new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 1 });
        for (var i = 0; i < indices.Length; i += 3)
        {
            var a = vertices[indices[i]];
            var b = vertices[indices[i + 1]];
            var c = vertices[indices[i + 2]];
            var normal = (b - a).Cross(c - a).Normalized();
            if (normal.Dot(normals[indices[i]]) < 0) normal = -normal;
            for (var corner = 0; corner < 3; corner++)
            {
                surface.SetColor(color);
                surface.SetNormal(normal);
                surface.AddVertex(vertices[indices[i + corner]]);
            }
        }
        var instance = new MeshInstance3D { Mesh = surface.Commit(), Position = at };
        parent.AddChild(instance);
        primitive.Dispose();
        return instance;
    }
}
