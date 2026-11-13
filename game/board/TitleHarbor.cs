using Godot;

namespace LinesOfSupport.Game;

// A decorative morning view of the same kit used on the board. No Session or game facts.
// Render on entry/resize, then retain the texture; the title has no continuous 3D work.
public partial class TitleHarbor : Control
{
    private SubViewport viewport = null!;
    private TextureRect image = null!;
    private int renderVersion;
    private Vector2I snapshotSize;
    private bool hasSnapshot;
    internal int KitPieces { get; private set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        viewport = new SubViewport { Name = "HarborViewport", OwnWorld3D = true, Size = new Vector2I(680, 680),
            Msaa3D = Viewport.Msaa.Msaa4X, RenderTargetUpdateMode = SubViewport.UpdateMode.Once };
        AddChild(viewport);
        using var texture = viewport.GetTexture();
        image = new TextureRect { Texture = texture, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(image); image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var world = new Node3D(); viewport.AddChild(world);
        using var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("96b5b3"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("e7e5d3"), AmbientLightEnergy = .35f
        };
        world.AddChild(new WorldEnvironment { Environment = environment });
        world.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-34, -48, 0),
            LightColor = new Color("fff0d9"), LightEnergy = .8f, ShadowEnabled = true,
            DirectionalShadowMaxDistance = 40 });
        var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal,
            Size = 14.8f, Position = new Vector3(12, 14, 18), Current = true };
        viewport.AddChild(camera); camera.LookAt(new Vector3(0, 0, .5f));
        Mesh(new PlaneMesh { Size = new Vector2(70, 70) }, new Vector3(0, -.12f, 0), "96b5b3", true);
        Land(new Vector3(0, 0, 0), 5.6f, 3.8f, 0);
        // A quiet quay, a connected road, and equipment staged above the beach.
        Box(new Vector3(0, .24f, 0), new Vector3(7.4f, .03f, .52f), "d5c8a5");
        Box(new Vector3(2.8f, .24f, 1.4f), new Vector3(.52f, .03f, 3.2f), "d5c8a5");
        for (var i = 0; i < 9; i++)
            Box(new Vector3(2.8f, .16f, 3.1f + i * .29f), new Vector3(1.55f, .15f, .25f), "978366");
        foreach (var x in new[] { 2.15f, 3.45f })
            foreach (var z in new[] { 3.2f, 4.8f })
                Box(new Vector3(x, .03f, z), new Vector3(.12f, .8f, .12f), "605d50");
        Kit("lodging", new Vector3(-2.8f, .26f, -.7f), 1.8f, 10);
        Kit("lodging", new Vector3(-2.8f, .26f, -2.2f), 1.8f, 10);
        Kit("food", new Vector3(-.3f, .26f, -1.9f), 1.9f, -5);
        Kit("power", new Vector3(.1f, .26f, 1), 1.45f, 0);
        Kit("water", new Vector3(-1.8f, .26f, 1.2f), 1.6f, 0);
        Kit("fuel", new Vector3(-3.3f, .26f, 1.3f), 1.35f, 0);
        Kit("transport", new Vector3(1.9f, .26f, -.1f), 1.55f, 90);
        Kit("mhe", new Vector3(3.5f, .26f, 1.9f), 1.35f, 75);
        Kit("comms", new Vector3(2.3f, .26f, -2), 1.8f, 0);
        for (var i = 0; i < 11; i++)
        {
            var x = -4.5f + i * .78f; var z = -2.7f + Mathf.Sin(i * 1.8f) * .35f;
            Tree(new Vector3(x, .22f, z), .8f + (i % 3) * .2f);
        }
        Tree(new Vector3(-4.7f, .22f, -.5f), 1.1f);
        Tree(new Vector3(4.2f, .22f, -1), .9f);
        // A low, generic supply lighter alongside the pier. No flags, markings or status.
        var boat = new Node3D { Position = new Vector3(4.6f, -.04f, 4.1f), RotationDegrees = new Vector3(0, -8, 0) };
        world.AddChild(boat);
        Mesh(new PrismMesh { Size = new Vector3(1.2f, .35f, 2.5f) }, Vector3.Zero, "4c5c57", parent: boat);
        Box(new Vector3(0, .16f, 0), new Vector3(1.05f, .1f, 2.15f), "c7bb98", boat);
        Box(new Vector3(0, .46f, -.72f), new Vector3(.85f, .55f, .55f), "eee4c9", boat);
        Box(new Vector3(.43f, .5f, -.7f), new Vector3(.02f, .16f, .3f), "617b7a", boat);
        Box(new Vector3(0, .38f, .2f), new Vector3(.7f, .34f, .6f), "a46b49", boat);
        Box(new Vector3(0, .36f, .88f), new Vector3(.7f, .3f, .5f), "72816d", boat);
        for (var i = 0; i < 19; i++)
        {
            var x = -10 + i * 1.15f; var z = 6.3f + Mathf.Sin(i * 2.3f) * 2;
            Box(new Vector3(x, -.105f, z), new Vector3(.45f + (i % 4) * .2f, .004f, .025f), "c5d4c7");
        }
        Resized += RenderOnce; VisibilityChanged += RenderOnce;
        RenderOnce();

        void Kit(string capability, Vector3 position, float scale, float rotation)
        {
            var model = MapEquipmentModels.Instantiate(capability);
            model.Name = "TitleKit" + KitPieces;
            model.Position = position; model.Scale = Vector3.One * scale;
            model.RotationDegrees = new Vector3(0, rotation, 0);
            world.AddChild(model); KitPieces++;
        }
        void Tree(Vector3 p, float height)
        {
            Box(p + Vector3.Up * height * .3f, new Vector3(.1f, height * .6f, .1f), "776f50");
            Mesh(new CylinderMesh { TopRadius = 0, BottomRadius = height * .37f,
                Height = height, RadialSegments = 6 }, p + Vector3.Up * height * .75f, "71866a");
        }
        void Land(Vector3 center, float width, float depth, int variant)
        {
            Coast(center + Vector3.Down * .09f, width * 1.22f, depth * 1.22f, .02f, "a6c5ba");
            Coast(center + Vector3.Down * .05f, width * 1.08f, depth * 1.08f, .07f, "c9b58b");
            Coast(center, width, depth, .23f, "929f7c");
            void Coast(Vector3 origin, float rx, float rz, float height, string color)
            {
                using var surface = new SurfaceTool(); surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
                Vector3 Point(int i)
                {
                    var angle = i * Mathf.Tau / 36;
                    var radius = 1 + Mathf.Sin(angle * 3 + variant) * .08f + Mathf.Cos(angle * 5 - variant) * .06f;
                    return new Vector3(Mathf.Cos(angle) * rx * radius, height * .15f, Mathf.Sin(angle) * rz * radius);
                }
                for (var i = 0; i < 36; i++)
                {
                    var a = Point(i); var b = Point(i + 1);
                    var innerA = new Vector3(a.X * .86f, height, a.Z * .86f);
                    var innerB = new Vector3(b.X * .86f, height, b.Z * .86f);
                    surface.AddVertex(Vector3.Up * height); surface.AddVertex(innerA); surface.AddVertex(innerB);
                    surface.AddVertex(innerA); surface.AddVertex(a); surface.AddVertex(b);
                    surface.AddVertex(innerA); surface.AddVertex(b); surface.AddVertex(innerB);
                }
                surface.GenerateNormals(); Mesh(surface.Commit(), origin, color);
            }
        }
        void Box(Vector3 p, Vector3 size, string color, Node3D? parent = null) => Mesh(new BoxMesh { Size = size }, p, color, parent: parent);
        void Mesh(Godot.Mesh mesh, Vector3 p, string color, bool unshaded = false, Node3D? parent = null)
        {
            using (mesh)
            using (var material = new StandardMaterial3D { AlbedoColor = new Color(color), Roughness = 1,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                ShadingMode = unshaded ? BaseMaterial3D.ShadingModeEnum.Unshaded : BaseMaterial3D.ShadingModeEnum.PerPixel })
                (parent ?? world).AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = p });
        }
    }

    private async void RenderOnce()
    {
        if (viewport is null) return;
        var version = ++renderVersion;
        var size = new Vector2I(Math.Max(1, (int)Size.X), Math.Max(1, (int)Size.Y));
        if (!IsVisibleInTree() || hasSnapshot && snapshotSize == size)
        {
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled; return;
        }
        if (viewport.Size != size) viewport.Size = size;
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        if (DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || version != renderVersion) return;
        // A stable image also survives renderer target invalidation during a look change.
        using var pixels = viewport.GetTexture().GetImage();
        using var snapshot = ImageTexture.CreateFromImage(pixels);
        image.Texture = snapshot; snapshotSize = size; hasSnapshot = true;
    }
}
