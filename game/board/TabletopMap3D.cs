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
    private readonly List<(Node3D Ship,Node3D Truck,Vector3 From,Vector3 Port,Vector3 To,bool Air)> runs=[];
    private readonly List<(Label Label,Node3D Node,Line2D? Leader)> movingLabels=[];
    private readonly List<MeshInstance3D> changeRings=[];
    internal List<(Control Marker,Node3D Model)> DownMarkers{get;}=[];
    internal List<Line2D> DownStems{get;}=[];
    internal List<Node3D> EquipmentModels{get;}=[];
    private readonly List<Vector3[]> equipmentExtents=[];
    private readonly List<MeshInstance3D> routeDots=[];
    internal IEnumerable<Rect2> ModelScreenBounds=>equipmentExtents.Select(vertices=>
    {
        var points=vertices.Select(v=>camera.UnprojectPosition(world.ToGlobal(v))).ToArray();
        var min=new Vector2(points.Min(p=>p.X),points.Min(p=>p.Y));
        return new Rect2(min,new Vector2(points.Max(p=>p.X),points.Max(p=>p.Y))-min);
    });
    internal IEnumerable<Vector2> ModelScreenPeaks=>equipmentExtents.Select(points=>camera.UnprojectPosition(world.ToGlobal(points[2])));
    internal IReadOnlyList<string> ReportedChangeLabels=>movingLabels.Where(x=>x.Label.HasMeta("reported_change")).Select(x=>x.Label.Text).ToArray();
    internal IReadOnlyList<(Label Label,Node3D Node,Line2D? Leader)> FactLabels=>movingLabels;
    internal Dictionary<string,Label> SiteStatuses{get;}=[];
    internal IEnumerable<Label> CaptionLabels=>captions.Where(c=>c.Label.Visible).Select(c=>c.Label).Concat(callouts.Where(c=>!c.Dynamic).SelectMany(c=>new[]{c.Heading,c.Words,c.ShortWords}).OfType<Label>().Where(l=>l.IsVisibleInTree()));
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
        ConfigureScenery();
        var ordered=board.Islands.OrderByDescending(i=>i.IsHub).ToArray();var spokes=ordered.Length-1;
        for(var i=0;i<ordered.Length;i++) islands[ordered[i].Id]=ordered.Length==1?Vector3.Zero:i==0?new Vector3(-4,0,0):new Vector3(3.5f,0,spokes==1?0:(i-1-(spokes-1)/2f)*9);
        var radii=new Dictionary<string,float>();
        foreach(var island in ordered)
        {
            addingIsland=island.Id;var members=board.Sites.Where(s=>s.Island==island.Id).ToArray();var radius=2.15f;
            for(var i=0;i<members.Length;i++)
            {
                var rows=(tiles.Count(t=>t.SiteId==members[i].Id)+3)/4;radius=Math.Max(radius,2.8f+(rows-2)*.65f);
                sites[members[i].Id]=islands[island.Id]+new Vector3((i-(members.Length-1)/2f)*4.8f,0,0);
            }
            radius+=(members.Length-1)*2.4f;radii[island.Id]=radius;islandRadii[island.Id]=radius;
            var p=islands[island.Id];var variant=island.Id.Sum(c=>c)%13;
            BuildCoast(p,radius,variant);
            for(var t=0;t<4;t++)
            {
                var q=p+new Vector3(-1.15f+t*.75f,.77f,-radius*.65f);
                Box(q+new Vector3(0,.25f,0),new Vector3(.08f,.5f,.08f),"71674c");Mesh(new CylinderMesh{TopRadius=0,BottomRadius=.3f,Height=.7f,RadialSegments=5},q+new Vector3(0,.7f,0),"72886c");
            }
            Box(p+new Vector3(-radius*1.18f,.08f,.5f),new Vector3(.95f,.08f,.4f),"927451");
            foreach(var z in new[]{.35f,.65f})Box(p+new Vector3(-radius*1.3f,-.03f,z),new Vector3(.07f,.22f,.07f),"71674c");
        }
        addingIsland=null;
        var minX=islands.Min(p=>p.Value.X-radii[p.Key]);var maxX=islands.Max(p=>p.Value.X+radii[p.Key]);
        var minZ=islands.Min(p=>p.Value.Z-radii[p.Key]);var maxZ=islands.Max(p=>p.Value.Z+radii[p.Key]);var center=new Vector3((minX+maxX)/2,0,(minZ+maxZ)/2);
        camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=Math.Max(9,Math.Max((maxZ-minZ)*.62f+4,(maxX-minX)/Math.Max(.5f,Size.X/Size.Y)+3)),Position=center+new Vector3(0,14,18),Current=true};viewport.AddChild(camera);camera.LookAt(center);fittedSize=camera.Size;
        foreach(var route in board.Links)
        {
            var a=islands[route.From];var b=islands[route.To];var mode=route.Mode=="aircraft"?"Air":route.Mode=="barge"?"Barge":"Transport";
            var state=links.FirstOrDefault(l=>l.FromIslandId==route.From&&l.ToIslandId==route.To&&l.Mode==route.Mode);var closed=state?.Open==false;
            // Distinct paths: the air route rises above the water; the barge stays on it.
            Vector3 PathPoint(float t)=>a.Lerp(b,t)+Vector3.Up*(route.Mode=="aircraft"?Mathf.Sin(t*Mathf.Pi)*2.2f:-.12f);
            for(var i=0;i<28;i++){var p=PathPoint(i/27f);routeDots.Add(Mesh(new CylinderMesh{TopRadius=.04f,BottomRadius=.04f,Height=.02f,RadialSegments=6},p,closed?"9f4937":"e2e4d0",true));}
            laneAnchors.Add((route.From,route.To,route.Mode,PathPoint(.5f)));
            LaneCaption(mode+" lane"+(closed?" · closed":""),PathPoint(.5f),closed);
        }
        foreach(var site in board.Sites)
        {
            addingIsland=site.Island;var p=sites[site.Id];var values=tiles.Where(t=>t.SiteId==site.Id).ToArray();
            for(var i=0;i<values.Length;i++)
            {
                var q=EquipmentPoint(p,i,values.Length);Equipment(q,values[i]);CapabilityObjects++;
                if(values[i].Status=="down")
                {
                    var marker=new MapDownMarker{Size=new Vector2(24,24),MouseFilter=MouseFilterEnum.Ignore};
                    AddChild(marker);DownMarkers.Add((marker,EquipmentModels[^1]));
                    var stem=new Line2D{Width=1.5f,DefaultColor=TabletopTheme.Alert,Antialiased=true};AddChild(stem);DownStems.Add(stem);
                }
            }
            selections[site.Id]=Mesh(new TorusMesh{InnerRadius=2.3f,OuterRadius=2.37f,Rings=32,RingSegments=8},p+new Vector3(0,.79f,0),"556e65",true);selections[site.Id].Visible=false;
            if(values.Any(t=>t.Status=="down"))
                Mesh(new TorusMesh{InnerRadius=2.3f,OuterRadius=2.37f,Rings=32,RingSegments=8},p+new Vector3(0,.8f,0),"9f4937",true);
            var state=SiteStatus(values,site.OpensDay);
            var title=board.Sites.Count(s=>s.Island==site.Island)==1?site.Name+" / "+board.Islands.Single(i=>i.Id==site.Island).Name:site.Name;
            // Attach at a free shore edge so every camera leaves room for facts.
            SiteStatuses[site.Id]=SiteCaption(site.Name,title,state+(confidence?.Contains(site.Id)==true?" · confidence lost":""),()=>SiteShoreAnchor(site.Island),values.Any(t=>t.Status=="down"),site.Island);
            SiteStatuses[site.Id].SetMeta("confidence_lost",confidence?.Contains(site.Id)==true);
        }
        FinishScenery();addingIsland=null;
    }
    private static string SiteStatus(IReadOnlyList<CapabilityTile> values,int opensDay)
    {
        if(values.Count==0)return "Not assessed";
        if(values.All(t=>t.Status=="notOpen"))return $"Opens day {opensDay}";
        string Names(string status)
        {
            return CapabilityNames(values.Where(t=>t.Status==status).Select(t=>t.Label));
        }
        var clauses=new List<string>();
        if(values.Any(t=>t.Status=="down"))clauses.Add(Names("down")+" down");
        if(values.Any(t=>t.Status=="degraded"))clauses.Add(Names("degraded")+" partly supported");
        var text=clauses.Count==0?"Supported":string.Join("; ",clauses);
        return char.ToUpperInvariant(text[0])+text[1..];
    }
    private static string CapabilityNames(IEnumerable<string> labels)
    {
        var names=labels.OrderBy(n=>n.Equals("power",StringComparison.OrdinalIgnoreCase)?0:1)
            .Select(n=>n.ToLowerInvariant()).ToArray();
        return names.Length<2?string.Join("",names):string.Join(", ",names[..^1])+" and "+names[^1];
    }
    private static string SentenceCase(string text)=>text.Length==0?text:char.ToUpperInvariant(text[0])+text[1..];
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
        var peaks=ModelScreenPeaks.ToArray();
        for(var i=0;i<DownMarkers.Count;i++)
        {
            var item=DownMarkers[i];var top=peaks[EquipmentModels.IndexOf(item.Model)];
            // A badge marks only the model directly below it. Equipment rows are
            // separated to leave this short vertical stem clear of their neighbours.
            item.Marker.Position=top-new Vector2(12,31);
            DownStems[i].Points=[top,top-new Vector2(0,7)];
        }
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
                var ship=Vehicle(run.Mode);var truck=Vehicle("truck");runs.Add((ship,truck,a,islands[run.ToIslandId]+new Vector3(-2.3f,.05f,.5f),b,run.Mode is "aircraft" or "airlift"));
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
                MovingLabel(crate,$"{capability} for {site} waiting: {reason}.",true,hold.Key.FromIslandId);
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
                if(!capabilityTiles.Any(t=>t.SiteId==group.Key&&t.Status=="down"))
                {
                    var ring=Mesh(new TorusMesh{InnerRadius=1.6f,OuterRadius=1.66f,Rings=32,RingSegments=6},p+Vector3.Up*.81f,group.All(c=>c.Kind=="restored")?"355d48":"9f4937",true,motion);
                    changeRings.Add(ring);
                }
                var marker=new Node3D{Position=p+Vector3.Up*1.1f};motion.AddChild(marker);
                string Report(MapChange change)
                {
                    var name=capabilityTiles.FirstOrDefault(t=>t.CapabilityId==change.CapabilityId)?.Label??"Support";
                    return change.Kind=="restored"?$"{name} restored":change.Kind=="vendorFailure"?$"{name}: supplier failure reported":$"{name} {change.Permille/10m:0.#}% supplied";
                }
                var changeWords=string.Join("\n",group.Select(Report));
                var label=SiteStatuses[group.Key];
                var stillShort=capabilityTiles.Where(t=>t.SiteId==group.Key&&!group.Any(c=>c.CapabilityId==t.CapabilityId)
                    &&t.Status is "down" or "degraded").Select(t=>t.Label+(t.Status=="down"?" down":" partly supported"));
                var confidence=label.GetMeta("confidence_lost").AsBool()?new[]{"Commander confidence lost"}:Array.Empty<string>();
                label.Text=string.Join("\n",new[]{changeWords}.Concat(stillShort).Concat(confidence));label.SetMeta("reported_change",true);
                var summary=group.GroupBy(c=>(c.Kind,c.Permille)).Select(reports=>
                {
                    var name=SentenceCase(CapabilityNames(reports.Select(c=>capabilityTiles.FirstOrDefault(t=>t.CapabilityId==c.CapabilityId)?.Label??"Support")));
                    return reports.Key.Kind=="restored"?$"{name} restored":reports.Key.Kind=="vendorFailure"?$"{name}: supplier failure reported"
                        :reports.Key.Permille==0?$"{name} down":$"{name}\n{reports.Key.Permille/10m:0.#}% supplied";
                });
                callouts.Single(c=>c.Words==label).ShortWords!.Text=string.Join("\n",summary.Concat(stillShort).Concat(confidence));
                movingLabels.Add((label,marker,null));
            }
        }
        foreach(var run in runs)
        {
            run.Ship.Visible=progress<.72f;run.Truck.Visible=progress>=.72f;
            var crossing=Math.Min(1,progress/.72f);
            run.Ship.Position=run.From.Lerp(run.Port,crossing)+Vector3.Up*(run.Air?Mathf.Sin(crossing*Mathf.Pi)*2.2f:.15f);
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
    private void MovingLabel(Node3D node,string text,bool anchored=false,string? island=null)
    {
        var callout=PaperCallout(anchored?text.Replace(": ",":\n"):text,()=>node.GlobalPosition,()=>node.Visible,anchored?180:220,anchored?1:0,true);
        callout.Island=island;movingLabels.Add((callout.Words,node,anchored?callout.Pointer:null));
    }

    private Label Caption(string text,Vector3 point,Vector2 offset,float width,int size,Color color)
    {
        var label=new Label{Size=new Vector2(width,54),AutowrapMode=TextServer.AutowrapMode.WordSmart,Text=text,HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=MouseFilterEnum.Ignore};
        label.AddThemeFontSizeOverride("font_size",size);label.AddThemeColorOverride("font_color",color);AddChild(label);label.Size=new Vector2(width,54);captions.Add((label,point,offset));return label;
    }
    private static Vector3 EquipmentPoint(Vector3 center,int index,int count)
        =>center+new Vector3((index%4-1.5f)*1.1f,.77f,(index/4-((count+3)/4-1)/2f)*2.6f);
    private void Equipment(Vector3 p,CapabilityTile tile)
    {
        var model=MapEquipmentModels.Instantiate(tile.CapabilityId);
        model.SetMeta("ground_island",addingIsland??"");
        model.SetMeta("capability_status",tile.Status);model.SetMeta("capability_id",tile.CapabilityId);
        model.Position=p;world.AddChild(model);EquipmentModels.Add(model);
        var bounds=MapEquipmentModels.Bounds(model);
        // Camera orientation is fixed. Cache the four real silhouette extrema
        // once; pan/zoom only reproject these points, not every mesh vertex.
        var projected=MapEquipmentModels.Vertices(model).Select(p=>(Point:p,Screen:camera.UnprojectPosition(world.ToGlobal(p)))).ToArray();
        equipmentExtents.Add([projected.MinBy(p=>p.Screen.X).Point,projected.MaxBy(p=>p.Screen.X).Point,
            projected.MinBy(p=>p.Screen.Y).Point,projected.MaxBy(p=>p.Screen.Y).Point]);
        solidBounds.Add(bounds);solidIslands.Add(addingIsland);
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
            if(!unshaded&&parent is null){solidBounds.Add(new Aabb(mesh.GetAabb().Position+point,mesh.GetAabb().Size));solidIslands.Add(addingIsland);}
            var result=new MeshInstance3D{Mesh=mesh,Position=point,MaterialOverride=material};(parent??world).AddChild(result);return result;
        }
    }
}
