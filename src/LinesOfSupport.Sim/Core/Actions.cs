using System.Text.Json.Serialization;

namespace LinesOfSupport.Sim.Core;

/// <summary>
/// Everything a player (or a policy) can do. Serialized into input logs with "kind" first.
/// Day is the game day the action is taken on; actions apply at the start of that day, before it advances.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(SurveyAction), "survey")]
[JsonDerivedType(typeof(SetLineAction), "setLine")]
[JsonDerivedType(typeof(AwardAction), "award")]
[JsonDerivedType(typeof(CoordinateAction), "coordinate")]
[JsonDerivedType(typeof(DefinitizeAction), "definitize")]
[JsonDerivedType(typeof(TerminateAction), "terminate")]
[JsonDerivedType(typeof(ResolveCommitmentAction), "resolveCommitment")]
[JsonDerivedType(typeof(EstablishBpaAction), "establishBpa")]
[JsonDerivedType(typeof(CureNoticeAction), "cureNotice")]
[JsonDerivedType(typeof(EstablishIdiqAction), "establishIdiq")]
[JsonDerivedType(typeof(DesignateCorAction), "designateCor")]
[JsonDerivedType(typeof(RequestExternalSupportAction), "requestExternalSupport")]
public abstract record PlayerAction
{
    public int Day { get; init; }
}

/// <summary>Order a site survey of an island. Costs shop hours; findings return after a delay.</summary>
public sealed record SurveyAction : PlayerAction
{
    public string Island { get; init; } = "";
}

/// <summary>OCS seat: switch a scope line on a requirement card on or off.</summary>
public sealed record SetLineAction : PlayerAction
{
    public string Card { get; init; } = "";
    public string Line { get; init; } = "";
    public bool On { get; init; }
}

