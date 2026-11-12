using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Requirements are completed by asking the right person (DESK-03, docs/design/desk-redesign.md D3). A request arrives
// incomplete, as it always did. A card's knownBy says who knows each part it left out and what they say. Asking that
// person about the request (a short visit) reveals the part; then "add what you learned" puts it on the request. A part
// nobody is asked about, or nobody knows, still bites, as Spoke B's fuel does. SetLineAction stays for the policies
// and older logs and is not gated.
public sealed partial class Session
{
    /// <summary>Card id to the people already asked about it (asked, not still on the way).</summary>
    private readonly Dictionary<string, HashSet<string>> _askedAbout = new(StringComparer.Ordinal);
    /// <summary>(card, line) revealed, with the day and the person who told you.</summary>
    private readonly Dictionary<(string Card, string Line), (int Day, string Person)> _learnedLines = [];

    private List<string> CheckAskAbout(AskAboutAction a)
    {
        var stops = new List<string>();
        var card = _cards.FirstOrDefault(c => c.Id == a.Card);
        if (card is null) { stops.Add("no such request"); return stops; }
        if (Day < card.SubmittedDay) { stops.Add($"{CardName(card.Id)} has not been brought to you yet"); return stops; }
        if (BuildPerson(a.Person) is not { } p) { stops.Add("no such person"); return stops; }
        if (_askedAbout.TryGetValue(card.Id, out var asked) && asked.Contains(a.Person))
            stops.Add($"you have already asked {p.DisplayName} about {CardName(card.Id)}");
        else if (VisitInWork("ask", AskTarget(a))) stops.Add($"a talk with {p.DisplayName} about {CardName(card.Id)} is already set up");
        return stops;
    }

    private static string AskTarget(AskAboutAction a) => $"{a.Card}|{a.Person}";

    private ActionResult ApplyAskAbout(AskAboutAction a)
    {
        var (stops, _) = CheckVisit(a);
        if (stops.Count > 0) return ActionResult.Stop([.. stops]);
        return StartVisit(a, "ask", AskTarget(a), PersonSubject(a.Person, a.Card),
            $"Asking {BuildPerson(a.Person)!.WithUnit} about {CardName(a.Card)}: about {People.AskAbout.ShopHours} shop hour{(People.AskAbout.ShopHours == 1 ? "" : "s")}.", a);
    }

    private void CompleteAskAbout(AskAboutAction a, int day, string cause)
    {
        var card = _cards.First(c => c.Id == a.Card);
        var data = Scenario.RequirementCards.First(c => c.Id == a.Card);
        if (!_askedAbout.TryGetValue(card.Id, out var asked)) _askedAbout[card.Id] = asked = new(StringComparer.Ordinal);
        asked.Add(a.Person);
        var told = new List<string>();
        foreach (var (line, k) in data.KnownBy.OrderBy(kv => LineOrder(card.CapabilityId, kv.Key)))
        {
            if (k.Person != a.Person) continue;
            _learnedLines.TryAdd((card.Id, line), (day, a.Person));
            told.Add(k.Says);
        }
        var said = told.Count > 0 ? string.Join(" ", told) : $"Nothing I can add on {CardName(card.Id)}.";
        AddRecord(day, RecordKind.Visit, PersonSubject(a.Person, card.Id), $"{Who(a.Person, "They")}: \"{said}\"", [cause]);
    }

    private int LineOrder(string cap, string line) => Cap(cap).Lines.FindIndex(l => l.Id == line);

    private ActionResult ApplyAddLine(AddLineAction a)
    {
        var card = _cards.FirstOrDefault(c => c.Id == a.Card);
        if (card is null) return ActionResult.Stop("no such request");
        if (!_learnedLines.TryGetValue((card.Id, a.Line), out var from))
            return ActionResult.Stop("that is not something anyone has told you about this request");
        if (card.Lines.GetValueOrDefault(a.Line)) return ActionResult.Stop($"{LineLabel(card.CapabilityId, a.Line)} is already in the request");
        card.Lines[a.Line] = true;
        AddRecord(Day, RecordKind.Action, card.Id,
            $"{Upper(CardName(card.Id))}: {LineLabel(card.CapabilityId, a.Line)} added, as {BuildPerson(from.Person)?.DisplayName ?? "they"} said.", []);
        return ActionResult.Ok($"{Upper(LineLabel(card.CapabilityId, a.Line))} added to the request.");   // DESK-36: the game shows this
    }

    /// <summary>True when someone has told you this line is needed (or it was in the request all along).</summary>
    internal bool LineKnown(CardState card, string line) => card.Lines.GetValueOrDefault(line) || _learnedLines.ContainsKey((card.Id, line));

    /// <summary>What the person said about this line, once they have.</summary>
    internal (string Says, PersonView? From)? LineLearned(CardState card, string line)
        => _learnedLines.TryGetValue((card.Id, line), out var r)
            ? (Scenario.RequirementCards.First(c => c.Id == card.Id).KnownBy[line].Says, BuildPerson(r.Person))
            : null;

    /// <summary>People worth asking about a request: whoever brought it, then anyone who knows a part it left out.</summary>
    internal IReadOnlyList<AskOption> BuildWhoToAsk(CardState card)
    {
        var data = Scenario.RequirementCards.First(c => c.Id == card.Id);
        var ids = new List<string>();
        if (card.PersonId is { } brought) ids.Add(brought);
        foreach (var k in data.KnownBy.OrderBy(kv => LineOrder(card.CapabilityId, kv.Key)).Select(kv => kv.Value.Person))
            if (!ids.Contains(k)) ids.Add(k);
        var asked = _askedAbout.GetValueOrDefault(card.Id);
        return ids.Select(id => new AskOption(BuildPerson(id)!, asked?.Contains(id) ?? false,
            VisitInWork("ask", AskTarget(new AskAboutAction { Card = card.Id, Person = id })))).ToList();
    }
}
