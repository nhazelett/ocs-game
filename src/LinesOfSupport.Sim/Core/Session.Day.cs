namespace LinesOfSupport.Sim.Core;

public sealed partial class Session
{
    private sealed class Today
    {
        public int FactorPermille = 1000;          // multiplier on this arrangement's supply today
        public bool Failed, Confused, Suspended;
        public string? FailureRecord, ConfusionRecord;
    }

    /// <summary>Plays the current day. Fixed order: events, knowledge, UCA clocks, shop work, lifecycle, supply, money, score.</summary>
    public void AdvanceDay()
    {
        if (Finished) throw new InvalidOperationException("the scenario has ended");
        var d = Day;
        _overnightFrom = _records.Count;

        FireEvents(d);
        UnitHabits(d);
        ReturnSurveys(d);
        UcaClocks(d);
        CommitmentClocks(d);
        DoShopWork(d);
        if (_cashReplenishDay == d)
        {
            _cash = _cashInitial;
            _cashReplenishDay = null;
            AddRecord(d, RecordKind.Cash, "cash", $"FM replenished cash on hand to {Money(_cash)}.", []);
        }
        Lifecycle(d);
        var today = RollArrangements(d);
        ResolveCapabilities(d, today);
        Settle(d, today);
        Score(d);
        _overnightTo = _records.Count;
        Day++;
    }

    // ------------------------------------------------------------------ events and knowledge

    private void FireEvents(int d)
    {
        foreach (var e in Scenario.Events)
        {
            var (start, fired) = _eventSchedule[e.Id];
            if (fired || d < start || d > e.Window.LatestDay) continue;
            if (!e.Preconditions.All(p => Events.Holds(this, p, d))) continue;
            _eventSchedule[e.Id] = (start, true);
            _eventsFired++;
            var text = e.PlayerVisibleAs is { Length: > 0 } msg ? $"{e.Label}. Inbox: \"{msg}\"" : e.Label;
            var rec = AddRecord(d, RecordKind.Event, e.Id, text, []);
            _eventRecordIds[e.Id] = rec;
            foreach (var f in e.Effects) Events.Apply(this, f, d, rec);
            foreach (var follow in e.FollowOns)
                if (_eventSchedule.TryGetValue(follow, out var s) && !s.Fired)
                    _eventSchedule[follow] = (Math.Max(s.Start, d + 1), false);
        }
    }

    internal void CloseLink(string linkId, int fromDay, int days, string cause)
    {
        _linkClosedThrough[linkId] = Math.Max(_linkClosedThrough.GetValueOrDefault(linkId, 0), fromDay + Math.Max(1, days) - 1);
        _linkClosureCause[linkId] = cause;
    }

    internal void WithdrawCoordination(string siteId, string capId, int day, int noticeDays, string cause)
    {
        foreach (var a in _arrangements.Where(x => x.IsCoordination && x.SiteId == siteId && x.CapabilityId == capId
                                                   && x.Status is ArrangementStatus.Active or ArrangementStatus.Pending))
        {
            a.EndDay = Math.Min(a.EndDay, day + noticeDays - 1);
            AddRecord(day, RecordKind.Withdrawal, a.Id,
                $"The other unit withdrew coordination {a.Id} ({CapLabel(capId)} to {SiteName(siteId)}); last day of support {a.EndDay}. A coordination is not a contract.", [cause, a.CauseId]);
        }
    }

    private void ReturnSurveys(int d)
    {
        foreach (var k in _knowledge.Where(x => x.SurveyReturnsDay == d))
        {
            k.Tier = "surveyed";
            k.SurveyReturnsDay = null;
            var i = IslandData(k.IslandId);
            var facts = new List<string>
            {
                i.Grid == true ? "grid power available" : "no grid power",
                i.FuelPoint == true ? "fuel point on the island" : "no fuel point",
                $"water: {i.WaterSource ?? "unknown"}",
            };
            foreach (var l in Scenario.TransportLinks.Where(l => l.From == i.Id || l.To == i.Id))
            {
                var other = IslandData(l.From == i.Id ? l.To : l.From).Name;
                var sched = l.ScheduleDays.Count == 0 ? "on demand" : $"{l.ScheduleDays.Count}x weekly";
                facts.Add($"{l.Mode} to {other} {sched}{(l.WeatherSensitive ? ", weather-sensitive" : "")}");
            }
            var local = Scenario.Vendors.Where(v => v.Island == i.Id).Select(v => v.Name).ToList();
            facts.Add(local.Count == 0 ? "no vendors on the island" : $"vendors on the island: {string.Join(", ", local)}");
            AddRecord(d, RecordKind.Knowledge, $"survey {k.IslandId}", $"Survey of {i.Name} returned: {string.Join("; ", facts)}.", []);
        }
    }

