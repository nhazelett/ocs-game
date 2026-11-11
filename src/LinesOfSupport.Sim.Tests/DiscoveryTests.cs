using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-03 (docs/design/desk-redesign.md D3): a request arrives incomplete; asking the right person reveals
/// what it left out, and only then can it be added. A part nobody tells you about still bites.</summary>
public class DiscoveryTests
{
    private static Session At(string scenario, string card)
    {
        var s = Session.Create(Repo.Load(scenario), 1);
        var day = s.Scenario.RequirementCards.First(c => c.Id == card).SubmittedDay;
        while (s.Day < day) s.AdvanceDay();
        return s;
    }

    private static Views.RequirementLine Line(Session s, string card, string line)
        => s.View.RequirementEditor(card)!.Lines.Single(l => l.Id == line);

    [Fact]
    public void Asking_the_customer_reveals_the_missing_operators_and_then_they_can_be_added()
    {
        var s = At("tutorial", "tutForklift");
        var before = Line(s, "tutForklift", "operator");
        Assert.False(before.Known);
        Assert.False(before.CanAdd);
        var ask = Assert.Single(s.View.RequirementEditor("tutForklift")!.WhoToAsk!);
        Assert.Equal("dunn", ask.Person.Id);
        Assert.False(s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" }).Accepted);

        Assert.True(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn" }).Accepted);
        Assert.True(s.View.RequirementEditor("tutForklift")!.WhoToAsk![0].Pending);
        s.AdvanceDay();

        var heard = s.Records.Last(r => r.Kind == RecordKind.Visit);
        Assert.StartsWith("SMSgt Ray Dunn, LRS: \"I should have written this down", heard.Text, StringComparison.Ordinal);
        var after = Line(s, "tutForklift", "operator");
        Assert.True(after.Known);
        Assert.True(after.CanAdd);
        Assert.False(after.On);
        Assert.Equal("dunn", after.LearnedFrom!.Id);
        Assert.Contains("come with drivers", after.Learned, StringComparison.Ordinal);
        Assert.True(s.View.RequirementEditor("tutForklift")!.WhoToAsk![0].Asked);

        Assert.True(s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" }).Accepted);
        Assert.True(Line(s, "tutForklift", "operator").On);
        Assert.False(Line(s, "tutForklift", "operator").CanAdd);
        Assert.Contains("Operators added, as SMSgt Ray Dunn said.", s.Records.Last(r => r.Kind == RecordKind.Action).Text, StringComparison.Ordinal);
        Assert.False(s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" }).Accepted);
        Assert.False(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn" }).Accepted);
    }

    [Fact]
    public void Someone_who_knows_nothing_says_so_and_the_one_who_knows_is_listed()
    {
        var s = At("spoke-b", "req0012");
        Assert.Equal(["okafor", "ruiz"], s.View.RequirementEditor("req0012")!.WhoToAsk!.Select(a => a.Person.Id));
        Assert.True(s.Apply(new AskAboutAction { Card = "req0012", Person = "okafor" }).Accepted);
        s.AdvanceDay();
        var said = s.Records.Last(r => r.Kind == RecordKind.Visit).Text;
        Assert.EndsWith("\"Nothing I can add on the request for water for Spoke C.\"", said, StringComparison.Ordinal);
        Assert.DoesNotContain("req0012", said, StringComparison.Ordinal);
        Assert.All(s.View.RequirementEditor("req0012")!.Lines.Where(l => l.Kind != "core"), l => Assert.False(l.Known));

        // CE knows nobody runs the plant (D3's own example).
        Assert.True(s.Apply(new AskAboutAction { Card = "req0012", Person = "ruiz" }).Accepted);
        s.AdvanceDay();
        Assert.Contains("Nobody is running the plant at Spoke C.", s.Records.Last(r => r.Kind == RecordKind.Visit).Text, StringComparison.Ordinal);
        Assert.True(Line(s, "req0012", "operator").CanAdd);
        Assert.True(Line(s, "req0012", "maintenance").CanAdd);
    }

    [Fact]
    public void Spoke_B_fuel_is_something_nobody_tells_you_so_it_still_bites()
    {
        var s = At("spoke-b", "req0007");
        Assert.True(s.Apply(new AskAboutAction { Card = "req0007", Person = "ruiz" }).Accepted);
        s.AdvanceDay();
        Assert.True(Line(s, "req0007", "operator").CanAdd);
        var fuel = Line(s, "req0007", "fuelSupply");
        Assert.False(fuel.Known);
        Assert.False(fuel.CanAdd);
        Assert.False(s.Apply(new AddLineAction { Card = "req0007", Line = "fuelSupply" }).Accepted);
        // The dependency checklist is still where fuel shows up: owned by the J4 and not covered.
        var dep = s.View.RequirementEditor("req0007")!.Dependencies.Single(d => d.CapabilityId == "fuel");
        Assert.Equal("none", dep.Coverage);
    }

    [Fact]
    public void Set_line_is_not_gated_for_the_policies_and_older_logs()
    {
        var s = At("spoke-b", "req0007");
        Assert.True(s.Apply(new SetLineAction { Card = "req0007", Line = "operator", On = true }).Accepted);
        Assert.True(Line(s, "req0007", "operator").On);
        Assert.True(Line(s, "req0007", "operator").Known);
    }

    [Fact]
    public void Asking_and_adding_replay_from_the_input_log()
    {
        var bundle = Repo.Load("tutorial");
        var s = At("tutorial", "tutForklift");
        s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn", Assignee = "novak" });
        s.AdvanceDay();
        s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" });
        var save = s.ToSave();
        var again = Replay.Resume(bundle, InputLog.Load(Write(save)));
        Assert.True(Line(again, "tutForklift", "operator").On);
        Assert.Equal(s.StateHash(), again.StateHash());
        Assert.False(again.Apply(new AskAboutAction { Card = "tutForklift", Person = "nobody" }).Accepted);
    }

    private static string Write(InputLog log)
    {
        var path = Path.Combine(Path.GetTempPath(), $"los-ask-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, log.ToJson());
        return path;
    }
}
