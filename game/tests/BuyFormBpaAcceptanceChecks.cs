using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class BuyFormSmokeTest
{
    private async Task CheckBpaAcceptanceWait(GameShell shell)
    {
        var input = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-23", "input-log-2.json"));
        var expected = LinesOfSupport.Sim.Replay.Run(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial")), input, stopAfterDay: 3);
        Require(expected.Apply(input.Actions.OfType<AskAboutAction>().Single(a => a.Day == 4)).Accepted, "The actual cold player's day-four ask is accepted.");
        var save = shell.AutosavePathOverride + ".bpa-wait";
        try
        {
            File.WriteAllText(save, expected.ToSave().ToJson()); shell.LoadSavedGame(save); shell.DeveloperToggle.ButtonPressed = false;
            shell.GoToMarket("tutForklift"); Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VendorInput, "harborEquipment");
            Pick(shell.VehicleInput, "equipBpa"); Pick(shell.FundingInput, "om1");
            shell.QuantityInput.Value = 3; shell.StartInput.Value = 8; shell.DurationInput.Value = 13;
            shell.AwardScroll.ScrollVertical = 0;
            for (var frame = 0; frame < 8; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var hash = shell.Session.StateHash; var log = shell.Session.CreateSave().ToJson();
            var summary = expected.View.AwardSummary(shell.PendingAward!)!;
            Require(hash == expected.StateHash() && shell.AwardPreview!.Accepted
                && shell.AwardSummaryLabel.Text == summary.Sentence
                && summary.Sentence.Contains("a BPA call tomorrow still starts on day 8 if the supplier accepts."),
                "The actual day-four quote names acceptance with the unchanged day-eight forecast and operator-learning advice.");
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--bpa-wait-screenshot");
            if (at >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Require(image.SavePng(args[at + 1]) == Error.Ok, "The actual conditional BPA quote is captured.");
            }
            var pane = shell.RequestBuyColumn.GetGlobalRect();
            GD.Print($"BPA_WAIT_LAYOUT: pane={pane}, scroll={shell.AwardScroll.GetGlobalRect()}, summary={shell.AwardSummaryLabel.GetGlobalRect()}, button={shell.AwardButton.GetGlobalRect()}");
            Require(pane.Encloses(shell.AwardSummaryLabel.GetGlobalRect()) && pane.Encloses(shell.AwardButton.GetGlobalRect())
                && shell.AwardScroll.Size.Y >= pane.Size.Y / 2,
                "The entire conditional sentence and action fit at ordinary type with at least half the pane for the form.");
            Require(shell.Session.StateHash == hash && shell.Session.CreateSave().ToJson() == log,
                "Reading the conditional quote changes neither Sim nor save.");
            GD.Print("BPA_WAIT_ACCEPTANCE_SMOKE_PASS: actual day-four quote, full acceptance condition, unchanged facts, pure Sim/save and half-pane footer.");
        }
        finally { File.Delete(save); }
    }
}
