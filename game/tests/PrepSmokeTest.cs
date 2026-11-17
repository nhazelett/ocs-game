using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PrepSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            var shell = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<GameShell>(); AddChild(shell);
            DeveloperWorkspace(shell);
            var picker = shell.GetNode<OptionButton>("%ScenarioPicker");
            var index = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemText(i) == "spoke-b");
            picker.Select(index); picker.EmitSignal(OptionButton.SignalName.ItemSelected, index);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var prep = PrepScreen.View(bundle);
            Require(shell.Session.View is null && shell.Session.StateHash is null && shell.GetNode<Button>("%NextDay").Disabled, "No session or day advance before deployment.");
            Require(shell.PrepChoices.Keys.SequenceEqual(prep.Offered.Select(o => o.Id)), "Only choices that change something here are available.");
            CheckPreparation();
            foreach (var choice in shell.PrepChoices.Values) choice.ButtonPressed = true;
            Require(!PrepScreen.Check(bundle, shell.PrepChoices.Keys).Accepted && shell.StartGameButton.Disabled, "Actual budget excess disables Deploy.");
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.View is null, "Even a forced button signal cannot deploy over budget.");
            foreach (var choice in shell.PrepChoices.Values) choice.ButtonPressed = false;
            var chosen = prep.Offered.First(o => o.PrepDays <= prep.BudgetDays);
            shell.PrepChoices[chosen.Id].ButtonPressed = true;
            Require(!shell.StartGameButton.Disabled && shell.Session.View is null, "Choosing an affordable preparation only updates the check.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Screenshot saved.");
            }
            var expected = Session.Create(bundle, 1, [chosen.Id]);
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "Deploy applies exactly the selected prep through Session.Create.");
            FinishOpening(shell);
            Step(shell); expected.AdvanceDay();
            Require(shell.Session.StateHash == expected.StateHash(), "First day preserves the selected preparation's actual effects.");
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, index);
            Require(shell.Session.View is null && prep.Offered.All(o => shell.PrepChoices[o.Id].ButtonPressed == o.Default), "Scenario reload returns to authored prep defaults.");
            Replay(shell);
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json"));
            Require(shell.Session.StateHash == LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 0).StateHash(), "Replay starts with its own recorded prep.");
            void CheckPreparation()
            {
                var shown = shell.Session.Prep!;
                var page = PlayScreen.Descendants(shell).Single(n => n.Name == "PrepScreen");
                var labels = PlayScreen.Descendants(page).OfType<Label>().ToArray();
                Require(shell.PrepChoices.Keys.SequenceEqual(shown.Offered.Select(o => o.Id))
                    && shown.Offered.All(o => shell.PrepChoices[o.Id].ButtonPressed == o.Default), "Only effective choices and their exact defaults are offered.");
                Require(labels.Where(l => l.HasMeta("prep_changes")).Select(l => l.Text).SequenceEqual(shown.Offered.Select(o => o.Changes)),
                    "Every offered choice displays the Sim's exact effect sentence in view.");
                Require(labels.Where(l => l.HasMeta("prep_where")).Select(l => l.Text).SequenceEqual(shown.Where is null ? [] : new[] { shown.Where }),
                    "The scenario's exact place briefing opens preparation.");
                Require(shell.PrepChoices.Values.All(c => c.TooltipText.Length == 0), "Effects no longer depend on hovering or carry the obsolete no-effect text.");
            }
            void Deploy(string scenario, params string[] picks)
            {
                var at = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemText(i) == scenario);
                picker.Select(at); picker.EmitSignal(OptionButton.SignalName.ItemSelected, at); CheckPreparation();
                foreach (var toggle in shell.PrepChoices.Values) toggle.ButtonPressed = false;
                foreach (var id in picks) shell.PrepChoices[id].ButtonPressed = true;
                shell.StartGameButton.EmitSignal(Button.SignalName.Pressed); FinishOpening(shell);
            }
            void CheckOnFile(string card, bool hasAgreements)
            {
                var hash = shell.Session.StateHash;
                shell.NavigateBoardItem(new BoardItem("", shell.Session.View!.Day, BoardCategory.Request, BoardSeverity.Info, BoardLinkKind.Card, card, "", []));
                var model = shell.Session.View!.WhereThingsStand().Single(r => r.CardId == card);
                var pane = shell.GetNode<VBoxContainer>("%DeskTabs/Requests/RequirementPane/Body/Body/Contents");
                var lines = pane.GetChildren().OfType<Label>().Where(l => l.HasMeta("request_on_file")).ToArray();
                Require(lines.Select(l => l.Text).SequenceEqual(model.OnFile) && (lines.Length > 0) == hasAgreements,
                    "The request shows every exact on-file agreement, and clears them when none are known.");
                var deadline = pane.GetChildren().OfType<Label>().SingleOrDefault(l => l.HasMeta("request_order_by"));
                Require(deadline is null || lines.All(l => l.GetIndex() > deadline.GetIndex()), "Agreements sit below the order-by sentence.");
                Require(hash == shell.Session.StateHash, "Reading agreements is pure.");
            }
            Deploy("spoke-b", "pullVehicles", "partnerAgreements"); CheckOnFile("req0007", true);
            Deploy("tutorial", "pullVehicles"); Step(shell, 3); CheckOnFile("tutForklift", true);
            Deploy("spoke-b"); CheckOnFile("req0007", false);
            shell.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("PREP_SMOKE_PASS: effective choices/defaults, exact visible place/effect sentences, budget gates, selected prep, first day, legacy replay and on-file agreements."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PREP_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