/// <summary>
/// CCO seat: create an arrangement through an instrument. The id names the arrangement for later actions.
/// Quantity is in the capability's units per day (kW, gal/day). Lines default to the card's lines when a card is given.
/// </summary>
public sealed record AwardAction : PlayerAction
{
    public string Id { get; init; } = "";
    public string Instrument { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string? Vehicle { get; init; }
    public string? Card { get; init; }
    public string? Site { get; init; }
    public string? Capability { get; init; }
    public List<string>? Lines { get; init; }
    public long Quantity { get; init; }
    public int? StartDay { get; init; }
    public int DurationDays { get; init; }
    public string? FundingLine { get; init; }
    public long? NteCents { get; init; }
    /// <summary>COR designated on the award (M3-04); null for none. Only contracts with written terms take one.</summary>
    public string? Cor { get; init; }
}

/// <summary>Borrow capacity from another site's unit. Not a contract; can be withdrawn.</summary>
public sealed record CoordinateAction : PlayerAction
{
    public string Id { get; init; } = "";
    public string Site { get; init; } = "";
    public string Capability { get; init; } = "";
    public long Quantity { get; init; }
    public string FromSite { get; init; } = "";
}

/// <summary>Definitize a UCA after the vendor's qualifying proposal, funding the definitized price from a line.</summary>
public sealed record DefinitizeAction : PlayerAction
{
    public string Arrangement { get; init; } = "";
    public string FundingLine { get; init; } = "";
}

/// <summary>End an arrangement early. Settlement cost is not modeled in v0 (docs/assumptions.md).</summary>
public sealed record TerminateAction : PlayerAction
{
    public string Arrangement { get; init; } = "";
}

/// <summary>
/// The player's decision on an unauthorized commitment (SPEC 4.5). Handshake: ratify, park or refuse.
/// Out-of-scope work: ratify or deny. FundingLine pays a ratification; omitted, the first usable line with money is used.
/// </summary>
public sealed record ResolveCommitmentAction : PlayerAction
{
    public string Commitment { get; init; } = "";
    public string Choice { get; init; } = "";
    public string? FundingLine { get; init; }
}

/// <summary>
/// Establish a blanket purchase agreement with a vendor (SPEC 4.1). Costs shop days (rules bpaEstablish); when the
/// work is done the BPA joins the shop's vehicles and takes calls. Obligates nothing: a BPA is not binding.
/// Id is the vehicle id. CallLimitCents defaults to the simplified acquisition threshold; OrderingPeriodDays to the
/// rest of the scenario.
/// </summary>
public sealed record EstablishBpaAction : PlayerAction
{
    public string Id { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string Capability { get; init; } = "";
    public List<string> ScopeLines { get; init; } = [];
    public long? CallLimitCents { get; init; }
    public int? OrderingPeriodDays { get; init; }
}

/// <summary>
/// Establish a single-award IDIQ contract with a vendor (SPEC 4.1, M3-06). Costs shop days (rules idiqEstablish,
/// 8 to 15); when the work is done the IDIQ joins the shop's vehicles and takes orders (AwardAction with instrument
/// "idiqOrder"). Binding: orders are not declined. The minimum guarantee is obligated from FundingLine when the
/// contract is awarded and is owed whether or not it is ordered; orders draw it down first.
/// Id is the vehicle id. MinimumGuaranteeCents defaults to the rules' obligatesMinimumGuaranteeFraction of the
/// ceiling, which is also its floor; OrderingPeriodDays defaults to the rest of the scenario.
/// </summary>
public sealed record EstablishIdiqAction : PlayerAction
{
    public string Id { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string Capability { get; init; } = "";
    public List<string> ScopeLines { get; init; } = [];
    public long CeilingCents { get; init; }
    public long? MinimumGuaranteeCents { get; init; }
    public int? OrderingPeriodDays { get; init; }
    public string FundingLine { get; init; } = "";
}

/// <summary>Issue a cure notice on a performance issue (FAR 49.402-3; M2-18). Costs shop hours; the vendor then has
/// the rules' cure period to recover or be in default.</summary>
public sealed record CureNoticeAction : PlayerAction
{
    public string Issue { get; init; } = "";
}

/// <summary>Designate a COR on a running or pending contract, replacing any COR it has (M3-04, DFARS 201.602-2).
/// Takes effect at once; no shop hours.</summary>
public sealed record DesignateCorAction : PlayerAction
{
    public string Arrangement { get; init; } = "";
    public string Cor { get; init; } = "";
}

/// <summary>
/// OCS seat: request an external support contract task order (SPEC 4.1, M3-05). The request costs shop days to
/// prepare (rules externalSupport.shopDays); then it goes through the SCO to the external office, which approves it
/// after rules approvalLagDays and awards it; the contractor mobilizes for rules mobilizationDays and starts no
/// earlier than StartDay. The external office obligates the task order from FundingLine when it awards (the unit's
/// money, sent to that office). The provider and its price are scenario data (actors.externalSupport).
/// Hard stops: no provider for the capability, a duration under rules minimumUsefulDurationDays, scope without the
/// core, operator and maintenance lines the provider runs (it is self-managed), quantity above the provider's
/// capacity, and the usual funding checks. Id names the arrangement it becomes. Lines default to the card's lines.
/// </summary>
public sealed record RequestExternalSupportAction : PlayerAction
{
    public string Id { get; init; } = "";
    public string? Card { get; init; }
    public string? Site { get; init; }
    public string? Capability { get; init; }
    public List<string>? Lines { get; init; }
    public long Quantity { get; init; }
    public int? StartDay { get; init; }
    public int DurationDays { get; init; }
    public string FundingLine { get; init; } = "";
    /// <summary>COR the unit nominates for local administration (M3-04); null for none.</summary>
    public string? Cor { get; init; }
}

public sealed record ActionResult(bool Accepted, string Message, IReadOnlyList<string> HardStops)
{
    public static ActionResult Ok(string message) => new(true, message, []);
    public static ActionResult Stop(params string[] reasons) => new(false, string.Join("; ", reasons), reasons);
}
