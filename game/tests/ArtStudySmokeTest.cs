using Godot;
using LinesOfSupport.Game.Art;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ArtStudySmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            using var scene=GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell=scene.Instantiate<GameShell>(); AddChild(shell);
            Require(!shell.ArtStudiesButton.IsVisibleInTree(),"Default launch does not show art studies or change the normal theme.");
            DeveloperWorkspace(shell); Scenario(shell,"spoke-b");
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed);
            Require(shell.ArtStudiesButton.IsVisibleInTree(),"Explicit developer switch exposes the art comparison.");
            var original=shell.Session.StateHash;
            shell.ArtStudiesButton.EmitSignal(Button.SignalName.Pressed);
            var study=shell.GetChildren().OfType<ArtDirections>().Single();
            var folder=Path.Combine(GamePaths.ScenarioRoot,"spoke-b");
            var direct=LinesOfSupport.Sim.Replay.Run(ScenarioBundle.Load(folder),InputLog.Load(Path.Combine(folder,"golden","approach1-as-written.json")),stopAfterDay:10);
            var hash=direct.StateHash();
            for(var i=0;i<3;i++)
            {
                study.ShowDirection(i);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var expectedLead=direct.View.MorningBoard().Lead!;
                Require(study.StateHash==hash,"Every direction uses exactly the same real Sim morning.");
                Require((study.Lead with { Person=null })==(expectedLead with { Person=null }) && study.Lead.Person?.WithUnit==expectedLead.Person?.WithUnit,
                    "Every lead field and the displayed person's name, rank and unit match direct Sim.");
                Require(study.LeadWords.Text==direct.View.MorningBoard().Lead!.Text && study.Morning.Text=="Morning of day 11","No authored lead prose or day is invented.");
                Require(study.LeadWords.GetGlobalRect().End.X<=1280 && study.LeadWords.Size.Y>=study.LeadWords.GetLineCount()*study.LeadWords.GetThemeFont("font").GetHeight(27),"Full-size lead text wraps inside the visible panel.");
                if(i==2) Require(study.Diorama?.MeshCount>80,"Tabletop is real native 3D geometry, not a painted mock.");
                Require(shell.Session.StateHash==original,"The player's active game is preserved while comparing studies.");
            }
            study.ExitStudy!(); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Require(shell.AtFrontDoor && shell.Session.StateHash==original,"Leaving the art comparison returns to the unchanged game.");
            shell.DeveloperToggle.ButtonPressed=false;
            Require(!shell.ArtStudiesButton.IsVisibleInTree(),"Developer opt-out hides the comparison again.");
            shell.QueueFree(); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("ART_STUDY_SMOKE_PASS: developer gating, exact day-eleven data, readable lead, actual 3D and unchanged player state."); GetTree().Quit();
        }
        catch(Exception error){GD.PushError($"ART_STUDY_SMOKE_FAIL: {error}");GetTree().Quit(1);}
    }
}
