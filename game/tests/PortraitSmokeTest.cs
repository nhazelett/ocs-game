using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using System.Security.Cryptography;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PortraitSmokeTest:Node
{
    public override async void _Ready()
    {
        try
        {
            var people=Directory.GetDirectories(GamePaths.ScenarioRoot).Where(p=>File.Exists(Path.Combine(p,"scenario.json")))
                .SelectMany(p=>Session.Create(ScenarioBundle.Load(p),1).View.People).GroupBy(p=>p.Portrait).Select(g=>g.First()).ToList();
            string[] first=["ruiz","cole","whitfield","mills"];
            people=people.OrderBy(p=>Array.IndexOf(first,p.Portrait) is var i && i>=0?i:99).ThenBy(p=>p.DisplayName).ToList();
            var hashes=new HashSet<string>();
            using var theme=TabletopTheme.Create();
            var gallery=new Control{Theme=theme};AddChild(gallery);gallery.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var bg=new ColorRect{Color=TabletopTheme.Background,MouseFilter=Control.MouseFilterEnum.Ignore};gallery.AddChild(bg);bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            Art.ArtDirections.Label(gallery,"Tabletop / the people you meet",new Rect2(24,18,900,40),28,"343d4b");
            Art.ArtDirections.Label(gallery,"Original fictional character designs · no photographs or unit marks",new Rect2(24,59,1100,30),18,"66716d");
            for(var i=0;i<=people.Count;i++)
            {
                var cell=Art.ArtDirections.Panel(gallery,new Rect2(24+i%5*245,106+i/5*158,232,146),"faf5ea");
                var portrait=new PersonPortrait();portrait.SetPerson(i<people.Count?people[i]:people[0] with{Portrait="missing-study-key"});cell.AddChild(portrait);portrait.Position=new Vector2(76,5);portrait.Size=new Vector2(80,100);
                Require(portrait.PortraitKey==(i<people.Count?people[i].Portrait??"fallback":"fallback"),"Every declared cast key loads; unknown keys use the neutral fallback.");
                using var image=portrait.Texture!.GetImage();if(portrait.PortraitKey!="fallback")Require(hashes.Add(Convert.ToHexString(SHA256.HashData(image.GetData()))),"Every named cast portrait is visually distinct.");
                if (portrait.PortraitKey == "fallback")
                {
                    var face = image.GetPixel(128, 140);
                    for (var y = 114; y < 178; y++)
                        for (var x = 90; x < 166; x++)
                            Require(image.GetPixel(x, y).IsEqualApprox(face),
                                "Unknown people have a featureless silhouette, never a shared face.");
                }
                else for (var side = 0; side < 2; side++)
                {
                    var whites = 0; var pupils = 0;
                    for (var y = 110; y < 166; y++)
                        for (var x = side == 0 ? 60 : 128; x < (side == 0 ? 128 : 200); x++)
                        {
                            var pixel = image.GetPixel(x, y);
                            if (pixel.R > .82f && pixel.G > .82f && pixel.B > .72f) whites++;
                            if (pixel.R < .32f && pixel.G < .35f && pixel.B < .32f) pupils++;
                        }
                    Require(whites > 32 && pupils > 20,
                        "Each eye renders with a visible light opening and dark pupil, including glasses: " + portrait.PortraitKey);
                }
                Art.ArtDirections.Label(cell,i<people.Count?people[i].DisplayName:"Unassigned portrait",new Rect2(6,112,220,30),18,"343d4b",align:HorizontalAlignment.Center);
            }
            Require(people.Count>=17,"Every current scenario's cast is represented, including the four requested leads.");
            var args=OS.GetCmdlineUserArgs();var shot=Array.IndexOf(args,"--screenshot");
            if(shot>=0)
            {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=GetViewport().GetTexture().GetImage();Require(image.SavePng(args[shot+1])==Error.Ok,"Gallery screenshot saved.");
            }
            gallery.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("PORTRAIT_SMOKE_PASS: all casts, distinct native renders, declared keys and neutral missing-key fallback.");GetTree().Quit();
        }
        catch(Exception e){GD.PushError($"PORTRAIT_SMOKE_FAIL: {e}");GetTree().Quit(1);}
    }
}
