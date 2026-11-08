using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

public sealed partial class Session
{
    private static readonly HashSet<string> AwardableInM0 = ["sf44", "gpc", "purchaseOrder", "bpaCall", "idiqOrder", "uca"];

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
            ResolveCommitmentAction r => ApplyResolve(r),
            EstablishBpaAction e => ApplyEstablishBpa(e),
            EstablishIdiqAction e => ApplyEstablishIdiq(e),
            CureNoticeAction c => ApplyCureNotice(c),
            DesignateCorAction c => ApplyDesignateCor(c),
            RequestExternalSupportAction r => ApplyRequestExternalSupport(r),
            AcsaOrderAction o => ApplyAcsaOrder(o),
            HostNationRequestAction h => ApplyHostNationRequest(h),
            PlanTransitionAction p => ApplyPlanTransition(p),
            _ => ActionResult.Stop($"unknown action {action.GetType().Name}"),
        };
        _applied.Add(action);
        _appliedRejected.Add(!result.Accepted);
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
        ResolveCommitmentAction r => $"{r.Choice} {r.Commitment}",
        EstablishBpaAction e => $"establish BPA {e.Id}",
        EstablishIdiqAction e => $"establish IDIQ {e.Id}",
        CureNoticeAction c => $"cure notice {c.Issue}",
        DesignateCorAction c => $"designate COR {c.Cor} on {c.Arrangement}",
        RequestExternalSupportAction r => $"external support request {r.Id}",
        AcsaOrderAction o => $"ACSA order {o.Id}",
        HostNationRequestAction h => $"host nation support request {h.Id}",
        PlanTransitionAction p => $"transition plan {p.Id}",
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

    /// <summary>Every check an award must pass, with the cost it would carry. Reads state only; draws no random numbers.</summary>
    internal AwardCheck CheckAward(AwardAction a)
    {
        var stops = new List<string>();
        if (string.IsNullOrWhiteSpace(a.Id)) stops.Add("an award needs an id");
        else if (_arrangements.Any(x => x.Id == a.Id) || _work.Any(w => w.Payload is AwardPayload p && p.Action.Id == a.Id))
            stops.Add($"id {a.Id} is already used");
        if (!Rules.Instruments.TryGetValue(a.Instrument, out var inst)) return AwardCheck.Fail($"instrument {a.Instrument} is not in the rules");
        if (!AwardableInM0.Contains(a.Instrument)) return AwardCheck.Fail($"{inst.Label} is not implemented in this build");

        CardState? card = null;
        if (a.Card is not null)
        {
            card = _cards.FirstOrDefault(c => c.Id == a.Card);
            if (card is null) return AwardCheck.Fail($"card {a.Card} does not exist");
            if (Day < card.SubmittedDay) return AwardCheck.Fail($"card {a.Card} has not been submitted yet");
        }
        var siteId = a.Site ?? card?.SiteId;
        var capId = a.Capability ?? card?.CapabilityId;
        if (siteId is null || capId is null) return AwardCheck.Fail("an award needs a card, or a site and a capability");
        if (!Scenario.Sites.Any(s => s.Id == siteId)) return AwardCheck.Fail($"site {siteId} does not exist");
        if (!_capData.ContainsKey(capId)) return AwardCheck.Fail($"capability {capId} does not exist");
        var lines = a.Lines ?? card?.Lines.Where(kv => kv.Value).Select(kv => kv.Key).ToList()
                    ?? (_capData.TryGetValue(capId, out var cd) ? cd.Lines.Where(l => l.Kind == "core").Select(l => l.Id).ToList() : []);
        if (lines.Count == 0) return AwardCheck.Fail("no scope lines selected");
        foreach (var l in lines)
            if (!Cap(capId).Lines.Any(x => x.Id == l)) stops.Add($"{l} is not a {capId} line");

        var vendor = VendorData(a.Vendor);
        if (vendor is null) return AwardCheck.Fail($"vendor {a.Vendor} does not exist");
        var offer = vendor.Offers.FirstOrDefault(o => o.Capability == capId);
        if (offer is null) return AwardCheck.Fail($"{vendor.Name} does not offer {CapLabel(capId)}");
        foreach (var l in lines)
            if (!offer.Lines.Contains(l)) stops.Add($"{vendor.Name} does not offer {LineLabel(capId, l)}");
        if (a.Quantity <= 0) stops.Add("quantity must be positive");
        if (a.DurationDays <= 0) stops.Add("duration must be positive");
        // Vendor capacity is shared across every arrangement that runs on the same days as this one.
        var capacity = Fixed.Milli(offer.CapacityPerDay);
        var newStart = Math.Max(a.StartDay ?? Day, Day);
        var newEnd = newStart + Math.Max(1, a.DurationDays) - 1;
        bool Overlaps(int s0, int e0) => s0 <= newEnd && e0 >= newStart;
        var committed = _arrangements.Where(x => x.VendorId == vendor.Id && x.CapabilityId == capId
                                                 && x.Status is ArrangementStatus.Pending or ArrangementStatus.Active && Overlaps(x.StartDay, x.EndDay))
                                     .Sum(x => x.QuantityMilli)
                      + _work.Where(w => w.CompletedDay is null && w.Payload is AwardPayload p && p.Action.Vendor == vendor.Id && p.CapabilityId == capId
                                         && Overlaps(p.Action.StartDay ?? w.CreatedDay, (p.Action.StartDay ?? w.CreatedDay) + Math.Max(1, p.Action.DurationDays) - 1))
                             .Sum(w => ((AwardPayload)w.Payload!).Action.Quantity * 1000);
        // An IDIQ order is binding on the vendor within the ceiling, so it is accepted above the vendor's capacity;
        // the shortfall plays out as a capacity event at delivery (M3-06).
        if (a.Instrument != "idiqOrder" && a.Quantity * 1000 + committed > capacity)
            stops.Add($"{vendor.Name} can supply {offer.CapacityPerDay:0} {Cap(capId).Unit}/day in total and {committed / 1000} is already committed");
        if (vendor.VehicleOnly && a.Instrument is not ("bpaCall" or "idiqOrder")) stops.Add($"{vendor.Name} is reachable only through its vehicle");

        if (_standing.TryGetValue(vendor.Id, out var standing) && standing.DeclinesWork)
            stops.Add($"{vendor.Name} declines work from this office after walking off an unpaid handshake");
        var daily = a.Quantity * UnitPriceFor(vendor, offer);
        var baseCost = daily * Math.Max(0, a.DurationDays);
        // An incumbent already on site and continuing the same work does not mobilize again.
        var incumbent = _arrangements.Any(x => x.VendorId == vendor.Id && x.SiteId == siteId && x.CapabilityId == capId
                                               && x.Status == ArrangementStatus.Active);
        var mob = incumbent ? 0 : Fixed.MulPermille(baseCost, Fixed.Permille(offer.MobilizationFraction));
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

        long fromGuarantee = 0;
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
                var veh = _vehicles.FirstOrDefault(v => v.Id == a.Vehicle);
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
            case "idiqOrder":
            {
                var idv = _vehicles.FirstOrDefault(v => v.Id == a.Vehicle);
                if (idv is null) { stops.Add("an IDIQ order needs an IDIQ vehicle"); break; }
                if (idv.Kind != "idiq") { stops.Add($"{idv.Id} is not an IDIQ"); break; }
                if (!idv.VisibleOnDay1) stops.Add($"{idv.Id} is not known to this shop");
                if (idv.Vendor != vendor.Id) stops.Add($"{idv.Id} is with a different vendor");
                if (Day > idv.OrderingPeriodEndDay) stops.Add($"{idv.Id}'s ordering period ended on day {idv.OrderingPeriodEndDay}");
                foreach (var l in lines) if (!idv.ScopeTags.Contains(l)) stops.Add($"{LineLabel(capId, l)} is outside {idv.Id}'s scope");
                if (idv.PlaceOfPerformance.Count > 0 && !idv.PlaceOfPerformance.Contains(SiteData(siteId).Island))
                    stops.Add($"{SiteName(siteId)} is outside {idv.Id}'s place of performance");
                var idq = Idiq(idv.Id)!;
                var pending = PendingOrderCents(idv.Id);
                if (idq.CeilingCents is { } ceiling && idq.OrderedCents + pending + total > ceiling)
                    stops.Add($"{Money(total)} would bring orders on {idv.Id} to {Money(idq.OrderedCents + pending + total)}, over its ceiling of {Money(ceiling)}");
                fromGuarantee = Math.Min(total, Math.Max(0, idq.GuaranteeUnorderedCents - pending));
                break;
            }
        }
        if (inst.RequiresSystemOrManual)
        {
            var need = total - fromGuarantee;
            var line = _funding.FirstOrDefault(f => f.Id == a.FundingLine);
            if (need == 0 && a.FundingLine is null && a.Instrument == "idiqOrder") { } // paid in full from the IDIQ's minimum guarantee
            else if (line is null) stops.Add("a written action needs a funding line");
            else if (!line.UsableOn(Day)) stops.Add($"funding line {line.Id} is not usable on day {Day}");
            else if (a.Instrument != "uca" && line.RemainingCents < need) stops.Add($"funding line {line.Id} has {Money(line.RemainingCents)} left");
            else if (a.Instrument == "uca" && line.RemainingCents <= 0) stops.Add($"funding line {line.Id} is empty");
            if (line is not null) stops.AddRange(FundingStops(line, capId, a.StartDay, a.DurationDays));
        }
        if (a.Cor is not null) stops.AddRange(CorStops(a.Cor, inst));
        stops.AddRange(OffIslandStops(a, vendor, capId));
        return new AwardCheck(stops, inst, card, vendor, offer, siteId, capId, lines, daily, mob, total);
    }

    private ActionResult ApplyAward(AwardAction a)
    {
        var chk = CheckAward(a);
        if (chk.Stops.Count > 0) return ActionResult.Stop([.. chk.Stops]);
        var inst = chk.Instrument!; var vendor = chk.Vendor!; var offer = chk.Offer!;
        var siteId = chk.SiteId!; var capId = chk.CapabilityId!; var lines = chk.Lines;
        long daily = chk.DailyCents, mob = chk.MobilizationCents, total = chk.TotalCents;

        var startLag = _rngWork.NextInt(inst.StartLagDays);
        // Off-island goods add strategic lift to the vendor's lead time, drawn after the work stream so local awards are unchanged.
        var lead = offer.LeadTimeDays + (vendor.OffIsland ? DrawLift(a.Lift!) : 0);
        var payload = new AwardPayload(a, siteId, capId, lines, daily, mob, total, startLag, lead, Fixed.Permille(vendor.Reliability), StockIsland(vendor, capId));
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
        long obligated = 0, fromGuarantee = 0;
        var idiq = a.Instrument == "idiqOrder" ? Idiq(a.Vehicle) : null;
        if (idiq is not null) fromGuarantee = Math.Min(p.TotalCents, idiq.GuaranteeUnorderedCents);
        if (inst.RequiresSystemOrManual && a.Instrument != "uca")
        {
            // An IDIQ order spends the minimum guarantee (obligated at award) before new money from the line.
            var need = p.TotalCents - fromGuarantee;
            var line = _funding.FirstOrDefault(f => f.Id == a.FundingLine);
            if (need > 0 && (line is null || !line.UsableOn(day) || line.RemainingCents < need))
            {
                var rej = AddRecord(day, RecordKind.Rejected, a.Id, $"{inst.Label} {a.Id} could not be awarded on day {day}: {FundingShortReason(line, need, day)}.", [causeId]);
                _fundingRejections.Add((a.Id, p.SiteId, p.CapabilityId, day, rej));
                return;
            }
            if (line is not null) line.ObligatedCents += need;
            obligated = p.TotalCents;
            if (idiq is not null) idiq.OrderedCents += p.TotalCents;
        }
        else if (a.Instrument is "sf44" or "gpc") obligated = p.TotalCents;

        var earliest = day + 1 + p.StartLag + p.LeadTime;
        var start = Math.Max(a.StartDay ?? earliest, earliest);
        var arr = new Arrangement
        {
            Id = a.Id, Instrument = a.Instrument, VendorId = a.Vendor, VehicleId = a.Vehicle, SiteId = p.SiteId, CapabilityId = p.CapabilityId,
            Lines = p.Lines, SupplierIslandId = p.SupplierIsland, QuantityMilli = a.Quantity * 1000, DailyCostCents = p.DailyCostCents,
            MobilizationCents = p.MobilizationCents, ObligatedCents = obligated, FundingLineId = a.FundingLine, FromGuaranteeCents = fromGuarantee,
            ReliabilityPermille = p.ReliabilityPermille, CreatedDay = day, CardId = a.Card, CauseId = causeId,
            StartDay = start, EndDay = start + a.DurationDays - 1, CorId = a.Cor,
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
        if (a.Lift is not null && VendorData(a.Vendor) is { OffIsland: true }) RegisterShipment(arr, a.Lift, day, causeId);
        var tail = arr.Uca is { } u
            ? $" UCA issued with NTE {Money(u.NteCents)}; obligations capped at {Rules.Uca.ObligationCapBeforeProposal * 100:0}% of NTE until a qualifying proposal."
            : fromGuarantee > 0
                ? $" Obligated {Money(obligated)}, {Money(fromGuarantee)} of it from {a.Vehicle}'s minimum guarantee."
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
        if (_funding.FirstOrDefault(f => f.Id == d.FundingLine) is not { } dl) return ActionResult.Stop($"funding line {d.FundingLine} does not exist");
        if (!dl.Pays(arr.CapabilityId)) return ActionResult.Stop([.. FundingStops(dl, arr.CapabilityId, null, 0)]);
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
                // The definitization modification obligates the definitized price with money current today.
                // Definitizing never creates money: if the line cannot cover it, the remainder waits on funding.
                var line = _funding.First(f => f.Id == d.FundingLine);
                var need = Math.Max(0, arr.Uca.QuotedTotalCents - arr.ObligatedCents - arr.Uca.PriorObligatedCents);
                var add = line.UsableOn(day) ? Math.Min(need, line.RemainingCents) : 0;
                arr.ObligatedCents += add;
                line.ObligatedCents += add;
                var funded = add == need ? $"fully funded from {line.Id}" : $"{Money(add)} of {Money(need)} funded from {line.Id}; the rest waits on money";
                AddRecord(day, RecordKind.Definitized, arr.Id,
                    $"{arr.Id} definitized at {Money(arr.Uca.QuotedTotalCents)}; obligation caps lifted; {funded}.", [w.CauseId]);
                break;
            }
            case WorkKind.Ratification:
                _ratifications++;
                if (!CompleteRatification((string)w.Payload!, day, w.CauseId))
                    AddRecord(day, RecordKind.Compliance, (string)w.Payload!, $"Ratification package for {(string)w.Payload!} completed.", [w.CauseId]);
                break;
            case WorkKind.CureNotice:
                CompleteCureNotice((string)w.Payload!, day, w.CauseId);
                break;
            case WorkKind.EstablishVehicle:
                if (w.Payload is EstablishIdiqAction idiqAction) CompleteEstablishIdiq(idiqAction, day, w.CauseId);
                else CompleteEstablishBpa((EstablishBpaAction)w.Payload!, day, w.CauseId);
                break;
            case WorkKind.AcsaOrder:
                CompleteAcsaOrder((AcsaPayload)w.Payload!, day, w.CauseId);
                break;
            case WorkKind.HostNationRequest:
                CompleteHostNationRequest((HostNationPayload)w.Payload!, day, w.CauseId);
                break;
            case WorkKind.ExternalSupportRequest:
                CompleteExternalRequestWork((string)w.Payload!, day, w.CauseId);
                break;
            case WorkKind.Denial:
                AddRecord(day, RecordKind.WorkDone, (string)w.Payload!, $"Denial of payment for {(string)w.Payload!} sent to the vendor: work outside the contract was not ordered by a contracting officer.", [w.CauseId]);
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
                // Deobligate the unperformed portion of a fully obligated award. It returns to its line only while
                // the line is still current; otherwise it lapses. UCAs obligate as they go, so nothing comes back.
                var tail = "";
                if (arr.Uca is null && arr.FundingLineId is not null && _funding.FirstOrDefault(f => f.Id == arr.FundingLineId) is { } fl)
                {
                    var performedDays = Math.Max(0, arr.EndDay - arr.StartDay + 1);
                    var ratifiedExtra = _commitments.Where(c => c.Kind == CommitmentKind.OutOfScope && c.ArrangementId == arr.Id && c.Decision == CommitmentDecision.Ratified)
                                                    .Sum(c => c.AmountCents);
                    // The part of an IDIQ order paid from the minimum guarantee stays spent: the guarantee is owed regardless.
                    var keep = Math.Min(arr.ObligatedCents, Math.Max(arr.FromGuaranteeCents,
                        arr.DailyCostCents * performedDays + (performedDays > 0 ? arr.MobilizationCents : 0) + ratifiedExtra));
                    var back = arr.ObligatedCents - keep;
                    if (back > 0)
                    {
                        arr.ObligatedCents = keep;
                        if (fl.UsableOn(day)) { fl.ObligatedCents -= back; tail = $" {Money(back)} deobligated back to {fl.Id}."; }
                        else tail = $" {Money(back)} deobligated; {fl.Id} has expired, so the money lapses.";
                    }
                }
                AddRecord(day, RecordKind.ArrangementEnd, arr.Id, $"{arr.Id} terminated; performance ends day {arr.EndDay}.{tail}", [w.CauseId]);
                break;
            }
        }
    }

    internal static string Money(long cents) => (cents / 100m).ToString("$#,##0", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>Result of <see cref="Session.CheckAward"/>. Stops empty means the award would be accepted today.</summary>
internal sealed record AwardCheck(
    IReadOnlyList<string> Stops, InstrumentData? Instrument, CardState? Card, VendorData? Vendor, VendorOfferData? Offer,
    string? SiteId, string? CapabilityId, List<string> Lines, long DailyCents, long MobilizationCents, long TotalCents)
{
    public static AwardCheck Fail(string reason) => new([reason], null, null, null, null, null, null, [], 0, 0, 0);
}
