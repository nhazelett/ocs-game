using System.Text.Json;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

/// <summary>
/// The predicate and effect vocabulary scenario events may use. Scenario loading rejects any kind not listed here,
/// so an author cannot write an event the engine silently ignores.
/// Targets of the form "site.capability" name a site id and a capability id.
/// </summary>
public static class Events
{
    public static readonly IReadOnlySet<string> KnownPredicates = new HashSet<string>(StringComparer.Ordinal)
    {
        "site.open",                 // target: site id
        "capability.below",          // target: site.capability; value: fraction 0..1 (yesterday's level)
        "coordination.runningDays",  // target: site.capability; comparator; value: days
        "contract.absent",           // target: site.capability; true when no contract (or pending award) supplies it
        "contract.present",          // target: site.capability; true when an active contract (not a handshake or coordination) supplies it
        "arrangement.active",        // target: arrangement id
        "arrangement.noCureNotice",  // target: arrangement id; true while no cure notice on it is delivered or in work (M3-18)
        "arrangement.notReplaced",   // target: arrangement id; true while nothing awarded this game for its site and capability is delivering (M3-18)
    };

    public static readonly IReadOnlySet<string> KnownEffects = new HashSet<string>(StringComparer.Ordinal)
    {
        "link.closed",                   // target: link id; days
        "coordination.withdraw",         // target: site.capability; days: notice (0 = today)
        "unit.unauthorizedCommitment",   // target: site.capability; value: vendor id
        "vendor.outOfScopeWork",         // target: site.capability; value: cents billed for work outside the active contract
        "site.populationJump",           // target: site id; fraction: share added; days or daysRange: notice before arrival
        "funding.late",                  // target: funding line id; days or daysRange: delay to its release (M3-03)
        "cor.rotates",                   // target: COR id; an untrained relief takes over its designations (M3-04)
        "acsa.withdraw",                 // target: capability id, or absent for all; days or daysRange: notice (M3-06)
        "hostNation.withdraw",           // target: capability id, or absent for all; days or daysRange: notice, else rules hostNation.withdrawalNoticeDays (M3-06)
        "vendor.reliabilityDrop",        // target: inherited arrangement id; value: its reliability from the event day, 0..1 (M3-18)
        "inbox",                         // message only (playerVisibleAs)
    };

    private static (string Site, string Cap) SiteCap(string? target)
    {
        var parts = (target ?? "").Split('.', 2);
        if (parts.Length != 2) throw new InvalidDataException($"target '{target}' must be site.capability");
        return (parts[0], parts[1]);
    }

    internal static bool Holds(Session s, PredicateData p, int day)
    {
        switch (p.Kind)
        {
            case "site.open":
            {
                var site = s.Scenario.Sites.First(x => x.Id == p.Target);
                return s.PopulationOn(site, day) > 0;
            }
            case "capability.below":
            {
                var (site, cap) = SiteCap(p.Target);
                var cs = s.Capabilities.FirstOrDefault(c => c.SiteId == site && c.CapabilityId == cap);
                var threshold = p.Value is { ValueKind: JsonValueKind.Number } v ? Fixed.Permille(v.GetDouble()) : 1000;
                return cs is not null && cs.History.Count > 0 && cs.TodayPermille < threshold;
            }
            case "coordination.runningDays":
            {
                var (site, cap) = SiteCap(p.Target);
                var running = s.Arrangements.Where(a => a.IsCoordination && a.SiteId == site && a.CapabilityId == cap && a.Status == ArrangementStatus.Active)
                                            .Select(a => a.DaysActive).DefaultIfEmpty(-1).Max();
                if (running < 0) return false;
                var value = p.Value is { ValueKind: JsonValueKind.Number } v ? v.GetInt32() : 0;
                return Compare(running, p.Comparator ?? ">", value);
            }
            case "contract.absent":
            {
                var (site, cap) = SiteCap(p.Target);
                var supplied = s.Arrangements.Any(a => a.SiteId == site && !a.IsCoordination && !a.Unauthorized
                                                       && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active
                                                       && (a.CapabilityId == cap || a.SuppliedQuantityMilli.ContainsKey(cap)));
                return !supplied;
            }
            case "contract.present":
            {
                var (site, cap) = SiteCap(p.Target);
                return s.Arrangements.Any(a => a.SiteId == site && a.CapabilityId == cap && !a.IsCoordination && !a.Unauthorized
                                               && a.Status == ArrangementStatus.Active);
            }
            case "arrangement.active":
                return s.Arrangements.Any(a => a.Id == p.Target && a.Status == ArrangementStatus.Active);
            case "arrangement.noCureNotice":
                return !s.CureNoticeIssued(p.Target ?? "");
            case "arrangement.notReplaced":
                return !s.ReplacementDelivering(p.Target ?? "");
            default:
                throw new InvalidDataException($"predicate kind {p.Kind} is not implemented");
        }
    }

