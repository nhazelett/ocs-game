using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// The buy summary above Arrange support (DESK-82, docs/design/desk-redesign.md D33; Codex's Q26 for DESK-71). One
// sentence sums the buy up: the cost, the hours and whose, the day it starts against the day it was wanted and what is
// uncovered between, and what it is on top of. It needs the player's attention when the buy starts after the need ends
// or only adds to days already covered; the Sim says so, so the Game never reads warning prose to pick a colour. A
// forecast from the plan, like the quote: it ignores rolls. Read only.
public sealed partial class Session
{
    internal AwardSummaryView? BuildAwardSummary(AwardAction a)
    {
        var preview = BuildPreview(a);
        if (!preview.Accepted) return null;
        var chk = CheckAward(a);
        if (chk.SiteId is null || chk.CapabilityId is not { } capId || chk.Vendor is not { } vendor) return null;
        var cap = Cap(capId);
        var site = SiteData(chk.SiteId);
        var start = preview.EarliestStartMin;
        var max = preview.EarliestStartMax;
        var startSaid = max == start ? $"on day {start}" : max == start + 1 ? $"on day {start} or {max}" : $"between day {start} and day {max}";

        // D46 (DESK-156, Claude play 21 Nov, third play, note 22): at most three sentences, as the summary is pinned above
        // Arrange support and a long one pushed pick two out of reach. What it brings, the cover before it starts, what it
        // is on top of, and whichever of the allowance cost and the answer due did not fit are in the quote's notes
        // (BuildPreview's soft gaps, SummaryNotes), each once. D47 (DESK-158) keeps finance or the proposal in the start.
        // DESK-86: a card buy's price carries the vendor's card surcharge, which the vendor's per-unit price leaves out.
        var surcharge = a.Instrument == "gpc" && Fixed.Permille(vendor.GpcSurchargeFraction) is var sur and > 0
            ? $" with {Possessive(vendor.Name)} {PercentSaid(sur)} card surcharge" : "";
        var parts = new List<string>
        {
            $"{vendor.Name}, {WhatFor(capId, site.Name)}: {Money(preview.TotalCents)}{surcharge} and {(PerDelivery(a) ? TripHoursOf(a, chk.SiteId, StockIsland(vendor, capId)) : HoursOf(preview, a.Assignee))}.",
        };
        var card = chk.Card;
        // D47 (DESK-158, Claude play 21 Nov, fourth play, note 14): what a written buy's start waits for, in one clause, so
        // the funds wait the Now line names after committing is in view before it.
        startSaid += WaitsFor(a, chk);
        if (card is null) parts.Add($"It starts {startSaid}.");
        else if (start <= card.RequestedStartDay) parts.Add($"It starts {startSaid}; it is wanted from day {card.RequestedStartDay}.");
        else parts.Add($"It starts {startSaid}; it was wanted from day {card.RequestedStartDay}.");
        var third = SummaryThird(a, start, max);
        if (third.Said is { } said) parts.Add(said);
        return new AwardSummaryView(string.Join(" ", parts), third.Why is not null, third.Why, third.Answer, third.PassCost);
    }

