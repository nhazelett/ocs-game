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
        var reading = session.View?.Finished == true && tabs.CurrentTab == 8;
        GetNode<Control>("Margin/Layout/WorkArea/BoardPanel").Visible = !reading;
        GetNode<Control>("Margin/Layout/TopStrip").Visible = !reading;
        tabs.TabsVisible = !reading;
    }

    private void RefreshDebrief(SessionView? view)
    {
        RefreshDebriefLayout();
        if (view?.Finished != true)
        {
            debriefHash = null; DisplayedDebrief = null;
            DeskUi.Clear(debriefPanel);
            debriefPanel.AddChild(DeskUi.Label("Debrief becomes available after the final day."));
            return;
        }
        if (debriefHash == session.StateHash) return;
        debriefHash = session.StateHash;
        var report = DisplayedDebrief = view.Debrief();
        DeskUi.Clear(debriefPanel);
        var navigation = new HBoxContainer(); debriefPanel.AddChild(navigation);
        navigation.AddChild(DeskUi.Button("Back to the map", () => GetNode<TabContainer>("%DeskTabs").CurrentTab = 0));
        var sender = new HBoxContainer(); sender.AddThemeConstantOverride("separation", 20); debriefPanel.AddChild(sender);
        if (report.Summary?.From is { } person)
        {
            sender.AddChild(new PersonPortrait { TooltipText = person.DisplayName, SizeFlagsVertical = SizeFlags.ShrinkBegin });
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
        debriefPanel.AddChild(DebriefRecordButton); debriefPanel.AddChild(DebriefFullRecord);
        BuildDebriefRecord(DebriefFullRecord, view, report);
        GetNode<TabContainer>("%DeskTabs").CurrentTab = 8;
        RefreshDebriefLayout();
        ((ScrollContainer)debriefPanel.GetParent()).ScrollVertical = 0;
    }

    private static string GapName(SessionView view, GapView gap) => $"{gap.Label} at {view.NameOf(gap.SiteId)}, days {gap.StartDay}–{gap.EndDay}";

    private static void AddDebriefGap(VBoxContainer parent, SessionView view, DebriefView report, GapView gap, bool open)
    {
        var detail = new VBoxContainer { Visible = open };
        detail.AddThemeConstantOverride("separation", 12); detail.SetMeta("gap_detail", gap.Index);
        var button = DeskUi.Button(GapName(view, gap) + "\n" + DebriefWords.TagMeaning(gap.Tag), () => detail.Visible = !detail.Visible);
        button.SetMeta("gap", gap.Index); parent.AddChild(button); parent.AddChild(detail);
        detail.AddChild(DeskUi.Label($"At its lowest, {gap.LowestPermille / 10m:0.#}% of the need was met. " +
            (gap.DownDays == 0 ? "It never went completely down." : $"It was completely down for {DebriefWords.Number(gap.DownDays)} {(gap.DownDays == 1 ? "day" : "days")}.")));
        for (var factorIndex = 0; factorIndex < gap.Factors.Count; factorIndex++)
        {
            var factor = gap.Factors[factorIndex];
            var quoted = report.Summary is { } summary && gap.Index == summary.LeadGap && summary.Quoted.Contains(factorIndex);
            var dependency = factor.DependsOnGap is { } index && report.Gaps.FirstOrDefault(g => g.Index == index) is { } other
                ? $"\nWent down with {other.Label.ToLowerInvariant()} at {view.NameOf(other.SiteId)}, days {other.StartDay}–{other.EndDay}." : "";
            var tag = gap.Index == report.Summary?.LeadGap && factor.Tag == gap.Tag
                ? "" : " " + DebriefWords.TagMeaning(factor.Tag);
            var label = DeskUi.Label($"{DebriefWords.Factor(factor.Kind)}." + tag +
                (quoted ? "" : $"\n{factor.Text}" + dependency));
            label.SetMeta("factor_index", factorIndex); detail.AddChild(label);
        }
        var trace = new VBoxContainer { Visible = false };
        var traceButton = DeskUi.Button("Trace the evidence", () =>
        {
            if (trace.GetChildCount() == 0)
                AddCauseTrace(trace, view, gap.Factors.SelectMany(f => f.RecordIds).Concat(gap.ShortfallRecordIds).Distinct().ToArray());
            trace.Visible = !trace.Visible;
        });
        traceButton.SetMeta("gap_trace", gap.Index); detail.AddChild(traceButton); detail.AddChild(trace);
    }

    private static void BuildDebriefRecord(VBoxContainer parent, SessionView view, DebriefView report)
    {
        var score = report.Scorecard;
        static string Fraction(double value) => value.ToString("P1", CultureInfo.InvariantCulture);
        parent.AddChild(DeskUi.Label($"{report.ScenarioTitle}, {DebriefWords.Number(report.DaysPlayed)} {(report.DaysPlayed == 1 ? "day" : "days")} played, through day {report.LastDay}."));
        var scoreText = DeskUi.Label($"Mission output {Fraction(score.MissionOutput)} · sortie generation {Fraction(score.SortieGeneration)}\nThroughput {Fraction(score.Throughput)} · endurance {Fraction(score.Endurance)} · resilience {Fraction(score.Resilience)}\nObligated {DeskUi.Money(score.ObligatedCents)} · incurred {DeskUi.Money(score.IncurredCents)} · unobligated incurred {DeskUi.Money(score.UnobligatedIncurredCents)}\nUnauthorized commitments {score.UnauthorizedCommitments} · overdue UCAs {score.UcaOverdue} · HHQ inquiries {score.HhqInquiries}\nSurveys {score.Surveys} · shop hours worked {score.ShopHoursWorked}\n{score.GapCount} gaps: {score.AvoidableGaps} avoidable · {score.DiscoverableGaps} discoverable · {score.UnforeseeableGaps} unforeseeable");
        scoreText.SetMeta("scorecard", true); parent.AddChild(scoreText);
        parent.AddChild(DeskUi.Label("Capability by day. Scroll across to see later days; — means the site was not open."));
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 190), VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        var grid = new GridContainer { Columns = report.DaysPlayed + 1 };
        scroll.AddChild(grid); parent.AddChild(scroll);
        grid.AddChild(new Label { Text = "Site / capability" });
        for (var day = 1; day <= report.DaysPlayed; day++) grid.AddChild(new Label { Text = $"Day {day}", CustomMinimumSize = new Vector2(62, 0) });
        foreach (var band in report.Timeline)
        {
            grid.AddChild(new Label { Text = $"{view.NameOf(band.SiteId)} · {band.Label}" });
            foreach (var value in band.PermilleByDay)
            {
                var cell = new Label { Text = value < 0 ? "—" : $"{value / 10m:0.#}%" };
                cell.AddThemeColorOverride("font_color",value < 0 ? TabletopTheme.Muted : TabletopTheme.Alert.Lerp(TabletopTheme.Success, Math.Clamp(value / 1000f, 0, 1)));
                grid.AddChild(cell);
            }
        }
        parent.AddChild(DeskUi.Label("What happened, day by day"));
        var dayPicker = new OptionButton(); dayPicker.AddItem("All days");
        // Day zero contains the inherited arrangements and is part of the full record too.
        for (var day = 0; day <= report.LastDay; day++) { dayPicker.AddItem($"Day {day}"); dayPicker.SetItemMetadata(day + 1, day); }
        parent.AddChild(dayPicker);
        var timeline = new VBoxContainer(); timeline.AddThemeConstantOverride("separation", 12); parent.AddChild(timeline);
        var events = report.Events.ToDictionary(e => e.RecordId, StringComparer.Ordinal);
        var decisions = report.Decisions.ToDictionary(d => d.RecordId, StringComparer.Ordinal);
        var records = view.Records.Where(view.Known).Where(r => events.ContainsKey(r.Id) || decisions.ContainsKey(r.Id)).ToArray();
        void ShowDay(long selection)
        {
            DeskUi.Clear(timeline);
            var selectedDay = selection == 0 ? -1 : dayPicker.GetItemMetadata((int)selection).AsInt32();
            foreach (var group in records.Where(r => selectedDay < 0 || r.Day == selectedDay).GroupBy(r => r.Day).OrderBy(g => g.Key))
            {
                timeline.AddChild(DeskUi.Label($"Day {group.Key}"));
                var known = group.Where(r => decisions.ContainsKey(r.Id)).Select(r => decisions[r.Id]).FirstOrDefault()?.Known;
                if (known is not null)
                {
                    var label = DeskUi.Label("Known then: " + string.Join(", ", known.Select(k => view.NameOf(k.IslandId) + " " + k.Tier)));
                    label.SetMeta("known_day", group.Key); timeline.AddChild(label);
                }
                foreach (var record in group)
                {
                    // Sim's sentence already names its source/label. A second heading repeats it.
                    var label = DeskUi.Label(record.Text);
                    if (events.TryGetValue(record.Id, out var item))
                    {
                        label.TooltipText = DebriefWords.TimelineKind(item.Kind); label.SetMeta("event_record", item.RecordId);
                    }
                    else if (decisions.TryGetValue(record.Id, out var decision))
                    {
                        label.TooltipText = decision.Rejected ? "Rejected decision" : "Decision"; label.SetMeta("decision_record", decision.RecordId);
                    }
                    timeline.AddChild(label);
                }
            }
            if (timeline.GetChildCount() == 0) timeline.AddChild(DeskUi.Label("No events or decisions for this day."));
        }
        dayPicker.ItemSelected += ShowDay; ShowDay(0);
        foreach (var commitment in report.Commitments)
            parent.AddChild(DeskUi.Label($"{view.NameOf(commitment.Id)}, opened day {commitment.OpenedDay}.\n{commitment.Text}\nDecision: {commitment.Decision}, {(commitment.DecidedDay is { } day ? $"on day {day}" : "not yet decided")}. " +
                (commitment.WalkedOff ? "The vendor walked off. " : "") + (commitment.InquiryDrawn ? "It drew a higher headquarters inquiry." : "")));
        if (report.Commitments.Count == 0) parent.AddChild(DeskUi.Label("No unauthorized commitments."));
    }

    private static void AddCauseTrace(VBoxContainer parent, SessionView view, IReadOnlyList<string> roots)
    {
        var records = view.Records.Where(view.Known).ToDictionary(r => r.Id, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string id, int depth)
        {
            if (!seen.Add(id)) return;
            if (!records.TryGetValue(id, out var record)) { parent.AddChild(DeskUi.Label("An earlier cause was not reported to the shop.")); return; }
            var label = DeskUi.Label($"{(depth == 0 ? "Evidence" : "Because")}, day {record.Day}\n{record.Text}");
            label.SetMeta("cause_record", id); label.SetMeta("cause_depth", depth);
            var indent = new MarginContainer(); indent.AddThemeConstantOverride("margin_left", Math.Min(depth, 8) * 12);
            indent.AddChild(label); parent.AddChild(indent);
            foreach (var cause in record.CausedBy) Visit(cause, depth + 1);
        }
        foreach (var id in roots) Visit(id, 0);
    }
}
