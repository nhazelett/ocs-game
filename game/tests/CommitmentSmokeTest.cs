using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class CommitmentSmokeTest : Node
{
    public override void _Ready()
    {
        try
        {
            foreach (var (kind, replay, choices) in new[]
            {
                (CommitmentKind.Handshake, "approach1 as written", new[] { "ratify", "park", "refuse" }),
                (CommitmentKind.OutOfScope, "approach3 uca turnkey with bridge", new[] { "deny", "ratify" })
            })
            foreach (var choice in choices)
            {
                var shell = Shell(this); Replay(shell, replay);
                while (!shell.Session.View!.Finished && !shell.Session.View.Commitments.Any(c => c.Kind == kind && c.Unresolved)) Step(shell);
                var commitment = shell.Session.View.Commitments.First(c => c.Kind == kind && c.Unresolved);
                Require(choices.All(c => shell.CommitmentChoices.ContainsKey((commitment.Id, c))), "The kind's required choices are offered.");
                var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
                var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", replay.Replace(' ', '-') + ".json"));
                var expected = LinesOfSupport.Sim.Replay.Run(bundle, log, shell.Session.CompletedDay);
                var action = new ResolveCommitmentAction { Day = expected.Day, Commitment = commitment.Id, Choice = choice };
                var result = expected.Apply(action);
                Require(result.Accepted, "The real fixture supports this decision.");
                shell.CommitmentChoices[(commitment.Id, choice)].EmitSignal(Button.SignalName.Pressed);
                Require(shell.Session.StateHash == expected.StateHash(), "The button sends exactly the Sim decision.");
                Require(shell.GetNode<Label>("%BuildStatus").Text.Contains(result.Message), "The Sim's ActionResult message is shown.");
                Require(!shell.Session.IsReplay, "A player decision takes control from future replay commands.");
                Require(shell.Session.View.Commitments.Single(c => c.Id == commitment.Id).Decision == expected.View.Commitments.Single(c => c.Id == commitment.Id).Decision, "The decided status is retained.");
                expected.AdvanceDay(); Step(shell);
                Require(shell.Session.StateHash == expected.StateHash(), "The authored ratify/deny does not overwrite the player's choice on the next step.");
                RemoveChild(shell); shell.QueueFree();
            }
            GD.Print("COMMITMENT_SMOKE_PASS: all five decisions, exact results, history and player takeover.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"COMMITMENT_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
    }
}
