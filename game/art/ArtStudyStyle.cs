using Godot;

namespace LinesOfSupport.Game.Art;

internal sealed record ArtStudyStyle(string Name, string Subtitle, string Background, string Panel, string Ink,
    string Muted, string Accent, string Alert, string Water, string Land, string Font, string HeadingFont)
{
    internal static readonly ArtStudyStyle[] All = [
        new("Field atlas", "Ink, paper and a coastline worth reading.", "e7e1d2", "f5f0e5", "243c3b", "576763", "a05d37", "9b3f32", "c9dad5", "ddd0a9", "Barlow-Regular.ttf", "Bitter.ttf"),
        new("Night watch", "A quiet signal room. The interruption cuts through.", "0d1b26", "142a37", "e2f0ec", "99b4bc", "76d9c7", "ffba86", "102d3d", "244d55", "Barlow-Regular.ttf", "Barlow-SemiBold.ttf"),
        new("Tabletop", "Small places, tangible consequences.", "e9e3d8", "faf5ea", "343d4b", "66716d", "556e65", "9f4937", "8eafb2", "bcb18a", "AtkinsonHyperlegible-Regular.ttf", "AtkinsonHyperlegible-Bold.ttf")
    ];

    internal static Font LoadFont(string name)
    {
        var path = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "assets", "art-studies", "fonts", name));
        var font = new FontFile();
        if (font.LoadDynamicFont(path) != Error.Ok) throw new IOException($"Art study font could not load: {name}");
        return name == "Bitter.ttf" ? new FontVariation { BaseFont = font, VariationEmbolden = 1,
            VariationOpentype = new Godot.Collections.Dictionary { [TextServerManager.GetPrimaryInterface().NameToTag("wght")] = 500 } } : font;
    }
}
