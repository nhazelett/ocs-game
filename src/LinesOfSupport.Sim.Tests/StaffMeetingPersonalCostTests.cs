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
}
