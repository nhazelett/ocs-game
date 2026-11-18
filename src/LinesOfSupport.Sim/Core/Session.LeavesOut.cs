using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// A buy in work that leaves out a part says so (DESK-103, docs/design/desk-redesign.md section 13; Claude play 18 Nov,
// note 18). On tutorial day 4 the player asked SMSgt Dunn about the forklifts and, in the same hour, called the Harbor
// Equipment BPA without drivers. Dunn's answer that night said the rental has to come with drivers. On the morning of
// day 5 Today said "0 alerts" and Where things stand said only who was writing the call. The player's one change: "raise
// it as a real alert on the Today tab". Now the request's standing names the buy and who said the part was needed, and
// the request counts in the morning's alerts until a buy brings the part, the buy is stopped, or it starts. Read only.
public sealed partial class Session
{
    /// <summary>
    /// The first buy in work, or arrangement not yet started, for this request that leaves out a part someone said the
    /// request needs, told to the shop on or after the day the buy went in, when nothing else for the request on its days
    /// brings that part: the sentence ("The BPA call MSgt Joan Avery is writing with Harbor Equipment Co comes without
    /// operators, and SMSgt Ray Dunn said the request needs them."), who said it, and the record of their answer. Null when
    /// no such buy. A part the shop knew of before the buy went in is a part bought around on purpose (D34): the request's
    /// standing names it as still to find, as before, and it raises no alert.
    /// </summary>
    internal (string Sentence, PersonView? Said, string? RecordId)? LeavesOut(CardState card)
    {
        var to = Math.Min(LastDay, card.RequestedStartDay + Math.Max(1, card.DurationDays) - 1);
        if (Day > to) return null;
        var from = card.RequestedStartDay;
        var learned = Cap(card.CapabilityId).Lines
            .Where(l => _learnedLines.ContainsKey((card.Id, l.Id)))
            .Select(l => (Line: l.Id, Info: _learnedLines[(card.Id, l.Id)])).ToList();
        if (learned.Count == 0) return null;

        // Everything for the request on its days, with what it brings and, for the ones not yet started, how to name it.
        var buys = new List<(int Created, List<string> Lines, string? Name)>();
        foreach (var a in _arrangements.Where(a => a.SiteId == card.SiteId && a.CapabilityId == card.CapabilityId && Live(a)
                                                   && a.EndDay >= Day
                                                   && (a.CardId == card.Id || (a.StartDay <= to && CountsThrough(a, card) >= from))))
            buys.Add((WentIn(a), a.Lines, a.StartDay > Day ? $"{SupplierName(a)}, starting on day {a.StartDay}," : null));
        foreach (var w in _work.Where(w => w.CompletedDay is null && w.Payload is AwardPayload && BuysFor(w, card)))
        {
            var p = (AwardPayload)w.Payload!;
            buys.Add((w.CreatedDay, p.Lines,
                $"the {InstrumentWords(p.Action.Instrument)} {WhoIs(w.Assignee)} writing with {VendorData(p.Action.Vendor)?.Name ?? "the vendor"}"));
        }

        foreach (var i in Enumerable.Range(0, buys.Count).Where(i => buys[i].Name is not null).OrderBy(i => buys[i].Created))
        {
            var (created, lines, name) = buys[i];
            var left = learned.Where(l => l.Info.Day >= created && !lines.Contains(l.Line)
                                          && !buys.Where((_, j) => j != i).Any(o => o.Lines.Contains(l.Line))).ToList();
            if (left.Count == 0) continue;
            var said = left.Select(l => l.Info.Person).Distinct().Select(BuildPerson).OfType<PersonView>().ToList();
            var them = left.Count > 1 || LineLabel(card.CapabilityId, left[0].Line).EndsWith('s') ? "them" : "it";
            var who = said.Count > 0
                ? $"{string.Join(" and ", said.Select(p => p.DisplayName))} said the request needs {them}"
                : $"you were told the request needs {them}";
            var first = left[0];
            var says = Scenario.RequirementCards.First(c => c.Id == card.Id).KnownBy[first.Line].Says;
            var record = _records.LastOrDefault(r => r.Kind == RecordKind.Visit && r.Day == first.Info.Day
                                                     && r.Subject == PersonSubject(first.Info.Person, card.Id)
                                                     && r.Text.Contains(says, StringComparison.Ordinal));
            return ($"{Upper(name!)} comes without {PartsLabels(card.CapabilityId, left.Select(l => l.Line))}, and {who}.",
                    BuildPerson(first.Info.Person), record?.Id);
        }
        return null;
    }

    /// <summary>The day the buy behind an arrangement went in: its award's action record, not the day the work finished.</summary>
    private int WentIn(Arrangement a)
        => _records.FirstOrDefault(r => r.Kind == RecordKind.Action && r.Subject == a.Id)?.Day ?? a.CreatedDay;

    /// <summary>
    /// One alert on the morning board for each request a buy leaves a part out of (DESK-103): it opens the request and
    /// counts in the morning's alerts like a shortfall. Category Request with a card link, so the day's inbox, which
    /// lists what happened overnight, leaves it out. Its record is the answer that named the part, so the item names the
    /// person the player heard it from, as every board item does.
    /// </summary>
    private IEnumerable<BoardItem> LeavesOutAlerts()
    {
        if (Finished) yield break;
        foreach (var card in _cards.Where(c => c.SubmittedDay <= Day))
            if (LeavesOut(card) is var (sentence, said, record) && record is not null)
                yield return new BoardItem(record, Math.Max(0, Day - 1), BoardCategory.Request, BoardSeverity.Alert,
                    BoardLinkKind.Card, card.Id, $"{CardTitle(card)}: {Lower(sentence)}", [record], said);
    }
}
