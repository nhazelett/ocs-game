using Godot;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class TabletopThemeSmokeTest:Node
{
    public override async void _Ready()
    {
        try
        {
            using var scene=GD.Load<PackedScene>("res://scenes/Main.tscn");var shell=scene.Instantiate<GameShell>();AddChild(shell);
            shell.AutosavePathOverride=Path.Combine(Path.GetTempPath(),"ocs-tabletop-"+Guid.NewGuid()+".json");
            var args=OS.GetCmdlineUserArgs();var shot=Array.IndexOf(args,"--screenshots");
            async Task Capture(string suffix)
            {
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(suffix=="morning") Require(shell.GetNode<Label>("%GameTitle").GetGlobalRect().Position.Y>=0,"Theme keeps the game header inside the viewport.");
                if(shot<0)return;
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=GetViewport().GetTexture().GetImage();Require(image.SavePng(args[shot+1]+"-"+suffix+".png")==Error.Ok,"Screenshot saved.");
            }
            Require(shell.GetThemeDefaultFont().GetFontName().Contains("Atkinson"),"Title uses the chosen redistributable typeface.");
            await Capture("title");shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            await Capture("prep");DeployWithoutPrep(shell);
            var hash=shell.Session.StateHash;var tabs=shell.GetNode<TabContainer>("%DeskTabs");
            for(var i=0;i<tabs.GetTabCount();i++)
            {
                tabs.CurrentTab=i;
                Require(tabs.GetCurrentTabControl().GetThemeDefaultFont().GetFontName().Contains("Atkinson"),"Every desk inherits the same theme.");
                Require(shell.Session.StateHash==hash,"Theme/navigation leaves the live Sim unchanged.");
            }
            shell.DeveloperToggle.ButtonPressed=true;Replay(shell);Step(shell,10);shell.DeveloperToggle.ButtonPressed=false;tabs.CurrentTab=GameShell.TodayTab;
            await Capture("morning");while(!shell.Session.View!.Finished)Step(shell);await Capture("debrief");
            Require(shell.DebriefSummaryText.GetThemeDefaultFont().GetFontName().Contains("Atkinson"),"After-action inherits the chosen typeface.");
            File.Delete(shell.AutosavePath);shell.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("TABLETOP_THEME_SMOKE_PASS: title/prep/all desks/debrief, portable font and unchanged Sim navigation.");GetTree().Quit();
        }
        catch(Exception error){GD.PushError($"TABLETOP_THEME_SMOKE_FAIL: {error}");GetTree().Quit(1);}
    }
}
