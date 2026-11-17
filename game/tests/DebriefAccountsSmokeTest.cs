using Godot;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DebriefAccountsSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-debrief-accounts-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temporary);
            shell = Shell(this); shell.DeveloperToggle.ButtonPressed = false;
            foreach (var name in new[] { "tutorial", "spoke-b" })
            {
                var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, name));
                var expected = LinesOfSupport.Sim.Replay.Run(bundle, 1, PolicyCatalog.Create(name == "tutorial" ? "textbook" : "do-nothing"));
                var path = Path.Combine(temporary, name + ".json");
                File.WriteAllText(path, expected.ToSave().ToJson()); shell.LoadSavedGame(path);
                Require(shell.Session.View!.Finished && shell.DisplayedDebrief is not null && shell.Session.StateHash == expected.StateHash(), "The real saved deployment opens its exact completed debrief.");
                shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed); await Frames();
                var report = expected.View.Debrief();
                var labels = PlayScreen.Descendants(shell.DebriefFullRecord).OfType<Label>().ToArray();
                var score = labels.Single(l => l.HasMeta("scorecard"));
                var cures = labels.Where(l => l.HasMeta("running_cure")).ToArray();
                Require(cures.Select(l => l.Text).SequenceEqual(report.Cures.Select(c => c.Text)), "Every running cure is shown verbatim in Sim order, without list marks.");
                if (name == "tutorial")
                {
                    Require(report.Inherited is not null && report.Cures.Count > 0, "The real textbook tutorial has both inherited obligations and a running cure.");
                    var lines = score.Text.Split('\n');
                    Require(Array.IndexOf(lines, report.Inherited) == Array.FindIndex(lines, l => l.StartsWith("Obligated ")) + 1, "The exact inherited sentence sits directly under the obligation total.");
                    Require(cures.All(l => l.GetThemeFontSize("font_size") == score.GetThemeFontSize("font_size")), "Cure sentences use the debrief's normal text size.");
                }
                else
                    Require(report.Inherited is null && report.Cures.Count == 0 && cures.Length == 0
                        && !score.Text.Contains("last rotation") && !labels.Any(l => l.Text == "Still open at the end"), "Spoke B invents no inherited sentence, cure or empty section.");
                var scroll = (ScrollContainer)shell.DebriefPanel.GetParent();
                scroll.EnsureControlVisible(cures.LastOrDefault() ?? score); await Frames();
                Require(GetViewport().GetVisibleRect().Encloses(score.GetGlobalRect()) && cures.All(l => GetViewport().GetVisibleRect().Encloses(l.GetGlobalRect())), "The full totals and running cure can be read together in the debrief.");
                var hash = shell.Session.StateHash;
                shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed); shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed);
                Require(shell.Session.StateHash == hash, "Reading and toggling the full record is pure.");
                var args = OS.GetCmdlineUserArgs(); var index = Array.IndexOf(args, "--screenshots");
                if (index >= 0)
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    using var image = GetViewport().GetTexture().GetImage();
                    Require(image.SavePng(args[index + 1] + "-" + name + ".png") == Error.Ok, "The actual finished debrief is captured.");
                }
            }
            shell.QueueFree(); await Frames(); shell = null;
            GD.Print("DEBRIEF_ACCOUNTS_SMOKE_PASS: actual completed tutorial and Spoke B saves, exact inherited and running-cure text, correct placement and type, no empty sections, pure record navigation."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("DEBRIEF_ACCOUNTS_SMOKE_FAIL: " + error); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
        async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    }
}
