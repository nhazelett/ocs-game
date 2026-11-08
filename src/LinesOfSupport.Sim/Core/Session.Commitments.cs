using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// Unauthorized commitments (SPEC 4.5, backlog M1-04).
// Situation 1, handshake: a unit arranges support with a vendor directly; the player ratifies, parks or refuses.
// Situation 2, out-of-scope work: a vendor under contract does extra work on a unit member's say-so; deny or ratify.
// Precedent: each site's unit has a habit level that moves with how these are handled and sets the odds that the
// unit goes around contracting again when a capability is down and nothing is on contract for it.
// All draws come from the "unauthorized" stream, so games without commitments replay exactly as before.
public sealed partial class Session
{
    private readonly List<CommitmentState> _commitments = [];
    private readonly Dictionary<string, VendorStanding> _standing = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _habit = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _habitCause = new(StringComparer.Ordinal);
    private int _walkOffs, _outOfScope, _outOfScopeDenied, _confidenceHits, _habitHandshakes;

    public IReadOnlyList<CommitmentState> Commitments => _commitments;
    internal IReadOnlyDictionary<string, VendorStanding> Standing => _standing;
    internal int Habit(string siteId) => _habit.TryGetValue(siteId, out var h) ? h : Rules.Unauthorized.HabitStartPermille;

    private UnauthorizedRulesData Uc => Rules.Unauthorized;

    internal long UnitPriceFor(VendorData vendor, VendorOfferData offer)
        => _standing.TryGetValue(vendor.Id, out var st) && st.MarkupPermille > 0
            ? offer.UnitPriceCents + Fixed.MulPermille(offer.UnitPriceCents, st.MarkupPermille)
            : offer.UnitPriceCents;

    private string ShiftHabit(string siteId, int deltaPermille, string why, string causeId)
    {
        var before = Habit(siteId);
        var after = Math.Clamp(before + deltaPermille, 0, Uc.HabitCapPermille);
        _habit[siteId] = after;
        if (after != before) _habitCause[siteId] = causeId;
        return after == before ? "" : $" The {SiteName(siteId)} unit's habit of going around contracting {(after > before ? "rises" : "falls")} ({why}).";
    }

    // ------------------------------------------------------------------ situation 1: the handshake

    internal void UnauthorizedCommitment(string siteId, string capId, string vendorId, int day, string cause)
    {
        var vendor = VendorData(vendorId) ?? throw new InvalidDataException($"vendor {vendorId} not found");
        var offer = vendor.Offers.First(o => o.Capability == capId);
        var qty = Math.Min(Fixed.Milli(offer.CapacityPerDay), PeakDemandMilli(siteId, capId, day, LastDay));
        var core = Cap(capId).Lines.First(l => l.Kind == "core").Id;
        var id = $"uc{_unauthorized + 1}";
        var arr = new Arrangement
        {
            Id = id, Instrument = "unauthorizedCommitment", VendorId = vendorId, SiteId = siteId, CapabilityId = capId, Lines = [core],
            SupplierIslandId = StockIsland(vendor, capId), QuantityMilli = qty, DailyCostCents = qty * offer.UnitPriceCents / 1000,
            ReliabilityPermille = Fixed.Permille(vendor.Reliability), Unauthorized = true, CreatedDay = day, CauseId = cause,
            StartDay = day + 1, EndDay = LastDay,
        };
        _arrangements.Add(arr);
        _unauthorized++;
        var rec = AddRecord(day, RecordKind.Compliance, id,
            $"Unauthorized commitment {id}: the {SiteName(siteId)} unit arranged {CapLabel(capId)} directly with {vendor.Name}. No contract, no terms, no obligation. The vendor is delivering in good faith and is not being paid. Decision: ratify, park or refuse.", [cause]);
        _commitments.Add(new CommitmentState
        {
            Id = id, Kind = CommitmentKind.Handshake, ArrangementId = id, SiteId = siteId, CapabilityId = capId, VendorId = vendorId,
            OpenedDay = day, PatienceDays = _rngUnauthorized.NextInt(Uc.PatienceDays), CauseId = rec,
        });
    }

    // ------------------------------------------------------------------ situation 2: out-of-scope work

