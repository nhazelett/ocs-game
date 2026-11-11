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
    internal int CorReportPermille(CorState c, string capId, string? arrangementId)
    {
        var q = CorQuality(c);
        if (c.Knows is null) return q.ReportPermille;
        var pm = q.ReportPermille
                 - (c.Knows.Contains(capId) ? 0 : Rules.Cor.OffFitPermille)
                 - CorWatching(c, arrangementId) * Rules.Cor.PerOtherContractPermille;
        return Math.Clamp(pm, Math.Min(Rules.Cor.FloorPermille, q.ReportPermille), 1000);
    }

    /// <summary>The capability an award is for, from its card or its own fields.</summary>
    private string? CapabilityOf(AwardAction a) => a.Capability ?? _cards.FirstOrDefault(c => c.Id == a.Card)?.CapabilityId;

    /// <summary>Why this COR fits the work or does not, in one sentence with the odds and the lag.</summary>
    internal string CorWhy(CorState c, string capId, string? arrangementId)
    {
        var who = c.Unit is { Length: > 0 } u ? $"{c.Name} ({u})" : c.Name;
        var work = Lower(CapLabel(capId));
        var fit = CorFits(c, capId) switch
        {
            true => $"works with {work} every day",
            false => $"does not work with {work}",
            null => $"is rated {c.Quality}",
        };
        var others = CorWatching(c, arrangementId);
        var load = others == 0 ? "watches no other contract" : $"already watches {Plural(others, "other contract")}";
        var lag = CorQuality(c).LagDays;
        return $"{who} {fit} and {load}: catches about {CorReportPermille(c, capId, arrangementId) / 10}% of problems, reported {(lag == 0 ? "the same day" : $"{Plural(lag, "day")} late")}.";
    }

    internal List<CorChoice> BuildCorChoices(string capId)
        => _cors.Select(c => new CorChoice(c.Id, c.Name, c.Unit, CorFits(c, capId), CorWatching(c, null),
                                           CorReportPermille(c, capId, null), CorQuality(c).LagDays, CorWhy(c, capId, null)))
                .OrderByDescending(x => x.ReportPermille).ThenBy(x => x.LagDays).ToList();
}
