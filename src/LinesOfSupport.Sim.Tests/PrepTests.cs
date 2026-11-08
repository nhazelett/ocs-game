using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2-09: the prep screen contract.</summary>
public class PrepTests
{
    [Fact]
    public void Prep_view_lists_every_choice_and_the_budget_is_a_real_tradeoff()
    {
        var b = Repo.Load("spoke-b");
        var v = PrepScreen.View(b);
        Assert.Equal(b.Rules.PrepChoices.Count, v.Options.Count);
        Assert.Equal(14, v.BudgetDays);
        Assert.True(v.AllChoicesDays > v.BudgetDays, "picking everything should not fit");
        Assert.All(v.Options, o => Assert.NotEmpty(o.Effects));
        var sys = v.Options.Single(o => o.Id == "systemAccess").Effects.Single();
        Assert.True(sys.HasEffect);
        Assert.Contains("day 3", sys.With);
        Assert.Contains("day 12", sys.Without);
        Assert.Contains(v.Options.SelectMany(o => o.Effects), e => !e.HasEffect);
    }

    [Fact]
    public void Check_agrees_with_session_create()
    {
        var b = Repo.Load("spoke-b");
        var picks = PolicyCatalog.Create("textbook").PrepChoices(b).ToList();
        var ok = PrepScreen.Check(b, picks);
        Assert.True(ok.Accepted);
        Assert.Equal(14, ok.UsedDays);
        _ = Session.Create(b, 1, picks);

        foreach (var bad in new[] { b.Rules.PrepChoices.Select(c => c.Id).ToList(), ["nope"], ["gpcAccounts", "gpcAccounts"] })
        {
            var chk = PrepScreen.Check(b, bad);
            Assert.False(chk.Accepted);
            var ex = Assert.Throws<InvalidDataException>(() => Session.Create(b, 1, bad));
            Assert.Contains(ex.Message, chk.Stops);
        }
    }
}
