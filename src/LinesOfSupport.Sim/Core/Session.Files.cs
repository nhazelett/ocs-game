using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Hidden inherited problems and the file review (M3-17, docs/design/M3-07-representative-scenario.md section 5).
// An inherited row with visibleProblem false keeps its problem out of the views: a UCA's definitization clock and
// overdue state, an IDIQ's ceiling and ordered value, the vendor's real delivery record (reportedReliability shown
// instead). The Sim runs on the true state throughout; only what the player is shown is gated. A file review finds
// the problem; a higher-HQ inquiry about a past-due UCA finds that one the hard way.
/// <summary>How a hidden inherited problem came to light: the file review, higher HQ's inquiry, or the vendor (M2R-04).</summary>
internal enum RevealSource { Review, Inquiry, Vendor }

public sealed partial class Session
{
    // Inherited arrangements whose problem the handover did not mention.
    private readonly HashSet<string> _hiddenProblems = new(StringComparer.Ordinal);
    // The handover's delivery record for a hidden row, permille.
    private readonly Dictionary<string, int> _reportedReliability = new(StringComparer.Ordinal);
    // Day each inherited file was reviewed.
    private readonly Dictionary<string, int> _reviewedDay = new(StringComparer.Ordinal);
    // Day each hidden problem came to light, by review or by an inquiry, with the record that showed it.
    private readonly Dictionary<string, (int Day, string RecordId, RevealSource Source)> _revealed = new(StringComparer.Ordinal);
    /// <summary>The delivery record (permille) each file review found, so the debrief quotes what the review showed
    /// even after a later drop or cure moved the vendor's reliability (M3-22).</summary>
    private readonly Dictionary<string, int> _reviewedReliability = new(StringComparer.Ordinal);

    private const string HostNationInstrumentId = "hostNation";

    private void RegisterHidden(InheritedArrangementData a)
    {
        if (a.VisibleProblem) return;
        _hiddenProblems.Add(a.Id);
        if (a.ReportedReliability is { } rr) _reportedReliability[a.Id] = Fixed.Permille(rr);
    }

    /// <summary>True while an inherited arrangement's problem has not come to light (M3-17).</summary>
    internal int ReliabilityShownByReview(Arrangement a) => _reviewedReliability.GetValueOrDefault(a.Id, a.ReliabilityPermille);

    internal bool ProblemHidden(Arrangement a) => _hiddenProblems.Contains(a.Id) && !_revealed.ContainsKey(a.Id);

    /// <summary>True when the hidden problem had not come to light by the end of the given day.</summary>
    private bool HiddenOn(string arrangementId, int day)
        => _hiddenProblems.Contains(arrangementId) && !(_revealed.TryGetValue(arrangementId, out var r) && r.Day <= day);

    /// <summary>The delivery record the office believes: the handover's report while a hidden one is not found.</summary>
    internal int ShownReliability(Arrangement a)
        => ProblemHidden(a) && _reportedReliability.TryGetValue(a.Id, out var reported) ? reported : a.ReliabilityPermille;

    /// <summary>True while an inherited order on this IDIQ hides the vehicle's ceiling and ordered value and none of the
    /// hidden rows on it has been found.</summary>
    internal bool IdiqTermsHidden(string? vehicleId)
    {
        if (vehicleId is null) return false;
        var rows = _arrangements.Where(a => a.VehicleId == vehicleId && a.CreatedDay == 0 && a.Instrument == "idiqOrder" && _hiddenProblems.Contains(a.Id)).ToList();
        return rows.Count > 0 && rows.All(ProblemHidden);
    }

    /// <summary>Whether the office knows about this record yet. A vendor failure waits for a COR or unit report (M3-04);
    /// a hidden inherited UCA's overdue notice waits for the file review or the inquiry (M3-17).</summary>
    internal bool RecordKnown(CauseRecord r)
    {
        if (r.Kind == RecordKind.VendorFailure)
            return _arrangements.FirstOrDefault(a => a.Id == r.Subject) is not { } fa || Observed(fa);
        if (r.Kind is RecordKind.UcaOverdue or RecordKind.UcaProposal)
            return _arrangements.FirstOrDefault(a => a.Id == r.Subject) is not { } ua || !ProblemHidden(ua);
        return true;
    }

