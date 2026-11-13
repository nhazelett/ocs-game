using Godot;

namespace LinesOfSupport.Game;

public partial class TabletopMap3D
{
    private sealed record MapCallout(PanelContainer Card, Label Words, Line2D Pointer,
        Func<Vector3> Anchor, Func<bool> IsShown, int Priority, bool Dynamic);
    private readonly List<MapCallout> callouts=[];
    private readonly List<Aabb> solidBounds=[];
    private readonly List<Vector3> closedLanes=[];
    private readonly Dictionary<Label,string> originalStatuses=[];
    private readonly List<Line2D> laneBarriers=[];
    private string? calloutLayoutKey;
    internal float BottomOcclusion{get;set;}
    internal bool CalloutLayoutFits{get;private set;}
    internal IEnumerable<Control> CalloutCards=>callouts.Where(c=>c.Card.Visible).Select(c=>(Control)c.Card);
    internal IEnumerable<Line2D> CalloutPointers=>callouts.Where(c=>c.Card.Visible).Select(c=>c.Pointer);
    internal IReadOnlyList<Line2D> ClosedLaneBarriers=>laneBarriers;
    internal IReadOnlyList<Vector2[]> ProjectedSolids=>solidBounds.Select(Project).ToArray();

    private Label SiteCaption(string title,string location,string status,Vector3 point,bool alert)
    {
        var item=PaperCallout(status,()=>world.ToGlobal(point),()=>true,190,0,false,alert);
        originalStatuses[item.Words]=status;item.Card.TooltipText=location;
        var heading=new Label{Text=title+(alert?" · down":""),AutowrapMode=TextServer.AutowrapMode.WordSmart,MouseFilter=MouseFilterEnum.Ignore};
        heading.AddThemeFontSizeOverride("font_size",20);
        heading.AddThemeColorOverride("font_color",TabletopTheme.Ink);
        var column=item.Words.GetParent();column.AddChild(heading);column.MoveChild(heading,0);
        return item.Words;
    }
    private MapCallout PaperCallout(string text,Func<Vector3> anchor,Func<bool> shown,float width,int priority,bool dynamic,bool alert=false)
    {
        var pointer=new Line2D{Width=1.5f,DefaultColor=alert?TabletopTheme.Alert:TabletopTheme.Muted,Antialiased=true,ZIndex=1};AddChild(pointer);
        var panel=new PanelContainer{CustomMinimumSize=new Vector2(width,0),Size=new Vector2(width,0),MouseFilter=MouseFilterEnum.Ignore,ZIndex=2};
        using var box=TabletopTheme.Box(TabletopTheme.Paper,7,3);box.BorderColor=alert?TabletopTheme.Alert:TabletopTheme.Outline;
        box.BorderWidthLeft=3;box.ShadowColor=new Color(0,0,0,.12f);box.ShadowSize=2;box.ShadowOffset=new Vector2(1,2);
        panel.AddThemeStyleboxOverride("panel",box);AddChild(panel);
        var column=new VBoxContainer{MouseFilter=MouseFilterEnum.Ignore};column.AddThemeConstantOverride("separation",3);panel.AddChild(column);
        var label=new Label{Text=text,AutowrapMode=TextServer.AutowrapMode.WordSmart,MouseFilter=MouseFilterEnum.Ignore};
        label.AddThemeFontSizeOverride("font_size",17);label.AddThemeColorOverride("font_color",alert?TabletopTheme.Alert:TabletopTheme.Ink);column.AddChild(label);
        var item=new MapCallout(panel,label,pointer,anchor,shown,priority,dynamic);callouts.Add(item);calloutLayoutKey=null;return item;
    }
    private void ClearMovingCallouts()
    {
        foreach(var item in callouts.Where(c=>c.Dynamic).ToArray())
        {
            RemoveChild(item.Card);item.Card.QueueFree();RemoveChild(item.Pointer);item.Pointer.QueueFree();callouts.Remove(item);
        }
        foreach(var pair in originalStatuses){pair.Key.Text=pair.Value;pair.Key.RemoveMeta("reported_change");}
        movingLabels.Clear();calloutLayoutKey=null;
    }
    private void LaneCaption(string text,Vector3 point,bool closed)
    {
        PaperCallout(text,()=>world.ToGlobal(point),()=>true,closed?170:125,2,false,closed);
        if(!closed)return;
        closedLanes.Add(point);
        for(var i=0;i<2;i++){var stroke=new Line2D{Width=4,DefaultColor=TabletopTheme.Alert,Antialiased=true,ZIndex=1};AddChild(stroke);laneBarriers.Add(stroke);}
    }
    private Vector2[] Project(Aabb bounds)
        =>Geometry2D.ConvexHull(Enumerable.Range(0,8).Select(i=>camera.UnprojectPosition(world.ToGlobal(bounds.GetEndpoint(i)))).ToArray());
    // Convex polygon / rectangle separating-axis test. Avoid reserving empty corners of an island's screen AABB.
    internal static bool IntersectsSolid(Rect2 rectangle,Vector2[] polygon)
    {
        if(polygon.Length<3)return false;
        if(polygon.Max(p=>p.X)<rectangle.Position.X||polygon.Min(p=>p.X)>rectangle.End.X
            ||polygon.Max(p=>p.Y)<rectangle.Position.Y||polygon.Min(p=>p.Y)>rectangle.End.Y)return false;
        for(var i=0;i<polygon.Length-1;i++)
        {
            var edge=polygon[i+1]-polygon[i];var axis=new Vector2(-edge.Y,edge.X);
            var low=float.MaxValue;var high=float.MinValue;
            foreach(var p in polygon){var d=p.Dot(axis);low=Math.Min(low,d);high=Math.Max(high,d);}
            var middle=rectangle.GetCenter().Dot(axis);
            var half=(Math.Abs(axis.X)*rectangle.Size.X+Math.Abs(axis.Y)*rectangle.Size.Y)/2;
            if(middle+half<low||middle-half>high)return false;
        }
        return true;
    }
    private void LayoutCallouts()
    {
        bool Shown(MapCallout item)=>item.IsShown()&&new Rect2(Vector2.Zero,Size).Grow(24).HasPoint(camera.UnprojectPosition(item.Anchor()));
        var active=callouts.Where(Shown).OrderBy(c=>c.Priority).ToArray();
        foreach(var item in callouts){item.Card.Visible=Shown(item);item.Pointer.Visible=item.Card.Visible;}
        foreach(var item in active)item.Card.Size=new Vector2(item.Card.CustomMinimumSize.X,item.Card.GetCombinedMinimumSize().Y);
        var key=$"{BottomOcclusion}:{Size}:{world.Position}:{camera.Size}:"+string.Join(';',active.Select(c=>$"{c.Card.Size}:{(camera.UnprojectPosition(c.Anchor())/12).Round()}"));
        if(key!=calloutLayoutKey)
        {
            var solids=ProjectedSolids;
            var choices=new Dictionary<MapCallout,Vector2[]>();
            foreach(var item in active)
            {
                var anchor=camera.UnprojectPosition(item.Anchor());var size=item.Card.Size;
                var preferred=anchor+new Vector2(-size.X/2,item.Priority==0?65:-size.Y-25);
                var candidates=new List<Vector2>{item.Card.Position,preferred};
                for(var y=6f;y<=Size.Y-BottomOcclusion-size.Y-6;y+=12)
                    for(var x=6f;x<=Size.X-size.X-6;x+=16)candidates.Add(new Vector2(x,y));
                choices[item]=candidates.Where(p=>p.X>=6&&p.Y>=6&&p.X+size.X<=Size.X-6&&p.Y+size.Y<=Size.Y-BottomOcclusion-6)
                    .Where(p=>!solids.Any(s=>IntersectsSolid(new Rect2(p,size).Grow(5),s)))
                    .OrderBy(p=>(new Rect2(p,size).GetCenter()-anchor).LengthSquared()+p.DistanceSquaredTo(preferred)*.12f)
                    .Distinct().ToArray();
            }
            var placed=new Dictionary<MapCallout,Rect2>();var attempts=0;
            bool Place()
            {
                if(placed.Count==active.Length)return true;
                if(++attempts>6000)return false;
                // Place the most constrained card first, and backtrack when its nearest slot blocks another card.
                var remaining=active.Where(c=>!placed.ContainsKey(c)).Select(c=>(Card:c,Slots:choices[c]
                    .Where(p=>!placed.Values.Any(r=>r.Intersects(new Rect2(p,c.Card.Size).Grow(6)))).ToArray())).OrderBy(x=>x.Slots.Length).First();
                foreach(var p in remaining.Slots)
                {
                    placed[remaining.Card]=new Rect2(p,remaining.Card.Card.Size);
                    if(Place())return true;
                    placed.Remove(remaining.Card);
                }
                return false;
            }
            if(Place())foreach(var pair in placed)pair.Key.Card.Position=pair.Value.Position;
            CalloutLayoutFits=placed.Count==active.Length;

            calloutLayoutKey=key;
        }
        foreach(var item in active)
        {
            var anchor=camera.UnprojectPosition(item.Anchor());var box=item.Card.GetRect();
            var end=new Vector2(Math.Clamp(anchor.X,box.Position.X+6,box.End.X-6),Math.Clamp(anchor.Y,box.Position.Y+6,box.End.Y-6));
            item.Pointer.Points=[anchor,end];
        }
        for(var i=0;i<closedLanes.Count;i++)
        {
            var p=camera.UnprojectPosition(world.ToGlobal(closedLanes[i]));
            laneBarriers[i*2].Points=[p+new Vector2(-7,-7),p+new Vector2(7,7)];
            laneBarriers[i*2+1].Points=[p+new Vector2(-7,7),p+new Vector2(7,-7)];
        }
    }
}
