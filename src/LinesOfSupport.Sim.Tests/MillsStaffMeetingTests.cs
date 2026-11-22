using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-207 (Claude play 22 Nov, note P2-7). The second player joined Col Ward's staff meeting on day 1, and that
/// night Mills's note told them to get the shop into it: the advice arrived after the decision it advised on. The staff
/// meeting sentence is now its own note, sent only when the shop is neither attending nor joining.</summary>
public class MillsStaffMeetingTests
{
    private static bool StaffMeetingNote(Session s) => s.Records.Any(r => r.Kind == RecordKind.Event && r.Subject == "mentorStaffMeeting");

    [Fact]
    public void A_shop_that_joined_on_day_one_gets_no_staff_meeting_note()
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22", "input-log-2.json"));
        Assert.Contains(log.Actions, a => a is JoinStaffMeetingAction { Attend: true } && a.Day == 1);
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 1);
        Assert.False(StaffMeetingNote(s));
        var agreements = s.Records.Single(r => r.Kind == RecordKind.Event && r.Subject == "mentorAgreements");
        Assert.DoesNotContain("staff meeting", agreements.Text);
    }

    [Fact]
    public void A_shop_that_never_joins_gets_it_on_day_one()
    {
        var s = Session.Create(Repo.Load("tutorial"), 20261122);
        s.AdvanceDay();
        var note = Assert.Single(s.Records, r => r.Kind == RecordKind.Event && r.Subject == "mentorStaffMeeting");
        Assert.Equal(1, note.Day);
        // Its window is day 1 only: joining later does not bring it back, and it is never sent twice.
        while (!s.Finished) s.AdvanceDay();
        Assert.Single(s.Records, r => r.Kind == RecordKind.Event && r.Subject == "mentorStaffMeeting");
    }
}
