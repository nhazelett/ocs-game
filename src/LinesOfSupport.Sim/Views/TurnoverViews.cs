namespace LinesOfSupport.Sim.Views;

// The opening (M2R-02, docs/design/opening-and-guidance.md 3.2, R9). Built from a session on day 1, before anything is
// played: the briefing, then four turnover sections in the outgoing CCO's voice. Each section's Text is a short paragraph
// of prose (no bullets, no headers, no ids); the rows beside it are the same facts for a ledger or a tooltip.
// Hidden inherited problems (M3-17) stay hidden: contracts show what the outgoing CCO believes.

/// <summary>The scenario's opening lines: who you are, where you are, what is due.</summary>
public sealed record BriefingView(string Title, string Situation, string Due);

/// <summary>Which turnover section a paragraph belongs to, in the order the turnover tells them.</summary>
public enum TurnoverSectionKind { Shop, Contracts, Vendors, Money }

/// <summary>One paragraph of the turnover, in the outgoing CCO's voice.</summary>
public sealed record TurnoverSection(TurnoverSectionKind Kind, string Text);

/// <summary>An inherited contract as handed over. Where is the site's name; Remark is the outgoing CCO's, if any.</summary>
public sealed record TurnoverContract(
    string ArrangementId, string CapabilityLabel, string VendorName, string SiteName, int ThroughDay, string? Remark);

/// <summary>A vendor on the outgoing CCO's list. Confidence is the vendor's information tier (assumed, estimated,
/// surveyed, observed): how much the shop knows about them, not how good they are.</summary>
public sealed record TurnoverVendor(
    string VendorId, string Name, string IslandName, IReadOnlyList<string> CapabilityLabels, string Confidence, bool Vetted, string? Remark);

/// <summary>A funding line as handed over: what it pays for ("any" when not restricted), what is left, when it ends.</summary>
public sealed record TurnoverMoney(
    string FundingLineId, string Type, string PaysFor, long RemainingCents, int AvailableDay, int ExpiresDay, string? Remark);

/// <summary>The turnover: who hands over, their greeting, the four sections in order, the rows, and their last line,
/// which hands the player to the first customer.</summary>
/// <param name="From">The outgoing CCO, or null when the scenario has none.</param>
/// <param name="Briefing">Null when the scenario has no briefing.</param>
/// <param name="Sections">Shop, Contracts, Vendors, Money, always all four, in that order.</param>
public sealed record TurnoverView(
    PersonView? From, string? Greeting, BriefingView? Briefing, IReadOnlyList<TurnoverSection> Sections,
    IReadOnlyList<TurnoverContract> Contracts, IReadOnlyList<TurnoverVendor> Vendors, IReadOnlyList<TurnoverMoney> Money,
    string? LastLine)
{
    public string Text(TurnoverSectionKind kind) => Sections.First(s => s.Kind == kind).Text;
}
