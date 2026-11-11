using Godot;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;
using System.Text.Json.Nodes;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class DebriefSmokeTest : Node
{
    public override async void _Ready()
    {
        GameShell? shell = null;
        var temporary = Path.Combine(Path.GetTempPath(), "ocs-debrief-" + Guid.NewGuid());
        try
        {
            // The gate's approach 1 path through New game, award and ratification controls; later cards use legacy scope fixtures.
            static GameShell CreateShell() { using var scene = GD.Load<PackedScene>("res://scenes/Main.tscn"); return scene.Instantiate<GameShell>(); }
            shell = CreateShell(); shell.AutosavePathOverride = Path.Combine(temporary, "autosave.json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed);
            DeployWithoutPrep(shell);
            Require(shell.DisplayedDebrief is null, "Debrief waits for the final day.");
            Pick(shell.VendorInput, "vendorC"); Pick(shell.InstrumentInput, "bpaCall"); Pick(shell.VehicleInput, "bosBpa");
            shell.QuantityInput.Value = 360; shell.DurationInput.Value = 60;
            Require(shell.PendingAward!.StartDay == 7 && !shell.AwardButton.Disabled, "Gate path buys the generators as written, starting day seven.");
            var expected = Session.Create(ScenarioBundle.Load(Path.Combine(GamePaths.ScenarioRoot, "spoke-b")), 1);
            Require(expected.Apply(shell.PendingAward!).Accepted, "The engine accepts the exact player award."); shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            void OpenCard(string card)
            {
                shell.NavigateBoardItem(new BoardItem("", shell.Session.View!.Day, BoardCategory.Request, BoardSeverity.Info, BoardLinkKind.Card, card, "", []));
            }
            void GoToMarket(string card)
            {
                OpenCard(card);
                Buttons(shell.GetNode<TabContainer>("%DeskTabs").GetNode("Requirements/RequirementContext/Contents")).Single(b => b.HasMeta("market_card")).EmitSignal(Button.SignalName.Pressed);
            }
            void SetLine(string card, string line)
            {
                OpenCard(card); Require(expected.Apply(new SetLineAction { Day = expected.Day, Card = card, Line = line, On = true }).Accepted, "Scope line is accepted by Sim.");
                // These later-card fixtures keep the debrief's original evidence coverage. Discovery UI has its own native suite.
                Require(shell.SendAction(new SetLineAction { Day = expected.Day, Card = card, Line = line, On = true }).Accepted,
                    "The supported legacy scope fixture is accepted by the game bridge.");
            }
            void Award(string vendor, string instrument, long quantity, int start, int duration)
            {
                Pick(shell.VendorInput, vendor); Pick(shell.InstrumentInput, instrument);
                shell.QuantityInput.Value = quantity; shell.StartInput.Value = start; shell.DurationInput.Value = duration;
                Require(!shell.AwardButton.Disabled && expected.Apply(shell.PendingAward!).Accepted, "Spoke C support is accepted with its exact UI terms.");
                shell.AwardButton.EmitSignal(Button.SignalName.Pressed);
            }
            while (!shell.Session.View!.Finished)
            {
                if (expected.Day == 2)
                {
                    SetLine("req0012", "operator"); SetLine("req0012", "maintenance");
                    GoToMarket("req0012"); Award("vendorF", "uca", 2640, 5, 60);
                    PickAwardTarget(shell, "spokeCSite.water"); Award("vendorG", "purchaseOrder", 2640, 5, 12);
                }
                if (expected.Day == 3)
                {
                    SetLine("req0014", "operator"); GoToMarket("req0014"); Award("vendorF", "purchaseOrder", 480, 3, 28);
                }
                var commitment = shell.Session.View.Commitments.FirstOrDefault(c => c.Unresolved);
                if (commitment is not null)
                {
                    Require(expected.Apply(new ResolveCommitmentAction { Day = expected.Day, Commitment = commitment.Id, Choice = "ratify" }).Accepted, "The engine accepts ratification.");
                    shell.CommitmentChoices[(commitment.Id, "ratify")].EmitSignal(Button.SignalName.Pressed);
                }
                Step(shell); expected.AdvanceDay();
                Require(shell.Session.StateHash == expected.StateHash(), "Controls and authored scope fixtures reproduce the same independent Sim game each morning.");
            }
            Require(!shell.DeveloperToggle.ButtonPressed && !shell.Session.IsReplay, "The by-hand path needs neither files nor developer tools.");
            await CheckOpening(shell);
            Require(shell.DebriefSummaryText.Text.Contains("Nobody owned fuel at Spoke B"), "The after-action leads with the player's unowned fuel.");
            Require(shell.DisplayedDebrief!.Summary!.Quoted.Count > 0, "Approach 1 exercises evidence already quoted in the paragraph.");
            var lead = shell.DisplayedDebrief.Gaps.Single(g => g.Index == shell.DisplayedDebrief.Summary.LeadGap);
            Require(lead.Factors.Any(f => f.Tag == lead.Tag) && lead.Factors.Any(f => f.Tag != lead.Tag),
                "Approach 1 exercises both repeated and different lead-factor tags.");
            await Capture("--screenshot");
            CheckRecord(shell);
            var hash = shell.Session.StateHash;
            shell.GetNode<Button>("Navigation/Menu").EmitSignal(Button.SignalName.Pressed); shell.ContinueGameButton.EmitSignal(Button.SignalName.Pressed);
            Require(shell.Session.StateHash == hash && shell.DisplayedDebrief?.Summary?.Text == expected.View.Debrief().Summary?.Text, "Continue restores the completed deployment and its summary.");
            await CheckOpening(shell);
            shell.QueueFree(); shell = null; await Frames();

            // Authored replay covers many decisions in one day, cause chains and the full record.
            shell = Shell(this); Replay(shell); while (!shell.Session.View!.Finished) Step(shell);
            await CheckOpening(shell); CheckRecord(shell);
            shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed);
            await Frames(); await Capture("--record-screenshot");
            var tabs = shell.GetNode<TabContainer>("%DeskTabs"); Buttons(shell.DebriefPanel).Single(b => b.Text == "Back to the map").EmitSignal(Button.SignalName.Pressed); await Frames();
            Require(shell.GetNode<Control>("Margin/Layout/WorkArea/BoardPanel").Visible && tabs.TabsVisible
                && shell.GetNode<Control>("Margin/Layout/TopStrip").Visible, "Returning to the map restores the desk and top strip.");
            tabs.CurrentTab = 8; await Frames();
            Require(!shell.GetNode<Control>("Margin/Layout/WorkArea/BoardPanel").Visible && !tabs.TabsVisible, "Reopening Debrief uses the whole work area again.");
            Scenario(shell, "_stub");
            Require(shell.DisplayedDebrief is null && shell.GetNode<Control>("Margin/Layout/WorkArea/BoardPanel").Visible && tabs.TabsVisible, "Changing deployment clears the debrief and restores normal layout.");
            shell.QueueFree(); shell = null; await Frames();

            // A one-day cut of real Spoke B ends before the remote sites open: a genuine no-gap completion.
            var fixture = Path.Combine(temporary, "scenarios", "spoke-b"); Directory.CreateDirectory(fixture);
            Directory.CreateDirectory(Path.Combine(temporary, "scenarios", "rules"));
            File.Copy(Path.Combine(GamePaths.ScenarioRoot, "rules", "rules-2025-10.json"), Path.Combine(temporary, "scenarios", "rules", "rules-2025-10.json"));
            var json = JsonNode.Parse(File.ReadAllText(Path.Combine(GamePaths.ScenarioRoot, "spoke-b", "scenario.json")))!;
            json["days"] = 1; File.WriteAllText(Path.Combine(fixture, "scenario.json"), json.ToJsonString());
            shell = CreateShell(); shell.ScenarioRootOverride = Path.GetDirectoryName(fixture); shell.AutosavePathOverride = Path.Combine(temporary, "no-gap.json"); AddChild(shell);
            shell.NewGameButton.EmitSignal(Button.SignalName.Pressed); shell.SpokeBGameButton.EmitSignal(Button.SignalName.Pressed); DeployWithoutPrep(shell); Step(shell);
            Require(shell.DisplayedDebrief!.Gaps.Count == 0 && shell.DisplayedDebrief.Summary!.LeadGap is null, "The no-gap completion has no invented lead or evidence.");
            await CheckOpening(shell); await Capture("--no-gap-screenshot");
            Require(!Buttons(shell.DebriefPanel).Any(b => b.HasMeta("gap")), "No empty gap control appears for a no-gap run.");
            CheckRecord(shell);
            shell.QueueFree(); shell = null; await Frames();
            // The cast-free stub exercises Summary.From == null without inventing a person.
            File.Copy(Path.Combine(GamePaths.ScenarioRoot, "_stub", "scenario.json"), Path.Combine(fixture, "scenario.json"), true);
            shell = CreateShell(); shell.ScenarioRootOverride = Path.GetDirectoryName(fixture); shell.AutosavePathOverride = Path.Combine(temporary, "no-cast.json"); AddChild(shell);
            DeveloperWorkspace(shell); Scenario(shell, "spoke-b"); while (!shell.Session.View!.Finished) Step(shell);
            Require(shell.DisplayedDebrief!.Summary!.From is null && shell.DebriefSpeaker.Text == "After the deployment"
                && !shell.DebriefPanel.FindChildren("*", "PersonPortrait", true, false).Any(), "A cast-free deployment uses a neutral attribution.");
            shell.QueueFree(); shell = null; await Frames();
            GD.Print("DEBRIEF_SMOKE_PASS: by-hand approach 1, exact speaker/summary/lead, full-width layout, full record, one known snapshot per day, verbatim events, quoted evidence and lead tag once with complete traces, no-gap and cast-free completions, saved finish and read-only rendering."); GetTree().Quit();
        }
        catch (Exception error) { shell?.QueueFree(); GD.PushError($"DEBRIEF_SMOKE_FAIL: {error}"); GetTree().Quit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }

    private async Task CheckOpening(GameShell shell)
    {
        var report = shell.Session.View!.Debrief(); var hash = shell.Session.StateHash;
        Require(shell.DisplayedDebrief!.Scorecard == report.Scorecard && shell.DebriefSummaryText.Text == report.Summary!.Text, "Summary and scores bind exactly to Sim.");
        Require(shell.DebriefSpeaker.Text == (report.Summary!.From?.WithUnit ?? "After the deployment"), "Rank, name and unit come from the summary's person.");
        Require(shell.GetNode<TabContainer>("%DeskTabs").CurrentTab == 8 && !shell.DebriefFullRecord.Visible, "Final day opens the after-action with the full record closed.");
        if (report.Summary!.LeadGap is { } lead)
            Require(shell.DebriefPanel.GetChildren().OfType<VBoxContainer>().Single(v => v.HasMeta("gap_detail") && v.GetMeta("gap_detail").AsInt32() == lead).Visible, "The Sim's lead gap evidence is already open.");
        foreach (var gap in report.Gaps)
        {
            Require(Buttons(shell.DebriefPanel).Single(b => b.HasMeta("gap") && b.GetMeta("gap").AsInt32() == gap.Index).Text.Contains(DebriefWords.TagMeaning(gap.Tag)), "Gap tags use the contract's plain meaning.");
            var detail = shell.DebriefPanel.GetChildren().OfType<VBoxContainer>().Single(v => v.HasMeta("gap_detail") && v.GetMeta("gap_detail").AsInt32() == gap.Index);
            var factors = Labels(detail).Where(l => l.HasMeta("factor_index")).ToArray();
            Require(factors.Length == gap.Factors.Count, "Every factor keeps its own evidence row.");
            for (var i = 0; i < gap.Factors.Count; i++)
            {
                var factor = gap.Factors[i];
                var label = factors.Single(l => l.GetMeta("factor_index").AsInt32() == i);
                var tagLine = $"{DebriefWords.Factor(factor.Kind)}." +
                    (gap.Index == report.Summary.LeadGap && factor.Tag == gap.Tag ? "" : " " + DebriefWords.TagMeaning(factor.Tag));
                if (gap.Index == report.Summary.LeadGap && report.Summary.Quoted.Contains(i))
                {
                    Require(label.Text == tagLine && shell.DebriefSummaryText.Text.Contains(factor.Text), "Quoted lead factors keep their kind and only a different tag; their evidence stays verbatim in the paragraph.");
                    Require(!Labels(detail).Any(l => l.Text.Contains(factor.Text)), "The lead evidence does not repeat the paragraph's quoted sentence.");
                }
                else
                    Require(label.Text.StartsWith(tagLine + "\n" + factor.Text), "Unquoted factors and every other gap retain their full exact evidence.");
            }
        }
        await Frames();
        var tabs = shell.GetNode<TabContainer>("%DeskTabs"); var area = shell.GetNode<Control>("Margin/Layout/WorkArea");
        Require(!shell.GetNode<Control>("Margin/Layout/WorkArea/BoardPanel").Visible && !tabs.TabsVisible && tabs.Size.X >= area.Size.X * .95f, "The after-action fills the work area instead of sitting beside the map.");
        Require(shell.DebriefSpeaker.Size.X > tabs.Size.X * .5f && shell.DebriefSummaryText.GetGlobalRect().End.Y < shell.GetViewportRect().Size.Y, "The speaker and complete paragraph are readable in the initial viewport.");
        Require(shell.Session.StateHash == hash, "Rendering is read-only.");
    }

    private static void CheckRecord(GameShell shell)
    {
        var report = shell.DisplayedDebrief!; var view = shell.Session.View!; var hash = shell.Session.StateHash;
        Require(!shell.DebriefFullRecord.Visible, "The record begins behind one disclosure.");
        shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed); Require(shell.DebriefFullRecord.Visible, "One control opens scores, grid and timeline.");
        foreach (var trace in Buttons(shell.DebriefPanel).Where(b => b.HasMeta("gap_trace")).ToArray()) trace.EmitSignal(Button.SignalName.Pressed);
        var labels = Labels(shell.DebriefPanel).ToArray();
        foreach (var item in report.Events.Where(e => view.Records.Any(r => r.Id == e.RecordId && view.Known(r))))
        {
            var label = labels.Single(l => l.HasMeta("event_record") && l.GetMeta("event_record").AsString() == item.RecordId);
            Require(label.Text == item.Text && label.TooltipText == DebriefWords.TimelineKind(item.Kind), "Each event is shown once as its exact sentence; its tooltip uses plain kind words.");
        }
        var snapshots = labels.Where(l => l.HasMeta("known_day")).ToArray();
        Require(snapshots.Length == report.Decisions.Where(d => view.Records.Any(r => r.Id == d.RecordId && view.Known(r))).Select(d => d.Day).Distinct().Count()
            && snapshots.Select(l => l.GetMeta("known_day").AsInt32()).Distinct().Count() == snapshots.Length, "Known then appears once per decision day, including days with several decisions.");
        foreach (var id in report.Gaps.SelectMany(g => g.Factors.SelectMany(f => f.RecordIds).Concat(g.ShortfallRecordIds)).Distinct()
            .Where(id => view.Records.Any(r => r.Id == id && view.Known(r))))
            Require(labels.Any(l => l.HasMeta("cause_record") && l.GetMeta("cause_record").AsString() == id), "Every known factor and shortfall trace remains available, including factors quoted in the paragraph.");
        foreach (var commitment in report.Commitments) Require(labels.Any(l => l.Text.Contains(view.NameOf(commitment.Id)) && l.Text.Contains(commitment.Decision)), "Commitment names and decisions remain in the record.");
        Require(!Buttons(shell.DebriefPanel).Any(b => System.Text.RegularExpressions.Regex.IsMatch(b.Text, @"\bGap \d")), "No internal gap index reaches the player.");
        var picker = shell.DebriefFullRecord.FindChildren("*", "OptionButton", true, false).OfType<OptionButton>().Single();
        foreach (var day in new[] { 0, Math.Min(2, report.LastDay) })
        {
            picker.Select(day + 1); picker.EmitSignal(OptionButton.SignalName.ItemSelected, day + 1);
            var shown = Labels(shell.DebriefFullRecord).Where(l => l.HasMeta("event_record")).Select(l => l.GetMeta("event_record").AsString()).ToArray();
            var expectedEvents = report.Events.Where(e => e.Day == day && view.Records.Any(r => r.Id == e.RecordId && view.Known(r))).Select(e => e.RecordId).ToArray();
            Require(shown.Order().SequenceEqual(expectedEvents.Order()), "Day filtering includes inherited day-zero events and only that day's known entries.");
        }
        picker.Select(0); picker.EmitSignal(OptionButton.SignalName.ItemSelected, 0);
        Require(hash == shell.Session.StateHash, "Disclosures and cause traces do not change the game.");
        shell.DebriefRecordButton.EmitSignal(Button.SignalName.Pressed);
    }

    private async Task Frames() { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Capture(string flag)
    {
        var args = OS.GetCmdlineUserArgs(); var at = Array.IndexOf(args, flag); if (at < 0) return;
        await Frames(); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Require(GetViewport().GetTexture().GetImage().SavePng(args[at + 1]) == Error.Ok, "Screenshot saved.");
    }
    private static IEnumerable<Label> Labels(Node node) => node.GetChildren().SelectMany(child => (child is Label label ? new[] { label } : Enumerable.Empty<Label>()).Concat(Labels(child)));
    private static IEnumerable<Button> Buttons(Node node) => node.GetChildren().SelectMany(child => (child is Button button ? new[] { button } : Enumerable.Empty<Button>()).Concat(Buttons(child)));
}