    /// <summary>D46 (DESK-156): the candidates for the summary's third sentence, in order: the attention reason (D33), already
    /// covered, the allowance cost of the quote's latest start (D43), the answer due from someone asked (D42), no written
    /// terms on a service (D52). <c>Said</c> is the first
    /// of them; <c>Notes</c> the ones that did not fit, for the quote's notes. Each field keeps its value whether or not it
    /// is said. A forecast from the plan; it ignores rolls.</summary>
    private (string? Why, string? PassCost, string? Answer, string? Said, List<string> Notes) SummaryThird(AwardAction a, int start, int latestStart)
    {
        var chk = CheckAward(a);
        var card = chk.Card;
        string? why = null, passCost = null, answer = null, covered = null;
        if (chk.SiteId is { } siteId && chk.CapabilityId is { } capId)
        {
            var duration = Math.Max(1, a.DurationDays);
            var needEnd = card is null ? (int?)null : card.RequestedStartDay + Math.Max(1, card.DurationDays) - 1;
            var onTop = InPlace(a, siteId, capId, start, start + duration - 1).Count > 0;
            if (card is not null && start > needEnd)
                why = $"It starts after the need ends on day {needEnd}.";
            else if (card is not null && ToldNotAdded(card) is { } told)
                why = told;   // D40, DESK-119: in place of "already covered", which is beside the point while the part is missing
            else if (card is not null && NotYetConfirmed(card, chk) is { } unconfirmed)
                why = unconfirmed;   // D48, DESK-172: a part at risk left out while the requester has not answered
            else if (card is not null && IncludedLeftOut(card, chk) is { } included)
            {
                why = included;   // D49, DESK-175: ahead of "already covered", which moves to the quote's notes
                if (onTop && OnlyCoveredDays(a, siteId, capId, start, Math.Min(start + duration - 1, needEnd ?? int.MaxValue)))
                    covered = "Every day it would run is already covered.";
            }
            else if (onTop && OnlyCoveredDays(a, siteId, capId, start, Math.Min(start + duration - 1, needEnd ?? int.MaxValue)))
                why = "Every day it would run is already covered.";
            passCost = card is null ? null : PassLineCost(card, latestStart);
            answer = card is null ? null : AnswerDue(card, a, chk, start, latestStart);
        }
        // D52 (DESK-194; Claude play 21 Nov, eleventh play, notes P2-13, P2-20): a buy with no written terms on a service long
        // enough to be disputed says so where it is committed to, when nothing ahead of it takes the place (the answer due
        // and what waiting costs, D51, stay ahead: both are about whether to buy today). The eleventh play's second
        // player bought the forklifts on the card for thirteen days with the warning below the fold; on day 16 the forklifts
        // ran at half and the debrief called it avoidable.
        var noTerms = chk.Instrument is { } inst ? NoWrittenTerms(a, inst) : null;
        var all = new[] { why, covered, passCost, answer, noTerms }.OfType<string>().ToList();
        return (why, passCost, answer, all.FirstOrDefault(), all.Skip(1).ToList());
    }

    /// <summary>D46 (DESK-156): what the summary used to say that the quote's notes did not, for <c>BuildPreview</c>'s soft
    /// gaps: what the buy brings when it brings more than one part and leaves nothing out (a part left out has its own note),
    /// and whichever of the summary's third-sentence candidates did not fit. The cover before it starts, the priced proposal
    /// and what it is on top of already have their notes; finance's day is in the summary's start (D47).</summary>
    private IEnumerable<string> SummaryNotes(AwardAction a, AwardCheck chk, int start, int latestStart)
    {
        if (chk.CapabilityId is not { } capId || chk.Vendor is not { } vendor) yield break;
        var brings = chk.Lines.Select(l => Lower(LineLabel(capId, l))).ToList();
        var leftOut = chk.Card is { } c && LeftOut(c, chk.Lines).Count > 0;
        if (brings.Count > 1 && !leftOut) yield return $"{vendor.Name} brings {JoinAnd(brings)}.";
        // D47 (DESK-158): finance's day and the priced proposal's days are in the summary's start sentence (WaitsFor).
        foreach (var note in SummaryThird(a, start, latestStart).Notes) yield return note;
    }

    /// <summary>D47 (DESK-158): the clause after the start days saying what a written buy waits for before the vendor's
    /// notice: ", after finance certifies the funds on day 6 and the buy is written"; with a vendor that prices first
    /// (DESK-134), ", after Meridian Site Services' priced proposal comes (day 15 to 21) and the buy is written". Empty for a
    /// buy with no writing, or one that waits for neither.</summary>
    private string WaitsFor(AwardAction a, AwardCheck chk)
    {
        if (chk.Vendor is not { } vendor) return "";
        if (PricesFirst(a.Instrument, vendor) is { } pricing)
            return $", after {Possessive(vendor.Name)} priced proposal comes (day {Day + pricing.Min} to {Day + pricing.Max}) and the buy is written";
        if (chk.Instrument is { RequiresSystemOrManual: true } && chk.TotalCents > 0 && Scenario.Actors.Fm.CertificationLagDays > 0)
            return $", after finance certifies the funds on day {Day + Scenario.Actors.Fm.CertificationLagDays} and the buy is written";
        return "";
    }

    /// <summary>D43 (DESK-129, Claude play 20 Nov, third play, note 1): when the quote's latest start is after the day the
    /// request wants it and a pass line counts down days for that site and capability, the days nothing covers until then
    /// added to the days already out, against the allowance: "If it starts on day 9, material handling equipment at Hub is
    /// out 1 day in all: all the commander allows." Null otherwise. A forecast from the plan; it ignores rolls.</summary>
    private string? PassLineCost(CardState card, int latestStart)
        => DownDaysIf(card, latestStart) is { } cost ? $"If it starts on day {latestStart}, {cost}." : null;

