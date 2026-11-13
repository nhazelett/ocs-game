using Godot;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DeskHierarchySmokeTest : Node
{
    public override async void _Ready()
    {
        var folder=Path.Combine(Path.GetTempPath(),"ocs-hierarchy-"+Guid.NewGuid());
        Directory.CreateDirectory(folder); GameShell? shell=null;
        try
        {
            using var scene=GD.Load<PackedScene>("res://scenes/Main.tscn");
            shell=scene.Instantiate<GameShell>();
            shell.AutosavePathOverride=Path.Combine(folder,"save.json");
            shell.TeamSettingsPathOverride=Path.Combine(folder,"team.cfg");
            shell.LookSettingsPathOverride=Path.Combine(folder,"look.cfg");
            shell.AudioSettingsPathOverride=Path.Combine(folder,"sound.cfg");
            shell.MusicSettingsPathOverride=Path.Combine(folder,"music.cfg");
            File.WriteAllText(shell.AudioSettingsPathOverride,"[sound]\nmuted=true\n");
            File.WriteAllText(shell.MusicSettingsPathOverride,"[music]\nmuted=true\n");
            AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed);
            shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed);DeployWithoutPrep(shell);
            var args=OS.GetCmdlineUserArgs();var baseline=args.Contains("--baseline");
            var tabs=shell.GetNode<TabContainer>("%DeskTabs");
            await Shot("today");
            Pick(shell.PersonContactInput,"finance");shell.VisitButtons["finance"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.TeamBars["you"].Value>0&&shell.TeamBars["you"].Value<shell.TeamBars["you"].MaxValue,
                "The real finance visit creates a partial hours bar.");
            if(!baseline)Require(shell.GetNode<Label>("%BuildStatus").Text=="The task is on your team's work list.",
                "Queued work has a complete acknowledgement sentence.");
            await Shot("partial-hours");
            Pick(shell.PersonContactInput,"meeting");shell.VisitButtons["meeting"].EmitSignal(Button.SignalName.Pressed);
            var reps=Enumerable.Range(0,shell.PersonContactInput.ItemCount).Select(i=>shell.PersonContactInput.GetItemMetadata(i).AsString())
                .Where(id=>id.StartsWith("vendor:")).ToArray();
            foreach(var rep in reps)
            {
                Pick(shell.PersonContactInput,rep);
                if(!shell.VisitButtons["vendor"].Disabled)shell.VisitButtons["vendor"].EmitSignal(Button.SignalName.Pressed);
                if(shell.TeamBars["you"].Value==shell.TeamBars["you"].MaxValue)break;
            }
            Require(shell.TeamBars["you"].Value==shell.TeamBars["you"].MaxValue
                &&shell.TeamBars.Values.Any(b=>b.Value==0),"Real queued visits show full and empty personal bars together.");
            await Shot("hours");Step(shell,3);await Shot("today-down");
            shell.NavigateBoardItem(new BoardItem("",shell.Session.View!.Day,BoardCategory.Request,
                BoardSeverity.Info,BoardLinkKind.Card,"tutForklift","",[]));
            await Shot("request");
            shell.RequirementMarketButton.EmitSignal(Button.SignalName.Pressed);
            Pick(shell.VendorInput,"harborEquipment");Pick(shell.InstrumentInput,"bpaCall");Pick(shell.VehicleInput,"equipBpa");
            PickPriorities(shell,"fast","good");
            shell.AwardScroll.ScrollVertical=0;await Shot("award");
            var hash=shell.Session.StateHash;
            for(var direction=0;direction<3;direction++)
            {
                shell.LookPicker.Select(direction);shell.LookPicker.EmitSignal(OptionButton.SignalName.ItemSelected,direction);
                for(var i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(!baseline)CheckHierarchy();
                Require(shell.Session.StateHash==hash,"Changing look preserves the actual live state.");
            }
            shell.QueueFree();shell=null;
            for(var i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("DESK_HIERARCHY_SMOKE_PASS: real empty/full team hours, portrait controls, headings, navigation clearance, button alignment and pure Today/request/award renders.");
            GetTree().Quit();

            async Task Shot(string name)
            {
                shell.DeveloperToggle.ButtonPressed=false;
                if(name is "today" or "hours" or "partial-hours" or "today-down")tabs.CurrentTab=GameShell.TodayTab;
                var hash=shell.Session.StateHash;
                for(var i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(!baseline)CheckHierarchy();
                Require(shell.Session.StateHash==hash,"Rendering the desk changes no Sim facts.");
                var at=Array.IndexOf(args,"--screenshots");if(at<0)return;
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=GetViewport().GetTexture().GetImage();
                Require(image.GetWidth()==1280&&image.GetHeight()==800,"Review captures use the release viewport.");
                Require(image.SavePng(args[at+1]+"-"+name+".png")==Error.Ok,"Saved native hierarchy screenshot.");
            }
            void CheckHierarchy()
            {
                var strip=shell.GetNode<Control>("Margin/Layout/TopStrip");
                var nav=shell.GetNode<Control>("Navigation");var viewport=GetViewport().GetVisibleRect();
                Require(!strip.GetGlobalRect().Intersects(nav.GetGlobalRect())&&viewport.Encloses(nav.GetGlobalRect()),
                    "Menu and developer navigation clear the strip and stay in the viewport.");
                Require(viewport.Encloses(shell.GetNode<Button>("%NextDay").GetGlobalRect()),"Three faces and names leave the day action on screen.");
                Require(shell.GetNode<Label>("%DayCounter").GetLineCount()==1,"The morning stays on one line.");
                var map=shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
                MapCalloutSmokeTest.Check(map);
                Require(shell.GetNode<Label>("%CashLabel").GetLineCount()==1,"The complete cash comparison stays on one line.");
                var heading=shell.GetNode<Label>("Margin/Layout/WorkArea/BoardPanel/BoardLayout/Heading");
                Require(heading.GetThemeFontSize("font_size")>shell.GetThemeDefaultFontSize(),"Pane headings have a stronger type hierarchy than body text.");
                using var bodyFont=shell.GetThemeDefaultFont();
                Require(bodyFont.GetFontName().Contains("Barlow")&&shell.GetThemeDefaultFontSize()>=20,
                    "Live body copy uses the bundled unslashed-zero typeface at a readable size.");
                foreach(var member in shell.Session.View!.Team)
                {
                    var button=shell.TeamButtons[member.Id];var bar=shell.TeamBars[member.Id];
                    using var icon=button.Icon;
                    using var nameStyle=button.GetThemeStylebox("normal");
                    Require(icon is not null&&nameStyle is StyleBoxEmpty,
                        "A teammate is a face and name, without an input-like frame.");
                    Require(bar.Value==member.HoursSpokenForToday&&bar.MaxValue==member.HoursPerDay,
                        "Personal bar fill is exactly the Sim's occupied hours.");
                    using var fill=(StyleBoxFlat)bar.GetThemeStylebox("fill");
                    using var background=(StyleBoxFlat)bar.GetThemeStylebox("background");
                    Require(!fill.BgColor.IsEqualApprox(background.BgColor),"Occupied time has a distinct fill against its track.");
                }
                foreach(var button in new[]{shell.GetNode<Button>("%NextDay"),shell.PeopleToggle,shell.BoardDetailsToggle,shell.LeadOpen,
                    shell.AwardButton,shell.RequirementMarketButton})
                    Require(button.Alignment==HorizontalAlignment.Left,"Desk actions consistently align their labels to the left.");
            }
        }
        catch(Exception error){shell?.QueueFree();GD.PushError("DESK_HIERARCHY_SMOKE_FAIL: "+error);GetTree().Quit(1);}
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
}
