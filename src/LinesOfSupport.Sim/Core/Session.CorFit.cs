using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// COR fit (DESK-07, docs/design/desk-redesign.md D8). A COR from the unit that does the work catches more: odds drop by
// cor.offFitPermille when the contract's work is not the COR's unit's, and by cor.perOtherContractPermille for each other
// running contract the COR already watches, never below cor.floorPermille. Only for a COR whose scenario entry lists
// what its unit knows (cors[].knows); others keep the quality's odds, so older scenarios play as before. Report lag stays
// the quality's.
public sealed partial class Session
{
    /// <summary>Other running contracts this COR watches, not counting the one in question.</summary>
    internal int CorWatching(CorState c, string? excludingArrangementId)
        => _arrangements.Count(a => a.CorId == c.Id && a.Id != excludingArrangementId
                                    && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended);

    internal bool? CorFits(CorState c, string capId) => c.Knows is null ? null : c.Knows.Contains(capId);

    /// <summary>The odds, in thousandths, that this COR reports a failure on a contract for this capability.</summary>
    internal int CorReportPermille(CorState c, string capId, string? arrangementId) => CorReportPermille(c, capId, arrangementId, 0);

    /// <summary>The same, with <paramref name="more"/> contracts added to the COR's load (DESK-112: what naming him on
    /// one more would do to a contract he already watches).</summary>
    private int CorReportPermille(CorState c, string capId, string? arrangementId, int more)
    {
        var q = CorQuality(c);
        if (c.Knows is null) return q.ReportPermille;
        var pm = q.ReportPermille
                 - (c.Knows.Contains(capId) ? 0 : Rules.Cor.OffFitPermille)
                 - (CorWatching(c, arrangementId) + more) * Rules.Cor.PerOtherContractPermille;
        return Math.Clamp(pm, Math.Min(Rules.Cor.FloorPermille, q.ReportPermille), 1000);
    }

    /// <summary>
    /// DESK-112 (Claude play 19 Nov, note 22): in the award editor, naming a COR who already watches running contracts
    /// lowers his odds on those too (D8). One sentence when it does: the contract named when there is one, counted when
    /// there are more. Null when nothing he watches would drop.
    /// </summary>
    private string? CorLoadNote(CorState c)
    {
        if (c.Knows is null) return null;
        var watched = _arrangements.Where(a => a.CorId == c.Id
                                               && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended).ToList();
        var dropping = watched.Where(a => CorReportPermille(c, a.CapabilityId, a.Id, 1) < CorReportPermille(c, a.CapabilityId, a.Id)).ToList();
        if (dropping.Count == 0) return null;
        if (dropping.Count == 1)
        {
            var a = dropping[0];
            return $"Naming {c.Name} here also lowers the watch on {NameOf(a.Id)} to about {CorReportPermille(c, a.CapabilityId, a.Id, 1) / 10}%.";
        }
        return $"Naming {c.Name} here also lowers the watch on the {Plural(dropping.Count, "other contract")}.";
    }

    /// <summary>The capability an award is for, from its card or its own fields.</summary>
    private string? CapabilityOf(AwardAction a) => a.Capability ?? _cards.FirstOrDefault(c => c.Id == a.Card)?.CapabilityId;

    /// <summary>Why this COR fits the work or does not, in one sentence with the odds and the lag.</summary>
    internal string CorWhy(CorState c, string capId, string? arrangementId, bool naming = false)
    {
        var who = c.Unit is { Length: > 0 } u ? $"{c.Name} ({u})" : c.Name;
        var work = Lower(CapLabel(capId));
        var fit = CorFits(c, capId) switch
        {
            true => $"knows {work}",
            false => $"does not work with {work}",
            null => $"is rated {c.Quality}",
        };
        var others = CorWatching(c, arrangementId);
        var load = others == 0 ? "watches no other contract" : $"already watches {Plural(others, "other contract")}";
        // DESK-132 (Claude play 20 Nov, third play, note 5): a buy being written that names this COR is one more to watch.
        var writing = _work.Count(w => w.Kind == WorkKind.Award && w.CompletedDay is null && w.StoppedDay is null
                                       && w.Payload is AwardPayload p && p.Action.Cor == c.Id && p.Action.Id != arrangementId);
        if (writing > 0) load += $" and is named on {(writing == 1 ? "one more" : $"{Qty(writing)} more")} being written";
        var lag = CorQuality(c).LagDays;
        var why = $"{who} {fit} and {load}: catches about {CorReportPermille(c, capId, arrangementId) / 10}% of problems, reported {(lag == 0 ? "the same day" : $"{Plural(lag, "day")} late")}.";
        return naming && CorLoadNote(c) is { } note ? $"{why} {note}" : why;
    }

    internal List<CorChoice> BuildCorChoices(string capId)
        => _cors.Select(c => new CorChoice(c.Id, c.Name, c.Unit, CorFits(c, capId), CorWatching(c, null),
                                           CorReportPermille(c, capId, null), CorQuality(c).LagDays, CorWhy(c, capId, null, naming: true)))
                .OrderByDescending(x => x.ReportPermille).ThenBy(x => x.LagDays).ToList();
}
