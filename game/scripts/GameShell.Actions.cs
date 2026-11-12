using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private sealed record AwardTarget(string Id, string Label, string Site, string Capability, string? Card);
    private sealed record Pick(string Id, string Text, string? Block = null);
    private VBoxContainer awardPanel = null!;
    private VBoxContainer previewPanel = null!, awardFooter = null!;
    private string? awardTarget, awardInstrument, awardVendor, awardVehicle, awardFunding, awardCor;
    private long? quantity;
    private int? start, duration;
    private int awardSequence = 1;
    private int awardFormVersion;
    private OptionButton instrumentInput = null!, vendorInput = null!, vehicleInput = null!, fundingInput = null!;
    private SpinBox quantityInput = null!, startInput = null!, durationInput = null!;
    private Label previewCost = null!, previewTiming = null!, previewStops = null!, previewGaps = null!;
    private Button awardButton = null!;
    internal AwardAction? PendingAward { get; private set; }
    internal ActionPreview? AwardPreview { get; private set; }
    internal ActionEditorView? CurrentActionEditor { get; private set; }
    internal OptionButton AwardRequestInput { get; private set; } = null!;
    internal OptionButton? InitiativeTargetInput { get; private set; }
    internal Label? InitiativeDetails { get; private set; }
    internal OptionButton InstrumentInput => instrumentInput;
    internal OptionButton FundingInput => fundingInput;
    internal OptionButton? CorInput { get; private set; }
    internal Label? SupplierKnowledge { get; private set; }
    internal OptionButton VendorInput => vendorInput;
    internal OptionButton VehicleInput => vehicleInput;
    internal SpinBox QuantityInput => quantityInput;
    internal SpinBox DurationInput => durationInput;
    internal SpinBox StartInput => startInput;
    internal Button AwardButton => awardButton;
    internal Label AwardTiming => previewTiming;
    internal Label AwardCost => previewCost;
    internal Label AwardBlockedReason { get; private set; } = null!;
    internal ScrollContainer AwardScroll => (ScrollContainer)awardPanel.GetParent().GetParent();

    private void GateAward(SessionView? view)
    {
        string? reason = view is null ? "Start a deployment to arrange support."
            : view.Finished ? DeskUi.FinishedReason
            : CurrentActionEditor is null ? "Choose a request or something to buy on your own initiative."
            : string.IsNullOrEmpty(awardVendor) ? "Choose an available supplier."
            : string.IsNullOrEmpty(awardInstrument) ? "Choose an available arrangement."
            : awardInstrument is "bpaCall" or "idiqOrder" && string.IsNullOrEmpty(awardVehicle) ? "Choose an available vehicle."
            : awardInstrument is not ("sf44" or "gpc") && string.IsNullOrEmpty(awardFunding) ? "Choose an available funding line."
            : AwardPreview?.HardStops.FirstOrDefault();
        reason ??= AwardPreview is null ? "Complete the terms to see the quote." : null;
        DeskUi.Gate(awardButton, AwardBlockedReason, reason);
    }

    private void BuildActionDesk()
    {
        var body = DeskUi.Pane(GetNode<TabContainer>("%DeskTabs").GetNode<Control>("Actions"), "AwardEditor", out awardFooter);
        awardPanel = new VBoxContainer { Name = "Inputs" }; body.AddChild(awardPanel);
        awardPanel.AddThemeConstantOverride("separation", 10);
        previewPanel = new VBoxContainer { Name = "Quote" }; body.AddChild(previewPanel);
        previewPanel.AddThemeConstantOverride("separation", 10);
    }
    private void ResetAwardForm()
    {
        awardTarget = awardInstrument = awardVendor = awardVehicle = awardFunding = awardCor = null;
        awardChoiceNotice = null;
        awardPriorities.Clear(); // DESK-26
        quantity = null; start = duration = null; awardSequence = 1;
    }

    private static List<AwardTarget> AwardTargets(SessionView view)
    {
        var targets = view.Cards.Select(c => view.RequirementEditor(c.Id)).Where(c => c is not null)
            .Select(c => new AwardTarget(c!.CardId, $"{c.Title} · {c.Person?.DisplayName ?? c.SubmittedBy}", c.SiteId, c.CapabilityId, c.CardId)).ToList();
        targets.AddRange(view.OwnInitiative.Select(t => new AwardTarget(t.SiteId + "." + t.CapabilityId,
            t.Label + " · " + view.Scenario.Sites.Single(s => s.Id == t.SiteId).Name, t.SiteId, t.CapabilityId, null)));
        targets.AddRange(view.MorningBoard().Tiles.Select(t => new AwardTarget(t.SiteId + "." + t.CapabilityId,
            $"{view.Scenario.Sites.Single(s => s.Id == t.SiteId).Name} {t.Label.ToLowerInvariant()}", t.SiteId, t.CapabilityId, null)));
        return targets.GroupBy(t => t.Id).Select(g => g.First()).ToList();
    }

    private static OptionButton AddPick(VBoxContainer panel, string label, IReadOnlyList<Pick> options, string? selected, Action<string> changed)
    {
        panel.AddChild(DeskUi.Label(label));
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, FitToLongestItem = false, ClipText = true };
        for (var i = 0; i < options.Count; i++)
        {
            picker.AddItem(options[i].Text + (options[i].Block is null ? "" : $" — {options[i].Block}"));
            picker.SetItemMetadata(i, options[i].Id);
            picker.SetItemDisabled(i, options[i].Block is not null);
            if (options[i].Block is not null) picker.SetItemTooltip(i, options[i].Block);
        }
        var index = options.Select((o, i) => (o, i)).FirstOrDefault(x => x.o.Id == selected).i;
        if (selected is null || !options.Any(o => o.Id == selected)) index = options.Select((o, i) => (o, i)).Where(x => x.o.Block is null).Select(x => x.i).DefaultIfEmpty(0).First();
        if (options.Count > 0) picker.Select(index); else picker.Disabled = true;
        picker.ItemSelected += i => changed(options[(int)i].Id);
        panel.AddChild(picker); return picker;
    }

    private static string? Picked(OptionButton picker) => picker.ItemCount == 0 || picker.Selected < 0 ? null : picker.GetItemMetadata(picker.Selected).AsString();
    private SpinBox AddNumber(string label, double value, double max, Action<double> changed)
    {
        awardPanel.AddChild(DeskUi.Label(label));
        var input = new SpinBox { MinValue = 1, MaxValue = max, Step = 1, Value = value, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        input.ValueChanged += value => changed(value); awardPanel.AddChild(input); return input;
    }

    private void RefreshActionDesk(SessionView? view)
    {
        DeskUi.Clear(awardPanel); DeskUi.Clear(previewPanel); DeskUi.Clear(awardFooter);
        PendingAward = null; AwardPreview = null; CurrentActionEditor = null; CurrentAwardChoices = null; CorInput = null; CorReasonLabels.Clear(); InitiativeTargetInput = null; InitiativeDetails = null;
        PriorityInputs.Clear(); AwardTrade = null; // DESK-26
        var formVersion = ++awardFormVersion;
        var formHash = session.StateHash;
        awardButton = DeskUi.Button("Arrange support", () =>
        {
            if (formVersion != awardFormVersion || formHash != session.StateHash || AtFrontDoor || ReadingTurnover || PlayingDay) return;
            if (PendingAward is not null && AwardPreview?.Accepted == true && session.View?.Finished == false)
            {
                var action = PendingAward;
                awardPriorities.Clear(); // DESK-26: each new written contract gets its own pick.
                SendAction(action);
            }
        });
        AwardBlockedReason = DeskUi.ActionReason(awardFooter, awardButton); GateAward(view);
        if (view is null) { awardPanel.AddChild(DeskUi.Label("Load a scenario to assemble an action.")); return; }
        SupplierKnowledge = null;
        var targets = AwardTargets(view);
        if (targets.Count == 0) { awardPanel.AddChild(DeskUi.Label("No site capabilities are available.")); return; }
        if (!targets.Any(t => t.Id == awardTarget))
            awardTarget = (targets.FirstOrDefault(t => SelectedCard is not null && t.Card == SelectedCard)
                ?? targets.FirstOrDefault(t => t.Card is not null) ?? targets[0]).Id;
        void ChooseTarget(string id)
        {
            if (formVersion != awardFormVersion || formHash != session.StateHash) return;
            awardTarget = id; awardVendor = awardInstrument = awardVehicle = awardFunding = awardCor = null;
            awardChoiceNotice = null; awardPriorities.Clear(); quantity = null; start = duration = null; RefreshActionDesk(session.View);
        }
        var target = targets.Single(t => t.Id == awardTarget);
        var ownTargets = targets.Where(t => t.Card is null).ToArray();
        var requestOptions = targets.Where(t => t.Card is not null).Select(t => new Pick(t.Id, t.Label)).ToList();
        if (ownTargets.Length > 0) requestOptions.Add(new Pick("own-initiative", "Buy on your own initiative"));
        AwardRequestInput = AddPick(awardPanel, "What are you arranging?", requestOptions,
            target.Card ?? "own-initiative", id => ChooseTarget(id == "own-initiative" ? ownTargets[0].Id : id));
        if (target.Card is null)
            InitiativeTargetInput = AddPick(awardPanel, "What would you buy on your own initiative?",
                ownTargets.Select(t => new Pick(t.Id, t.Label)).ToArray(), target.Id, ChooseTarget);
        if (target.Card is null && view.OwnInitiative.FirstOrDefault(t => t.SiteId == target.Site && t.CapabilityId == target.Capability) is { } initiative)
        {
            InitiativeDetails = DeskUi.Label(initiative.Why + "\nWho sells it: " + string.Join(", ", initiative.Vendors.Select(view.NameOf)) + "."
                + (initiative.Have ? "\nThe office has one running today." : ""));
            awardPanel.AddChild(InitiativeDetails);
        }
        var editor = view.ActionEditor(target.Site, target.Capability, target.Card)!; CurrentActionEditor = editor;
        awardPanel.AddChild(DeskUi.Label($"You're looking for {editor.CapabilityLabel.ToLowerInvariant()} at {view.Scenario.Sites.Single(s => s.Id == editor.SiteId).Name}."));
        var choices = ReconcileAwardChoices(view, target);
        vendorInput = AddAwardChoice("Who could provide it?", "Choose a supplier…", choices.Vendors, awardVendor,
            id => { awardVendor = id; awardCor = null; }, out var vendorReason, awardChoiceNotice);
        VendorChoiceReason = vendorReason;
        instrumentInput = AddAwardChoice("How would you arrange it?", "Choose an arrangement…", choices.Instruments, awardInstrument,
            id => { awardInstrument = id; awardCor = null; }, out var instrumentReason);
        InstrumentChoiceReason = instrumentReason;
        if (!string.IsNullOrEmpty(awardVendor) && editor.Vendors.FirstOrDefault(v => v.Id == awardVendor) is { } supplier)
        {
            SupplierKnowledge = DeskUi.Label($"{supplier.Name} is based on {supplier.IslandName}.\nCash: "
                + Payment(supplier.PaymentKnown ? supplier.AcceptsCash : null) + " · Card: " + Payment(supplier.PaymentKnown ? supplier.AcceptsGpc : null)
                + "\nSupply per day: " + (supplier.CapacityKnown ? $"{supplier.CapacityPerDay} {editor.Unit}" : "—")
                + " · Available today: " + (supplier.CapacityKnown ? supplier.CapacityFreeToday.ToString() : "—")
                + "\nNotice: " + (supplier.PaymentKnown ? (supplier.LeadTimeDays == 1 ? "1 day" : $"{supplier.LeadTimeDays} days") : "—"));
            awardPanel.AddChild(SupplierKnowledge);
        }
        var needsVehicle = awardInstrument is "bpaCall" or "idiqOrder";
        vehicleInput = AddAwardChoice("Vehicle", needsVehicle ? "Choose a vehicle…" : "No vehicle needed", choices.Vehicles, awardVehicle,
            id => awardVehicle = id, out var vehicleReason);
        vehicleInput.Disabled = !needsVehicle;
        if (!needsVehicle) { vehicleReason.Text = ""; vehicleReason.Hide(); }
        var immediate = awardInstrument is "sf44" or "gpc";
        if (awardFunding is null && !immediate) awardFunding = choices.Funding.FirstOrDefault(f => f.Available)?.Id ?? "";
        fundingInput = AddAwardChoice("Funding line", immediate ? "No funding line used" : "Choose a funding line…", choices.Funding, awardFunding,
            id => awardFunding = id, out var fundingReason);
        FundingChoiceReason = fundingReason;
        fundingInput.Disabled = immediate;
        BuildAwardPriorities(view); GateAward(view); // DESK-26
        if (string.IsNullOrEmpty(awardVendor) || string.IsNullOrEmpty(awardInstrument))
        {
            previewPanel.AddChild(DeskUi.Label("Choose an available supplier and arrangement, and any vehicle and funding line they need. The quote will then show the cost, timing and anything the terms leave uncovered."));
            return;
        }
        if (view.CorModeled && view.Rules.Instruments.TryGetValue(awardInstrument ?? "", out var instrument) && instrument.WrittenTerms)
        {
            var cors = view.CorChoices(editor.CapabilityId);
            var options = new[] { new Pick("", "None") }.Concat(cors.Select(c => new Pick(c.Id,
                c.Name + (string.IsNullOrEmpty(c.Unit) ? "" : " · " + c.Unit)))).ToArray();
            CorInput = AddPick(awardPanel, "Who will watch this contract?", options, awardCor, id =>
            {
                if (formVersion != awardFormVersion || formHash != session.StateHash || AtFrontDoor || ReadingTurnover || PlayingDay
                    || session.View is not { Finished: false }) return;
                awardCor = id; UpdateAwardPreview();
            });
            CorInput.Disabled = view.Finished;
            awardCor = Picked(CorInput);
            for (var i = 0; i < cors.Count; i++)
            {
                CorInput.SetItemTooltip(i + 1, cors[i].Why);
                var reason = DeskUi.Label(cors[i].Why);
                awardPanel.AddChild(reason); CorReasonLabels.Add(reason);
            }
        }
        else awardCor = null;
        var requirement = target.Card is null ? null : view.RequirementEditor(target.Card);
        quantity ??= Math.Max(1, editor.PeakDemandUnits); start ??= Math.Max(view.Day, requirement?.RequestedStartDay ?? view.Day);
        duration ??= Math.Max(1, requirement?.DurationDays ?? view.LastDay);
        // SpinBox stores doubles: this is its exact-integer display limit, not a simulation capacity rule.
        quantityInput = AddNumber($"Quantity ({editor.Unit})" + (editor.CardId is null ? "" : $" · Asked: {editor.RequestedQuantity ?? "No quantity stated"}"), quantity.Value, 9007199254740991, value => { quantity = checked((long)value); UpdateAwardPreview(); });
        startInput = AddNumber("Start day" + (editor.RequestedStartDay is { } askedStart ? $" · Asked: day {askedStart}" : ""), start.Value, int.MaxValue, value => { start = checked((int)value); UpdateAwardPreview(); });
        durationInput = AddNumber("Duration (days)" + (editor.RequestedDurationDays is { } askedDays ? $" · Asked: {askedDays} days" : ""), duration.Value, int.MaxValue, value => { duration = checked((int)value); UpdateAwardPreview(); });
        if (!AwardTermsComplete)
        {
            previewPanel.AddChild(DeskUi.Label("Choose an available vehicle and funding line for these terms before asking for the quote."));
            return;
        }
        AwardTrade = DeskUi.Label(); previewPanel.AddChild(AwardTrade); // DESK-26
        previewTiming = DeskUi.Label(); previewPanel.AddChild(previewTiming);
        previewCost = DeskUi.Label(); previewPanel.AddChild(previewCost);
        previewStops = DeskUi.Label(); previewStops.AddThemeColorOverride("font_color", TabletopTheme.Alert); previewPanel.AddChild(previewStops);
        previewGaps = DeskUi.Label(); previewGaps.AddThemeColorOverride("font_color", TabletopTheme.Warning); previewPanel.AddChild(previewGaps);
        previewPanel.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8), MouseFilter = MouseFilterEnum.Ignore });
        if (session.IsReplay) previewPanel.AddChild(DeskUi.Label("Awarding takes control and stops future replay actions."));
        UpdateAwardPreview();
    }

    private void UpdateAwardPreview()
    {
        var view = session.View; var editor = CurrentActionEditor;
        if (view is null || editor is null) return;
        if (!AwardTermsComplete) { PendingAward = null; AwardPreview = null; GateAward(view); return; }
        while (view.Arrangements.Any(a => a.Id == $"player-award-{awardSequence}") || view.InWork($"player-award-{awardSequence}")) awardSequence++;
        PendingAward = new AwardAction { Day = view.Day, Id = $"player-award-{awardSequence}", Card = editor.CardId,
            Site = editor.SiteId, Capability = editor.CapabilityId, Instrument = awardInstrument ?? "", Vendor = awardVendor ?? "",
            Vehicle = string.IsNullOrEmpty(awardVehicle) ? null : awardVehicle, FundingLine = string.IsNullOrEmpty(awardFunding) ? null : awardFunding,
            Cor = string.IsNullOrEmpty(awardCor) ? null : awardCor,
            Quantity = quantity ?? 1, StartDay = start, DurationDays = duration ?? 1,
            Priorities = WrittenAward ? [.. PriorityOrder.Where(awardPriorities.Contains)] : null }; // DESK-26
        try
        {
            AwardPreview = view.Preview(PendingAward);
            AwardTrade!.Text = AwardPreview.Trade ?? ""; AwardTrade.Visible = AwardPreview.Trade is not null; // DESK-26
            static string Range(int low, int high) => low == high ? low.ToString() : $"{low}–{high}";
            previewTiming.Text = $"It would take {Range(AwardPreview.ShopHoursMin, AwardPreview.ShopHoursMax)} shop hours. The earliest start is day {Range(AwardPreview.EarliestStartMin, AwardPreview.EarliestStartMax)}{DeskUi.QueueClause(AwardPreview.QueueAheadHours)}.";
            previewCost.Text = $"This would cost {DeskUi.Money(AwardPreview.DailyCents)} a day and {DeskUi.Money(AwardPreview.MobilizationCents)} to mobilize, for {DeskUi.Money(AwardPreview.TotalCents)} in total.";
            previewStops.Text = string.Join("\n\n", AwardPreview.HardStops);
            previewGaps.Text = string.Join("\n\n", AwardPreview.SoftGaps);
            previewStops.Visible = AwardPreview.HardStops.Count > 0;
            previewGaps.Visible = AwardPreview.SoftGaps.Count > 0;
            GateAward(view);
        }
        catch (OverflowException)
        {
            AwardPreview = null; AwardTrade!.Text = ""; AwardTrade.Hide(); previewCost.Text = ""; previewTiming.Text = ""; previewStops.Text = "These values are too large to preview."; previewStops.Visible = true;
            previewGaps.Text = ""; DeskUi.Gate(awardButton, AwardBlockedReason, "These values are too large to preview.");
        }
    }

    private void GoToMarket(string card)
    {
        ResetAwardForm(); awardTarget = card; SelectedCard = card;
        RefreshActionDesk(session.View); GetNode<TabContainer>("%DeskTabs").CurrentTab = 2;
    }
}
