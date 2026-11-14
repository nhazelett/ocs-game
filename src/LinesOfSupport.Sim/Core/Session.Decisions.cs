using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Your decisions, in one list (DESK-41, docs/design/desk-redesign.md D21). Nick on his 13 Nov replay: "I don't know what
// decisions I've made easily." Every action the office committed to, newest first: the day, what it was, who it went to,
// the request it was for, and how it stands now. Only what was accepted; what the player is still composing never is, and
// a refused action is not a decision. Read only.
public sealed partial class Session
{
    /// <summary>For each applied action, the record that said what it was (null when refused or when it made none).</summary>
    private readonly List<string?> _appliedRecord = [];

    internal IReadOnlyList<DecisionView> BuildDecisions(string? cardId)
    {
        var list = new List<DecisionView>();
        for (var i = _applied.Count - 1; i >= 0; i--)
        {
            if (_appliedRejected[i] || _appliedRecord[i] is not { } recId) continue;
            var rec = _records.First(r => r.Id == recId);
            var action = _applied[i];
            var work = _work.FirstOrDefault(w => w.CauseId == recId);
            var arr = ArrangementFor(action, rec);
            var card = CardFor(action, arr, work);
            if (cardId is not null && card != cardId) continue;
            var who = action switch
            {
                AssignAction h => h.To,
                _ => work?.Assignee ?? arr?.TripAssignee ?? (_team.Any(m => m.Id == action.Assignee) ? action.Assignee : _lead),
            };
            var name = MemberName(Member(who));
            var what = DecisionWhat(action, rec, work);
            var givenTo = work is not null || action is AssignAction ? $", given to {name}" : "";
            list.Add(new DecisionView(rec.Day, SentenceCase($"Day {rec.Day}: {Upper(what)}{givenTo}."),
                Member(who).Id, name, card, card is null ? null : CardTitle(_cards.First(c => c.Id == card)),
                SentenceCase(DecisionNow(action, work, arr))));
        }
        return list;
    }

    private Arrangement? ArrangementFor(PlayerAction action, CauseRecord rec)
    {
        var id = action switch
        {
            AwardAction a => a.Id,
            HostNationRequestAction h => h.Id,
            AcsaOrderAction o => o.Id,
            RequestExternalSupportAction r => r.Id,
            CoordinateAction c => c.Id,
            EngageJ4Action j => j.Id,
            DefinitizeAction d => d.Arrangement,
            TerminateAction t => t.Arrangement,
            _ => null,
        };
        return id is null ? null : _arrangements.FirstOrDefault(a => a.Id == id);
    }

    private string? CardFor(PlayerAction action, Arrangement? arr, WorkItem? work)
    {
        var card = action switch
        {
            AwardAction a => a.Card,
            AskAboutAction q => q.Card,
            AddLineAction l => l.Card,
            SetLineAction l => l.Card,
            _ => null,
        } ?? arr?.CardId;
        if (card is null && work?.Payload is HostNationPayload or AcsaPayload or VisitPayload { Kind: "j4" })
        {
            var (site, cap) = work.Payload switch
            {
                HostNationPayload p => (p.SiteId, p.CapabilityId),
                AcsaPayload p => (p.SiteId, p.CapabilityId),
                VisitPayload { Extra: EngageJ4Action j } => (j.Site, j.Capability),
                _ => ("", ""),
            };
            card = _cards.Where(c => c.SiteId == site && c.CapabilityId == cap).OrderBy(c => c.SubmittedDay).LastOrDefault()?.Id;
        }
        return card is not null && _cards.Any(c => c.Id == card) ? card : null;
    }

