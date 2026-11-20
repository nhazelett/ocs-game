using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

// DESK-125: reproduce the verification player's actual Payments and Inbox observations.
internal static class PaymentReportsSmokeChecks
{
    internal static async Task Run(Node scene)
    {
        var shell = Shell(scene);
        var input = InputLog.Load(Path.Combine(Path.GetDirectoryName(GamePaths.ScenarioRoot)!, "docs", "reviews", "plays", "2026-11-20-2", "input-log-play1.json"));
        var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
        var path = Path.Combine(Path.GetTempPath(), "ocs-desk125-" + Guid.NewGuid() + ".json");
        try
        {
            await Load(2);
            var water = shell.Session.View!.Arrangements.Single(a => a.VendorId == "islandMart");
            Require(water.Status == ArrangementStatus.Pending && water.StartDay == 2 && PaymentRow(water.Id).Text.Contains("Starts today"),
                "Day-two Payments says the pending Island Mart water starts today.");
            var future = shell.Session.View.Arrangements.First(a => a.Status == ArrangementStatus.Pending && a.StartDay > 2);
            Require(PaymentRow(future.Id).Text.Contains($"Starts on day {future.StartDay}"), "A future arrangement names its actual Sim start day.");
            await Capture("water", PaymentRow(water.Id));
            var hash = shell.Session.StateHash;
            PaymentRow(water.Id).EmitSignal(Button.SignalName.Pressed);
            Require(PlayScreen.Descendants(shell.ContractView).OfType<Label>().Any(l => l.Text.StartsWith("Starts today. Supplies"))
                && shell.Session.StateHash == hash, "The contract page agrees with Payments and opening it never acts.");
            await Load(9);
            var nation = shell.Session.View!.Arrangements.Single(a => a.Instrument == "hostNation" && a.CapabilityId == "water");
            Require(nation.StartDay == 9 && PaymentRow(nation.Id).Text.Contains("Starts today"), "Day-nine host nation water also says Starts today.");
            await Capture("host-nation", PaymentRow(nation.Id));
            await Load(12);
            Require(shell.Session.View!.PerformanceIssues.Count == 0, "The recorded play has reports but no formal issue yet on day twelve.");
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.InboxTab;
            var reports = shell.Session.View.Records.Where(shell.Session.View.Known).Where(r => r.Kind == RecordKind.CorReport).OrderByDescending(r => r.Day).ToArray();
            Require(reports.Length >= 2 && reports.Any(r => r.Text.Contains("SSgt Lane") && r.Text.Contains("day 11")), "The fixture includes Lane's missed-delivery reports.");
            var labels = shell.PerformanceRows.GetChildren().OfType<Label>().ToArray();
            var shown = labels.Where(l => l.HasMeta("performance_report")).ToArray();
            Require(shown.Select(l => l.GetMeta("performance_report").AsString()).SequenceEqual(reports.Select(r => r.Id))
                && shown.Select(l => l.Text).SequenceEqual(reports.Select(r => $"Day {r.Day} · {r.Text}"))
                && !labels.Any(l => l.Text == "No performance issues reported."),
                "Inbox lists all and only known COR reports in their exact words, newest first, without the false empty-issue message.");
            hash = shell.Session.StateHash;
            await Capture("reports", shell.PerformanceRows);
            Require(shell.Session.StateHash == hash, "Reading and scrolling the reports is pure.");
            Step(shell);
            Require(shell.Session.View!.PerformanceIssues.Count > 0 && shell.PerformanceChoices.Count > 0,
                "The formal issue and its existing decisions still appear when the Sim opens it the next morning.");
            var latest = shell.Session.View.Records.Last(r => r.Kind == RecordKind.CorReport && r.Day == 12 && r.Text.Contains("SSgt Lane"));
            Require(shell.GetNode<RichTextLabel>("%InboxRecords").Text.Contains(latest.Text),
                "The Inbox retains Lane's exact arriving day-twelve report when the formal issue opens.");
            shell.QueueFree(); await Frames();
            GD.Print("PAYMENT_REPORTS_SMOKE_PASS: recorded day-two/day-nine starts, future start, contract consistency, day-twelve exact known COR reports without a false empty state, pure navigation and next-day issue decisions.");
        }
        finally { File.Delete(path); }

        Button PaymentRow(string id) => shell.GetNode<VBoxContainer>("%DeskTabs/Payments/LedgerContext/Contents").GetChildren().OfType<Button>()
            .Single(b => b.HasMeta("arrangement") && b.GetMeta("arrangement").AsString() == id);
        async Task Frames() { for (var i = 0; i < 5; i++) await scene.ToSignal(scene.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Load(int day)
        {
            var saved = input with { SavedDay = day, Actions = input.Actions.Where(a => a.Day < day).ToList() };
            File.WriteAllText(path, saved.ToJson()); shell.LoadSavedGame(path); shell.DeveloperToggle.ButtonPressed = false;
            shell.GetNode<TabContainer>("%DeskTabs").CurrentTab = GameShell.PaymentsTab; await Frames();
            Require(shell.Session.StateHash == LinesOfSupport.Sim.Replay.Resume(bundle, saved).StateHash(), "The exact recorded play is loaded without extra actions.");
        }
        async Task Capture(string suffix, Control focus)
        {
            var scroll = shell.GetNode<ScrollContainer>(suffix == "reports" ? "%DeskTabs/Inbox/Contents" : "%DeskTabs/Payments/LedgerContext");
            await Frames(); scroll.EnsureControlVisible(focus); await Frames();
            Require(scroll.GetGlobalRect().Encloses(focus.GetGlobalRect()), "The complete start or reports fit in the existing desk scroll.");
            var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--payment-report-screenshots"); if (at < 0) return;
            await scene.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = scene.GetViewport().GetTexture().GetImage();
            Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Native start/report screenshot saved.");
        }
    }
}
