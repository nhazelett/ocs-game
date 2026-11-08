using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

public sealed partial class Session
{
    private static readonly HashSet<string> AwardableInM0 = ["sf44", "gpc", "purchaseOrder", "bpaCall", "uca"];

    private sealed record AwardPayload(AwardAction Action, string SiteId, string CapabilityId, List<string> Lines,
        long DailyCostCents, long MobilizationCents, long TotalCents, int StartLag, int LeadTime, int ReliabilityPermille, string SupplierIsland);

    /// <summary>
    /// Applies an action on the current day. Hard stops (legal limits, missing prerequisites) reject it and change nothing
    /// except a Rejected record. Soft gaps (a line left off, no overlap) are allowed; the world decides what they cost.
    /// </summary>
    public ActionResult Apply(PlayerAction action)
    {
        if (Finished) return ActionResult.Stop("the scenario has ended");
        if (action.Day != 0 && action.Day != Day) return ActionResult.Stop($"action is dated day {action.Day} but the current day is {Day}");
        action = action with { Day = Day };
        var result = action switch
        {
            SurveyAction s => ApplySurvey(s),
            SetLineAction s => ApplySetLine(s),
            AwardAction a => ApplyAward(a),
            CoordinateAction c => ApplyCoordinate(c),
            DefinitizeAction d => ApplyDefinitize(d),
            TerminateAction t => ApplyTerminate(t),
            _ => ActionResult.Stop($"unknown action {action.GetType().Name}"),
        };
        _applied.Add(action);
        if (!result.Accepted)
        {
            _rejected++;
            AddRecord(Day, RecordKind.Rejected, Describe(action), $"Rejected: {result.Message}", []);
        }
        return result;
    }

    internal static string Describe(PlayerAction a) => a switch
    {
        SurveyAction s => $"survey {s.Island}",
        SetLineAction s => $"{s.Card}.{s.Line}={(s.On ? "on" : "off")}",
        AwardAction a2 => $"{a2.Instrument} {a2.Id}",
        CoordinateAction c => $"coordinate {c.Id}",
        DefinitizeAction d => $"definitize {d.Arrangement}",
        TerminateAction t => $"terminate {t.Arrangement}",
        _ => a.GetType().Name,
    };

    private long Hours(int shopDays, bool written)
    {
        var milli = shopDays * Fixed.Milli(Rules.Model.HoursPerPersonPerDay);
        if (written && Day < _systemOnlineDay) milli = Fixed.MulPermille(milli, _manualFactorPermille);
        return milli;
    }

    private WorkItem AddWork(WorkKind kind, string label, long hoursMilli, int waitUntil, object? payload, string causeId)
    {
        var item = new WorkItem
        {
            Id = $"w{++_workSeq}", Kind = kind, Label = label, HoursTotalMilli = hoursMilli, HoursRemainingMilli = hoursMilli,
            WaitUntilDay = waitUntil, CreatedDay = Day, Payload = payload, CauseId = causeId,
        };
        _work.Add(item);
        return item;
    }

    // ------------------------------------------------------------------ survey

    private ActionResult ApplySurvey(SurveyAction s)
    {
        var k = _knowledge.FirstOrDefault(x => x.IslandId == s.Island);
        if (k is null) return ActionResult.Stop($"island {s.Island} does not exist");
        if (k.Tier is "surveyed" or "observed") return ActionResult.Stop($"{IslandData(s.Island).Name} is already surveyed");
        if (k.SurveyReturnsDay is not null || _work.Any(w => w.Kind == WorkKind.Survey && w.CompletedDay is null && (string?)w.Payload == s.Island))
            return ActionResult.Stop($"a survey of {IslandData(s.Island).Name} is already in work");
        var rec = AddRecord(Day, RecordKind.Action, $"survey {s.Island}", $"Ordered a site survey of {IslandData(s.Island).Name}.", []);
        AddWork(WorkKind.Survey, $"Survey {IslandData(s.Island).Name}", Fixed.Milli(Rules.Model.Work.SurveyShopHours), Day, s.Island, rec);
        _surveys++;
        return ActionResult.Ok("survey ordered");
    }

    // ------------------------------------------------------------------ OCS seat

