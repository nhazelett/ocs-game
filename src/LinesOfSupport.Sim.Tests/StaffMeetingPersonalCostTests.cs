using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

public class StaffMeetingPersonalCostTests
{
    [Theory]
    [InlineData("you", "2 hours", "1 hour", 1)]
    [InlineData("novak", "3 hours", "1.5 hours", 1.5)]
    [InlineData("avery", "2 hours", "1 hour", 1)]
    public void Joining_quotes_the_selected_person_without_changing_state(string id, string joining, string daily, double hours)
    {
        var s=Session.Create(Repo.Load("tutorial"),1);var hash=s.StateHash();
        var meeting=s.View.StaffMeetingFor(id);
        Assert.Null(meeting.AttendeeId);
        Assert.StartsWith("Joining takes "+joining,meeting.CostSays);
        Assert.Contains(daily+" at each meeting",meeting.JoinSays);
        Assert.Equal(hours,meeting.MeetingHours);
        Assert.Equal(hash,s.StateHash());
    }

    [Fact]
    public void Pending_and_running_attendance_keep_the_real_holder()
    {
        var s=Session.Create(Repo.Load("tutorial"),1);
        Assert.True(s.Apply(new JoinStaffMeetingAction{Day=1,Assignee="novak",Attend=true}).Accepted);
        var pending=s.View.StaffMeetingFor("you");
        Assert.Equal("novak",pending.AttendeeId);
        Assert.Equal("SrA Eli Novak is getting into the staff meeting.",pending.StateSays);
        Assert.Contains("3 hours of SrA Eli Novak's time",pending.CostSays);
        s.AdvanceDay();var current=s.View.StaffMeetingFor("you");
        Assert.True(current.Attending);
        Assert.StartsWith("SrA Eli Novak is attending",current.StateSays);
        Assert.Contains("1.5 hours of SrA Eli Novak's time",current.CostSays);
        Assert.Equal(1.5,s.View.DayPlan().People.Single(p=>p.MemberId=="novak").Work.Single(w=>w.Label=="Staff meeting").HoursToday);
        Assert.True(s.Apply(new JoinStaffMeetingAction{Day=s.Day,Attend=false}).Accepted);
        Assert.Null(s.View.StaffMeeting.AttendeeId);
    }

    [Fact]
    public void The_eighth_play_day_fifteen_names_Novak_not_the_player()
    {
        var log=InputLog.Load(Path.Combine(Repo.Root,"docs","reviews","plays","2026-11-22-8","input-log-2.json"));
        var s=Replay.Run(Repo.Load("tutorial"),log,stopAfterDay:14);var hash=s.StateHash();
        Assert.Equal(15,s.Day);
        Assert.Equal("novak",s.View.StaffMeeting.AttendeeId);
        Assert.Equal("SrA Eli Novak is attending the staff meeting. The next is on day 15.",s.View.StaffMeeting.StateSays);
        Assert.Contains("1.5 hours",s.View.StaffMeeting.CostSays);
        Assert.Equal(hash,s.StateHash());
    }

    [Theory]
    [InlineData("you", "2", "1", 2000)]
    [InlineData("avery", "2", "1", 1600)]
    [InlineData("novak", "3", "1.5", 2667)]
    public void Joining_records_the_selected_persons_hours_and_keeps_the_actual_work(string id, string joining, string daily, long milli)
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        var result = s.Apply(new JoinStaffMeetingAction { Assignee = id });
        Assert.True(result.Accepted);
        var work = Assert.Single(s.Work);
        var text = $"Getting the shop into the commander's staff meeting: about {joining} shop hours, then {daily} at each meeting, every day.";
        Assert.Equal(text, s.Records.Single(r => r.Id == result.RecordId).Text);
        Assert.Equal(id, work.Assignee);
        Assert.Equal(milli, work.HoursTotalMilli);
        Assert.Equal(milli, work.HoursRemainingMilli);
        var hash = s.StateHash();
        Assert.Equal(text, Assert.Single(s.View.Team.Single(m => m.Id == id).Work).Label);
        Assert.Equal(hash, s.StateHash());
        s.AdvanceDay();
        Assert.Equal(1, work.CompletedDay);
        Assert.Equal(id, s.View.StaffMeeting.AttendeeId);
        Assert.Equal(s.View.StaffMeeting.MeetingHours,
            s.View.DayPlan().People.Single(m => m.MemberId == id).Work.Single(w => w.Label == "Staff meeting").HoursToday);
    }

    [Theory]
    [InlineData("you", "novak", "3", "1.5", 2667, "3")]
    [InlineData("novak", "avery", "2", "1", 1601, "2")]
    [InlineData("novak", "you", "2", "1", 2001, "2.5")]
    public void Handing_setup_over_records_the_new_holders_hours_and_preserves_remaining_work(string from, string to, string joining, string daily, long milli, string remaining)
    {
        var bundle = Repo.Load("tutorial");
        var s = Session.Create(bundle, 1);
        Assert.True(s.Apply(new JoinStaffMeetingAction { Assignee = from }).Accepted);
        var work = Assert.Single(s.Work);
        var original = s.Records.Last(r => r.Kind == RecordKind.Action);
        var originalText = original.Text;
        var result = s.Apply(new AssignAction { Work = work.Id, To = to });
        Assert.True(result.Accepted);
        Assert.Equal(to, work.Assignee);
        Assert.Equal(milli, work.HoursRemainingMilli);
        var record = s.Records.Single(r => r.Id == result.RecordId);
        Assert.Contains($"about {joining} shop hours, then {daily} at each meeting, every day.", record.Text);
        var oldHolder = s.View.Team.Single(m => m.Id == from);
        var oldName = oldHolder.Role == "you" ? "you" : $"{oldHolder.Rank} {oldHolder.Name}";
        Assert.EndsWith($"From {oldName}: {remaining} hours left.", record.Text);
        Assert.Equal(originalText, original.Text);
        var hash = s.StateHash();
        Assert.Contains($"about {joining} shop hours, then {daily} at each meeting", Assert.Single(s.View.Team.Single(m => m.Id == to).Work).Label);
        Assert.Equal(hash, s.StateHash());
        var resumed = Replay.Resume(bundle, s.ToSave());
        Assert.Equal(hash, resumed.StateHash());
        Assert.Equal(s.ToSave().ToJson(), resumed.ToSave().ToJson());
        s.AdvanceDay(); resumed.AdvanceDay();
        Assert.Equal(s.StateHash(), resumed.StateHash());
        Assert.Equal(1, work.CompletedDay);
    }

    [Fact]
    public void The_ninth_plays_day_two_handover_names_Novaks_hours_without_changing_the_result()
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-9", "input-log-2.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log);
        var handover = Assert.Single(s.Records.Where(r => r.Day == 2 && r.Text.StartsWith("SrA Eli Novak took over getting the shop")));
        Assert.Equal("SrA Eli Novak took over getting the shop into the commander's staff meeting: about 3 shop hours, then 1.5 at each meeting, every day. From you: 3 hours left.", handover.Text);
        Assert.Equal("novak", s.View.StaffMeeting.AttendeeId);
        Assert.Equal(.984, s.Metrics["mission.output"], precision: 6);
        Assert.Equal(1, s.Metrics["site.hubSite.water.downDays"]);
        Assert.Equal(0, s.Metrics["site.hubSite.mhe.downDays"]);
        Assert.Equal(0, s.Metrics["site.hubSite.waste.downDays"]);
        Assert.Equal(2, s.Work.Single(w => w.Id == "w2").CompletedDay);
    }

}
