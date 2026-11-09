using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// The one decision in front (M2R-05, docs/design/opening-and-guidance.md R5 and 6.3). Read-only and draws nothing, so
// it never changes an outcome. The fixed rule, first match wins:
//   1. an unresolved inbox decision: an open unauthorized commitment, a performance issue with no decision yet, or a
//      requirement that arrived today and nothing covers;
//   2. a requirement past its start that nothing covers;
//   3. an alert on this morning's board;
//   4. the nearest clock within LeadClockDays: an uncovered requirement about to start, or a funding line with money
//      left about to expire.
// Nothing matched means a quiet day: Lead is null and Continue may skip it.
public sealed partial class Session
{
    /// <summary>How near a clock must be to lead the day (days from today, inclusive).</summary>
    public const int LeadClockDays = 3;

    private static string LeadFundType(string type) => type == "OM" ? "O&M" : type;

    private bool CardCoveredNow(CardState c) =>
        _arrangements.Any(a => a.CardId == c.Id && a.Status != ArrangementStatus.Declined && a.Status != ArrangementStatus.Ended)
        || _work.Any(w => w.CompletedDay is null && w.Payload is AwardPayload p && p.Action.Card == c.Id)
        || _arrangements.Any(a => a.SiteId == c.SiteId && a.CapabilityId == c.CapabilityId && Live(a)
                                  && a.StartDay <= Math.Max(c.RequestedStartDay, Day) + LeadClockDays && a.EndDay >= Math.Max(c.RequestedStartDay, Day));

    internal LeadItem? BuildLead(IReadOnlyList<BoardItem> board)
    {
        if (Finished) return null;
        string What(CardState c) => CardTitle(c);

        // 1. Inbox decisions.
        foreach (var c in _commitments.Where(x => x.Decision == CommitmentDecision.Open).OrderBy(x => x.OpenedDay))
        {
            var vendor = c.VendorId is null ? "A vendor" : VendorData(c.VendorId)?.Name ?? "A vendor";
            var text = c.Kind == CommitmentKind.Handshake
                ? $"{vendor} is supplying {CapLabel(c.CapabilityId).ToLowerInvariant()} at {SiteName(c.SiteId)} on a handshake, with no contract behind it."
                : $"{vendor} has billed for work at {SiteName(c.SiteId)} that is not in its contract.";
            return new LeadItem(LeadKind.Decision, text, BoardLinkKind.Arrangement, c.ArrangementId, VendorRep(c.VendorId), null);
        }
        foreach (var i in _issues.Where(x => x.CureNoticeDay is null && IssueDecision(x) == "no decision").OrderBy(x => x.OpenedDay))
        {
            var vendor = i.VendorId is null ? "A contractor" : VendorData(i.VendorId)?.Name ?? "A contractor";
            return new LeadItem(LeadKind.Decision,
                $"{vendor} has fallen short on {CapLabel(i.CapabilityId).ToLowerInvariant()} at {SiteName(i.SiteId)} on {Plural(i.FailureDays.Count, "day")}.",
                BoardLinkKind.Arrangement, i.ArrangementId, VendorRep(i.VendorId), null);
        }
        // In scenario order, so the author decides who is first through the door.
        foreach (var c in _cards.Where(x => x.SubmittedDay == Day && !CardCoveredNow(x)))
        {
            var who = BuildPerson(c.PersonId);
            var text = who is null
                ? $"New requirement: {What(c)}, wanted from day {c.RequestedStartDay}."
                : $"{who.WithUnit}, is here about {CapLabel(c.CapabilityId).ToLowerInvariant()} for {SiteName(c.SiteId)}, wanted from day {c.RequestedStartDay}.";
            return new LeadItem(LeadKind.Decision, text, BoardLinkKind.Card, c.Id, who, c.RequestedStartDay);
        }

        // 2. A requirement past its start with nothing covering it.
        foreach (var c in _cards.Where(x => x.SubmittedDay <= Day && x.RequestedStartDay <= Day
                                            && x.RequestedStartDay + Math.Max(1, x.DurationDays) > Day && !CardCoveredNow(x))
                                .OrderBy(x => x.RequestedStartDay))
            return new LeadItem(LeadKind.Overdue, $"{What(c)} was wanted from day {c.RequestedStartDay} and nothing covers it.",
                BoardLinkKind.Card, c.Id, BuildPerson(c.PersonId), c.RequestedStartDay);

        // 3. An alert.
        if (board.FirstOrDefault(b => b.Severity == BoardSeverity.Alert) is { } alert)
            return new LeadItem(LeadKind.Alert, alert.Text, alert.LinkKind, alert.LinkId, alert.Person, null, alert.RecordId);

        // 4. The nearest clock.
        var clocks = new List<LeadItem>();
        foreach (var c in _cards.Where(x => x.SubmittedDay <= Day && x.RequestedStartDay > Day
                                            && x.RequestedStartDay <= Day + LeadClockDays && !CardCoveredNow(x)))
            clocks.Add(new LeadItem(LeadKind.Clock, $"{What(c)} is wanted from day {c.RequestedStartDay} and nothing covers it yet.",
                BoardLinkKind.Card, c.Id, BuildPerson(c.PersonId), c.RequestedStartDay));
        foreach (var f in _funding.Where(x => x.UsableOn(Day) && x.RemainingCents > 0 && x.ExpiresDay <= Day + LeadClockDays && x.ExpiresDay < LastDay))
            clocks.Add(new LeadItem(LeadKind.Clock,
                $"{LeadFundType(Scenario.FundingLines.First(x => x.Id == f.Id).Type)} money ends on day {f.ExpiresDay} with {Money(f.RemainingCents)} not obligated.",
                BoardLinkKind.Funding, f.Id, BuildPerson(Scenario.Actors.Fm.Person), f.ExpiresDay));
        return clocks.OrderBy(x => x.DueDay).FirstOrDefault();
    }
}
