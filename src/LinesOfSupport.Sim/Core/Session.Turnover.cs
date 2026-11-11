using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// The turnover (M2R-02, docs/design/opening-and-guidance.md 3.2). Read-only. Facts come from the scenario as the
// outgoing CCO knows it plus the shop as prep left it; inherited contracts are listed from the handover rows, so a hidden
// problem (M3-17) never reaches this text. Paragraphs are prose in the outgoing CCO's voice (R9): no bullets, no ids.
public sealed partial class Session
{
    private static string Join(IReadOnlyList<string> parts) => parts.Count switch
    {
        0 => "",
        1 => parts[0],
        2 => $"{parts[0]} and {parts[1]}",
        _ => $"{string.Join(", ", parts.Take(parts.Count - 1))} and {parts[^1]}",
    };

    /// <summary>Joins phrases that may hold their own commas or "and" with semicolons, so the list stays readable.</summary>
    private static string JoinGroups(IReadOnlyList<string> parts) =>
        parts.Count > 1 && parts.Any(x => x.Contains(',') || x.Contains(" and "))
            ? parts.Count == 2
                ? $"{parts[0]}, and {parts[1]}"
                : $"{string.Join("; ", parts.Take(parts.Count - 1))}; and {parts[^1]}"
            : Join(parts);

