namespace LinesOfSupport.Sim.Core;

// Termination notice and vendor claims (M3-19, FAR 49; docs/design/M3-07-representative-scenario.md chain 3b). A
// contract with a notice clause (an inherited row's noticeDays) terminated with notice runs on for the notice days
// and costs nothing more. Terminated without notice it stops at once and the vendor files a claim: rules
// claims.settlementDays of its daily cost (capped at the days cut short), settled as shop work and charged to the
// arrangement's line. A vendor in default (M2-18) has no claim.
public sealed partial class Session
{
    private readonly HashSet<string> _noticeTerminations = new(StringComparer.Ordinal);
    private readonly List<ClaimState> _claims = [];
    public IReadOnlyList<ClaimState> Claims => _claims;

    private void FileClaimIfOwed(Arrangement arr, int plannedEnd, int day, string causeId)
    {
        var amount = ClaimCents(arr, plannedEnd, day, out var days);
        if (amount <= 0) return;
        var vendor = VendorData(arr.VendorId)?.Name ?? "The vendor";
        var rec = AddRecord(day, RecordKind.Compliance, arr.Id,
            $"{vendor} filed a claim on {NameOf(arr.Id)}: terminated without the {arr.NoticeDays} days' notice the contract requires. Settlement about {Money(amount)} ({days} days of its cost); settling it is shop work.", [causeId]);
        _claims.Add(new ClaimState { ArrangementId = arr.Id, FiledDay = day, AmountCents = amount, FiledRecordId = rec });
        AddWork(WorkKind.ClaimSettlement, $"Settle claim on {NameOf(arr.Id)}", Fixed.Milli(Rules.Claims.SettleShopHours), day + 1, arr.Id, rec);
    }

    /// <summary>The claim a termination without notice done on <paramref name="day"/> draws (0 for none): rules
    /// claims.settlementDays of the daily cost, capped at the days cut short. Shared with the end-contract quote (DESK-196).</summary>
    private long ClaimCents(Arrangement arr, int plannedEnd, int day, out int days)
    {
        days = 0;
        if (arr.NoticeDays is not > 0 || arr.IsCoordination || plannedEnd <= day) return 0;
        if (_issues.Any(i => i.ArrangementId == arr.Id && i.Outcome == CureOutcome.Default)) return 0;
        days = Math.Min(Rules.Claims.SettlementDays, plannedEnd - day);
        return Math.Max(0, arr.DailyCostCents * days);
    }

    private void SettleClaim(string arrangementId, int day, string causeId)
    {
        var claim = _claims.First(c => c.ArrangementId == arrangementId && c.SettledDay is null);
        var arr = _arrangements.First(a => a.Id == arrangementId);
        claim.SettledDay = day;
        // The settlement is a cost of the contract: incurred now, obligated from its line as far as the line allows.
        arr.IncurredCents += claim.AmountCents;
        var line = _funding.FirstOrDefault(f => f.Id == arr.FundingLineId);
        var add = line is not null && line.UsableOn(day) ? Math.Min(claim.AmountCents, Math.Max(0, line.RemainingCents)) : 0;
        if (line is not null) line.ObligatedCents += add;
        arr.ObligatedCents += add;
        var funded = add == claim.AmountCents ? $"paid from {NameOf(line!.Id)}" : $"{Money(add)} paid{(line is null ? "" : $" from {NameOf(line.Id)}")}; the rest waits on money";
        AddRecord(day, RecordKind.Compliance, arrangementId, $"Claim on {NameOf(arrangementId)} settled at {Money(claim.AmountCents)}, {funded}.", [causeId, claim.FiledRecordId]);
    }
}
