using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>Codex's Q29 (DESK-147) and Q30 (DESK-148): a decision has a stable id, and the writer picker gives the day the
/// writing is done, not only the day support starts.</summary>
public class ReadContractsTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static AwardAction Order(string assignee) => new()
    {
        Id = "water", Instrument = "purchaseOrder", Vendor = "harborBottling", Card = "tutWater", Quantity = 4500,
        DurationDays = 6, FundingLine = "om1", Assignee = assignee,
    };

    [Fact]
    public void Q29_a_decision_keeps_its_id_through_a_hand_over_and_reads_as_it_stands()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 3) s.AdvanceDay();
        var applied = s.Apply(Order("novak"));
        Assert.True(applied.Accepted);
        var id = Assert.IsType<string>(applied.RecordId);
        Assert.Equal(id, s.View.Decisions()[0].RecordId);
        Assert.EndsWith("given to SrA Eli Novak.", s.View.Decision(id)!.Sentence);

        var work = s.View.Decisions()[0].StopWorkId!;
        var handed = s.Apply(new AssignAction { Work = work, To = "avery" });
        Assert.True(handed.Accepted);
        Assert.NotEqual(id, handed.RecordId);
        var now = s.View.Decision(id)!;
        Assert.Equal("avery", now.TeammateId);
        Assert.EndsWith("given to MSgt Joan Avery.", now.Sentence);

        while (s.View.Decision(id)!.StopWorkId is not null) s.AdvanceDay();
        Assert.Equal(id, s.View.Decision(id)!.RecordId);
        Assert.Null(s.View.Decision("no-such-record"));
        Assert.Null(s.Apply(new MeetVendorAction { Vendor = "no-such-vendor" }).RecordId);
    }

    [Fact]
    public void Q29_the_id_is_the_same_on_a_replay_of_the_same_inputs()
    {
        string Run()
        {
            var s = Session.Create(Repo.Load("tutorial"), 7, Prep);
            while (s.Day < 3) s.AdvanceDay();
            return s.Apply(Order("avery")).RecordId!;
        }
        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void Q30_the_writer_picker_gives_the_day_the_writing_is_done_before_the_start()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 3) s.AdvanceDay();
        var order = Order("you");
        var writers = s.View.AssigneeChoices(order).Where(c => c.ResultDayMin is not null).ToList();
        Assert.Equal(3, writers.Count);
        foreach (var c in writers)
        {
            var p = s.View.Preview(order with { Assignee = c.MemberId });
            Assert.Equal(p.WrittenDayMin, c.DoneDayMin);
            Assert.Equal(p.WrittenDayMax, c.DoneDayMax);
            Assert.True(c.DoneDayMin >= s.Day && c.DoneDayMin < c.ResultDayMin);
            Assert.True(c.DoneDayMax >= c.DoneDayMin && c.DoneDayMax < c.ResultDayMax);
        }

        var card = order with { Instrument = "gpc", Vendor = "islandMart", StartDay = 4 };
        Assert.All(s.View.AssigneeChoices(card), c => Assert.Null(c.DoneDayMin));
        Assert.Equal(-1, s.View.Preview(card).WrittenDayMin);

        var visit = new MeetVendorAction { Vendor = "sanitation" };
        Assert.All(s.View.AssigneeChoices(visit).Where(c => c.ResultDayMin is not null),
                   c => Assert.Equal((c.ResultDayMin, c.ResultDayMax), (c.DoneDayMin, c.DoneDayMax)));
    }
}
