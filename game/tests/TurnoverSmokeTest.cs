using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class TurnoverSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-turnover-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var prep = PrepScreen.View(bundle);
            Require(shell.Session.View is null && prep.Options.All(o => shell.PrepChoices[o.Id].ButtonPressed == o.Default), "Authored defaults are shown before any Session exists.");
            Require(prep.Options.All(o => o.Says is null || shell.PrepChoices[o.Id].Text.Contains(o.Says)), "Prep uses the exact person's authored words.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string name)
            {
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1] + "-" + name + ".png") == Error.Ok, "Screenshot saved.");
            }
            await Capture("prep");
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed);
            var expected = Session.Create(bundle, 1, prep.Defaults);
            var report = expected.View.Turnover(); var hash = expected.StateHash();
            Require(shell.ReadingTurnover && shell.Session.StateHash == hash && !shell.GetNode<Button>("%NextDay").IsVisibleInTree(), "Deployment creates the exact prepped session and opens turnover, with daily controls hidden.");
            for (var i = 0; i < report.Sections.Count; i++)
            {
                var section = report.Sections[i];
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(shell.TurnoverNext.IsVisibleInTree() && shell.TurnoverNext.GetGlobalRect().End.Y <= GetViewport().GetVisibleRect().Size.Y,
                    "Continue stays on screen after the sender and body have been laid out.");
                Require(shell.CurrentTurnoverSection == section.Kind && shell.TurnoverWords.Text.Contains(section.Text), "Shop, contracts, vendors and money preserve the Sim's ordered prose.");
                if (i == 0)
                {
                    Require(report.Greeting is null || shell.TurnoverWords.Text.Contains(report.Greeting), "Greeting is shown.");
                    Require(report.Briefing is null || shell.TurnoverWords.Text.Contains(report.Briefing.Due), "What is due is shown.");
                    shell.GetNode<Button>("%NextDay").EmitSignal(Button.SignalName.Pressed);
                    shell.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
                    Require(shell.Session.StateHash == hash, "Forced daily-control signals cannot skip turnover or advance Sim.");
                }
                if (i == 1)
                {
                    shell.TurnoverBack.EmitSignal(Button.SignalName.Pressed);
                    Require(shell.CurrentTurnoverSection == TurnoverSectionKind.Shop, "Player can reread an earlier paragraph.");
                    shell.TurnoverNext.EmitSignal(Button.SignalName.Pressed);
                }
                await Capture(section.Kind.ToString().ToLowerInvariant());
                shell.TurnoverNext.EmitSignal(Button.SignalName.Pressed);
                Require(shell.Session.StateHash == hash, "Reading and moving between paragraphs is read-only.");
            }
            Require(!shell.ReadingTurnover && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 6 && shell.RequestOpenButtons.Count > 0, "Turnover ends with the first customer messages, with their card behind Open.");
            Step(shell); expected.AdvanceDay();
            Require(shell.Session.StateHash == expected.StateHash(), "First day uses precisely the selected preparations.");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("TURNOVER_SMOKE_PASS: authored prep/defaults, exact four paragraphs, day gating, reread, first customer and unchanged Sim."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"TURNOVER_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true); }
    }
}
