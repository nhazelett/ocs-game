using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// A plan of the day before it ends (DESK-39, docs/design/desk-redesign.md D19). Nick on his 13 Nov replay asked for
// "something to show everything that you're executing on the day before ending the day". End the day opens one page:
// each teammate's work today in queue order, what should finish and what carries over, then deliveries and starts due
// today, then what the DESK-04 check says is open. It is a forecast: the hours follow the same rules as the day loop
// (SF 44 trips at the front, then the staff meeting, then the queue in the order given, behind a lost-confidence site
// when there is one), but events and rolls are not known, so it says "should" and never promises a delivery. Read only.
public sealed partial class Session
{
    internal DayPlanView BuildDayPlan()
    {
        var d = Day;
        var (people, due) = PlanPeople();

        // Support due to start or to end today.
        foreach (var a in _arrangements.Where(a => a.Status == ArrangementStatus.Pending && a.StartDay == d && !ForShop(a.CapabilityId)).OrderBy(a => a.SiteId).ThenBy(a => a.CapabilityId))
        {
            var site = SiteData(a.SiteId);
            var crossing = Cap(a.CapabilityId).Delivery == "consumable" && a.SupplierIslandId != site.Island && !a.PaidPerDelivery;
            due.Add($"{SupplierName(a)} should start {Lower(CapLabel(a.CapabilityId))} at {site.Name} today{(crossing ? ", if the boat runs" : "")}.");
        }
        foreach (var a in _arrangements.Where(a => a.Status is ArrangementStatus.Active or ArrangementStatus.Suspended && a.EndDay == d && d < LastDay && !ForShop(a.CapabilityId)))
            due.Add($"Today is the last day of {Lower(CapLabel(a.CapabilityId))} at {SiteName(a.SiteId)} from {SupplierName(a)}.");

        return new DayPlanView(d, people, [.. due.Select(SentenceCase)], BuildEndOfDayCheck());
    }

