using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// What people told you and the day's news reach you (DESK-73, docs/design/desk-redesign.md D29). The 17 Nov gameplay
// review found that what the player pays hours to learn never reached a screen they read: in Spoke B the J4's answer
// (day 1), both SCO answers (days 4 and 6), the 64 more people for Spoke C (day 12), the second group for Spoke B (day 14)
// and both weather closures (day 16); in the tutorial Dunn's "nobody licensed" and the forklift vendor running short (the
// morning of day 5) and a new request (day 7). The records were all written; nothing gathered them. This file gathers them:
// the day's results page gets what was said and the news, the morning gets one ordered list of what is new since
// yesterday, and each open request gets the last day an order can go in. Read only: nothing here changes state or draws a
// number, so goldens do not move.
public sealed partial class Session
{
    /// <summary>Staff meeting records that had something for the shop (the first meeting's priorities, or a request
    /// raised or mentioned). A meeting that was only ops talk is not news (D30). Derived as the game plays and rebuilt on
    /// replay; not part of the state hash.</summary>
    private readonly HashSet<string> _meetingForShop = new(StringComparer.Ordinal);

    /// <summary>Kinds the morning lists as news: what the world told the shop overnight. Starts, ends, shortfalls and
    /// finished work have their own places (the day's results, Where things stand).</summary>
    private static bool IsNews(RecordKind k) => k is RecordKind.Event or RecordKind.Withdrawal or RecordKind.VendorCapacity
        or RecordKind.Declined or RecordKind.CorReport or RecordKind.ExternalSupport or RecordKind.Inbox or RecordKind.Funding
        or RecordKind.Transition or RecordKind.UcaProposal or RecordKind.UcaOverdue or RecordKind.UcaStopWork
        or RecordKind.Compliance or RecordKind.Knowledge;

    /// <summary>The records the last day wrote overnight, in order, that the shop knows about.</summary>
    private IEnumerable<CauseRecord> Overnight()
    {
        for (var i = _overnightFrom; i < _overnightTo && i < _records.Count; i++)
            if (RecordKnown(_records[i])) yield return _records[i];
    }

    /// <summary>One answer: a visit record (an ask, a vendor meeting, the SCO, finance, the J4, a staff meeting that had
    /// something for the shop), with who said it and who on the team asked.</summary>
    private NewItem? Answer(CauseRecord r)
    {
        if (r.Kind != RecordKind.Visit) return null;
        if (r.Subject == PersonSubject(Scenario.Actors.Commander.Person, "staffMeeting") && !_meetingForShop.Contains(r.Id)) return null;
        var (lk, lid) = Link(r.Subject);
        var asker = _work.FirstOrDefault(w => r.CausedBy.Contains(w.CauseId) && w.Kind == WorkKind.Visit);
        var member = asker is null ? null : Member(asker.Assignee);
        return new NewItem(NewKind.Answer, r.Text, PersonFor(lk, lid), member is null ? null : MemberName(member), member?.Id, lk, lid, r.Id);
    }

    private NewItem News(CauseRecord r)
    {
        var (lk, lid) = Link(r.Subject);
        // A scenario event's record can be its bare title ("Harbor Equipment Co is short of forklifts this week").
        var text = r.Text.TrimEnd();
        if (text.Length > 0 && !".!?\"'".Contains(text[^1])) text += ".";
        return new NewItem(NewKind.News, text, PersonFor(lk, lid), null, null, lk, lid, r.Id);
    }

    /// <summary>The contract-writing system comes up on a set day; nothing wrote that down (the Spoke B player was told
    /// "down until about day 12" and never heard it come back). Said on the morning it is up, and on the results page of
    /// the day before. Derived, not a record, so no record id or golden moves.</summary>
    private NewItem? SystemUp(int morning)
        => _systemOnlineDay > 1 && _systemOnlineDay <= LastDay && morning == _systemOnlineDay
            ? new NewItem(NewKind.News,
                $"The contract-writing system is up from day {_systemOnlineDay}: written actions take their usual time again, and invoices are no longer manual.",
                null, null, null, BoardLinkKind.None, "system", null)
            : null;

    /// <summary>What was said overnight, and the news, for the day's results page (DESK-73). A declined call is already
    /// first under short or missed there (DESK-57), so it is not news twice.</summary>
    private (List<NewItem> Said, List<NewItem> News) SaidAndNews(bool forSummary)
    {
        var said = new List<NewItem>();
        var newsRecords = new List<CauseRecord>();
        foreach (var r in Overnight())
        {
            if (Answer(r) is { } a) said.Add(a);
            else if (IsNews(r.Kind) && !(forSummary && r.Kind == RecordKind.Declined)) newsRecords.Add(r);
        }
        // One thing, told once: when a piece of news caused another the same night (the host nation taking its forklift
        // back, then the withdrawal it brings), the one it caused says it in full and the cause is not listed again.
        var causes = newsRecords.SelectMany(r => r.CausedBy).ToHashSet(StringComparer.Ordinal);
        var news = newsRecords.Where(r => !causes.Contains(r.Id)).Select(News).ToList();
        if (SystemUp(Day) is { } up) news.Add(up);
        return (said, news);
    }

    internal (IReadOnlyList<NewItem> Said, IReadOnlyList<NewItem> News) BuildDayNews()
    {
        var (said, news) = SaidAndNews(forSummary: true);
        return (said, news);
    }

