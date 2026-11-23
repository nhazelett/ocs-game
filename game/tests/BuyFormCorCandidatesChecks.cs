using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class BuyFormSmokeTest
{
    private async Task CheckCorCandidates(GameShell shell)
    {
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var input = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-23", "input-log-1.json"));
        var expected = LinesOfSupport.Sim.Replay.Run(bundle, input, stopAfterDay: 4);
        foreach (var action in input.Actions.Where(a => a.Day == 5).TakeWhile(a => a is not AwardAction))
            Require(expected.Apply(action).Accepted, "The cold player's actual day-five actions are accepted.");
        var save = shell.AutosavePathOverride + ".cor-candidates";
        try
        {
            File.WriteAllText(save, expected.ToSave().ToJson()); shell.LoadSavedGame(save); shell.DeveloperToggle.ButtonPressed = false;
            shell.GoToMarket("tutForklift");
            Pick(shell.InstrumentInput, "purchaseOrder"); Pick(shell.VendorInput, "coastalRentals"); Pick(shell.FundingInput, "om1");
            shell.QuantityInput.Value = 3; shell.StartInput.Value = 8; shell.DurationInput.Value = 13;
            PickPriorities(shell, "fast", "good");
            var hash = shell.Session.StateHash; var log = shell.Session.CreateSave().ToJson();
            foreach (var cor in new[] { "", "corPratt", "" })
            {
                Pick(shell.CorInput!, cor);
                for (var frame = 0; frame < 8; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(shell.PendingAward!.Cor == (cor == "" ? null : cor), "None, Pratt and None bind exactly to the pending award.");
                Require(shell.CorReasonLabels.Select(label => label.Text).SequenceEqual(
                    shell.Session.View!.CorChoices("mhe").Select(choice => "Possible COR: " + choice.Why)),
                    "Every candidate's complete Sim-owned facts are explicitly candidate information, independent of selection.");
                Require(shell.Session.StateHash == hash && shell.Session.CreateSave().ToJson() == log,
                    "Choosing a monitor and reading its facts applies no action and changes neither Sim nor save.");
            }
            var reason = shell.CorReasonLabels.First();
            shell.AwardScroll.EnsureControlVisible(reason);
            for (var frame = 0; frame < 8; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var pane = shell.RequestBuyColumn.GetGlobalRect();
            Require(shell.AwardScroll.GetGlobalRect().Encloses(reason.GetGlobalRect())
                && shell.AwardScroll.Size.Y >= pane.Size.Y / 2 && pane.Encloses(shell.AwardButton.GetGlobalRect())
                && pane.Encloses(shell.AwardSummaryLabel.GetGlobalRect()), "The full candidate paragraph fits at ordinary type above the complete pinned footer.");
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--cor-candidates-screenshot");
            if (at >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[at + 1]) == Error.Ok, "The actual cold-play candidate screen is captured.");
            }
            GD.Print("COR_CANDIDATES_SMOKE_PASS: exact cold day-five fixture, None/Pratt/None, all candidate facts, pure Sim/save and full half-pane footer.");
        }
        finally { File.Delete(save); }
    }
}
