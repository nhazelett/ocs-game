using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;
using System.Globalization;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer debriefPanel = null!;
    private string? debriefHash;
    internal DebriefView? DisplayedDebrief { get; private set; }
    internal VBoxContainer DebriefPanel => debriefPanel;
    internal VBoxContainer DebriefFullRecord { get; private set; } = null!;
    internal Button DebriefRecordButton { get; private set; } = null!;
    internal Label DebriefSummaryText { get; private set; } = null!;
    internal Label DebriefSpeaker { get; private set; } = null!;

    private void BuildDebriefDesk()
    {
        var tabs = GetNode<TabContainer>("%DeskTabs");
        debriefPanel = DeskUi.Panel(tabs.GetNode<Control>("Debrief"), "DebriefSummary");
        debriefPanel.AddThemeConstantOverride("separation", 18);
        tabs.TabChanged += _ => RefreshDebriefLayout();
    }

    private void RefreshDebriefLayout()
    {
        var tabs = GetNode<TabContainer>("%DeskTabs");
        var reading = session.View?.Finished == true && tabs.CurrentTab == DebriefTab;
        GetNode<Control>("Margin/Layout/WorkArea/BoardPanel").Visible = !reading;
        GetNode<Control>("Margin/Layout/TopStrip").Visible = !reading;
        tabs.TabsVisible = !reading;
    }

    private void RefreshDebrief(SessionView? view)
    {
        var tabs = GetNode<TabContainer>("%DeskTabs");
        // Completing a day still owes playback and its results before the final verdict.
        var ready = view?.Finished == true && !pendingDaySummary;
        if (!ready && tabs.CurrentTab == DebriefTab) tabs.CurrentTab = TodayTab;
        tabs.SetTabHidden(DebriefTab, !ready);
        RefreshDebriefLayout();
        if (!ready)
        {
            debriefHash = null; DisplayedDebrief = null; DebriefBar = null;
            DeskUi.Clear(debriefPanel);
            debriefPanel.AddChild(DeskUi.Label("Debrief becomes available after the final day."));
            return;
        }
        if (debriefHash == session.StateHash) return;
        debriefHash = session.StateHash;
        var report = DisplayedDebrief = view!.Debrief();
        DeskUi.Clear(debriefPanel);
        var navigation = new HBoxContainer(); debriefPanel.AddChild(navigation);
        navigation.AddChild(DeskUi.Button("Back to the map", () => GetNode<TabContainer>("%DeskTabs").CurrentTab = TodayTab));
        var folder = scenarios[GetNode<OptionButton>("%ScenarioPicker").Selected].Name;
        var hash = session.StateHash;
        DebriefPlayAgain = DeskUi.Button("Play again", () => { if (hash == session.StateHash) BeginNewGame(folder); });
        DebriefChooseScenario = DeskUi.Button("Choose a scenario", () => { if (hash == session.StateHash) { ShowFrontDoor(); ShowNewGames(); } });
        navigation.AddChild(DebriefPlayAgain); navigation.AddChild(DebriefChooseScenario);
        DebriefBar = new VBoxContainer { Name = "Verdict" }; DebriefBar.AddThemeConstantOverride("separation", 10);
        debriefPanel.AddChild(DebriefBar); RenderBar(DebriefBar, report.Bar, carried: report.Carried);
        var sender = new HBoxContainer(); sender.AddThemeConstantOverride("separation", 20); debriefPanel.AddChild(sender);
        if (report.Summary?.From is { } person)
        {
            var portrait=new PersonPortrait{SizeFlagsVertical=SizeFlags.ShrinkBegin};portrait.SetPerson(person);sender.AddChild(portrait);
        }
        DebriefSpeaker = DeskUi.Label(report.Summary?.From?.WithUnit ?? "After the deployment");
        DebriefSpeaker.AddThemeFontSizeOverride("font_size", 22);
        DebriefSpeaker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        DebriefSpeaker.SizeFlagsVertical = SizeFlags.ShrinkCenter; sender.AddChild(DebriefSpeaker);
        DebriefSummaryText = DeskUi.Label(report.Summary?.Text ?? "The deployment is complete. Its record is below.");
        DebriefSummaryText.AddThemeFontSizeOverride("font_size", 23); debriefPanel.AddChild(DebriefSummaryText);
        // LeadGap is the Sim's choice of lesson, never a second ranking in Game.
        foreach (var gap in report.Gaps.OrderBy(g => g.Index == report.Summary?.LeadGap ? 0 : 1).ThenBy(g => g.Index))
            AddDebriefGap(debriefPanel, view, report, gap, gap.Index == report.Summary?.LeadGap);
        DebriefFullRecord = new VBoxContainer { Name = "FullRecord", Visible = false };
        DebriefFullRecord.AddThemeConstantOverride("separation", 18);
        DebriefRecordButton = DeskUi.Button("The full record", () => DebriefFullRecord.Visible = !DebriefFullRecord.Visible);
        DebriefRecordButton.Text = $"The full record · {report.DaysPlayed} days";
        debriefPanel.AddChild(DebriefRecordButton); debriefPanel.AddChild(DebriefFullRecord);
        BuildDebriefRecord(DebriefFullRecord, view, report);
        GetNode<TabContainer>("%DeskTabs").CurrentTab = DebriefTab;
        RefreshDebriefLayout();
        ((ScrollContainer)debriefPanel.GetParent()).ScrollVertical = 0;
    }

    private static string GapName(SessionView view, GapView gap) => $"{gap.Label} at {view.NameOf(gap.SiteId)}, {DeskUi.DaySpan(gap.StartDay, gap.EndDay)}";

    private static void AddDebriefGap(VBoxContainer parent, SessionView view, DebriefView report, GapView gap, bool open)
    {
        var detail = new VBoxContainer { Visible = open };
        detail.AddThemeConstantOverride("separation", 12); detail.SetMeta("gap_detail", gap.Index);
        var button = DeskUi.Button(GapName(view, gap) + "\n" + (gap.TagLine ?? DebriefWords.TagMeaning(gap.Tag)), () => detail.Visible = !detail.Visible);
        button.SetMeta("gap", gap.Index); parent.AddChild(button); parent.AddChild(detail);
        detail.AddChild(DeskUi.Label($"At its lowest, {gap.LowestPermille / 10m:0.#}% of the need was met. " +
            (gap.DownDays == 0 ? "It never went completely down." : $"It was completely down for {DebriefWords.Number(gap.DownDays)} {(gap.DownDays == 1 ? "day" : "days")}.")));
        for (var factorIndex = 0; factorIndex < gap.Factors.Count; factorIndex++)
        {
            var factor = gap.Factors[factorIndex];
            var quoted = report.Summary is { } summary && gap.Index == summary.LeadGap && summary.Quoted.Contains(factorIndex);
            var dependency = factor.DependsOnGap is { } index && report.Gaps.FirstOrDefault(g => g.Index == index) is { } other
                ? $"\nWent down with {other.Label.ToLowerInvariant()} at {view.NameOf(other.SiteId)}, {DeskUi.DaySpan(other.StartDay, other.EndDay)}." : "";
            var label = DeskUi.Label($"{DebriefWords.Factor(factor.Kind)}." +
                (quoted ? "" : $"\n{factor.Text}" + dependency));
            label.SetMeta("factor_index", factorIndex); detail.AddChild(label);
        }
        var trace = new VBoxContainer { Visible = false };
        var traceButton = DeskUi.Button("Trace the evidence", () =>
        {
            if (trace.GetChildCount() == 0)
                AddCauseTrace(trace, view, gap.ShortfallRecordIds.Concat(gap.Factors.SelectMany(f => f.RecordIds)).Distinct().ToArray());
            trace.Visible = !trace.Visible;
        });
        traceButton.SetMeta("gap_trace", gap.Index); detail.AddChild(traceButton); detail.AddChild(trace);
    }

    private static void BuildDebriefRecord(VBoxContainer parent, SessionView view, DebriefView report)
    {
        var score = report.Scorecard;
        static string Fraction(double value) => value.ToString("P1", CultureInfo.InvariantCulture);
        parent.AddChild(DeskUi.Label($"{report.ScenarioTitle}, {DebriefWords.Number(report.DaysPlayed)} {(report.DaysPlayed == 1 ? "day" : "days")} played, through day {report.LastDay}."));
        var inherited = string.IsNullOrEmpty(report.Inherited) ? "" : report.Inherited + "\n";
        var scoreText = DeskUi.Label($"Mission output {Fraction(score.MissionOutput)} · sortie generation {Fraction(score.SortieGeneration)}\nThroughput {Fraction(score.Throughput)} · endurance {Fraction(score.Endurance)} · resilience {Fraction(score.Resilience)}\nObligated {DeskUi.Money(score.ObligatedCents)} · incurred {DeskUi.Money(score.IncurredCents)} · unobligated incurred {DeskUi.Money(score.UnobligatedIncurredCents)}\n{inherited}Unauthorized commitments {score.UnauthorizedCommitments} · overdue UCAs {score.UcaOverdue} · HHQ inquiries {score.HhqInquiries}\nSurveys {score.Surveys} · shop hours worked {score.ShopHoursWorked}\n{score.GapCount} gaps: {score.AvoidableGaps} avoidable · {score.DiscoverableGaps} discoverable · {score.UnforeseeableGaps} unforeseeable");
        scoreText.SetMeta("scorecard", true); parent.AddChild(scoreText);
        if (report.Cures.Count > 0)
        {
            parent.AddChild(DeskUi.Label("Still open at the end"));
            foreach (var cure in report.Cures)
            {
                var sentence = DeskUi.Label(cure.Text); sentence.SetMeta("running_cure", cure.IssueId);
                parent.AddChild(sentence);
            }
        }
        parent.AddChild(DeskUi.Label("Support during each day; — means the site was not open."));
        foreach (var band in report.Timeline)
        {
            parent.AddChild(DeskUi.Label($"{view.NameOf(band.SiteId)} · {band.Label}"));
            parent.AddChild(DeskUi.Label(string.Join(" · ", band.PermilleByDay.Select((value, index) =>
                $"Day {index + 1}: " + (value < 0 ? "—" : $"{value / 10m:0.#}%")))));
        }
        parent.AddChild(DeskUi.Label("What you saw each morning"));
        var dayPicker = new OptionButton(); dayPicker.AddItem("All mornings");
        for (var day = 1; day <= report.LastDay; day++)
        { dayPicker.AddItem($"Morning of day {day}"); dayPicker.SetItemMetadata(day, day); }
        parent.AddChild(dayPicker);
        var timeline = new VBoxContainer(); timeline.AddThemeConstantOverride("separation", 12); parent.AddChild(timeline);
        var events = report.Events.ToDictionary(e => e.RecordId, StringComparer.Ordinal);
        var decisions = report.Decisions.ToDictionary(d => d.RecordId, StringComparer.Ordinal);
        var records = view.Records.Where(view.Known).Where(r => events.ContainsKey(r.Id) || decisions.ContainsKey(r.Id)).ToArray();
        void ShowDay(long selection)
        {
            DeskUi.Clear(timeline);
            var selectedDay = selection == 0 ? -1 : dayPicker.GetItemMetadata((int)selection).AsInt32();
            for (var morning = 1; morning <= report.LastDay; morning++)
            {
                if (selectedDay > 0 && morning != selectedDay) continue;
                var heading = DeskUi.Label($"Morning of day {morning}"); heading.SetMeta("record_morning", morning); timeline.AddChild(heading);
                // Decisions belong to the morning they were made. Overnight events reach the following morning.
                // The final day's results remain after its last morning, rather than inventing a morning beyond the game.
                var shown = records.Where(r => decisions.ContainsKey(r.Id) ? r.Day == morning : r.Day == morning - 1).ToArray();
                foreach (var record in shown) AddRecord(record.Id, record.Text);
                if (shown.Length == 0) timeline.AddChild(DeskUi.Label("No events or decisions this morning."));
                if (morning == report.LastDay)
                {
                    var final = records.Where(r => events.ContainsKey(r.Id) && r.Day == report.LastDay).ToArray();
                    if (final.Length > 0)
                    {
                        timeline.AddChild(DeskUi.Label($"After day {report.LastDay}"));
                        foreach (var record in final) AddRecord(record.Id, record.Text);
                    }
                }
            }
        }
        void AddRecord(string id, string text)
        {
            var label = DeskUi.Label(text);
            if (events.TryGetValue(id, out var item))
            { label.TooltipText = DebriefWords.TimelineKind(item.Kind); label.SetMeta("event_record", id); }
            else if (decisions.TryGetValue(id, out var decision))
            { label.TooltipText = decision.Rejected ? "Rejected decision" : "Decision"; label.SetMeta("decision_record", id); }
            timeline.AddChild(label);
        }
        dayPicker.ItemSelected += ShowDay; ShowDay(0);
        foreach (var commitment in report.Commitments)
            parent.AddChild(DeskUi.Label($"{view.NameOf(commitment.Id)}, opened day {commitment.OpenedDay}.\n{commitment.Text}\nDecision: {commitment.Decision}, {(commitment.DecidedDay is { } day ? $"on day {day}" : "not yet decided")}. " +
                (commitment.WalkedOff ? "The vendor walked off. " : "") + (commitment.InquiryDrawn ? "It drew a higher headquarters inquiry." : "")));
        if (report.Commitments.Count == 0) parent.AddChild(DeskUi.Label("No unauthorized commitments."));
    }

    private static void AddCauseTrace(VBoxContainer parent, SessionView view, IReadOnlyList<string> roots)
    {
        var records = view.Records.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string id, int depth)
        {
            if (!seen.Add(id)) return;
            if (!records.TryGetValue(id, out var record)) { parent.AddChild(DeskUi.Label("An earlier cause was not reported to the shop.")); return; }
            var label = DeskUi.Label(view.Known(record)
                ? $"{(depth == 0 ? "Evidence" : "Because")}, day {record.Day}\n{record.Text}"
                : "An earlier cause was not reported to the shop.");
            label.SetMeta("cause_record", id); label.SetMeta("cause_depth", depth);
            var indent = new MarginContainer(); indent.AddThemeConstantOverride("margin_left", Math.Min(depth, 8) * 12);
            indent.AddChild(label); parent.AddChild(indent);
            foreach (var cause in record.CausedBy) Visit(cause, depth + 1);
        }
        foreach (var id in roots) Visit(id, 0);
    }
}
