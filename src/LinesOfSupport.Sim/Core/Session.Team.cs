using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// The team (DESK-02, docs/design/desk-redesign.md D2). The office is you, an experienced CCO and a junior CCO. Each
// has their own hours for the day. Every work item belongs to one person and comes out of that person's hours; work
// nobody was given stays with you. The experienced CCO gets through work faster than the junior; that is the only
// difference. Hours on a work item are the assignee's own hours: a job of the usual size is divided by their pace when
// it is created or handed over. Teammates' hours left at the end of the day go to walk-in small buys, as the shop's did.
// A scenario without shop.team has one member, you, with all the shop's hours, so older scenarios play as before.
// Names and faces are prep inputs (InputLog.Team), so a replay reads the same.
public sealed partial class Session
{
    internal sealed class TeamMember
    {
        public required string Id { get; init; }
        public required string Role { get; init; }
        public required string Name { get; set; }
        public string? Rank { get; init; }
        public string? Portrait { get; set; }
        public long HoursPerDayMilli { get; init; }
        public int SpeedPermille { get; init; } = 1000;
        /// <summary>Hours this person had left after their own work on the last day worked.</summary>
        public long HoursLeftMilli { get; set; }
    }

    private readonly List<TeamMember> _team = [];
    private string _lead = "you";
    /// <summary>Who the work created by the action being applied goes to; the lead outside an action.</summary>
    private string? _assigneeNow;

    private static List<TeamMember> BuildTeam(ShopData shop, long hoursPerPersonMilli, IReadOnlyList<TeamPick>? picks)
    {
        var team = shop.Team is { Count: > 0 } t
            ? t.Select(m => new TeamMember
            {
                Id = m.Id, Role = m.Role, Name = m.Name, Rank = m.Rank, Portrait = m.Portrait,
                HoursPerDayMilli = Fixed.Milli(m.HoursPerDay), SpeedPermille = Fixed.Permille(m.Speed),
            }).ToList()
            : [new TeamMember { Id = "you", Role = "you", Name = "you", HoursPerDayMilli = shop.People * hoursPerPersonMilli }];
        foreach (var p in picks ?? [])
        {
            var m = team.FirstOrDefault(x => x.Id == p.Id)
                    ?? throw new InvalidDataException($"team pick {p.Id} is not on this scenario's team");
            if (p.Name is { } n)
            {
                n = n.Trim();
                if (n.Length is 0 or > 40) throw new InvalidDataException($"team pick {p.Id} name must be 1 to 40 characters");
                m.Name = n;
            }
            if (p.Portrait is { } face) m.Portrait = face.Trim().Length == 0 ? null : face.Trim();
        }
        return team;
    }

    /// <summary>The names and faces the player chose, for the input log; only what differs from the scenario.</summary>
    private List<TeamPick>? TeamPicks()
    {
        if (Scenario.Shop.Team is not { } data) return null;
        var picks = new List<TeamPick>();
        foreach (var m in _team)
        {
            var d = data.First(x => x.Id == m.Id);
            var name = m.Name != d.Name ? m.Name : null;
            var face = m.Portrait != d.Portrait ? m.Portrait ?? "" : null;
            if (name is not null || face is not null) picks.Add(new TeamPick { Id = m.Id, Name = name, Portrait = face });
        }
        return picks.Count == 0 ? null : picks;
    }

    private TeamMember Member(string? id) => _team.FirstOrDefault(m => m.Id == id) ?? _team.First(m => m.Id == _lead);
    private bool HasTeam => Scenario.Shop.Team is { Count: > 0 };

    /// <summary>A job of the usual size in this person's own hours (their pace), rounded up to the milli-hour.</summary>
    private long PersonHours(long usualMilli, string? who)
    {
        var speed = Member(who).SpeedPermille;
        return speed == 1000 || usualMilli <= 0 ? usualMilli : (usualMilli * 1000 + speed - 1) / speed;
    }

    /// <summary>Days this person needs for so many of their own hours, counting today as the first.</summary>
    private int WorkDays(long personMilli, string? who)
    {
        var perDay = Member(who).HoursPerDayMilli;
        return perDay <= 0 ? 0 : (int)((personMilli + perDay - 1) / perDay);
    }

