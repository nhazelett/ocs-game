using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class ScreenPolishSmokeTest : Node
{
    public override async void _Ready()
    {
        var save = Path.Combine(Path.GetTempPath(), "ocs-screen-polish-" + Guid.NewGuid() + ".json");
        try
        {
            using var packed = GD.Load<PackedScene>("res://scenes/Main.tscn");
            var shell = packed.Instantiate<GameShell>(); shell.AutosavePathOverride = save + ".autosave"; AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.TutorialGameButton.EmitSignal(Button.SignalName.Pressed);
            var screen = new PlayScreen(shell);
            await Frames();
            Require(!screen.Read().Any(e => e.Kind == "button" && string.IsNullOrWhiteSpace(e.Text)),
                "The partly clipped fourth preparation choice is never exposed as a nameless button.");
            await Capture("prep");
            var fourth = shell.PrepChoices.Values.ElementAt(3);
            await Frames(); shell.PrepScroll.EnsureControlVisible(fourth); await Frames();
            var entry = screen.Read().Single(e => e.Control == fourth);
            Require(entry.Text == fourth.Text, $"The fourth preparation checkbox has its visible name: '{entry.Text}', rect {entry.Rect}, full {fourth.GetGlobalRect()}.");
            shell.StartGameButton.EmitSignal(Button.SignalName.Pressed); FinishOpening(shell);
            var input = InputLog.Load(Path.Combine(GamePaths.ScenarioRoot, "..", "docs", "reviews", "plays", "2026-11-20-3", "input-log.json"));
            var bundle = ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "tutorial"));
            var tabs = shell.GetNode<TabContainer>("%DeskTabs");
            void Load(int day)
            {
                var expected = LinesOfSupport.Sim.Replay.Run(bundle, input, stopAfterDay: day - 1);
                File.WriteAllText(save, expected.ToSave().ToJson()); shell.LoadSavedGame(save); shell.DeveloperToggle.ButtonPressed = false;
                Require(shell.Session.StateHash == expected.StateHash(), "The exact authored morning loads through the save boundary.");
            }
            Load(13); var hash = shell.Session.StateHash;
            var issue = shell.Session.View!.PerformanceIssues.Single(i => i.CureNoticeDay is null);
            var inbox = shell.GetNode<ScrollContainer>("%DeskTabs/Inbox/Contents");
            tabs.CurrentTab = GameShell.InboxTab; await Frames(); inbox.ScrollVertical = 100000;
            tabs.CurrentTab = GameShell.TodayTab; shell.TodayPerformanceLinks[issue.Id].EmitSignal(Button.SignalName.Pressed); await Frames();
            var cure = shell.PerformanceChoices[(issue.Id, "cure")];
            Require(inbox.GetGlobalRect().Encloses(cure.GetParent().GetChild<Control>(0).GetGlobalRect()), "Open vendor decision starts at the issue's heading even after an old Inbox scroll.");
            Require(shell.Session.StateHash == hash, "Opening the decision is pure navigation.");
            var picker = cure.GetParent().GetChildren().OfType<AssigneePicker>().Single();
            picker.ChoiceButtons["avery"].EmitSignal(Button.SignalName.Pressed); cure.EmitSignal(Button.SignalName.Pressed);
            var working = shell.PerformanceChoices[(issue.Id, "cure")];
            var reason = working.GetParent().GetChildren().OfType<Label>().First(l => l.GetIndex() > working.GetIndex());
            Require(working.Disabled && reason.Visible && reason.GetThemeColor("font_color").IsEqualApprox(TabletopTheme.Ink), "In-work refusal uses ordinary ink.");
            Require(!working.GetParent().GetChildren().OfType<AssigneePicker>().Any(p => p.IsVisibleInTree()), "Work already assigned offers no Who does it picker.");
            await Frames(); inbox.EnsureControlVisible(reason); await Capture("cure-in-work");
            shell.OpenContractView(issue.ArrangementId);
            var contractCure = shell.ContractPerformanceChoices[(issue.Id, "cure")];
            Require(contractCure.Disabled && !contractCure.GetParent().GetChildren().OfType<AssigneePicker>().Last(p => p.GetIndex() < contractCure.GetIndex()).IsVisibleInTree(), "The contract's in-work action follows the same rule.");
            shell.CloseContractView();

            Load(9); hash = shell.Session.StateHash; tabs.CurrentTab = GameShell.TodayTab;
            var board = shell.Session.View!.MorningBoard();
            var duplicates = board.New.Where(n => board.Items.Any(i => i.RecordId == n.RecordId && i.Severity == BoardSeverity.Alert)).ToArray();
            Require(duplicates.Length > 0, "Day nine exercises an alert also supplied in New since yesterday.");
            foreach (var duplicate in duplicates)
                Require(!shell.MorningBrief.GetChildren().OfType<PanelContainer>().Any(p => p.GetMeta("news_record").AsString() == duplicate.RecordId), "An already-present morning alert does not repeat as news.");
            Require(shell.MorningAlerts.GetChildCount() > 0 && shell.Session.StateHash == hash, "The alert remains readable without changing state.");
            await Frames(); shell.GetNode<ScrollContainer>("%DeskTabs/Today/TodayPane/Body/Body").ScrollVertical += 180; await Capture("day9-today");

            Load(4); hash = shell.Session.StateHash; PickAwardTarget(shell, "tutForklift"); shell.ShowRequests(); shell.GoToMarket("tutForklift");
            Pick(shell.InstrumentInput, "bpaCall");
            Require(shell.PendingAward is { Vehicle: "equipBpa", Vendor: "harborEquipment" } && shell.AwardPreview!.Accepted,
                "A call with the fitting vehicle starts on that vehicle's supplier and quotes the actual terms.");
            Require(shell.Session.StateHash == hash, "Defaulting the vehicle supplier is a pure preview.");
            await Capture("day4-bpa");
            Pick(shell.InstrumentInput, "purchaseOrder"); Pick(shell.VendorInput, "coastalRentals"); PickPriorities(shell, "fast", "good");
            await Frames(); shell.AwardScroll.EnsureControlVisible(shell.PriorityInputs["good"]); await Frames();
            foreach (var toggle in shell.PriorityInputs.Values)
            {
                var label = screen.Read().Single(e => e.Control == toggle);
                Require(label.Text == toggle.Text, $"Every pick-two checkbox keeps its visible name after two selections: '{label.Text}', rect {label.Rect}, full {toggle.GetGlobalRect()}.");
            }
            Require(!screen.Read().Any(e => e.Kind == "button" && string.IsNullOrWhiteSpace(e.Text)), "The driver never presents a nameless button.");
            shell.QueueFree(); await Frames();
            GD.Print("SCREEN_POLISH_SMOKE_PASS: in-work ink and no picker, decision context at top, one alert, vehicle supplier, named preparation and pick-two controls, exact saved mornings and pure navigation."); GetTree().Quit();
            async Task Frames() { for (var i = 0; i < 8; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            async Task Capture(string suffix)
            {
                await Frames(); var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, "--screenshots"); if (at < 0) return;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage(); Require(image.SavePng(args[at + 1] + "-" + suffix + ".png") == Error.Ok, "Review screenshot saved.");
            }
        }
        catch (Exception e) { GD.PushError("SCREEN_POLISH_SMOKE_FAIL: " + e); GetTree().Quit(1); }
        finally { File.Delete(save); }
    }
}
