using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

// Original fictional designs are keyed by the scenario's portrait reference, never guessed from names.
public partial class PersonPortrait : TextureRect
{
    internal string PortraitKey { get; private set; } = "fallback";
    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(80,100); ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.KeepAspectCentered; MouseFilter = MouseFilterEnum.Ignore;
        using var current = Texture;
        if(current is null) SetPerson(null);
    }
    internal void SetPerson(PersonView? person)
    {
        var key=person?.Portrait ?? "fallback";
        var root=Path.Combine(GamePaths.AssetRoot,"portraits");
        var candidate=Path.Combine(root,key+".svg");
        // A portrait key is an identifier, never an arbitrary filesystem path.
        if(key.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_') || !File.Exists(candidate)) key="fallback";
        using var current = Texture;
        if(current is not null && PortraitKey==key){TooltipText=person?.WithUnit ?? "No portrait available";return;}
        using var image=new Image();
        if(image.LoadSvgFromString(File.ReadAllText(Path.Combine(root,key+".svg")))!=Error.Ok) throw new InvalidDataException("Portrait could not render: "+key);
        using var texture = ImageTexture.CreateFromImage(image);
        Texture=texture;PortraitKey=key;SetMeta("portrait_key",key);
        TooltipText=person?.WithUnit ?? "No portrait available";
    }
}
