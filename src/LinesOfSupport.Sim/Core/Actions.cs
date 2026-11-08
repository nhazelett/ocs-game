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

public sealed record ActionResult(bool Accepted, string Message, IReadOnlyList<string> HardStops)
{
    public static ActionResult Ok(string message) => new(true, message, []);
    public static ActionResult Stop(params string[] reasons) => new(false, string.Join("; ", reasons), reasons);
}
