using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Why it is short (DESK-38, docs/design/desk-redesign.md D18). Nick on his 13 Nov replay: "Water 0% supplied" and "Food
// service 0% supplied" with nothing near them to say why or what to do. Wherever a site shows a capability short, one
// sentence from Sim facts says why and what is in motion. Only what the office has been told: a vendor failure the COR
// has not reported reads as "nobody has told you why yet", so the fog of D8 keeps its teeth. Read only.
public sealed partial class Session
{
    private const string NotToldYet = "The site reports it short. Nobody has told you why yet.";

    /// <summary>
    /// One sentence on why a capability at a site was short on the day just played, and what is in motion; null when it
    /// was supplied in full, the site is not open, or no day has been played.
    /// </summary>
    internal string? BuildWhyShort(string siteId, string capId)
    {
        var cs = CapState(siteId, capId);
        var d = Day - 1;   // the day just played: the level every view shows this morning
        if (cs is null || d < 1 || cs.TodayPermille >= 1000) return null;
        var site = SiteData(siteId);
        if (PopulationOn(site, Math.Min(d, LastDay)) == 0) return null;
        var cap = Cap(capId);
        var label = Lower(cap.Label);

        // A consumable with stock left after the day had all it needed from its own supply: the level came from
        // something it depends on.
        var own = cap.Delivery == "consumable" && cs.BufferMilli > 0 ? null : OwnCause(site, cap, d);
        var dep = cap.DependsOn.Select(x => (Id: x, State: CapState(siteId, x))).FirstOrDefault(x => x.State is { TodayPermille: < 1000 });
        string? depText = dep.State is null ? null
            : dep.State.TodayPermille == 0 ? $"no {Lower(CapLabel(dep.Id))}, so no {label}: {label} needs {Lower(CapLabel(dep.Id))}"
            : $"{Lower(CapLabel(dep.Id))} was at {dep.State.TodayPermille / 10}%, and {label} needs it";
        if (own is null) return SentenceCase((depText ?? NotToldYet.TrimEnd('.')) + ".");
        return SentenceCase(depText is null ? own + "." : $"{own}; it also needs {Lower(CapLabel(dep.Id))}, which was at {dep.State!.TodayPermille / 10}%.");
    }

    /// <summary>The cause in this capability's own supply, without a final period; null when its own supply covered the
    /// day (so a short level came from something it depends on) or the office has not been told why.</summary>
    private string? OwnCause(Data.SiteData site, Data.CapabilityData cap, int d)
    {
        var capId = cap.Id;
        var where = $"{Lower(cap.Label)} at {site.Name}";
        bool Feeds(Arrangement a) => a.SiteId == site.Id
            && ((a.CapabilityId == capId && a.Lines.Any(l => cap.Lines.FirstOrDefault(x => x.Id == l)?.Kind == "core")) || a.SuppliedQuantityMilli.ContainsKey(capId));
        long Gives(Arrangement a) => (a.CapabilityId == capId ? a.QuantityMilli : 0) + a.SuppliedQuantityMilli.GetValueOrDefault(capId);

        var running = _arrangements.Where(a => Feeds(a) && a.StartDay <= d && a.EndDay >= d
                                               && a.Status is ArrangementStatus.Active or ArrangementStatus.Suspended).ToList();
        if (running.Count == 0) return NothingRunning(site, cap, d, where);

        foreach (var a in running)
        {
            var who = SupplierName(a);
            if (a.Status == ArrangementStatus.Suspended)
                return $"{who} has stopped work on {where}: the UCA has gone unpaid";
            if (_sf44Held.TryGetValue(a.Id, out var held))
                return held.Reason.StartsWith("no cash", StringComparison.Ordinal)
                    ? $"{Possessive(who)} delivery did not happen: it is paid in cash at the door and the cash box was short{(_cashReplenishDay is { } r && r >= Day ? $"; finance tops it up on day {r}" : "")}"
                    : $"{Possessive(who)} delivery did not happen: the boat to {IslandData(site.Island).Name} was not running";
            if (_dayHeld.FirstOrDefault(h => h.Arr == a.Id && h.Cap == capId) is { Arr: not null } wait)
            {
                var from = IslandData(a.SupplierIslandId).Name;
                if (wait.ClosedLink is { } link)
                {
                    var through = _linkClosedThrough.TryGetValue(link, out var t) && t >= d ? $" through day {t}" : "";
                    return $"{Possessive(who)} delivery is waiting on {from}: the route to {IslandData(site.Island).Name} is closed{through}";
                }
                return $"{Possessive(who)} delivery is waiting on {from} for the next run to {IslandData(site.Island).Name}";
            }
            var failure = _records.LastOrDefault(r => r.Kind == RecordKind.VendorFailure && r.Subject == a.Id && r.Day == d);
            if (failure is not null)
            {
                if (!FailureKnown(failure)) return null;
                var missing = failure.Text.IndexOf(" (", StringComparison.Ordinal) is var i and > 0 ? failure.Text[i..].TrimEnd('.') : "";
                return cap.Delivery == "consumable" ? $"{who} missed the delivery{missing}" : $"{Possessive(who)} equipment was down{missing}";
            }
            if (CapacityShort(a.Id, d))
                return $"{who} has more on order than it can supply, so {NameOf(a.Id)} came up short; a second source covers the rest";
            if (_records.Any(r => r.Kind == RecordKind.ExecutionConfusion && r.Subject == a.Id && r.Day == d))
                return $"{who} delivered half: the vendor and the unit disagree on what was bought, and nothing is written down to settle it";
        }

        var supply = running.Sum(Gives);
        var demand = DemandMilli(site, capId, Math.Min(d, LastDay));
        if (supply < demand)
        {
            var named = running.Count == 1 ? SupplierName(running[0]) : "What is arranged";
            return $"{named} supplies {Amount(supply / 1000, cap)}, and {site.Name} needs {Amount((demand + 999) / 1000, cap)}{BuyingNote(site.Id, capId)}";
        }
        return null;   // its own supply covered the day: the cause is something it depends on, or not known
    }

