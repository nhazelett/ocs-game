using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Where things stand (DESK-31, docs/design/desk-redesign.md D16). Nick at the DESK gate: "I tend to also get lost in
// the sauce about what's going on", and, asked whether he had already bought the water, "I don't remember." One line
// per request on the desk: who asked, where it is, who on the team is on it. Read only: it changes nothing and draws
// nothing, so asking for it never changes a game.
public sealed partial class Session
{
    internal IReadOnlyList<RequestStanding> BuildWhereThingsStand()
    {
        var list = new List<(int Order, int Wanted, RequestStanding Entry)>();
        foreach (var card in _cards.Where(c => c.SubmittedDay <= Day))
        {
            var entry = StandingOf(card);
            list.Add((StandingOrder(entry.State, card.RequestedStartDay <= Day), card.RequestedStartDay, entry));
        }
        return [.. list.OrderBy(x => x.Order).ThenBy(x => x.Wanted).Select(x => x.Entry)];
    }

    /// <summary>Needing attention first: late with nothing, running short, nothing yet, then in hand, then done.</summary>
    private static int StandingOrder(string state, bool wantedAlready) => state switch
    {
        RequestStanding.Nothing when wantedAlready => 0,
        RequestStanding.Short => 1,
        RequestStanding.Nothing => 2,
        RequestStanding.Asking => 3,
        RequestStanding.Buying => 4,
        RequestStanding.Arranged => 5,
        RequestStanding.Running => 6,
        _ => 7,
    };

    private RequestStanding StandingOf(CardState card)
    {
        var title = CardTitle(card);
        var asker = BuildPerson(card.PersonId);
        var from = card.RequestedStartDay;
        var to = Math.Min(LastDay, card.RequestedStartDay + Math.Max(1, card.DurationDays) - 1);
        var cap = Cap(card.CapabilityId);
        bool Overlaps(int s, int e) => s <= to && e >= from;
        RequestStanding Entry(string state, string sentence, string? who = null)
            => new(card.Id, title, asker, state, who, who is null ? null : MemberName(Member(who)), SentenceCase(sentence));

        // What covers it: support tied to the request, or any support for the same thing at the same site on its days.
        var covering = _arrangements
            .Where(a => a.SiteId == card.SiteId && a.CapabilityId == card.CapabilityId && a.Status != ArrangementStatus.Declined
                        && (a.CardId == card.Id || Overlaps(a.StartDay, a.EndDay)))
            .OrderBy(a => a.StartDay).ToList();
        var live = covering.Where(Live).ToList();

        var running = live.Where(a => a.StartDay <= Day && a.EndDay >= Day).ToList();
        if (running.Count > 0)
        {
            var a = running[0];
            var cs = CapState(card.SiteId, card.CapabilityId);
            var yesterday = cs is { History.Count: > 0 } ? cs.History[^1] : 1000;
            var trips = Trips(a);
            if (a.Status == ArrangementStatus.Suspended)
                return Entry(RequestStanding.Short, $"{SupplierName(a)} is on hold; it runs through day {a.EndDay}{More(running.Count)}.{trips}", a.TripAssignee);
            if (yesterday < 1000)
                return Entry(RequestStanding.Short,
                    $"{SupplierName(a)} is meant to deliver{Amount(a, cap)} through day {a.EndDay}{More(running.Count)}, but {Lower(cap.Label)} at {SiteName(card.SiteId)} was at {yesterday / 10}% yesterday.{trips}", a.TripAssignee);
            return Entry(RequestStanding.Running, $"{SupplierName(a)} is delivering{Amount(a, cap)} through day {a.EndDay}{More(running.Count)}.{trips}", a.TripAssignee);
        }
        var coming = live.Where(a => a.StartDay > Day).ToList();
        if (coming.Count > 0)
        {
            var a = coming[0];
            return Entry(RequestStanding.Arranged,
                $"{SupplierName(a)} starts on day {a.StartDay}:{Amount(a, cap)} through day {a.EndDay}{More(coming.Count)}{(a.StartDay > from ? $". It was wanted from day {from}" : "")}.{Trips(a)}", a.TripAssignee);
        }

        var buying = _work.Where(w => w.CompletedDay is null && BuysFor(w, card)).OrderBy(w => w.CreatedDay).FirstOrDefault();
        if (buying is not null)
        {
            var hours = Plural(CeilHours(buying.HoursRemainingMilli), "hour");
            return Entry(RequestStanding.Buying, buying.WaitUntilDay > Day
                ? $"{Upper(WhoIs(buying.Assignee))} {BuyingWords(buying)}. It waits for finance to certify the funds on day {buying.WaitUntilDay}, then about {hours} of work."
                : $"{Upper(WhoIs(buying.Assignee))} {BuyingWords(buying)}, about {hours} of work left.", buying.Assignee);
        }

        var stopped = covering.OrderBy(a => a.EndDay).LastOrDefault();
        if (stopped is not null && Day > to)
            return Entry(RequestStanding.Ended, $"Done: {SupplierName(stopped)} delivered through day {stopped.EndDay}.");
        var stoppedNote = stopped is not null ? $" {SupplierName(stopped)} stopped after day {stopped.EndDay}; it is wanted through day {to}." : "";

        var asking = _work.Where(w => w.CompletedDay is null && w.Payload is VisitPayload { Kind: "ask" } v
                                      && (v.Target?.StartsWith(card.Id + "|", StringComparison.Ordinal) ?? false))
                          .OrderBy(w => w.CreatedDay).FirstOrDefault();
        if (asking is not null)
        {
            var person = BuildPerson(((VisitPayload)asking.Payload!).Target![(card.Id.Length + 1)..]);
            return Entry(RequestStanding.Asking,
                $"{Upper(WhoIs(asking.Assignee))} asking {person?.DisplayName ?? "someone"} about it; nothing is bought yet.", asking.Assignee);
        }

        var declined = _arrangements.Where(a => a.CardId == card.Id && a.Status == ArrangementStatus.Declined).OrderBy(a => a.CreatedDay).LastOrDefault();
        var why = (declined is not null ? $" {SupplierName(declined)} turned down the call on day {declined.CreatedDay}." : "") + stoppedNote;
        var wanted = Plural(card.DurationDays, "day");
        return Entry(RequestStanding.Nothing, from <= Day
            ? $"Nothing is arranged, and it was wanted from day {from}.{why}"
            : $"Nothing is arranged yet. {asker?.DisplayName ?? "They"} {(asker is null ? "want" : "wants")} it from day {from} for {wanted}.{why}");
    }

