using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Core;

// The customer small-buy stream and the purchase card program (backlog M2R-03, design doc 6.1).
// CE and the other units bring a steady trickle of small buys: lumber, piping, nails, tools. The shop works them
// with whatever hours the day's queued work leaves, so a busy shop builds a backlog and the customer notices.
// The player can hold the stream outright, or stand up a decentralized purchase card program (worked around
// everything else) that moves most of the stream onto cardholders. No random draws: the stream is population math.
public sealed partial class Session
{
    private long _smallBuyAccumMilli;          // fractional buys carried day to day
    private long _hoursLeftTodayMilli;         // set by DoShopWork
    private int _smallBuyBacklog;              // buys waiting (held, or more than a day's hours)
    private bool _smallBuysHeld;
    private bool _complained;
    private long _smallBuyHoursTodayMilli;
    private long _smallBuyHoursMilli;
    private int _smallBuysWorked, _smallBuysHeldDays, _smallBuysToday, _smallBuysOnCardsToday;
    private int? _gpcProgramDay;

    public bool SmallBuysHeld => _smallBuysHeld;
    public int SmallBuyBacklog => _smallBuyBacklog;
    public int? GpcProgramDay => _gpcProgramDay;
    internal int SmallBuysToday => _smallBuysToday;
    internal int SmallBuysOnCardsToday => _smallBuysOnCardsToday;
    internal long SmallBuyHoursTodayMilli => _smallBuyHoursTodayMilli;
    internal bool GpcProgramInWork => _work.Any(w => w.Kind == WorkKind.GpcProgram && w.CompletedDay is null);

    private string SmallBuyCustomer => Rules.SmallBuys.CustomerUnit;
    private static string HoursText(long milli) => milli % 1000 == 0 ? $"{milli / 1000} h" : $"{milli / 1000.0:0.#} h";

    /// <summary>Buys arriving today, from the population at every open site. Fractions carry to tomorrow.</summary>
    private int NewSmallBuys(int d)
    {
        long people = 0;
        foreach (var site in Scenario.Sites) if (d >= site.OpensDay) people += PopulationOn(site, d);
        _smallBuyAccumMilli += people * Rules.SmallBuys.BuysPerPersonPerDayMilli;
        var n = (int)(_smallBuyAccumMilli / 1000);
        _smallBuyAccumMilli -= n * 1000L;
        return n;
    }

