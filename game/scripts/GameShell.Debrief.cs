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

    private void BuildDebriefDesk() => debriefPanel = DeskUi.Panel(GetNode<TabContainer>("%DeskTabs").GetNode<Control>("Debrief"), "DebriefSummary");

    private void RefreshDebrief(SessionView? view)
    {
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
        debriefPanel.AddChild(DeskUi.Label($"{report.ScenarioTitle} · {report.DaysPlayed} days played · through day {report.LastDay}"));
        var score = report.Scorecard;
        static string Fraction(double value) => value.ToString("P1", CultureInfo.InvariantCulture);
        debriefPanel.AddChild(DeskUi.Label($"Mission output {Fraction(score.MissionOutput)} · sortie generation {Fraction(score.SortieGeneration)}\nThroughput {Fraction(score.Throughput)} · endurance {Fraction(score.Endurance)} · resilience {Fraction(score.Resilience)}\nObligated {DeskUi.Money(score.ObligatedCents)} · incurred {DeskUi.Money(score.IncurredCents)} · unobligated incurred {DeskUi.Money(score.UnobligatedIncurredCents)}\nUnauthorized commitments {score.UnauthorizedCommitments} · overdue UCAs {score.UcaOverdue} · HHQ inquiries {score.HhqInquiries}\nSurveys {score.Surveys} · shop hours worked {score.ShopHoursWorked}\n{score.GapCount} gaps: {score.AvoidableGaps} avoidable · {score.DiscoverableGaps} discoverable · {score.UnforeseeableGaps} unforeseeable"));
        debriefPanel.AddChild(DeskUi.Label("Capability by day · scroll horizontally; — means not open"));
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 190), VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        var grid = new GridContainer { Columns = report.DaysPlayed + 1 };
        scroll.AddChild(grid); debriefPanel.AddChild(scroll);
        grid.AddChild(new Label { Text = "Site / capability" });
        for (var day = 1; day <= report.DaysPlayed; day++) grid.AddChild(new Label { Text = $"Day {day}", CustomMinimumSize = new Vector2(62, 0) });
        foreach (var band in report.Timeline)
        {
            grid.AddChild(new Label { Text = $"{view.NameOf(band.SiteId)} · {band.Label}" });
            foreach (var value in band.PermilleByDay)
            {
                var cell = new Label { Text = value < 0 ? "—" : $"{value / 10m:0.#}%" };
                cell.Modulate = value < 0 ? Colors.Gray : new Color(1, .55f, .45f).Lerp(new Color(.55f, 1, .65f), Math.Clamp(value / 1000f, 0, 1));
                grid.AddChild(cell);
            }
        }
        debriefPanel.AddChild(DeskUi.Label("Timeline and decisions · choose a day"));
        var dayPicker = new OptionButton(); dayPicker.AddItem("All days");
        for (var day = 1; day <= report.LastDay; day++) dayPicker.AddItem($"Day {day}");
        debriefPanel.AddChild(dayPicker);
        var timeline = new VBoxContainer(); debriefPanel.AddChild(timeline);
        var knownIds = view.Records.Where(view.Known).Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        void ShowDay(long day)
        {
            DeskUi.Clear(timeline);
            foreach (var item in report.Events.Where(e => knownIds.Contains(e.RecordId) && (day == 0 || e.Day == day)))
            {
                var label = DeskUi.Label($"Day {item.Day} · {Words(item.Kind)} · {view.NameOf(item.Subject)}\n{item.Text}");
                label.SetMeta("event_record", item.RecordId); timeline.AddChild(label);
            }
            foreach (var decision in report.Decisions.Where(d => knownIds.Contains(d.RecordId) && (day == 0 || d.Day == day)))
            {
                var label = DeskUi.Label($"Day {decision.Day} · {(decision.Rejected ? "Rejected decision" : "Decision")}\n{decision.Text}\nKnown then: {string.Join(", ", decision.Known.Select(k => view.NameOf(k.IslandId) + " " + k.Tier))}");
                label.SetMeta("decision_record", decision.RecordId); timeline.AddChild(label);
            }
            if (timeline.GetChildCount() == 0) timeline.AddChild(DeskUi.Label("No events or decisions for this day."));
        }
        dayPicker.ItemSelected += ShowDay; ShowDay(0);
        debriefPanel.AddChild(DeskUi.Label("Gaps · expand for evidence and cause chains"));
        foreach (var gap in report.Gaps)
        {
            var detail = new VBoxContainer { Visible = false };
            var button = DeskUi.Button($"Gap {gap.Index} · {view.NameOf(gap.SiteId)} · {gap.Label}\nDays {gap.StartDay}–{gap.EndDay} · {gap.Tag} · lowest {gap.LowestPermille / 10m:0.#}% · {gap.DownDays} down days", () =>
            {
                if (detail.GetChildCount() == 0)
                {
                    foreach (var factor in gap.Factors)
                    {
                        detail.AddChild(DeskUi.Label($"{GapFactorLabel(factor.Kind)} · {factor.Tag} · first day {factor.FirstDay}\n{factor.Text}" + (factor.DependsOnGap is { } index ? $"\nDepends on gap {index}" : "")));
                        AddCauseTrace(detail, view, factor.RecordIds);
                    }
                    detail.AddChild(DeskUi.Label("Shortfall causes"));
                    AddCauseTrace(detail, view, gap.ShortfallRecordIds);
                }
                detail.Visible = !detail.Visible;
            });
            button.SetMeta("gap", gap.Index);
            debriefPanel.AddChild(button); debriefPanel.AddChild(detail);
        }
        debriefPanel.AddChild(DeskUi.Label("Unauthorized commitments and decisions"));
        foreach (var commitment in report.Commitments)
            debriefPanel.AddChild(DeskUi.Label($"{view.NameOf(commitment.Id)} · {Words(commitment.Kind)} · opened day {commitment.OpenedDay}\n{commitment.Text}\nDecision: {commitment.Decision} · day {commitment.DecidedDay?.ToString() ?? "—"} · walked off {commitment.WalkedOff} · inquiry drawn {commitment.InquiryDrawn}"));
        if (report.Commitments.Count == 0) debriefPanel.AddChild(DeskUi.Label("No unauthorized commitments."));
        GetNode<TabContainer>("%DeskTabs").CurrentTab = 8;
    }

    internal static string GapFactorLabel(GapFactorKind kind) => kind switch
    {
        GapFactorKind.FundingLapse => "Funding lapse",
        GapFactorKind.PopulationJump => "Unplanned population arrival",
        GapFactorKind.PriorityDeferred => "Shop work deferred after lost confidence",
        GapFactorKind.AcsaWithdrawn => "ACSA support withdrawn",
        GapFactorKind.HostNationWithdrawn => "Host nation support withdrawn",
        GapFactorKind.LiftDelay => "Transport delayed",
        _ => Words(kind)
    };

    private static void AddCauseTrace(VBoxContainer parent, SessionView view, IReadOnlyList<string> roots)
    {
        var records = view.Records.Where(view.Known).ToDictionary(r => r.Id, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string id, int depth)
        {
            if (!seen.Add(id)) { parent.AddChild(DeskUi.Label("This cause is shown above.")); return; }
            if (!records.TryGetValue(id, out var record)) { parent.AddChild(DeskUi.Label("An earlier cause was not reported to the shop.")); return; }
            var label = DeskUi.Label($"{(depth == 0 ? "Evidence" : "Because")} · day {record.Day} · {Words(record.Kind)}\n{record.Text}");
            label.SetMeta("cause_record", id); label.SetMeta("cause_depth", depth);
            var indent = new MarginContainer(); indent.AddThemeConstantOverride("margin_left", Math.Min(depth, 8) * 12);
            indent.AddChild(label); parent.AddChild(indent);
            foreach (var cause in record.CausedBy) Visit(cause, depth + 1);
        }
        foreach (var id in roots) Visit(id, 0);
    }
}
