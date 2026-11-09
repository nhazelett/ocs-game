using System.Text.RegularExpressions;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-24 (design doc R9): an event the player is told about reads as written. From a person: the label, then
/// that person's own words in quotes, no speaker prefix (the view carries the person). A notice with no person names its
/// source in its own text. No "Inbox:" tag anywhere.</summary>
public class EventTextTests
{
    public static IEnumerable<object[]> Scenarios() => [["spoke-b"], ["spoke-b-90"], ["sustainment"], ["tutorial"]];

    private static readonly Regex SpeakerPrefix = new(@"^[A-Z][A-Za-z0-9' ]{0,30}: ");

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Words_from_a_person_carry_no_speaker_prefix(string scenario)
    {
        foreach (var e in Repo.Load(scenario).Scenario.Events.Where(e => e.From is not null && e.PlayerVisibleAs is { Length: > 0 }))
            Assert.False(SpeakerPrefix.IsMatch(e.PlayerVisibleAs!), $"{scenario} event {e.Id} is from a person but starts with a speaker: {e.PlayerVisibleAs}");
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void No_record_says_Inbox(string scenario)
    {
        var bundle = Repo.Load(scenario);
        foreach (var name in PolicyCatalog.Names)
        {
            var s = Session.Create(bundle, 1);
            var policy = PolicyCatalog.Create(name);
            while (!s.Finished)
            {
                foreach (var a in policy.Decide(s.View)) s.Apply(a);
                s.AdvanceDay();
            }
            foreach (var r in s.Records)
                Assert.DoesNotContain("Inbox:", r.Text);
            foreach (var e in s.View.Debrief().Events)
                Assert.DoesNotContain("Inbox:", e.Text);
        }
    }

    [Fact]
    public void Tutorial_mentor_note_reads_as_his_words()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        s.AdvanceDay();
        var item = s.View.MorningBoard().Items.Single(i => i.LinkId == "mentorDay1");
        Assert.StartsWith("Capt Mills on the cash box. \"Watch the cash box.", item.Text);
        Assert.EndsWith("only takes cash.\"", item.Text);
        Assert.Equal("mills", item.Person?.Id);
    }

    [Fact]
    public void A_notice_names_its_own_source_without_quotes()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var policy = PolicyCatalog.Create("do-nothing");
        while (!s.Finished && !s.Records.Any(r => r.Subject == "bargeWeather"))
        {
            foreach (var a in policy.Decide(s.View)) s.Apply(a);
            s.AdvanceDay();
        }
        var rec = s.Records.Single(r => r.Subject == "bargeWeather");
        Assert.Equal("Weather stops the barge. Harbor master: no barge movements for the next three days.", rec.Text);
    }
}