    private static string Para(string? remark, IEnumerable<string> sentences) =>
        string.Join(" ", new[] { remark }.Concat(sentences).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

    private static string Number(int n) => n switch
    {
        1 => "one", 2 => "two", 3 => "three", 4 => "four", 5 => "five", 6 => "six", 7 => "seven", 8 => "eight", 9 => "nine",
        10 => "ten", _ => n.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    private string IslandName(string id) => Scenario.Islands.FirstOrDefault(i => i.Id == id)?.Name ?? id;

    private static string FundType(string type) => type switch { "OM" => "O&M", _ => type };

    // What the shop knows about a vendor, said once for everyone at that tier (M2R-19): one sentence per tier, in the order
    // the outgoing CCO would vouch for them, instead of the same phrase after every name.
    private static readonly string[] ConfidenceOrder = ["observed", "surveyed", "estimated", "assumed"];

    private static string ConfidenceSentence(string tier, IReadOnlyList<string> names) => tier switch
    {
        "observed" => $"I've seen the work of {Join(names)} myself.",
        "surveyed" => $"We've been out to look at {Join(names)}.",
        "estimated" => $"I've talked to {Join(names)}, but the prices are estimates.",
        _ => names.Count == 1
            ? $"{names[0]} is just a name on a list; I've never called them."
            : $"{Join(names)} are just names on a list; I've never called them.",
    };

    private static string Tier(string infoTier) => ConfidenceOrder.Contains(infoTier) ? infoTier : "assumed";

    /// <summary>The outgoing CCO introduces the team (DESK-02): who they are, how fast, whose hours.</summary>
    private string TeamSentence()
    {
        var others = _team.Where(m => m.Role != "you").ToList();
        if (others.Count == 0) return $"It's just you, {CeilHours(Member(_lead).HoursPerDayMilli)} hours a day.";
        static string Short(TeamMember m) => m.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
        var parts = new List<string> { $"You've got {Join(others.Select(MemberName).ToList())}." };
        foreach (var m in others)
            parts.Add(m.SpeedPermille > 1000 ? $"{Short(m)} has done this before and works fast."
                    : m.SpeedPermille < 1000 ? $"{Short(m)} is new and still learning."
                    : $"{Short(m)} works at the usual pace.");
        parts.Add($"You have {CeilHours(Member(_lead).HoursPerDayMilli)} hours a day; "
                  + string.Join(", ", others.Select(m => $"{Short(m)} has {CeilHours(m.HoursPerDayMilli)}")) + ".");
        parts.Add("Hand them work, or it stays with you.");
        return string.Join(" ", parts);
    }

    internal TurnoverView BuildTurnover()
    {
        var t = Scenario.Turnover;
        var remarks = t?.Remarks ?? new TurnoverRemarksData();
        var from = BuildPerson(t?.From) ?? BuildPerson(Scenario.People.FirstOrDefault(p => p.Role == "outgoingCco")?.Id);
        var b = Scenario.Briefing;
        var briefing = b is null ? null : new BriefingView(b.Title, b.Situation, b.Due);

        // Shop, as prep left it.
        var hours = CeilHours(_hoursPerDayMilli);
        var shop = new List<string>
        {
            HasTeam ? TeamSentence() : _people == 1 ? $"It's just you, {hours} hours a day." : $"There are {Number(_people)} of you, {hours} hours a day between you.",
            _systemOnlineDay <= 1
                ? "The contract-writing system is up."
                : _systemOnlineDay > LastDay
                    ? "The contract-writing system won't be up for weeks, so everything's manual."
                    : $"The contract-writing system is down until about day {_systemOnlineDay}, so everything's manual until then.",
            _payingAgentDay is { } pa
                ? (pa <= 1 ? $"The paying agent has {Money(_cash)} in cash for SF 44 buys."
                           : $"The paying agent starts on day {pa} with {Money(_cash)} in cash for SF 44 buys.")
                : "There's no paying agent, so no cash buys.",
            _gpcAvailableDay is { } g
                ? (g <= 1 ? "Purchase cards work." : $"Purchase cards work from day {g}.")
                : "No purchase cards this time.",
        };

        // Contracts, from the handover rows. Grouped by vendor and end day so a host nation's dozen rows read as a sentence.
        var contracts = Scenario.InheritedArrangements.Select(a => new TurnoverContract(
            a.Id, CapLabel(a.Capability), VendorData(a.Vendor)?.Name ?? a.Vendor, SiteName(a.Site), a.EndDay,
            remarks.Arrangements.TryGetValue(a.Id, out var r) ? r : null)).ToList();
        var contractSentences = new List<string>();
        if (contracts.Count == 0) contractSentences.Add("You're not inheriting any contracts.");
        foreach (var grp in Scenario.InheritedArrangements.GroupBy(a => (a.Vendor, a.EndDay)))
        {
            var bySite = grp.GroupBy(a => a.Site)
                            .Select(sg => $"{Join(sg.Select(a => CapLabel(a.Capability).ToLowerInvariant()).ToList())} at {SiteName(sg.Key)}")
                            .ToList();
            var through = grp.Key.EndDay >= LastDay ? "for the whole deployment" : $"through day {grp.Key.EndDay}";
            contractSentences.Add($"{Upper(VendorData(grp.Key.Vendor)?.Name ?? grp.Key.Vendor)} has {JoinGroups(bySite)}, {through}.");
            foreach (var a in grp)
                if (remarks.Arrangements.TryGetValue(a.Id, out var rem)) contractSentences.Add(rem);
        }

        // Vendor list, with how much the shop knows about each. Support reached only through someone else's agreement (the
        // host nation's utilities, the partner force under the ACSA) is in the contracts paragraph, not a vendor you call.
        var vendors = Scenario.Vendors.Where(v => !v.VehicleOnly).Select(v => new TurnoverVendor(
            v.Id, v.Name, IslandName(v.Island), v.Offers.Select(o => CapLabel(o.Capability)).Distinct().ToList(), v.InfoTier, v.Vetted,
            remarks.Vendors.TryGetValue(v.Id, out var r) ? r : null)).ToList();
        // Where they are first (the map is how the player finds them), then what the shop knows, then the remarks.
        var vendorSentences = new List<string>();
        foreach (var island in vendors.GroupBy(v => v.IslandName))
        {
            var groups = island.GroupBy(v => Join(v.CapabilityLabels.Select(c => c.ToLowerInvariant()).ToList()))
                               .Select(g => $"{Join(g.Select(v => v.Name).ToList())} for {g.Key}").ToList();
            vendorSentences.Add($"On {island.Key} there's {JoinGroups(groups)}.");
        }
        foreach (var tier in ConfidenceOrder)
        {
            var names = vendors.Where(v => Tier(v.Confidence) == tier).Select(v => v.Name).ToList();
            if (names.Count > 0) vendorSentences.Add(ConfidenceSentence(tier, names));
        }
        foreach (var v in vendors)
            if (v.Remark is not null) vendorSentences.Add(v.Remark);
        var unvetted = vendors.Where(v => !v.Vetted).Select(v => v.Name).ToList();
        if (unvetted.Count > 0) vendorSentences.Add($"{Join(unvetted)} {(unvetted.Count == 1 ? "isn't" : "aren't")} vetted yet.");

        // Money.
        var money = _funding.Select(f => new TurnoverMoney(
            f.Id, FundType(Scenario.FundingLines.First(x => x.Id == f.Id).Type),
            f.Capabilities.Count == 0 ? "any" : Join(f.Capabilities.Select(c => CapLabel(c).ToLowerInvariant()).ToList()),
            f.RemainingCents, f.AvailableDay, f.ExpiresDay,
            remarks.FundingLines.TryGetValue(f.Id, out var r) ? r : null)).ToList();
        var moneySentences = new List<string>();
        if (money.Count == 0) moneySentences.Add("There's no funding yet.");
        foreach (var m in money)
        {
            var purpose = m.PaysFor == "any" ? "" : $" for {m.PaysFor} only";
            moneySentences.Add(m.AvailableDay <= 1
                ? $"{m.Type} has {Money(m.RemainingCents)}{purpose}, good through day {m.ExpiresDay}."
                : $"{m.Type} has {Money(m.RemainingCents)}{purpose} coming on day {m.AvailableDay}, good through day {m.ExpiresDay}.");
            if (m.Remark is not null) moneySentences.Add(m.Remark);
        }

        var sections = new List<TurnoverSection>
        {
            new(TurnoverSectionKind.Shop, Para(t?.Shop, shop)),
            new(TurnoverSectionKind.Contracts, Para(t?.Contracts, contractSentences)),
            new(TurnoverSectionKind.Vendors, Para(t?.VendorList, vendorSentences)),
            new(TurnoverSectionKind.Money, Para(t?.Money, moneySentences)),
        };
        return new TurnoverView(from, from?.Line("greeting"), briefing, sections, contracts, vendors, money, from?.Line("farewell"));
    }
}
