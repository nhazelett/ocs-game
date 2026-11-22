namespace LinesOfSupport.Sim.Core;

public sealed partial class Session
{
    /// <summary>DESK-205 (Claude play 22 Nov, notes P1-6, P2-10): the contract page's money line counted a half-delivered day
    /// at its full price and said nothing about why. On a buy with confusion days this is the one sentence that says so:
    /// "Day 3 was disputed and half arrived; the card had already paid for the whole day, and with nothing written there is
    /// no claim." Null when the buy has had no confusion day or the id is unknown. No money moves.</summary>
    internal string? BuildDisputedDaysLine(string arrangementId)
    {
        if (_arrangements.FirstOrDefault(x => x.Id == arrangementId) is not { } a) return null;
        var days = _records.Where(r => r.Kind == RecordKind.ExecutionConfusion && r.Subject == a.Id)
                           .Select(r => r.Day).Distinct().Order().ToList();
        if (days.Count == 0) return null;
        var one = days.Count == 1;
        var when = one ? $"Day {days[0]} was disputed and half arrived"
                       : $"Days {JoinAnd(days.Select(d => d.ToString()).ToList())} were disputed and half arrived each time";
        var whole = one ? "the whole day" : days.Count == 2 ? "both whole days" : "each whole day";
        var paid = a.Unauthorized ? $"the vendor still charges for {whole}"
                 : a.Instrument == "gpc" ? $"the card had already paid for {whole}"
                 : a.Instrument == "sf44" ? $"the cash for {whole} was already paid"
                 : $"the charge covers {whole}";
        return $"{when}; {paid}, and with nothing written there is no claim.";
    }
}