    private void SmallBuys(int d)
    {
        var arrived = NewSmallBuys(d);
        _smallBuysOnCardsToday = 0;
        _smallBuyHoursTodayMilli = 0;
        if (_gpcProgramDay is { } g && d > g)
        {
            _smallBuysOnCardsToday = (int)((long)arrived * Rules.GpcProgram.DivertedPermille / 1000);
            arrived -= _smallBuysOnCardsToday;
        }
        _smallBuysToday = arrived;
        _smallBuyBacklog += arrived;
        if (_smallBuysHeld) _smallBuysHeldDays++;
        var each = Rules.SmallBuys.HoursEachMilli;
        var hours = _smallBuysHeld ? 0 : _hoursLeftTodayMilli;
        var canWork = _smallBuyBacklog == 0 ? 0 : each <= 0 ? _smallBuyBacklog : (int)Math.Min(_smallBuyBacklog, hours / Math.Max(1, each));
        _smallBuyHoursTodayMilli = canWork * (long)each;
        _smallBuyHoursMilli += _smallBuyHoursTodayMilli;
        _hoursWorkedMilli += _smallBuyHoursTodayMilli;
        _smallBuysWorked += canWork;
        _smallBuyBacklog -= canWork;
        // The customer notices when the backlog has sat for a while: held, or simply not reached.
        var dayLoad = Math.Max(1, _smallBuysToday);
        if (_smallBuyBacklog >= dayLoad * Rules.SmallBuys.ComplainAfterHeldDays)
        {
            if (!_complained)
            {
                _complained = true;
                _confidenceHits++;
                var who = Scenario.People.FirstOrDefault(p => p.Role == "customer" && p.Unit.Contains(SmallBuyCustomer, StringComparison.OrdinalIgnoreCase));
                var name = who is null ? SmallBuyCustomer : $"{who.Rank} {who.Name} ({who.Unit})".Trim();
                AddRecord(d, RecordKind.Compliance, "smallBuys",
                    $"{name} went to the commander: {_smallBuyBacklog} small buys are waiting {(_smallBuysHeld ? "with the shop holding them" : "behind the shop's other work")}. The commander's confidence in contracting drops.", []);
            }
        }
        else _complained = false;
        if (canWork == 0 && _smallBuyBacklog == 0) return;
        if (canWork == 0)
        {
            AddRecord(d, RecordKind.WorkDone, "smallBuys", $"{SmallBuyCustomer} small buys: none worked today ({(_smallBuysHeld ? "held" : "no hours left")}); {_smallBuyBacklog} waiting{(_smallBuysOnCardsToday > 0 ? $", {_smallBuysOnCardsToday} went on cards" : "")}.", []);
            return;
        }
        var cards = _smallBuysOnCardsToday > 0 ? $", {_smallBuysOnCardsToday} on cards" : "";
        var left = _smallBuyBacklog > 0 ? $"; {_smallBuyBacklog} still waiting" : "";
        AddRecord(d, RecordKind.WorkDone, "smallBuys",
            $"{SmallBuyCustomer} small buys: {canWork} worked today ({HoursText(_smallBuyHoursTodayMilli)} of shop time{cards}){left}.", []);
    }

    private ActionResult ApplyHoldSmallBuys(HoldSmallBuysAction h)
    {
        if (h.Hold == _smallBuysHeld) return ActionResult.Stop(h.Hold ? "small buys are already held" : "small buys are not held");
        _smallBuysHeld = h.Hold;
        if (h.Hold)
            AddRecord(Day, RecordKind.Action, "smallBuys", $"{SmallBuyCustomer} small buys held. The shop stops working them until released; {SmallBuyCustomer} notices once about {Rules.SmallBuys.ComplainAfterHeldDays} days' worth are waiting.", []);
        else
            AddRecord(Day, RecordKind.Action, "smallBuys", $"{SmallBuyCustomer} small buys released; {_smallBuyBacklog} waiting.", []);
        return ActionResult.Ok(h.Hold ? "held" : "released");
    }

    private ActionResult ApplyStandUpGpcProgram()
    {
        if (_gpcProgramDay is not null) return ActionResult.Stop("the purchase card program is already running");
        if (GpcProgramInWork) return ActionResult.Stop("the purchase card program is already being stood up");
        if (_gpcAvailableDay is null) return ActionResult.Stop("no purchase card accounts this deployment; a program needs cards to put in hands");
        var hours = Fixed.Milli(Rules.GpcProgram.StandUpShopHours);
        var rec = AddRecord(Day, RecordKind.Action, "gpcProgram",
            $"Standing up a decentralized purchase card program in {SmallBuyCustomer}: nominations, training, an approving official and a billing official. About {Rules.GpcProgram.StandUpShopHours} shop hours, worked around everything else.", []);
        AddWork(WorkKind.GpcProgram, "Purchase card program", hours, Math.Max(Day, _gpcAvailableDay.Value), "gpcProgram", rec);
        return ActionResult.Ok("in work");
    }

    private void CompleteGpcProgram(int day, string causeId)
    {
        _gpcProgramDay = day;
        AddRecord(day, RecordKind.WorkDone, "gpcProgram",
            $"Purchase card program running: cardholders in {SmallBuyCustomer} now handle about {Rules.GpcProgram.DivertedPermille / 10}% of the small buys. The shop keeps the rest.", [causeId]);
    }
}