    private ActionResult ApplySetLine(SetLineAction s)
    {
        var card = _cards.FirstOrDefault(c => c.Id == s.Card);
        if (card is null) return ActionResult.Stop($"card {s.Card} does not exist");
        if (Day < card.SubmittedDay) return ActionResult.Stop($"card {s.Card} has not been submitted yet");
        if (!Cap(card.CapabilityId).Lines.Any(l => l.Id == s.Line)) return ActionResult.Stop($"{s.Line} is not a {card.CapabilityId} line");
        card.Lines[s.Line] = s.On;
        AddRecord(Day, RecordKind.Action, s.Card, $"Requirement {s.Card}: {LineLabel(card.CapabilityId, s.Line)} switched {(s.On ? "on" : "off")}.", []);
        return ActionResult.Ok("line set");
    }

    internal string LineLabel(string cap, string line) => Cap(cap).Lines.FirstOrDefault(l => l.Id == line)?.Label ?? line;

    // ------------------------------------------------------------------ awards

    private ActionResult ApplyAward(AwardAction a)
    {
        var stops = new List<string>();
        if (string.IsNullOrWhiteSpace(a.Id)) stops.Add("an award needs an id");
        else if (_arrangements.Any(x => x.Id == a.Id) || _work.Any(w => w.Payload is AwardPayload p && p.Action.Id == a.Id))
            stops.Add($"id {a.Id} is already used");
        if (!Rules.Instruments.TryGetValue(a.Instrument, out var inst)) return ActionResult.Stop($"instrument {a.Instrument} is not in the rules");
        if (!AwardableInM0.Contains(a.Instrument)) return ActionResult.Stop($"{inst.Label} is not implemented in this build");

        CardState? card = null;
        if (a.Card is not null)
        {
            card = _cards.FirstOrDefault(c => c.Id == a.Card);
            if (card is null) return ActionResult.Stop($"card {a.Card} does not exist");
            if (Day < card.SubmittedDay) return ActionResult.Stop($"card {a.Card} has not been submitted yet");
        }
        var siteId = a.Site ?? card?.SiteId;
        var capId = a.Capability ?? card?.CapabilityId;
        if (siteId is null || capId is null) return ActionResult.Stop("an award needs a card, or a site and a capability");
        if (!Scenario.Sites.Any(s => s.Id == siteId)) return ActionResult.Stop($"site {siteId} does not exist");
        if (!_capData.ContainsKey(capId)) return ActionResult.Stop($"capability {capId} does not exist");
        var lines = a.Lines ?? card?.Lines.Where(kv => kv.Value).Select(kv => kv.Key).ToList()
                    ?? (_capData.TryGetValue(capId, out var cd) ? cd.Lines.Where(l => l.Kind == "core").Select(l => l.Id).ToList() : []);
        if (lines.Count == 0) return ActionResult.Stop("no scope lines selected");
        foreach (var l in lines)
            if (!Cap(capId).Lines.Any(x => x.Id == l)) stops.Add($"{l} is not a {capId} line");

        var vendor = VendorData(a.Vendor);
        if (vendor is null) return ActionResult.Stop($"vendor {a.Vendor} does not exist");
        var offer = vendor.Offers.FirstOrDefault(o => o.Capability == capId);
        if (offer is null) return ActionResult.Stop($"{vendor.Name} does not offer {CapLabel(capId)}");
        foreach (var l in lines)
            if (!offer.Lines.Contains(l)) stops.Add($"{vendor.Name} does not offer {LineLabel(capId, l)}");
        if (a.Quantity <= 0) stops.Add("quantity must be positive");
        if (a.DurationDays <= 0) stops.Add("duration must be positive");
        var capacity = Fixed.Milli(offer.CapacityPerDay);
        var committed = _arrangements.Where(x => x.VendorId == vendor.Id && x.CapabilityId == capId
                                                 && x.Status is ArrangementStatus.Pending or ArrangementStatus.Active)
                                     .Sum(x => x.QuantityMilli)
                      + _work.Where(w => w.CompletedDay is null && w.Payload is AwardPayload p && p.Action.Vendor == vendor.Id && p.CapabilityId == capId)
                             .Sum(w => ((AwardPayload)w.Payload!).Action.Quantity * 1000);
        if (a.Quantity * 1000 + committed > capacity)
            stops.Add($"{vendor.Name} can supply {offer.CapacityPerDay:0} {Cap(capId).Unit}/day in total and {committed / 1000} is already committed");
        if (vendor.VehicleOnly && a.Instrument != "bpaCall") stops.Add($"{vendor.Name} is reachable only through its vehicle");

        var daily = a.Quantity * offer.UnitPriceCents;
        var baseCost = daily * Math.Max(0, a.DurationDays);
        var mob = Fixed.MulPermille(baseCost, Fixed.Permille(offer.MobilizationFraction));
        if (a.Instrument == "gpc")
        {
            var sur = Fixed.Permille(vendor.GpcSurchargeFraction);
            daily += Fixed.MulPermille(daily, sur);
            baseCost += Fixed.MulPermille(baseCost, sur);
            mob += Fixed.MulPermille(mob, sur);
        }
        var total = baseCost + mob;

        // Legal and practical limits by instrument. These are hard stops.
        switch (inst.LimitBasis)
        {
            case "microPurchase" when total > Rules.Thresholds.MicroPurchaseCents:
                stops.Add($"{Money(total)} exceeds the micro-purchase threshold of {Money(Rules.Thresholds.MicroPurchaseCents)}"); break;
            case "simplifiedAcquisition" when total > Rules.Thresholds.SimplifiedAcquisitionCents:
                stops.Add($"{Money(total)} exceeds the simplified acquisition threshold of {Money(Rules.Thresholds.SimplifiedAcquisitionCents)}"); break;
        }
        if (_warrantCents is { } warrant && total > warrant) stops.Add($"{Money(total)} exceeds the warrant of {Money(warrant)}");

        switch (a.Instrument)
        {
            case "sf44":
                if (_payingAgentDay is null || Day < _payingAgentDay) stops.Add("no paying agent with cash on hand yet");
                if (!vendor.AcceptsCash) stops.Add($"{vendor.Name} does not take cash");
                if (total > _cash) stops.Add($"cash on hand is {Money(_cash)}");
                break;
            case "gpc":
                if (_gpcAvailableDay is null || Day < _gpcAvailableDay) stops.Add("no government purchase card accounts yet");
                if (!vendor.AcceptsGpc) stops.Add($"{vendor.Name} does not accept the card");
                if (total > _gpcSingle) stops.Add($"{Money(total)} exceeds the card's single-purchase limit of {Money(_gpcSingle)}");
                var month = _gpcSpend.Where(s => s.Day > Day - 30).Sum(s => s.Cents);
                if (month + total > _gpcMonthly) stops.Add($"card spend this month would be {Money(month + total)} against a limit of {Money(_gpcMonthly)}");
                break;
            case "bpaCall":
                var veh = Scenario.Vehicles.FirstOrDefault(v => v.Id == a.Vehicle);
                if (veh is null) { stops.Add("a BPA call needs a BPA vehicle"); break; }
                if (veh.Kind != "bpa") stops.Add($"{veh.Id} is not a BPA");
                if (!veh.VisibleOnDay1) stops.Add($"{veh.Id} is not known to this shop");
                if (veh.Vendor != vendor.Id) stops.Add($"{veh.Id} is with a different vendor");
                if (Day > veh.OrderingPeriodEndDay) stops.Add($"{veh.Id}'s ordering period ended on day {veh.OrderingPeriodEndDay}");
                foreach (var l in lines) if (!veh.ScopeTags.Contains(l)) stops.Add($"{LineLabel(capId, l)} is outside {veh.Id}'s scope");
                if (veh.CallLimitCents is { } lim && total > lim) stops.Add($"{Money(total)} exceeds {veh.Id}'s call limit of {Money(lim)}");
                if (veh.PlaceOfPerformance.Count > 0 && !veh.PlaceOfPerformance.Contains(SiteData(siteId).Island))
                    stops.Add($"{SiteName(siteId)} is outside {veh.Id}'s place of performance");
                break;
        }
        if (inst.RequiresSystemOrManual)
        {
            var line = _funding.FirstOrDefault(f => f.Id == a.FundingLine);
            if (line is null) stops.Add("a written action needs a funding line");
            else if (!line.UsableOn(Day)) stops.Add($"funding line {line.Id} is not usable on day {Day}");
            else if (a.Instrument != "uca" && line.RemainingCents < total) stops.Add($"funding line {line.Id} has {Money(line.RemainingCents)} left");
            else if (a.Instrument == "uca" && line.RemainingCents <= 0) stops.Add($"funding line {line.Id} is empty");
        }
        if (stops.Count > 0) return ActionResult.Stop([.. stops]);

        var lead = offer.LeadTimeDays;
        var startLag = _rngWork.NextInt(inst.StartLagDays);
        var payload = new AwardPayload(a, siteId, capId, lines, daily, mob, total, startLag, lead, Fixed.Permille(vendor.Reliability), vendor.Island);
        var where = $"{CapLabel(capId)} at {SiteName(siteId)}";
        var lineText = string.Join(", ", lines.Select(l => LineLabel(capId, l)));

        switch (a.Instrument)
        {
            case "sf44":
            case "gpc":
            {
                if (a.Instrument == "sf44") { _cash -= total; ScheduleReplenish(); }
                else _gpcSpend.Add((Day, total));
                var rec = AddRecord(Day, RecordKind.Action, a.Id, $"{inst.Label} to {vendor.Name}: {where}, {a.Quantity} {Cap(capId).Unit}/day for {a.DurationDays} days ({lineText}); {Money(total)} paid on the spot.", []);
                CompleteAward(payload, Day, rec);
                return ActionResult.Ok("bought");
            }
            default:
            {
                var shopDays = _rngWork.NextInt(inst.ShopDays);
                var wait = Day + Scenario.Actors.Fm.CertificationLagDays;
                var approval = "";
                if (a.Instrument == "uca")
                {
                    var lag = _rngWork.NextInt(Rules.Uca.ApprovalLagDays);
                    wait += lag;
                    approval = $" Approval request routed through the SCO; answer expected in {lag} day(s).";
                }
                var rec = AddRecord(Day, RecordKind.Action, a.Id,
                    $"{inst.Label} started with {vendor.Name}: {where}, {a.Quantity} {Cap(capId).Unit}/day for {a.DurationDays} days ({lineText}); quoted {Money(total)}.{approval}", []);
                AddWork(WorkKind.Award, $"{inst.Label} {a.Id}", Hours(shopDays, inst.RequiresSystemOrManual), wait, payload, rec);
                return ActionResult.Ok("in work");
            }
        }
    }

