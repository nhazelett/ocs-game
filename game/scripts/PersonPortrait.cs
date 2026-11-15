using Godot;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

// Scenario and saved-team keys are stable. All live portraits use the same original 3D cast.
public partial class PersonPortrait : TextureRect
{
    internal static IReadOnlyList<string> CastKeys { get; } = Array.AsReadOnly(new[]
    {
        "bell", "brandt", "brooks", "carvalho", "cole", "delgado", "dunn", "haddad", "hale",
        "kowalski", "lin", "mensah", "mills", "moreau", "nair", "okafor", "ortiz", "park",
        "pryce", "reyna", "ruiz", "sato", "silva", "tan", "ward", "whitfield"
    });
    internal string PortraitKey { get; private set; } = "fallback";

    public override void _Ready()
    {
        if (CustomMinimumSize == Vector2.Zero) CustomMinimumSize = new Vector2(80, 100);
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.KeepAspectCentered;
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        MouseFilter = MouseFilterEnum.Ignore;
        using var current = Texture;
        if (current is null) SetPerson(null);
    }

    internal static Texture2D LoadPortrait(string? key)
        => GD.Load<Texture2D>($"res://art/cast/{(key is not null && CastKeys.Contains(key) ? key : "fallback")}.png")
            ?? throw new InvalidDataException("The packaged character cast is incomplete.");

    internal void SetPerson(PersonView? person)
    {
        var key = person?.Portrait;
        // An explicit catalog, never an arbitrary filesystem or resource path.
        if (key is null || !CastKeys.Contains(key)) key = "fallback";
        using var current = Texture;
        if (current is null || PortraitKey != key)
        {
            using var texture = LoadPortrait(key);
            Texture = texture;
            PortraitKey = key;
            SetMeta("portrait_key", key);
        }
        TooltipText = person?.WithUnit ?? "No portrait available";
    }
}
