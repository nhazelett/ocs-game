using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

internal static class IncurredCostSmokeChecks
{
    internal static async Task Run(Node parent)
    {
        var shell=Shell(parent);
        var log=InputLog.Load(Path.Combine(GamePaths.ScenarioRoot,"..","docs","reviews","plays","2026-11-22-8","input-log-1.json"));
        var expected=LinesOfSupport.Sim.Replay.Run(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot,"tutorial")),log,stopAfterDay:10);
        var path=Path.Combine(Path.GetTempPath(),"ocs-cost-"+Guid.NewGuid()+".json");
        try
        {
            File.WriteAllText(path,expected.ToSave().ToJson());shell.LoadSavedGame(path);
            var view=shell.Session.View!;var water=view.Arrangements.Single(a=>a.Instrument=="gpc"&&a.CapabilityId=="water");
            var hash=shell.Session.StateHash;
            var row=shell.GetNode<TabContainer>("%DeskTabs").GetNode("Payments/LedgerContext/Contents").GetChildren().OfType<Button>().Single(b=>b.HasMeta("arrangement")&&b.GetMeta("arrangement").AsString()==water.Id);
            Require(row.Text.Contains("cost incurred "+DeskUi.Money(water.IncurredCents))&&row.Text.Contains("Disputed supply was paid in full; open for details."),"Payment row distinguishes actual charged cost from physical half delivery.");
            shell.OpenContractView(water.Id);
            var cost=shell.ContractView.FindChildren("*","Label",true,false).OfType<Label>().Single(l=>l.Text.Contains("Money committed"));
            Require(cost.Text.Contains(view.DisputedDaysLine(water.Id)!)&&!cost.Text.Contains("cost of support received"),"Contract detail uses the same explanation and numeric facts.");
            var args=OS.GetCmdlineUserArgs();var at=Array.IndexOf(args,"--cost-screenshot");
            if(at>=0)
            {
                for(var i=0;i<4;i++)await parent.ToSignal(parent.GetTree(),SceneTree.SignalName.ProcessFrame);
                for(Node? p=cost.GetParent();p is not null;p=p.GetParent())if(p is ScrollContainer scroll){scroll.EnsureControlVisible(cost);break;}
                for(var i=0;i<4;i++)await parent.ToSignal(parent.GetTree(),SceneTree.SignalName.ProcessFrame);
                await parent.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=parent.GetViewport().GetTexture().GetImage();Require(image.SavePng(args[at+1])==Error.Ok,"Native cost screenshot saved.");
            }
            Require(shell.Session.StateHash==hash&&hash==expected.StateHash(),"Rendering leaves all costs and the exact resumed simulation unchanged.");
        }
        finally
        {
            File.Delete(path);File.Delete(shell.AutosavePath);shell.QueueFree();
            await parent.ToSignal(parent.GetTree(),SceneTree.SignalName.ProcessFrame);await parent.ToSignal(parent.GetTree(),SceneTree.SignalName.ProcessFrame);
        }
    }
}
