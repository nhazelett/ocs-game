using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// A request can be met in parts (DESK-77, docs/design/desk-redesign.md D34). The 17 Nov review's Spoke B player asked
// Capt Ruiz, learned the generators need operators and maintenance, added both, and two of the three power suppliers went
// "(unavailable)" with no reason in view; the player then bought the cheap sets through "Buy on your own initiative", which
// sold them unchecked, and the request still said nothing covered it. Now a vendor who brings every core part of a request
// stays selectable on it; the quote says what it leaves out and who said it was needed; the request stands as partly
// covered and names what is still to find; before a learned part is added the request says who could still meet all of
// it; and a buy for the request's site and capability, made anywhere, counts for the request.
public sealed partial class Session
{
    /// <summary>The parts the request has on that a buy with these lines leaves out, in the capability's order.</summary>
    internal List<string> LeftOut(CardState card, IEnumerable<string> lines)
    {
        var have = lines.ToHashSet(StringComparer.Ordinal);
        return Cap(card.CapabilityId).Lines.Where(l => card.Lines.TryGetValue(l.Id, out var on) && on && !have.Contains(l.Id))
                                          .Select(l => l.Id).ToList();
    }

    private string PartsLabels(string capId, IEnumerable<string> lines)
        => JoinWords(lines.Select(l => LineLabel(capId, l).ToLowerInvariant()).ToList());

    /// <summary>"Harbor Equipment Co brings power without operators or maintenance; Capt Ana Ruiz said they are needed."</summary>
    internal string PartsLeftOut(CardState card, string vendorName, List<string> left)
    {
        var them = left.Count == 1 ? "it is" : "they are";
        var said = left.Select(l => LineLearned(card, l)?.From?.DisplayName).OfType<string>().Distinct().ToList();
        var who = said.Count > 0 ? $"{JoinWords(said)} said {them} needed" : $"the request asks for {(left.Count == 1 ? "it" : "them")}";
        return $"{vendorName} brings {Lower(CapLabel(card.CapabilityId))} without {PartsLabels(card.CapabilityId, left)}; {who}. The request stays partly covered until {them} found.";
    }

    /// <summary>
    /// What of a request nothing arranged or in work brings yet, when something does cover it: the parts left out by
    /// every live arrangement and buy in work for its site and capability on its days. Empty when nothing covers it at
    /// all (that is "nothing arranged", not "partly") or when everything is covered.
    /// </summary>
    internal List<string> StillToFind(CardState card)
    {
        var from = card.RequestedStartDay;
        var to = Math.Min(LastDay, card.RequestedStartDay + Math.Max(1, card.DurationDays) - 1);
        var brought = new List<IEnumerable<string>>();
        foreach (var a in _arrangements.Where(a => a.SiteId == card.SiteId && a.CapabilityId == card.CapabilityId && Live(a)
                                                   && (a.CardId == card.Id || (a.StartDay <= to && a.EndDay >= from))))
            brought.Add(a.Lines);
        foreach (var w in _work.Where(w => w.CompletedDay is null && w.Payload is AwardPayload p && (p.Action.Card == card.Id
                                           || (p.SiteId == card.SiteId && p.CapabilityId == card.CapabilityId))))
            brought.Add(((AwardPayload)w.Payload!).Lines);
        if (brought.Count == 0) return [];
        return LeftOut(card, brought.SelectMany(x => x));
    }

    /// <summary>
    /// Before a learned part is added (D34): who could still meet all of the request with it. "With operators, only
    /// Meridian Site Services can meet all of it."
    /// </summary>
    internal string? IfAdded(CardState card, CapabilityLineData line)
    {
        var lines = card.Lines.Where(kv => kv.Value).Select(kv => kv.Key).Append(line.Id).ToList();
        var core = Cap(card.CapabilityId).Lines.Where(l => l.Kind == "core").Select(l => l.Id).ToList();
        var full = Scenario.Vendors
            .Where(v => v.Offers.FirstOrDefault(o => o.Capability == card.CapabilityId) is { } o && lines.All(o.Lines.Contains)
                        && VendorReason(v, card.SiteId, card.CapabilityId, core) is null)
            .Select(v => v.Name).ToList();
        var with = $"With {(line.Label ?? line.Id).ToLowerInvariant()}";
        return full.Count switch
        {
            0 => $"{with}, no supplier you could buy from brings all of it; a buy can still meet it in part.",
            1 => $"{with}, only {full[0]} can meet all of it; others can still meet it in part.",
            _ => $"{with}, {JoinWords(full)} can meet all of it.",
        };
    }
}
