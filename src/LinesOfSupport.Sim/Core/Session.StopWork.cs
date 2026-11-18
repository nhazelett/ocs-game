using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// DESK-104 (FOR-NICK #23 (a), approved by Nick 18 Nov 2026). In the 18 Nov Claude play the player called a forklift BPA
// without drivers, learned that night that drivers were needed, wrote the right purchase order the next morning, and then
// looked everywhere for a way to stop the first call. A buy still being written can now be stopped before it is signed:
// the hours already spent stay spent, nothing is obligated (nothing is until the award is done), the work leaves the
// queue, and the request stands as it did before it, because every standing, alert and summary reads only open work.
// Left out: undo of anything signed (that is ending a contract), a cost or a delay for stopping, and stopping any other
// kind of work.
public sealed partial class Session
{
    /// <summary>Why this work cannot be stopped, or null when it can.</summary>
    private string? CheckStopWork(StopWorkAction s, out WorkItem? work)
    {
        work = _work.FirstOrDefault(x => x.Id == s.Work);
        if (work is null) return "that work is not in the queue";
        if (work.StoppedDay is not null) return "that work was already stopped";
        if (work.CompletedDay is not null) return "that work is already done";
        if (work.Kind != WorkKind.Award || work.Payload is not AwardPayload) return "only a buy still being written can be stopped";
        return null;
    }

    private ActionResult ApplyStopWork(StopWorkAction s)
    {
        if (CheckStopWork(s, out var w) is { } stop) return ActionResult.Stop(stop);
        w!.StoppedDay = Day;
        w.CompletedDay = Day;
        AddRecord(Day, RecordKind.Action, w.SubjectId ?? w.Id, $"{Upper(StoppedWhat(w))} before it was signed. {SpentWords(w, stopped: true)}", [w.CauseId]);
        return ActionResult.Ok("stopped");
    }

    /// <summary>Stopping takes no hours and spends nothing: the preview is accepted or carries the reason it cannot be.</summary>
    private ActionPreview BuildStopWorkPreview(StopWorkAction s)
    {
        var stops = CheckStopWork(s, out _) is { } stop ? new[] { stop } : [];
        return new ActionPreview(stops.Length == 0, stops, [], 0, 0, 0, 0, 0, Day, Day, QueueHours(s.Assignee));
    }

    /// <summary>The line under "Stop this work" for a work item, or null when it cannot be stopped.</summary>
    internal string? StopSentence(string workId)
        => _work.FirstOrDefault(x => x.Id == workId) is { } w ? StopSentence(w) : null;

    /// <summary>"Stops the BPA call with Harbor Equipment Co. The 1.5 hours MSgt Joan Avery spent stay spent; nothing is owed."</summary>
    private string? StopSentence(WorkItem w)
    {
        if (CheckStopWork(new StopWorkAction { Work = w.Id }, out _) is not null) return null;
        var p = (AwardPayload)w.Payload!;
        return $"Stops the {InstrumentWords(p.Action.Instrument)} with {VendorData(p.Action.Vendor)?.Name ?? "the vendor"}. {SpentWords(w)}";
    }

    /// <summary>"stopped the BPA call with Harbor Equipment Co for material handling equipment at Hub", for the record and
    /// the decisions list.</summary>
    private string StoppedWhat(WorkItem w)
    {
        var p = (AwardPayload)w.Payload!;
        return $"stopped the {InstrumentWords(p.Action.Instrument)} with {VendorData(p.Action.Vendor)?.Name ?? "the vendor"} for {Lower(CapLabel(p.CapabilityId))} at {SiteName(p.SiteId)}";
    }

    /// <summary>What stopping leaves behind: the hours already worked, in the holder's own hours, and that nothing is owed.</summary>
    private string SpentWords(WorkItem w, bool stopped = false)
    {
        var spent = Math.Max(0, w.HoursTotalMilli - w.HoursRemainingMilli);
        var who = Member(w.Assignee);
        var name = who.Role == "you" ? "you" : MemberName(who);
        return spent <= 0
            ? stopped ? "No hours went into it, and nothing is owed." : "No hours have gone into it yet, and nothing is owed."
            : $"The {HoursSaid(spent)} {name} spent {(HalfHours(spent) == 1 ? "stays" : "stay")} spent; nothing is owed.";
    }
}
