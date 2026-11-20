using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-106 (D38): what earned each line of the bar. The 18 Nov Claude play won the tutorial and read a debrief that
/// graded the outcome and named no decision. Under each line of the verdict one sentence names the arrangements that
/// carried it, the day each was decided and by whom, and who said a part was needed when an ask added it; a missed line
/// names its days and first cause.
/// </summary>
public class CarriedTests
{
    private static InputLog Play()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-18", "input-log.json"));

    private static Session Run(string scenario, string policyName)
    {
        var b = Repo.Load(scenario);
        var policy = PolicyCatalog.Create(policyName);
        var s = Session.Create(b, 1, policy.PrepChoices(b));
        while (!s.Finished)
        {
            foreach (var a in policy.Decide(s.View).ToList()) s.Apply(a);
            s.AdvanceDay();
        }
        return s;
    }

    private static string Line(Session s, string id) => Assert.Single(s.View.Debrief().Carried, c => c.LineId == id).Sentence;

    [Fact]
    public void The_18_Nov_play_names_who_ordered_each_line_and_who_said_the_drivers_were_needed()
    {
        var s = Replay.Run(Repo.Load("tutorial"), Play());
        Assert.Equal("Island Mart from day 2, ordered by you on day 1; then Main Island Utilities (host nation) from day 9.", Line(s, "waterUp"));
        Assert.Equal("Main Island Utilities (host nation) from the start; then Coastal Rentals with operators and maintenance from day 8, "
                     + "ordered on day 5 and written by MSgt Joan Avery, after SMSgt Ray Dunn said the request needs operators.", Line(s, "mheUp"));
        Assert.Equal("Main Island Utilities (host nation) from the start; then Main Island Sanitation from day 13, ordered on day 8 and "
                     + "written by MSgt Joan Avery.", Line(s, "wasteUp"));
        Assert.Equal("Carried by the lines above; it first dipped on day 11, when material handling equipment at Hub ran short.", Line(s, "mission"));
    }

    [Fact]
    public void One_sentence_per_line_in_the_bars_order()
    {
        var s = Replay.Run(Repo.Load("tutorial"), Play());
        var d = s.View.Debrief();
        Assert.Equal(d.Bar!.Lines.Select(l => l.Id), d.Carried.Select(c => c.LineId));
        foreach (var c in d.Carried)
        {
            Assert.EndsWith(".", c.Sentence, StringComparison.Ordinal);
            Assert.DoesNotContain("tut", c.Sentence, StringComparison.Ordinal);
            Assert.DoesNotContain("hubSite", c.Sentence, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_greedy_buy_made_without_asking_names_no_ask()
    {
        var s = Run("tutorial", "greedy");
        Assert.Equal("Main Island Utilities (host nation) from the start; then Coastal Rentals with maintenance from day 9, ordered by you on day 7.", Line(s, "mheUp"));
    }

    [Fact]
    public void A_missed_line_names_its_days_and_first_cause()
    {
        var s = Run("tutorial", "do-nothing");
        var mhe = Line(s, "mheUp");
        Assert.StartsWith("Out on days 8 to 16. Inherited host nation support for material handling equipment at Hub was withdrawn", mhe, StringComparison.Ordinal);
        Assert.Equal("It first fell on day 3, when water at Hub ran short.", Line(s, "mission"));
    }

    [Fact]
    public void Days_out_are_said_as_runs()
    {
        var s = Run("spoke-b", "greedy");
        Assert.StartsWith("Out on days 10 to 12 and 16 to 30.", Line(s, "powerUp"), StringComparison.Ordinal);
        Assert.StartsWith("Out on days 7 and 8.", Line(s, "waterUp"), StringComparison.Ordinal);   // DESK-12: the team
        Assert.Equal("Two commitments were made without authority, the first on day 11.", Line(s, "noUc"));
    }

    [Fact]
    public void A_line_nothing_the_shop_arranged_carried_says_so()
    {
        var s = Run("sustainment", "greedy");
        Assert.Equal("Second Island Fuel, left by the last rotation, covered it throughout; nothing the shop arranged was needed for it.", Line(s, "bFuelUp"));
        // Lines on neither a site and capability, mission output nor unauthorized commitments have no sentence.
        Assert.DoesNotContain(s.View.Debrief().Carried, c => c.LineId is "noHhq" or "noUnpaid");
    }

    [Fact]
    public void Building_it_changes_nothing()
    {
        var log = Play();
        var a = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 8);
        var b = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 8);
        _ = a.View.Debrief();
        Assert.Equal(b.StateHash(), a.StateHash());
    }
}
