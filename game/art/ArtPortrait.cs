using Godot;

namespace LinesOfSupport.Game.Art;

// Fictional study face, explicitly not a likeness inferred from a real name or rank.
public partial class ArtPortrait : Control
{
    internal int Study { get; init; }
    public override void _Draw()
    {
        var s = ArtStudyStyle.All[Study]; var ink = new Color(s.Ink);
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(Study == 0 ? "d8d0bc" : Study == 1 ? "1f4651" : "d4c9b4"));
        var skin = new Color(Study == 0 ? "bd9276" : Study == 1 ? "d1b195" : "b88e6b");
        var hair = new Color(Study == 1 ? "102631" : "3b3b35");
        DrawColoredPolygon([new(5,110),new(15,85),new(34,75),new(58,75),new(77,85),new(88,110)], new Color(Study == 0 ? "5c7167" : Study == 1 ? "649b9b" : "6f8071"));
        DrawRect(new Rect2(37,62,18,25), skin.Darkened(.1f));
        DrawCircle(new Vector2(46,43), 29, hair);
        DrawCircle(new Vector2(46,51), 24, skin);
        DrawColoredPolygon([new(19,43),new(25,20),new(55,15),new(73,31),new(69,48),new(56,29),new(31,35)], hair);
        DrawLine(new Vector2(32,49),new Vector2(39,49),ink,2,true); DrawLine(new Vector2(53,49),new Vector2(60,49),ink,2,true);
        DrawPolyline([new(46,51),new(43,61),new(48,62)], skin.Darkened(.3f),1.5f,true);
        DrawArc(new Vector2(46,62),8,.25f,2.7f,14,skin.Darkened(.4f),1.5f,true);
        DrawLine(new Vector2(30,84),new Vector2(43,98),new Color(s.Panel),2,true); DrawLine(new Vector2(62,84),new Vector2(49,98),new Color(s.Panel),2,true);
        if (Study == 0) for (var i=0;i<7;i++) DrawLine(new Vector2(7+i*5,96),new Vector2(12+i*5,106),new Color("40594e"),1);
        if (Study == 1)
        {
            for(var i=0;i<9;i++) DrawLine(new Vector2(0,9+i*12),new Vector2(92,9+i*12),new Color(0.05f,.14f,.19f,.2f),1);
            DrawRect(new Rect2(1,1,90,108),new Color(s.Accent),false,1);
        }
        if (Study == 2) DrawColoredPolygon([new(46,30),new(68,41),new(63,67),new(46,75)],new Color(.95f,.82f,.64f,.25f));
    }
}
