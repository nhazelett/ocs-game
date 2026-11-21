using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// D37 (DESK-97; FOR-NICK #20 (a)): a running contract can take the part it left out. One action, on the contract's own
// page: a few hours of shop time to write the modification and the part's price for the days left, obligated when it is
// signed. From the day after, the vendor brings the part and the failures it causes stop; failures before stay as they
// were. Left out: quantity, dates or price of what is bought, taking a part off, a part the vendor does not sell, an
// approval chain, and a modifications screen.
public sealed partial class Session
{
    private static readonly HashSet<string> Modifiable = ["purchaseOrder", "bpaCall", "idiqOrder"];

    /// <summary>DESK-195 (Claude play 21 Nov, eleventh play, note P2-11): the request a contract is changed by. A contract
    /// bought on the office's own initiative has no request of its own, so it is the request on the desk for its site and
    /// capability whose days overlap it; null when there is none.</summary>
    private CardState? RequestFor(Arrangement a)
        => a.CardId is not null ? _cards.FirstOrDefault(c => c.Id == a.CardId)
            : _cards.Where(c => c.SubmittedDay <= Day && c.SiteId == a.SiteId && c.CapabilityId == a.CapabilityId
                                && c.RequestedStartDay <= a.EndDay && c.RequestedStartDay + Math.Max(1, c.DurationDays) - 1 >= a.StartDay)
                    .OrderBy(c => c.RequestedStartDay).FirstOrDefault();