    internal void OutOfScopeWork(string siteId, string capId, long amountCents, int day, string cause)
    {
        var arr = _arrangements.Where(a => a.SiteId == siteId && a.CapabilityId == capId && !a.IsCoordination && !a.Unauthorized
                                           && a.Status == ArrangementStatus.Active)
                               .OrderBy(a => a.Id, StringComparer.Ordinal).FirstOrDefault();
        if (arr is null) return;
        var id = $"oos{_outOfScope + 1}";
        _outOfScope++;
        var vendor = VendorData(arr.VendorId);
        var rec = AddRecord(day, RecordKind.Compliance, id,
            $"Out-of-scope work {id}: {vendor?.Name ?? "the vendor"} billed {Money(amountCents)} for work outside {arr.Id}'s scope, done on a unit member's say-so. Decision: deny payment or ratify.", [cause, arr.CauseId]);
        _commitments.Add(new CommitmentState
        {
            Id = id, Kind = CommitmentKind.OutOfScope, ArrangementId = arr.Id, SiteId = siteId, CapabilityId = capId, VendorId = arr.VendorId,
            OpenedDay = day, AmountCents = amountCents, CauseId = rec,
        });
    }

    // ------------------------------------------------------------------ the decision

    private ActionResult ApplyResolve(ResolveCommitmentAction r)
    {
        var c = _commitments.FirstOrDefault(x => x.Id == r.Commitment);
        if (c is null) return ActionResult.Stop($"commitment {r.Commitment} does not exist");
        if (!c.Unresolved) return ActionResult.Stop($"{c.Id} is already {c.Decision.ToString().ToLowerInvariant()}");
        var allowed = c.Kind == CommitmentKind.Handshake ? new[] { "ratify", "park", "refuse" } : ["ratify", "deny"];
        if (!allowed.Contains(r.Choice)) return ActionResult.Stop($"{r.Choice} is not a choice for {c.Id}; choose {string.Join(", ", allowed)}");
        if (r.FundingLine is not null && !_funding.Any(f => f.Id == r.FundingLine)) return ActionResult.Stop($"funding line {r.FundingLine} does not exist");
        if (c.Decision == CommitmentDecision.Parked && r.Choice == "park") return ActionResult.Stop($"{c.Id} is already parked");

        var arr = _arrangements.First(a => a.Id == c.ArrangementId);
        var vendor = VendorData(c.VendorId)?.Name ?? "the vendor";
        switch (r.Choice)
        {
            case "ratify":
            {
                c.Decision = CommitmentDecision.Ratifying;
                c.DecidedDay = Day;
                c.FundingLineId = r.FundingLine;
                var rec = AddRecord(Day, RecordKind.Action, c.Id,
                    $"Ratification of {c.Id} started: {Rules.Model.Work.RatificationShopDays} shop day(s). {vendor} is told it will be paid.", [c.CauseId]);
                AddWork(WorkKind.Ratification, $"Ratify {c.Id}", Fixed.Milli(Rules.Model.Work.RatificationShopDays * Rules.Model.HoursPerPersonPerDay), Day, c.Id, rec);
                return ActionResult.Ok("ratification in work");
            }
            case "park":
            {
                c.Decision = CommitmentDecision.Parked;
                c.DecidedDay = Day;
                var rec = AddRecord(Day, RecordKind.Action, c.Id, $"{c.Id} parked: {vendor} keeps delivering unpaid.", [c.CauseId]);
                var habit = ShiftHabit(c.SiteId, Uc.HabitRiseParkOrRefusePermille, "the handshake was parked", rec);
                if (habit.Length > 0) AddRecord(Day, RecordKind.Compliance, c.SiteId, habit.Trim(), [rec]);
                return ActionResult.Ok("parked");
            }
            case "refuse":
            {
                c.Decision = CommitmentDecision.Refused;
                c.DecidedDay = Day;
                if (arr.Status is ArrangementStatus.Active or ArrangementStatus.Pending or ArrangementStatus.Suspended)
                {
                    arr.EndDay = Math.Min(arr.EndDay, Day - 1);
                    arr.Status = ArrangementStatus.Ended;
                }
                _confidenceHits++;
                var rec = AddRecord(Day, RecordKind.ArrangementEnd, c.Id,
                    $"{c.Id} refused: {vendor} stops at once and {CapLabel(c.CapabilityId).ToLowerInvariant()} at {SiteName(c.SiteId)} has no source from it. The site commander's confidence in contracting drops.", [c.CauseId]);
                LoseConfidence(c.SiteId, rec);
                var habit = ShiftHabit(c.SiteId, Uc.HabitRiseParkOrRefusePermille, "the handshake was refused with nothing in its place", rec);
                if (habit.Length > 0) AddRecord(Day, RecordKind.Compliance, c.SiteId, habit.Trim(), [rec]);
                return ActionResult.Ok("refused");
            }
            default: // deny
            {
                c.Decision = CommitmentDecision.Denied;
                c.DecidedDay = Day;
                _outOfScopeDenied++;
                var rec = AddRecord(Day, RecordKind.Action, c.Id, $"Payment for {c.Id} denied: {Money(c.AmountCents)} of work outside {arr.Id}'s scope.", [c.CauseId]);
                AddWork(WorkKind.Denial, $"Deny {c.Id}", Fixed.Milli(Uc.OutOfScopeDenyShopHours), Day, c.Id, rec);
                return ActionResult.Ok("denied");
            }
        }
    }

