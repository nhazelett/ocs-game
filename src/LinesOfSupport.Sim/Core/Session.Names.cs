using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Plain names for things the player sees (backlog M2R-07, design doc R6). Ids stay the keys for actions, records and
// links; every sentence a player can read names things instead: arrangements by who and what and where, funding lines by
// type and dates, requirements by what and where, vehicles by kind and vendor, people by rank and name.
public sealed partial class Session
{
    /// <summary>"Harbor Equipment Co power at Spoke B": who delivers what, where. Falls back to what and where.</summary>
    internal string ArrName(Arrangement a) => $"{Supplier(a)} {Lower(CapLabel(a.CapabilityId))} at {SiteName(a.SiteId)}";

    /// <summary>An arrangement by id, or a pending award's, or "that arrangement" for an id the shop no longer has.</summary>
    internal string ArrName(string? id)
    {
        if (id is null) return "that arrangement";
        if (_arrangements.FirstOrDefault(a => a.Id == id) is { } a) return ArrName(a);
        if (_work.FirstOrDefault(w => w.Payload is AwardPayload p && p.Action.Id == id)?.Payload is AwardPayload ap)
            return $"{VendorData(ap.Action.Vendor)?.Name ?? "the vendor"} {Lower(CapLabel(ap.CapabilityId))} at {SiteName(ap.SiteId)}";
        if (_commitments.FirstOrDefault(c => c.Id == id) is { } c) return CommitmentName(c);
        return "that arrangement";
    }

    private string Supplier(Arrangement a)
    {
        if (VendorData(a.VendorId) is { } v) return v.Name;
        return a.Instrument switch
        {
            "acsa" => PartnerName,
            "hostNation" => Scenario.Actors.HostNation?.Name ?? "the host nation",
            "coordination" => "the other unit's",
            _ => _external.Any(x => x.Id == a.Id) ? "the external contract's" : "the",
        };
    }

    /// <summary>"the O&amp;M line" when it is the only one of its type; otherwise with its dates.</summary>
    internal string FundingName(string? id)
    {
        if (id is null || Scenario.FundingLines.FirstOrDefault(f => f.Id == id) is not { } f) return "the funding line";
        var type = FundType(f.Type);
        if (Scenario.FundingLines.Count(x => x.Type == f.Type) == 1) return $"the {type} line";
        return f.AvailableDay <= 1 ? $"the {type} line good through day {f.ExpiresDay}" : $"the {type} line from day {f.AvailableDay}";
    }

    /// <summary>"the request for power at Spoke B".</summary>
    internal string CardName(string? id)
        => _cards.FirstOrDefault(c => c.Id == id) is { } c ? $"the request for {Lower(CardTitle(c))}" : "that request";

    /// <summary>"Meridian Site Services's BPA" by kind and vendor.</summary>
    internal string VehicleName(string? id)
    {
        if (id is null || _vehicles.FirstOrDefault(v => v.Id == id) is not { } v) return "that contract vehicle";
        var kind = v.Kind switch { "bpa" => "BPA", "idiq" => "IDIQ", _ => "contract vehicle" };
        return $"the {VendorData(v.Vendor)?.Name ?? "vendor"} {kind}";
    }

    internal string IssueName(PerformanceIssueState i)
        => $"the performance issue with {VendorData(i.VendorId)?.Name ?? "the supplier"} on {Lower(CapLabel(i.CapabilityId))} at {SiteName(i.SiteId)}";

    internal string IssueName(string? id) => _issues.FirstOrDefault(i => i.Id == id) is { } i ? IssueName(i) : "that performance issue";

    internal string CommitmentName(CommitmentState c)
        => c.Kind == CommitmentKind.Handshake
            ? $"the handshake with {VendorData(c.VendorId)?.Name ?? "the vendor"} for {Lower(CapLabel(c.CapabilityId))} at {SiteName(c.SiteId)}"
            : $"the extra work {VendorData(c.VendorId)?.Name ?? "the vendor"} did on {Lower(CapLabel(c.CapabilityId))} at {SiteName(c.SiteId)}";

    internal string CommitmentName(string? id) => _commitments.FirstOrDefault(c => c.Id == id) is { } c ? CommitmentName(c) : "that commitment";

    internal string TransitionName(TransitionState t)
        => $"the hand-off of {Lower(CapLabel(t.CapabilityId))} at {SiteName(t.SiteId)}";

    internal string ExternalName(ExternalSupportState x)
        => $"the external support request for {Lower(CapLabel(x.CapabilityId))} at {SiteName(x.SiteId)}";

    internal string ExternalName(string? id) => _external.FirstOrDefault(x => x.Id == id) is { } x ? ExternalName(x) : "that external support request";