    /// <summary>Each teammate's day as the day loop will work it, and the deliveries due (DESK-39); the strip's hours come
    /// from here too (DESK-78), so the two never disagree.</summary>
    private (List<DayPlanPerson> People, List<string> Due) PlanPeople()
    {
        var d = Day;
        var people = new List<DayPlanPerson>();
        var due = new List<string>();

        // Before the shop works, as AdvanceDay does: SF 44 deliveries due today (made or held), then the staff meeting.
        var trips = TripsToday(due);
        var meeting = _attendingMeetings && _nextMeetingDay == d ? Member(_meetingAssignee) : null;

        // Then the queue, in the order the day loop works it.
        var ready = _work.Where(w => w.CompletedDay is null && w.WaitUntilDay <= d && w.Kind != WorkKind.GpcProgram).ToList();
        var order = _lostConfidence.Count == 0 ? ready : ready.OrderBy(ConfidenceRank).ToList();
        foreach (var m in _team)
        {
            var left = m.HoursPerDayMilli;
            var lines = new List<(string? WorkId, string Label, long Need, long Spend, string Late)>();
            void Take(string? workId, string label, long need, string late = "")
            {
                var spend = Math.Min(left, need);
                left -= spend;
                lines.Add((workId, label, need, spend, late));
            }
            foreach (var t in trips.Where(t => t.Who.Id == m.Id)) Take(null, t.Label, t.Milli);
            if (meeting?.Id == m.Id) Take(null, "Staff meeting", PersonHours(Fixed.Milli(People.StaffMeeting.WeeklyShopHours), m.Id));
            foreach (var w in order.Where(w => Member(w.Assignee).Id == m.Id))
                Take(w.Id, WorkName(w), w.HoursRemainingMilli, AfterNeed(w));   // DESK-78: a buy that starts after its need ended says so
            var shownToday = ShownToday(lines.Select(l => l.Spend).ToList());
            var items = new List<DayPlanItem>();
            for (var i = 0; i < lines.Count; i++)
            {
                var (workId, label, need, spend, late) = lines[i];
                var today = shownToday[i];
                var finishes = need - spend <= 0 && workId is not null;
                // What it says is left after today is what the team page will say tomorrow.
                var after = TaskHours(need - spend);
                // DESK-132: the standing staff meeting and an SF 44 trip take hours today; they are not jobs that finish.
                var line = workId is null && need - spend <= 0 ? $"{label}: {Said(today)} today."
                    : finishes
                    ? $"{label}: {Said(today)} today, and it should be done today."
                    : spend <= 0 ? $"{label}: no hours left for it today; {Said(after)} to go."
                    : $"{label}: {Said(today)} today, {Said(after)} left after today.";
                items.Add(new DayPlanItem(workId, label, today, after, finishes, SentenceCase(line) + late));
            }
            var waiting = _work.Where(w => w.CompletedDay is null && w.WaitUntilDay > d && w.Kind != WorkKind.GpcProgram && Member(w.Assignee).Id == m.Id)
                               .OrderBy(w => w.WaitUntilDay).ToList();

            var name = m.Role == "you" ? "You" : MemberName(m);
            // The page puts the name above this line, so the line does not repeat it. Hours are the ones the player can
            // add up on the page: planned is the sum of the work lines as shown, and what is left over is the day less
            // that, so "2 of 8 hours planned" never sits beside "6.5 hours left over" (Fable's PR 242 review).
            var perDay = HalfHours(m.HoursPerDayMilli);
            var shown = Math.Min(perDay, items.Sum(i => i.HoursToday));
            var leftShown = perDay - shown;
            var jobs = items.Where(i => i.WorkId is not null).ToList();   // DESK-132: only queued work is a job that finishes
            var done = jobs.Count(i => i.FinishesToday);
            var carried = jobs.Count - done;
            var sentence = items.Count == 0
                ? $"Nothing to work on today; {Said(perDay)} go to walk-in small buys."
                : $"{HoursText(shown)} of {Said(perDay)} planned."
                  + (jobs.Count == 0 ? "" : $" {Jobs(done)} should finish today{(carried > 0 ? $", and {Jobs(carried).ToLowerInvariant()} carr{(carried == 1 ? "ies" : "y")} over" : "")}.")
                  + (leftShown > 0 ? $" {Said(leftShown)} left over {(leftShown == 1 ? "goes" : "go")} to walk-in small buys." : "");
            // DESK-60 (D19; Claude play note 19): say why a job waits, not "not ready until a later day".
            foreach (var w in waiting) sentence += $" {WorkName(w)} {WaitReason(w)}.{AfterNeed(w)}";
            people.Add(new DayPlanPerson(m.Id, name, m.Portrait, perDay, shown, items, sentence));
        }
        // DESK-149 (c), Claude play 21 Nov, second play, note 11: three plays in a row asked whether hours going to walk-in
        // small buys was good or bad. Until spare hours have first gone to them, the first plan line that sends hours there
        // says once what they are and what happens when they pile up (SmallBuys: the customer goes to the commander once
        // about three days' worth wait).
        if (_smallBuyHoursMilli == 0 && !_smallBuysHeld
            && people.FindIndex(p => p.Sentence.Contains("to walk-in small buys", StringComparison.Ordinal)) is var first and >= 0)
            people[first] = people[first] with { Sentence = people[first].Sentence + " " + WalkInsSaid() };
        return (people, due);

        static string Jobs(int n) => n switch { 0 => "Nothing", 1 => "One job", _ => $"{n} jobs" };
        static string Said(double h) => h == 1 ? "1 hour" : $"{HoursText(h)} hours";
    }

    /// <summary>DESK-149 (c): what walk-in small buys are, in the Sim's terms (SmallBuys.cs).</summary>
    private string WalkInsSaid()
        => $"Walk-in small buys are the lumber, tools and parts {SmallBuyCustomer} brings to the counter; spare hours there are well spent, because once about {Rules.SmallBuys.ComplainAfterHeldDays} {(Rules.SmallBuys.ComplainAfterHeldDays == 1 ? "day's" : "days'")} worth are waiting, {SmallBuyCustomer} goes to the commander.";

    /// <summary>Why a work item cannot be worked yet, without a final period: "waits for finance to certify the funds on day 5".</summary>
    internal string WaitReason(WorkItem w) => w.Kind switch
    {
        // DESK-134: a purchase order with a vendor that prices first waits for the proposal when it comes after the funds.
        WorkKind.Award when w.Payload is AwardPayload { ProposalDay: { } pd } p && pd >= w.WaitUntilDay
                            && VendorData(p.Action.Vendor) is { } pricer =>
            $"waits for {Possessive(pricer.Name)} priced proposal, due on day {w.WaitUntilDay}",
        WorkKind.Award when w.Payload is AwardPayload { Action.Instrument: "uca" } =>
            $"waits for finance to certify the funds and for the UCA to be approved, until day {w.WaitUntilDay}",
        WorkKind.Award or WorkKind.AcsaOrder or WorkKind.HostNationRequest or WorkKind.ExternalSupportRequest =>
            $"waits for finance to certify the funds on day {w.WaitUntilDay}",
        _ => $"can start on day {w.WaitUntilDay}",
    };

