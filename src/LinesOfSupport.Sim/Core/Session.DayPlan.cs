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
        var people = new List<DayPlanPerson>();
        var due = new List<string>();

        // Before the shop works, as AdvanceDay does: SF 44 deliveries due today (made or held), then the staff meeting.
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
                due.Add($"{what} will not happen today: the boat to {IslandData(site.Island).Name} is not running.");
                continue;
            }
            var cost = a.DailyCostCents + (a.MobilizationCharged ? 0 : a.MobilizationCents);
            if (cost > cash)
            {
                due.Add($"{what} will not happen today: it is paid in cash at the door, {Money(cost)}, and the cash box holds {Money(cash)}.");
                continue;
            }
            cash -= cost;
            var who = Member(a.TripAssignee);
            trips.Add((who, $"SF 44 delivery and payment at {site.Name}", PersonHours(TripMilli(a, d), who.Id)));
            due.Add($"{what} is due today: {(who.Role == "you" ? "you make" : MemberName(who) + " makes")} the trip and pays {Money(cost)} in cash.");
        }
        var meeting = _attendingMeetings && _nextMeetingDay == d ? Member(_meetingAssignee) : null;

        // Then the queue, in the order the day loop works it.
        var ready = _work.Where(w => w.CompletedDay is null && w.WaitUntilDay <= d && w.Kind != WorkKind.GpcProgram).ToList();
        var order = _lostConfidence.Count == 0 ? ready : ready.OrderBy(ConfidenceRank).ToList();
        foreach (var m in _team)
        {
            var left = m.HoursPerDayMilli;
            var items = new List<DayPlanItem>();
            void Take(string? workId, string label, long need)
            {
                var spend = Math.Min(left, need);
                left -= spend;
                var after = need - spend;
                var finishes = after <= 0;
                var sentence = finishes
                    ? $"{label}: {HoursSaid(spend)} today, and it should be done today."
                    : spend <= 0 ? $"{label}: no hours left for it today; {HoursSaid(after)} to go."
                    : $"{label}: {HoursSaid(spend)} today, {HoursSaid(after)} left after today.";
                items.Add(new DayPlanItem(workId, label, HalfHours(spend), HalfHours(after), finishes, SentenceCase(sentence)));
            }
            foreach (var t in trips.Where(t => t.Who.Id == m.Id)) Take(null, t.Label, t.Milli);
            if (meeting?.Id == m.Id) Take(null, "Staff meeting", PersonHours(Fixed.Milli(People.StaffMeeting.WeeklyShopHours), m.Id));
            foreach (var w in order.Where(w => Member(w.Assignee).Id == m.Id)) Take(w.Id, w.Label, w.HoursRemainingMilli);
            var waiting = _work.Count(w => w.CompletedDay is null && w.WaitUntilDay > d && w.Kind != WorkKind.GpcProgram && Member(w.Assignee).Id == m.Id);

            var name = m.Role == "you" ? "You" : MemberName(m);
            var used = m.HoursPerDayMilli - left;
            var done = items.Count(i => i.FinishesToday);
            var carried = items.Count - done;
            var sentence = items.Count == 0
                ? $"{name}: nothing to work on today; {HoursSaid(m.HoursPerDayMilli)} go to walk-in small buys."
                : $"{name}: {HoursSaid(used)} of {HoursSaid(m.HoursPerDayMilli)} planned. {Jobs(done)} should finish today{(carried > 0 ? $", and {Jobs(carried).ToLowerInvariant()} carr{(carried == 1 ? "ies" : "y")} over" : "")}."
                  + (left > 0 ? $" {HoursSaid(left)} left over go to walk-in small buys." : "");
            if (waiting > 0) sentence += $" {(waiting == 1 ? "One more job is" : $"{waiting} more jobs are")} not ready until a later day.";
            people.Add(new DayPlanPerson(m.Id, name, m.Portrait, HalfHours(m.HoursPerDayMilli), HalfHours(used), items, sentence));
        }

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

        static string Jobs(int n) => n switch { 0 => "Nothing", 1 => "One job", _ => $"{n} jobs" };
    }

    /// <summary>"1 hour", "2.5 hours", rounded up to the half hour as every view rounds (DESK-14).</summary>
    private static string HoursSaid(long milli) => HalfHours(milli) is var h && h == 1 ? "1 hour" : $"{HoursText(HalfHours(milli))} hours";
}