    /// <summary>The parts (line ids) this arrangement could take today: operators or maintenance that its request has,
    /// that it left out, and that its vendor sells; or why none can be added, as one plain sentence.</summary>
    private (List<string> Parts, string? Why) AddableParts(Arrangement? a)
    {
        if (a is null) return ([], "That contract does not exist.");
        if (NotOursToEnd(a) is not null)
            return ([], $"{Capitalize(NameOf(a.Id))} is not this office's contract, so it is not this office's to change.");
        if (a.Instrument == "gpc") return ([], "A card buy cannot be changed. Buy what it left out on its own, or cancel it and buy again.");
        if (a.Instrument == "sf44") return ([], "A cash buy cannot be changed. Buy what it left out on its own.");
        if (a.Uca is not null && !a.Uca.Definitized) return ([], "A letter contract is changed when it is definitized, not before.");
        if (!Modifiable.Contains(a.Instrument)) return ([], $"{Capitalize(NameOf(a.Id))} cannot be changed from this office.");
        if (a.Status is not (ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended) || a.EndDay < Day)
            return ([], $"{Capitalize(NameOf(a.Id))} is not running.");
        if (_work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == a.Id))
            return ([], $"{Capitalize(NameOf(a.Id))} is being ended.");
        if (_work.Any(w => w.Kind == WorkKind.Modify && w.CompletedDay is null && ((ModifyAction)w.Payload!).Arrangement == a.Id))
            return ([], $"A change to {NameOf(a.Id)} is already being written.");
        var cap = Cap(a.CapabilityId);
        var card = RequestFor(a);   // DESK-195
        var has = LinesOn(a, int.MaxValue).ToHashSet();
        var leftOut = cap.Lines.Where(l => l.Kind is "operator" or "maintenance" && !has.Contains(l.Id)).ToList();
        if (leftOut.Count == 0) return ([], $"{Capitalize(NameOf(a.Id))} already brings everything it can.");
        var asked = leftOut.Where(l => card is not null && card.Lines.TryGetValue(l.Id, out var on) && on).ToList();
        if (asked.Count == 0)
            return ([], card is null
                ? $"Nothing on your desk asks for {JoinWords(leftOut.Select(l => (l.Label ?? l.Id).ToLowerInvariant()).ToList())} at {SiteName(a.SiteId)}."
                : $"The request does not ask for {JoinWords(leftOut.Select(l => (l.Label ?? l.Id).ToLowerInvariant()).ToList())}. Find out what it needs first.");
        var offer = VendorData(a.VendorId)?.Offers.FirstOrDefault(o => o.Capability == a.CapabilityId);
        var sold = asked.Where(l => offer is not null && offer.Lines.Contains(l.Id)).ToList();
        if (sold.Count == 0)
            return ([], $"{VendorData(a.VendorId)?.Name ?? "The vendor"} does not offer {JoinWords(asked.Select(l => (l.Label ?? l.Id).ToLowerInvariant()).ToList())}. Buy that from someone who does.");
        return (sold.Select(l => l.Id).ToList(), null);
    }

    private (Arrangement? Arr, List<string> Parts, List<string> Stops) CheckModify(ModifyAction m)
    {
        var a = _arrangements.FirstOrDefault(x => x.Id == m.Arrangement);
        var (addable, why) = AddableParts(a);
        if (why is not null) return (a, [], [why]);
        var parts = m.Lines.Count == 0 ? addable : m.Lines.Distinct().ToList();
        var stops = parts.Where(l => !addable.Contains(l))
                         .Select(l => $"{LineLabel(a!.CapabilityId, l)} cannot be added to {NameOf(a.Id)}.").ToList();
        return (a, parts, stops);
    }

    /// <summary>A day of the added parts: each a share of the contract's daily price (rules modificationPartPricePermille).</summary>
    private long PartsDaily(Arrangement a, int parts) => Fixed.MulPermille(a.DailyCostCents, Rules.Model.Work.ModificationPartPricePermille) * parts;

    /// <summary>The parts' price for the days from <paramref name="fromDay"/> to the contract's end.</summary>
    private long PartsPrice(Arrangement a, int parts, int fromDay)
        => PartsDaily(a, parts) * Math.Max(0, a.EndDay - Math.Max(fromDay, a.StartDay) + 1);

    /// <summary>What the added parts cost through a day, for a termination that keeps what was delivered.</summary>
    private long AddedPartsCost(Arrangement a, int throughDay)
        => a.Added.Sum(x => x.DailyCents * Math.Max(0, throughDay - Math.Max(x.FromDay, a.StartDay) + 1));

    private ActionPreview BuildModifyPreview(ModifyAction m)
    {
        var (a, parts, stops) = CheckModify(m);
        var h = Fixed.Milli(Rules.Model.Work.ModificationShopHours);
        var p = WorkPreview(m, stops, h, h, 1);   // the result day is the first day the vendor brings the parts
        if (stops.Count > 0 || a is null) return p;
        var price = PartsPrice(a, parts.Count, p.EarliestStartMin);
        var line = _funding.FirstOrDefault(f => f.Id == a.FundingLineId);
        if (price > 0 && (line is null || !line.UsableOn(p.EarliestStartMax - 1) || line.RemainingCents < price))
            return p with { Accepted = false, HardStops = [$"{FundingShortReason(line, price, p.EarliestStartMax - 1)}."] };
        return p with { DailyCents = PartsDaily(a, parts.Count), TotalCents = price };
    }

    private ActionResult ApplyModify(ModifyAction m)
    {
        var preview = BuildModifyPreview(m);
        if (!preview.Accepted) return ActionResult.Stop(preview.HardStops[0]);
        var (a, parts, _) = CheckModify(m);
        var words = JoinWords(parts.Select(l => LineLabel(a!.CapabilityId, l).ToLowerInvariant()).ToList());
        var rec = AddRecord(Day, RecordKind.Action, a!.Id,
            $"Add {words} to {NameOf(a.Id)}: a change to the contract, {Plural(Rules.Model.Work.ModificationShopHours, "shop hour")} to write; about {Money(preview.TotalCents)} for the days left.", [a.CauseId]);
        AddWork(WorkKind.Modify, $"Add {words} to {NameOf(a.Id)}", Fixed.Milli(Rules.Model.Work.ModificationShopHours), Day,
            m with { Lines = parts }, rec);
        return ActionResult.Ok("modification in work");
    }

    /// <summary>Signed: the parts come from the next day, their price for the days left is obligated from the contract's
    /// line as far as it goes (the rest waits on money, as a definitization does).</summary>
    private void CompleteModify(ModifyAction m, int day, string causeId)
    {
        var a = _arrangements.First(x => x.Id == m.Arrangement);
        var from = day + 1;
        if (a.Status is ArrangementStatus.Ended or ArrangementStatus.Declined || from > a.EndDay)
        {
            AddRecord(day, RecordKind.WorkDone, a.Id, $"The change to {NameOf(a.Id)} was not signed: it is no longer running.", [causeId]);
            return;
        }
        var daily = PartsDaily(a, 1);
        foreach (var l in m.Lines) a.Added.Add(new AddedPart(l, from, daily, causeId));
        var price = PartsPrice(a, m.Lines.Count, from);
        var line = _funding.FirstOrDefault(f => f.Id == a.FundingLineId);
        var add = line is not null && line.UsableOn(day) ? Math.Min(price, Math.Max(0, line.RemainingCents)) : 0;
        if (line is not null) line.ObligatedCents += add;
        a.ObligatedCents += add;
        var words = JoinWords(m.Lines.Select(l => LineLabel(a.CapabilityId, l).ToLowerInvariant()).ToList());
        var funded = add == price ? $"{Money(price)} obligated{(line is null ? "" : $" from {NameOf(line.Id)}")}"
                                  : $"{Money(add)} of {Money(price)} obligated; the rest waits on money";
        AddRecord(day, RecordKind.WorkDone, a.Id,
            $"{Upper(NameOf(a.Id))} changed: {VendorData(a.VendorId)?.Name ?? "the vendor"} brings {words} from day {from}; {funded}.", [causeId]);
    }

    /// <summary>D37 (DESK-139): the button is offered only where the buy left out a part the request could want. Off the page
    /// for a buy that is not this office's, not running or being ended, that already brings every part, or a card or cash buy
    /// whose request asks for nothing it left out. Everything else shows the button, greyed with its reason when it cannot.</summary>
    private bool ModifyOffered(Arrangement a)
    {
        if (NotOursToEnd(a) is not null) return false;
        if (a.Status is not (ArrangementStatus.Pending or ArrangementStatus.Active or ArrangementStatus.Suspended) || a.EndDay < Day) return false;
        if (_work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == a.Id)) return false;
        var has = LinesOn(a, int.MaxValue).ToHashSet();
        var leftOut = Cap(a.CapabilityId).Lines.Where(l => l.Kind is "operator" or "maintenance" && !has.Contains(l.Id)).ToList();
        if (leftOut.Count == 0) return false;
        if (Modifiable.Contains(a.Instrument) || a.Uca is not null) return true;
        var card = RequestFor(a);   // DESK-195
        return leftOut.Any(l => card is not null && card.Lines.TryGetValue(l.Id, out var on) && on);
    }

    /// <summary>The view of D37's button for one arrangement.</summary>
    internal ModifyView? BuildModifyView(string arrangementId, string? assignee)
    {
        var a = _arrangements.FirstOrDefault(x => x.Id == arrangementId);
        if (a is null || a.IsCoordination) return null;
        var added = a.Added.GroupBy(x => x.FromDay).OrderBy(g => g.Key)
                     .Select(g => $"{Capitalize(JoinWords(g.Select(x => LineLabel(a.CapabilityId, x.Line).ToLowerInvariant()).ToList()))} added from day {g.Key}.").ToList();
        var (parts, why) = AddableParts(a);
        var words = parts.Count == 0 ? "" : JoinWords(parts.Select(l => LineLabel(a.CapabilityId, l).ToLowerInvariant()).ToList());
        var button = parts.Count == 0 ? "Add to this contract" : $"Add {words} to this contract";
        if (why is not null) return new ModifyView(false, why, [], button, 0, 0, 0, 0, 0, null, added, ModifyOffered(a));
        var p = BuildModifyPreview(new ModifyAction { Arrangement = a.Id, Lines = parts, Assignee = assignee, Day = Day });
        var says = p.Accepted
            ? $"{Plural(p.ShopHoursMax, "hour")} to write the change; {Money(p.TotalCents)} for the days left, obligated when it is signed. {Capitalize(words)} from day {p.EarliestStartMin}{(p.EarliestStartMax > p.EarliestStartMin ? $" or {p.EarliestStartMax}" : "")}."
            : null;
        return new ModifyView(p.Accepted, p.Accepted ? null : p.HardStops[0], parts, button, p.ShopHoursMax, p.TotalCents,
            p.DailyCents, p.EarliestStartMin, p.EarliestStartMax, says, added);
    }
}
