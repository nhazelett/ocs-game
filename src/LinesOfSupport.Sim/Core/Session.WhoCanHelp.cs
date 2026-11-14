using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Core;

// Who can help with a request (DESK-42, docs/design/desk-redesign.md D22). Nick on his 13 Nov replay did not know "what
// unlocks by talking to specific people". The request page names the people worth asking and, for each, what talking to
// them can do, the hours it takes and where it stands. Asking someone informs the request (they may know a part it left
// out); meeting a vendor or going to the J4 opens something you can then do. Discovery stays discovery: the page names who
// to ask and what kind of help they are, never the answer before it is given. Read only.
public sealed partial class Session
{
    internal IReadOnlyList<HelperView> BuildWhoCanHelp(CardState card)
    {
        var list = new List<HelperView>();
        var cap = Cap(card.CapabilityId);
        var what = Lower(cap.Label);
        var site = SiteName(card.SiteId);
        var askHours = PersonHours(Fixed.Milli(People.AskAbout.ShopHours), null);

        // People to ask: whoever brought it, then anyone who knows a part it left out (the existing WhoToAsk order).
        foreach (var option in BuildWhoToAsk(card))
        {
            var p = option.Person;
            var brought = p.Id == card.PersonId;
            var does = brought
                ? $"Brought this request, so can say what it is for and what goes with it."
                : $"Works with {what} at {site}{(p.Unit.Length > 0 ? $" for {p.Unit}" : "")} and may know what the request leaves out.";
            var work = VisitWork("ask", AskTarget(new AskAboutAction { Card = card.Id, Person = p.Id }));
            if (option.Asked)
            {
                var asked = _work.LastOrDefault(w => w.CompletedDay is not null && w.Payload is VisitPayload { Kind: "ask" } v && v.Target == $"{card.Id}|{p.Id}");
                var answer = asked is null ? null : _records.LastOrDefault(r => r.Kind == RecordKind.Visit && r.CausedBy.Contains(asked.CauseId))?.Text;
                var learned = Scenario.RequirementCards.First(c => c.Id == card.Id).KnownBy
                    .Where(kv => kv.Value.Person == p.Id && !card.Lines.GetValueOrDefault(kv.Key)).Select(kv => LineLabel(card.CapabilityId, kv.Key)).ToList();
                var next = learned.Count == 0 ? "" : $" You can now add {Lower(string.Join(" and ", learned))} to the request.";
                list.Add(new HelperView("ask", p, p.DisplayName, does, HalfHours(askHours), true, HelperView.Answered, null, null, asked?.CompletedDay, answer,
                    $"{p.DisplayName}: asked on day {asked?.CompletedDay}.{next}"));
            }
            else list.Add(Pending("ask", p, p.DisplayName, does, askHours, true, work,
                start: new AskAboutAction { Card = card.Id, Person = p.Id, Day = Day }));
        }

        // The J4, when the partner force can spare this or something it runs on (fuel for power, R10): they can have the
        // site added to the ACSA order. Opens an action.
        if (Partner is { } partner
            && new[] { card.CapabilityId }.Concat(cap.DependsOn).FirstOrDefault(c => partner.Stock.TryGetValue(c, out var n) && n > 0) is { } stocked)
        {
            var item = Lower(CapLabel(stocked));
            var does = $"The J4 can ask {PartnerName} to add {site} to its {item} order under the ACSA. Not a contract: the partner can cut it back.";
            var work = _work.FirstOrDefault(w => w.CompletedDay is null && w.Payload is VisitPayload { Kind: "j4", Extra: EngageJ4Action j }
                                                 && j.Site == card.SiteId && j.Capability == stocked);
            var hours = PersonHours(Fixed.Milli(People.EngageJ4.ShopHours), null);
            var done = _arrangements.FirstOrDefault(a => a.SiteId == card.SiteId && a.CapabilityId == stocked && a.Instrument == AcsaInstrument && a.CreatedDay > 0);
            list.Add(done is not null && work is null
                ? new HelperView("j4", null, "The J4", does, HalfHours(hours), false, HelperView.Answered, null, null, done.CreatedDay,
                    $"{Upper(PartnerName)} supplies {item} at {site} from day {done.StartDay}.", $"The J4: {PartnerName} added {site} to the {item} order.",
                    CapabilityId: stocked)
                : Pending("j4", null, "The J4", does, hours, false, work, stocked,
                    new EngageJ4Action { Site = card.SiteId, Capability = stocked, Day = Day }));
        }

        // Vendors who sell it and whom the office has not met: a meeting fills in their price and how they are paid.
        foreach (var v in Scenario.Vendors.Where(v => !NotAVendor(v) && v.Offers.Any(o => o.Capability == card.CapabilityId) && !VendorTermsKnown(v)))
        {
            var rep = Scenario.People.FirstOrDefault(x => x.Role == "vendorRep" && x.Vendor == v.Id)?.Id;
            if (BuildPerson(rep) is not { } person) continue;
            var does = $"Sells {what}. Sitting down with them fills in their vendor card: their price, how they are paid and where they deliver.";
            var hours = WithShopVehicle(Fixed.Milli(People.MeetVendor.ShopHours), Day);
            list.Add(Pending("vendor", person, $"{person.DisplayName} of {v.Name}", does, PersonHours(hours, null), false, VisitWork("vendor", v.Id),
                start: new MeetVendorAction { Vendor = v.Id, Day = Day }));
        }
        return list;
    }

    // Q25: the J4 line carries the capability it is about, and a line nobody is on yet carries the action that starts it,
    // so the game binds the line to the Sim's own target instead of working one out.
    private HelperView Pending(string kind, PersonView? person, string name, string does, long hoursMilli, bool informs, WorkItem? work,
        string? capabilityId = null, PlayerAction? start = null)
    {
        if (work is null)
            return new HelperView(kind, person, name, does, HalfHours(hoursMilli), informs, HelperView.NotAsked, null, null, null, null,
                $"{name}: {(kind == "vendor" ? "not met yet" : "not asked yet")}, about {HoursSaid(hoursMilli)}.", capabilityId, start);
        var due = PlayQueue(Member(work.Assignee), 0, Day, work, work.HoursRemainingMilli);
        var who = Member(work.Assignee);
        return new HelperView(kind, person, name, does, HalfHours(hoursMilli), informs, HelperView.Asking, who.Id, MemberName(who), due, null,
            $"{name}: {WhoIs(who.Id)} on it; {(kind == "vendor" ? "the meeting should be done" : "the answer should be in")} on day {due}.",
            capabilityId);
    }
}
