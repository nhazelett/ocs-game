using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private VBoxContainer visitsPanel = null!, visitsForm = null!, visitResults = null!, mentorMessages = null!;
    private string? visitContact, scoQuestion, j4Site;
    private long? j4Quantity;
    private int j4Sequence = 1;
    private readonly Dictionary<string, string?> contactPeople = [];
    internal Button PeopleToggle { get; private set; } = null!;
    internal OptionButton PersonContactInput { get; private set; } = null!;
    internal OptionButton? ScoQuestionInput { get; private set; }
    internal OptionButton? J4SiteInput { get; private set; }
    internal SpinBox? J4QuantityInput { get; private set; }
    internal Dictionary<string, Button> VisitButtons { get; } = [];
    internal Dictionary<string, (PlayerAction Action, ActionPreview Preview)> VisitOptions { get; } = [];
    internal VBoxContainer VisitResults => visitResults;
    internal VBoxContainer MentorMessages => mentorMessages;

    private void BuildVisitsDesk()
    {
        var board = GetNode<TabContainer>("%DeskTabs").GetNode<VBoxContainer>("Board/MorningBoard/Contents");
        mentorMessages = new VBoxContainer { Name = "MentorMessages", Visible = false };
        mentorMessages.AddThemeConstantOverride("separation", 12); board.AddChild(mentorMessages); board.MoveChild(mentorMessages, 0);
        PeopleToggle = new Button { Text = "People to reach", ToggleMode = true, CustomMinimumSize = new Vector2(0, 42) };
        board.AddChild(PeopleToggle); board.MoveChild(PeopleToggle, 2);
        visitsPanel = new VBoxContainer { Name = "ReachPeople", Visible = false };
        visitsPanel.AddThemeConstantOverride("separation", 14); board.AddChild(visitsPanel); board.MoveChild(visitsPanel, 3);
        PeopleToggle.Toggled += open => visitsPanel.Visible = open;
        visitsForm = new VBoxContainer(); visitsForm.AddThemeConstantOverride("separation", 12); visitsPanel.AddChild(visitsForm);
        visitResults = new VBoxContainer { Name = "VisitResults" }; visitResults.AddThemeConstantOverride("separation", 14); visitsPanel.AddChild(visitResults);
    }

    private void ResetVisits()
    {
        visitContact = scoQuestion = j4Site = null; j4Quantity = null; j4Sequence = 1;
        PeopleToggle.ButtonPressed = false;
    }

    private void RefreshVisits(SessionView? view, MorningBoardView? board)
    {
        DeskUi.Clear(visitsForm); DeskUi.Clear(visitResults); DeskUi.Clear(mentorMessages);
        VisitButtons.Clear(); VisitOptions.Clear(); contactPeople.Clear();
        ScoQuestionInput = J4SiteInput = null; J4QuantityInput = null;
        mentorMessages.Visible = false; PeopleToggle.Disabled = view is null;
        if (view is null) return;
        // Only tutorial-authored messages from the outgoing CCO are guidance. The Sim supplies their exact words and day.
        foreach (var item in (board?.Items ?? []).Where(i => view.Scenario.Tutorial && i.Person?.Role == "outgoingCco"))
        {
            var card = new PanelContainer();
            using var cardStyle = new StyleBoxFlat { BgColor = TabletopTheme.Paper, ContentMarginLeft = 16,
                ContentMarginRight = 16, ContentMarginTop = 14, ContentMarginBottom = 14 };
            card.AddThemeStyleboxOverride("panel", cardStyle);
            mentorMessages.AddChild(card);
            var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 14); card.AddChild(row);
            var portrait = new PersonPortrait { SizeFlagsVertical = SizeFlags.ShrinkBegin };
            portrait.SetPerson(item.Person);
            row.AddChild(portrait);
            var words = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddChild(words);
            words.AddChild(DeskUi.Label(item.Person!.WithUnit));
            var message = DeskUi.Label(item.Text); message.SetMeta("mentor_record", item.RecordId); words.AddChild(message);
            mentorMessages.Visible = true;
        }
        var contacts = new List<Pick>();
        void Contact(string key, string label, PersonView? person)
        {
            contacts.Add(new Pick(key, label + (person is null ? "" : $" · {person.DisplayName}"))); contactPeople[key] = person?.Id;
        }
        Contact("finance", "Finance", view.FinanceContact);
        Contact("meeting", "Staff meeting", view.Commander);
        Contact("sco", "SCO staff", view.ScoContact);
        if (view.Scenario.Actors.AcsaPartner is not null) Contact("j4", "J4 / ACSA manager", null);
        foreach (var rep in view.VendorReps) Contact("vendor:" + rep.Id, DeskUi.ListLabel(rep.VendorName ?? "Vendor"), rep);
        PersonContactInput = AddPick(visitsForm, "Who would you like to reach?", contacts, visitContact, id => { visitContact = id; RefreshVisits(session.View, session.View?.MorningBoard()); });
        visitContact = Picked(PersonContactInput);
        var person = contactPeople.TryGetValue(visitContact ?? "", out var personId) && personId is not null ? view.Person(personId) : null;
        if (person is not null)
        {
            var face=new PersonPortrait{SizeFlagsHorizontal=SizeFlags.ShrinkBegin};face.SetPerson(person);visitsForm.AddChild(face);
            visitsForm.AddChild(DeskUi.Label(person.WithUnit));
            if (person.Line("greeting") is { } greeting) visitsForm.AddChild(DeskUi.Label(greeting));
        }
        switch (visitContact)
        {
            case "finance":
                if (view.FinanceFoundDay is { } found) visitsForm.AddChild(DeskUi.Label($"You found the finance office on day {found}."));
                AddVisit("finance", "Find finance", new FindFinanceAction { Day = view.Day }, view); break;
            case "meeting":
                var meeting = view.StaffMeeting;
                visitsForm.AddChild(DeskUi.Label(meeting.Joining ? "You are getting yourself into the staff meeting." : meeting.Attending
                    ? $"You are attending the staff meeting. The next is on day {meeting.NextDay}." : "You are not attending the staff meeting."));
                visitsForm.AddChild(DeskUi.Label($"Once joined, each meeting takes {view.Rules.GoingToPeople.StaffMeeting.WeeklyShopHours} shop hours."));
                AddVisit("meeting", "Join the staff meeting", new JoinStaffMeetingAction { Day = view.Day }, view);
                AddVisit("leave-meeting", "Stop attending", new JoinStaffMeetingAction { Day = view.Day, Attend = false }, view); break;
            case "sco":
                var questions = view.ScoQuestions.Select(q => new Pick(q.Id, q.Label + (q.Pending ? " · answer pending" : ""))).ToArray();
                ScoQuestionInput = AddPick(visitsForm, "What would you like to ask?", questions, scoQuestion, id => { scoQuestion = id; RefreshVisits(session.View, session.View?.MorningBoard()); });
                scoQuestion = Picked(ScoQuestionInput);
                if (scoQuestion is not null) AddVisit("sco", "Send the question", new AskScoAction { Day = view.Day, Question = scoQuestion }, view);
                break;
            case "j4": RenderJ4Visit(view); break;
            default:
                if (person is not null && view.Scenario.People.First(p => p.Id == person.Id).Vendor is { } vendor)
                {
                    if (view.VendorMetDay(vendor) is { } met) visitsForm.AddChild(DeskUi.Label($"You met on day {met}."));
                    AddVisit("vendor", "Meet " + person.DisplayName, new MeetVendorAction { Day = view.Day, Vendor = vendor }, view);
                }
                break;
        }
        foreach (var record in view.Records.Where(view.Known).Where(r => r.Kind == RecordKind.Visit).TakeLast(6))
        {
            var label = DeskUi.Label($"Day {record.Day}\n{record.Text}"); label.SetMeta("visit_record", record.Id);
            label.FocusMode = FocusModeEnum.All; visitResults.AddChild(label);
        }
    }

    private void AddVisit(string key, string text, PlayerAction action, SessionView view, VBoxContainer? parent = null)
    {
        var host = parent ?? visitsForm;
        var preview = view.PreviewVisit(action); VisitOptions[key] = (action, preview);
        var button = DeskUi.Button(text, () => SendAction(action)); button.Disabled = view.Finished || !preview.Accepted;
        button.SetMeta("visit", key); host.AddChild(button); VisitButtons[key] = button;
        static string Range(int min, int max) => min == max ? min.ToString() : $"{min}–{max}";
        host.AddChild(DeskUi.Label($"This takes {Range(preview.ShopHoursMin, preview.ShopHoursMax)} shop hours. The earliest result is day {Range(preview.EarliestStartMin, preview.EarliestStartMax)}{DeskUi.QueueClause(preview.QueueAheadHours)}."));
        if (preview.HardStops.Count > 0)
        {
            var stops = DeskUi.Label(string.Join("\n\n", preview.HardStops)); stops.AddThemeColorOverride("font_color", TabletopTheme.Alert); host.AddChild(stops);
        }
        if (preview.SoftGaps.Count > 0) host.AddChild(DeskUi.Label(string.Join("\n\n", preview.SoftGaps)));
    }

    private void OpenJ4FuelVisit(string site)
    {
        if (session.View is not { Finished: false } view) return;
        visitContact = "j4"; j4Site = site; j4Quantity = null;
        GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
        PeopleToggle.ButtonPressed = true; RefreshVisits(view, view.MorningBoard());
        var quantity = J4QuantityInput;
        Callable.From(() =>
        {
            if (quantity is null || !GodotObject.IsInstanceValid(quantity) || !quantity.IsInsideTree()) return;
            ((ScrollContainer)visitsPanel.GetParent().GetParent()).EnsureControlVisible(quantity);
            quantity.GetLineEdit().GrabFocus();
        }).CallDeferred();
    }

    private void RenderJ4Visit(SessionView view)
    {
        if (j4Site is null && SelectedCard is { } card) j4Site = view.RequirementEditor(card)?.SiteId;
        var sites = view.Scenario.Sites.Select(s => new Pick(s.Id, s.Name)).ToArray();
        J4SiteInput = AddPick(visitsForm, "Which site needs fuel?", sites, j4Site, id => { j4Site = id; j4Quantity = null; RefreshVisits(session.View, session.View?.MorningBoard()); });
        j4Site = Picked(J4SiteInput); if (j4Site is null) return;
        var editor = view.ActionEditor(j4Site, "fuel");
        if (editor is null) { visitsForm.AddChild(DeskUi.Label("Fuel support is not available here.")); return; }
        j4Quantity ??= Math.Max(1, editor.PeakDemandUnits);
        visitsForm.AddChild(DeskUi.Label($"Daily quantity ({editor.Unit})"));
        J4QuantityInput = new SpinBox { MinValue = 1, MaxValue = 9007199254740991, Step = 1, Value = j4Quantity.Value };
        visitsForm.AddChild(J4QuantityInput);
        var preview = new VBoxContainer(); preview.AddThemeConstantOverride("separation", 12); visitsForm.AddChild(preview);
        void Update()
        {
            DeskUi.Clear(preview);
            while (view.Arrangements.Any(a => a.Id == $"player-j4-{j4Sequence}") || view.Work.Any(w => w.SubjectId == $"player-j4-{j4Sequence}")) j4Sequence++;
            AddVisit("j4", "Ask the J4 to add fuel support", new EngageJ4Action { Day = view.Day, Id = $"player-j4-{j4Sequence}", Site = j4Site, Quantity = j4Quantity.Value }, view, preview);
        }
        J4QuantityInput.ValueChanged += value => { j4Quantity = checked((long)value); Update(); };
        Update();
    }

    private void FocusPerson(BoardItem item)
    {
        if (session.View is not { } view) return;
        GetNode<TabContainer>("%DeskTabs").CurrentTab = 0;
        var contact = contactPeople.FirstOrDefault(c => c.Value == item.LinkId).Key;
        if (contact is not null) visitContact = contact;
        PeopleToggle.ButtonPressed = true; RefreshVisits(view, view.MorningBoard());
        var label = visitResults.GetChildren().OfType<Label>().FirstOrDefault(l => l.GetMeta("visit_record").AsString() == item.RecordId);
        if (label is null) visitResults.AddChild(label = DeskUi.Label(item.Text));
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(label) || !label.IsInsideTree()) return;
            ((ScrollContainer)visitsPanel.GetParent().GetParent()).EnsureControlVisible(label);
        }).CallDeferred();
    }

    private static bool HasPeopleNews(SessionView view, MorningBoardView board)
        => board.Items.Any(i => view.Scenario.Tutorial && i.Person?.Role == "outgoingCco"
            || view.Records.Any(r => r.Id == i.RecordId && r.Kind == RecordKind.Visit));
}
