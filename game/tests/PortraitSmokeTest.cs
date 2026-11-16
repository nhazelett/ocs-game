using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using System.Security.Cryptography;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PortraitSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var sessions = Directory.GetDirectories(GamePaths.ScenarioRoot)
                .Where(p => File.Exists(Path.Combine(p, "scenario.json")))
                .Select(p => Session.Create(ScenarioBundle.Load(p), 1)).ToArray();
            var thumbnails = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in PersonPortrait.CastKeys.Append("fallback"))
            {
                var face = new PersonPortrait();
                face.SetPerson(Person(key)); AddChild(face);
                Require(face.PortraitKey == key, "Every catalog key, including the neutral fallback, loads.");
                using var source = face.Texture!.GetImage();
                using var image = (Image)source.Duplicate();
                image.Convert(Image.Format.Rgba8);
                Require(image.GetWidth() == 256 && image.GetHeight() == 320, "Portraits use the bounded 256 by 320 render.");
                Require(image.HasMipmaps(), "Small UI faces have mipmaps in the native imported texture.");
                Require(image.GetPixel(0, 0).A == 0 && image.GetPixel(128, 150).A > .99,
                    "The bust is opaque and the surrounding canvas transparent for every look.");
                var headPixels = Enumerable.Range(0, image.GetWidth())
                    .Count(x => image.GetPixel(x, 154).A > .99f);
                Require(headPixels >= 154,
                    "The head fills at least three fifths of the frame at the eye line, including at strip size: " + key);
                if (key != "fallback") CheckEyes(image, key);
                image.Resize(48, 60, Image.Interpolation.Lanczos);
                Require(thumbnails.TryAdd(key, Convert.ToHexString(SHA256.HashData(image.GetData()))), "Unique cast key.");
                // Small result rows choose 40x50 explicitly; Ready must preserve that size.
                var small = new PersonPortrait { CustomMinimumSize = new Vector2(40, 50) };
                small.SetPerson(Person(key)); AddChild(small);
                Require(small.CustomMinimumSize == new Vector2(40, 50), "The result row keeps its smaller portrait size.");
                small.QueueFree(); face.QueueFree();
            }
            Require(thumbnails.Values.Distinct().Count() == thumbnails.Count,
                "All 26 busts and the silhouette remain different after rendering down to 48 pixels.");
            foreach (var session in sessions)
            {
                var keys = session.View.People.Select(p => p.Portrait)
                    .Concat(session.View.Team.Select(p => p.Portrait)).Where(p => p is not null).Cast<string>().ToArray();
                Require(keys.All(PersonPortrait.CastKeys.Contains), "Every scenario person and default teammate uses the new cast.");
            }
            foreach (var invalid in new string?[] { null, "", "../ruiz", "ruiz.png", "res://art/cast/ruiz", "missing-study-key" })
            {
                var face = new PersonPortrait(); face.SetPerson(Person(invalid));
                Require(face.PortraitKey == "fallback", "Missing and malformed keys resolve to the same unassigned sculpt.");
                face.Free();
            }
            using var fallback = PersonPortrait.LoadPortrait(null);
            using var neutral = fallback.GetImage();
            Require(neutral.GetWidth() == 256 && neutral.GetHeight() == 320,
                "Thumbnail inspection never mutates the shared imported texture, including with the headless renderer.");
            var brightEyes = 0;
            for (var y = 100; y < 180; y++)
                for (var x = 70; x < 180; x++)
                {
                    var pixel = neutral.GetPixel(x, y);
                    if (pixel.R > .8f && pixel.G > .8f && pixel.B > .7f) brightEyes++;
                }
            Require(brightEyes == 0, "The fallback is a featureless unassigned sculpt, never a shared named face.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("PORTRAIT_SMOKE_PASS: complete rendered cast, all scenario/team keys, mipmaps, 48px uniqueness, open eyes, safe neutral fallback.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PORTRAIT_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }

    private static PersonView Person(string? key) => new("test", "Test", "", "", "", key, new Dictionary<string, string>());

    private static void CheckEyes(Image image, string key)
    {
        for (var side = 0; side < 2; side++)
        {
            var light = 0; var dark = 0;
            // The eye line of the fixed orthographic kit, including glasses; not the collar or hair.
            for (var y = 130; y < 180; y++)
                for (var x = side == 0 ? 50 : 130; x < (side == 0 ? 127 : 206); x++)
                {
                    var pixel = image.GetPixel(x, y);
                    if (pixel.R > .75f && pixel.G > .73f && pixel.B > .63f) light++;
                    if (pixel.R < .32f && pixel.G < .35f && pixel.B < .32f && pixel.A > .99f) dark++;
                }
            Require(light > 12 && dark > 8, "Both eyes have a light opening and dark iris at native size: " + key);
        }
    }
}
