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
            Require(!host.Available && shell.VendorInput.GetItemTooltip(Index(shell.VendorInput, host.Id)) == host.Reason
                && !shell.VendorChoiceReason.Text.Contains(host.Reason!),"The unavailable supplier keeps its exact host nation reason in the picker before preview, beside any route guidance.");
            Pick(shell.VendorInput,"hubUtilities");
            Require(Selected(shell.VendorInput)=="" && shell.PendingAward is null,"A disabled supplier cannot be chosen even by a forced selection signal.");
            await Capture("host-nation",shell.VendorInput,true);
            Target("spokeBSite.fuel"); Pick(shell.InstrumentInput,"bpaCall");
            Require(shell.CurrentAwardChoices!.Vendors.All(v => !v.Available)
                && shell.VendorChoiceReason.Text.Contains("To open available choices, choose")
                && shell.VendorChoiceReason.Text.Contains(shell.Session.View!.Rules.Instruments["purchaseOrder"].Label), "When a BPA leaves every supplier unavailable, the visible line names a usable arrangement from the Sim projection.");
            await Capture("no-supplier", shell.VendorChoiceReason);
            Target("req0007"); Pick(shell.InstrumentInput,"bpaCall"); Bindings();
            Require(shell.CurrentAwardChoices!.Vendors.Single(v=>v.Id=="vendorC").Available
                && shell.CurrentAwardChoices.Vendors.Where(v=>v.Id!="vendorC").All(v=>!v.Available),"Instrument-first selection re-asks the Sim and greys suppliers without that BPA.");
            var unavailableVendors = shell.CurrentAwardChoices.Vendors.Count(v=>!v.Available);
            Require(unavailableVendors > 1, "The instrument-first fixture has several supplier refusals.");
            void VisibleReasons()
            {
                foreach (var option in shell.CurrentAwardChoices!.Vendors.Where(o => !o.Available))
                {
                    var index = Index(shell.VendorInput, option.Id);
                    var reason = option.Reason ?? "Unavailable today.";
                    if (reason.StartsWith(option.Label + " ", StringComparison.OrdinalIgnoreCase)) reason = reason[(option.Label.Length + 1)..];
                    Require(shell.VendorInput.IsItemDisabled(index) && shell.VendorInput.GetItemText(index).EndsWith(DeskUi.ListLabel(reason))
                        && shell.VendorInput.GetItemTooltip(index) == option.Reason,
                        "Every supplier refusal remains complete on its disabled picker entry.");
                }
                foreach (var (options, label) in new[] { (shell.CurrentAwardChoices.Vehicles, shell.VehicleChoiceReason),
                    (shell.CurrentAwardChoices.Funding, shell.FundingChoiceReason) })
                    Require(options.Where(o => !o.Available).All(o => label.Text.Contains(o.Reason!)), "Every vehicle and funding refusal stays visible in full without pointer focus.");
            }
            VisibleReasons();
            var priorReasons = shell.VendorChoiceReason.Text;
            shell.VendorInput.GetPopup().EmitSignal(PopupMenu.SignalName.IdFocused,Index(shell.VendorInput,"vendorC"));
            shell.InstrumentInput.GetPopup().EmitSignal(Popup.SignalName.PopupHide);
            Require(shell.VendorChoiceReason.Text == priorReasons, "Opening and closing a popup never hides refusal reasons.");
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
            // DESK-77 (D34): a request can be met in parts. Adding what Capt Ruiz said keeps Harbor Equipment's call valid;
            // the quote names what it leaves out instead of greying the supplier.
            var harbor=shell.Session.View!.AwardChoices("spokeBSite","power","req0007","vendorC","bpaCall")!.Vendors.Single(v=>v.Id=="vendorC");
            Require(harbor.Available && Selected(shell.VendorInput)=="vendorC" && Selected(shell.InstrumentInput)=="bpaCall" && shell.PendingAward is not null,
                "Learned scope keeps a supplier who brings part of the request selected.");
            var leftOut=shell.Session.View!.Preview(shell.PendingAward!).SoftGaps;
            Require(leftOut.Count>0 && leftOut[0].StartsWith("Harbor Equipment Co brings power without operators",StringComparison.Ordinal),
                "The quote's first warning names the part the supplier leaves out.");
            await Capture("parts-kept",shell.VendorInput,true);
            await Capture("parts-quote",shell.FundingInput);
            Scenario(shell,"spoke-b");
            Require(shell.Session.Apply(new FindFinanceAction()).Accepted,"Finance fixture accepted."); Step(shell,6);
            Target("req0007"); Pick(shell.VendorInput,"vendorB"); Bindings();
            var cash=shell.CurrentAwardChoices!.Instruments.Single(i=>i.Id=="sf44");
            Require(!cash.Available && cash.Reason=="Meridian Site Services does not take cash.","The current supplier's cash restriction comes from the Sim.");
            shell.InstrumentInput.GetPopup().EmitSignal(PopupMenu.SignalName.IdFocused,Index(shell.InstrumentInput,"sf44"));
            Require(shell.InstrumentInput.GetItemTooltip(Index(shell.InstrumentInput, "sf44")) == cash.Reason && !shell.InstrumentChoiceReason.Visible, "The disabled cash option keeps its exact reason in the arrangement picker, without a separate warning about another buy.");
            hash=shell.Session.StateHash; Pick(shell.InstrumentInput,"sf44");
            Require(Selected(shell.InstrumentInput)=="" && shell.PendingAward is null,"A disabled cash route never reaches Preview.");
            await Capture("no-cash",shell.InstrumentInput,true);
            Pick(shell.VendorInput,"vendorA"); Pick(shell.InstrumentInput,"sf44"); Bindings();
            Require(shell.FundingInput.Disabled && Selected(shell.FundingInput)=="" && shell.PendingAward?.FundingLine is null
                && shell.FundingChoiceReason.Text=="A cash buy is paid from the cash box, not a funding line.","Cash greys and clears funding with the exact explanation.");
            Require(shell.Session.StateHash==hash,"Availability changes and previews never change the world.");
            File.Delete(shell.AutosavePath);shell.QueueFree();
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GD.Print("AWARD_CHOICES_SMOKE_PASS: exact availability/reasons, blocked signals, instrument-first filtering, parts left out kept selectable, cash funding and pure preview.");GetTree().Quit();

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
                if(openList)
                {
                    ((OptionButton)focus).ShowPopup(); await Layout();
                    var popup = ((OptionButton)focus).GetPopup();
                    Require(new Rect2I(0,0,1280,800).Encloses(new Rect2I(popup.Position,popup.Size)),
                        $"Complete supplier explanations fit in the native window: {popup.Position}, {popup.Size}.");
                }
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