    /// <summary>Inherited contract files the shop can review: everything inherited except host nation support.</summary>
    private IEnumerable<Arrangement> ReviewableFiles()
        // Host nation support and an ACSA line are someone else's paper (the agreement, the J4's order), not this office's
        // contract files.
        => _arrangements.Where(a => a.CreatedDay == 0 && a.Instrument != HostNationInstrumentId && a.Instrument != AcsaInstrument
                                    && Scenario.InheritedArrangements.Any(i => i.Id == a.Id));

    private bool ReviewInWork(string id)
        => _work.Any(w => w.Kind == WorkKind.FileReview && w.CompletedDay is null && w.Payload is List<string> ids && ids.Contains(id));

    /// <summary>The files a review would cover and its hard stops. Reads state only.</summary>
    internal (List<string> Files, List<string> Stops) CheckReviewFiles(ReviewFilesAction r)
    {
        var reviewable = ReviewableFiles().Select(a => a.Id).ToList();
        if (reviewable.Count == 0) return ([], ["there are no inherited contract files to review"]);
        var stops = new List<string>();
        List<string> files;
        if (r.Arrangements is { Count: > 0 } named)
        {
            files = [];
            foreach (var id in named.Distinct(StringComparer.Ordinal))
            {
                if (!reviewable.Contains(id))
                    stops.Add(_arrangements.Any(a => a.Id == id) ? $"{id} is not an inherited contract file" : $"{id} does not exist");
                else if (_reviewedDay.ContainsKey(id)) stops.Add($"the file on {id} was already reviewed on day {_reviewedDay[id]}");
                else if (ReviewInWork(id)) stops.Add($"the file on {id} is already in review");
                else files.Add(id);
            }
        }
        else
        {
            files = reviewable.Where(id => !_reviewedDay.ContainsKey(id) && !ReviewInWork(id)).ToList();
            if (files.Count == 0) stops.Add("every inherited contract file is reviewed or in review");
        }
        return (files, stops);
    }

    private ActionResult ApplyReviewFiles(ReviewFilesAction r)
    {
        var (files, stops) = CheckReviewFiles(r);
        if (stops.Count > 0) return ActionResult.Stop([.. stops]);
        var rec = AddRecord(Day, RecordKind.Action, "file review", $"Ordered a review of the inherited contract files: {string.Join(", ", files.Select(ArrName))}.", []);
        var hours = Fixed.Milli(Rules.FileReview.HoursPerArrangement) * files.Count;
        AddWork(WorkKind.FileReview, $"Review {files.Count} inherited contract file{(files.Count == 1 ? "" : "s")}", hours, Day, files, rec);
        return ActionResult.Ok("file review in work");
    }

    private void CompleteFileReview(List<string> files, int day, string causeId)
    {
        foreach (var id in files)
        {
            var a = _arrangements.First(x => x.Id == id);
            _reviewedDay[id] = day;
            _reviewedReliability[id] = a.ReliabilityPermille;
            var rec = AddRecord(day, RecordKind.Knowledge, id, $"File review: {Findings(a, day)}", [causeId]);
            if (_hiddenProblems.Contains(id)) _revealed.TryAdd(id, (day, rec, RevealSource.Review));
        }
    }

