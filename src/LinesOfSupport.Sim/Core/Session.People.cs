using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Going to people (backlog M2R-04, docs/design/opening-and-guidance.md 6.2 and 6.2a, rule R3). Finance, the commander's
// staff meeting, the SCO's staff, a vendor who comes by and the J4 are not in the inbox: going to them is an action that
// costs shop hours and returns facts. Nothing here runs unless the player goes, so games that never do replay exactly as
// before. The SCO's answer lag is the one random draw, from its own "people" stream.
public sealed partial class Session
{
    private sealed record VisitPayload(string Kind, string? Target, object? Extra = null);

    private DeterministicRandom? _rngPeopleStream;
    private DeterministicRandom RngPeople => _rngPeopleStream ??= DeterministicRandom.ForStream(Seed, "people");

    private int? _financeFoundDay;
    private bool _attendingMeetings;
    private int? _nextMeetingDay;
    private string _meetingJoinRecord = "";
    private readonly List<int> _meetingDays = [];
    private readonly HashSet<string> _previewedCards = new(StringComparer.Ordinal);
    private readonly List<(int Day, string Question, string CauseId)> _scoAnswersDue = [];
    private readonly HashSet<string> _scoAsked = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _vendorConfidence = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _vendorsMet = new(StringComparer.Ordinal);

    private static readonly string[] Tiers = ["assumed", "estimated", "surveyed", "observed"];

    internal int? FinanceFoundDay => _financeFoundDay;
    internal IReadOnlyCollection<string> PreviewedCards => _previewedCards;
    internal string VendorConfidence(VendorData v) => _vendorConfidence.GetValueOrDefault(v.Id, v.InfoTier);
    internal int? VendorMetDay(string vendorId) => _vendorsMet.TryGetValue(vendorId, out var d) ? d : null;

    private GoingToPeopleRulesData People => Rules.GoingToPeople;
    private bool VisitInWork(string kind, string? target = null)
        => _work.Any(w => w.Kind == WorkKind.Visit && w.CompletedDay is null && w.Payload is VisitPayload p && p.Kind == kind && (target is null || p.Target == target));

    /// <summary>Whether the usual owner has the capability at the site in hand (R10): an ACSA line covers it on some day of the
    /// window (inherited, ordered by the OCS seat, or added by the J4), or a J4 request for it is still in work.</summary>
    internal bool OwnerEngaged(string siteId, string capId, int from, int to)
        => _arrangements.Any(a => a.Instrument == AcsaInstrument && a.SiteId == siteId && a.CapabilityId == capId
                                  && a.Status is ArrangementStatus.Pending or ArrangementStatus.Active && a.StartDay <= to && a.EndDay >= from)
           || _work.Any(w => w.CompletedDay is null && w.Payload is VisitPayload { Kind: "j4", Extra: EngageJ4Action j } && j.Site == siteId && j.Capability == capId);

