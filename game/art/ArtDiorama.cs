using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game.Art;

public partial class ArtDiorama : Control
{
    private SubViewport viewport = null!;
    private Camera3D camera = null!;
    private Node3D world = null!;
    private readonly List<(Label Label, Vector3 Point, Vector2 Offset)> labels = [];
    private bool dragging;
    internal int MeshCount { get; private set; }

    internal void Configure(ScenarioBoardData board, IReadOnlyList<CapabilityTile> tiles, ArtStudyStyle style, Font font)
    {
        ClipContents = true;
        var container = new SubViewportContainer { Stretch = true, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(container); container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        viewport = new SubViewport { Size = new Vector2I(786, 516), OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always, Msaa3D = Viewport.Msaa.Msaa4X };
        container.AddChild(viewport);
        world = new Node3D(); viewport.AddChild(world);
        var environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(style.Water),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("eae3ce"), AmbientLightEnergy = .25f };
        world.AddChild(new WorldEnvironment { Environment = environment });
        var light = new DirectionalLight3D { RotationDegrees = new Vector3(-52, -28, 0), LightColor = new Color("fff0d1"), LightEnergy = .65f,
            ShadowEnabled = true, DirectionalShadowMaxDistance = 50 };
        world.AddChild(light);
        camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 13.8f, Position = new Vector3(0, 14, 18), Current = true };
        world.AddChild(camera); camera.LookAt(new Vector3(0, 0, .4f));
        Mesh(new PlaneMesh { Size = new Vector2(50, 50) }, new Vector3(0, -.15f, 0), "8eafb2");
        var ordered = board.Islands.OrderByDescending(i => i.IsHub).ToArray();
        var centers = new[] { new Vector3(-4f, 0, 1.2f), new Vector3(3.4f, 0, -4.3f), new Vector3(3.4f, 0, 5f) };
        var points = new Dictionary<string, Vector3>();
        for (var i = 0; i < ordered.Length; i++) points[ordered[i].Id] = centers[i % centers.Length];
        foreach (var route in board.Links)
        {
            var from = points[route.From]; var to = points[route.To];
            var shift = route.Mode == "aircraft" ? new Vector3(0,0,-.6f) : Vector3.Zero;
            for (var i = 0; i < 14; i++)
            {
                var point = from.Lerp(to, i / 13f)+shift; point.Y = -.12f;
                Mesh(new CylinderMesh { TopRadius = .045f, BottomRadius = .045f, Height = .015f, RadialSegments = 8 }, point, "dbe1cd");
            }
            var midpoint=from.Lerp(to,.5f)+shift;
            var routeLabel=ArtDirections.Label(this,route.Mode=="aircraft"?"Air lane":"Barge lane",new Rect2(0,0,130,25),16,style.Ink,font,HorizontalAlignment.Center);
            labels.Add((routeLabel,midpoint,new Vector2(-65,route.Mode=="aircraft"?-20:3)));
        }
        foreach (var island in ordered)
        {
            var center = points[island.Id];
            Mesh(new CylinderMesh { TopRadius = 2.05f, BottomRadius = 2.35f, Height = .65f, RadialSegments = 9 }, center + new Vector3(0, .18f, 0), "c0a479");
            Mesh(new CylinderMesh { TopRadius = 1.9f, BottomRadius = 2.05f, Height = .16f, RadialSegments = 9 }, center + new Vector3(0, .58f, 0), "9ba487");
            Box(center + new Vector3(-1.65f, .07f, .65f), new Vector3(1.1f, .15f, .45f), "927451");
            Box(center + new Vector3(-.4f, .68f, .8f), new Vector3(2.2f, .035f, .22f), "c6bc9f");
            for (var tree = 0; tree < 4; tree++)
            {
                var p = center + new Vector3(-1.2f + tree * .78f, .7f, -1.25f + tree % 2 * .2f);
                Box(p + new Vector3(0, .3f, 0), new Vector3(.08f, .6f, .08f), "71674c");
                Mesh(new CylinderMesh { TopRadius = 0, BottomRadius = .37f, Height = .75f, RadialSegments = 5 }, p + new Vector3(0, .8f, 0), "72886c");
            }
            var site = board.Sites.First(s => s.Island == island.Id); var states = tiles.Where(t => t.SiteId == site.Id).ToArray();
            for (var i = 0; i < states.Length; i++)
                Equipment(center + new Vector3((i % 3 - 1) * .85f, .68f, i / 3 * .9f - .35f), states[i]);
            var anchor = camera.UnprojectPosition(center + new Vector3(0, .75f, 0));
            var primary = states.FirstOrDefault(t => t.CapabilityId == "power") ?? states[0]; var down = states.Any(t => t.Status == "down");
            var title = ArtDirections.Label(this, site.Name + " / " + island.Name, new Rect2(anchor.X - 175, anchor.Y + 66, 350, 30), 20, style.Ink, font, HorizontalAlignment.Center);
            var state = ArtDirections.Label(this, down ? $"{primary.Label} {primary.TodayPermille / 10m:0.#}% · support interrupted" : "Supported", new Rect2(anchor.X - 180, anchor.Y + 98, 360, 40), 16, down ? style.Alert : style.Ink, font, HorizontalAlignment.Center);
            labels.Add((title,center + new Vector3(0,.75f,0),new Vector2(-175,66))); labels.Add((state,center + new Vector3(0,.75f,0),new Vector2(-180,98)));
        }
    }
    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseButton b)
        {
            if(b.ButtonIndex==MouseButton.Left) dragging=b.Pressed;
            if(b.Pressed && b.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
                camera.Size=Math.Clamp(camera.Size*(b.ButtonIndex==MouseButton.WheelUp?.9f:1.1f),8,22);
        }
        if(input is InputEventMouseMotion m && dragging) world.Position+=new Vector3(m.Relative.X*.02f,0,m.Relative.Y*.025f);
    }
    public override void _Process(double delta)
    {
        if(camera is null) return;
        foreach(var item in labels) item.Label.Position=camera.UnprojectPosition(world.ToGlobal(item.Point))+item.Offset;
    }
    private void Equipment(Vector3 p, CapabilityTile tile)
    {
        var down = tile.Status == "down"; var color = down ? "59615a" : "d5cba3";
        switch (tile.CapabilityId)
        {
            case "power":
                Box(p + new Vector3(0,.22f,0),new Vector3(.58f,.42f,.4f),color);
                Box(p + new Vector3(.18f,.55f,.1f),new Vector3(.055f,.4f,.055f),"3f4949");
                for(var i=0;i<4;i++) Box(p + new Vector3(-.1f+i*.09f,.24f,.207f),new Vector3(.035f,.23f,.015f),"394e4b");
                Box(p + new Vector3(.2f,.22f,.21f),new Vector3(.07f,.07f,.025f),down ? "292f31" : "e3bd60"); break;
            case "fuel": case "water":
                Mesh(new CylinderMesh { TopRadius=.26f,BottomRadius=.26f,Height=.55f,RadialSegments=12 },p+new Vector3(0,.28f,0),down ? "616d67" : tile.CapabilityId=="fuel" ? "c0a36c" : "72949b"); break;
            case "lodging":
                var tent = Mesh(new PrismMesh { Size=new Vector3(.68f,.65f,.75f) },p+new Vector3(0,.33f,0),down ? "687065" : "e4d9b7");
                Box(p + new Vector3(0,.13f,.38f),new Vector3(.18f,.26f,.02f),"3e4e4a"); break;
            default:
                Box(p+new Vector3(0,.24f,0),new Vector3(.57f,.45f,.55f),color);
                Mesh(new PrismMesh { Size=new Vector3(.68f,.24f,.66f) },p+new Vector3(0,.57f,0),"7d8e77"); break;
        }
    }
    private void Box(Vector3 p, Vector3 size, string color) => Mesh(new BoxMesh { Size=size },p,color);
    private MeshInstance3D Mesh(Godot.Mesh mesh, Vector3 position, string color)
    {
        var material=new StandardMaterial3D { AlbedoColor=new Color(color),Roughness=1 };
        if(mesh is PlaneMesh) material.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
        var node = new MeshInstance3D { Mesh=mesh,Position=position,MaterialOverride=material };
        world.AddChild(node); MeshCount++; return node;
    }
}