    /// <summary>What was decided, short: "water at Hub from Harbor Bottling, 4500 gal/day for 7 days on a purchase order".
    /// Sentences of the Sim's own keep their capitals (names); the ones made here start lower case.</summary>
    private string DecisionWhat(PlayerAction action, CauseRecord rec, WorkItem? work)
    {
        (string? Site, string? Cap) Where(string? card, string? site, string? cap)
        {
            var c = card is null ? null : _cards.FirstOrDefault(x => x.Id == card);
            return (site ?? c?.SiteId, cap ?? c?.CapabilityId);
        }
        string For(string site, string cap, long quantity, int days)
            => $"{Lower(CapLabel(cap))} at {SiteName(site)}, {quantity} {Rate(Cap(cap))} for {Plural(days, "day")}";
        switch (action)
        {
            case AwardAction a when Where(a.Card, a.Site, a.Capability) is ({ } site, { } cap) && VendorData(a.Vendor) is { } v:
                var inst = InstrumentWords(a.Instrument);
                return $"{Lower(CapLabel(cap))} at {SiteName(site)} from {v.Name}, {a.Quantity} {Rate(Cap(cap))} for {Plural(a.DurationDays, "day")} on {Article(inst)} {inst}";
            case HostNationRequestAction h when Where(h.Card, h.Site, h.Capability) is ({ } site, { } cap):
                return $"asked {HostNationName} for {For(site, cap, h.Quantity, h.DurationDays)}";
            case AcsaOrderAction o when Where(o.Card, o.Site, o.Capability) is ({ } site, { } cap):
                return $"ordered from {PartnerName} under the ACSA: {For(site, cap, o.Quantity, o.DurationDays)}";
            case RequestExternalSupportAction r when Where(r.Card, r.Site, r.Capability) is ({ } site, { } cap):
                return $"asked {ExternalProvider} for {For(site, cap, r.Quantity, r.DurationDays)}";
            case PlanTransitionAction t:
                return $"planned the hand-over from {ArrName(t.Outgoing)} to {ArrName(t.Incoming)}";
            case ReviewFilesAction:
                return "ordered a review of the inherited contract files";
            case AssignAction h when _work.FirstOrDefault(w => w.Id == h.Work) is { } w:
                return $"handed over {LowerLabel(WorkName(w))}";
        }
        if (work is not null && work.Kind == WorkKind.Visit) return WorkName(work);
        var text = rec.Text;
        var stop = text.IndexOf(". ", StringComparison.Ordinal);
        return (stop > 0 ? text[..stop] : text).TrimEnd('.');
    }

    /// <summary>"a" or "an" by the sound: "an SF 44 cash purchase", "an IDIQ order", "a purchase order".</summary>
    private static string Article(string words)
        => words.Length > 0 && ("aeiouAEIOU".Contains(words[0]) || words.StartsWith("SF ", StringComparison.Ordinal)) ? "an" : "a";

    /// <summary>How it stands now: the work in progress (who, done about when), then what it put in place.</summary>
    private string DecisionNow(PlayerAction action, WorkItem? work, Arrangement? arr)
    {
        if (action is AssignAction h) work = _work.FirstOrDefault(w => w.Id == h.Work);
        if (work is not null && work.CompletedDay is null)
        {
            var done = PlayQueue(Member(work.Assignee), 0, Day, work, work.HoursRemainingMilli);
            return work.WaitUntilDay > Day
                ? $"Waiting until day {work.WaitUntilDay}, then about {HoursSaid(work.HoursRemainingMilli)} of work: done about day {done}."
                : $"In work: done about day {done}.";
        }
        if (arr is not null)
            return arr.Status switch
            {
                ArrangementStatus.Declined => "The vendor turned it down.",
                ArrangementStatus.Pending => $"Arranged: starts on day {arr.StartDay}.",
                ArrangementStatus.Suspended => "On hold.",
                ArrangementStatus.Active when CapState(arr.SiteId, arr.CapabilityId) is { TodayPermille: < 1000 } cs
                    => $"Running{Through(arr.EndDay)}; {Lower(CapLabel(arr.CapabilityId))} at {SiteName(arr.SiteId)} was at {cs.TodayPermille / 10}% yesterday.",
                ArrangementStatus.Active => $"Running{Through(arr.EndDay)}.",
                _ => $"Ended after day {arr.EndDay}.",
            };
        return work?.CompletedDay is { } d ? $"Done on day {d}." : "Done.";
    }
}