    /// <summary>D43's count without its opening: "material handling equipment at Hub is out 1 day in all: all the commander
    /// allows". Null when the start is not past the day wanted, or no pass line counts the days.</summary>
    private string? DownDaysIf(CardState card, int latestStart)
    {
        if (latestStart <= card.RequestedStartDay) return null;
        var metric = $"site.{card.SiteId}.{card.CapabilityId}.downDays";
        if (Scenario.PassCriteria.FirstOrDefault(p => p.Metric == metric && p.Comparator == "<=") is not { } line) return null;
        if (CoverDays(card, latestStart) is not { None.Count: > 0 } days) return null;
        var used = Metrics.TryGetValue(metric, out var v) ? (long)v : 0;
        var total = used + days.None.Count;
        var allowed = (long)line.Value;
        var verdict = total == allowed ? "all the commander allows"
            : total < allowed ? $"within the {Plural(allowed, "day")} the commander allows"
            : total - allowed == 1 ? "one more than the commander allows"
            : $"{total - allowed} more than the commander allows";
        return $"{Lower(CapLabel(card.CapabilityId))} at {SiteName(card.SiteId)} is out {Plural(total, "day")} in all: {verdict}";
    }

    /// <summary>D42 (DESK-123, Claude play 20 Nov, second play, note 1): an ask about this request still in work, the
    /// earliest answer first: "SMSgt Ray Dunn's answer about this request comes in tonight, after you end the day; what
    /// they say may change what this buy needs." (D44, DESK-144: "on the night of day 6" for a later day.) The day is that work's forecast, as the decisions list gives it. Null when no ask is out.</summary>
    private string? AnswerDue(CardState card, AwardAction a, AwardCheck chk, int start, int latestStart)
    {
        var due = _work.Where(w => w.Kind == WorkKind.Visit && w.CompletedDay is null && w.StoppedDay is null
                                   && w.Payload is VisitPayload { Extra: AskAboutAction q } && q.Card == card.Id)
                       .Select(w => (Person: ((AskAboutAction)((VisitPayload)w.Payload!).Extra!).Person,
                                     Day: PlayQueue(Member(w.Assignee), 0, Day, w, w.HoursRemainingMilli) ?? Day))
                       .OrderBy(x => x.Day).FirstOrDefault();
        if (due.Person is null || BuildPerson(due.Person) is not { } person) return null;
        return $"{Possessive(person.DisplayName)} answer about this request comes in {ComesIn(due.Day)}; what they say may change what this buy needs."
               + AfterTheAnswer(card, a, chk, due.Day + 1, start, latestStart);
    }

    /// <summary>
    /// D51 (DESK-190; Claude play 21 Nov, tenth play, notes P1-4, P2-3): what waiting for the answer costs. The same buy
    /// placed the day after the answer comes, behind the same queue: " Ordered tomorrow, after the answer, it starts on day
    /// 9: material handling equipment at Hub is out 1 day in all: all the commander allows." The count against the line only
    /// when that start is past the day wanted and a pass line counts the days (D43). D53 (DESK-201): when it would start the
    /// same, " Ordered tomorrow, after the answer, it still starts on day 8."
    /// Three times a game both tenth-play players chose between ordering blind on the order-by day and waiting a night, with
    /// nothing to weigh. Left out: forecasting what the answer will be. A forecast from the plan; it ignores rolls.
    /// </summary>
    private string AfterTheAnswer(CardState card, AwardAction a, AwardCheck chk, int orderDay, int start, int latestStart)
    {
        if (chk.Instrument is not { } inst || chk.Offer is not { } offer || orderDay > LastDay) return "";
        var (min, max, _, _) = StartWindow(a, inst, offer, chk, orderDay);
        var when = orderDay == Day + 1 ? "tomorrow" : $"on day {orderDay}";
        var starts = max == min ? $"on day {min}" : max == min + 1 ? $"on day {min} or {max}" : $"between day {min} and day {max}";
        // D53 (DESK-201; Claude play 21 Nov, twelfth play, notes P1-11, P2-8): a wait that costs nothing is said too, or the
        // silence reads as "order now". Nothing is lost, so no count against the line.
        if (min == start && max == latestStart) return $" Ordered {when}, after the answer, it still starts {starts}.";
        var cost = DownDaysIf(card, max) is { } c ? $": {c}" : "";
        return $" Ordered {when}, after the answer, it starts {starts}{cost}.";
    }

