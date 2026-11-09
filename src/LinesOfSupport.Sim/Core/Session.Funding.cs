namespace LinesOfSupport.Sim.Core;

// Funding clocks (M3-03, SPEC 4 "a funding line expires at fiscal year end and its replacement is late").
// Lines carry an amount, a release day, a fiscal-year end and an optional purpose (the capabilities they are
// certified for). The comptroller's side of the clock is written as Funding records so the morning board shows it
// and the debrief can name a gap that fell in a window with no usable money. Nothing here draws a random number:
// a late release is a scenario event whose delay was drawn from the events stream at creation.
public sealed partial class Session
{
    /// <summary>Days before a line's fiscal-year end that the comptroller warns about money not yet obligated.</summary>
    public const int FundingExpiryWarningDays = 10;

    /// <summary>Longest severable service a line may pay for when the period runs past the line's fiscal-year end (10 U.S.C. 3133).</summary>
    public const int SeverableServiceMaxDays = 365;

    // Lines with money usable on the morning of each day, index = day (index 0 unused). The debrief reads it.
    private readonly List<string[]> _fundsMorning = [[]];

    // Release notices are good news: the morning board shows them as Info, the other funding notices as Warning.
    private readonly HashSet<string> _fundingReleases = new(StringComparer.Ordinal);

    private readonly List<(string AwardId, string SiteId, string CapabilityId, int Day, string RecordId)> _fundingRejections = [];

    private string[] UsableLineIds(int day)
        => [.. _funding.Where(f => f.UsableOn(day) && f.RemainingCents > 0).Select(f => f.Id)];

    private void SnapshotFunds(int day)
    {
        while (_fundsMorning.Count <= day) _fundsMorning.Add([]);
        _fundsMorning[day] = UsableLineIds(day);
    }

    /// <summary>True when, on the morning of the day, some line with money left could pay for the capability.</summary>
    internal bool FundedOn(int day, string capability)
        => day >= 1 && day < _fundsMorning.Count && _fundsMorning[day].Any(id => _funding.First(f => f.Id == id).Pays(capability));

    private string Purpose(FundingLineState line)
        => string.Join(", ", line.Capabilities.Select(c => CapLabel(c).ToLowerInvariant()));

    /// <summary>
    /// Stops that depend on the line rather than the money left in it: purpose (a line certified for named
    /// capabilities pays only for them) and the 12-month limit on a severable service that crosses the line's
    /// fiscal-year end. Bona fide need for a service starting after the year is not modeled (docs/assumptions.md).
    /// </summary>
    internal IEnumerable<string> FundingStops(FundingLineState line, string capability, int? startDay, int durationDays)
    {
        if (!line.Pays(capability))
            yield return $"{Upper(NameOf(line.Id))} is certified for {Purpose(line)} only, not {CapLabel(capability).ToLowerInvariant()}";
        var start = startDay ?? Day;
        if (durationDays > SeverableServiceMaxDays && start + durationDays - 1 > line.ExpiresDay)
            yield return $"a service paid from {NameOf(line.Id)} that runs past its fiscal-year end (day {line.ExpiresDay}) may last at most 12 months";
    }

    /// <summary>Why a line cannot pay for an award completing today, in words for the rejection record.</summary>
    private string FundingShortReason(FundingLineState? line, long need, int day)
        => line is null ? "no funding line was named"
            : day > line.ExpiresDay ? $"{Upper(NameOf(line.Id))} expired with day {line.ExpiresDay}"
            : day < line.AvailableDay ? $"{Upper(NameOf(line.Id))} is not released until day {line.AvailableDay}"
            : $"{Upper(NameOf(line.Id))} has {Money(line.RemainingCents)} left of the {Money(need)} needed";

    /// <summary>Event effect <c>funding.late</c>: the comptroller pushes a line's release back (M3-03).</summary>
    internal void FundingLate(string lineId, int day, int delayDays, string cause)
    {
        var line = _funding.First(f => f.Id == lineId);
        if (delayDays <= 0 || day >= line.AvailableDay) return; // already released: nothing to delay
        var was = line.AvailableDay;
        line.AvailableDay = was + delayDays;
        AddRecord(day, RecordKind.Funding, line.Id,
            $"Comptroller: {NameOf(line.Id)} ({Money(line.AmountCents)}) will not be released on day {was} as planned; expect it on day {line.AvailableDay}.", [cause]);
    }

