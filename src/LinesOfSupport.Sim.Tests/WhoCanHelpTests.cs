using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-42 (docs/design/desk-redesign.md D22): on a request, who can help and what talking to them does. Nick on
/// his 13 Nov replay did not know what unlocks by talking to specific people.</summary>
public class WhoCanHelpTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static Session OnForklifts()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.View.RequirementEditor("tutForklift") is null) s.AdvanceDay();
        return s;
    }

    [Fact]
    public void The_person_who_knows_is_named_with_what_asking_does_and_never_the_answer_first()
    {
        var s = OnForklifts();
        var helpers = s.View.RequirementEditor("tutForklift")!.WhoCanHelp!;
        var dunn = helpers.First(h => h.Kind == "ask");
        Assert.Equal("dunn", dunn.Person!.Id);
        Assert.True(dunn.Informs);
        Assert.Equal(HelperView.NotAsked, dunn.State);
        Assert.Equal("SMSgt Ray Dunn: not asked yet, about 1 hour.", dunn.Sentence);
        Assert.Null(dunn.Answer);
        var says = Repo.Load("tutorial").Scenario.RequirementCards.First(c => c.Id == "tutForklift").KnownBy["operator"].Says;
        Assert.All(helpers, h => Assert.DoesNotContain(says, h.Does + h.Sentence));
    }

    [Fact]
    public void Asking_shows_who_is_on_it_and_when_then_the_answer_and_what_it_opens()
    {
        var s = OnForklifts();
        Assert.True(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn", Assignee = "novak" }).Accepted);
        var asking = s.View.RequirementEditor("tutForklift")!.WhoCanHelp!.First(h => h.Kind == "ask");
        Assert.Equal(HelperView.Asking, asking.State);
        Assert.Equal("novak", asking.TeammateId);
        Assert.Equal($"SMSgt Ray Dunn: SrA Eli Novak is on it; the answer should be in on day {asking.Day}.", asking.Sentence);
        while (s.Day <= asking.Day) s.AdvanceDay();
        var answered = s.View.RequirementEditor("tutForklift")!.WhoCanHelp!.First(h => h.Kind == "ask");
        Assert.Equal(HelperView.Answered, answered.State);
        Assert.Equal(asking.Day, answered.Day);
        Assert.NotNull(answered.Answer);
        Assert.Contains("You can now add operators to the request.", answered.Sentence);
    }

    [Fact]
    public void A_vendor_not_met_is_offered_as_opening_their_card_and_drops_off_once_met()
    {
        var s = OnForklifts();
        var vendors = s.View.RequirementEditor("tutForklift")!.WhoCanHelp!.Where(h => h.Kind == "vendor").ToList();
        Assert.All(vendors, v => { Assert.False(v.Informs); Assert.Contains("fills in their vendor card", v.Does); });
        Assert.NotEmpty(vendors);
        var name = vendors[0].Name;
        var vendorId = Repo.Load("tutorial").Scenario.People.First(p => p.Id == vendors[0].Person!.Id).Vendor!;
        Assert.True(s.Apply(new MeetVendorAction { Vendor = vendorId, Assignee = "avery" }).Accepted);
        Assert.Equal(HelperView.Asking, s.View.RequirementEditor("tutForklift")!.WhoCanHelp!.Single(h => h.Name == name).State);
        s.AdvanceDay();
        Assert.DoesNotContain(s.View.RequirementEditor("tutForklift")!.WhoCanHelp!, h => h.Name == name);
    }

    [Fact]
    public void For_power_that_runs_on_fuel_the_partner_can_spare_the_J4_is_named_as_opening_an_action()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var j4 = Assert.Single(s.View.RequirementEditor("req0007")!.WhoCanHelp!, h => h.Kind == "j4");
        Assert.False(j4.Informs);
        Assert.Null(j4.Person);
        Assert.StartsWith("The J4 can ask ", j4.Does);
        Assert.Contains("to its fuel order under the ACSA", j4.Does);
        Assert.Equal(HelperView.NotAsked, j4.State);
        Assert.DoesNotContain(s.View.RequirementEditor("req0012")!.WhoCanHelp!, h => h.Kind == "j4");
    }

    [Fact]
    public void Every_request_in_every_scenario_lists_its_helpers_in_plain_words()
    {
        foreach (var scenario in new[] { "spoke-b", "spoke-b-90", "sustainment", "tutorial" })
        {
            var s = Session.Create(Repo.Load(scenario), 1);
            for (var day = 0; day < 12 && !s.Finished; day++)
            {
                foreach (var st in s.View.WhereThingsStand())
                {
                    var r = s.View.RequirementEditor(st.CardId)!;
                    Assert.NotNull(r.WhoCanHelp);
                    Assert.Equal(r.WhoToAsk!.Count, r.WhoCanHelp!.Count(h => h.Kind == "ask"));
                    Assert.All(r.WhoCanHelp!, h =>
                    {
                        Assert.EndsWith(".", h.Does, StringComparison.Ordinal);
                        Assert.EndsWith(".", h.Sentence, StringComparison.Ordinal);
                        Assert.DoesNotContain(st.CardId, h.Sentence + h.Does, StringComparison.Ordinal);
                        Assert.True(h.Hours > 0, h.Sentence);
                    });
                }
                s.AdvanceDay();
            }
        }
    }

    // Q25 (Codex, DESK-48): the J4 line names the capability it is about, and a line nobody is on yet carries the action
    // that starts it, so the request page binds each line to the Sim's own target.
    [Fact]
    public void The_J4_line_names_its_capability_and_starts_the_J4_on_it()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var power = s.View.RequirementEditor("req0007")!;
        var j4 = Assert.Single(power.WhoCanHelp!, h => h.Kind == "j4");
        Assert.Equal("fuel", j4.CapabilityId);
        var start = Assert.IsType<EngageJ4Action>(j4.Start);
        Assert.Equal("fuel", start.Capability);
        Assert.Equal(power.SiteId, start.Site);
        Assert.All(power.WhoCanHelp!.Where(h => h.Kind != "j4"), h => Assert.Null(h.CapabilityId));
    }

    [Fact]
    public void Every_line_nobody_is_on_starts_an_action_the_Sim_accepts_and_the_rest_start_nothing()
    {
        var checkedKinds = new HashSet<string>();
        foreach (var scenario in new[] { "spoke-b", "spoke-b-90", "sustainment", "tutorial" })
        {
            var s = scenario == "tutorial" ? Session.Create(Repo.Load(scenario), 1, Prep) : Session.Create(Repo.Load(scenario), 1);
            for (var day = 0; day < 12 && !s.Finished; day++)
            {
                foreach (var st in s.View.WhereThingsStand())
                    foreach (var h in s.View.RequirementEditor(st.CardId)!.WhoCanHelp!)
                    {
                        if (h.State != HelperView.NotAsked) { Assert.Null(h.Start); continue; }
                        Assert.NotNull(h.Start);
                        Assert.Equal(s.Day, h.Start!.Day);
                        var action = h.Start is EngageJ4Action j ? j with { Id = "q25", Quantity = 1 } : h.Start;
                        var preview = s.View.PreviewAny(action);
                        checkedKinds.Add(h.Kind);
                        Assert.True(preview.Accepted, $"{scenario} day {s.Day} {h.Sentence} {string.Join("; ", preview.HardStops)}");
                    }
                s.AdvanceDay();
            }
            Assert.False(s.View.PreviewAny(new MeetVendorAction { Vendor = "nobody", Day = s.Day }).Accepted);
        }
        Assert.Equal(new[] { "ask", "j4", "vendor" }, checkedKinds.Order());
    }
}
