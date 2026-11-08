using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-16: inherited arrangements with their own quantity, price, obligation and funding line, and inherited
/// UCAs whose definitization clocks run from the day they were issued under the last rotation.</summary>
public class InheritedTests
{
    private static ScenarioBundle Variant(params JsonObject[] inherited)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b-90"), "scenario.json")))!;
        var list = root["inheritedArrangements"]!.AsArray();
        foreach (var a in inherited) list.Add(a);
        var dir = Path.Combine(Path.GetTempPath(), $"los-inh-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    /// <summary>An inherited UCA for Spoke B power from Meridian Site Services, $6,000 a day, NTE $1,000,000.</summary>
    private static JsonObject Uca(int issuedDay, long priorObligated, int? proposalDay, int endDay = 60) => new()
    {
        ["id"] = "oldUca", ["instrument"] = "uca", ["vendor"] = "vendorB", ["site"] = "spokeBSite", ["capability"] = "power",
        ["lines"] = new JsonArray("generation", "operator", "maintenance"), ["startDay"] = issuedDay + 1, ["endDay"] = endDay,
        ["quantity"] = 300, ["dailyCostCents"] = 600_000, ["fundingLine"] = "om1",
        ["ucaState"] = new JsonObject
        {
            ["nteCents"] = 100_000_000, ["obligatedCents"] = priorObligated, ["issuedDay"] = issuedDay,
            ["qualifyingProposalDay"] = proposalDay is { } p ? p : null, ["definitized"] = false,
        },
    };

    [Fact]
    public void Every_shipped_scenario_loads_its_inherited_rows_as_before()
    {
        // The shipped inherited rows (host nation power and fuel at the hub) set none of the new fields: they still
        // bill nothing and supply the vendor's capacity. The goldens hold this byte for byte; this names it.
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        foreach (var a in s.Arrangements.Where(a => a.CreatedDay == 0))
        {
            Assert.Equal(0, a.DailyCostCents);
            Assert.Equal(0, a.ObligatedCents);
            Assert.Null(a.Uca);
            Assert.Empty(a.SuppliedQuantityMilli);
        }
    }

    [Fact]
    public void An_inherited_contract_takes_its_quantity_price_and_obligation()
    {
        var bundle = Variant(new JsonObject
        {
            ["id"] = "oldFuel", ["instrument"] = "purchaseOrder", ["vendor"] = "vendorD", ["site"] = "spokeBSite", ["capability"] = "fuel",
            ["lines"] = new JsonArray("delivery", "storage"), ["startDay"] = -20, ["endDay"] = 30,
            ["quantity"] = 500, ["dailyCostCents"] = 150_000, ["obligatedCents"] = 4_500_000, ["fundingLine"] = "om1",
        });
        var s = Session.Create(bundle, 1);
        var a = s.Arrangements.Single(x => x.Id == "oldFuel");
        Assert.Equal(500_000, a.QuantityMilli);
        Assert.Equal(ArrangementStatus.Active, a.Status);
        Assert.Equal(4_500_000, s.View.Funding.Single(f => f.Id == "om1").ObligatedCents);
        while (s.Day <= 31) s.AdvanceDay();
        // 30 days at $1,500 with no mobilization (it mobilized under the last rotation), all covered.
        Assert.Equal(4_500_000, a.IncurredCents);
        Assert.Equal(0, a.UnobligatedIncurredCents);
        Assert.Equal(ArrangementStatus.Ended, a.Status);
    }

    [Fact]
    public void An_inherited_uca_with_a_proposal_in_hand_comes_due_on_its_own_clock()
    {
        var rules = Repo.Load("spoke-b-90").Rules.Uca;
        var s = Session.Create(Variant(Uca(issuedDay: -150, priorObligated: 30_000_000, proposalDay: -40)), 1);
        var u = s.Arrangements.Single(x => x.Id == "oldUca").Uca!;
        Assert.True(u.ProposalReceived);
        Assert.DoesNotContain(s.Records, r => r.Kind == RecordKind.UcaProposal && r.Subject == "oldUca");
        var due = -150 + rules.DefinitizeWithinDays;
        while (s.Day <= due) s.AdvanceDay();
        var overdue = s.Records.Single(r => r.Kind == RecordKind.UcaOverdue && r.Subject == "oldUca");
        Assert.Equal(due, overdue.Day);
        Assert.Equal(due + rules.HhqInquiryAfterOverdueDays, u.HhqInquiryDay);
    }

    [Fact]
    public void Prior_obligations_count_toward_the_cap_and_the_definitized_price()
    {
        // 45% of NTE obligated before day 1; $6,000 a day is 0.6% of NTE, so the 50% trigger comes on day 9.
        var bundle = Variant(Uca(issuedDay: -10, priorObligated: 45_000_000, proposalDay: -2, endDay: 150));
        var s = Session.Create(bundle, 1);
        var arr = s.Arrangements.Single(x => x.Id == "oldUca");
        var u = arr.Uca!;
        Assert.Equal(600_000L * (150 - (-9) + 1), u.QuotedTotalCents);
        // The 50% trigger: day 9 brings obligations to 45% + 9 x 0.6% = 50.4%.
        s.AdvanceDay();
        Assert.False(u.Overdue);
        while (s.Day <= 9) s.AdvanceDay();
        Assert.True(u.Overdue);
        Assert.Equal(9, s.Records.Single(r => r.Kind == RecordKind.UcaOverdue && r.Subject == "oldUca").Day);

        // Definitization funds the price less everything obligated, before day 1 and since.
        var def = Session.Create(bundle, 1);
        Assert.True(def.Apply(new DefinitizeAction { Arrangement = "oldUca", FundingLine = "om1" }).Accepted);
        while (def.Arrangements.Single(x => x.Id == "oldUca").Uca is { Definitized: false }) def.AdvanceDay();
        var d = def.Arrangements.Single(x => x.Id == "oldUca");
        Assert.Equal(d.Uca!.QuotedTotalCents - 45_000_000, d.ObligatedCents);
    }

    [Fact]
    public void Prior_obligations_use_up_the_cap_before_the_proposal()
    {
        // 45% obligated before day 1 against a 50% cap before the proposal: $50,000 of room, about eight days at $6,000.
        var s = Session.Create(Variant(Uca(issuedDay: 0, priorObligated: 45_000_000, proposalDay: 40, endDay: 150)), 1);
        var arr = s.Arrangements.Single(x => x.Id == "oldUca");
        while (s.Day <= 12) s.AdvanceDay();
        Assert.Equal(5_000_000, arr.ObligatedCents);
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Obligation && r.Subject == "oldUca" && r.Text.Contains("obligation cap"));
    }

