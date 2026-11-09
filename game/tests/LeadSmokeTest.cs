using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class LeadSmokeTest : Node
{
    public override async void _Ready()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-lead-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = scene.Instantiate<GameShell>(); shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed); DeployWithoutPrep(shell);
            var hash = shell.Session.StateHash;
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = 0; shell.DeveloperToggle.ButtonPressed = false;
            var lead = shell.Session.View!.MorningBoard().Lead!;
            Require(shell.DisplayedLead == lead && shell.LeadWords.Text == lead.Text && !shell.BoardDetailsVisible, "Day-one Sim lead is prominent and the other work starts collapsed.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshots");
            async Task Capture(string name)
            {
                if (shot < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(shell.LeadOpen.GetGlobalRect().End.Y <= GetViewport().GetVisibleRect().Size.Y, "Lead Open is on screen.");
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1] + "-" + name + ".png") == Error.Ok, "Screenshot saved.");
            }
            await Capture("day1");
            shell.LeadOpen.EmitSignal(Button.SignalName.Pressed);
            Require(lead.LinkKind == BoardLinkKind.Card && shell.SelectedCard == lead.LinkId && shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 1, "First customer's lead opens the exact card.");
            Require(shell.Session.StateHash == hash, "Opening a lead is read-only.");
            shell.BoardDetailsToggle.ButtonPressed = true;
            Require(shell.BoardDetailsVisible, "Player can still inspect all other work and sites.");
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            do { expected.AdvanceDay(); } while (!expected.Finished && expected.View.MorningBoard().Lead is null);
            shell.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && !shell.BoardDetailsVisible, "Continue stops at the next actual Sim lead and returns to the uncluttered morning.");
            shell.DeveloperToggle.ButtonPressed = true; Replay(shell); Step(shell, 10); shell.DeveloperToggle.ButtonPressed = false;
            lead = shell.Session.View!.MorningBoard().Lead!; hash = shell.Session.StateHash;
            Require(!shell.GetNode<Label>("%BuildStatus").Visible, "Replay and day-result diagnostics disappear when developer tools are turned off.");
            Require(shell.DisplayedLead == lead && shell.LeadWords.Text == lead.Text && !shell.BoardDetailsVisible, "Morning after day 10 binds the actual lead without re-ranking it.");
            await Capture("day11");
            shell.DeveloperToggle.ButtonPressed = true;
            Require(shell.GetNode<Label>("%BuildStatus").Visible && shell.GetNode<Label>("%BuildStatus").Text.Contains("Replay"), "Developer opt-in restores the diagnostic status.");
            shell.DeveloperToggle.ButtonPressed = false;
            shell.LeadOpen.EmitSignal(Button.SignalName.Pressed);
            var commitment = shell.Session.View.Commitments.FirstOrDefault(c => c.ArrangementId == lead.LinkId && c.Decision == CommitmentDecision.Open);
            if (lead.LinkKind == BoardLinkKind.Arrangement && commitment is not null)
                Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 6, "Unresolved commitment lead opens its decision controls in Inbox.");
            Require(shell.Session.StateHash == hash, "Inspecting the later decision also leaves state unchanged.");
            var foundCommitment = false;
            while (!shell.Session.View.Finished)
            {
                var current = shell.Session.View.MorningBoard().Lead;
                if (current is { Kind: LeadKind.Decision, LinkKind: BoardLinkKind.Arrangement }
                    && shell.Session.View.Commitments.Any(c => c.ArrangementId == current.LinkId && c.Decision == CommitmentDecision.Open))
                {
                    hash = shell.Session.StateHash; shell.LeadOpen.EmitSignal(Button.SignalName.Pressed);
                    Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 6 && shell.Session.StateHash == hash,
                        "A real commitment lead reaches its Inbox decision without taking that decision.");
                    foundCommitment = true; break;
                }
                Step(shell);
            }
            Require(foundCommitment, "The authored replay exercised an open commitment lead.");
            shell.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("LEAD_SMOKE_PASS: exact day-one/later leads, collapsed details, true Continue stop and read-only decision links."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"LEAD_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true); }
    }
}