    private void CompleteAward(AwardPayload p, int day, string causeId)
    {
        var a = p.Action;
        var inst = Rules.Instruments[a.Instrument];
        if (a.Instrument == "bpaCall")
        {
            var c = Rules.AggregateRates.BpaCallDeclineBase;
            var pm = Fixed.Decay(Fixed.Permille(c.Start), Fixed.Permille(c.Floor), (int)Math.Round(c.HalfLifeDays), day);
            if (_rngAggregate.Roll(pm))
            {
                _bpaDeclines++;
                _arrangements.Add(new Arrangement
                {
                    Id = a.Id, Instrument = a.Instrument, VendorId = a.Vendor, VehicleId = a.Vehicle, SiteId = p.SiteId, CapabilityId = p.CapabilityId,
                    Lines = p.Lines, SupplierIslandId = p.SupplierIsland, Status = ArrangementStatus.Declined, CreatedDay = day, CardId = a.Card, CauseId = causeId,
                });
                AddRecord(day, RecordKind.Declined, a.Id, $"{VendorData(a.Vendor)!.Name} declined BPA call {a.Id}. A BPA is not binding; the vendor may decline any call.", [causeId]);
                return;
            }
        }
        long obligated = 0;
        if (inst.RequiresSystemOrManual && a.Instrument != "uca")
        {
            var line = _funding.First(f => f.Id == a.FundingLine);
            if (!line.UsableOn(day) || line.RemainingCents < p.TotalCents)
            {
                AddRecord(day, RecordKind.Rejected, a.Id, $"{inst.Label} {a.Id} could not be awarded: funding line {line.Id} could not cover {Money(p.TotalCents)} on day {day}.", [causeId]);
                return;
            }
            line.ObligatedCents += p.TotalCents;
            obligated = p.TotalCents;
        }
        else if (a.Instrument is "sf44" or "gpc") obligated = p.TotalCents;

        var earliest = day + 1 + p.StartLag + p.LeadTime;
        var start = Math.Max(a.StartDay ?? earliest, earliest);
        var arr = new Arrangement
        {
            Id = a.Id, Instrument = a.Instrument, VendorId = a.Vendor, VehicleId = a.Vehicle, SiteId = p.SiteId, CapabilityId = p.CapabilityId,
            Lines = p.Lines, SupplierIslandId = p.SupplierIsland, QuantityMilli = a.Quantity * 1000, DailyCostCents = p.DailyCostCents,
            MobilizationCents = p.MobilizationCents, ObligatedCents = obligated, FundingLineId = a.FundingLine,
            ReliabilityPermille = p.ReliabilityPermille, CreatedDay = day, CardId = a.Card, CauseId = causeId,
            StartDay = start, EndDay = start + a.DurationDays - 1,
        };
        foreach (var lineId in p.Lines)
        {
            var line = Cap(p.CapabilityId).Lines.First(l => l.Id == lineId);
            if (line is { Kind: "supplies", Supplies: { } target } && CapState(p.SiteId, target) is not null)
                arr.SuppliedQuantityMilli[target] = Fixed.MulPermille(PeakDemandMilli(p.SiteId, target, arr.StartDay, arr.EndDay), Rules.Model.SupplyLineSizingPermille);
        }
        if (a.Instrument == "uca")
        {
            var vendor = VendorData(a.Vendor)!;
            var qpLag = _rngUca.NextInt(vendor.QualifyingProposalLagDays ?? Rules.Uca.QualifyingProposalLagDays);
            arr.Uca = new UcaState
            {
                NteCents = a.NteCents ?? Fixed.MulPermille(p.TotalCents, Rules.Uca.DefaultNtePermilleOfQuote),
                QuotedTotalCents = p.TotalCents, IssuedDay = day, QualifyingProposalDay = day + qpLag,
            };
        }
        _arrangements.Add(arr);
        var tail = arr.Uca is { } u
            ? $" UCA issued with NTE {Money(u.NteCents)}; obligations capped at {Rules.Uca.ObligationCapBeforeProposal * 100:0}% of NTE until a qualifying proposal."
            : $" Obligated {Money(obligated)}.";
        AddRecord(day, RecordKind.WorkDone, a.Id, $"{inst.Label} {a.Id} awarded to {VendorData(a.Vendor)!.Name}; performance starts day {arr.StartDay}, ends day {arr.EndDay}.{tail}", [causeId]);
    }

