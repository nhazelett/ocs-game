using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-17: inherited problems the handover did not mention stay out of the views until a file review (or, for
/// a past-due UCA, a higher-HQ inquiry) finds them. The Sim runs on the true state throughout.</summary>
public class FileReviewTests
{
    private static ScenarioBundle Variant(Action<JsonNode>? edit, params JsonObject[] inherited)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b-90"), "scenario.json")))!;
        var list = root["inheritedArrangements"]!.AsArray();
        foreach (var a in inherited) list.Add(a);
        edit?.Invoke(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-files-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    /// <summary>A UCA for Spoke B power issued 200 days before day 1: past due for definitization before the game starts.</summary>
    private static JsonObject PastDueUca(bool visible) => new()
    {
        ["id"] = "oldUca", ["instrument"] = "uca", ["vendor"] = "vendorB", ["site"] = "spokeBSite", ["capability"] = "power",
        ["lines"] = new JsonArray("generation", "operator", "maintenance"), ["startDay"] = -199, ["endDay"] = 60,
        ["quantity"] = 300, ["dailyCostCents"] = 600_000, ["fundingLine"] = "om1", ["visibleProblem"] = visible,
        ["ucaState"] = new JsonObject
        {
            ["nteCents"] = 100_000_000, ["obligatedCents"] = 20_000_000, ["issuedDay"] = -200,
            ["qualifyingProposalDay"] = -150, ["definitized"] = false,
        },
    };

    /// <summary>Spoke B fuel from Island Fuel (vendorD, true reliability 0.88) reported at handover as 0.99.</summary>
    private static JsonObject OverstatedFuel(bool visible = false, double? reported = 0.99) => new()
    {
        ["id"] = "oldFuel", ["instrument"] = "purchaseOrder", ["vendor"] = "vendorD", ["site"] = "spokeBSite", ["capability"] = "fuel",
        ["lines"] = new JsonArray("delivery", "storage"), ["startDay"] = -20, ["endDay"] = 90,
        ["quantity"] = 1170, ["dailyCostCents"] = 150_000, ["obligatedCents"] = 4_500_000, ["fundingLine"] = "om1",
        ["visibleProblem"] = visible, ["reportedReliability"] = reported is { } r ? r : null,
    };

    private const long Ceiling = 50_000_000, Ordered = 42_500_000;

    private static void AddFuelIdiq(JsonNode root) => root["vehicles"]!.AsArray().Add(new JsonObject
    {
        ["id"] = "fuelIdiq", ["kind"] = "idiq", ["vendor"] = "vendorE", ["scopeTags"] = new JsonArray("delivery", "storage"),
        ["ceilingCents"] = Ceiling, ["obligatedCents"] = Ordered, ["orderingPeriodEndDay"] = 90,
        ["placeOfPerformance"] = new JsonArray(), ["visibleOnDay1"] = true,
    });

    private static JsonObject IdiqOrder(bool visible) => new()
    {
        ["id"] = "aFuel", ["instrument"] = "idiqOrder", ["vendor"] = "vendorE", ["vehicle"] = "fuelIdiq", ["site"] = "spokeBSite",
        ["capability"] = "fuel", ["lines"] = new JsonArray("delivery", "storage"), ["startDay"] = -10, ["endDay"] = 40,
        ["quantity"] = 500, ["visibleProblem"] = visible,
    };

    private static void RunTo(Session s, int day) { while (s.Day < day && !s.Finished) s.AdvanceDay(); }

    private static void ReviewAll(Session s)
    {
        var r = s.Apply(new ReviewFilesAction());
        Assert.True(r.Accepted, r.Message);
    }

    [Fact]
    public void Shipped_scenarios_have_no_files_to_review()
    {
        // Their inherited rows are host nation support, which has no contract file of the shop's. Nothing is hidden,
        // so the textbook policy does not spend shop hours on a review and every golden is unchanged.
        foreach (var id in new[] { "spoke-b", "spoke-b-90" })
        {
            var s = Session.Create(Repo.Load(id), 1);
            Assert.Empty(s.View.InheritedFiles());
            Assert.All(s.Arrangements, a => Assert.False(s.View.ProblemHidden(a)));
            var r = s.Apply(new ReviewFilesAction());
            Assert.False(r.Accepted);
            Assert.Contains("no inherited contract files", r.Message);
        }
    }

    [Fact]
    public void A_hidden_past_due_uca_shows_as_an_ordinary_contract_until_the_file_is_reviewed()
    {
        var s = Session.Create(Variant(null, PastDueUca(visible: false)), 1);
        var a = s.Arrangements.Single(x => x.Id == "oldUca");
        var start = s.Records.Single(r => r.Kind == RecordKind.ArrangementStart && r.Subject == "oldUca");
        Assert.DoesNotContain("UCA issued", start.Text);
        Assert.True(s.View.ProblemHidden(a));
        s.AdvanceDay();
        // The Sim knows it is overdue; the office does not.
        Assert.True(a.Uca!.Overdue);
        var overdue = s.Records.Single(r => r.Kind == RecordKind.UcaOverdue && r.Subject == "oldUca");
        Assert.False(s.View.Known(overdue));
        Assert.DoesNotContain(s.View.MorningBoard().Items, i => i.RecordId == overdue.Id);

        ReviewAll(s);
        var preview = s.View.PreviewReviewFiles(new ReviewFilesAction());
        Assert.False(preview.Accepted);
        s.AdvanceDay();
        var found = s.Records.Single(r => r.Kind == RecordKind.Knowledge && r.Subject == "oldUca");
        Assert.Contains("past due for definitization", found.Text);
        Assert.Contains(s.View.MorningBoard().Items, i => i.RecordId == found.Id && i.LinkKind == BoardLinkKind.Arrangement);
        Assert.False(s.View.ProblemHidden(a));
        Assert.True(s.View.Known(overdue));
        Assert.Equal(found.Day, s.View.InheritedFiles().Single().ReviewedDay);
        Assert.Equal(found.Id, s.View.InheritedFiles().Single().FindingsRecordId);
    }

    [Fact]
    public void A_visible_past_due_uca_is_known_at_once()
    {
        var s = Session.Create(Variant(null, PastDueUca(visible: true)), 1);
        Assert.Contains("UCA issued", s.Records.Single(r => r.Kind == RecordKind.ArrangementStart && r.Subject == "oldUca").Text);
        s.AdvanceDay();
        Assert.True(s.View.Known(s.Records.Single(r => r.Kind == RecordKind.UcaOverdue && r.Subject == "oldUca")));
        Assert.False(s.View.ProblemHidden(s.Arrangements.Single(x => x.Id == "oldUca")));
    }

    [Fact]
    public void Higher_hq_finds_an_unreviewed_past_due_uca_the_hard_way()
    {
        var s = Session.Create(Variant(null, PastDueUca(visible: false)), 1);
        var a = s.Arrangements.Single(x => x.Id == "oldUca");
        s.AdvanceDay();
        var inquiryDay = a.Uca!.HhqInquiryDay!.Value;
        RunTo(s, inquiryDay);
        Assert.True(s.View.ProblemHidden(a));
        s.AdvanceDay();
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Compliance && r.Subject == "oldUca" && r.Day == inquiryDay);
        Assert.False(s.View.ProblemHidden(a));
        // Found, but not reviewed: the file is still there to read.
        Assert.Null(s.View.InheritedFiles().Single().ReviewedDay);
    }

    [Fact]
    public void Hiding_a_problem_changes_what_is_shown_not_what_happens()
    {
        // Same seed, same actions: every metric and every arrangement's money matches whether the problem is hidden.
        foreach (var seed in Enumerable.Range(1, 20))
        {
            var hidden = Session.Create(Variant(AddFuelIdiq, PastDueUca(false), OverstatedFuel(), IdiqOrder(false)), seed);
            var shown = Session.Create(Variant(AddFuelIdiq, PastDueUca(true), OverstatedFuel(visible: false, reported: null), IdiqOrder(true)), seed);
            RunTo(hidden, 91);
            RunTo(shown, 91);
            Assert.Equal(shown.Metrics, hidden.Metrics);
            Assert.Equal(shown.Arrangements.Select(a => (a.Id, a.Status, a.ObligatedCents, a.IncurredCents)),
                         hidden.Arrangements.Select(a => (a.Id, a.Status, a.ObligatedCents, a.IncurredCents)));
        }
    }

    [Fact]
    public void An_overstated_delivery_record_is_shown_until_the_file_is_reviewed()
    {
        var s = Session.Create(Variant(null, OverstatedFuel()), 1);
        var a = s.Arrangements.Single(x => x.Id == "oldFuel");
        // The vendor's true record (seeded around its 0.88) drives the Sim; the office sees the 0.99 reported.
        Assert.True(a.ReliabilityPermille < 900);
        Assert.Equal(990, s.View.ShownReliabilityPermille(a));
        ReviewAll(s);
        s.AdvanceDay();
        Assert.Equal(a.ReliabilityPermille, s.View.ShownReliabilityPermille(a));
        Assert.Contains("on time, not the 99% reported at handover", s.Records.Single(r => r.Kind == RecordKind.Knowledge && r.Subject == "oldFuel").Text);
    }

    [Fact]
    public void A_hidden_order_keeps_its_idiq_ceiling_and_ordered_value_out_of_the_editor()
    {
        var s = Session.Create(Variant(AddFuelIdiq, IdiqOrder(visible: false)), 1);
        Assert.True(s.View.IdiqTermsHidden("fuelIdiq"));
        var option = s.View.ActionEditor("spokeBSite", "fuel")!.Vehicles.Single(v => v.Id == "fuelIdiq");
        Assert.Null(option.CeilingCents);
        Assert.Null(option.OrderedCents);
        ReviewAll(s);
        s.AdvanceDay();
        Assert.False(s.View.IdiqTermsHidden("fuelIdiq"));
        option = s.View.ActionEditor("spokeBSite", "fuel")!.Vehicles.Single(v => v.Id == "fuelIdiq");
        Assert.Equal(Ceiling, option.CeilingCents);
        Assert.Equal(Ordered, option.OrderedCents);
        Assert.Contains("85%", s.Records.Single(r => r.Kind == RecordKind.Knowledge && r.Subject == "aFuel").Text);

        var visible = Session.Create(Variant(AddFuelIdiq, IdiqOrder(visible: true)), 1);
        Assert.False(visible.View.IdiqTermsHidden("fuelIdiq"));
        Assert.Equal(Ceiling, visible.View.ActionEditor("spokeBSite", "fuel")!.Vehicles.Single(v => v.Id == "fuelIdiq").CeilingCents);
    }

    [Fact]
    public void A_review_costs_shop_hours_per_file()
    {
        var bundle = Variant(AddFuelIdiq, PastDueUca(false), OverstatedFuel(), IdiqOrder(false));
        var s = Session.Create(bundle, 1);
        var per = bundle.Rules.FileReview.HoursPerArrangement;
        var all = s.View.PreviewReviewFiles(new ReviewFilesAction());
        Assert.True(all.Accepted);
        Assert.Equal((int)Math.Ceiling(per * 3), all.ShopHoursMin);
        var one = s.View.PreviewReviewFiles(new ReviewFilesAction { Arrangements = ["oldFuel"] });
        Assert.Equal((int)Math.Ceiling(per), one.ShopHoursMin);
        Assert.True(s.Apply(new ReviewFilesAction { Arrangements = ["oldFuel"] }).Accepted);
        var w = s.Work.Single(x => x.Kind == WorkKind.FileReview);
        Assert.Equal((long)(per * 1000), w.HoursTotalMilli);
        Assert.True(s.View.InheritedFiles().Single(f => f.ArrangementId == "oldFuel").InReview);
        // The rest can still be reviewed; the same file cannot be ordered twice.
        Assert.False(s.Apply(new ReviewFilesAction { Arrangements = ["oldFuel"] }).Accepted);
        var rest = s.Apply(new ReviewFilesAction());
        Assert.True(rest.Accepted, rest.Message);
        Assert.Equal((long)(per * 2000), s.Work.Last(x => x.Kind == WorkKind.FileReview).HoursTotalMilli);
    }

    [Theory]
    [InlineData("hubGrid", "is not an inherited contract file")]
    [InlineData("nope", "does not exist")]
    public void A_review_of_a_file_the_shop_does_not_hold_is_refused(string id, string why)
    {
        var s = Session.Create(Variant(null, OverstatedFuel()), 1);
        var r = s.Apply(new ReviewFilesAction { Arrangements = [id] });
        Assert.False(r.Accepted);
        Assert.Contains(why, r.Message);
    }

    [Fact]
    public void An_unreviewed_overstated_vendor_failure_is_discoverable_and_a_reviewed_one_avoidable()
    {
        // Island Fuel's real record (0.88, here 0.5 to make failures certain) against 0.99 reported.
        void Unreliable(JsonNode root) => root["vendors"]!.AsArray().Single(v => (string?)v!["id"] == "vendorD")!["reliability"] = 0.5;
        var bundle = Variant(Unreliable, OverstatedFuel());
        GapFactor? Factor(bool review)
        {
            var s = Session.Create(bundle, 3);
            if (review) ReviewAll(s);
            RunTo(s, 91);
            return s.View.Debrief().Gaps.SelectMany(g => g.Factors)
                     .FirstOrDefault(f => f.Kind == GapFactorKind.VendorFailure && f.Text.StartsWith(s.View.ArrangementName("oldFuel"), StringComparison.Ordinal));
        }
        var unreviewed = Factor(review: false);
        Assert.NotNull(unreviewed);
        Assert.Equal(GapTag.Discoverable, unreviewed!.Tag);
        Assert.Contains("review of the inherited files would have shown it", unreviewed.Text);
        var reviewed = Factor(review: true);
        Assert.NotNull(reviewed);
        Assert.Equal(GapTag.Avoidable, reviewed!.Tag);
        Assert.Contains("The file review on day", reviewed.Text);
    }

    [Fact]
    public void A_visible_vendor_failure_with_full_coverage_stays_unforeseeable()
    {
        void Unreliable(JsonNode root) => root["vendors"]!.AsArray().Single(v => (string?)v!["id"] == "vendorD")!["reliability"] = 0.5;
        var visibleRow = OverstatedFuel(visible: true, reported: null);
        var s = Session.Create(Variant(Unreliable, visibleRow), 3);
        RunTo(s, 91);
        var f = s.View.Debrief().Gaps.SelectMany(g => g.Factors)
                 .First(x => x.Kind == GapFactorKind.VendorFailure && x.Text.StartsWith(s.View.ArrangementName("oldFuel"), StringComparison.Ordinal));
        Assert.Equal(GapTag.Unforeseeable, f.Tag);
    }

    [Fact]
    public void The_textbook_policy_reviews_the_files_on_day_1()
    {
        var bundle = Variant(AddFuelIdiq, PastDueUca(false), OverstatedFuel(), IdiqOrder(false));
        var s = Replay.Run(bundle, 1, PolicyCatalog.Create("textbook"));
        Assert.Contains(s.AppliedActions, a => a is ReviewFilesAction && a.Day == 1);
        Assert.Equal(3, s.View.InheritedFiles().Count(f => f.ReviewedDay is not null));
        var greedy = Replay.Run(bundle, 1, PolicyCatalog.Create("greedy"));
        Assert.DoesNotContain(greedy.AppliedActions, a => a is ReviewFilesAction);
    }

    [Fact]
    public void A_review_round_trips_through_a_save()
    {
        var bundle = Variant(AddFuelIdiq, PastDueUca(false), OverstatedFuel(), IdiqOrder(false));
        var s = Session.Create(bundle, 5);
        ReviewAll(s);
        s.AdvanceDay();
        var json = s.ToSave().ToJson();
        Assert.Contains("\"reviewFiles\"", json);
        var path = Path.Combine(Path.GetTempPath(), $"los-files-save-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        var resumed = Replay.Resume(bundle, InputLog.Load(path));
        Assert.Equal(s.StateHash(), resumed.StateHash());
        Assert.False(resumed.View.ProblemHidden(resumed.Arrangements.Single(a => a.Id == "oldUca")));
    }

    [Theory]
    [InlineData("reportedReliability", 1.5, true, "reportedReliability must be between 0 and 1")]
    [InlineData("reportedReliability", 0.9, true, "has reportedReliability but visibleProblem is not false")]
    public void Bad_hidden_rows_are_refused(string field, double value, bool visible, string message)
    {
        var row = OverstatedFuel(visible: visible, reported: null);
        row[field] = value;
        var ex = Assert.Throws<InvalidDataException>(() => Variant(null, row));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void Host_nation_support_cannot_hide_a_problem()
    {
        var ex = Assert.Throws<InvalidDataException>(() => Variant(root =>
        {
            var hn = root["inheritedArrangements"]!.AsArray().First(a => (string?)a!["instrument"] == "hostNation")!;
            hn["visibleProblem"] = false;
        }));
        Assert.Contains("host nation support has no contract file", ex.Message);
    }
}
