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
                PickAwardTarget(shell, id);
            }
            void Bindings()
            {
                var c = shell.CurrentAwardChoices!;
                Check(shell.VendorInput,c.Vendors); Check(shell.InstrumentInput,c.Instruments);
                Check(shell.VehicleInput,c.Vehicles); Check(shell.FundingInput,c.Funding);
            }
            Target("spokeBSite.lodging"); Bindings();
            var host = shell.CurrentAwardChoices!.Vendors.Single(v=>v.Id=="hubUtilities");
            Require(!host.Available && shell.VendorChoiceReason.Text==host.Reason,"The single unavailable supplier has its exact host nation reason before preview.");
            Pick(shell.VendorInput,"hubUtilities");
            Require(Selected(shell.VendorInput)=="" && shell.PendingAward is null,"A disabled supplier cannot be chosen even by a forced selection signal.");
            await Capture("host-nation",shell.VendorChoiceReason);
            Target("req0007"); Pick(shell.InstrumentInput,"bpaCall"); Bindings();
            Require(shell.CurrentAwardChoices!.Vendors.Single(v=>v.Id=="vendorC").Available
                && shell.CurrentAwardChoices.Vendors.Where(v=>v.Id!="vendorC").All(v=>!v.Available),"Instrument-first selection re-asks the Sim and greys suppliers without that BPA.");
            Require(!shell.InstrumentChoiceReason.Visible && shell.InstrumentChoiceReason.Text=="",
                "A valid BPA selection has no unrelated cash or card reason under it.");
            var unavailableVendors = shell.CurrentAwardChoices.Vendors.Count(v=>!v.Available);
            Require(unavailableVendors>1 && shell.VendorChoiceReason.Text==$"{unavailableVendors} choices are unavailable; point at one to see why.",
                "The unselected supplier list describes all unavailable choices instead of one arbitrary supplier.");
            var blockedVendor=shell.CurrentAwardChoices.Vendors.First(v=>!v.Available);
            var blockedIndex=Index(shell.VendorInput,blockedVendor.Id);
            shell.VendorInput.GetPopup().EmitSignal(PopupMenu.SignalName.IdFocused,blockedIndex);
            Require(shell.VendorChoiceReason.Text==blockedVendor.Reason,"Pointing at a disabled supplier shows that supplier's exact reason.");
            shell.VendorInput.GetPopup().EmitSignal(PopupMenu.SignalName.IdFocused,Index(shell.VendorInput,"vendorC"));
            Require(shell.VendorChoiceReason.Text==$"{unavailableVendors} choices are unavailable; point at one to see why.",
                "Moving to an available supplier clears the unrelated hover reason.");
            shell.InstrumentInput.GetPopup().EmitSignal(PopupMenu.SignalName.IdFocused,Index(shell.InstrumentInput,"sf44"));
            Require(shell.InstrumentChoiceReason.Text==shell.CurrentAwardChoices.Instruments.Single(i=>i.Id=="sf44").Reason,
                "Even with BPA selected, a disabled instrument explains itself while focused.");
            shell.InstrumentInput.GetPopup().EmitSignal(Popup.SignalName.PopupHide);
            Require(!shell.InstrumentChoiceReason.Visible && shell.InstrumentChoiceReason.Text=="",
                "Closing the list restores the selected BPA's empty reason line.");
            Require(shell.Session.StateHash==hash,"Every list, hover and blocked attempt is read-only.");
            await Capture("bpa-first",shell.VendorInput,true);
            await Capture("bpa-selected",shell.InstrumentInput);
            await ReachFunding();
            Pick(shell.VendorInput,"vendorC");
            shell.QuantityInput.Value += 1;
            Require(shell.PendingAward is null && shell.AwardPreview is null,"Editing an amount cannot bypass a missing ordering vehicle.");
            Pick(shell.VehicleInput,"bosBpa");
            Require(shell.PendingAward is not null && shell.AwardPreview is not null,"A complete structural selection reaches the real amount-dependent preview.");
            await Capture("full-award",shell.FundingInput);
            // Complete the real CE visit, then add what it revealed while a BPA is assembled.
            shell.RequirementAskButtons["ruiz"].EmitSignal(Button.SignalName.Pressed); Step(shell);
            Target("req0007"); Pick(shell.VendorInput,"vendorC"); Pick(shell.InstrumentInput,"bpaCall"); Pick(shell.VehicleInput,"bosBpa");
            Require(shell.PendingAward is not null,"The BPA remains valid before learned scope is added.");
            shell.RequirementAddButtons["operator"].EmitSignal(Button.SignalName.Pressed);
            var why=shell.Session.View!.AwardChoices("spokeBSite","power","req0007","vendorC","bpaCall")!.Vendors.Single(v=>v.Id=="vendorC").Reason!;
            Require(Selected(shell.VendorInput)=="" && Selected(shell.InstrumentInput)=="" && shell.PendingAward is null
                && shell.AwardChoiceNotice!.Contains(why),"A scope change clears newly invalid selections and retains the Sim's reason before Preview.");
            shell.VendorInput.GetPopup().EmitSignal(Popup.SignalName.PopupHide);
            Require(shell.VendorChoiceReason.Text.Contains(why),"A cleared choice keeps its reason after closing the supplier list.");
            shell.NavigateBoardItem(new BoardItem("",shell.Session.View!.Day,BoardCategory.Request,BoardSeverity.Info,BoardLinkKind.Card,"req0007","",[]));
            Require(Selected(shell.VendorInput)=="","A cleared choice is not silently restored.");
            Scenario(shell,"spoke-b");
            Require(shell.Session.Apply(new FindFinanceAction()).Accepted,"Finance fixture accepted."); Step(shell,6);
            Target("req0007"); Pick(shell.VendorInput,"vendorB"); Bindings();
            var cash=shell.CurrentAwardChoices!.Instruments.Single(i=>i.Id=="sf44");
            Require(!cash.Available && cash.Reason=="Meridian Site Services does not take cash.","The current supplier's cash restriction comes from the Sim.");
            shell.InstrumentInput.GetPopup().EmitSignal(PopupMenu.SignalName.IdFocused,Index(shell.InstrumentInput,"sf44"));
            Require(shell.InstrumentChoiceReason.Text==cash.Reason,"The no-cash option gives its exact reason when focused.");
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

            async Task ReachFunding()
            {
                var tabs=shell.GetNode<TabContainer>("%DeskTabs");shell.ShowMarket();
                await Layout();
                var scroll=shell.AwardScroll;
                var bar=scroll.GetVScrollBar();
                Require(bar.Visible && bar.Size.X>=14 && bar.MaxValue>bar.Page,
                    "The overflowing award form has a visible scrollbar wide enough to use.");
                Require(scroll.FollowFocus,"The form scroll follows keyboard focus.");
                Require(((Control)shell.FundingInput.GetParent()).GetGlobalRect().End.X<=bar.GetGlobalRect().Position.X,
                    "The scrollbar reserves its full width so it cannot cover reasons or list text.");
                shell.FundingInput.GrabFocus();await Layout();
                var clip=scroll.GetGlobalRect();var field=shell.FundingInput.GetGlobalRect();
                Require(field.Position.Y>=clip.Position.Y && field.End.Y<=clip.End.Y,
                    "Keyboard focus scrolls the funding list fully into view.");
            }
            async Task Layout()
            {
                for(var i=0;i<5;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
            async Task Capture(string name,Control focus,bool openList=false)
            {
                var args=OS.GetCmdlineUserArgs();var index=Array.IndexOf(args,"--screenshots");if(index<0)return;
                shell.DeveloperToggle.ButtonPressed=false;shell.ShowMarket();
                await Layout();
                shell.AwardScroll.EnsureControlVisible(focus);
                await Layout();
                var formScroll=shell.AwardScroll;
                Require(((Control)focus.GetParent()).GetGlobalRect().End.X<=formScroll.GetVScrollBar().GetGlobalRect().Position.X,
                    "Screenshot text stays clear of the form scrollbar.");
                if(openList) { ((OptionButton)focus).ShowPopup();await Layout(); }
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[index+1]+"-"+name+".png")==Error.Ok,"Native choice screenshot saved.");
                if(openList) ((OptionButton)focus).GetPopup().Hide();
                shell.DeveloperToggle.ButtonPressed=true;
            }
        }
        catch(Exception error){GD.PushError($"AWARD_CHOICES_SMOKE_FAIL: {error}");GetTree().Quit(1);}
    }
    private static int Index(OptionButton picker,string id)=>Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsString()==id);
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