    private FundingLineState? RatificationLine(CommitmentState c, int day)
    {
        var named = c.FundingLineId is null ? null : _funding.FirstOrDefault(f => f.Id == c.FundingLineId);
        if (named is not null && named.UsableOn(day) && named.RemainingCents > 0) return named;
        var contract = _arrangements.FirstOrDefault(a => a.Id == c.ArrangementId)?.FundingLineId;
        return _funding.Where(f => f.UsableOn(day) && f.RemainingCents > 0)
                       .OrderBy(f => f.Id == contract ? 0 : 1).ThenBy(f => f.Id, StringComparer.Ordinal).FirstOrDefault();
    }

    /// <summary>Called when a ratification work item finishes. Returns false if it is not a tracked commitment.</summary>
    private bool CompleteRatification(string id, int day, string causeId)
    {
        var c = _commitments.FirstOrDefault(x => x.Id == id);
        if (c is null) return false;
        var arr = _arrangements.First(a => a.Id == c.ArrangementId);
        var line = RatificationLine(c, day);
        long need;
        var tail = "";
        if (c.Kind == CommitmentKind.Handshake)
        {
            need = arr.IncurredCents - arr.ObligatedCents;
            if (!c.WalkedOff && arr.Status is ArrangementStatus.Active or ArrangementStatus.Pending)
            {
                // The ratification covers what was delivered plus a short bridge. The vendor keeps delivering on the
                // unit's word after that, with no terms; those deliveries are owed until a contract covers the need.
                var bridgeEnd = Math.Min(arr.EndDay, day + Uc.RatifiedBridgeDays);
                need += arr.DailyCostCents * Math.Max(0, bridgeEnd - day + 1);
                tail = $" {VendorData(c.VendorId)!.Name} keeps delivering; deliveries after day {bridgeEnd} are owed and not obligated until a contract covers the need.";
            }
        }
        else need = c.AmountCents;

        var add = line is null ? 0 : Math.Min(need, line.RemainingCents);
        if (line is not null) line.ObligatedCents += add;
        arr.ObligatedCents += add;
        if (c.Kind == CommitmentKind.OutOfScope) arr.IncurredCents += c.AmountCents;
        var funded = need <= 0 ? "nothing owed"
            : add == need ? $"{Money(add)} obligated from {line!.Id}"
            : line is null ? $"no usable funding line; {Money(need)} still owed"
            : $"{Money(add)} of {Money(need)} obligated from {line.Id}; the rest is still owed";
        c.Decision = CommitmentDecision.Ratified;
        var rec = AddRecord(day, RecordKind.Compliance, c.Id, $"Ratification of {c.Id} completed: {funded}.{tail}", [causeId]);

        var habit = c.Kind == CommitmentKind.OutOfScope
            ? ShiftHabit(c.SiteId, Uc.HabitRiseOutOfScopeRatifyPermille, "out-of-scope work was paid", rec)
            : !c.WalkedOff && c.DecidedDay is { } dd && dd - c.OpenedDay <= Uc.QuickRatifyWithinDays
                ? ShiftHabit(c.SiteId, -Uc.HabitDropQuickRatifyPermille, "the handshake was ratified quickly", rec)
                : "";
        if (habit.Length > 0) AddRecord(day, RecordKind.Compliance, c.SiteId, habit.Trim(), [rec]);
        return true;
    }

    // ------------------------------------------------------------------ daily clocks

