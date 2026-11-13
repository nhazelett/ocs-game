using Godot;

namespace LinesOfSupport.Game;

public partial class TabletopMap3D
{
    private const int CoastSegments=40;
    private readonly Dictionary<string,Vector2[]> groundPads=[];
    internal int SceneryTriangles{get;private set;}
    internal SubViewport SceneryViewport=>world.GetParent<SubViewport>();

    private void ConfigureScenery()
    {
        var water=new Color(TabletopTheme.Style.Water);var night=TabletopTheme.Direction==1;
        // The drawn horizon is in the sea shader. A plain backdrop avoids
        // Compatibility sky radiance textures being retained on viewport teardown.
        using var environment=new Godot.Environment
        {
            BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=water.Lightened(night?.16f:.55f),
            AmbientLightSource=Godot.Environment.AmbientSource.Color,
            AmbientLightColor=night?new Color("a3bfce"):new Color("ece5d3"),AmbientLightEnergy=night?.27f:.35f
        };
        world.AddChild(new WorldEnvironment{Environment=environment});
        world.AddChild(new DirectionalLight3D{RotationDegrees=new Vector3(-52,-28,0),
            LightColor=new Color("fff0d1"),LightEnergy=.7f,ShadowEnabled=true,ShadowBlur=1.15f,ShadowOpacity=.72f});
        using var mesh=new PlaneMesh{Size=new Vector2(120,120),SubdivideWidth=24,SubdivideDepth=24};
        using var shader=GD.Load<Shader>("res://board/TabletopSea.gdshader");
        using var material=new ShaderMaterial{Shader=shader};
        material.SetShaderParameter("sea_color",TabletopTheme.Direction==2?new Color("628e9a"):water.Darkened(.18f));
        material.SetShaderParameter("sky_color",water.Lightened(night?.16f:.55f));
        material.SetShaderParameter("haze_color",water.Lightened(night?.26f:.72f));
        world.AddChild(new MeshInstance3D{Mesh=mesh,MaterialOverride=material,Position=new Vector3(0,-.15f,0),
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
        SceneryTriangles+=mesh.GetFaces().Length/3;
    }

    private static float CoastRadius(float radius,float angle,int variant)
        =>radius*(1.22f+.055f*Mathf.Sin(angle*3+variant)+.025f*Mathf.Cos(angle*5-variant));

    private MeshInstance3D SceneryMesh(ArrayMesh mesh,Vector3 point,bool transparent,Color tint)
    {
        using(mesh)
        {
            using var material=new StandardMaterial3D{VertexColorUseAsAlbedo=transparent,AlbedoColor=tint,Roughness=1,
                CullMode=BaseMaterial3D.CullModeEnum.Disabled};
            if(transparent)
            {
                material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
                material.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
            }
            var node=new MeshInstance3D{Mesh=mesh,MaterialOverride=material,Position=point};
            if(transparent)node.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            world.AddChild(node);SceneryTriangles+=mesh.GetFaces().Length/3;
            return node;
        }
    }

    private void BuildCoast(Vector3 point,float radius,int variant)
    {
        var land=TabletopTheme.Direction==2?new Color("9ba487"):new Color(TabletopTheme.Style.Land);
        var sand=TabletopTheme.Direction==1?land.Lightened(.13f):new Color("cbbd9a");
        var rock=land.Darkened(.3f);
        var outline=Enumerable.Range(0,CoastSegments).Select(i=>
        {
            var a=i*Mathf.Tau/CoastSegments;var r=radius*(1+.065f*Mathf.Sin(a*3+variant)+.035f*Mathf.Cos(a*5-variant));
            return new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r);
        }).ToList();
        // Preserve a natural outline while reserving real flat space for every
        // facility cell. No model foot can be left on a sloping beach.
        foreach(var site in board.Sites.Where(site=>site.Island==addingIsland))
        {
            var count=capabilityTiles.Count(t=>t.SiteId==site.Id);
            for(var i=0;i<count;i++)
            {
                var q=EquipmentPoint(sites[site.Id],i,count)-point;
                foreach(var x in new[]{-.5f,.5f})foreach(var z in new[]{-.5f,.5f})outline.Add(new Vector2(q.X+x,q.Z+z));
            }
        }
        var pad=Geometry2D.ConvexHull(outline.ToArray());var segments=pad.Length-1;
        Vector3 Ring(int i,int band)
        {
            var p=pad[i%segments];var a=Mathf.Atan2(p.Y,p.X);var inner=p.Length();
            var outer=Math.Max(CoastRadius(radius,a,variant),inner+radius*.1f);
            var r=band==0?inner:band==1?(inner+outer)/2:outer;
            return new Vector3(Mathf.Cos(a)*r,band==0?.77f:band==1?.25f:-.13f,Mathf.Sin(a)*r);
        }
        Aabb? groundBounds=null;
        for(var band=-1;band<2;band++)
        {
            using var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
            void Triangle(Vector3 a,Vector3 b,Vector3 c){st.AddVertex(a);st.AddVertex(b);st.AddVertex(c);}
            for(var i=0;i<segments;i++)
            {
                var j=(i+1)%segments;
                if(band<0)Triangle(new Vector3(0,.77f,0),Ring(j,0),Ring(i,0));
                else
                {
                    Triangle(Ring(i,band),Ring(j,band),Ring(i,band+1));
                    Triangle(Ring(j,band),Ring(j,band+1),Ring(i,band+1));
                }
            }
            st.GenerateNormals();
            var color=band<0?land:band==0?land.Lerp(sand,.65f):sand.Lerp(rock,.08f);
            var node=SceneryMesh(st.Commit(),point,false,color);
            var bounds=node.GetAabb();bounds.Position+=point;
            groundBounds=groundBounds is null?bounds:groundBounds.Value.Merge(bounds);
        }
        // Reserve the complete shore as the island's first scenery obstacle.
        var index=solidBounds.Count;
        solidBounds.Add(groundBounds!.Value);solidIslands.Add(addingIsland);
        solidSilhouettes[index]=Enumerable.Range(0,segments).SelectMany(i=>Enumerable.Range(0,3).Select(b=>point+Ring(i,b)))
            .Append(point+new Vector3(0,.77f,0)).ToArray();
        groundPads[addingIsland!]=pad.Select(p=>p+new Vector2(point.X,point.Z)).ToArray();
        BuildShallows(point,radius,variant,false);
    }

    private void BuildShallows(Vector3 point,float radius,int variant,bool reef)
    {
        var water=new Color(TabletopTheme.Style.Water);
        var pale=TabletopTheme.Direction==1?water.Lightened(.25f):water.Lightened(.6f);
        using var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        Vector3 P(int i,float scale)
        {
            var a=i*Mathf.Tau/CoastSegments;var r=CoastRadius(radius,a,variant)*scale;
            return new Vector3(Mathf.Cos(a)*r,-.09f,Mathf.Sin(a)*r*(reef?.55f:1));
        }
        void V(Vector3 p,float opacity){st.SetColor(new Color(1,1,1,opacity));st.AddVertex(p);}
        for(var i=0;i<CoastSegments;i++)
        {
            var j=(i+1)%CoastSegments;var inner=reef?.25f:1f;var outer=reef?1f:1.23f;
            if(reef)
            {
                V(new Vector3(0,-.09f,0),.35f);V(P(j,1),0);V(P(i,1),0);
            }
            else
            {
                V(P(i,inner),.5f);V(P(j,inner),.5f);V(P(i,outer),0);
                V(P(j,inner),.5f);V(P(j,outer),0);V(P(i,outer),0);
            }
        }
        st.GenerateNormals();SceneryMesh(st.Commit(),point,true,pale);
    }

    private void FinishScenery()
    {
        using var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        foreach(var model in EquipmentModels)
        {
            var bounds=MapEquipmentModels.Bounds(model);var island=model.GetMeta("ground_island").AsString();
            var center=new Vector2(bounds.GetCenter().X,bounds.GetCenter().Z);var pad=groundPads[island];
            Vector3 Edge(int i)
            {
                var a=i*Mathf.Tau/16;var p=center+new Vector2(Mathf.Cos(a)*bounds.Size.X*.65f,Mathf.Sin(a)*bounds.Size.Z*.65f);
                if(!Geometry2D.IsPointInPolygon(p,pad))
                {
                    var low=0f;var high=1f;
                    for(var n=0;n<8;n++)
                    {
                        var middle=(low+high)/2;
                        if(Geometry2D.IsPointInPolygon(center+(p-center)*middle,pad))low=middle;else high=middle;
                    }
                    p=center+(p-center)*low*.995f;
                }
                return new Vector3(p.X,.775f,p.Y);
            }
            for(var i=0;i<16;i++)
            {
                st.SetColor(new Color(1,1,1,.22f));st.AddVertex(new Vector3(center.X,.775f,center.Y));
                st.SetColor(new Color(1,1,1,0));st.AddVertex(Edge(i+1));st.AddVertex(Edge(i));
            }
        }
        st.GenerateNormals();SceneryMesh(st.Commit(),Vector3.Zero,true,new Color(0,0,0));
        if(board.Islands.Count==1)
        {
            var p=islands.Values.Single();
            BuildShallows(p+new Vector3(-5,0,-2),.85f,3,true);
            BuildShallows(p+new Vector3(5,0,1.6f),1.15f,7,true);
        }
    }

    internal bool ModelsHaveGround=>EquipmentModels.All(model=>
    {
        var polygon=groundPads[model.GetMeta("ground_island").AsString()];
        return MapEquipmentModels.Vertices(model).Where(p=>p.Y<.775f)
            .All(p=>Geometry2D.IsPointInPolygon(new Vector2(p.X,p.Z),polygon));
    });
}