    private void UcaClocks(int d)
    {
        foreach (var a in _arrangements)
        {
            if (a.Uca is not { } u) continue;
            if (!u.ProposalReceived && d >= u.QualifyingProposalDay)
            {
                u.ProposalReceived = true;
                AddRecord(d, RecordKind.UcaProposal, a.Id,
                    $"{VendorData(a.VendorId)!.Name} submitted a qualifying proposal on {a.Id}. Obligation cap rises to {Rules.Uca.ObligationCapAfterProposal * 100:0}% of NTE; definitization can start.", [a.CauseId]);
            }
            if (u.HhqInquiryDay == d && !u.Definitized)
            {
                _hhqInquiries++;
                var rec = AddRecord(d, RecordKind.Compliance, a.Id, $"Higher HQ asked why {a.Id} is past due for definitization. A response is now shop work.", []);
                AddWork(WorkKind.HhqResponse, $"Respond to HHQ on {a.Id}", Fixed.Milli(Rules.Model.Work.HhqInquiryResponseHours), d, a.Id, rec);
            }
        }
    }

    private void DoShopWork(int d)
    {
        var hours = _hoursPerDayMilli;
        foreach (var w in _work)
        {
            if (hours <= 0) break;
            if (w.CompletedDay is not null || w.WaitUntilDay > d) continue;
            var spend = Math.Min(hours, w.HoursRemainingMilli);
            w.HoursRemainingMilli -= spend;
            hours -= spend;
            _hoursWorkedMilli += spend;
        }
        // Complete in creation order after spending, so a zero-hour item finishes the day it is ready.
        foreach (var w in _work.ToList())
            if (w.CompletedDay is null && w.WaitUntilDay <= d && w.HoursRemainingMilli <= 0)
                CompleteWork(w, d);
    }

    private void Lifecycle(int d)
    {
        foreach (var a in _arrangements)
        {
            if (a.Status == ArrangementStatus.Pending && a.StartDay <= d && a.EndDay >= d)
            {
                a.Status = ArrangementStatus.Active;
                var who = a.VendorId is null ? "the other unit" : VendorData(a.VendorId)!.Name;
                AddRecord(d, RecordKind.ArrangementStart, a.Id, $"{a.Id} began: {CapLabel(a.CapabilityId)} at {SiteName(a.SiteId)} from {who}, {a.QuantityMilli / 1000} {Cap(a.CapabilityId).Unit}/day.", [a.CauseId]);
            }
            if (a.Status is ArrangementStatus.Active or ArrangementStatus.Suspended && a.EndDay < d)
            {
                a.Status = ArrangementStatus.Ended;
                AddRecord(d, RecordKind.ArrangementEnd, a.Id, $"{a.Id} ended.", [a.CauseId]);
            }
            if (a.Status == ArrangementStatus.Pending && a.EndDay < d) a.Status = ArrangementStatus.Ended;
        }
    }

    // ------------------------------------------------------------------ supply

