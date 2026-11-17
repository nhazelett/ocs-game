using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// Shortfalls, the strip and quotes say the true thing (DESK-78, the 17 Nov gameplay review section 7). Each helper here
// answers one defect the review's players met: a peak demand with no day or headcount, a start range with nothing saying
// what decides it, the strip's hours leaving out the staff meeting and the delivery trips, the same COR reading two ways,
// and a buy that starts after its need ended with nothing in the plan saying so. Read only.
public sealed partial class Session
{
    /// <summary>The first day demand peaks between from and to.</summary>
    internal int PeakDay(SiteData site, string capId, int from, int to)
    {
        var best = Math.Max(1, from);
        long peak = -1;
        for (var d = Math.Max(1, from); d <= Math.Max(from, to); d++)
        {
            var m = DemandMilli(site, capId, d);
            if (m > peak) { peak = m; best = d; }
        }
        return best;
    }

    /// <summary>"Demand peaks at 468 kW on day 20, when 260 people are at Spoke B; today it is 324 kW, for 180." Null when
    /// demand is the same on every day of the request.</summary>
    internal string? PeakSaid(SiteData site, CapabilityData cap, int from, int to)
    {
        if (to < from) return null;
        var day = PeakDay(site, cap.Id, from, to);
        var peak = (DemandMilli(site, cap.Id, day) + 999) / 1000;
        var first = (DemandMilli(site, cap.Id, from) + 999) / 1000;
        if (peak == first) return null;
        var now = from <= Day ? "today" : $"on day {from}";
        return $"Demand peaks at {Amount(peak, cap)} on day {day}, when {Qty(PopulationOn(site, day))} people are at {site.Name}; {now} it is {Amount(first, cap)}, for {PopulationOn(site, from)}.";
    }

    /// <summary>What decides an earliest-start range, without a final period: the vendor's days to start after the order,
    /// the hours the paperwork takes, the lift.</summary>
    private string? StartRange(AwardAction a, InstrumentData inst, int hMin, int hMax, (int Min, int Max) lift)
    {
        var parts = new List<string>();
        if (hMax > hMin) parts.Add($"writing it takes {hMin} to {Plural(hMax, "shop hour")}");
        if (a.Instrument == "uca" && Rules.Uca.ApprovalLagDays.Max > Rules.Uca.ApprovalLagDays.Min)
            parts.Add($"approval takes {Rules.Uca.ApprovalLagDays.Min} to {Plural(Rules.Uca.ApprovalLagDays.Max, "day")}");
        if (inst.StartLagDays.Max > inst.StartLagDays.Min)
            parts.Add(inst.StartLagDays.Min == 0
                ? $"the vendor can take up to {Plural(inst.StartLagDays.Max, "day")} after the order to start"
                : $"the vendor takes {inst.StartLagDays.Min} to {Plural(inst.StartLagDays.Max, "day")} after the order to start");
        if (lift.Max > lift.Min) parts.Add($"the lift takes {lift.Min} to {Plural(lift.Max, "day")}");
        return parts.Count == 0 ? null : JoinWords(parts);
    }

    /// <summary>
    /// The hours a teammate's day is spoken for, as the day's plan counts them (DESK-78): their delivery trips, the staff
    /// meeting when it is theirs today, then their ready work, capped at their day. The strip read "0 of 16 h assigned" on
    /// a day with a meeting, trips and work, and "4 of 8" where the plan said 5.
    /// </summary>
    internal double PlannedHours(string memberId)
        => PlanPeople().People.FirstOrDefault(p => p.MemberId == memberId)?.HoursPlanned ?? 0;

    /// <summary>DESK-78: a COR as the same contract's order form reads them: fit and load for that contract's work.</summary>
    internal string CorSummary(CorState c, Arrangement a)
    {
        if (c.Knows is null) return CorSummary(c);
        var q = CorQuality(c);
        return $"{c.Quality}: reports {(q.LagDays == 0 ? "the same day" : $"{Plural(q.LagDays, "day")} late")}, about {CorReportPermille(c, a.CapabilityId, a.Id) / 10}% of problems on this contract";
    }

    /// <summary>DESK-78: a buy in work that starts after the day its request's need ends, said in the day's plan.</summary>
    private string AfterNeed(WorkItem w)
    {
        if (w.Payload is not AwardPayload p || p.Action.Card is not { } cardId || p.Action.StartDay is not { } start) return "";
        if (_cards.FirstOrDefault(c => c.Id == cardId) is not { } card) return "";
        var end = card.RequestedStartDay + Math.Max(1, card.DurationDays) - 1;
        return start > end ? $" It starts on day {start}, after {Lower(CardTitle(card))} is no longer needed (its last day is day {end})." : "";
    }
}
