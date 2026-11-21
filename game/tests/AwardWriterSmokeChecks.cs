using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

// Runs through AwardContextSmokeTest in the existing CI suite.
internal static class AwardWriterSmokeChecks
{
    internal static async Task Run(Node scene)
    {
        var shell = Shell(scene); Scenario(shell, "tutorial", preview: false); Step(shell, 3);
        shell.DeveloperToggle.ButtonPressed = false;
        Require(shell.SendAction(new FindFinanceAction { Day = 4 }).Accepted, "The player's existing task loads the queue before a new buy.");
        PickAwardTarget(shell, "tutForklift"); shell.ShowMarket();
        Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VendorInput, "harborEquipment"); Pick(shell.VehicleInput, "equipBpa");
        var hash = shell.Session.StateHash;
        Require(shell.PendingAward!.Assignee == "you" && shell.AwardWriterPicker!.Selected == "you", "Every new buy defaults to the player.");
        Match(); var ownHours = shell.AwardPreview!.ShopHoursMax;
        await Capture("you");
        var old = shell.AwardWriterPicker!;
        old.ChoiceButtons["avery"].EmitSignal(Button.SignalName.Pressed);
        Require(shell.PendingAward!.Assignee == "avery" && shell.AwardWriterPicker!.Selected == "avery"
            && shell.AwardPreview!.ShopHoursMax < ownHours, "Choosing Avery changes the exact pending action and their quoted pace.");
        Match(); await Capture("avery");
        shell.QuantityInput.Value = 3; Match();
        Require(shell.PendingAward!.Assignee == "avery", "Editing terms retains the writer and refreshes every forecast.");
        var obsolete = shell.AwardWriterPicker!;
        PickAwardTarget(shell, "tutWater"); Pick(shell.InstrumentInput, "gpc"); Pick(shell.VendorInput, "islandMart");
        Require(shell.AwardPreview!.ShopHoursMax == 0 && shell.AwardWriterPicker is null && shell.PendingAward!.Assignee is null,
            "A card buy has no writer picker and consumes no teammate's hours.");
        PickAwardTarget(shell, "tutForklift"); Match();
        Require(shell.PendingAward!.Assignee == "avery", "Returning to a request restores its draft writer.");
        obsolete.ChoiceButtons["novak"].EmitSignal(Button.SignalName.Pressed);
        Require(shell.PendingAward.Assignee == "avery" && shell.Session.StateHash == hash, "An obsolete picker cannot change the draft or Sim state.");
        Pick(shell.InstrumentInput, "purchaseOrder"); PickPriorities(shell, "fast", "good"); Match();
        Require(shell.PendingAward.Assignee == "avery", "Another written route keeps the selected writer.");
        var buy = shell.PendingAward!;
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var expected = LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave());
        Require(expected.Apply(buy).Accepted && !shell.AwardButton.Disabled, "The independent Sim accepts Avery's selected buy.");
        var submit = shell.AwardButton; submit.EmitSignal(Button.SignalName.Pressed); submit.EmitSignal(Button.SignalName.Pressed);
        Require(shell.Session.StateHash == expected.StateHash()
            && shell.Session.View!.Team.Single(m => m.Id == "avery").Work.Any(w => w.WorkId == expected.Work.Single(w => w.SubjectId == buy.Id).Id),
            "Submission assigns the real work to Avery exactly once.");
        Require(LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave()).StateHash() == expected.StateHash(), "The chosen writer survives save/resume.");
        Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VendorInput, "harborEquipment"); Pick(shell.VehicleInput, "equipBpa");
        Require(shell.PendingAward!.Assignee == "you" && shell.AwardWriterPicker!.Selected == "you", "The next new buy defaults to you again.");
        Step(shell); expected.AdvanceDay();
        Require(shell.Session.StateHash == expected.StateHash(), "The actual day loop uses the chosen writer's pace.");
        // The fifth play's exact day-five morning: the operators are known but not added yet.
        var play = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-21-5", "input-log.json"));
        var morning = LinesOfSupport.Sim.Replay.Run(bundle, play, stopAfterDay: 4);
        var save = shell.AutosavePathOverride + ".day5";
        File.WriteAllText(save, morning.ToSave().ToJson()); shell.LoadSavedGame(save); File.Delete(save);
        shell.DeveloperToggle.ButtonPressed = false;
        PickAwardTarget(shell, "tutForklift"); shell.GoToMarket("tutForklift");
        Pick(shell.InstrumentInput, "gpc"); Pick(shell.VendorInput, "coastalRentals"); await Frames();
        var add = shell.AwardAddButtons["operator"]; var pane5 = shell.RequestBuyColumn.GetGlobalRect();
        Require(shell.AwardWriterPicker is null && !shell.AwardScroll.IsAncestorOf(add) && pane5.Encloses(add.GetGlobalRect())
            && pane5.Encloses(shell.AwardSummaryLabel.GetGlobalRect()) && pane5.Encloses(shell.AwardButton.GetGlobalRect()),
            "The card buy pins its learned fix beside the complete summary and action, without a writer.");
        Require(!PlayScreen.Descendants(shell.AwardScroll).OfType<Button>().Any(b => b.Text == "Add what you learned")
            && !PlayScreen.Descendants(shell.AwardScroll).OfType<AssigneePicker>().Any(), "Neither footer control repeats in the quote.");
        var addPosition = add.GetGlobalRect(); shell.AwardScroll.ScrollVertical = 100000; await Frames();
        Require(add.GetGlobalRect() == addPosition && shell.AwardScroll.GetGlobalRect().End.Y <= add.GetGlobalRect().Position.Y,
            "The learned fix stays pinned without covering the quote.");
        var args5 = OS.GetCmdlineUserArgs(); var at5 = Array.IndexOf(args5, "--writer-screenshots");
        if (at5 >= 0)
        {
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args5[at5 + 1] + "-day5-learned.png") == Error.Ok, "The day-five missing-operators footer is captured.");
        }
        var exact = LinesOfSupport.Sim.Replay.Resume(bundle, shell.Session.CreateSave());
        Require(exact.Apply(new AddLineAction { Day = 5, Card = "tutForklift", Line = "operator" }).Accepted, "The exact learned part is addable.");
        add.EmitSignal(Button.SignalName.Pressed);
        Require(shell.Session.StateHash == exact.StateHash() && !shell.AwardAddButtons.ContainsKey("operator")
            && shell.PendingAward is { Instrument: "gpc", Vendor: "coastalRentals" }, "The pinned fix adds the exact part once and retains the selected buy.");
        add.EmitSignal(Button.SignalName.Pressed); Require(shell.Session.StateHash == exact.StateHash(), "The stale learned fix cannot act twice.");
        shell.QueueFree(); await Frames();
        GD.Print("AWARD_WRITER_SMOKE_PASS: day-four exact face/load/pace forecasts, pure writer choice, quote and summary refresh, draft retention, card exclusion, stale controls, exact submission, save/resume and next-buy default.");

        void Match()
        {
            var view = shell.Session.View!; var action = shell.PendingAward!; var picker = shell.AwardWriterPicker!;
            var choices = view.AssigneeChoices(action);
            Require(picker.Choices.SequenceEqual(choices) && picker.ChoiceButtons.Count == view.Team.Count
                && choices.All(c => picker.ChoiceButtons[c.MemberId].TooltipText == c.Sentence && picker.ChoiceButtons[c.MemberId].Icon is not null
                    && picker.ChoiceButtons[c.MemberId].Text.Contains(c.Name)
                    && picker.ChoiceButtons[c.MemberId].Text.Contains($"{c.FreeHoursToday} h free")
                    && picker.ChoiceButtons[c.MemberId].Text.Contains(c.DoneDayMin == c.DoneDayMax ? $"done day {c.DoneDayMin}" : $"done day {c.DoneDayMin}–{c.DoneDayMax}")),
                "Each compact row binds the face, name, free hours and writer done days; the full Sim sentence is its tooltip.");
            var preview = view.Preview(action);
            Require(shell.AwardPreview == preview || (shell.AwardPreview!.ShopHoursMin == preview.ShopHoursMin
                && shell.AwardPreview.ShopHoursMax == preview.ShopHoursMax && shell.AwardPreview.EarliestStartMin == preview.EarliestStartMin
                && shell.AwardPreview.EarliestStartMax == preview.EarliestStartMax && shell.AwardPreview.QueueAheadHours == preview.QueueAheadHours),
                "The quote binds the selected writer's exact hours, queue and start range.");
            Require(shell.AwardSummaryLabel.Text == view.AwardSummary(action)!.Sentence && shell.Session.StateHash == hash,
                "The summary is Sim's exact sentence and all draft changes are read only.");
        }
        async Task Frames() { for (var i = 0; i < 8; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Capture(string suffix)
        {
            await Frames();
            var picker = shell.AwardWriterPicker!; var scroll = shell.AwardScroll;
            await Frames();
            Require(!scroll.IsAncestorOf(picker) && shell.RequestBuyColumn.GetGlobalRect().Encloses(picker.GetGlobalRect())
                && PlayScreen.Descendants(shell.RequestWorkspace).OfType<ScrollContainer>().Count(s => s.IsVisibleInTree()) == 2,
                "All three writer choices stay pinned outside the one buy scroll; no new scroll is added.");
            foreach (var button in picker.ChoiceButtons.Values)
                Require(button.Size.Y < 60 && button.AutowrapMode == TextServer.AutowrapMode.Off && button.ButtonPressed == button.Text.StartsWith("✓ "),
                    "Each writer is one compact line and selection has a visible check mark.");
            var before = picker.GetGlobalRect(); scroll.ScrollVertical = 100000; await Frames();
            Require(picker.GetGlobalRect() == before && scroll.GetGlobalRect().End.Y <= picker.GetGlobalRect().Position.Y,
                "Scrolling the quote leaves the writer pinned above the summary and clear of the quote.");
            var pane = shell.RequestBuyColumn.GetGlobalRect();
            Require(pane.Encloses(shell.AwardSummaryLabel.GetGlobalRect()) && pane.Encloses(shell.AwardButton.GetGlobalRect()),
                "The whole summary and Arrange support remain pinned inside the buy pane.");
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--writer-screenshots"); if (at < 0) return;
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Captured the actual day-four writer choice.");
        }
    }
}