    private void ScheduleReplenish()
    {
        if (_cashReplenishDay is null && _cash * 1000 < _cashInitial * Rules.Model.CashReplenishThresholdPermille)
        {
            _cashReplenishDay = Day + _cashReplenishLagDays;
            AddRecord(Day, RecordKind.Cash, "cash", $"Cash on hand down to {Money(_cash)}; FM replenishment expected day {_cashReplenishDay}.", []);
        }
    }

    // ------------------------------------------------------------------ coordination

    private ActionResult ApplyCoordinate(CoordinateAction c)
    {
        var stops = new List<string>();
        if (string.IsNullOrWhiteSpace(c.Id) || _arrangements.Any(x => x.Id == c.Id)) stops.Add("coordination needs a new id");
        if (!Scenario.Sites.Any(s => s.Id == c.Site)) stops.Add($"site {c.Site} does not exist");
        if (!Scenario.Sites.Any(s => s.Id == c.FromSite)) stops.Add($"site {c.FromSite} does not exist");
        if (!_capData.ContainsKey(c.Capability)) stops.Add($"capability {c.Capability} does not exist");
        if (c.Quantity <= 0) stops.Add("quantity must be positive");
        if (stops.Count > 0) return ActionResult.Stop([.. stops]);
        var from = SiteData(c.FromSite);
        var lag = _rngWork.NextInt(Rules.Instruments["coordination"].StartLagDays);
        var rec = AddRecord(Day, RecordKind.Action, c.Id, $"Coordinated with the {from.Name} unit for {c.Quantity} {Cap(c.Capability).Unit}/day of {CapLabel(c.Capability)} to {SiteName(c.Site)}. Not a contract; they can withdraw it.", []);
        var core = Cap(c.Capability).Lines.First(l => l.Kind == "core").Id;
        _arrangements.Add(new Arrangement
        {
            Id = c.Id, Instrument = "coordination", SiteId = c.Site, CapabilityId = c.Capability, Lines = [core],
            SupplierIslandId = from.Island, QuantityMilli = c.Quantity * 1000, ReliabilityPermille = 950, CreatedDay = Day, CauseId = rec,
            StartDay = Day + lag, EndDay = LastDay,
        });
        return ActionResult.Ok("coordinated");
    }