    /// <summary>Patience, walk-off, the parked-ratification inquiry and the long-open habit rise. Runs before supply.</summary>
    private void CommitmentClocks(int d)
    {
        foreach (var c in _commitments.Where(x => x.Unresolved).ToList())
        {
            var open = d - c.OpenedDay;
            if (c.Kind == CommitmentKind.Handshake)
            {
                var arr = _arrangements.First(a => a.Id == c.ArrangementId);
                if (!c.WalkedOff && open >= c.PatienceDays && arr.Status is ArrangementStatus.Active or ArrangementStatus.Pending)
                {
                    c.WalkedOff = true;
                    _walkOffs++;
                    arr.EndDay = d - 1;
                    arr.Status = ArrangementStatus.Ended;
                    var declines = _rngUnauthorized.Roll(Uc.WalkOffDeclinePermille);
                    var vendor = VendorData(c.VendorId)!;
                    var after = declines ? "It will decline work from this office from now on."
                                         : $"It will quote this office {Uc.WalkOffMarkupPermille / 10}% higher from now on.";
                    var rec = AddRecord(d, RecordKind.Withdrawal, c.Id,
                        $"{vendor.Name} walked off {c.Id} after {open} days of unpaid delivery. {CapLabel(c.CapabilityId)} at {SiteName(c.SiteId)} loses that source today. {after}", [c.CauseId]);
                    _standing[vendor.Id] = new VendorStanding { DeclinesWork = declines, MarkupPermille = declines ? 0 : Uc.WalkOffMarkupPermille, CauseId = rec };
                }
            }
            if (!c.InquiryDrawn && open >= Uc.ParkedInquiryAfterDays)
            {
                c.InquiryDrawn = true;
                _hhqInquiries++;
                var text = c.Kind == CommitmentKind.Handshake
                    ? $"Higher HQ asked why unauthorized commitment {c.Id} has gone {open} days without ratification."
                    : $"{VendorData(c.VendorId)?.Name ?? "The vendor"} filed a claim for {Money(c.AmountCents)} of out-of-scope work ({c.Id}) after {open} days with no answer; higher HQ asked for the file.";
                var rec = AddRecord(d, RecordKind.Compliance, c.Id, $"{text} A response is now shop work ({Uc.ParkedInquiryResponseHours} hours).", [c.CauseId]);
                AddWork(WorkKind.HhqResponse, $"Respond to HHQ on {c.Id}", Fixed.Milli(Uc.ParkedInquiryResponseHours), d, c.Id, rec);
            }
            if (c.Kind == CommitmentKind.Handshake && !c.LongOpenCounted && open >= Uc.HabitLongOpenDays)
            {
                c.LongOpenCounted = true;
                var habit = ShiftHabit(c.SiteId, Uc.HabitRiseLongOpenPermille, $"{c.Id} has been open {open} days", c.CauseId);
                if (habit.Length > 0) AddRecord(d, RecordKind.Compliance, c.SiteId, habit.Trim(), [c.CauseId]);
            }
        }
    }

    /// <summary>
    /// Precedent: a unit with a habit of going around contracting may do it again when one of its capabilities was
    /// down yesterday and nothing on contract (or awarded) supplies it. One roll per site per day, only when the
    /// habit is above zero.
    /// </summary>
    private void UnitHabits(int d)
    {
        foreach (var site in Scenario.Sites)
        {
            var habit = Habit(site.Id);
            if (habit <= 0 || d < site.OpensDay) continue;
            foreach (var capId in _capOrder)
            {
                var cs = _caps.FirstOrDefault(x => x.SiteId == site.Id && x.CapabilityId == capId);
                if (cs is null || cs.History.Count == 0 || cs.TodayPermille >= Rules.Model.DownThresholdPermille) continue;
                var supplied = _arrangements.Any(a => a.SiteId == site.Id && !a.IsCoordination
                                                      && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active
                                                      && (a.CapabilityId == capId || a.SuppliedQuantityMilli.ContainsKey(capId)))
                               || _work.Any(w => w.CompletedDay is null && w.Payload is AwardPayload p && p.SiteId == site.Id && p.CapabilityId == capId);
                if (supplied) continue;
                var vendor = Scenario.Vendors
                    .Where(v => !v.VehicleOnly && !_standing.ContainsKey(v.Id) && v.Offers.Any(o => o.Capability == capId && o.CapacityPerDay > 0))
                    .OrderBy(v => v.Offers.First(o => o.Capability == capId).UnitPriceCents).ThenBy(v => v.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (vendor is null) continue;
                if (!_rngUnauthorized.Roll(habit)) break;
                _habitHandshakes++;
                var causes = new List<string>();
                if (cs.OpenShortfallCause is { } sc) causes.Add(sc);
                if (_habitCause.TryGetValue(site.Id, out var hc)) causes.Add(hc);
                var rec = AddRecord(d, RecordKind.Event, $"{site.Id}.{capId}",
                    $"With {CapLabel(capId).ToLowerInvariant()} down and nothing on contract, the {site.Name} unit goes around contracting again (habit {habit / 10}%).", causes);
                UnauthorizedCommitment(site.Id, capId, vendor.Id, d, rec);
                break;
            }
        }
    }
}