    /// <summary>DESK-179 (Claude play 21 Nov, eighth play, notes P2-15, P2-18): a task's hours as every view says them, rounded
    /// up to the half hour: the team page, the decisions, the hand-over and the day's plan.</summary>
    internal static double TaskHours(long milli) => HalfHours(milli);

    /// <summary>DESK-179: one person's hours today, line by line in the order they are worked, in half hours that add up to
    /// their running total rounded up, so "8 of 8 hours planned" is the sum of the lines shown: an experienced person's
    /// 0.8-hour staff meeting and 7.2 hours of writing read 1 and 7. A line with any hours shows at least half an hour.</summary>
    private static double[] ShownToday(IReadOnlyList<long> lines)
    {
        var shown = new double[lines.Count];
        long run = 0; double said = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i] <= 0) continue;
            run += lines[i];
            shown[i] = Math.Max(0.5, HalfHours(run) - said);
            said += shown[i];
        }
        return shown;
    }

    /// <summary>"1 hour", "2.5 hours", rounded up to the half hour as every view rounds (DESK-14).</summary>
    private static string HoursSaid(long milli) => TaskHours(milli) is var h && h == 1 ? "1 hour" : $"{HoursText(h)} hours";

    /// <summary>The SF 44 trips the day loop makes today before the queue, each with who makes it and their hours; what is
    /// due or held goes to <paramref name="due"/> when given. Shared by the day's plan and <c>FixedTodayMilli</c>.</summary>
    private List<(TeamMember Who, string Label, long Milli)> TripsToday(List<string>? due = null)
    {
        var d = Day;
        var trips = new List<(TeamMember Who, string Label, long Milli)>();
        var cash = _cash;
        foreach (var a in _arrangements.Where(x => x.PaidPerDelivery))
        {
            if (!(a.StartDay <= d && a.EndDay >= d && a.Status is ArrangementStatus.Active or ArrangementStatus.Pending
                  && PopulationOn(SiteData(a.SiteId), d) > 0)) continue;
            var site = SiteData(a.SiteId);
            var what = $"The SF 44 delivery of {Lower(CapLabel(a.CapabilityId))} from {SupplierName(a)} at {site.Name}";
            if (SurfaceClosed(a.SupplierIslandId, site.Island, d))
            {
                due?.Add($"{what} will not happen today: the boat to {IslandData(site.Island).Name} is not running.");
                continue;
            }
            var cost = a.DailyCostCents + (a.MobilizationCharged ? 0 : a.MobilizationCents);
            if (cost > cash)
            {
                due?.Add($"{what} will not happen today: it is paid in cash at the door, {Money(cost)}, and the cash box holds {Money(cash)}.");
                continue;
            }
            cash -= cost;
            var who = Member(a.TripAssignee);
            trips.Add((who, $"SF 44 delivery and payment at {site.Name}", PersonHours(TripMilli(a, d), who.Id)));
            due?.Add($"{what} is due today: {(who.Role == "you" ? "you make" : MemberName(who) + " makes")} the trip and pays {Money(cost)} in cash.");
        }
        return trips;
    }

    /// <summary>DESK-176 (Claude play 21 Nov, seventh play, note P2-13): the hours of this person's day that go before their
    /// queue today, as the day loop and the day's plan spend them: SF 44 trips, then the standing staff meeting. The
    /// queue forecasts (<c>PlayQueue</c>) start today from what is left, so a hand-over's done day matches the plan.</summary>
    private long FixedTodayMilli(TeamMember m)
    {
        var milli = TripsToday().Where(t => t.Who.Id == m.Id).Sum(t => t.Milli);
        if (_attendingMeetings && _nextMeetingDay == Day && Member(_meetingAssignee).Id == m.Id)
            milli += PersonHours(Fixed.Milli(People.StaffMeeting.WeeklyShopHours), m.Id);
        return milli;
    }
}