    private Dictionary<string, Today> RollArrangements(int d)
    {
        var result = new Dictionary<string, Today>(StringComparer.Ordinal);
        var f = Rules.Model.Failure;
        var conf = Rules.AggregateRates.ExecutionConfusionNoTerms;
        var confPm = Fixed.Decay(Fixed.Permille(conf.Start), Fixed.Permille(conf.Floor), (int)Math.Round(conf.HalfLifeDays), d);
        foreach (var a in _arrangements)
        {
            if (a.Status is not (ArrangementStatus.Active or ArrangementStatus.Suspended)) continue;
            // Days in service count only while the site is open: equipment wears, invoices fall due and
            // coordinations wear out their welcome while someone is actually being supplied.
            if (PopulationOn(SiteData(a.SiteId), d) > 0) a.DaysActive++;
            var t = new Today();
            result[a.Id] = t;
            if (a.Status == ArrangementStatus.Suspended) { t.Suspended = true; t.FactorPermille = 0; continue; }

            var cap = Cap(a.CapabilityId);
            var failPm = 1000 - a.ReliabilityPermille;
            var missingOperator = cap.Lines.Any(l => l.Kind == "operator") && !a.Lines.Any(l => cap.Lines.First(x => x.Id == l).Kind == "operator");
            var missingMaintenance = cap.Lines.Any(l => l.Kind == "maintenance") && !a.Lines.Any(l => cap.Lines.First(x => x.Id == l).Kind == "maintenance");
            if (!a.IsCoordination && missingOperator) failPm += f.MissingOperatorPermillePerDay;
            if (!a.IsCoordination && missingMaintenance)
                failPm += Math.Min(f.MissingMaintenanceCapPermille, Math.Max(0, a.DaysActive - f.MissingMaintenanceGraceDays) * f.MissingMaintenancePermillePerDayAfterGrace);
            if (_rngVendor.Roll(failPm))
            {
                _vendorFailures++;
                t.Failed = true;
                var consumable = cap.Delivery == "consumable";
                t.FactorPermille = consumable ? 0 : f.ContinuousFailureSupplyPermille;
                var why = new List<string>();
                if (!a.IsCoordination && missingMaintenance) why.AddRange(cap.Lines.Where(l => l.Kind == "maintenance").Select(l => $"no {l.Label!.ToLowerInvariant()} line"));
                if (!a.IsCoordination && missingOperator) why.AddRange(cap.Lines.Where(l => l.Kind == "operator").Select(l => $"no {l.Label!.ToLowerInvariant()} line"));
                t.FailureRecord = AddRecord(d, RecordKind.VendorFailure, a.Id,
                    $"{a.Id} {(consumable ? "missed today's delivery" : "had equipment down")}{(why.Count > 0 ? $" ({string.Join(", ", why)})" : "")}.", [a.CauseId]);
            }
            // Execution confusion is the cost of running a recurring service on a cash or card buy (or a handshake):
            // nobody wrote down what was bought. Agreements and coordinations between governments or units are exempt.
            var noTerms = a.Unauthorized || a.Instrument is "sf44" or "gpc";
            if (noTerms && a.EndDay - a.StartDay + 1 >= Rules.Model.ExecutionConfusionMinDurationDays && _rngAggregate.Roll(confPm))
            {
                _confusionDays++;
                t.Confused = true;
                t.FactorPermille = t.FactorPermille * 500 / 1000;
                t.ConfusionRecord = AddRecord(d, RecordKind.ExecutionConfusion, a.Id,
                    $"Execution confusion on {a.Id}: the vendor and the unit disagree on quantity and schedule, and there are no written terms to settle it. Half of today's supply delivered.", [a.CauseId]);
            }
        }
        return result;
    }

    private void ResolveCapabilities(int d, Dictionary<string, Today> today)
    {
        var downAt = Rules.Model.DownThresholdPermille;
        foreach (var site in Scenario.Sites)
        {
            var open = PopulationOn(site, d) > 0;
            if (open && d == site.OpensDay)
            {
                foreach (var sc in site.Capabilities)
                    if (sc.InitialBufferDays is { } init && CapState(site.Id, sc.Id) is { } cs)
                        cs.BufferMilli = DemandMilli(site, sc.Id, d) * Fixed.Permille(init) / 1000;
            }
            foreach (var capId in _capOrder)
            {
                var cs = CapState(site.Id, capId);
                if (cs is null) continue;
                if (!open) { cs.TodayPermille = 1000; continue; }
                var cap = Cap(capId);
                var demand = DemandMilli(site, capId, d);
                var reasons = new List<string>();
                var causes = new List<string>();
                long supply = 0;
                var contributors = 0;
                var offIslandBlocked = false;
                foreach (var a in _arrangements)
                {
                    if (a.SiteId != site.Id || !today.TryGetValue(a.Id, out var t)) continue;
                    long q = 0;
                    if (a.CapabilityId == capId && a.Lines.Any(l => cap.Lines.First(x => x.Id == l).Kind == "core")) q += a.QuantityMilli;
                    if (a.SuppliedQuantityMilli.TryGetValue(capId, out var sq)) q += sq;
                    if (q == 0) continue;
                    contributors++;
                    q = Fixed.MulPermille(q, t.FactorPermille);
                    if (t.Suspended) { reasons.Add($"{a.Id} stopped work (unpaid UCA)"); causes.Add(a.CauseId); }
                    if (t.FailureRecord is not null) causes.Add(t.FailureRecord);
                    if (t.ConfusionRecord is not null) causes.Add(t.ConfusionRecord);
                    if (cap.Delivery == "consumable" && a.SupplierIslandId != site.Island)
                    {
                        a.InTransitMilli += q;
                        if (SurfaceDeliveryRuns(a.SupplierIslandId, site.Island, d, out var closed))
                        {
                            supply += a.InTransitMilli;
                            a.InTransitMilli = 0;
                        }
                        else if (closed is not null)
                        {
                            offIslandBlocked = true;
                            if (_linkClosureCause.TryGetValue(closed, out var lc) && !causes.Contains(lc)) causes.Add(lc);
                        }
                    }
                    else supply += q;
                }

                int permille;
                if (demand == 0) permille = 1000;
                else if (cap.Delivery == "consumable")
                {
                    var available = cs.BufferMilli + supply;
                    var take = Math.Min(available, demand);
                    var maxBuffer = demand * cs.BufferDaysPermille / 1000;
                    cs.BufferMilli = Math.Min(available - take, Math.Max(maxBuffer, 0));
                    permille = Fixed.Ratio(take, demand);
                    if (permille < 1000)
                    {
                        reasons.Add($"{cap.Label.ToLowerInvariant()} on hand ran out");
                        if (offIslandBlocked) reasons.Add("surface link closed");
                    }
                }
                else
                {
                    permille = Fixed.Ratio(supply, demand);
                    if (permille < 1000 && contributors > 0) reasons.Add($"supply {supply / 1000} {cap.Unit} against demand {demand / 1000}");
                }
                if (contributors == 0 && permille < 1000) reasons.Add($"no arrangement supplies {cap.Label.ToLowerInvariant()}");

                foreach (var dep in cap.DependsOn)
                {
                    var ds = CapState(site.Id, dep);
                    if (ds is null || ds.TodayPermille >= 1000) continue;
                    permille = permille * ds.TodayPermille / 1000;
                    reasons.Add($"{CapLabel(dep).ToLowerInvariant()} at {ds.TodayPermille / 10}%");
                    if (ds.OpenShortfallCause is not null) causes.Add(ds.OpenShortfallCause);
                }

                var prev = cs.TodayPermille;
                cs.TodayPermille = permille;
                cs.History.Add(permille);
                cs.MinPermille = Math.Min(cs.MinPermille, permille);
                if (permille < downAt) cs.DownDays++;
                else if (permille < 1000) cs.DegradedDays++;

                int Band(int p) => p >= 1000 ? 2 : p >= downAt ? 1 : 0;
                if (Band(permille) != Band(prev) || (permille < 1000 && cs.OpenShortfallCause is null))
                {
                    if (permille >= 1000)
                    {
                        AddRecord(d, RecordKind.Restored, $"{site.Id}.{capId}", $"{cap.Label} at {site.Name} restored to 100%.", cs.OpenShortfallCause is null ? [] : [cs.OpenShortfallCause]);
                        cs.OpenShortfallCause = null;
                    }
                    else
                    {
                        var band = permille < downAt ? "down" : "degraded";
                        cs.OpenShortfallCause = AddRecord(d, RecordKind.Shortfall, $"{site.Id}.{capId}",
                            $"{cap.Label} at {site.Name} {band} to {permille / 10}%: {string.Join("; ", reasons.Distinct())}.", causes.Distinct().ToList());
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------ money

    private void Settle(int d, Dictionary<string, Today> today)
    {
        var pay = Rules.AggregateRates.PaymentErrorManual;
        var payPm = Fixed.Decay(Fixed.Permille(pay.Start), Fixed.Permille(pay.Floor), (int)Math.Round(pay.HalfLifeDays), d);
        foreach (var a in _arrangements)
        {
            if (!today.ContainsKey(a.Id) || a.IsCoordination) continue;
            if (a.Status == ArrangementStatus.Active)
            {
                a.IncurredCents += a.DailyCostCents;
                if (!a.MobilizationCharged) { a.IncurredCents += a.MobilizationCents; a.MobilizationCharged = true; }
            }
            if (a.Uca is { } u) UcaObligations(a, u, d);

            var inst = Rules.Instruments.GetValueOrDefault(a.Instrument);
            if (inst is { RequiresSystemOrManual: true } && a.DaysActive > 0 && a.DaysActive % Rules.Model.InvoiceIntervalDays == 0 && d < _systemOnlineDay && _rngAggregate.Roll(payPm))
            {
                _paymentErrors++;
                var rec = AddRecord(d, RecordKind.PaymentError, a.Id,
                    $"Payment error on {a.Id}'s invoice (manual contract, no contract-writing system yet). Rework costs the shop {Rules.Model.Work.PaymentErrorReworkHours} hours.", [a.CauseId]);
                AddWork(WorkKind.PaymentRework, $"Fix payment on {a.Id}", Fixed.Milli(Rules.Model.Work.PaymentErrorReworkHours), d, a.Id, rec);
            }
        }
    }

    private void UcaObligations(Arrangement a, UcaState u, int d)
    {
        var capPm = u.ProposalReceived ? Fixed.Permille(Rules.Uca.ObligationCapAfterProposal) : Fixed.Permille(Rules.Uca.ObligationCapBeforeProposal);
        var ceiling = u.Definitized ? u.QuotedTotalCents : Fixed.MulPermille(u.NteCents, capPm);
        var need = a.IncurredCents - a.ObligatedCents;
        if (need > 0)
        {
            var room = Math.Max(0, ceiling - a.ObligatedCents);
            var line = _funding.FirstOrDefault(f => f.Id == a.FundingLineId);
            var money = line is not null && line.UsableOn(d) ? line.RemainingCents : 0;
            var add = Math.Min(need, Math.Min(room, money));
            if (add > 0)
            {
                a.ObligatedCents += add;
                line!.ObligatedCents += add;
            }
        }
        var unpaid = a.IncurredCents > a.ObligatedCents;
        if (unpaid)
        {
            u.UnpaidDays++;
            if (u.UnpaidDays == 1)
            {
                var capBinding = !u.Definitized && a.ObligatedCents >= ceiling;
                var fl = _funding.FirstOrDefault(f => f.Id == a.FundingLineId);
                var why = capBinding ? $"the UCA obligation cap of {capPm / 10}% of NTE is reached"
                    : fl is null ? "it has no funding line"
                    : d > fl.ExpiresDay ? $"funding line {fl.Id} expired on day {fl.ExpiresDay}"
                    : d < fl.AvailableDay ? $"funding line {fl.Id} is not available until day {fl.AvailableDay}"
                    : $"funding line {fl.Id} has {Money(fl.RemainingCents)} left";
                AddRecord(d, RecordKind.Obligation, a.Id, $"{a.Id} has incurred {Money(a.IncurredCents)} against {Money(a.ObligatedCents)} obligated: {why}. The vendor stops work in {Rules.Uca.StopWorkAfterUnpaidDays} days if this stands.", [a.CauseId]);
            }
            if (u.UnpaidDays >= Rules.Uca.StopWorkAfterUnpaidDays && a.Status == ArrangementStatus.Active)
            {
                a.Status = ArrangementStatus.Suspended;
                AddRecord(d, RecordKind.UcaStopWork, a.Id, $"{VendorData(a.VendorId)!.Name} stopped work on {a.Id}: costs incurred are not obligated.", [a.CauseId]);
            }
        }
        else
        {
            if (a.Status == ArrangementStatus.Suspended)
            {
                a.Status = ArrangementStatus.Active;
                AddRecord(d, RecordKind.UcaResumed, a.Id, $"{a.Id} resumed: obligations caught up.", [a.CauseId]);
            }
            u.UnpaidDays = 0;
        }
        if (!u.Definitized && !u.Overdue)
        {
            var obligatedPm = Fixed.Ratio(a.ObligatedCents, u.NteCents);
            if (obligatedPm >= Fixed.Permille(Rules.Uca.DefinitizeAtObligatedFraction) || d >= u.IssuedDay + Rules.Uca.DefinitizeWithinDays)
            {
                u.Overdue = true;
                u.HhqInquiryDay = d + Rules.Uca.HhqInquiryAfterOverdueDays;
                _ucaOverdue++;
                AddRecord(d, RecordKind.UcaOverdue, a.Id,
                    $"{a.Id} is due for definitization ({obligatedPm / 10}% of NTE obligated; the schedule calls for definitization by 50% or 180 days). Not definitized.", [a.CauseId]);
            }
        }
    }

    // ------------------------------------------------------------------ score

    private void Score(int d)
    {
        foreach (var site in Scenario.Sites)
        {
            var pop = PopulationOn(site, d);
            if (pop == 0 || site.Capabilities.Count == 0) continue;
            int F(string cap) => CapState(site.Id, cap)?.TodayPermille ?? 1000;
            var sortie = new[] { F("power"), F("fuel"), F("transport"), F("comms") }.Min();
            var throughput = new[] { F("transport"), F("mhe"), F("power") }.Min();
            var life = site.Capabilities.Where(c => c.Id is "lodging" or "food" or "water").Select(c => F(c.Id)).ToList();
            var endurance = life.Count == 0 ? 1000 : life.Sum() / life.Count;
            var critical = _caps.Where(c => c.SiteId == site.Id && c.Critical).ToList();
            int resilience;
            if (critical.Count == 0) resilience = 1000;
            else
            {
                var backed = critical.Count(c => _arrangements.Where(a => a.SiteId == site.Id && a.Status == ArrangementStatus.Active
                                                                          && (a.CapabilityId == c.CapabilityId || a.SuppliedQuantityMilli.ContainsKey(c.CapabilityId)))
                                                              .Select(a => a.VendorId ?? a.Id).Distinct().Count() >= 2);
                resilience = backed * 1000 / critical.Count;
            }
            var wsum = _missionWeights.Sum();
            var output = (sortie * _missionWeights[0] + throughput * _missionWeights[1] + endurance * _missionWeights[2] + resilience * _missionWeights[3]) / Math.Max(1, wsum);
            // Each open site counts equally: a small spoke carrying the flying mission matters as much as a large hub.
            _mission += output;
            _sortie += sortie;
            _throughput += throughput;
            _endurance += endurance;
            _resilience += resilience;
            _missionWeightDays += 1;
        }
    }

    /// <summary>
    /// Flat metric map. Keys match passCriteria.metric in scenario files. Values are exact (derived from integers).
    /// </summary>
    public IReadOnlyDictionary<string, double> Metrics
    {
        get
        {
            double Avg(long sum) => _missionWeightDays == 0 ? 1.0 : (double)(sum / _missionWeightDays) / 1000.0;
            var m = new SortedDictionary<string, double>(StringComparer.Ordinal)
            {
                ["mission.output"] = Avg(_mission),
                ["mission.sortieGeneration"] = Avg(_sortie),
                ["mission.throughput"] = Avg(_throughput),
                ["mission.endurance"] = Avg(_endurance),
                ["mission.resilience"] = Avg(_resilience),
                ["cost.obligatedCents"] = _arrangements.Sum(a => a.ObligatedCents),
                ["cost.incurredCents"] = _arrangements.Sum(a => a.IncurredCents),
                ["cost.unobligatedIncurredCents"] = _arrangements.Sum(a => a.UnobligatedIncurredCents),
                ["compliance.unauthorizedCommitments"] = _unauthorized,
                ["compliance.ucaOverdue"] = _ucaOverdue,
                ["compliance.hhqInquiries"] = _hhqInquiries,
                ["compliance.ratifications"] = _ratifications,
                ["compliance.handshakeWalkOffs"] = _walkOffs,
                ["compliance.habitHandshakes"] = _habitHandshakes,
                ["compliance.outOfScopeWork"] = _outOfScope,
                ["compliance.outOfScopeDenied"] = _outOfScopeDenied,
                ["compliance.commanderConfidenceHits"] = _confidenceHits,
                ["compliance.commitmentsUnresolved"] = _commitments.Count(c => c.Unresolved),
                ["info.surveys"] = _surveys,
                ["shop.hoursWorked"] = _hoursWorkedMilli / 1000,
                ["work.paymentErrors"] = _paymentErrors,
                ["work.executionConfusionDays"] = _confusionDays,
                ["work.vendorFailures"] = _vendorFailures,
                ["work.bpaDeclines"] = _bpaDeclines,
                ["events.fired"] = _eventsFired,
                ["actions.rejected"] = _rejected,
                ["day"] = Math.Min(Day - 1, LastDay),
            };
            foreach (var c in _caps)
            {
                var k = $"site.{c.SiteId}.{c.CapabilityId}";
                m[$"{k}.downDays"] = c.DownDays;
                m[$"{k}.degradedDays"] = c.DegradedDays;
                m[$"{k}.minPercent"] = c.MinPermille / 10;
            }
            foreach (var site in Scenario.Sites) m[$"site.{site.Id}.unitHabitPermille"] = Habit(site.Id);
            return m;
        }
    }
}
