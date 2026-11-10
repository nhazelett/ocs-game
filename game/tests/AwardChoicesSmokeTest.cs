using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class AwardChoicesSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = Shell(this);
            var hash = shell.Session.StateHash;
            void Target(string id)
            {
                var picker = shell.GetNode<TabContainer>("%DeskTabs").GetNode("Actions/AwardEditor").FindChildren("*", "OptionButton", true, false).OfType<OptionButton>().First();
                Pick(picker, id);
            }
            void Bindings()
            {
                var c = shell.CurrentAwardChoices!;
                Check(shell.VendorInput,c.Vendors); Check(shell.InstrumentInput,c.Instruments);
                Check(shell.VehicleInput,c.Vehicles); Check(shell.FundingInput,c.Funding);
            }
            Target("spokeBSite.lodging"); Bindings();
            var host = shell.CurrentAwardChoices!.Vendors.Single(v=>v.Id=="hubUtilities");
            Require(!host.Available && shell.VendorChoiceReason.Text==host.Reason,"The host nation reason is visible before choosing or previewing it.");
            Pick(shell.VendorInput,"hubUtilities");
            Require(Selected(shell.VendorInput)=="" && shell.PendingAward is null,"A disabled supplier cannot be chosen even by a forced selection signal.");
            await Capture("host-nation",shell.VendorChoiceReason);
            Target("req0007"); Pick(shell.InstrumentInput,"bpaCall"); Bindings();
            Require(shell.CurrentAwardChoices!.Vendors.Single(v=>v.Id=="vendorC").Available
                && shell.CurrentAwardChoices.Vendors.Where(v=>v.Id!="vendorC").All(v=>!v.Available),"Instrument-first selection re-asks the Sim and greys suppliers without that BPA.");
            Require(shell.Session.StateHash==hash,"Every list and blocked attempt is read-only.");
            await Capture("bpa-first",shell.InstrumentInput);
            Pick(shell.VendorInput,"vendorC");
            shell.QuantityInput.Value += 1;
            Require(shell.PendingAward is null && shell.AwardPreview is null,"Editing an amount cannot bypass a missing ordering vehicle.");
            Pick(shell.VehicleInput,"bosBpa");
            Require(shell.PendingAward is not null && shell.AwardPreview is not null,"A complete structural selection reaches the real amount-dependent preview.");
            shell.RequirementToggles["operator"].ButtonPressed=true;
            var why=shell.Session.View!.AwardChoices("spokeBSite","power","req0007","vendorC","bpaCall")!.Vendors.Single(v=>v.Id=="vendorC").Reason!;
            Require(Selected(shell.VendorInput)=="" && Selected(shell.InstrumentInput)=="" && shell.PendingAward is null
                && shell.AwardChoiceNotice!.Contains(why),"A scope change clears newly invalid selections and retains the Sim's reason before Preview.");
            shell.RequirementToggles["operator"].ButtonPressed=false;
            Require(Selected(shell.VendorInput)=="","A cleared choice is not silently restored.");
            Require(shell.Session.Apply(new FindFinanceAction()).Accepted,"Finance fixture accepted."); Step(shell,6);
            Target("req0007"); Pick(shell.VendorInput,"vendorB"); Bindings();
            var cash=shell.CurrentAwardChoices!.Instruments.Single(i=>i.Id=="sf44");
            Require(!cash.Available && cash.Reason=="Meridian Site Services does not take cash."
                && shell.InstrumentChoiceReason.Text==cash.Reason,"The current supplier's cash restriction is shown under the control.");
            hash=shell.Session.StateHash; Pick(shell.InstrumentInput,"sf44");
            Require(Selected(shell.InstrumentInput)=="" && shell.PendingAward is null,"A disabled cash route never reaches Preview.");
            await Capture("no-cash",shell.InstrumentChoiceReason);
            Pick(shell.VendorInput,"vendorA"); Pick(shell.InstrumentInput,"sf44"); Bindings();
            Require(shell.FundingInput.Disabled && Selected(shell.FundingInput)=="" && shell.PendingAward?.FundingLine is null
                && shell.FundingChoiceReason.Text=="A cash buy is paid from the cash box, not a funding line.","Cash greys and clears funding with the exact explanation.");
            Require(shell.Session.StateHash==hash,"Availability changes and previews never change the world.");
            File.Delete(shell.AutosavePath);shell.QueueFree();
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("AWARD_CHOICES_SMOKE_PASS: exact availability/reasons, blocked signals, instrument-first filtering, stale selection clearing, cash funding and pure preview.");GetTree().Quit();

            async Task Capture(string name,Control focus)
            {
                var args=OS.GetCmdlineUserArgs();var index=Array.IndexOf(args,"--screenshots");if(index<0)return;
                shell.DeveloperToggle.ButtonPressed=false;shell.GetNode<TabContainer>("%DeskTabs").CurrentTab=2;
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                ((ScrollContainer)focus.GetParent().GetParent()).EnsureControlVisible(focus);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[index+1]+"-"+name+".png")==Error.Ok,"Native choice screenshot saved.");shell.DeveloperToggle.ButtonPressed=true;
            }
        }
        catch(Exception error){GD.PushError($"AWARD_CHOICES_SMOKE_FAIL: {error}");GetTree().Quit(1);}
    }
    private static string Selected(OptionButton picker)=>picker.GetItemMetadata(picker.Selected).AsString();
    private static void Check(OptionButton picker,IReadOnlyList<ChoiceOption> choices)
    {
        Require(picker.ItemCount==choices.Count+1,"Every published choice remains in its list plus an explicit empty selection.");
        foreach(var c in choices)
        {
            var i=Enumerable.Range(1,picker.ItemCount-1).Single(i=>picker.GetItemMetadata(i).AsString()==c.Id);
            Require(picker.IsItemDisabled(i)==!c.Available && picker.GetItemTooltip(i)==(c.Reason??""),"Availability and explanation exactly match the Sim, including disabled vehicle and funding entries.");
        }
    }
}
