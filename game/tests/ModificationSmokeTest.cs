using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ModificationSmokeTest : Node
{
    public override async void _Ready()
    {
        var path = Path.Combine(Path.GetTempPath(), "ocs-modification-" + Guid.NewGuid() + ".json");
        try
        {
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var expected = Session.Create(bundle, 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
            while (expected.Day < 4) expected.AdvanceDay();
            Require(expected.Apply(new AwardAction { Id = "fork", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3,
                StartDay = 8, DurationDays = 13, FundingLine = "om1" }).Accepted, "The original forklift order is accepted.");
            var beforeKnowledge = expected.ToSave();
            while (expected.Day < 8) expected.AdvanceDay();
            var shell = Shell(this);
            await Load();
            var blocked = Modify();
            Require(blocked.Disabled && Words().Any(l => l.Text == expected.View.ModifyOptions("fork")!.Why),
                "A request that has not learned it needs drivers shows Sim's refusal beside the disabled control.");
            expected = LinesOfSupport.Sim.Replay.Resume(bundle, beforeKnowledge);
            Require(expected.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn" }).Accepted, "Dunn can be asked.");
            while (expected.Day < 8) expected.AdvanceDay();
            Require(expected.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" }).Accepted, "The learned driver part is added to the request.");
            expected.AdvanceDay(); await Load();
            var before = shell.Session.StateHash;
            var button = Modify(); var picker = button.GetParent().GetChildren().OfType<AssigneePicker>().Single();
            Require(button.Text == expected.View.ModifyOptions("fork")!.Button && !button.Disabled && picker.Selected == "you", "The day-nine contract offers the named part and defaults to you.");
            var action = new ModifyAction { Day = expected.Day, Arrangement = "fork", Lines = expected.View.ModifyOptions("fork")!.Parts.ToList() };
            Require(picker.Choices.SequenceEqual(expected.View.AssigneeChoices(action)), "The picker shows exact Sim forecasts for this modification.");
            Require(Cost(button).Text.Contains(expected.View.ModifyOptions("fork")!.Says!), "The button carries Sim's exact hours, price, obligation point and first day.");
            await Capture("before", button);
            shell.CloseContractView(); button.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == before, "A control on a closed contract cannot change it.");
            shell.OpenContractView("fork"); button = Modify(); picker = button.GetParent().GetChildren().OfType<AssigneePicker>().Single();
            picker.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == before && Cost(button).Text.Contains("Novak")
                && Cost(button).Text.Contains(expected.View.ModifyOptions("fork", "novak")!.Says!), "Choosing Novak is pure and refreshes the exact named cost.");
            await Capture("assigned", button);
            var result = expected.Apply(action with { Assignee = "novak" }); Require(result.Accepted, result.Message);
            button.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && Modify().Disabled, "The button sends the exact parts and assignee once and disables while in work.");
            Require(Words().Any(l => l.Text == expected.View.ModifyOptions("fork")!.Why), "In-work refusal is shown as text.");
            Require(shell.Session.View!.Team.Single(m => m.Id == "novak").Work.Any(w => w.Label.StartsWith("Add operators")), "Novak receives the actual modification work.");
            button.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "A repeated stale click cannot duplicate the change.");
            while (expected.Work.Any(w => w.Kind == WorkKind.Modify && w.CompletedDay is null)) { expected.AdvanceDay(); Step(shell); }
            Require(shell.Session.StateHash == expected.StateHash(), "Signing and advancing match independent Sim execution.");
            shell.OpenContractView("fork");
            foreach (var line in expected.View.ModifyOptions("fork")!.Added)
                Require(Words().Any(l => l.HasMeta("contract_added") && l.Text == line), "The contract names each added part and first day.");
            await Capture("after", Words().First(l => l.HasMeta("contract_added")));
            shell.CloseContractView();
            var decision = expected.View.Decisions().Single(d => d.Sentence.Contains("Add operators"));
            Require(shell.DecisionRows.GetChildren().Any(n => n.HasMeta("decision_sentence") && n.GetMeta("decision_sentence").AsString() == decision.Sentence
                && n.GetMeta("decision_now").AsString() == decision.Now), "The decisions list shows Sim's signed change and its first day.");
            Require(LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave()).StateHash() == expected.StateHash(), "Parts and assignee survive save/replay.");
            shell.QueueFree(); await Frames();
            GD.Print("MODIFICATION_SMOKE_PASS: exact parts, refusals, assignee quotes, stale guards, signing, contract and decision readback, save/replay."); GetTree().Quit();

            Button Modify() => PlayScreen.Descendants(shell.ContractView).OfType<Button>().Single(b => b.HasMeta("contract_modify") && b.GetMeta("contract_modify").AsString() == "fork");
            IEnumerable<Label> Words() => PlayScreen.Descendants(shell.ContractView).OfType<Label>();
            Label Cost(Button b) => b.GetParent().GetChildren().OfType<Label>().Single(l => l.HasMeta("action_cost"));
            async Task Frames() { for (var f = 0; f < 8; f++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Load()
            {
                File.WriteAllText(path, expected.ToSave().ToJson()); shell.LoadSavedGame(path); shell.DeveloperToggle.ButtonPressed = false;
                shell.OpenContractView("fork"); await Frames();
                Require(shell.Session.StateHash == expected.StateHash(), "The fixture loads through the real saved-game path.");
            }
            async Task Capture(string suffix, Control target)
            {
                await Frames();
                if (target is Button b) target = Cost(b);
                var scroll = PlayScreen.Descendants(shell.ContractView).OfType<ScrollContainer>().Single(); scroll.EnsureControlVisible(target); await Frames();
                Require(scroll.GetGlobalRect().Encloses(target.GetGlobalRect()) && GetViewport().GetVisibleRect().Encloses(shell.ContractView.GetGlobalRect()), "The changed control is reachable in the actual viewport with one body scroll.");
                var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--screenshots"); if (at < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage(); Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Modification screenshot saved.");
            }
        }
        catch (Exception error) { GD.PushError($"MODIFICATION_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { File.Delete(path); }
    }
}