    /// <summary>Unfinished work that will put support in place for this request's capability at its site.</summary>
    private bool BuysFor(WorkItem w, CardState card) => w.Payload switch
    {
        AwardPayload p => p.Action.Card == card.Id || (p.SiteId == card.SiteId && p.CapabilityId == card.CapabilityId),
        AcsaPayload p => p.SiteId == card.SiteId && p.CapabilityId == card.CapabilityId,
        HostNationPayload p => p.SiteId == card.SiteId && p.CapabilityId == card.CapabilityId,
        VisitPayload { Kind: "j4", Extra: EngageJ4Action j } => j.Site == card.SiteId && j.Capability == card.CapabilityId,
        _ => false,
    };

    private string BuyingWords(WorkItem w) => w.Payload switch
    {
        AwardPayload p => $"writing the {LowerLabel(Rules.Instruments[p.Action.Instrument].Label)} with {VendorData(p.Action.Vendor)?.Name ?? "the vendor"}",
        AcsaPayload => $"ordering it from {PartnerName}",
        HostNationPayload => $"asking {HostNationName} for it",
        _ => "asking the J4 to add it to the fuel order",
    };

    private static string Amount(Arrangement a, Data.CapabilityData cap)
        => a.QuantityMilli > 0 ? $" {a.QuantityMilli / 1000} {Rate(cap)}" : "";

    private static string More(int count) => count switch { <= 1 => "", 2 => " (and one other arrangement)", _ => $" (and {count - 1} others)" };

    /// <summary>An SF 44 paid at each delivery says whose trips they are (DESK-09).</summary>
    private string Trips(Arrangement a) => a.PaidPerDelivery && a.TripAssignee is { } who && a.EndDay >= Day
        ? $" Each delivery is a trip for {MemberName(Member(who))}." : "";
}
