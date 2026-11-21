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
        var text = (CoveredWithdrawal(r) ?? r.Text).TrimEnd();   // DESK-198
        if (text.Length > 0 && !".!?\"'".Contains(text[^1])) text += ".";
        return new NewItem(NewKind.News, text, PersonFor(lk, lid), null, null, lk, lid, r.Id);
    }

    /// <summary>
    /// DESK-198 (Claude play 21 Nov, eleventh play, note P1-16). A withdrawal whose site and capability already have support
    /// arranged from the day after its last day reads as news, not an alert, and names what takes over: "The host nation
    /// withdrew inherited host nation support for waste removal at Hub; last day of support 12; the purchase order from Main
    /// Island Sanitation takes over on day 13. ..." The eleventh play's first player had ordered that purchase order on day 7
    /// and still met "1 alert" on day 9, which "reads as if I still needed to act". Said as DESK-188 says a running line's
    /// follow-on. The record is unchanged; a withdrawal with nothing behind it stays an alert. Null when not covered.
    /// </summary>
    private string? CoveredWithdrawal(CauseRecord r)
    {
        if (r.Kind != RecordKind.Withdrawal || _arrangements.FirstOrDefault(a => a.Id == r.Subject) is not { } a) return null;
        var last = $"last day of support {a.EndDay}";
        if (!r.Text.Contains(last + ".", StringComparison.Ordinal)) return null;
        var next = _arrangements.Where(b => b.Id != a.Id && b.SiteId == a.SiteId && b.CapabilityId == a.CapabilityId && Live(b)
                                            && b.StartDay <= a.EndDay + 1 && b.EndDay > a.EndDay)
                                .OrderBy(b => b.StartDay).FirstOrDefault();
        if (next is null) return null;
        return r.Text.Replace(last + ".", $"{last}; {Called(next)} takes over on day {Math.Max(next.StartDay, a.EndDay + 1)}.", StringComparison.Ordinal);
    }

    /// <summary>
    /// DESK-170 (Claude play 21 Nov, sixth play, note 34): the map said "Hub · Fuel: supplier failure reported" on the
    /// morning of day 16 and Today said nothing new. A host nation or partner supplier that missed a delivery, or had
    /// equipment down, on a day the site was not short for it is news, in one sentence that says whose it is: "Main Island
    /// Utilities, the host nation, missed a fuel delivery at Hub on day 15; the stock on hand covered it, and the host
    /// nation puts it right." Not an alert: there is nothing for the office to do. A failure that left the site short is
    /// already an alert with its reason; the office's own vendors' failures reach it through the COR or the unit.
    /// </summary>
    private NewItem? TheirsCovered(CauseRecord r)
    {
        if (r.Kind != RecordKind.VendorFailure) return null;
        if (_arrangements.FirstOrDefault(a => a.Id == r.Subject) is not { } a) return null;
        if (a.Instrument != HostNationInstrument && a.Instrument != AcsaInstrument) return null;
        var cs = _caps.FirstOrDefault(c => c.SiteId == a.SiteId && c.CapabilityId == a.CapabilityId);
        if (cs is null || r.Day < 1 || r.Day > DaysPlayed || Band(cs)[r.Day - 1] is >= 0 and < 1000) return null;
        var cap = Cap(a.CapabilityId);
        var hostNation = a.Instrument == HostNationInstrument;
        var name = SupplierName(a);
        if (name.IndexOf(" (", StringComparison.Ordinal) is var paren and > 0) name = name[..paren];   // "Main Island Utilities (host nation)"
        var who = hostNation ? $"{name}, the host nation," : $"{name}, under the ACSA,";
        var what = cap.Delivery == "consumable" ? $"missed a {Lower(cap.Label)} delivery" : $"had {Lower(cap.Label)} equipment down";
        var covered = cap.Delivery == "consumable" ? "the stock on hand covered it" : $"{SiteName(a.SiteId)} was not short for it";
        var owner = hostNation ? "the host nation puts it right" : "the J4 takes it up with them";
        var (lk, lid) = Link(r.Subject);
        return new NewItem(NewKind.News, $"{who} {what} at {SiteName(a.SiteId)} on day {r.Day}; {covered}, and {owner}.",
                           PersonFor(lk, lid), null, null, lk, lid, r.Id);
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
        news.AddRange(Overnight().Select(TheirsCovered).OfType<NewItem>());   // DESK-170
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
    /// The last day an order can go in and still start on the day a request is wanted (D29), one the player can trust
    /// (D35): the day is set by the slow roll, and the sentence gives the window. Among the routes that make the wanted
    /// day on the slow roll it names one whose quote carries no warning of missing written terms, then the fastest; when
    /// it names a route with no written terms on a service long enough to suffer for it, it says so. When no route is
    /// sure of the day, it names the one that starts soonest on the slow roll and says what covers the days before it
    /// (DESK-60). A forecast from today's queue, funding and cash. A route is one the shop could place today (the award
    /// check passes) with a vendor whose terms it knows or holds an agreement with; a letter contract is not offered as a
    /// route, since it is for when nothing else will do. Null when the request is covered or in work, its days are over,
    /// or the shop knows no route.
    /// </summary>
    internal OrderByView? OrderBy(CardState card)
    {
        var to = Math.Min(LastDay, card.RequestedStartDay + Math.Max(1, card.DurationDays) - 1);
        if (Day > to) return null;
        var from = Math.Max(card.RequestedStartDay, Day);
        var need = Math.Max(1, (PeakDemandMilli(card.SiteId, card.CapabilityId, from, to) + 999) / 1000);
        // D39, DESK-111: a part someone the shop asked said is needed counts before the player adds it.
        var lines = card.Lines.Where(kv => kv.Value).Select(kv => kv.Key)
            .Union(_learnedLines.Keys.Where(k => k.Card == card.Id).Select(k => k.Line), StringComparer.Ordinal).ToList();
        var routes = new List<(int Lead, int LeadMax, long Cost, string Route, string Inst, string Vendor, bool NoTerms)>();
        foreach (var v in Scenario.Vendors)
        {
            if (v.Offers.FirstOrDefault(o => o.Capability == card.CapabilityId) is not { } offer) continue;
            if (!lines.All(offer.Lines.Contains)) continue;   // DESK-77: a buy that meets it only in part does not make the day
            if (VendorReason(v, card.SiteId, card.CapabilityId, lines) is not null) continue;
            if (HeardDeclining(v.Id)) continue;   // D39: the news says they are short; the line does not send the player there
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
                var noTerms = !Rules.Instruments[inst].WrittenTerms && to - from + 1 >= Rules.Model.ExecutionConfusionMinDurationDays;
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
                    routes.Add((lead, Math.Max(lead, p.EarliestStartMax - Day), p.TotalCents, RouteName(inst, v.Name), inst, v.Id, noTerms));
                }
            }
        }
        if (routes.Count == 0) return null;
        var want = card.RequestedStartDay;
        var sure = routes.Where(r => Day + r.LeadMax <= want).OrderBy(r => r.LeadMax).ThenBy(r => r.Cost).ToList();
        var b = sure.Where(r => !r.NoTerms).Concat(sure).Concat(
            routes.OrderBy(r => r.LeadMax).ThenBy(r => r.NoTerms).ThenBy(r => r.Lead).ThenBy(r => r.Cost)).First();
        var by = want - b.LeadMax;
        var terms = b.NoTerms ? ", with no written terms" : "";
        var takes = $"{b.Route} starts {LeadSaid(b.Lead, b.LeadMax)} after it goes in{terms}";
        var (first, last) = (Day + b.Lead, Day + b.LeadMax);
        var sentence = by > Day ? $"Order by day {by}: {takes}."
            : by == Day ? $"Order today to start by day {want}: {takes}."
            : first <= want ? $"Order today: {b.Route} starts {StartSaid(first, last)}{terms}; it is wanted from day {want}."
            : $"Too late for day {want} by any route the shop knows: {b.Route} starts {StartSaid(first, last)}{terms}.";
        if (by < Day && BeforeStartCover(card, last) is { } cover) sentence += " " + cover;
        return new OrderByView(by, b.Route, b.Lead, SentenceCase(sentence), b.Inst, b.Vendor, b.LeadMax);
    }

    /// <summary>"1 day", "1 or 2 days", "2 to 4 days".</summary>
    private static string LeadSaid(int lead, int leadMax)
        => leadMax <= lead ? Plural(lead, "day") : leadMax == lead + 1 ? $"{lead} or {leadMax} days" : $"{lead} to {leadMax} days";

    /// <summary>"on day 3", "on day 2 or 3", "between day 12 and day 14".</summary>
    private static string StartSaid(int first, int last)
        => last <= first ? $"on day {first}" : last == first + 1 ? $"on day {first} or {last}" : $"between day {first} and day {last}";

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
