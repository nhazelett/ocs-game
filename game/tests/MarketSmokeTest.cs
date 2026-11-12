using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class MarketSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-market-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed); DeployWithoutPrep(shell);
            var hash = shell.Session.StateHash;
            var card = shell.Session.View!.Cards.First(c => shell.Session.View.RequirementEditor(c.Id)?.CapabilityId == "power");
            shell.NavigateBoardItem(new LinesOfSupport.Sim.Views.BoardItem("", 1, LinesOfSupport.Sim.Views.BoardCategory.Request, LinesOfSupport.Sim.Views.BoardSeverity.Info, LinesOfSupport.Sim.Views.BoardLinkKind.Card, card.Id, "", []));
            var go = shell.RequirementMarketButton;
            go.EmitSignal(Button.SignalName.Pressed);
            Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == GameShell.RequestsTab && shell.CurrentActionEditor!.CardId == card.Id, "Requirement leads directly to its own market.");
            Require(shell.VendorInput.GetItemMetadata(shell.VendorInput.Selected).AsString() == "" && shell.PendingAward is null && shell.AwardButton.Disabled, "No vendor is ever preselected, including vehicle-only or refused suppliers.");
            var requests=shell.AwardRequestInput;
            Require(Enumerable.Range(0,requests.ItemCount).All(i=>!requests.GetItemText(i).Contains("without a request",StringComparison.OrdinalIgnoreCase)),
                "Customer requests are not mixed with unrequested support.");
            Pick(requests,"own-initiative");
            var targetPicker=shell.InitiativeTargetInput!;
            var lodging=Enumerable.Range(0,targetPicker.ItemCount).Single(i=>targetPicker.GetItemMetadata(i).AsString()=="spokeBSite.lodging");
            Require(targetPicker.GetItemText(lodging)=="Spoke B lodging", "Own-initiative targets use ordinary capability names.");
            var captureArgs=OS.GetCmdlineUserArgs();var labelsShot=Array.IndexOf(captureArgs,"--labels-screenshot");
            if(labelsShot>=0)
            {
                shell.DeveloperToggle.ButtonPressed=false;
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                targetPicker.ShowPopup();
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(captureArgs[labelsShot+1])==Error.Ok,"Native target-list screenshot saved.");
                targetPicker.GetPopup().Hide();
            }
            Pick(shell.AwardRequestInput,card.Id);
            Pick(shell.VendorInput, "vendorC");
            Require(shell.InstrumentInput.GetItemMetadata(shell.InstrumentInput.Selected).AsString() == "" && shell.PendingAward is null, "Who is selected before how; no instrument silently chosen.");
            Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "bosBpa");
            shell.QuantityInput.Value = 360; shell.DurationInput.Value = 30;
            Require(shell.Session.StateHash == hash, "Every market choice remains a pure preview.");
            PickPriorities(shell, "fast", "cheap");
            var candidate = shell.PendingAward!;
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            var preview = expected.View.Preview(candidate);
            Require(preview.Accepted && shell.AwardPreview!.TotalCents == preview.TotalCents && shell.AwardPreview.HardStops.SequenceEqual(preview.HardStops) && shell.AwardPreview.SoftGaps.SequenceEqual(preview.SoftGaps), "Market preserves exact Sim quote and all warnings.");
            var timing = shell.AwardTiming;
            Require(preview.QueueAheadHours == 0 && !timing.Text.Contains("queued") && !timing.Text.Contains("queue"), "An empty award queue adds no queue clause.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Screenshot saved.");
            }
            Require(expected.Apply(candidate).Accepted, "Real fixture accepts the chosen terms.");
            shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "Arrange support sends exactly the previewed action.");
            var queuedHours = shell.AwardPreview!.QueueAheadHours;
            timing = shell.AwardTiming;
            Require(queuedHours > 0 && timing.Text.Contains($", not counting the {queuedHours} hours already queued.")
                && !timing.Text.Contains("before allowing"), "A queued award states exactly the Sim's pending hours in ordinary prose.");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("MARKET_SMOKE_PASS: requirement entry, explicit who/then-how, pure exact quote/warnings and exact award."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"MARKET_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true); }
    }
}