    /// <summary>What an inherited file shows: the true state as of the day of the review.</summary>
    private string Findings(Arrangement a, int day)
    {
        var parts = new List<string>
        {
            $"{CapLabel(a.CapabilityId).ToLowerInvariant()} at {SiteName(a.SiteId)} from {VendorData(a.VendorId)?.Name ?? "the vendor"} ({InstrumentLabel(a.Instrument)}), days {a.StartDay} to {a.EndDay}" +
            (a.FundingLineId is null ? "" : $", funded from {NameOf(a.FundingLineId)}"),
        };
        if (a.Uca is { } u)
        {
            var due = u.IssuedDay + Rules.Uca.DefinitizeWithinDays;
            var pct = Fixed.Ratio(a.ObligatedCents + u.PriorObligatedCents, u.NteCents) / 10;
            parts.Add(u.Definitized ? "UCA, definitized"
                : $"UCA issued day {u.IssuedDay}, NTE {Money(u.NteCents)}, {pct}% obligated; " +
                  (u.Overdue ? "past due for definitization (due by day " + due + " or at " + $"{Rules.Uca.DefinitizeAtObligatedFraction * 100:0}% of NTE obligated)"
                             : $"definitization due by day {due}") +
                  (u.ProposalReceived ? "; qualifying proposal in hand" : "; no qualifying proposal yet") +
                  (u.DefinitizationInWork ? "; definitization in work" : ""));
        }
        if (a.Instrument == "idiqOrder" && Idiq(a.VehicleId) is { } q)
            parts.Add(q.CeilingCents is { } c
                ? $"{Upper(NameOf(q.VehicleId))} ceiling {Money(c)}, {Money(q.OrderedCents)} ordered ({Fixed.Ratio(q.OrderedCents, c) / 10}%), {Money(Math.Max(0, c - q.OrderedCents))} left to order"
                : $"{Upper(NameOf(q.VehicleId))} has no ceiling; {Money(q.OrderedCents)} ordered");
        if (_reportedReliability.TryGetValue(a.Id, out var reported))
            parts.Add($"delivery record {a.ReliabilityPermille / 10}% on time, not the {reported / 10}% reported at handover");
        else
            parts.Add($"delivery record {a.ReliabilityPermille / 10}% on time");
        if (!_hiddenProblems.Contains(a.Id)) parts.Add("nothing the handover did not mention");
        return string.Join("; ", parts) + ".";
    }

    /// <summary>A higher-HQ inquiry about a past-due UCA brings a hidden one to light (M3-17).</summary>
    private void RevealByInquiry(Arrangement a, int day, string recordId)
    {
        if (_hiddenProblems.Contains(a.Id)) _revealed.TryAdd(a.Id, (day, recordId, RevealSource.Inquiry));
    }

    /// <summary>Debrief wording for a factor on an inherited arrangement with a hidden problem, or null when it had none.
    /// Unfound on the day: discoverable (the file review would have shown it). Found: avoidable from then on.</summary>
    private (GapTag Tag, string Note, string? RecordId)? HiddenNote(string arrangementId, int day, string what)
    {
        if (!_hiddenProblems.Contains(arrangementId)) return null;
        if (HiddenOn(arrangementId, day))
            return (GapTag.Discoverable, $" The handover did not mention {what}; a review of the inherited files would have shown it.", null);
        var r = _revealed[arrangementId];
        return (GapTag.Avoidable, r.Source switch
        {
            RevealSource.Review => $" The file review on day {r.Day} showed {what}.",
            RevealSource.Vendor => $" The vendor raised it when you met them on day {r.Day}, which showed {what}.",
            _ => $" Higher HQ's inquiry on day {r.Day} brought {what} to light.",
        }, r.RecordId);
    }

    internal ActionPreview BuildReviewFilesPreview(ReviewFilesAction r)
    {
        var (files, stops) = CheckReviewFiles(r);
        var queue = CeilHours(_work.Where(w => w.CompletedDay is null).Sum(w => w.HoursRemainingMilli));
        var hours = CeilHours(Fixed.Milli(Rules.FileReview.HoursPerArrangement) * files.Count);
        // Findings are ready the day the hours are worked, ignoring the queue ahead.
        var ready = _hoursPerDayMilli <= 0 ? Day : Day + Math.Max(0, (int)((hours * 1000L + _hoursPerDayMilli - 1) / _hoursPerDayMilli) - 1);
        return new ActionPreview(stops.Count == 0, stops, [], 0, 0, 0, hours, hours, ready, ready, queue);
    }

    internal IReadOnlyList<InheritedFileView> BuildInheritedFiles()
        => ReviewableFiles().Select(a => new InheritedFileView(a.Id, a.SiteId, a.CapabilityId, a.Instrument,
                                                               _reviewedDay.TryGetValue(a.Id, out var d) ? d : null, ReviewInWork(a.Id),
                                                               _reviewedDay.ContainsKey(a.Id) ? _records.LastOrDefault(x => x.Kind == RecordKind.Knowledge && x.Subject == a.Id)?.Id : null))
                            .ToList();
}
