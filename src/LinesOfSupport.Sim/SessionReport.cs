using System.Globalization;
using System.Text;
using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim;

/// <summary>
/// Plain-text account of a game: a day-by-day log of what happened and why, then the scorecard and the gap list.
/// This is the M1 review artifact (text before UI) and the seed of the debrief.
/// </summary>
public static class SessionReport
{
    public static string ToText(Session s, string? title = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(title ?? $"{s.Scenario.Title}");
        sb.AppendLine($"Scenario {s.Scenario.Id}, rules {s.Rules.RulesVersion}, seed {s.Seed}, {s.LastDay} days.");
        if (s.PrepChoices.Count > 0) sb.AppendLine($"Prep: {string.Join(", ", s.PrepChoices)}.");
        sb.AppendLine();

        sb.AppendLine("TIMELINE");
        foreach (var group in s.Records.GroupBy(r => r.Day).OrderBy(g => g.Key))
        {
            sb.AppendLine(group.Key == 0 ? "Before day 1" : $"Day {group.Key}");
            foreach (var r in group)
            {
                if (r.Kind == RecordKind.Rejected && r.Text.StartsWith("Rejected:", StringComparison.Ordinal))
                    sb.AppendLine($"  ! {r.Subject}: {r.Text}");
                else
                    sb.AppendLine($"  {Tag(r.Kind),-9} {r.Text}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("CAPABILITY BY DAY (percent; . = 100, blank = site not open)");
        var days = s.LastDay;
        sb.Append("".PadRight(26));
        for (var d = 1; d <= days; d++) sb.Append(d % 5 == 0 ? (d / 10 % 10).ToString(CultureInfo.InvariantCulture) : " ");
        sb.AppendLine();
        sb.Append("".PadRight(26));
        for (var d = 1; d <= days; d++) sb.Append((d % 10).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine();
        foreach (var c in s.Capabilities)
        {
            var site = s.Scenario.Sites.First(x => x.Id == c.SiteId);
            var label = $"{site.Name} {s.Rules.Model.Capabilities.First(x => x.Id == c.CapabilityId).Label}";
            sb.Append(label.Length > 25 ? label[..25] : label.PadRight(25)).Append(' ');
            var firstOpen = Math.Max(1, site.OpensDay);
            for (var d = 1; d <= days; d++)
            {
                var idx = d - firstOpen;
                if (d < firstOpen || idx >= c.History.Count) { sb.Append(' '); continue; }
                var p = c.History[idx];
                sb.Append(p >= 1000 ? '.' : p >= 750 ? '7' : p >= 500 ? '5' : p >= 250 ? '2' : p > 0 ? '1' : 'X');
            }
            sb.AppendLine();
        }
        sb.AppendLine("  key: . full, 7 = 75-99%, 5 = 50-74%, 2 = 25-49%, 1 = 1-24%, X = none");
        sb.AppendLine();

        sb.AppendLine("SCORECARD");
        var m = s.Metrics;
        string Pct(string k) => $"{m[k] * 100:0}%";
        sb.AppendLine($"  Mission output {Pct("mission.output")} (sortie generation {Pct("mission.sortieGeneration")}, throughput {Pct("mission.throughput")}, endurance {Pct("mission.endurance")}, resilience {Pct("mission.resilience")})");
        sb.AppendLine($"  Obligated {Session.Money((long)m["cost.obligatedCents"])}; incurred {Session.Money((long)m["cost.incurredCents"])}; incurred but not obligated {Session.Money((long)m["cost.unobligatedIncurredCents"])}");
        sb.AppendLine($"  Unauthorized commitments {m["compliance.unauthorizedCommitments"]}; UCAs past due {m["compliance.ucaOverdue"]}; HHQ inquiries {m["compliance.hhqInquiries"]}");
        sb.AppendLine($"  Surveys {m["info.surveys"]}; shop hours worked {m["shop.hoursWorked"]}; payment errors {m["work.paymentErrors"]}; execution-confusion days {m["work.executionConfusionDays"]}; vendor failures {m["work.vendorFailures"]}; declined BPA calls {m["work.bpaDeclines"]}");
        foreach (var c in s.Capabilities.Where(c => c.DownDays + c.DegradedDays > 0))
            sb.AppendLine($"  {c.SiteId}.{c.CapabilityId}: {c.DownDays} day(s) down, {c.DegradedDays} degraded, lowest {c.MinPermille / 10}%");
        sb.AppendLine();

        if (s.Commitments.Count > 0)
        {
            sb.AppendLine("UNAUTHORIZED COMMITMENTS (SPEC 4.5)");
            foreach (var c in s.Commitments)
            {
                var what = c.Kind == CommitmentKind.Handshake
                    ? $"handshake with {s.Scenario.Vendors.FirstOrDefault(x => x.Id == c.VendorId)?.Name ?? c.VendorId} for {c.SiteId}.{c.CapabilityId}"
                    : $"out-of-scope work on {c.ArrangementId}, {Session.Money(c.AmountCents)}";
                var decided = c.DecidedDay is { } dd ? $" on day {dd}" : "";
                var walked = c.WalkedOff ? "; vendor walked off" : "";
                var inquiry = c.InquiryDrawn ? "; drew an HHQ inquiry" : "";
                sb.AppendLine($"  {c.Id} (day {c.OpenedDay}): {what}. Decision: {c.Decision.ToString().ToLowerInvariant()}{decided}{walked}{inquiry}.");
            }
            foreach (var site in s.Scenario.Sites)
                if (s.Metrics.TryGetValue($"site.{site.Id}.unitHabitPermille", out var h) && h > 0)
                    sb.AppendLine($"  {site.Name} unit habit of going around contracting: {h / 10:0}%");
            sb.AppendLine();
        }

        var gaps = s.Records.Where(r => r.Kind == RecordKind.Shortfall).ToList();
        if (gaps.Count > 0)
        {
            sb.AppendLine("GAPS AND WHY (each traced back through its causes)");
            var byId = s.Records.ToDictionary(r => r.Id);
            foreach (var g in gaps)
            {
                sb.AppendLine($"  Day {g.Day}: {g.Text}");
                foreach (var line in Chain(g, byId, 1, []))
                    sb.AppendLine(line);
            }
            sb.AppendLine();
        }
        var debrief = s.View.Debrief();
        if (debrief.Gaps.Count > 0)
        {
            sb.AppendLine("GAP TAGS (avoidable: the information existed; discoverable: available for a cost; unforeseeable: no path to know)");
            foreach (var g in debrief.Gaps)
            {
                sb.AppendLine($"  #{g.Index} {g.SiteId}.{g.CapabilityId} days {g.StartDay}-{g.EndDay}, lowest {g.LowestPermille / 10}%, {g.DownDays} day(s) down: {g.Tag.ToString().ToUpperInvariant()}");
                foreach (var f in g.Factors)
                    sb.AppendLine($"      [{f.Tag.ToString().ToLowerInvariant()}] {f.Text}{(f.DependsOnGap is { } x ? $" (see #{x})" : "")}");
            }
            sb.AppendLine();
        }
        var pass = s.Scenario.PassCriteria;
        if (pass.Count > 0)
        {
            sb.AppendLine("PASS CRITERIA");
            foreach (var p in pass)
            {
                var ok = m.TryGetValue(p.Metric, out var v) && Compare(v, p.Comparator, p.Value);
                sb.AppendLine($"  [{(ok ? "met" : "not met")}] {p.Label} ({p.Metric} = {(m.TryGetValue(p.Metric, out var vv) ? vv.ToString("0.###", CultureInfo.InvariantCulture) : "n/a")})");
            }
        }
        sb.AppendLine();
        sb.AppendLine($"State hash {s.StateHash()}");
        return sb.ToString();
    }

    private static IEnumerable<string> Chain(CauseRecord r, Dictionary<string, CauseRecord> byId, int depth, HashSet<string> seen)
    {
        if (depth > 4) yield break;
        foreach (var id in r.CausedBy)
        {
            if (!seen.Add(id) || !byId.TryGetValue(id, out var c)) continue;
            yield return $"{new string(' ', 2 + depth * 4)}<- day {c.Day}: {c.Text}";
            foreach (var deeper in Chain(c, byId, depth + 1, seen)) yield return deeper;
        }
    }

    private static bool Compare(double a, string op, double b) => op switch
    {
        "<=" => a <= b, ">=" => a >= b, "<" => a < b, ">" => a > b, "==" => Math.Abs(a - b) < 1e-9, _ => false,
    };

    private static string Tag(RecordKind k) => k switch
    {
        RecordKind.Action => "ACTION",
        RecordKind.WorkDone => "DONE",
        RecordKind.ArrangementStart => "START",
        RecordKind.ArrangementEnd => "END",
        RecordKind.Declined => "DECLINED",
        RecordKind.Shortfall => "SHORT",
        RecordKind.Restored => "OK",
        RecordKind.VendorFailure => "FAIL",
        RecordKind.ExecutionConfusion => "CONFUSED",
        RecordKind.PaymentError => "PAYERR",
        RecordKind.Event => "EVENT",
        RecordKind.Knowledge => "SURVEY",
        RecordKind.UcaProposal => "UCA",
        RecordKind.UcaOverdue => "UCA-DUE",
        RecordKind.UcaStopWork => "STOPWORK",
        RecordKind.UcaResumed => "RESUMED",
        RecordKind.Definitized => "DEFINITZD",
        RecordKind.Compliance => "COMPLY",
        RecordKind.Cash => "CASH",
        RecordKind.Withdrawal => "WITHDRAWN",
        RecordKind.Obligation => "OBLIGATE",
        _ => k.ToString().ToUpperInvariant(),
    };
}