    [Fact]
    public void A_uca_past_due_before_day_1_is_overdue_on_day_1_and_draws_the_inquiry()
    {
        var rules = Repo.Load("spoke-b-90").Rules.Uca;
        var s = Session.Create(Variant(Uca(issuedDay: -200, priorObligated: 20_000_000, proposalDay: -150)), 1);
        s.AdvanceDay();
        var u = s.Arrangements.Single(x => x.Id == "oldUca").Uca!;
        Assert.True(u.Overdue);
        Assert.Equal(1 + rules.HhqInquiryAfterOverdueDays, u.HhqInquiryDay);
        while (s.Day <= u.HhqInquiryDay) s.AdvanceDay();
        Assert.Equal(1, s.Metrics["compliance.hhqInquiries"]);
    }

    [Fact]
    public void An_inherited_uca_without_a_proposal_draws_one_after_issue()
    {
        var a = Session.Create(Variant(Uca(issuedDay: 0, priorObligated: 0, proposalDay: null)), 1);
        var b = Session.Create(Variant(Uca(issuedDay: 0, priorObligated: 0, proposalDay: null)), 1);
        var ua = a.Arrangements.Single(x => x.Id == "oldUca").Uca!;
        Assert.Equal(ua.QualifyingProposalDay, b.Arrangements.Single(x => x.Id == "oldUca").Uca!.QualifyingProposalDay);
        Assert.True(ua.QualifyingProposalDay >= 1);
        Assert.False(ua.ProposalReceived);
        while (a.Day <= ua.QualifyingProposalDay) a.AdvanceDay();
        Assert.True(ua.ProposalReceived);
    }

    public static IEnumerable<object[]> BadRows() =>
    [
        [new JsonObject { ["quantity"] = 0 }, "quantity must be positive"],
        [new JsonObject { ["obligatedCents"] = 100, ["fundingLine"] = null }, "obligatedCents but no fundingLine"],
        [new JsonObject { ["fundingLine"] = "nope" }, "funding line nope not found"],
        [new JsonObject { ["instrument"] = "purchaseOrder" }, "has ucaState but instrument purchaseOrder"],
        [new JsonObject { ["endDay"] = -50 }, "ends before it starts"],
    ];

    [Theory]
    [MemberData(nameof(BadRows))]
    public void The_bundle_check_rejects_bad_inherited_rows(JsonObject change, string expected)
    {
        var row = Uca(issuedDay: -10, priorObligated: 1_000_000, proposalDay: null);
        foreach (var (k, v) in change) row[k] = v?.DeepClone();
        var ex = Assert.ThrowsAny<Exception>(() => Variant(row));
        Assert.Contains(expected, ex.Message);
    }
}