    /// <summary>
    /// DESK-37 (D17): plays this person's queue forward the way the day loop works it (their own hours, in the order the
    /// work was given, each item only from the day it is ready), with a new job of <paramref name="extraMilli"/> of their
    /// hours ready on <paramref name="fromDay"/> added at the back. Returns the day the new job is done, or with no new job
    /// the day the last of their work is; null when they hold nothing. The purchase card program, which takes only what the
    /// queue leaves, and the M2-14 reordering behind a lost-confidence site are left out.
    /// </summary>
    private int? PlayQueue(TeamMember m, long extraMilli, int fromDay)
    {
        var items = _work.Where(w => w.CompletedDay is null && w.Kind != WorkKind.GpcProgram && Member(w.Assignee).Id == m.Id)
                         .Select(w => (Wait: w.WaitUntilDay, Left: w.HoursRemainingMilli)).ToList();
        if (extraMilli > 0) items.Add((fromDay, extraMilli));
        if (items.Count == 0) return null;
        if (m.HoursPerDayMilli <= 0) return Math.Max(Day, items.Max(i => i.Wait));
        var done = new int?[items.Count];
        var lastWait = items.Max(i => i.Wait);
        for (var d = Day; done.Any(x => x is null); d++)
        {
            var left = m.HoursPerDayMilli;
            for (var i = 0; i < items.Count; i++)
            {
                if (done[i] is not null || items[i].Wait > d) continue;
                var spend = Math.Min(left, items[i].Left);
                items[i] = (items[i].Wait, items[i].Left - spend);
                left -= spend;
                if (items[i].Left <= 0) done[i] = d;
            }
            if (d > Math.Max(lastWait, Day) + 1000) break;   // cannot happen with positive hours; a guard, not a rule
        }
        return extraMilli > 0 ? done[^1] : done.Max();
    }

    /// <summary>
    /// DESK-37: the last day a job of so many of this person's own hours would be worked if given to them now and ready on
    /// <paramref name="fromDay"/>, behind the work already queued ahead of it. A job of no hours is done the day it is ready.
    /// </summary>
    private int FinishDay(long personMilli, string? who, int fromDay)
        => personMilli <= 0 ? fromDay : PlayQueue(Member(who), personMilli, fromDay) ?? fromDay;

    /// <summary>DESK-37: the last day this person works on what they already hold, or null when they hold nothing.</summary>
    private int? QueueClearsDay(TeamMember m) => PlayQueue(m, 0, Day);

    /// <summary>Hours of unfinished work this person holds.</summary>
    private int QueueHours(string? who)
    {
        var id = Member(who).Id;
        return CeilHours(_work.Where(w => w.CompletedDay is null && w.Assignee == id).Sum(w => w.HoursRemainingMilli));
    }

    /// <summary>Stops for an action's assignee: it must be on the team.</summary>
    private string? AssigneeStop(PlayerAction a)
        => a.Assignee is { } id && _team.All(m => m.Id != id) ? "that person is not on the team" : null;

    /// <summary>"you are" or "MSgt Joan Avery is", for sentences about who is doing something.</summary>
    internal string WhoIs(string? id) => Member(id).Role == "you" ? "you are" : $"{MemberName(Member(id))} is";

    private static string MemberName(TeamMember m) => m.Role == "you" ? "you" : m.Rank is { Length: > 0 } r ? $"{r} {m.Name}" : m.Name;

    // ------------------------------------------------------------------ reassign

    private ActionResult ApplyAssign(AssignAction a)
    {
        var w = _work.FirstOrDefault(x => x.Id == a.Work);
        if (w is null) return ActionResult.Stop("that work is not in the queue");
        if (w.CompletedDay is not null) return ActionResult.Stop("that work is already done");
        var to = _team.FirstOrDefault(m => m.Id == a.To);
        if (to is null) return ActionResult.Stop("that person is not on the team");
        if (w.Assignee == to.Id) return ActionResult.Stop($"{WhoIs(to.Id)} already doing it");
        var from = Member(w.Assignee);
        // Back to the usual size at the old pace, then into the new person's hours.
        long Rescale(long milli) => milli <= 0 ? milli : (milli * from.SpeedPermille * 1L + to.SpeedPermille - 1) / to.SpeedPermille;
        w.HoursRemainingMilli = Rescale(w.HoursRemainingMilli);
        w.HoursTotalMilli = Rescale(w.HoursTotalMilli);
        w.Assignee = to.Id;
        AddRecord(Day, RecordKind.Action, w.SubjectId ?? w.Id,
            $"{Upper(to.Role == "you" ? "you" : MemberName(to))} took over {LowerLabel(w.Label)} from {MemberName(from)}: {CeilHours(w.HoursRemainingMilli)} hours left.", [w.CauseId]);
        return ActionResult.Ok("reassigned");
    }

    /// <summary>Hours a work item would take if handed to this person, at their pace; null if it cannot be handed over.</summary>
    internal double? HoursIfAssigned(string workId, string memberId)
    {
        var w = _work.FirstOrDefault(x => x.Id == workId && x.CompletedDay is null);
        var to = _team.FirstOrDefault(m => m.Id == memberId);
        if (w is null || to is null) return null;
        var from = Member(w.Assignee);
        return HalfHours(w.HoursRemainingMilli <= 0 ? 0 : (w.HoursRemainingMilli * from.SpeedPermille + to.SpeedPermille - 1) / to.SpeedPermille);
    }

    /// <summary>Milli-hours shown to the player, rounded up to the half hour (DESK-14), so a faster teammate's 2.4 hours
    /// reads 2.5, not 3. Only the views round; the Sim still charges milli-hours.</summary>
    internal static double HalfHours(long milli) => milli <= 0 ? 0 : (milli + 499) / 500 / 2.0;

