using Godot;

namespace LinesOfSupport.Game;

// Neutral, code-drawn portrait while the cast has portrait keys but no authored likeness assets.
// This is presentation only; no appearance or personality is inferred from a person's name.
public partial class PersonPortrait : Control
{
    public override void _Ready() { CustomMinimumSize = new Vector2(64, 80); MouseFilter = MouseFilterEnum.Ignore; }
    public override void _Draw()
    {
        var width = Size.X; var height = Size.Y;
        DrawStyleBox(new StyleBoxFlat { BgColor = new Color("d4c9b4"), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 }, new Rect2(Vector2.Zero, Size));
        DrawCircle(new Vector2(width * .5f, height * .34f), width * .18f, new Color("b8aa95"));
        DrawColoredPolygon([new(width * .15f, height * .9f), new(width * .24f, height * .66f), new(width * .4f, height * .59f),
            new(width * .6f, height * .59f), new(width * .76f, height * .66f), new(width * .85f, height * .9f)], new Color("6f8071"));
    }
}