    // ------------------------------------------------------------------ UCA definitization

    private ActionResult ApplyDefinitize(DefinitizeAction d)
    {
        var arr = _arrangements.FirstOrDefault(x => x.Id == d.Arrangement);
        if (arr?.Uca is not { } u) return ActionResult.Stop($"{d.Arrangement} is not a UCA");
        if (u.Definitized) return ActionResult.Stop($"{d.Arrangement} is already definitized");
        if (u.DefinitizationInWork) return ActionResult.Stop($"definitization of {d.Arrangement} is already in work");
        if (!u.ProposalReceived) return ActionResult.Stop($"{VendorData(arr.VendorId)!.Name} has not submitted a qualifying proposal yet");
        if (!_funding.Any(f => f.Id == d.FundingLine)) return ActionResult.Stop($"funding line {d.FundingLine} does not exist");
        u.DefinitizationInWork = true;
        var rec = AddRecord(Day, RecordKind.Action, d.Arrangement, $"Definitization of {d.Arrangement} started; to be funded from {d.FundingLine}.", []);
        AddWork(WorkKind.Definitize, $"Definitize {d.Arrangement}", Hours(_rngWork.NextInt(Rules.Uca.DefinitizationShopDays), true), Day, d, rec);
        return ActionResult.Ok("definitization in work");
    }