    /// <summary>
    /// What is new since yesterday, in one ordered list (DESK-73, D29): new requests (soonest wanted first), then what
    /// people answered, then news. On day 1 every request on the desk is new.
    /// </summary>
    internal IReadOnlyList<NewItem> BuildNewThisMorning()
    {
        var list = new List<NewItem>();
        foreach (var c in _cards.Where(c => c.SubmittedDay == Day || (Day == 1 && c.SubmittedDay <= 1)).OrderBy(c => c.RequestedStartDay))
        {
            var person = BuildPerson(c.PersonId);
            var who = person?.DisplayName ?? c.SubmittedBy ?? "A unit";
            list.Add(new NewItem(NewKind.Request,
                $"{who} asks for {Lower(CardTitle(c))}, wanted from day {c.RequestedStartDay} for {Plural(Math.Max(1, c.DurationDays), "day")}.",
                person, null, null, BoardLinkKind.Card, c.Id, null));
        }
        var (said, news) = SaidAndNews(forSummary: false);
        list.AddRange(said);
        list.AddRange(news);
        return list;
    }

    // ------------------------------------------------------------------ the last day to order

    /// <summary>
    /// The last day an order can go in and still start on the day a request is wanted, by the fastest route the shop
    /// knows (D29). A forecast: best rolls, today's queue, the funding and cash the shop has today. A route is one the
    /// shop could place today (the award check passes) with a vendor whose terms it knows or holds an agreement with; a
    /// letter contract is not offered as a route, since it is for when nothing else will do. Null when the request is
    /// covered or in work, its days are over, or the shop knows no route.
    /// </summary>
    internal OrderByView? OrderBy(CardState card)
    {
        var to = Math.Min(LastDay, card.RequestedStartDay + Math.Max(1, card.DurationDays) - 1);
        if (Day > to) return null;
        var from = Math.Max(card.RequestedStartDay, Day);
        var need = Math.Max(1, (PeakDemandMilli(card.SiteId, card.CapabilityId, from, to) + 999) / 1000);
        var lines = card.Lines.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        (int Lead, long Cost, string Route, string Inst, string Vendor)? best = null;
        foreach (var v in Scenario.Vendors)
        {
            if (v.Offers.FirstOrDefault(o => o.Capability == card.CapabilityId) is not { } offer) continue;
            if (!lines.All(offer.Lines.Contains)) continue;   // DESK-77: a buy that meets it only in part does not make the day
            if (VendorReason(v, card.SiteId, card.CapabilityId, lines) is not null) continue;
            foreach (var inst in new[] { "sf44", "gpc", "purchaseOrder", "bpaCall", "idiqOrder" })
            {
                var kind = inst switch { "bpaCall" => "bpa", "idiqOrder" => "idiq", _ => null };
                var vehicle = kind is null ? null
                    : _vehicles.FirstOrDefault(x => x.Kind == kind && x.VisibleOnDay1 && x.Vendor == v.Id && Day <= x.OrderingPeriodEndDay);
                if (kind is not null && vehicle is null) continue;
                if (kind is null && !VendorTermsKnown(v)) continue;
                if (PairReason(v, inst, card.SiteId, card.CapabilityId, lines) is not null) continue;
                var written = Rules.Instruments[inst].RequiresSystemOrManual;
                var line = written ? _funding.FirstOrDefault(f => f.UsableOn(Day) && f.Pays(card.CapabilityId) && f.RemainingCents > 0)?.Id : null;
                foreach (var lift in v.OffIsland ? new string?[] { "airlift", "sealift" } : new string?[] { null })
                {
                    var a = new AwardAction
                    {
                        Id = "\u0001order-by", Instrument = inst, Vendor = v.Id, Vehicle = vehicle?.Id, Card = card.Id,
                        Quantity = Math.Min(need, (long)Math.Floor(offer.CapacityPerDay)), DurationDays = to - from + 1,
                        FundingLine = line, Lift = lift, Day = Day,
                    };
                    var p = BuildPreview(a);
                    if (!p.Accepted) continue;
                    var lead = Math.Max(1, p.EarliestStartMin - Day);
                    if (best is null || lead < best.Value.Lead || (lead == best.Value.Lead && p.TotalCents < best.Value.Cost))
                        best = (lead, p.TotalCents, RouteName(inst, v.Name), inst, v.Id);
                }
            }
        }
        if (best is not { } b) return null;
        var by = card.RequestedStartDay - b.Lead;
        var takes = $"{b.Route} starts {Plural(b.Lead, "day")} after it goes in";
        var sentence = by > Day ? $"Order by day {by}: {takes}."
            : by == Day ? $"Order today to start on day {card.RequestedStartDay}: {takes}."
            : $"Too late for day {card.RequestedStartDay} by any route the shop knows: {takes}, so the soonest start is day {Day + b.Lead}.";
        return new OrderByView(by, b.Route, b.Lead, SentenceCase(sentence), b.Inst, b.Vendor);
    }

    private static string RouteName(string inst, string vendor) => inst switch
    {
        "sf44" => $"a cash buy from {vendor}",
        "gpc" => $"a card buy from {vendor}",
        "purchaseOrder" => $"a purchase order with {vendor}",
        "bpaCall" => $"a BPA call with {vendor}",
        "idiqOrder" => $"an IDIQ order with {vendor}",
        _ => $"a buy from {vendor}",
    };
}