    /// <summary>End of each day: the comptroller's notices for tomorrow morning, and tomorrow's funding snapshot.</summary>
    private void FundingClocks(int d)
    {
        var next = d + 1;
        foreach (var f in _funding)
        {
            if (f.ExpiresDay == d && f.AvailableDay <= d)
                AddRecord(d, RecordKind.Funding, f.Id, f.RemainingCents > 0
                    ? $"{Upper(NameOf(f.Id))} reached its fiscal-year end with day {d}: {Money(f.RemainingCents)} never obligated can no longer pay for anything new."
                    : $"{Upper(NameOf(f.Id))} reached its fiscal-year end with day {d}, fully obligated.", []);
            if (f.ExpiresDay - d == FundingExpiryWarningDays && f.AvailableDay <= d && f.RemainingCents > 0 && next <= LastDay)
                AddRecord(d, RecordKind.Funding, f.Id,
                    $"Comptroller: {NameOf(f.Id)} reaches its fiscal-year end with day {f.ExpiresDay}, {Money(f.RemainingCents)} still unobligated. Awards, definitizations and ratifications that complete after day {f.ExpiresDay} need another line; a service awarded before then may run up to 12 months past it.", []);
            if (f.AvailableDay == next && next > 1 && next <= LastDay)
            {
                var late = f.AvailableDay > f.PlannedAvailableDay ? $", {Plural(f.AvailableDay - f.PlannedAvailableDay, "day")} later than planned" : "";
                var purpose = f.Capabilities.Count > 0 ? $" for {Purpose(f)}" : "";
                _fundingReleases.Add(AddRecord(d, RecordKind.Funding, f.Id,
                    $"Comptroller released {NameOf(f.Id)}: {Money(f.RemainingCents)}{purpose} usable from day {next}{late}.", []));
            }
        }
        SnapshotFunds(next);
        if (next <= LastDay && _fundsMorning[next].Length == 0 && _fundsMorning[d].Length > 0)
        {
            var coming = _funding.Where(f => f.AvailableDay > next && f.RemainingCents > 0).OrderBy(f => f.AvailableDay).FirstOrDefault();
            AddRecord(d, RecordKind.Funding, "funding",
                $"No funding line has money usable from day {next}: nothing new can be awarded, definitized or ratified until one does. "
                + (coming is null ? "No further funding is scheduled." : $"Next release: {NameOf(coming.Id)}, expected day {coming.AvailableDay}."), []);
        }
    }

    /// <summary>
    /// Debrief: a day with nothing supplying the capability that also fell in a window with no money that could pay
    /// for it. Avoidable when money was usable before the window opened (the fiscal-year end is on the ledger from the
    /// line's release, and a service awarded before it may run up to 12 months past it); unforeseeable when no line
    /// ever could have paid. Awards rejected for money at completion are named.
    /// </summary>
    private (string Key, Views.GapFactor F)? FundingLapseFactor(string siteId, string capId, int d)
    {
        if (FundedOn(d, capId)) return null;
        var from = d;
        while (from > 1 && !FundedOn(from - 1, capId)) from--;
        var to = d;
        while (to + 1 <= DaysPlayed && !FundedOn(to + 1, capId)) to++;
        var reopened = to < DaysPlayed;
        var cap = CapLabel(capId).ToLowerInvariant();
        var ended = _funding.Where(f => f.Pays(capId) && f.ExpiresDay == from - 1).Select(f => f.Id).ToList();
        var released = _funding.Where(f => f.Pays(capId) && f.AvailableDay == to + 1).ToList();
        var lateNotices = _records.Where(r => r.Kind == RecordKind.Funding && r.Day < d && released.Any(f => f.Id == r.Subject)
                                              && r.Text.Contains("will not be released", StringComparison.Ordinal)).ToList();
        var text = $"No funding line with money could pay for {cap} on day {d}"
                   + (from < to ? $" (days {from} to {to}{(reopened ? "" : ", the end of the game")})" : "") + ".";
        if (ended.Count > 0) text += $" {Upper(string.Join(" and ", ended.Select(FundingName)))} reached its fiscal-year end with day {from - 1}.";
        foreach (var f in released)
            text += f.AvailableDay > f.PlannedAvailableDay
                ? $" {Upper(NameOf(f.Id))} was released day {f.AvailableDay}, {Plural(f.AvailableDay - f.PlannedAvailableDay, "day")} late" + (lateNotices.Count > 0 ? $" (the comptroller warned on day {lateNotices[0].Day})." : ".")
                : $" {Upper(NameOf(f.Id))} was released day {f.AvailableDay}.";
        var rejected = _fundingRejections.Where(x => x.SiteId == siteId && x.CapabilityId == capId && x.Day >= from && x.Day <= d).ToList();
        if (rejected.Count > 0) text += $" {Upper(string.Join(", ", rejected.Select(x => ArrName(x.AwardId))))} reached award with no money to pay for it.";
        var before = from > 1 && Enumerable.Range(1, from - 1).Any(x => FundedOn(x, capId));
        var ids = new List<string>();
        ids.AddRange(lateNotices.Select(r => r.Id));
        ids.AddRange(rejected.Select(x => x.RecordId));
        return ($"funds|{from}", before
            ? new Views.GapFactor(Views.GapFactorKind.FundingLapse, Views.GapTag.Avoidable, d,
                text + $" Money was usable until day {from - 1}: a follow-on awarded before then could have run through the window.", ids)
            : new Views.GapFactor(Views.GapFactorKind.FundingLapse, Views.GapTag.Unforeseeable, d,
                text + " No line that could pay for it had been released.", ids));
    }
}