    // ------------------------------------------------------------------ termination

    private ActionResult ApplyTerminate(TerminateAction t)
    {
        var arr = _arrangements.FirstOrDefault(x => x.Id == t.Arrangement);
        if (arr is null) return ActionResult.Stop($"{t.Arrangement} does not exist");
        if (arr.Status is ArrangementStatus.Ended or ArrangementStatus.Declined) return ActionResult.Stop($"{t.Arrangement} is not running");
        if (_work.Any(w => w.Kind == WorkKind.Terminate && w.CompletedDay is null && (string?)w.Payload == t.Arrangement))
            return ActionResult.Stop($"termination of {t.Arrangement} is already in work");
        var rec = AddRecord(Day, RecordKind.Action, t.Arrangement, $"Termination of {t.Arrangement} started.", []);
        var hours = arr.IsCoordination ? 0 : Hours(Rules.Model.Work.TerminationShopDays, true);
        AddWork(WorkKind.Terminate, $"Terminate {t.Arrangement}", hours, Day, t.Arrangement, rec);
        return ActionResult.Ok("termination in work");
    }

    // ------------------------------------------------------------------ work completion

    private void CompleteWork(WorkItem w, int day)
    {
        w.CompletedDay = day;
        switch (w.Kind)
        {
            case WorkKind.Award:
                CompleteAward((AwardPayload)w.Payload!, day, w.CauseId);
                break;
            case WorkKind.Survey:
            {
                var island = (string)w.Payload!;
                var k = _knowledge.First(x => x.IslandId == island);
                k.SurveyReturnsDay = day + Rules.Model.Work.SurveyReturnDays;
                AddRecord(day, RecordKind.WorkDone, $"survey {island}", $"Survey team dispatched to {IslandData(island).Name}; findings due day {k.SurveyReturnsDay}.", [w.CauseId]);
                break;
            }
            case WorkKind.Definitize:
            {
                var d = (DefinitizeAction)w.Payload!;
                var arr = _arrangements.First(x => x.Id == d.Arrangement);
                arr.Uca!.Definitized = true;
                arr.Uca.DefinitizationInWork = false;
                arr.FundingLineId = d.FundingLine;
                AddRecord(day, RecordKind.Definitized, arr.Id,
                    $"{arr.Id} definitized at {Money(arr.Uca.QuotedTotalCents)}. Obligation caps lifted; obligations still need money on line {d.FundingLine}.", [w.CauseId]);
                break;
            }
            case WorkKind.Ratification:
                _ratifications++;
                AddRecord(day, RecordKind.Compliance, (string)w.Payload!, $"Ratification package for {(string)w.Payload!} completed.", [w.CauseId]);
                break;
            case WorkKind.PaymentRework:
                AddRecord(day, RecordKind.WorkDone, (string)w.Payload!, $"Payment error on {(string)w.Payload!} corrected.", [w.CauseId]);
                break;
            case WorkKind.HhqResponse:
                AddRecord(day, RecordKind.WorkDone, (string)w.Payload!, $"Response to higher HQ on {(string)w.Payload!} sent.", [w.CauseId]);
                break;
            case WorkKind.Terminate:
            {
                var arr = _arrangements.First(x => x.Id == (string)w.Payload!);
                arr.EndDay = Math.Min(arr.EndDay, day);
                if (arr.Status == ArrangementStatus.Pending) arr.Status = ArrangementStatus.Ended;
                AddRecord(day, RecordKind.ArrangementEnd, arr.Id, $"{arr.Id} terminated; performance ends day {arr.EndDay}.", [w.CauseId]);
                break;
            }
        }
    }

    internal static string Money(long cents) => (cents / 100m).ToString("$#,##0", System.Globalization.CultureInfo.InvariantCulture);
}
