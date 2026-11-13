using Godot;

namespace LinesOfSupport.Game;

public partial class TabletopMap3D
{
    private sealed class MapCallout(PanelContainer card,Label words,Line2D pointer,
        Func<Vector3> anchor,Func<bool> isShown,int priority,bool dynamic)
    {
        internal PanelContainer Card{get;}=card;
        internal Label Words{get;}=words;
        internal Line2D Pointer{get;}=pointer;
        internal Func<Vector3> Anchor{get;}=anchor;
        internal Func<bool> IsShown{get;}=isShown;
        internal int Priority{get;}=priority;
        internal bool Dynamic{get;}=dynamic;
        internal Label? Heading;
        internal string? Name;
        internal bool Compact;
        internal bool Lane;
        internal bool Alert;
        internal string? Island;
        internal float Width;
    }
    private readonly List<MapCallout> callouts=[];
    private readonly List<Aabb> solidBounds=[];
    private readonly List<string?> solidIslands=[];
    internal IEnumerable<(Vector2[] Polygon,string? Island)> SolidInventory=>solidBounds.Select((s,i)=>(Project(s),solidIslands[i]));
    internal IEnumerable<(Line2D Pointer,string? Island)> PointerTargets=>callouts.Where(c=>c.Pointer.Visible).Select(c=>(c.Pointer,c.Island));
    internal IEnumerable<Label> FullCalloutWords=>callouts.Where(c=>c.Card.Visible&&!c.Compact).Select(c=>c.Words);
    internal IEnumerable<string> FullCalloutText=>callouts.Where(c=>c.Card.Visible&&!c.Compact).Select(c=>c.Words.Text);
    internal IEnumerable<(string From,string To,string Mode,Vector2 Point)> LanePoints=>laneAnchors.Select(l=>(l.From,l.To,l.Mode,camera.UnprojectPosition(world.ToGlobal(l.Point))));
    private readonly List<(string From,string To,string Mode,Vector3 Point)> laneAnchors=[];
    private string? addingIsland;
    private readonly List<Vector3> closedLanes=[];
    private readonly Dictionary<Label,string> originalStatuses=[];
    private readonly List<Line2D> laneBarriers=[];
    private string? calloutLayoutKey;
    internal float BottomOcclusion{get;set;}
    internal bool CalloutLayoutFits{get;private set;}
    internal string CalloutFailure{get;private set;}="";
    internal IEnumerable<Control> CalloutCards=>callouts.Where(c=>c.Card.Visible).Select(c=>(Control)c.Card);
    internal IEnumerable<Line2D> CalloutPointers=>callouts.Where(c=>c.Pointer.Visible).Select(c=>c.Pointer);
    internal IEnumerable<Control> FullCalloutCards=>callouts.Where(c=>c.Card.Visible&&!c.Compact).Select(c=>(Control)c.Card);
    internal IReadOnlyList<Line2D> ClosedLaneBarriers=>laneBarriers;
    internal IReadOnlyList<Vector2[]> ProjectedSolids=>solidBounds.Select(Project).ToArray();