    /// <summary>D40, DESK-119: the parts someone the shop asked said the request needs and the request does not include
    /// yet: "It leaves out operators, which SMSgt Ray Dunn said the request needs: add what you learned first." Null when
    /// there are none. Whatever the vendor, a buy is written for the request's parts, so it leaves these out until the
    /// player adds them.</summary>
    private string? ToldNotAdded(CardState card)
    {
        var told = Cap(card.CapabilityId).Lines
            .Where(l => !card.Lines.GetValueOrDefault(l.Id) && _learnedLines.ContainsKey((card.Id, l.Id)))
            .Select(l => l.Id).ToList();
        if (told.Count == 0) return null;
        var said = told.Select(l => _learnedLines[(card.Id, l)].Person).Distinct().Select(BuildPerson).OfType<PersonView>().ToList();
        var who = said.Count > 0 ? $"which {string.Join(" and ", said.Select(p => p.DisplayName))} said" : "which you were told";
        return $"It leaves out {PartsLabels(card.CapabilityId, told)}, {who} the request needs: add what you learned first.";
    }

    /// <summary>D48 (DESK-172, Claude play 21 Nov, sixth play, notes 23, 24): the operator and maintenance parts at risk
    /// (DESK-59) that this buy leaves out and nobody has told the shop about, while the person the request came from has not
    /// answered: "It leaves out operators; SMSgt Ray Dunn has not been asked." or, with that ask in work, "It leaves out
    /// operators; MSgt Joan Avery is asking SMSgt Ray Dunn, answer due day 6." The same person and parts the quote's ask-first
    /// warning names (AskFirst, DESK-58), so it marks no ask as the one that pays off. Null once they have answered: then
    /// D40 says what they told, or nothing does. A buy is written for the request's parts, so a part nobody listed is left
    /// out whatever the vendor offers.</summary>
    private string? NotYetConfirmed(CardState card, AwardCheck chk)
    {
        if (chk.CapabilityId is not { } capId || chk.Vendor is not { } vendor) return null;
        if (BuildPerson(card.PersonId) is not { } p) return null;
        if (_askedAbout.TryGetValue(card.Id, out var asked) && asked.Contains(p.Id)) return null;
        var cap = Cap(capId);
        var left = cap.Lines.Where(l => l.Kind is "operator" or "maintenance" && !chk.Lines.Contains(l.Id)
                                        && !LineKnown(card, l.Id) && PlantPartAtRisk(cap, l, vendor.Id))
                            .Select(l => l.Id).ToList();
        if (left.Count == 0) return null;
        var parts = PartsLabels(capId, left);
        if (VisitWork("ask", AskTarget(new AskAboutAction { Card = card.Id, Person = p.Id })) is not { } w)
            return $"It leaves out {parts}; {p.DisplayName} has not been asked.";
        var asker = Member(w.Assignee);
        var day = PlayQueue(asker, 0, Day, w, w.HoursRemainingMilli) ?? Day;
        var who = asker.Role == "you" ? "you are" : $"{MemberName(asker)} is";
        return $"It leaves out {parts}; {who} asking {p.DisplayName}, answer due day {day}.";
    }

    /// <summary>D49 (DESK-175, Claude play 21 Nov, seventh play, note P2-14): the operator and maintenance parts at risk
    /// (DESK-59) the request includes and this buy does not bring, because the vendor cannot: "Harbor Equipment Co does not
    /// bring operators, which the request includes; without them it fails more often every day it runs." Null when there are
    /// none, or when the vendor's offer leaves the plant out of goods (no risk to name).</summary>
    private string? IncludedLeftOut(CardState card, AwardCheck chk)
    {
        if (chk.CapabilityId is not { } capId || chk.Vendor is not { } vendor) return null;
        var cap = Cap(capId);
        var left = LeftOut(card, chk.Lines)
            .Where(id => cap.Lines.FirstOrDefault(l => l.Id == id) is { } l && PlantPartAtRisk(cap, l, vendor.Id)).ToList();
        if (left.Count == 0) return null;
        var parts = PartsLabels(capId, left);
        var them = left.Count > 1 || parts.EndsWith('s') ? "them" : parts;
        return $"{vendor.Name} does not bring {parts}, which the request includes; without {them} it fails more often every day it runs.";
    }

    /// <summary>"3%", "2.5%".</summary>
    private static string PercentSaid(int permille) => permille % 10 == 0 ? $"{permille / 10}%" : $"{permille / 10}.{permille % 10}%";

