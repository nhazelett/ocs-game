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
        if (chk.SiteId is not { } siteId || chk.CapabilityId is not { } capId || chk.Vendor is not { } vendor) return null;
        var cap = Cap(capId);
        var site = SiteData(siteId);
        var duration = Math.Max(1, a.DurationDays);
        var start = preview.EarliestStartMin;
        var max = preview.EarliestStartMax;
        var startSaid = max == start ? $"on day {start}" : max == start + 1 ? $"on day {start} or {max}" : $"between day {start} and day {max}";

        // DESK-86: a card buy's price carries the vendor's card surcharge, which the vendor's per-unit price leaves out.
        var surcharge = a.Instrument == "gpc" && Fixed.Permille(vendor.GpcSurchargeFraction) is var sur and > 0
            ? $" with {Possessive(vendor.Name)} {PercentSaid(sur)} card surcharge" : "";
        var parts = new List<string>
        {
            $"{vendor.Name}, {Lower(cap.Label)} for {site.Name}: {Money(preview.TotalCents)}{surcharge} and {HoursOf(preview, a.Assignee)}.",
        };
        var card = chk.Card;
        // DESK-86: what the buy brings, when the capability has more than one part, and what it leaves out of the request.
        var brings = chk.Lines.Select(l => Lower(LineLabel(capId, l))).ToList();
        var without = (card?.Lines.Where(kv => kv.Value).Select(kv => kv.Key) ?? []).Where(l => !chk.Lines.Contains(l))
                                                                                 .Select(l => Lower(LineLabel(capId, l))).ToList();
        if (brings.Count > 1 || without.Count > 0)
            parts.Add($"It brings {(brings.Count > 0 ? JoinAnd(brings) : "none of it")}{(without.Count > 0 ? $", not {JoinAnd(without)}" : "")}.");
        // DESK-149 (d), Claude play 21 Nov, second play, note 12: a vendor's price is per unit whatever parts it brings, so a
        // part added from what someone said can leave the total unchanged; say so, or the player wonders whether it was free.
        var learned = card is null ? [] : chk.Lines.Where(l => card.Lines.GetValueOrDefault(l) && _learnedLines.ContainsKey((card.Id, l))).ToList();
        if (learned.Count > 0)
            parts.Add($"{Possessive(vendor.Name)} price already includes its {PartsLabels(capId, learned)}.");
        var needEnd = card is null ? (int?)null : card.RequestedStartDay + Math.Max(1, card.DurationDays) - 1;
        if (card is null) parts.Add($"It starts {startSaid}.");
        else if (start <= card.RequestedStartDay) parts.Add($"It starts {startSaid}; it is wanted from day {card.RequestedStartDay}.");
        else
        {
            parts.Add($"It starts {startSaid}; it was wanted from day {card.RequestedStartDay}.");
            if (start <= needEnd && BeforeStartCover(card, start) is { } cover) parts.Add(cover);
        }
        // D43, DESK-129: what the late end of the quoted range costs against the commander's allowance.
        var passCost = card is null ? null : PassLineCost(card, max);
        if (passCost is not null) parts.Add(passCost);
        // DESK-86: why a written buy starts when it does: finance certifies the funds before the writing can start. The
        // range's other causes stay in the quote (DESK-78); the summary is pinned above the button and must stay short.
        if (chk.Instrument is { RequiresSystemOrManual: true } && preview.TotalCents > 0 && Scenario.Actors.Fm.CertificationLagDays > 0)
            parts.Add($"Finance certifies the funds on day {Day + Scenario.Actors.Fm.CertificationLagDays}.");
        // DESK-134: a purchase order with a vendor that prices first is written once the priced proposal is in.
        if (PricesFirst(a.Instrument, vendor) is { } pricing)
            parts.Add($"It is written once {Possessive(vendor.Name)} priced proposal comes, between day {Day + pricing.Min} and day {Day + pricing.Max}.");
        // DESK-110: what it is on top of, and what of the request that thing leaves out and this buy brings.
        var inPlace = InPlace(a, siteId, capId, start, start + duration - 1).DistinctBy(x => x.Name).ToList();
        var onTop = inPlace.Select(x => x.Name).ToList();
        if (inPlace is [{ Without.Count: > 0 } only])
            parts.Add($"It is on top of {only.Name}, which comes without {PartsLabels(capId, only.Without)}.");
        else if (onTop.Count > 0)
        {
            parts.Add($"It is on top of {JoinAnd(onTop)}.");
            parts.AddRange(inPlace.Where(x => x.Without.Count > 0).Select(x => $"{Upper(x.Name)} comes without {PartsLabels(capId, x.Without)}."));
        }

        string? why = null;
        if (card is not null && start > needEnd)
            why = $"It starts after the need ends on day {needEnd}.";
        else if (card is not null && ToldNotAdded(card) is { } told)
            why = told;   // D40, DESK-119: in place of "already covered", which is beside the point while the part is missing
        else if (onTop.Count > 0 && OnlyCoveredDays(a, siteId, capId, start, Math.Min(start + duration - 1, needEnd ?? int.MaxValue)))
            why = "Every day it would run is already covered.";
        if (why is not null) parts.Add(why);
        // D42, DESK-123: the summary's last sentence, after any attention reason; never one itself.
        var answer = card is null ? null : AnswerDue(card);
        if (answer is not null) parts.Add(answer);
        return new AwardSummaryView(string.Join(" ", parts), why is not null, why, answer, passCost);
    }

    /// <summary>D43 (DESK-129, Claude play 20 Nov, third play, note 1): when the quote's latest start is after the day the
    /// request wants it and a pass line counts down days for that site and capability, the days nothing covers until then
    /// added to the days already out, against the allowance: "If it starts on day 9, material handling equipment at Hub is
    /// out 1 day in all: all the commander allows." Null otherwise. A forecast from the plan; it ignores rolls.</summary>
    private string? PassLineCost(CardState card, int latestStart)
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
        return $"If it starts on day {latestStart}, {Lower(CapLabel(card.CapabilityId))} at {SiteName(card.SiteId)} is out {Plural(total, "day")} in all: {verdict}.";
    }

    /// <summary>D42 (DESK-123, Claude play 20 Nov, second play, note 1): an ask about this request still in work, the
    /// earliest answer first: "SMSgt Ray Dunn's answer about this request comes in tonight, after you end the day; what
    /// they say may change what this buy needs." (D44, DESK-144: "on the night of day 6" for a later day.) The day is that work's forecast, as the decisions list gives it. Null when no ask is out.</summary>
    private string? AnswerDue(CardState card)
    {
        var due = _work.Where(w => w.Kind == WorkKind.Visit && w.CompletedDay is null && w.StoppedDay is null
                                   && w.Payload is VisitPayload { Extra: AskAboutAction q } && q.Card == card.Id)
                       .Select(w => (Person: ((AskAboutAction)((VisitPayload)w.Payload!).Extra!).Person,
                                     Day: PlayQueue(Member(w.Assignee), 0, Day, w, w.HoursRemainingMilli) ?? Day))
                       .OrderBy(x => x.Day).FirstOrDefault();
        if (due.Person is null || BuildPerson(due.Person) is not { } person) return null;
        return $"{Possessive(person.DisplayName)} answer about this request comes in {ComesIn(due.Day)}; what they say may change what this buy needs.";
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

    /// <summary>"3%", "2.5%".</summary>
    private static string PercentSaid(int permille) => permille % 10 == 0 ? $"{permille / 10}%" : $"{permille / 10}.{permille % 10}%";

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
