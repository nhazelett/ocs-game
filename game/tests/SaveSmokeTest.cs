using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class SaveSmokeTest : Node
{
    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        var at = Array.IndexOf(args, "--save-path");
        var temporary = at < 0;
        var path = temporary ? Path.Combine(Path.GetTempPath(), "ocs-save-" + Guid.NewGuid().ToString("N"), "autosave.json") : args[at + 1];
        try
        {
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b"));
            var log = InputLog.Load(Path.Combine(bundle.Directory, "golden", "approach1-as-written.json"));
            var expected = LinesOfSupport.Sim.Replay.Run(bundle, log, stopAfterDay: 9);
            if (!args.Contains("--resume-only"))
            {
                var writer = NewShell(path);
                Require(writer.SaveButton.Disabled, "Saving is disabled before deployment.");
                Scenario(writer, "spoke-b"); Replay(writer); Step(writer, 9);
                Require(writer.Session.StateHash == expected.StateHash(), "Writer is approach 1 on the morning of day 10.");
                writer.SaveButton.EmitSignal(Button.SignalName.Pressed);
                var saved = GameSaveStore.Read(path);
                Require(saved.SavedDay == 10 && saved.ToJson() == expected.ToSave().ToJson(), "Save writes exactly Session.ToSave with no future scripted actions.");
                Require(!writer.ContinueSaveButton.Disabled, "Saved game becomes available immediately.");
                RemoveChild(writer); writer.Free();
                GD.Print("SAVE_WRITTEN: approach 1, morning of day 10.");
                if (args.Contains("--write-only")) { GetTree().Quit(); return; }
            }
            var shell = NewShell(path);
            Require(shell.Session.View is null && !shell.ContinueSaveButton.Disabled, "Fresh shell offers Continue saved game before deployment.");
            shell.ContinueSaveButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash() && !shell.Session.IsReplay, "Restart restores the exact Sim state and gives control to the player.");
            Require(shell.GetNode<Label>("%DayCounter").Text == "Day 10 (next)", "Saved morning is shown without advancing it.");
            Require(shell.GetNode<ProgressBar>("%CashGauge").Value == expected.View.CashCents, "Restart restores cash.");
            var map = shell.GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
            var status = expected.View.MorningBoard().Tiles.Where(t => t.SiteId == "spokeBSite").MinBy(t => t.TodayPermille)!.Status;
            var condition = status switch { "ok" => SiteCondition.Full, "degraded" => SiteCondition.Degraded, "down" => SiteCondition.Down, _ => SiteCondition.NotOpen };
            Require(map.Data!.Id == "spoke-b" && map.Conditions["spokeBSite"] == condition, "Restart restores the actual Spoke B power/site condition.");
            var picker = shell.GetNode<OptionButton>("%ScenarioPicker");
            Require(picker.GetItemText(picker.Selected) == "spoke-b", "Continue selects the saved scenario.");
            Step(shell); expected.AdvanceDay();
            Require(shell.Session.StateHash == expected.StateHash() && map.Conditions["spokeBSite"] == SiteCondition.Down, "Resumed day 10 reaches the same power-down tile.");
            Require(GameSaveStore.Read(path).ToJson() == expected.ToSave().ToJson(), "Next day autosaves the new morning.");

            var survey = new SurveyAction { Day = expected.Day, Island = "spokeB" };
            Require(expected.Apply(survey).Accepted && shell.SendAction(survey).Accepted, "Same-morning decision is accepted.");
            shell.SaveButton.EmitSignal(Button.SignalName.Pressed);
            shell.ContinueSaveButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == expected.StateHash(), "Saved same-day action is restored exactly once.");
            Step(shell, 2); expected.AdvanceDay(); expected.AdvanceDay();
            Require(shell.Session.StateHash == expected.StateHash(), "A saved authored replay does not apply future scripted ratification after resume.");

            var good = GameSaveStore.Read(path);
            var badPath = Path.Combine(Path.GetDirectoryName(path)!, "bad.json");
            var badSaves = new[]
            {
                "{", "null", good.ToJson().Replace("\"actions\": [", "\"actions\": [null,"),
                (good with { Scenario = "missing-scenario" }).ToJson(),
                (good with { RulesVersion = "incompatible-rules" }).ToJson(),
                (good with { SavedDay = 0 }).ToJson(),
                (good with { Actions = [new SurveyAction { Day = 99, Island = "spokeB" }] }).ToJson(),
                "{\"scenario\":\"spoke-b\",\"rulesVersion\":\"2025-10\",\"actions\":[{\"kind\":\"unknown-action\"}]}"
            };
            foreach (var bad in badSaves)
            {
                File.WriteAllText(badPath, bad);
                shell.LoadSavedGame(badPath);
                Require(shell.GetNode<Label>("%BuildStatus").Text.Contains("Cannot load save:"), "Bad saves give a visible error.");
                Require(shell.Session.StateHash == expected.StateHash() && map.Data.Id == "spoke-b", "Refused save retains the open session and board.");
            }
            shell.LoadSavedGame(badPath + ".missing");
            Require(shell.Session.StateHash == expected.StateHash(), "Missing save also retains the open session.");

            var retained = File.ReadAllText(path);
            shell.AutosavePathOverride = Path.Combine(path, "blocked.json");
            Step(shell); expected.AdvanceDay();
            Require(shell.Session.StateHash == expected.StateHash() && shell.GetNode<Label>("%BuildStatus").Text.Contains("Save failed:"), "Unwritable autosave reports failure while leaving the game playable.");
            Require(File.ReadAllText(path) == retained, "Failed autosave preserves the previous valid save.");
            shell.AutosavePathOverride = path;
            shell.GetNode<Button>("%Continue").EmitSignal(Button.SignalName.Pressed);
            var continued = shell.Session.CreateSave();
            Require(GameSaveStore.Read(path).ToJson() == continued.ToJson() && continued.SavedDay > good.SavedDay, "Continue-until-event autosaves too.");
            Require(!shell.GetNode<Label>("%BuildStatus").Text.Contains("Save failed:"), "A successful autosave clears the error.");

            var longBundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b-90"));
            var longLog = InputLog.Load(Path.Combine(longBundle.Directory, "golden", "approach1-as-written.json"));
            var longExpected = LinesOfSupport.Sim.Replay.Run(longBundle, longLog, stopAfterDay: 10);
            var chosenPath = Path.Combine(Path.GetDirectoryName(path)!, "chosen.json");
            GameSaveStore.Write(chosenPath, longExpected.ToSave());
            shell.LoadSaveButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.SaveFilePicker.Visible, "Load opens the file chooser.");
            shell.SaveFilePicker.EmitSignal(FileDialog.SignalName.FileSelected, chosenPath);
            shell.SaveFilePicker.Hide();
            Require(shell.Session.StateHash == longExpected.StateHash() && map.Data.Id == "spoke-b-90", "Load chosen file resumes and switches the board to its scenario.");
            Require(picker.GetItemText(picker.Selected) == "spoke-b-90", "Manual load selects its own scenario.");
            Step(shell); longExpected.AdvanceDay();
            Require(shell.Session.StateHash == longExpected.StateHash(), "Manually loaded game advances from the saved day.");

            var finished = LinesOfSupport.Sim.Replay.Run(bundle, log);
            GameSaveStore.Write(chosenPath, finished.ToSave());
            shell.LoadSavedGame(chosenPath);
            Require(shell.Session.StateHash == finished.StateHash() && shell.GetNode<Button>("%NextDay").Disabled, "Finished saves restore the completed game without another day.");
            shell.SaveButton.EmitSignal(Button.SignalName.Pressed);
            Require(GameSaveStore.Read(path).SavedDay is null, "Finished game saves keep the Sim's null SavedDay convention.");
            GD.Print("SAVE_SMOKE_PASS: process restart, day/cash/power, same-day decisions, autosave, chosen file, refused saves, write failure and finished games.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError($"SAVE_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (temporary && Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path)!, recursive: true); }
    }

    private GameShell NewShell(string path)
    {
        var shell = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<GameShell>();
        shell.AutosavePathOverride = path;
        AddChild(shell);
        return shell;
    }
}