    /// <summary>DESK-189 (Claude play 21 Nov, tenth play, note P1-10): what a buy is and where it goes, "water for Hub". The
    /// office's own support leaves out the site, which it is bought at but does not serve: "vehicle for the contracting
    /// office", as the plan says it (DESK-185).</summary>
    private string WhatFor(string capId, string siteName)
        => ForShop(capId) ? ShopSupport(capId) is var said && said.StartsWith("the ", StringComparison.Ordinal) ? said[4..] : said
                          : $"{Lower(Cap(capId).Label)} for {siteName}";

    /// <summary>DESK-183 (Claude play 21 Nov, ninth play, note P1-26): an SF 44 paid at each delivery takes no writing, but
    /// every delivery is a trip for the buyer and the paying agent (DESK-09), so its quote says the trip's hours, as the
    /// buy's record and the day loop charge them: "2 shop hours of your time on each delivery day". A one-day SF 44 is paid
    /// on the spot with no trip and keeps "no shop hours". A forecast from today's trip; a shop vehicle bought later cuts it.</summary>
    private string TripHoursOf(AwardAction a, string siteId, string stockIsland)
    {
        var m = Member(a.Assignee);
        var whose = m.Role == "you" ? "your" : MemberName(m) + "'s";
        var h = HalfHours(PersonHours(TripMilli(siteId, stockIsland, Day), m.Id));
        return $"{(h == 1 ? "1 shop hour" : $"{HoursText(h)} shop hours")} of {whose} time on each delivery day";
    }

    /// <summary>"no shop hours", "6 hours of your time", "6 to 9 hours of MSgt Joan Avery's time".</summary>
    private string HoursOf(ActionPreview p, string? assignee)
    {
        if (p.ShopHoursMax == 0) return "no shop hours";
        var m = Member(assignee);
        var whose = m.Role == "you" ? "your" : MemberName(m) + "'s";
        // Q28 (DESK-142): the same half hours as the picker and the line under the button; whole hours where the preview
        // has only those.
        var (lo, hi) = p.HoursMaxMilli >= 0 ? (HalfHours(p.HoursMinMilli), HalfHours(p.HoursMaxMilli)) : (p.ShopHoursMin, (double)p.ShopHoursMax);
        var hours = lo == hi
            ? (lo == 1 ? "1 hour" : $"{HoursText(lo)} hours")
            : $"{HoursText(lo)} to {HoursText(hi)} hours";
        return $"{hours} of {whose} time";
    }

    /// <summary>True when every day from <paramref name="from"/> to <paramref name="to"/> that has demand at the site is
    /// already met in full by live arrangements and buys being written, so this buy would only add to covered days. Days
    /// with no demand do not count; with none left, the buy covers nothing anyone needs, which the start-after-need rule
    /// already says for a request. Something that leaves out a part this buy brings and the request needs covers none of
    /// it (DESK-110): the purchase order with drivers is not a double buy of the call without them.</summary>
    private bool OnlyCoveredDays(AwardAction award, string siteId, string capId, int from, int to)
    {
        var site = SiteData(siteId);
        var card = CardOf(award);   // DESK-102
        var needed = NeededParts(award, card);
        var any = false;
        for (var g = Math.Max(from, Math.Max(Day, 1)); g <= Math.Min(to, LastDay); g++)
        {
            var demand = DemandMilli(site, capId, g);
            if (demand <= 0) continue;
            any = true;
            var supply = _arrangements.Where(x => x.Id != award.Id && x.SiteId == siteId && Live(x) && x.StartDay <= g && CountsThrough(x, card) >= g
                                                  && (x.CapabilityId != capId || needed.All(x.Lines.Contains)))
                                      .Sum(x => (x.CapabilityId == capId ? x.QuantityMilli : 0) + x.SuppliedQuantityMilli.GetValueOrDefault(capId));
            supply += _work.Where(w => w.CompletedDay is null).Select(w => w.Payload).OfType<AwardPayload>()
                           .Where(p => p.Action.Id != award.Id && p.SiteId == siteId && p.CapabilityId == capId && needed.All(p.Lines.Contains)
                                       && p.Action.StartDay is { } sd
                                       && sd <= g && sd + Math.Max(1, p.Action.DurationDays) - 1 >= g)
                           .Sum(p => p.Action.Quantity * 1000);
            if (supply < demand) return false;
        }
        return any;
    }
}