    /// <summary>Hours as a player reads them: "3", "2.5".</summary>
    internal static string HoursText(double hours) => hours.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ views

    internal IReadOnlyList<TeamMemberView> BuildTeam()
    {
        var result = new List<TeamMemberView>();
        foreach (var m in _team)
        {
            var open = _work.Where(w => w.CompletedDay is null && w.Assignee == m.Id).ToList();
            var readyMilli = open.Where(w => w.WaitUntilDay <= Day).Sum(w => w.HoursRemainingMilli);
            var perDay = HalfHours(m.HoursPerDayMilli);
            var today = HalfHours(Math.Min(m.HoursPerDayMilli, readyMilli));
            var work = open.Select(w => new TeamWorkView(w.Id, w.Label, HalfHours(w.HoursRemainingMilli), w.WaitUntilDay <= Day,
                w.WaitUntilDay > Day ? w.WaitUntilDay : null)).ToList();
            result.Add(new TeamMemberView(m.Id, m.Role, m.Role == "you" ? "You" : m.Name, m.Rank, m.Portrait,
                m.Role switch { "you" => "You", "experienced" => "Experienced contracting officer", _ => "Junior contracting officer" },
                Pace(m.SpeedPermille), perDay, today, Math.Max(0, perDay - today), HalfHours(open.Sum(w => w.HoursRemainingMilli)), work,
                QueueClearsDay(m)));
        }
        return result;
    }

    // ------------------------------------------------------------------ who could take it (DESK-37, D17)

    /// <summary>The preview of any action that has one, for the assignee picker.</summary>
    private ActionPreview PreviewAny(PlayerAction a) => a switch
    {
        AwardAction x => BuildPreview(x),
        EstablishBpaAction x => BuildEstablishPreview(x),
        EstablishIdiqAction x => BuildEstablishIdiqPreview(x),
        HostNationRequestAction x => BuildHostNationPreview(x),
        AcsaOrderAction x => BuildAcsaPreview(x),
        RequestExternalSupportAction x => BuildExternalSupportPreview(x),
        ReviewFilesAction x => BuildReviewFilesPreview(x),
        _ => BuildVisitPreview(a),
    };

    internal IReadOnlyList<AssigneeChoice> BuildAssigneeChoices(PlayerAction action)
    {
        var team = BuildTeam();
        var result = new List<AssigneeChoice>();
        // A buy's preview day is when support starts; anything else's is when the work's result is in.
        var starts = action is AwardAction or HostNationRequestAction or AcsaOrderAction or RequestExternalSupportAction;
        foreach (var m in team)
        {
            var p = Said(PreviewAny(action with { Assignee = m.Id }));
            var who = m.Role == "you" ? "You" : m.Rank is { Length: > 0 } r ? $"{r} {m.Name}" : m.Name;
            var them = m.Role == "you" ? "you" : "them";
            var load = m.HoursQueued <= 0
                ? $"{who}: {Hrs(m.FreeHoursToday)} free today, nothing queued."
                : m.FreeHoursToday > 0
                    ? $"{who}: {Hrs(m.FreeHoursToday)} free today, {Hrs(m.HoursQueued)} queued, clear by day {m.QueueClearsDay}."
                    : $"{who}: no hours free today, {Hrs(m.HoursQueued)} queued, clear by day {m.QueueClearsDay}.";
            string? when = null;
            if (p.Accepted)
            {
                var days = p.EarliestStartMin == p.EarliestStartMax ? $"day {p.EarliestStartMin}" : $"day {p.EarliestStartMin} to {p.EarliestStartMax}";
                var hours = p.ShopHoursMax <= 0 ? null
                    : p.ShopHoursMin == p.ShopHoursMax ? Hrs(p.ShopHoursMax) : $"{p.ShopHoursMin} to {p.ShopHoursMax} hours";
                var lead = hours is null ? "With " + them : $"This takes {them} {hours}";
                when = starts ? $"{lead}; it could start {days}." : $"{lead}; it would be done {days}.";
            }
            result.Add(new AssigneeChoice(m.Id, who, m.Portrait, m.FreeHoursToday, m.HoursQueued, m.QueueClearsDay,
                p.Accepted ? p.ShopHoursMin : 0, p.Accepted ? p.ShopHoursMax : 0,
                p.Accepted ? p.EarliestStartMin : null, p.Accepted ? p.EarliestStartMax : null,
                when is null ? load : $"{load} {when}"));
        }
        return result;

        static string Hrs(double h) => h == 1 ? "1 hour" : $"{h:0.#} hours";
    }

    private static string LowerLabel(string s) => s.Length > 1 && char.IsUpper(s[1]) ? s : Lower(s);

    private static string Pace(int speedPermille) => speedPermille switch
    {
        > 1000 => "Gets through work faster than most.",
        < 1000 => "Still learning: work takes longer.",
        _ => "Works at the usual pace.",
    };
}
