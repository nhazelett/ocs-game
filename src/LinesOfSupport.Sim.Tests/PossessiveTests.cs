using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-115 (Fable's review of PRs 352 and 353): the preparation page read "Island Camp Services's BPA". Agreements are
/// named the way the rest of the game names them ("the Island Camp Services BPA"), and every sentence that puts a
/// vendor, site or person in the possessive uses one rule: a name ending in s takes the apostrophe alone.
/// </summary>
public class PossessiveTests
{
    [Fact]
    public void The_preparation_page_names_agreements_as_the_desk_does()
    {
        var pull = PrepScreen.Choice(Repo.Load("tutorial"), "pullVehicles");
        Assert.Equal("Each request names the agreements already in place that could serve it from day 1 (the Island Camp Services BPA and the Harbor Equipment Co BPA).", pull.Changes);
    }

    [Fact]
    public void No_sentence_in_the_source_writes_s_apostrophe_s_after_a_name()
    {
        // The literal patterns that built "Services's": a name interpolated, then 's. Possessive() is the one place.
        var src = Path.Combine(Repo.Root, "src", "LinesOfSupport.Sim");
        var offenders = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(f => File.ReadLines(f).Select((l, i) => (f, i, l)))
            .Where(x => x.l.Contains(".Name}'s", StringComparison.Ordinal) || x.l.Contains("Name + \"'s\"", StringComparison.Ordinal)
                        || x.l.Contains("?.Name ?? \"a vendor\"}'s", StringComparison.Ordinal))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}")
            .ToList();
        Assert.Empty(offenders);
    }
}