    /// <summary>Nothing fed it on the day: say what is coming, being bought or being asked about, or that nothing is.</summary>
    private string NothingRunning(Data.SiteData site, Data.CapabilityData cap, int d, string where)
    {
        var capId = cap.Id;
        var coming = _arrangements.Where(a => a.SiteId == site.Id && a.CapabilityId == capId && a.StartDay > d && Live(a))
                                  .OrderBy(a => a.StartDay).FirstOrDefault();
        var buying = BuyingNote(site.Id, capId);
        if (coming is not null)
            return $"Nothing supplies {where} until {SupplierName(coming)} starts on day {coming.StartDay}{buying}";
        if (buying.Length > 0) return $"Nothing supplies {where} yet{buying}";
        var asking = _work.Where(w => w.CompletedDay is null && w.Payload is VisitPayload { Kind: "ask" } v && v.Target is { } t
                                      && _cards.FirstOrDefault(c => t.StartsWith(c.Id + "|", StringComparison.Ordinal)) is { } c
                                      && c.SiteId == site.Id && c.CapabilityId == capId)
                          .OrderBy(w => w.CreatedDay).FirstOrDefault();
        if (asking is not null)
        {
            var target = ((VisitPayload)asking.Payload!).Target!;
            var person = BuildPerson(target[(target.IndexOf('|') + 1)..]);
            return $"Nothing is bought for {where} yet: {WhoIs(asking.Assignee)} asking {person?.DisplayName ?? "someone"} about it";
        }
        var ended = _arrangements.Where(a => a.SiteId == site.Id && a.CapabilityId == capId && a.EndDay < d && a.Status != ArrangementStatus.Declined)
                                 .OrderBy(a => a.EndDay).LastOrDefault();
        return ended is not null
            ? $"{SupplierName(ended)} stopped after day {ended.EndDay}, and nothing is bought to follow it"
            : $"Nothing is bought for {where}";
    }

    /// <summary>", and MSgt Joan Avery is writing the purchase order with Harbor Bottling, done about day 6", or "".</summary>
    private string BuyingNote(string siteId, string capId)
    {
        var w = _work.Where(x => x.CompletedDay is null && BuysFor(x, siteId, capId)).OrderBy(x => x.CreatedDay).FirstOrDefault();
        if (w is null) return "";
        var done = PlayQueue(Member(w.Assignee), 0, Day, w, w.HoursRemainingMilli) ?? Day;
        return $"; {WhoIs(w.Assignee)} {BuyingWords(w)}, done about day {done}";
    }

    /// <summary>A vendor failure the office knows about: one it can see, or one a COR or the unit has reported.</summary>
    private bool FailureKnown(CauseRecord failure)
        => RecordKnown(failure) || _records.Any(r => r.Kind == RecordKind.CorReport && r.CausedBy.Contains(failure.Id));

    private static string Possessive(string name) => name.EndsWith('s') ? name + "'" : name + "'s";
}
