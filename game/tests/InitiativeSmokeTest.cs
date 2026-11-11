using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class InitiativeSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            shell.DeveloperToggle.ButtonPressed = false; shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 2;
            var view = shell.Session.View!; var hash = shell.Session.StateHash;
            var card = shell.CurrentActionEditor!.CardId!;
            Require(card is not null && shell.InitiativeTargetInput is null, "A customer request opens first with no unrelated targets.");
            var requests = shell.AwardRequestInput;
            Require(Enumerable.Range(0, requests.ItemCount).All(i => requests.GetItemMetadata(i).AsString() == "own-initiative"
                || view.Cards.Any(c => c.Id == requests.GetItemMetadata(i).AsString())), "The main list holds requests and one separate initiative entry.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(OptionButton picker, string suffix)
            {
                for(var i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(shot<0)return;
                picker.ShowPopup();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[shot+1]+"-"+suffix+".png")==Error.Ok,"Native picker screenshot saved.");
                picker.GetPopup().Hide();
            }
            await Capture(requests,"requests");
            Pick(requests,"own-initiative");
            Require(shell.CurrentActionEditor!.CardId is null && shell.InitiativeTargetInput is not null
                && shell.PendingAward is null && shell.AwardButton.Disabled,"Initiative is explicit and clears stale award choices.");
            Require(Enumerable.Range(0,shell.InitiativeTargetInput!.ItemCount).All(i=>
                !shell.InitiativeTargetInput.GetItemText(i).Contains("without a request",StringComparison.OrdinalIgnoreCase)),
                "Own targets need no confusing without-a-request suffix.");
            Pick(shell.InitiativeTargetInput,"spokeBSite.power");
            await Capture(shell.InitiativeTargetInput!,"initiative");
            Require(shell.Session.StateHash==hash,"Changing modes and inspecting lists leaves the Sim unchanged.");
            Pick(shell.AwardRequestInput,card!);
            Require(shell.CurrentActionEditor!.CardId==card && shell.InitiativeTargetInput is null,"Returning to a request restores its linked editor.");
            PickAwardTarget(shell,"spokeBSite.power");
            Pick(shell.VendorInput,"vendorC");Pick(shell.InstrumentInput,"bpaCall");Pick(shell.VehicleInput,"bosBpa");
            shell.QuantityInput.Value=360;shell.DurationInput.Value=30;
            PickPriorities(shell, "fast", "cheap");
            var action=shell.PendingAward!;
            Require(action.Card is null && action.Site=="spokeBSite" && action.Capability=="power","The own-initiative action carries the actual target without a request.");
            var expected=Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot,"spoke-b")),1);
            Require(expected.Apply(action).Accepted && !shell.AwardButton.Disabled,"The fixture is a valid existing Sim action.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash==expected.StateHash(),"The new entry sends exactly the previewed action.");
            shell.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("INITIATIVE_SMOKE_PASS: requests separated, explicit own targets, pure mode changes and exact existing award.");
            GetTree().Quit();
        }
        catch(Exception error){GD.PushError("INITIATIVE_SMOKE_FAIL: "+error);GetTree().Quit(1);}
    }
}
