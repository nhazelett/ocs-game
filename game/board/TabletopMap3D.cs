using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

// Native presentation of existing graph/tile/link facts. No supply, route or failure rules.
public partial class TabletopMap3D:Control
{
    private Node3D world=null!;
    private Camera3D camera=null!;
    private float fittedSize;
    private readonly Dictionary<string,Vector3> islands=[],sites=[];
    private readonly List<(Label Label,Vector3 Point,Vector2 Offset)> captions=[];
    private readonly Dictionary<string,MeshInstance3D> selections=[];
    private readonly List<(Node3D Ship,Node3D Truck,Vector3 From,Vector3 Port,Vector3 To)> runs=[];
    private readonly List<(Label Label,Node3D Node,Line2D? Leader)> movingLabels=[];
    private readonly List<MeshInstance3D> changeRings=[];
    internal IReadOnlyList<string> ReportedChangeLabels=>movingLabels.Where(x=>x.Label.HasMeta("reported_change")).Select(x=>x.Label.Text).ToArray();
    internal IReadOnlyList<(Label Label,Node3D Node,Line2D? Leader)> FactLabels=>movingLabels;
    internal Dictionary<string,Label> SiteStatuses{get;}=[];
    internal IEnumerable<Label> CaptionLabels=>captions.Select(c=>c.Label).Concat(callouts.Where(c=>!c.Dynamic).Select(c=>c.Words));
    private readonly Dictionary<string,float> islandRadii=[];
    private ScenarioBoardData board=null!;
    private Node3D? motion;
    private string? factKey;
    private IReadOnlyList<CapabilityTile> capabilityTiles=[];
    internal int CapabilityObjects{get;private set;}
    internal int SiteCount=>sites.Count;
    internal Vector3 Pan=>world.Position;
    internal float CameraSize=>camera.Size;
    internal Vector2 SitePoint(string id)=>camera.UnprojectPosition(world.ToGlobal(sites[id]+new Vector3(0,.7f,0)));
    internal void Configure(ScenarioBoardData board,IReadOnlyList<CapabilityTile> tiles,IReadOnlyList<MapLink> links,Font font,IReadOnlySet<string>? confidence=null)
    {
        MouseFilter=MouseFilterEnum.Ignore;ClipContents=true;using var theme=new Theme{DefaultFont=font,DefaultFontSize=18};Theme=theme;
        capabilityTiles=tiles;this.board=board;
        var holder=new SubViewportContainer{Stretch=true,MouseFilter=MouseFilterEnum.Ignore};AddChild(holder);holder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var viewport=new SubViewport{OwnWorld3D=true,Size=new Vector2I((int)Size.X,(int)Size.Y),Msaa3D=Viewport.Msaa.Msaa4X,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};holder.AddChild(viewport);
        world=new Node3D();viewport.AddChild(world);
        using var environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color(TabletopTheme.Style.Water),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new Color("eae3ce"),AmbientLightEnergy=.25f};
        world.AddChild(new WorldEnvironment{Environment=environment});
        world.AddChild(new DirectionalLight3D{RotationDegrees=new Vector3(-52,-28,0),LightColor=new Color("fff0d1"),LightEnergy=.65f,ShadowEnabled=true});
        Mesh(new PlaneMesh{Size=new Vector2(100,100)},new Vector3(0,-.15f,0),"8eafb2",true);
        var ordered=board.Islands.OrderByDescending(i=>i.IsHub).ToArray();var spokes=ordered.Length-1;
        for(var i=0;i<ordered.Length;i++) islands[ordered[i].Id]=ordered.Length==1?Vector3.Zero:i==0?new Vector3(-4,0,0):new Vector3(3.5f,0,spokes==1?0:(i-1-(spokes-1)/2f)*9);
        var radii=new Dictionary<string,float>();
        foreach(var island in ordered)
        {
            var members=board.Sites.Where(s=>s.Island==island.Id).ToArray();var radius=2.15f;
            for(var i=0;i<members.Length;i++)
            {
                var rows=(tiles.Count(t=>t.SiteId==members[i].Id)+2)/3;radius=Math.Max(radius,rows*.48f+1);
                sites[members[i].Id]=islands[island.Id]+new Vector3((i-(members.Length-1)/2f)*4.8f,0,0);
            }
            radius+=(members.Length-1)*2.4f;radii[island.Id]=radius;islandRadii[island.Id]=radius;
            var p=islands[island.Id];var variant=island.Id.Sum(c=>c)%13;
            Mesh(Island(radius,.65f,variant),p,"c0a479");Mesh(Island(radius*.9f,.12f,variant),p+new Vector3(0,.65f,0),"9ba487");
            for(var t=0;t<4;t++)
            {
                var q=p+new Vector3(-1.15f+t*.75f,.77f,-radius*.65f);
                Box(q+new Vector3(0,.25f,0),new Vector3(.08f,.5f,.08f),"71674c");Mesh(new CylinderMesh{TopRadius=0,BottomRadius=.3f,Height=.7f,RadialSegments=5},q+new Vector3(0,.7f,0),"72886c");
            }
            Box(p+new Vector3(-radius,.1f,.5f),new Vector3(.9f,.16f,.4f),"927451");
        }
        var minX=islands.Min(p=>p.Value.X-radii[p.Key]);var maxX=islands.Max(p=>p.Value.X+radii[p.Key]);
        var minZ=islands.Min(p=>p.Value.Z-radii[p.Key]);var maxZ=islands.Max(p=>p.Value.Z+radii[p.Key]);var center=new Vector3((minX+maxX)/2,0,(minZ+maxZ)/2);
        camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=Math.Max(9,Math.Max((maxZ-minZ)*.62f+4,(maxX-minX)/Math.Max(.5f,Size.X/Size.Y)+3)),Position=center+new Vector3(0,14,18),Current=true};viewport.AddChild(camera);camera.LookAt(center);fittedSize=camera.Size;
        foreach(var route in board.Links)
        {
            var a=islands[route.From];var b=islands[route.To];var mode=route.Mode=="aircraft"?"Air":route.Mode=="barge"?"Barge":"Transport";
            var state=links.FirstOrDefault(l=>l.FromIslandId==route.From&&l.ToIslandId==route.To&&l.Mode==route.Mode);var closed=state?.Open==false;
            var shift=route.Mode=="aircraft"?new Vector3(0,0,-.7f):Vector3.Zero;
            for(var i=0;i<18;i++){var p=a.Lerp(b,i/17f)+shift;p.Y=-.12f;Mesh(new CylinderMesh{TopRadius=.04f,BottomRadius=.04f,Height=.02f,RadialSegments=6},p,closed?"9f4937":"e2e4d0",true);}
            LaneCaption(mode+" lane"+(closed?" · closed":""),(a+b)/2+shift,closed);
        }
        foreach(var site in board.Sites)
        {
            var p=sites[site.Id];var values=tiles.Where(t=>t.SiteId==site.Id).ToArray();var rows=(values.Length+2)/3;
            for(var i=0;i<values.Length;i++)
            {
                var q=p+new Vector3((i%3-1)*.85f,.77f,(i/3-(rows-1)/2f)*.85f);Equipment(q,values[i]);CapabilityObjects++;
                if(values[i].Status=="down")Caption("×",q+new Vector3(0,.8f,0),new Vector2(-12,-15),24,24,TabletopTheme.Alert);
            }
            selections[site.Id]=Mesh(new TorusMesh{InnerRadius=1.8f,OuterRadius=1.87f,Rings=32,RingSegments=8},p+new Vector3(0,.79f,0),"556e65",true);selections[site.Id].Visible=false;
            var state=SiteStatus(values,site.OpensDay);
            var title=board.Sites.Count(s=>s.Island==site.Island)==1?site.Name+" / "+board.Islands.Single(i=>i.Id==site.Island).Name:site.Name;
            SiteStatuses[site.Id]=SiteCaption(site.Name,title,state+(confidence?.Contains(site.Id)==true?" · confidence lost":""),p+new Vector3(0,.75f,0),values.Any(t=>t.Status=="down"));
        }
    }
    private static string SiteStatus(IReadOnlyList<CapabilityTile> values,int opensDay)
    {
        if(values.Count==0)return "Not assessed";
        if(values.All(t=>t.Status=="notOpen"))return $"Opens day {opensDay}";
        string Names(string status)
        {
            var names=values.Where(t=>t.Status==status).Select(t=>t.Label.ToLowerInvariant()).ToArray();
            return names.Length<2?string.Join("",names):string.Join(", ",names[..^1])+" and "+names[^1];
        }
        var clauses=new List<string>();
        if(values.Any(t=>t.Status=="down"))clauses.Add(Names("down")+" down");
        if(values.Any(t=>t.Status=="degraded"))clauses.Add(Names("degraded")+" partly supported");
        var text=clauses.Count==0?"Supported":string.Join("; ",clauses);
        return char.ToUpperInvariant(text[0])+text[1..];
    }
    internal void Select(string? id){foreach(var pair in selections)pair.Value.Visible=pair.Key==id;}
    internal void Fit(){world.Position=Vector3.Zero;camera.Size=fittedSize;}
    internal void Restore(Vector3 pan,float size){world.Position=pan;camera.Size=size;}
    internal void PanBy(Vector2 amount)=>world.Position+=new Vector3(amount.X*camera.Size/Size.Y,0,amount.Y*camera.Size/Size.Y*1.6f);
    internal void ZoomAt(Vector2 anchor,float factor)
    {
        Vector3 Ground(){var p=camera.ProjectRayOrigin(anchor);var d=camera.ProjectRayNormal(anchor);return p+d*((-p.Y)/d.Y);}
        var before=Ground();camera.Size=Math.Clamp(camera.Size/factor,5,50);world.Position+=Ground()-before;
    }
    public override void _Process(double delta)
    {
        if(camera is null)return;
        foreach(var item in captions)item.Label.Position=camera.UnprojectPosition(world.ToGlobal(item.Point))+item.Offset;
        LayoutCallouts();
    }

    internal void ShowFacts(MapDayView? day,float progress)
    {
        if(day is null)return;
        var key=day.Day+string.Join(";",day.Runs.Select(r=>r.ToString()))+string.Join(";",day.Held.Select(h=>h.ToString()))+string.Join(";",day.Shipments.Select(s=>s.ToString()))+string.Join(";",day.Changes.Where(c=>c.Known));
        if(key!=factKey)
        {
            if(motion is not null){world.RemoveChild(motion);motion.QueueFree();}
            ClearMovingCallouts();runs.Clear();changeRings.Clear();
            motion=new Node3D();world.AddChild(motion);factKey=key;
            foreach(var run in day.Runs)
            {
                if(!islands.TryGetValue(run.FromIslandId,out var a)||!sites.TryGetValue(run.SiteId,out var b))continue;
                var ship=Vehicle(run.Mode);var truck=Vehicle("truck");runs.Add((ship,truck,a,islands[run.ToIslandId]+new Vector3(-2.3f,.05f,.5f),b));
                var description=(capabilityTiles.FirstOrDefault(t=>t.CapabilityId==run.CapabilityId)?.Label??"Supplies")+$" · {run.Quantity:N0} {run.Unit}";
                MovingLabel(ship,description);MovingLabel(truck,description);
            }
            foreach(var hold in day.Held.GroupBy(h=>(h.FromIslandId,h.ToIslandId,h.SiteId,h.CapabilityId,h.LinkId,h.Reason)))
            {
                if(!islands.TryGetValue(hold.Key.FromIslandId,out var p)||!islands.TryGetValue(hold.Key.ToIslandId,out var destination))continue;
                var direction=(destination-p).Normalized();
                var crate=Vehicle("cargo");crate.Position=p+direction*islandRadii[hold.Key.FromIslandId]+new Vector3(0,.25f,0);
                var capability=capabilityTiles.FirstOrDefault(t=>t.CapabilityId==hold.Key.CapabilityId)?.Label??"Supplies";
                var site=board.Sites.FirstOrDefault(s=>s.Id==hold.Key.SiteId)?.Name??"the site";
                var mode=day.Links.FirstOrDefault(l=>l.Id==hold.Key.LinkId)?.Mode;
                var lane=mode switch{"barge" or "sealift"=>"barge lane","aircraft" or "airlift"=>"air lane",_=>"route"};
                var reason=hold.Key.Reason=="closed"?lane+" closed":"no scheduled service today";
                MovingLabel(crate,$"{capability} for {site} waiting: {reason}.",true);
            }
            foreach(var transit in day.Shipments)
            {
                if(!sites.TryGetValue(transit.SiteId,out var p))continue;
                var carrier=Vehicle(transit.Mode);carrier.Position=p+new Vector3(-3,.5f,-1);
                MovingLabel(carrier,transit.CustomsHeld?"Held in customs":$"On its way · due day {transit.ArrivalDay}");
            }
            foreach(var group in day.Changes.Where(c=>c.Known).GroupBy(c=>c.SiteId))
            {
                if(!sites.TryGetValue(group.Key,out var p))continue;
                var ring=Mesh(new TorusMesh{InnerRadius=1.6f,OuterRadius=1.66f,Rings=32,RingSegments=6},p+Vector3.Up*.81f,group.All(c=>c.Kind=="restored")?"355d48":"9f4937",true,motion);
                changeRings.Add(ring);
                var marker=new Node3D{Position=p+Vector3.Up*1.1f};motion.AddChild(marker);
                var changeWords=string.Join("\n",group.Select(change=>
                {
                    var name=capabilityTiles.FirstOrDefault(t=>t.CapabilityId==change.CapabilityId)?.Label??"Support";
                    return change.Kind=="restored"?$"{name} restored":change.Kind=="vendorFailure"?$"{name}: supplier failure reported":$"{name} {change.Permille/10m:0.#}% supplied";
                }));
                var label=SiteStatuses[group.Key];label.Text=changeWords;label.SetMeta("reported_change",true);
                movingLabels.Add((label,marker,null));
            }
        }
        foreach(var run in runs)
        {
            run.Ship.Visible=progress<.72f;run.Truck.Visible=progress>=.72f;
            run.Ship.Position=run.From.Lerp(run.Port,Math.Min(1,progress/.72f))+Vector3.Up*.15f;
            run.Truck.Position=run.Port.Lerp(run.To,Math.Clamp((progress-.72f)/.28f,0,1))+Vector3.Up*.9f;
        }
        foreach(var label in movingLabels)label.Label.Visible=label.Node.Visible;
        foreach(var ring in changeRings)ring.Scale=Vector3.One*(1+Mathf.Sin(progress*Mathf.Tau*2)*.06f);
    }
    private Node3D Vehicle(string mode)
    {
        var root=new Node3D();motion!.AddChild(root);
        void Part(Vector3 point,Vector3 size,string color)=>Mesh(new BoxMesh{Size=size},point,color,parent:root);
        if(mode is "aircraft" or "airlift"){Part(Vector3.Zero,new Vector3(.9f,.12f,.15f),"e6d8ba");Part(new Vector3(0,0,0),new Vector3(.16f,.07f,.9f),"e6d8ba");}
        else if(mode=="truck"){Part(Vector3.Zero,new Vector3(.7f,.25f,.3f),"bc9f65");Part(new Vector3(.35f,.02f,0),new Vector3(.2f,.3f,.32f),"6f8071");}
        else if(mode=="cargo")Part(Vector3.Zero,new Vector3(.4f,.3f,.35f),"c3a267");
        else{Part(Vector3.Zero,new Vector3(1.1f,.16f,.45f),"4a5c60");Part(new Vector3(-.1f,.2f,0),new Vector3(.7f,.25f,.34f),"c3a267");}
        return root;
    }
    private void MovingLabel(Node3D node,string text,bool anchored=false)
    {
        var callout=PaperCallout(text,()=>node.GlobalPosition,()=>node.Visible,anchored?180:220,anchored?1:0,true);
        movingLabels.Add((callout.Words,node,anchored?callout.Pointer:null));
    }

    private Label Caption(string text,Vector3 point,Vector2 offset,float width,int size,Color color)
    {
        var label=new Label{Size=new Vector2(width,54),AutowrapMode=TextServer.AutowrapMode.WordSmart,Text=text,HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=MouseFilterEnum.Ignore};
        label.AddThemeFontSizeOverride("font_size",size);label.AddThemeColorOverride("font_color",color);AddChild(label);label.Size=new Vector2(width,54);captions.Add((label,point,offset));return label;
    }
    private void Equipment(Vector3 p,CapabilityTile tile)
    {
        var dark=tile.Status is "down" or "notOpen";var color=dark?"61675d":"d5cba3";
        if(tile.CapabilityId=="power")
        {
            Box(p+new Vector3(0,.22f,0),new Vector3(.58f,.44f,.4f),color);Box(p+new Vector3(.18f,.5f,0),new Vector3(.05f,.4f,.05f),"3f4949");
            for(var i=0;i<4;i++)Box(p+new Vector3(-.16f+i*.09f,.22f,.21f),new Vector3(.035f,.23f,.015f),"394e4b");
            Box(p+new Vector3(.21f,.22f,.21f),new Vector3(.06f,.06f,.02f),dark?"292f31":"e3bd60");
        }
        else if(tile.CapabilityId is "fuel" or "water")Mesh(new CylinderMesh{TopRadius=.25f,BottomRadius=.25f,Height=.55f,RadialSegments=12},p+new Vector3(0,.28f,0),dark?"61675d":tile.CapabilityId=="fuel"?"c0a36c":"72949b");
        else if(tile.CapabilityId=="lodging"){Mesh(new PrismMesh{Size=new Vector3(.65f,.65f,.75f)},p+new Vector3(0,.33f,0),color);Box(p+new Vector3(0,.12f,.38f),new Vector3(.16f,.24f,.02f),"3e4e4a");}
        else{Box(p+new Vector3(0,.22f,0),new Vector3(.55f,.44f,.55f),color);Mesh(new PrismMesh{Size=new Vector3(.65f,.25f,.65f)},p+new Vector3(0,.57f,0),dark?"626b5f":"7d8e77");}
    }
    private static ArrayMesh Island(float radius,float height,int variant)
    {
        using var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        Vector3 Point(int i,float y,float scale=1){var a=i*Mathf.Tau/14;var r=radius*(1+.08f*Mathf.Sin(i*2.6f+variant)+.06f*Mathf.Cos(i*1.8f+variant));return new Vector3(Mathf.Cos(a)*r*scale,y,Mathf.Sin(a)*r*scale);}
        for(var i=0;i<14;i++)
        {
            var a=Point(i,height);var b=Point((i+1)%14,height);var c=Point(i,0,1.12f);var d=Point((i+1)%14,0,1.12f);
            foreach(var v in new[]{new Vector3(0,height,0),b,a,a,b,c,b,d,c})st.AddVertex(v);
        }
        st.GenerateNormals();return st.Commit();
    }
    private void Box(Vector3 point,Vector3 size,string color)=>Mesh(new BoxMesh{Size=size},point,color);
    private static Color LookColor(string color)
    {
        if(TabletopTheme.Direction==2)return new Color(color);
        var style=TabletopTheme.Style;
        return color switch
        {
            "8eafb2" => new Color(style.Water),
            "c0a479" => new Color(style.Land).Darkened(.16f),
            "9ba487" => new Color(style.Land),
            "9f4937" => TabletopTheme.Alert,
            "556e65" => TabletopTheme.Moss,
            "e2e4d0" => TabletopTheme.Muted,
            _ => new Color(color)
        };
    }
    private MeshInstance3D Mesh(Godot.Mesh mesh,Vector3 point,string color,bool unshaded=false,Node3D? parent=null)
    {
        using(mesh)
        {
            using var material=new StandardMaterial3D{AlbedoColor=LookColor(color),Roughness=1,CullMode=BaseMaterial3D.CullModeEnum.Disabled};if(unshaded)material.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
            if(!unshaded&&parent is null)solidBounds.Add(new Aabb(mesh.GetAabb().Position+point,mesh.GetAabb().Size));
            var result=new MeshInstance3D{Mesh=mesh,Position=point,MaterialOverride=material};(parent??world).AddChild(result);return result;
        }
    }
}