    internal static void Apply(Session s, EventData ev, int index, int day, string cause)
    {
        var e = ev.Effects[index];
        switch (e.Kind)
        {
            case "link.closed":
                s.CloseLink(e.Target ?? throw new InvalidDataException("link.closed needs a target"), day, s.EffectDays(ev, index, 1), cause);
                break;
            case "site.populationJump":
                s.PopulationJumpNotice(e.Target ?? throw new InvalidDataException("site.populationJump needs a site"), day,
                                       s.EffectDays(ev, index, 3), Fixed.Permille(e.Fraction ?? 0), cause);
                break;
            case "coordination.withdraw":
            {
                var (site, cap) = SiteCap(e.Target);
                s.WithdrawCoordination(site, cap, day, s.EffectDays(ev, index, 0), cause);
                break;
            }
            case "unit.unauthorizedCommitment":
            {
                var (site, cap) = SiteCap(e.Target);
                var vendor = e.Value is { ValueKind: JsonValueKind.String } v ? v.GetString()! : throw new InvalidDataException("unit.unauthorizedCommitment needs a vendor id value");
                s.UnauthorizedCommitment(site, cap, vendor, day, cause);
                break;
            }
            case "vendor.outOfScopeWork":
            {
                var (site, cap) = SiteCap(e.Target);
                var cents = e.Value is { ValueKind: JsonValueKind.Number } v ? v.GetInt64() : throw new InvalidDataException("vendor.outOfScopeWork needs a cents value");
                s.OutOfScopeWork(site, cap, cents, day, cause);
                break;
            }
            case "funding.late":
                s.FundingLate(e.Target ?? throw new InvalidDataException("funding.late needs a funding line"), day, s.EffectDays(ev, index, 0), cause);
                break;
            case "cor.rotates":
                s.RotateCor(e.Target ?? throw new InvalidDataException("cor.rotates needs a COR"), day, cause);
                break;
            case "acsa.withdraw":
                s.WithdrawAcsa(e.Target, day, s.EffectDays(ev, index, 0), cause);
                break;
            case "hostNation.withdraw":
                s.WithdrawHostNation(e.Target, day, s.EffectDays(ev, index, -1), cause);
                break;
            case "vendor.reliabilityDrop":
            {
                var to = e.Value is { ValueKind: JsonValueKind.Number } v ? v.GetDouble() : throw new InvalidDataException("vendor.reliabilityDrop needs a reliability value");
                s.ReliabilityDrop(e.Target ?? throw new InvalidDataException("vendor.reliabilityDrop needs an arrangement"), Fixed.Permille(to), day, cause);
                break;
            }
            case "inbox":
                break;
            default:
                throw new InvalidDataException($"effect kind {e.Kind} is not implemented");
        }
    }

    private static bool Compare(int a, string op, int b) => op switch
    {
        ">" => a > b, ">=" => a >= b, "<" => a < b, "<=" => a <= b, "==" => a == b,
        _ => throw new InvalidDataException($"comparator {op} is not supported"),
    };
}

/// <summary>Effects that pre-deployment choices may carry (rules prepChoices).</summary>
public static class Prep
{
    public static readonly IReadOnlySet<string> KnownEffects = new HashSet<string>(StringComparer.Ordinal)
    {
        "shop.gpcAvailableDay", "shop.payingAgentAvailableDay", "shop.cashReplenishLagDays", "shop.systemOnlineDay",
        "model.manualContractTimeFactorOverride", "shop.warrantCents",
        // Recorded for the debrief; no mechanical effect in v0 (docs/assumptions.md).
        "vehicles.infoTier", "vendors.infoTierUpgrade", "vetting.shopDaysFactor", "actors.partnersConfirmed",
    };

    internal static void Apply(Session s, EffectData e)
    {
        int Int() => e.Value is { ValueKind: JsonValueKind.Number } v ? v.GetInt32() : throw new InvalidDataException($"{e.Kind} needs a number");
        switch (e.Kind)
        {
            case "shop.gpcAvailableDay": s.PrepSetGpcDay(Int()); break;
            case "shop.payingAgentAvailableDay": s.PrepSetPayingAgentDay(Int()); break;
            case "shop.cashReplenishLagDays": s.PrepSetReplenishLag(Int()); break;
            case "shop.systemOnlineDay": s.PrepSetSystemDay(Int()); break;
            case "model.manualContractTimeFactorOverride":
                s.PrepSetManualFactor(e.Value is { ValueKind: JsonValueKind.Number } f ? f.GetDouble() : 1.0); break;
            case "shop.warrantCents":
                s.PrepSetWarrant(e.Value is { ValueKind: JsonValueKind.Number } w ? w.GetInt64() : null); break;
            case "vehicles.infoTier": case "vendors.infoTierUpgrade": case "vetting.shopDaysFactor": case "actors.partnersConfirmed":
                break;
            default:
                throw new InvalidDataException($"prep effect {e.Kind} is not implemented");
        }
    }
}
