using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class HelpersSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            shell = Shell(this);
            var actions = new List<PlayerAction>(); shell.Session.ActionAccepted += actions.Add;
            void Open(string card) => shell.NavigateBoardItem(new BoardItem("", shell.Session.View!.Day, BoardCategory.Request,
                BoardSeverity.Info, BoardLinkKind.Card, card, "", []));
            Label[] Labels() => shell.RequestHelperRows.FindChildren("*", "Label", true, false).OfType<Label>().ToArray();
            void Check(string card)
            {
                var helpers = shell.Session.View!.RequirementEditor(card)!.WhoCanHelp!;
                var rows = shell.RequestHelperRows.FindChildren("*", "VBoxContainer", true, false).OfType<VBoxContainer>().Where(r => r.HasMeta("helper")).ToArray();
                Require(rows.Length == helpers.Count, "Every Sim helper is shown, in order.");
                foreach (var (row, helper) in rows.Zip(helpers))
                {
                    var words = row.FindChildren("*", "Label", true, false).OfType<Label>().Select(l => l.Text).ToArray();
                    Require(row.GetMeta("helper_state").AsString() == helper.State && words.Contains(helper.Does)
                        && words.Contains(helper.Sentence), "Helper description, pending teammate/day and state bind exactly.");
                    Require(words.Any(t => t.Contains(helper.Informs ? "Informs this request" : "Opens ")), "Information and further action are distinguished.");
                    Require((helper.Answer is null) == !row.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.HasMeta("helper_answer")),
                        "No answer is exposed before Sim provides it.");
                    if (helper.Answer is not null) Require(words.Contains(helper.Answer), "Completed help keeps the exact answer.");
                    var cost = $"This takes about {helper.Hours} shop {(helper.Hours == 1 ? "hour" : "hours")}.";
                    Require(words.Contains(cost) == (helper.State == HelperView.Asking),
                        "The work-hours line appears only while someone is on it; an answered entry ends with its answer.");
                    Require(row.FindChildren("*", "Button", true, false).Count == (helper.Start is null ? 0 : 1), "Pending and completed help have no duplicate start.");
                }
            }
            Scenario(shell, "tutorial"); Step(shell, 3); Open("tutForklift"); Check("tutForklift");
            await Frames();
            Require(shell.RequestHelperRows.FindChildren("HelperName", "Label", true, false).OfType<Label>().All(l => l.Size.Y < 70),
                "Helper names use the available width beside portraits, never wrap as a column of letters.");
            var start = shell.Session.View!.RequirementEditor("tutForklift")!.WhoCanHelp!.First(h => h.Person?.Id == "dunn");
            Require(start.Answer is null && !Labels().Any(l => l.HasMeta("helper_answer")), "First view reveals no answers.");
            await Capture("before", "ask:dunn");
            var stale = shell.RequestHelperButtons["ask:dunn"];
            stale.EmitSignal(Button.SignalName.Pressed); Check("tutForklift");
            Require(actions.Last() == start.Start, "Ask sends the exact supplied typed target.");
            var count = actions.Count; stale.EmitSignal(Button.SignalName.Pressed);
            Require(actions.Count == count, "A stale callback cannot repeat an accepted visit.");
            await Capture("pending", "ask:dunn");
            while (shell.Session.View!.RequirementEditor("tutForklift")!.WhoCanHelp!.First(h => h.Person?.Id == "dunn").State == HelperView.Asking) Step(shell);
            Open("tutForklift"); Check("tutForklift");
            Require(shell.RequirementAddButtons.ContainsKey("operator"), "The answer unlocks the existing explicit Add what you learned action.");
            await Capture("answered", "ask:dunn");
            var vendor = shell.Session.View!.RequirementEditor("tutForklift")!.WhoCanHelp!.First(h => h.Kind == "vendor");
            shell.RequestHelperButtons["vendor:" + vendor.Person!.Id].EmitSignal(Button.SignalName.Pressed);
            Require(actions.Last() == vendor.Start, "Vendor help sends its supplied vendor target."); Check("tutForklift");

            Scenario(shell, "spoke-b"); Open("req0007"); Check("req0007");
            var j4 = shell.Session.View!.RequirementEditor("req0007")!.WhoCanHelp!.First(h => h.Kind == "j4");
            var before = shell.Session.StateHash;
            shell.RequestHelperButtons["j4:" + j4.CapabilityId].EmitSignal(Button.SignalName.Pressed);
            var proposed = (EngageJ4Action)shell.VisitOptions["j4"].Action;
            Require(shell.Session.StateHash == before && proposed.Site == ((EngageJ4Action)j4.Start!).Site
                && proposed.Capability == j4.CapabilityId && proposed.Quantity == shell.Session.View.ActionEditor(proposed.Site, proposed.Capability)!.PeakDemandUnits,
                "J4 opens a pure preview at the supplied site/capability with its own demand and units.");
            shell.VisitButtons["j4"].EmitSignal(Button.SignalName.Pressed); Open("req0007"); Check("req0007");
            Require(!shell.RequestHelperButtons.ContainsKey("j4:fuel"), "Pending J4 help offers no second start.");

            // The general J4 form must honor a non-fuel target, even though current request helpers select fuel.
            Scenario(shell, "sustainment");
            var site = shell.Session.View!.Scenario.Sites.First().Id;
            shell.OpenJ4Support(new EngageJ4Action { Site = site, Capability = "water", Day = shell.Session.View.Day });
            proposed = (EngageJ4Action)shell.VisitOptions["j4"].Action;
            Require(proposed.Capability == "water" && proposed.Site == site && shell.VisitButtons["j4"].Text.Contains("water")
                && proposed.Quantity == shell.Session.View.ActionEditor(site, "water")!.PeakDemandUnits, "Non-fuel J4 requests retain their own target and labels.");
            Scenario(shell, "tutorial"); Step(shell, 3); Open("tutForklift");
            stale = shell.RequestHelperButtons["ask:dunn"]; Open("tutWater"); count = actions.Count; stale.EmitSignal(Button.SignalName.Pressed);
            Require(actions.Count == count, "An old helper cannot act on another request.");
            while (!shell.Session.View!.Finished) Step(shell);
            Open("tutForklift"); before = shell.Session.StateHash;
            foreach (var button in shell.RequestHelperButtons.Values.ToArray()) { Require(button.Disabled, "Finished helper actions are disabled."); button.EmitSignal(Button.SignalName.Pressed); }
            Require(before == shell.Session.StateHash, "Forced finished-game helper actions change no state.");
            File.Delete(shell.AutosavePath); shell.QueueFree(); shell = null; await Frames();
            GC.Collect(); GC.WaitForPendingFinalizers(); await Frames();
            GD.Print("HELPERS_SMOKE_PASS: exact helpers and typed starts, hidden answers, pending/completed state, stale/finished guards and non-fuel J4 targets."); GetTree().Quit();

            async Task Capture(string suffix, string key)
            {
                var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--screenshots"); if (at < 0) return;
                shell!.DeveloperToggle.ButtonPressed = false;
                var target = shell.RequestHelperRows.FindChildren("*", "VBoxContainer", true, false).OfType<VBoxContainer>().Single(r => r.HasMeta("helper") && r.GetMeta("helper").AsString() == key);
                var scroll = (ScrollContainer)shell.RequestHelperRows.GetParent().GetParent();
                await Frames(); scroll.ScrollVertical = (int)(target.Position.Y + target.GetParent<Control>().Position.Y + shell.RequestHelperRows.Position.Y); await Frames();
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var picture = GetViewport().GetTexture().GetImage();
                Require(picture.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Native helper screenshot saved.");
            }
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError("HELPERS_SMOKE_FAIL: " + error); GetTree().Quit(1); }
    }
    private async Task Frames() { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
}
