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
            Require(shell.PrepChoices.Keys.SequenceEqual(prep.Options.Select(o => o.Id)), "All actual prep choices are available.");
            foreach (var choice in shell.PrepChoices.Values) choice.ButtonPressed = true;
            Require(!PrepScreen.Check(bundle, shell.PrepChoices.Keys).Accepted && shell.StartGameButton.Disabled, "Actual budget excess disables Deploy.");
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.View is null, "Even a forced button signal cannot deploy over budget.");
            foreach (var choice in shell.PrepChoices.Values) choice.ButtonPressed = false;
            var chosen = prep.Options.First(o => o.PrepDays <= prep.BudgetDays);
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
            Require(shell.Session.View is null && prep.Options.All(o => shell.PrepChoices[o.Id].ButtonPressed == o.Default), "Scenario reload returns to authored prep defaults.");
            Replay(shell);
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json"));
            Require(shell.Session.StateHash == LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 0).StateHash(), "Replay starts with its own recorded prep.");
            GD.Print("PREP_SMOKE_PASS: before-session choices, budget gates, exact selected prep, first day, reset and replay."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"PREP_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