    /// <summary>An instrument as the rules label it ("BPA call"), lower case after the first word's capital is kept.</summary>
    internal string InstrumentLabel(string id) => Rules.Instruments.TryGetValue(id, out var i) && i.Label.Length > 1 ? (char.IsLower(i.Label[1]) ? Lower(i.Label) : i.Label) : id switch
    {
        "hostNation" => "host nation support",
        "unauthorizedCommitment" => "a handshake",
        _ => id,
    };

    // Event vendor.declinesCalls (M2R-08): a BPA vendor turns down every call that reaches award through this day.
    private readonly Dictionary<string, int> _declinesCallsThrough = new(StringComparer.Ordinal);
    internal void DeclineCalls(string vendorId, int day, int days) => _declinesCallsThrough[vendorId] = day + Math.Max(1, days) - 1;
    private bool DeclinesCalls(string? vendorId, int day) => vendorId is not null && _declinesCallsThrough.TryGetValue(vendorId, out var through) && day <= through;

    /// <summary>The id a work item is for, from its payload (WorkItem.SubjectId).</summary>
    private static string? WorkSubject(object? payload) => payload switch
    {
        AwardPayload p => p.Action.Id,
        AcsaPayload p => p.Order.Id,
        HostNationPayload p => p.Request.Id,
        EstablishBpaAction e => e.Id,
        EstablishIdiqAction e => e.Id,
        DefinitizeAction d => d.Arrangement,
        VisitPayload v => v.Target,
        string s => s,
        _ => null,
    };

    /// <summary>A count with its noun, singular for one ("1 day", "3 shop days"): no "day(s)" in player text (M2R-27).</summary>
    private static readonly System.Text.RegularExpressions.Regex SentenceStart =
        new(@"(^|[.!?]\s+)([a-z])", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>Capitalizes each sentence's first letter, so a name that starts lower case ("the external contract's power")
    /// does not start a sentence that way (M2R-27).</summary>
    internal static string SentenceCase(string text) => SentenceStart.Replace(text, m => m.Groups[1].Value + char.ToUpperInvariant(m.Groups[2].Value[0]));

    /// <summary>A preview as the player reads it: every hard stop and soft gap starts with a capital, so a sentence that opens
    /// on a name written to read mid-sentence ("the partner force", "the O&amp;M line from day 76") reads as a sentence (M2R-40).</summary>
    internal static ActionPreview Said(ActionPreview p) =>
        p with { HardStops = [.. p.HardStops.Select(SentenceCase)], SoftGaps = [.. p.SoftGaps.Select(SentenceCase)] };

    internal static TransitionPreview Said(TransitionPreview p) =>
        p with { HardStops = [.. p.HardStops.Select(SentenceCase)], SoftGaps = [.. p.SoftGaps.Select(SentenceCase)] };

    /// <summary>The award editor's reasons, each starting with a capital (M2R-40).</summary>
    internal static AwardChoicesView? Said(AwardChoicesView? c)
    {
        if (c is null) return null;
        static IReadOnlyList<ChoiceOption> Cased(IReadOnlyList<ChoiceOption> xs) =>
            [.. xs.Select(o => o.Reason is { } r ? o with { Reason = SentenceCase(r) } : o)];
        return c with { Instruments = Cased(c.Instruments), Vendors = Cased(c.Vendors), Vehicles = Cased(c.Vehicles), Funding = Cased(c.Funding) };
    }

    internal static string Plural(long n, string noun) => n == 1 ? $"1 {noun}" : $"{n} {noun}s";
    internal static string Plural(long? n, string noun) => n is { } v ? Plural(v, noun) : $"{noun}s";

    internal static string Upper(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    /// <summary>Any subject id a record or debrief line refers to, as a name.</summary>
    internal string NameOf(string? id)
    {
        if (id is null) return "that";
        if (_arrangements.Any(a => a.Id == id)) return ArrName(id);
        if (_cards.Any(c => c.Id == id)) return CardName(id);
        if (_issues.Any(i => i.Id == id)) return IssueName(id);
        if (_commitments.Any(c => c.Id == id)) return CommitmentName(id);
        if (_external.Any(x => x.Id == id)) return ExternalName(id);
        if (_transitions.FirstOrDefault(t => t.Id == id) is { } t) return TransitionName(t);
        if (Scenario.FundingLines.Any(f => f.Id == id)) return FundingName(id);
        if (_vehicles.Any(v => v.Id == id)) return VehicleName(id);
        if (Scenario.Sites.FirstOrDefault(s => s.Id == id) is { } s) return s.Name;
        if (Scenario.Vendors.FirstOrDefault(v => v.Id == id) is { } v) return v.Name;
        if (Scenario.Islands.FirstOrDefault(i => i.Id == id) is { } i) return i.Name;
        if (Scenario.Events.FirstOrDefault(e => e.Id == id) is { } e) return e.Label;
        return ArrName(id);
    }
}