    /// <summary>
    /// Debrief factor (M2R-06, R10): the capability has a usual owner other than the office (fuel: the J4 through the ACSA
    /// partner) and the partner here can spare it, yet on a gap day no ACSA line covered it. Avoidable: the owner was there to
    /// be engaged from day 1 (the dependency checklist names them), and a local vendor was the stopgap meanwhile.
    /// </summary>
    private (string Key, GapFactor F)? OwnerFactor(string siteId, string capId, int d)
    {
        var cap = Cap(capId);
        if (cap.UsualOwner is null || Partner is not { } partner) return null;
        if (!partner.Stock.TryGetValue(capId, out var stock) || stock <= 0) return null;
        // Only when nothing at all supplied it that day: a stopgap that was bought and then failed (a barge closure, a vendor
        // failure) has its own factor, and the owner's line would have crossed the same water.
        if (_arrangements.Any(a => a.SiteId == siteId && Supplies(a, capId) && ActiveOn(a, d) && a.Status is not ArrangementStatus.Declined))
            return null;
        var what = cap.Label.ToLowerInvariant();
        var where = SiteName(siteId);
        var req = J4Request(siteId, capId);
        string text; List<string> ids = [];
        if (req is null)
            text = $"Nobody owned {what} at {where}. {Capitalize(what)} is usually {cap.UsualOwner.TrimEnd('.')}. The J4 was never asked, and {PartnerName} had {stock:#,0} {cap.Unit} a day to spare under the ACSA. Buying {what} yourself is the stopgap, not the plan.";
        else
        {
            var (asked, start, id) = req.Value;
            text = start is { } sd
                ? $"The J4 was asked for {what} at {where} on day {asked} and the ACSA line began on day {sd}; until then nobody owned it. {Capitalize(what)} is usually {cap.UsualOwner.TrimEnd('.')}."
                : $"The J4 was asked for {what} at {where} on day {asked} and could not add it; nothing stood in for the ACSA line. {Capitalize(what)} is usually {cap.UsualOwner.TrimEnd('.')}.";
            if (_records.FirstOrDefault(r => r.Subject == id) is { } rec) ids.Add(rec.Id);
        }
        return ($"owner|{siteId}|{capId}", new GapFactor(GapFactorKind.OwnerNotEngaged, GapTag.Avoidable, d, text, ids));
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    /// <summary>The first J4 request for the capability at the site, if any: the day it was asked and the day support began (or
    /// was refused).</summary>
    internal (int AskedDay, int? StartDay, string RequestId)? J4Request(string siteId, string capId)
    {
        var items = _work.Where(w => w.Payload is VisitPayload { Kind: "j4", Extra: EngageJ4Action j } && j.Site == siteId && j.Capability == capId)
                         .OrderBy(w => w.CreatedDay).ToList();
        if (items.Count == 0) return null;
        var w = items[0];
        var j = (EngageJ4Action)((VisitPayload)w.Payload!).Extra!;
        var arr = _arrangements.FirstOrDefault(a => a.Id == j.Id && a.Instrument == AcsaInstrument);
        return (w.CreatedDay, arr?.StartDay, j.Id);
    }

    /// <summary>Who said it, as the player meets them ("Capt Priya Nair, Comptroller"), or the fallback.</summary>
    private string Who(string? personId, string fallback) => BuildPerson(personId)?.WithUnit ?? fallback;
    /// <summary>The same, as the subject of a sentence: an appositive unit gets its closing comma ("Lt Col Daniel Park, Hub
    /// commander, ran it").</summary>
    private string WhoDoes(string? personId, string fallback)
        => BuildPerson(personId) is { } p ? (p.Unit.Length == 0 ? p.DisplayName : $"{p.WithUnit},") : fallback;
    private static string DayText(int day) => day >= 1 ? $"day {day}" : $"{1 - day} days before you arrived";
    private static string NoticeText(int days) => days switch { <= 0 => "no notice", 1 => "a day's notice", _ => $"{days} days' notice" };
    private static string PersonSubject(string? personId, string fallback) => personId is null ? fallback : $"person {personId}";

    // ------------------------------------------------------------------ checks (shared by Apply and the preview)

    internal (List<string> Stops, long HoursMilli) CheckVisit(PlayerAction action)
    {
        var stops = new List<string>();
        long hours = 0;
        switch (action)
        {
            case FindFinanceAction:
                hours = Fixed.Milli(People.FindFinance.ShopHours);
                if (_financeFoundDay is not null) stops.Add("you have already found the finance office");
                else if (VisitInWork("finance")) stops.Add("someone is already on the way to the finance office");
                break;
            case JoinStaffMeetingAction { Attend: true }:
                hours = Fixed.Milli(People.StaffMeeting.JoinShopHours);
                if (_attendingMeetings) stops.Add("you are already going to the staff meeting");
                else if (VisitInWork("joinMeeting")) stops.Add("you are already getting yourself into the staff meeting");
                break;
            case JoinStaffMeetingAction:
                if (!_attendingMeetings && !VisitInWork("joinMeeting")) stops.Add("you are not going to the staff meeting");
                break;
            case AskScoAction q:
                hours = Fixed.Milli(People.AskSco.ShopHours);
                if (People.AskSco.Questions.All(x => x.Id != q.Question)) stops.Add("the SCO's staff take questions from their list");
                else if (VisitInWork("sco", q.Question) || _scoAnswersDue.Any(a => a.Question == q.Question))
                    stops.Add("that question is already with the SCO's staff");
                break;
            case MeetVendorAction m:
                hours = Fixed.Milli(People.MeetVendor.ShopHours);
                if (VendorData(m.Vendor) is not { } v) stops.Add("no such vendor");
                else if (_vendorsMet.ContainsKey(v.Id)) stops.Add($"you have already met {v.Name}");
                else if (VisitInWork("vendor", v.Id)) stops.Add($"a meeting with {v.Name} is already set up");
                break;
            case EngageJ4Action j:
                hours = Fixed.Milli(People.EngageJ4.ShopHours);
                stops.AddRange(CheckEngageJ4(j, Day));
                break;
            default:
                stops.Add("not a visit");
                break;
        }
        return (stops, hours);
    }

    private List<string> CheckEngageJ4(EngageJ4Action j, int day, bool atCompletion = false)
    {
        var stops = new List<string>();
        if (!atCompletion)
        {
            if (string.IsNullOrWhiteSpace(j.Id)) stops.Add("a request needs an id");
            else if (IdInUse(j.Id) || _work.Any(w => w.CompletedDay is null && w.Payload is VisitPayload { Kind: "j4" } p && p.Target == j.Id))
                stops.Add("This request to the J4 has already been made");
        }
        if (!Scenario.Sites.Any(s => s.Id == j.Site)) { stops.Add("no such site"); return stops; }
        if (!_capData.ContainsKey(j.Capability)) { stops.Add("no such capability"); return stops; }
        var what = CapLabel(j.Capability).ToLowerInvariant();
        if (Partner is not { } partner) { stops.Add($"there is no ACSA partner here, so the J4 has no ACSA order to add {SiteName(j.Site)} to"); return stops; }
        if (!partner.Stock.TryGetValue(j.Capability, out var stock) || stock <= 0) { stops.Add($"{PartnerName} has no {what} to spare under the ACSA"); return stops; }
        if (Withdrawn(j.Capability)) stops.Add($"{PartnerName} has withdrawn its {what} support: its own needs come first");
        if (j.Quantity <= 0) stops.Add("quantity must be positive");
        if (j.DurationDays is <= 0) stops.Add("duration must be positive");
        var (start, end) = J4Window(j, day);
        var committed = AcsaCommittedMilli(j.Capability, start, end)
                        + (atCompletion ? 0 : _work.Where(w => w.CompletedDay is null && w.Payload is VisitPayload { Kind: "j4" })
                                                  .Select(w => (EngageJ4Action)((VisitPayload)w.Payload!).Extra!)
                                                  .Where(o => o.Capability == j.Capability).Sum(o => o.Quantity * 1000));
        if (j.Quantity * 1000 + committed > Fixed.Milli(stock))
            stops.Add($"{PartnerName} can spare {stock:0} {Cap(j.Capability).Unit} a day of {what} and {committed / 1000} is already spoken for");
        return stops;
    }

    /// <summary>First and last day of J4 support if the request completes on <paramref name="doneDay"/>.</summary>
    private (int Start, int End) J4Window(EngageJ4Action j, int doneDay)
    {
        var start = doneDay + 1 + People.EngageJ4.LagDays;
        var end = j.DurationDays is { } n ? start + n - 1 : Math.Max(start, LastDay);
        return (start, end);
    }

    internal ActionPreview BuildVisitPreview(PlayerAction action)
    {
        var (stops, hours) = CheckVisit(action);
        var queue = CeilHours(_work.Where(w => w.CompletedDay is null).Sum(w => w.HoursRemainingMilli));
        var done = _hoursPerDayMilli <= 0 ? Day : Day + Math.Max(0, (int)((hours + _hoursPerDayMilli - 1) / _hoursPerDayMilli) - 1);
        var ready = action switch
        {
            AskScoAction => done + ScoLag().Min,
            EngageJ4Action j => J4Window(j, done).Start,
            _ => done,
        };
        var readyMax = action switch
        {
            AskScoAction => done + ScoLag().Max,
            _ => ready,
        };
        var gaps = new List<string>();
        if (action is EngageJ4Action && Partner is { } p)
            gaps.Add($"{p.Name} can cut this back when its own needs rise: ACSA support is what the partner can spare, not a contract.");
        return new ActionPreview(stops.Count == 0, stops, gaps, 0, 0, 0, CeilHours(hours), CeilHours(hours), ready, readyMax, queue);
    }

    private IntRange ScoLag()
    {
        var lag = Rules.Sco.ResponseLagDaysByResponsiveness;
        return Scenario.Actors.Sco.Responsiveness switch { "high" => lag.High, "low" => lag.Low, _ => lag.Medium };
    }

    // ------------------------------------------------------------------ apply

    private ActionResult StartVisit(PlayerAction action, string kind, string? target, string subject, string text, object? extra = null)
    {
        var (stops, hours) = CheckVisit(action);
        if (stops.Count > 0) return ActionResult.Stop([.. stops]);
        var rec = AddRecord(Day, RecordKind.Action, subject, text, []);
        AddWork(WorkKind.Visit, text, hours, Day, new VisitPayload(kind, target, extra), rec);
        return ActionResult.Ok("in work");
    }

    private ActionResult ApplyFindFinance()
    {
        var fm = Scenario.Actors.Fm.Person;
        return StartVisit(new FindFinanceAction(), "finance", null, PersonSubject(fm, "finance"),
            $"Going to find the finance office: about {People.FindFinance.ShopHours} shop hours.");
    }

    private ActionResult ApplyJoinStaffMeeting(JoinStaffMeetingAction j)
    {
        var cmd = Scenario.Actors.Commander.Person;
        if (j.Attend)
            return StartVisit(j, "joinMeeting", null, PersonSubject(cmd, "staffMeeting"),
                $"Getting yourself into the commander's staff meeting: about {People.StaffMeeting.JoinShopHours} shop hours, then {People.StaffMeeting.WeeklyShopHours} each week.");
        var (stops, _) = CheckVisit(j);
        if (stops.Count > 0) return ActionResult.Stop([.. stops]);
        _work.RemoveAll(w => w.CompletedDay is null && w.Payload is VisitPayload { Kind: "joinMeeting" or "meeting" });
        _attendingMeetings = false;
        _nextMeetingDay = null;
        AddRecord(Day, RecordKind.Action, PersonSubject(cmd, "staffMeeting"),
            "You stop going to the commander's staff meeting. Requirements will reach you when they are submitted.", []);
        return ActionResult.Ok("stopped");
    }

    private ActionResult ApplyAskSco(AskScoAction q)
    {
        var label = People.AskSco.Questions.FirstOrDefault(x => x.Id == q.Question)?.Label ?? q.Question;
        var sco = Scenario.Actors.Sco.Person;
        return StartVisit(q, "sco", q.Question, PersonSubject(sco, "sco"), $"Question to the SCO's staff: \"{label}\"");
    }

    private ActionResult ApplyMeetVendor(MeetVendorAction m)
    {
        var name = VendorData(m.Vendor)?.Name ?? m.Vendor;
        var rep = Scenario.People.FirstOrDefault(p => p.Role == "vendorRep" && p.Vendor == m.Vendor)?.Id;
        return StartVisit(m, "vendor", m.Vendor, PersonSubject(rep, m.Vendor),
            $"Sitting down with {(BuildPerson(rep) is { } rp ? $"{rp.DisplayName} of {name}" : name)}: about {People.MeetVendor.ShopHours} shop hours.");
    }

    private ActionResult ApplyEngageJ4(EngageJ4Action j)
    {
        var (stops, _) = CheckVisit(j);
        if (stops.Count > 0) return ActionResult.Stop([.. stops]);
        var cap = Cap(j.Capability);
        return StartVisit(j, "j4", j.Id, j.Id,
            $"Asked the J4, through the OCS seat, to have the ACSA manager add {SiteName(j.Site)} to the {cap.Label.ToLowerInvariant()} order: {j.Quantity} {cap.Unit} a day.", j);
    }

    // ------------------------------------------------------------------ the day

    /// <summary>Before the shop works: a weekly staff meeting goes to the front of the queue (it happens at its hour).</summary>
    private void PeopleBeforeWork(int d)
    {
        if (!_attendingMeetings || _nextMeetingDay != d) return;
        _nextMeetingDay = d + Math.Max(1, People.StaffMeeting.EveryDays);
        var item = AddWork(WorkKind.Visit, "Staff meeting", Fixed.Milli(People.StaffMeeting.WeeklyShopHours), d,
                           new VisitPayload("meeting", null), _meetingJoinRecord);
        _work.Remove(item);
        _work.Insert(0, item);
    }

    /// <summary>After the shop works: SCO answers that have come back.</summary>
    private void PeopleAfterWork(int d)
    {
        foreach (var due in _scoAnswersDue.Where(a => a.Day <= d).ToList())
        {
            _scoAnswersDue.Remove(due);
            var sco = Scenario.Actors.Sco.Person;
            var q = People.AskSco.Questions.First(x => x.Id == due.Question);
            var answer = ScoAnswer(q.Id, d);
            if (Scenario.Tutorial && q.TutorialDirection is { Length: > 0 } dir) answer += " " + dir;
            AddRecord(d, RecordKind.Visit, PersonSubject(sco, "sco"),
                $"{WhoDoes(sco, "The SCO's staff")} answered your question, \"{q.Label}\" \"{answer}\"", [due.CauseId]);
        }
    }

    private void CompleteVisit(WorkItem w, int day)
    {
        var p = (VisitPayload)w.Payload!;
        switch (p.Kind)
        {
            case "finance": CompleteFindFinance(day, w.CauseId); break;
            case "joinMeeting":
                _attendingMeetings = true;
                _meetingJoinRecord = w.CauseId;
                HoldMeeting(day, w.CauseId, first: true);
                _nextMeetingDay = day + Math.Max(1, People.StaffMeeting.EveryDays);
                break;
            case "meeting": HoldMeeting(day, w.CauseId, first: false); break;
            case "sco":
                _scoAsked.Add(p.Target!);
                _scoAnswersDue.Add((day + RngPeople.NextInt(ScoLag()), p.Target!, w.CauseId));
                break;
            case "vendor": CompleteMeetVendor(p.Target!, day, w.CauseId); break;
            case "j4": CompleteEngageJ4((EngageJ4Action)p.Extra!, day, w.CauseId); break;
        }
    }

    private void CompleteFindFinance(int day, string cause)
    {
        _financeFoundDay = day;
        var r = People.FindFinance;
        var before = _payingAgentDay;
        PrepSetPayingAgentDay(day + r.PayingAgentWithinDays);
        PrepSetReplenishLag(r.ReplenishLagDays);
        var agent = _payingAgentDay!.Value;
        var agentText = agent <= day ? "Your paying agent can carry cash for SF 44 buys now"
            : $"I can have a paying agent carrying cash for your SF 44 buys by day {agent}{(before is null || before > agent ? "" : ", as planned")}";
        var cards = _gpcAvailableDay is { } g
            ? g <= day ? "Purchase cards are live." : $"Purchase cards go live on day {g}."
            : "There are no purchase card accounts on this deployment.";
        var fm = Scenario.Actors.Fm.Person;
        AddRecord(day, RecordKind.Visit, PersonSubject(fm, "finance"),
            $"{Who(fm, "The finance office")}: \"{agentText}. You have {Money(_cash)} on hand, and when it runs low I can turn it around in {_cashReplenishLagDays} days. {cards} Send me the paperwork and certification takes {Scenario.Actors.Fm.CertificationLagDays} day{(Scenario.Actors.Fm.CertificationLagDays == 1 ? "" : "s")}.\"", [cause]);
    }

    private void HoldMeeting(int day, string cause, bool first)
    {
        _meetingDays.Add(day);
        var cmd = Scenario.Actors.Commander.Person;
        var order = Scenario.Actors.Commander.PriorityOrder.Where(id => Scenario.Sites.Any(s => s.Id == id)).Select(SiteName).ToList();
        var priorities = order.Count switch
        {
            0 => "",
            1 => $" If we run short, {order[0]} comes first.",
            _ => $" If we run short, it's {string.Join(", then ", order)}.",
        };
        var horizon = day + People.StaffMeeting.PreviewDays;
        var coming = _cards.Where(c => c.SubmittedDay > day && c.SubmittedDay <= horizon && !_previewedCards.Contains(c.Id))
                           .OrderBy(c => c.SubmittedDay).ToList();
        var heard = new List<string>();
        foreach (var c in coming)
        {
            _previewedCards.Add(c.Id);
            var who = WhoDoes(c.PersonId, c.SubmittedBy ?? "A unit");
            heard.Add($"{who} said they will be asking for {Lower(CardTitle(c))}, wanted from day {c.RequestedStartDay}.");
        }
        var news = heard.Count == 0 ? " Nobody raised anything new for the shop." : " " + string.Join(" ", heard);
        var opening = first ? "You got yourself into the commander's staff meeting." : "Staff meeting.";
        AddRecord(day, RecordKind.Visit, PersonSubject(cmd, "staffMeeting"),
            $"{opening} {WhoDoes(cmd, "The commander")} ran it.{priorities}{news}", cause.Length == 0 ? [] : [cause]);
    }

    private static string Lower(string s) => s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..];

    private void CompleteMeetVendor(string vendorId, int day, string cause)
    {
        var v = VendorData(vendorId)!;
        _vendorsMet[v.Id] = day;
        var was = VendorConfidence(v);
        var to = People.MeetVendor.Confidence;
        if (Array.IndexOf(Tiers, to) > Array.IndexOf(Tiers, was)) _vendorConfidence[v.Id] = to;
        var rep = Scenario.People.FirstOrDefault(p => p.Role == "vendorRep" && p.Vendor == v.Id)?.Id;
        var offers = v.Offers.Select(o =>
        {
            var cap = Cap(o.Capability);
            var from = o.SourceIsland is { } si && si != v.Island ? $" from {IslandData(si).Name}" : "";
            return cap.Delivery == "consumable"
                ? $"{Lower(cap.Label)}, up to {o.CapacityPerDay:0} {cap.Unit} a day at {Money(o.UnitPriceCents)} each{from}, on {NoticeText(o.LeadTimeDays)}"
                : $"{Lower(cap.Label)} for up to {o.CapacityPerDay:0} {cap.Unit} at {Money(o.UnitPriceCents)} each a day{from}, on {NoticeText(o.LeadTimeDays)}";
        }).ToList();
        var offerText = offers.Count == 0 ? "They have nothing to offer you directly." : $"They offer {string.Join("; ", offers)}.";
        // A vendor wants to be paid: an undefinitized UCA the handover did not mention is the old contract they raise.
        var raised = new List<string>();
        foreach (var a in _arrangements.Where(a => a.VendorId == v.Id && a.CreatedDay == 0 && ProblemHidden(a) && a.Uca is { Definitized: false }))
        {
            raised.Add($"{CapLabel(a.CapabilityId)} at {SiteName(a.SiteId)} was started on a letter contract {DayText(a.Uca!.IssuedDay)} and never definitized; they are waiting on the modification and their money");
        }
        var said = raised.Count == 0 ? "" : $" They raised an old contract: {string.Join("; and ", raised)}.";
        var tier = _vendorConfidence.ContainsKey(v.Id) ? $" Your directory entry for them is now {to}, not {was}." : "";
        var rec = AddRecord(day, RecordKind.Visit, PersonSubject(rep, v.Id),
            $"Met {(BuildPerson(rep) is { } rp ? $"{rp.DisplayName} of {v.Name}" : v.Name)}. {offerText}{said}{tier}", [cause]);
        foreach (var a in _arrangements.Where(a => a.VendorId == v.Id && a.CreatedDay == 0 && ProblemHidden(a) && a.Uca is { Definitized: false }))
            _revealed.TryAdd(a.Id, (day, rec, RevealSource.Vendor));
    }

    private void CompleteEngageJ4(EngageJ4Action j, int day, string cause)
    {
        var stops = CheckEngageJ4(j, day, atCompletion: true);
        var cap = Cap(j.Capability);
        if (stops.Count > 0)
        {
            AddRecord(day, RecordKind.Rejected, j.Id,
                $"The J4 could not add {SiteName(j.Site)} to the ACSA {cap.Label.ToLowerInvariant()} order: {stops[0]}.", [cause]);
            return;
        }
        var partner = Partner!;
        var (start, end) = J4Window(j, day);
        _arrangements.Add(new Arrangement
        {
            Id = j.Id, Instrument = AcsaInstrument, SiteId = j.Site, CapabilityId = j.Capability,
            Lines = cap.Lines.Where(l => l.Kind != "supplies").Select(l => l.Id).ToList(),
            SupplierIslandId = partner.Island ?? SiteData(j.Site).Island, QuantityMilli = j.Quantity * 1000, DailyCostCents = 0,
            ObligatedCents = 0, FundingLineId = null, ReliabilityPermille = Fixed.Permille(partner.Reliability),
            CreatedDay = day, CauseId = cause, StartDay = start, EndDay = Math.Min(end, LastDay),
        });
        AddRecord(day, RecordKind.Visit, j.Id,
            $"The J4's ACSA manager is adding {SiteName(j.Site)} to the {cap.Label.ToLowerInvariant()} order with {PartnerName}: {j.Quantity} {cap.Unit} a day from day {start}. The reimbursement is the J4's, not the shop's. {Upper(PartnerName)} can cut it back when its own needs rise.", [cause]);
    }

    // ------------------------------------------------------------------ views

    internal StaffMeetingView BuildStaffMeeting()
        => new(_attendingMeetings, VisitInWork("joinMeeting"), _nextMeetingDay, [.. _meetingDays], [.. _previewedCards]);

    internal IReadOnlyList<ScoQuestionView> BuildScoQuestions()
        => People.AskSco.Questions.Select(q => new ScoQuestionView(q.Id, q.Label,
               VisitInWork("sco", q.Id) || _scoAnswersDue.Any(a => a.Question == q.Id), _scoAsked.Contains(q.Id))).ToList();

    /// <summary>A question's answer as the SCO's staff give it: facts and limits from the scenario and rules (R4).</summary>
    private string ScoAnswer(string question, int day) => question switch
    {
        "fuel" => FuelAnswer(),
        "routes" => RoutesAnswer(day),
        "vehicles" => VehiclesAnswer(),
        "thresholds" => $"The micro-purchase threshold for this operation is {Money(Rules.Thresholds.MicroPurchaseCents)} and the simplified acquisition threshold is {Money(Rules.Thresholds.SimplifiedAcquisitionCents)}. That's the contingency, outside-the-US figure.",
        _ => "We don't have an answer for that.",
    };

    private string FuelAnswer()
    {
        const string bulk = "Bulk fuel normally comes through the J4: the ACSA manager draws on a partner's stock, or DLA Energy supplies it.";
        if (!_capData.ContainsKey("fuel")) return bulk;
        var unit = Cap("fuel").Unit;
        if (Partner is { } p && p.Stock.TryGetValue("fuel", out var stock) && stock > 0 && !Withdrawn("fuel"))
        {
            var from = p.Island is { } i ? $" on {IslandData(i).Name}" : "";
            return $"{bulk} Here the partner is {p.Name}{from}, which can spare {stock:0} {unit} a day; the J4 can add a site to that order. A local fuel vendor is the stopgap, at the vendor's price.";
        }
        return $"{bulk} There is no ACSA fuel here right now, so a local fuel vendor is the stopgap, at the vendor's price, until DLA Energy is set up.";
    }

    private string RoutesAnswer(int day)
    {
        var parts = new List<string>();
        foreach (var l in Scenario.TransportLinks)
        {
            var mode = l.Mode is "aircraft" or "airlift" ? "The aircraft" : $"The {l.Mode}";
            var route = $"{IslandData(l.From).Name} to {IslandData(l.To).Name}";
            if (l.ScheduleDays.Count == 0) { parts.Add($"{mode} from {route} runs every day."); continue; }
            var next = Enumerable.Range(day + 1, 14).Where(x => x <= LastDay && l.ScheduleDays.Contains(Weekday(x))).Take(3).ToList();
            parts.Add(next.Count == 0 ? $"{mode} from {route} has no more runs scheduled."
                : $"{mode} from {route} runs {l.ScheduleDays.Count} day{(l.ScheduleDays.Count == 1 ? "" : "s")} a week; the next runs are days {string.Join(", ", next)}.");
        }
        if (parts.Count == 0) return "Everything here is on one island; nothing has to cross water.";
        parts.Add("The harbor only warns of a closure ahead of time to people who have surveyed the island at the far end.");
        return string.Join(" ", parts);
    }

    private string VehiclesAnswer()
    {
        var known = Scenario.Actors.Sco.KnownVehicles
            .Select(id => Scenario.Vehicles.FirstOrDefault(v => v.Id == id)).Where(v => v is not null)
            .Select(v => $"a {VehicleKindText(v!.Kind)} with {VendorData(v.Vendor)?.Name ?? "a vendor"}").ToList();
        return known.Count == 0
            ? "We don't know of a contract vehicle that covers your area."
            : $"You can order from {string.Join(" and ", known)}, within each one's scope.";
    }

    private static string VehicleKindText(string kind) => kind switch
    {
        "bpa" => "blanket purchase agreement",
        "idiq" => "task order contract",
        _ => "contract vehicle",
    };
}