    private Label SiteCaption(string title,string location,string status,Vector3 point,bool alert,string island)
    {
        var item=PaperCallout(status,()=>world.ToGlobal(point),()=>true,190,0,false,alert);item.Name=title;item.Island=island;
        originalStatuses[item.Words]=status;item.Card.TooltipText=location;
        var heading=new Label{Text=title+(alert?" · down":""),AutowrapMode=TextServer.AutowrapMode.Off,MouseFilter=MouseFilterEnum.Ignore};
        heading.AddThemeFontSizeOverride("font_size",20);
        heading.AddThemeColorOverride("font_color",TabletopTheme.Ink);
        item.Heading=heading;var column=item.Words.GetParent();column.AddChild(heading);column.MoveChild(heading,0);
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
        var label=new Label{Text=text,AutowrapMode=TextServer.AutowrapMode.Off,MouseFilter=MouseFilterEnum.Ignore};
        label.AddThemeFontSizeOverride("font_size",17);label.AddThemeColorOverride("font_color",alert?TabletopTheme.Alert:TabletopTheme.Ink);column.AddChild(label);
        var item=new MapCallout(panel,label,pointer,anchor,shown,priority,dynamic);item.Width=width;item.Alert=alert;callouts.Add(item);calloutLayoutKey=null;return item;
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
        var item=PaperCallout(text,()=>world.ToGlobal(point),()=>true,closed?170:125,2,false,closed);
        item.Lane=true;if(!closed)item.Name=text;
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
    internal static bool SegmentHits(Vector2 a,Vector2 b,Vector2[] polygon)
    {
        if(Geometry2D.IsPointInPolygon(a,polygon)||Geometry2D.IsPointInPolygon(b,polygon))return true;
        for(var i=0;i<polygon.Length-1;i++)
            if(Geometry2D.SegmentIntersectsSegment(a,b,polygon[i],polygon[i+1]).VariantType!=Variant.Type.Nil)return true;
        return false;
    }
    internal static Vector2[] RectanglePolygon(Rect2 r)=>[r.Position,new(r.End.X,r.Position.Y),r.End,new(r.Position.X,r.End.Y),r.Position];
    private static Vector2 PointerEnd(Vector2 anchor,Rect2 box)=>new(
        Math.Clamp(anchor.X,box.Position.X+3,box.End.X-3),Math.Clamp(anchor.Y,box.Position.Y+3,box.End.Y-3));
    private void CompactCallout(MapCallout item,bool compact)
    {
        item.Compact=compact;
        item.Words.Visible=!compact||item.Heading is null;
        if(item.Heading is not null)
            item.Heading.Text=item.Name+(item.Words.Text.Contains("down",StringComparison.OrdinalIgnoreCase)
                ||item.Words.Text.Contains("0%")?" · down":compact&&item.Words.Text!="Supported"?" · update":"");
        if(item.Heading is null&&compact)item.Words.Text=item.Name??item.Words.Text;
        item.Card.CustomMinimumSize=new Vector2(compact?0:item.Width,0);
        if(compact){using var empty=new StyleBoxEmpty();item.Card.AddThemeStyleboxOverride("panel",empty);}
        else
        {
            using var box=TabletopTheme.Box(TabletopTheme.Paper,7,3);
            box.BorderColor=item.Alert?TabletopTheme.Alert:TabletopTheme.Outline;box.BorderWidthLeft=3;
            item.Card.AddThemeStyleboxOverride("panel",box);
        }
        // Container minimum sizes propagate on the next frame. Measure the labels now
        // so changing between a card and a shore tag cannot leave a stale footprint.
        var words=item.Words.GetMinimumSize();
        var content=item.Heading is null?words:item.Heading.GetMinimumSize();
        if(!compact&&item.Heading is not null)
            content=new Vector2(Math.Max(content.X,words.X),content.Y+words.Y+3);
        var padding=compact?Vector2.Zero:item.Card.GetThemeStylebox("panel").GetMinimumSize();
        item.Card.CustomMinimumSize=new Vector2(Math.Max(compact?0:item.Width,content.X+padding.X),content.Y+padding.Y);
        item.Card.Size=item.Card.CustomMinimumSize;
    }
    private void LayoutCallouts()
    {
        var available=new Rect2(new Vector2(6,6),Size-new Vector2(12,BottomOcclusion+12));
        bool Shown(MapCallout item)=>item.IsShown()&&available.Grow(24).HasPoint(camera.UnprojectPosition(item.Anchor()));
        var active=callouts.Where(Shown).OrderBy(c=>c.Priority).ToArray();
        foreach(var item in callouts){item.Card.Visible=Shown(item);item.Pointer.Visible=false;}
        var key=$"{BottomOcclusion}:{Size}:{world.Position}:{camera.Size}:"+string.Join(';',active.Select(c=>$"{c.Card.Size}:{c.Card.GetCombinedMinimumSize()}:{c.Words.Text}:{(camera.UnprojectPosition(c.Anchor())/3).Round()}"));
        if(key!=calloutLayoutKey)
        {
            var solids=ProjectedSolids;
            // The fixed-size outage badges and their stems live above the imported kit.
            // Reserve them alongside 3D scenery before placing cards or quiet names.
            var markers=captions.Select(c=>c.Label.GetRect()).Concat(DownMarkers.Select(m=>m.Marker.GetRect())).ToArray();
            var stems=DownStems.SelectMany(s=>Enumerable.Range(1,s.Points.Length-1)
                .Select(i=>(From:s.Points[i-1],To:s.Points[i]))).ToArray();
            var choices=new Dictionary<MapCallout,List<(Rect2 Box,bool Compact,Vector2 End)>>(ReferenceEqualityComparer.Instance);
            foreach(var item in active)
            {
                var anchor=camera.UnprojectPosition(item.Anchor());
                item.Card.TooltipText=item.Words.Text;
                var quiet=item.Lane&&item.Name is not null||item.Name is not null
                    &&item.Words.Text is "Supported" or "Not assessed"||item.Words.Text.StartsWith("Opens day ");
                choices[item]=[];
                for(var pass=quiet?1:0;pass<2;pass++)
                {
                    CompactCallout(item,pass==1);var size=item.Card.Size;
                    var candidates=new List<Vector2>();
                    for(var y=Math.Max(available.Position.Y,anchor.Y-120-size.Y);y<=Math.Min(available.End.Y-size.Y,anchor.Y+120);y+=6)
                        for(var x=Math.Max(available.Position.X,anchor.X-120-size.X);x<=Math.Min(available.End.X-size.X,anchor.X+120);x+=6)
                            candidates.Add(new Vector2(x,y));
                    var preferred=item.Lane?anchor+new Vector2(-size.X/2,8):anchor+new Vector2(-size.X/2,65);
                    var samples=new List<Vector2>();
                    foreach(var position in candidates.OrderBy(p=>PointerEnd(anchor,new Rect2(p,size)).DistanceSquaredTo(anchor)
                        +p.DistanceSquaredTo(preferred)*.3f))
                    {
                        var box=new Rect2(position,size);var end=PointerEnd(anchor,box);
                        if(anchor.DistanceTo(end)>(quiet&&item.Lane?28:120)||solids.Any(s=>IntersectsSolid(box.Grow(4),s))
                            ||markers.Any(r=>r.Intersects(box.Grow(5)))
                            ||stems.Any(s=>SegmentHits(s.From,s.To,RectanglePolygon(box.Grow(3)))))continue;
                        if(pass==0&&(markers.Any(r=>SegmentHits(anchor,end,RectanglePolygon(r.Grow(2))))
                            ||stems.Any(s=>Geometry2D.SegmentIntersectsSegment(anchor,end,s.From,s.To).VariantType!=Variant.Type.Nil)))continue;
                        if(pass==0&&solids.Where((s,index)=>item.Island is null||solidIslands[index]!=item.Island).Any(s=>SegmentHits(anchor,end,s)))continue;
                        // Keep spatially distinct nearby alternatives so a small slide or a compact tag can free a neighbour.
                        if(samples.Any(p=>p.DistanceTo(position)<18))continue;
                        choices[item].Add((box,pass==1,end));samples.Add(position);
                        if(samples.Count==32)break;
                    }
                }
            }
            var placed=new Dictionary<MapCallout,(Rect2 Box,bool Compact,Vector2 End)>(ReferenceEqualityComparer.Instance);var attempts=0;
            bool Compatible(MapCallout item,(Rect2 Box,bool Compact,Vector2 End) slot)
            {
                var anchor=camera.UnprojectPosition(item.Anchor());
                foreach(var other in placed)
                {
                    if(slot.Box.Grow(5).Intersects(other.Value.Box))return false;
                    var source=camera.UnprojectPosition(other.Key.Anchor());
                    if(!slot.Compact&&SegmentHits(anchor,slot.End,RectanglePolygon(other.Value.Box.Grow(3))))return false;
                    if(!other.Value.Compact&&SegmentHits(source,other.Value.End,RectanglePolygon(slot.Box.Grow(3))))return false;
                    if(!slot.Compact&&!other.Value.Compact&&Geometry2D.SegmentIntersectsSegment(anchor,slot.End,source,other.Value.End).VariantType!=Variant.Type.Nil)return false;
                }
                return true;
            }
            bool Place()
            {
                if(placed.Count==active.Length)return true;
                if(++attempts>12000)return false;
                var next=active.Where(c=>!placed.ContainsKey(c)).Select(c=>(Item:c,Slots:choices[c].Where(s=>Compatible(c,s)).ToArray()))
                    .OrderBy(x=>x.Slots.Any(s=>!s.Compact)?0:1).ThenBy(x=>x.Slots.Length).First();
                foreach(var slot in next.Slots)
                {
                    placed[next.Item]=slot;
                    if(Place())return true;
                    placed.Remove(next.Item);
                }
                return false;
            }
            CalloutLayoutFits=Place();
            CalloutFailure=string.Join("; ",active.Where(c=>!placed.ContainsKey(c)).Select(c=>$"{c.Words.Text} at {camera.UnprojectPosition(c.Anchor())}: {choices[c].Count} slots in {available}"));
            foreach(var pair in placed)
            {
                CompactCallout(pair.Key,pair.Value.Compact);pair.Key.Card.Position=pair.Value.Box.Position;
            }
            calloutLayoutKey=key;
        }
        foreach(var item in active.Where(c=>c.Card.Visible&&!c.Compact))
        {
            var anchor=camera.UnprojectPosition(item.Anchor());
            item.Pointer.Points=[anchor,PointerEnd(anchor,item.Card.GetRect())];item.Pointer.Visible=true;
        }
        for(var i=0;i<closedLanes.Count;i++)
        {
            var p=camera.UnprojectPosition(world.ToGlobal(closedLanes[i]));
            laneBarriers[i*2].Points=[p+new Vector2(-7,-7),p+new Vector2(7,7)];
            laneBarriers[i*2+1].Points=[p+new Vector2(-7,7),p+new Vector2(7,-7)];
        }
    }
}
