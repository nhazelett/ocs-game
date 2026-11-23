using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private sealed record AwardTarget(string Id, string Label, string Site, string Capability, string? Card);
    private sealed record Pick(string Id, string Text, string? Block = null);
    private sealed record AwardDraft(string? Instrument, string? Vendor, string? Vehicle, string? Funding,
        string? Cor, string? Assignee, long? Quantity, int? Start, int? Duration, string[] Priorities, bool Touched);
    private readonly Dictionary<string, AwardDraft> awardDrafts = [];
    private int awardDraftDay;
    private readonly Dictionary<string, string> awardReceipts = [];
    private VBoxContainer awardPanel = null!;
    private VBoxContainer previewPanel = null!, awardFooter = null!;
    private string? awardTarget, awardInstrument, awardVendor, awardVehicle, awardFunding, awardCor, awardAssignee;
    private long? quantity;
    private int? start, duration;
    private readonly List<Label> awardChoiceReasons = [];
    private Label? priorityHelp;
    private int awardSequence = 1;
    private int awardFormVersion;
    private bool awardFormTouched;
    private OptionButton instrumentInput = null!, vendorInput = null!, vehicleInput = null!, fundingInput = null!;
    private SpinBox quantityInput = null!, startInput = null!, durationInput = null!;
    private Label previewCost = null!, previewTiming = null!, previewStops = null!, previewGaps = null!;
    private Button awardButton = null!;
    private VBoxContainer? previewLearned, awardLearnedActions;
    internal Label AwardSummaryLabel { get; private set; } = null!;
    internal Dictionary<string, Button> AwardAddButtons { get; } = [];
    internal Label VehicleChoiceReason { get; private set; } = null!;
    internal AwardAction? PendingAward { get; private set; }
    internal ActionPreview? AwardPreview { get; private set; }
    internal ActionEditorView? CurrentActionEditor { get; private set; }
    internal Label BuyRequestLabel { get; private set; } = null!;
    internal OptionButton? InitiativeTargetInput { get; private set; }
    internal Label? InitiativeDetails { get; private set; }
    internal OptionButton InstrumentInput => instrumentInput;
    internal OptionButton FundingInput => fundingInput;
    internal OptionButton? CorInput { get; private set; }
    internal Label? SupplierKnowledge { get; private set; }
    internal Label? SupplierNotice { get; private set; }
    internal OptionButton VendorInput => vendorInput;
    internal OptionButton VehicleInput => vehicleInput;
    internal SpinBox QuantityInput => quantityInput;
    internal SpinBox DurationInput => durationInput;
    internal SpinBox StartInput => startInput;
    internal Button AwardButton => awardButton;
    internal Button AwardBackButton { get; private set; } = null!;
    internal Label AwardTiming => previewTiming;
    internal Label AwardCost => previewCost;
    internal Button AwardBlockedReason { get; private set; } = null!;
    internal Control? AwardBlockedControl { get; private set; }
    internal ScrollContainer AwardScroll => (ScrollContainer)awardPanel.GetParent().GetParent();

    private void GateAward(SessionView? view)
    {
        string? reason = view is null ? "Start a deployment to arrange support."
            : view.Finished ? DeskUi.FinishedReason
            : CurrentActionEditor is null ? "Choose a request or something to buy on your own initiative."
            : string.IsNullOrEmpty(awardVendor) ? "Choose an available supplier."
            : string.IsNullOrEmpty(awardInstrument) ? "Choose an available arrangement."
            : awardInstrument is "bpaCall" or "idiqOrder" && string.IsNullOrEmpty(awardVehicle) ? "Choose a contract vehicle."
            : awardInstrument is not ("sf44" or "gpc") && string.IsNullOrEmpty(awardFunding) ? "Choose an available funding line."
            : AwardPreview?.HardStops.FirstOrDefault();
        reason ??= AwardPreview is null ? "Complete the terms to see the quote." : null;
        SetAwardGate(reason, AwardGateControl(view, reason));
        // An untouched form is an invitation to start another buy, including after a successful one.
        // Keep submission gated, but wait for an edit before showing missing-field errors.
        if (!awardFormTouched && view is { Finished: false } && CurrentActionEditor is not null)
        { AwardBlockedReason.Text = ""; AwardBlockedReason.Hide(); }
    }

    private void SetAwardGate(string? reason, Control? control)
    {
        awardButton.Disabled = reason is not null;
        AwardBlockedReason.Text = reason ?? ""; AwardBlockedReason.Visible = reason is not null;
        AwardBlockedControl = control;
        AwardBlockedReason.Disabled = control is null;
        AwardBlockedReason.TooltipText = control is null ? "" : "Show this choice in the buy form";
    }

    // Compare authoritative previews to identify a field's reason without interpreting its prose.
    private string? PickTwoReason(SessionView view)
        => WrittenAward && awardPriorities.Count != 2 && PendingAward is { } action && AwardPreview is { } preview
            ? preview.HardStops.Except(view.Preview(action with { Priorities = ["fast", "good"] }).HardStops).FirstOrDefault() : null;

    private Control? AwardGateControl(SessionView? view, string? reason)
    {
        if (view is null || view.Finished || reason is null || CurrentActionEditor is null) return null;
        if (string.IsNullOrEmpty(awardVendor)) return vendorInput;
        if (string.IsNullOrEmpty(awardInstrument)) return instrumentInput;
        if (awardInstrument is "bpaCall" or "idiqOrder" && string.IsNullOrEmpty(awardVehicle)) return vehicleInput;
        if (awardInstrument is not ("sf44" or "gpc") && string.IsNullOrEmpty(awardFunding)) return fundingInput;
        if (reason == PickTwoReason(view)) return PriorityInputs.Values.FirstOrDefault()?.GetParent<Control>();
        if (PendingAward is { Cor: not null } action && CorInput is not null
            && !view.Preview(action with { Cor = null }).HardStops.Contains(reason)) return CorInput;
        return null;
    }

    private void BuildActionDesk()
    {
        var body = DeskUi.Pane(RequestBuyColumn, "AwardEditor", out awardFooter);
        awardFooter.AddThemeConstantOverride("separation", 2);
        awardPanel = new VBoxContainer { Name = "Inputs" }; body.AddChild(awardPanel);
        awardPanel.AddThemeConstantOverride("separation", 10);
        previewPanel = new VBoxContainer { Name = "Quote" }; body.AddChild(previewPanel);
        previewPanel.AddThemeConstantOverride("separation", 10);
    }
    private void ResetAwardForm()
    {
        awardTarget = awardInstrument = awardVendor = awardVehicle = awardFunding = awardCor = awardAssignee = null;
        awardChoiceNotice = null; awardFormTouched = false;
        awardPriorities.Clear(); // DESK-26
        quantity = null; start = duration = null; awardSequence = 1;
    }

    private void RefreshAwardDraftDay(SessionView? view)
    {
        if (awardDraftDay == (view?.Day ?? 0)) return;
        awardDrafts.Clear(); awardReceipts.Clear(); awardDraftDay = view?.Day ?? 0;
    }

    private void SwitchAwardTarget(string? id)
    {
        RefreshAwardDraftDay(session.View);
        if (awardTarget == id) return;
        if (awardTarget is { } previous)
            awardDrafts[previous] = new(awardInstrument, awardVendor, awardVehicle, awardFunding, awardCor, awardAssignee,
                quantity, start, duration, [.. awardPriorities], awardFormTouched);
        ResetAwardForm(); awardTarget = id;
        if (id is null || !awardDrafts.TryGetValue(id, out var draft)) return;
        awardInstrument = draft.Instrument; awardVendor = draft.Vendor; awardVehicle = draft.Vehicle;
        awardFunding = draft.Funding; awardCor = draft.Cor; awardAssignee = draft.Assignee; quantity = draft.Quantity; start = draft.Start;
        duration = draft.Duration; awardPriorities.UnionWith(draft.Priorities); awardFormTouched = draft.Touched;
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

    private static OptionButton AddPick(VBoxContainer panel, string label, IReadOnlyList<Pick> options, string? selected, Action<string> changed, bool inline = false)
    {
        var caption = DeskUi.Label(label);
        var row = inline ? new HBoxContainer() : null;
        if (row is not null) { row.AddThemeConstantOverride("separation", 12); panel.AddChild(row); caption.AutowrapMode = TextServer.AutowrapMode.Off; caption.CustomMinimumSize = new Vector2(150, 0); caption.SizeFlagsVertical = SizeFlags.ShrinkCenter; }
        (row as Node ?? panel).AddChild(caption);
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
        (row as Node ?? panel).AddChild(picker);
        foreach (var option in options.Where(o => o.Block is not null))
            panel.AddChild(DeskUi.Label(option.Text + ": " + option.Block));
        return picker;
    }

    private static string? Picked(OptionButton picker) => picker.ItemCount == 0 || picker.Selected < 0 ? null : picker.GetItemMetadata(picker.Selected).AsString();
    private SpinBox AddNumber(VBoxContainer parent, string name, string label, double value, double max, Action<double> changed)
    {
        var caption = DeskUi.Label(label); caption.Name = name + "Label"; parent.AddChild(caption);
        var input = new SpinBox { Name = name, MinValue = 1, MaxValue = max, Step = 1, Value = value, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        input.ValueChanged += value => { awardFormTouched = true; changed(value); }; parent.AddChild(input); return input;
    }

    private void RefreshActionDesk(SessionView? view)
    {
        RefreshAwardDraftDay(view);
        DeskUi.Clear(awardPanel); DeskUi.Clear(previewPanel); DeskUi.Clear(awardFooter);
        awardChoiceReasons.Clear(); priorityHelp = null;
        PendingAward = null; AwardPreview = null; CurrentActionEditor = null; CurrentAwardChoices = null; CorInput = null; CorReasonLabels.Clear(); InitiativeTargetInput = null; InitiativeDetails = null;
        PriorityInputs.Clear(); AwardTrade = null; // DESK-26
        previewLearned = null; AwardAddButtons.Clear(); awardWriters = null; AwardWriterPicker = null;
        var formVersion = ++awardFormVersion;
        var formHash = session.StateHash;
        awardButton = DeskUi.Button("Arrange support", () =>
        {
            if (formVersion != awardFormVersion || formHash != session.StateHash || AtFrontDoor || ReadingTurnover || PlayingDay) return;
            if (PendingAward is not null && AwardPreview?.Accepted == true && session.View?.Finished == false)
            {
                var action = PendingAward;
                awardPriorities.Clear(); // DESK-26: each new written contract gets its own pick.
                var result = SendAction(action);
                if (result.Accepted)
                {
                    SelectedCard = action.Card;
                    if (awardTarget is { } bought) awardDrafts.Remove(bought);
                    var target = action.Card ?? action.Site + "." + action.Capability;
                    if (result.RecordId is { } recordId) awardReceipts[target] = recordId;
                    ResetAwardForm(); awardTarget = target; RefreshActionDesk(session.View);
                    ShowRequests();
                    RequestScroll.ScrollVertical = 0; AwardScroll.ScrollVertical = 0;

                }
            }
        });
        awardButton.ThemeTypeVariation="TabletopPrimary";
        AwardBackButton = RequestBackButton;
        awardWriters = new VBoxContainer(); awardFooter.AddChild(awardWriters);
        awardLearnedActions = new VBoxContainer(); awardFooter.AddChild(awardLearnedActions);
        AwardSummaryLabel = DeskUi.Label(); AwardSummaryLabel.Hide(); awardFooter.AddChild(AwardSummaryLabel);
        awardFooter.AddChild(awardButton);
        AwardBlockedReason = DeskUi.Button("", () =>
        {
            if (formVersion != awardFormVersion || formHash != session.StateHash || AtFrontDoor || ReadingTurnover || PlayingDay) return;
            if (AwardBlockedControl is { } control && IsInstanceValid(control) && control.IsVisibleInTree())
                AwardScroll.EnsureControlVisible(control);
        });
        AwardBlockedReason.Flat = true;
        AwardBlockedReason.MouseDefaultCursorShape = CursorShape.PointingHand;
        using (var empty = new StyleBoxEmpty())
            foreach (var state in new[] { "normal", "hover", "pressed", "disabled" }) AwardBlockedReason.AddThemeStyleboxOverride(state, empty);
        AwardBlockedReason.AddThemeColorOverride("font_color", TabletopTheme.Moss);
        AwardBlockedReason.AddThemeColorOverride("font_hover_color", TabletopTheme.Ink);
        AwardBlockedReason.AddThemeColorOverride("font_disabled_color", TabletopTheme.Alert);
        awardFooter.AddChild(AwardBlockedReason); GateAward(view);
        if (view is null) { awardPanel.AddChild(DeskUi.Label("Load a scenario to assemble an action.")); return; }
        SupplierKnowledge = null; SupplierNotice = null;
        VBoxContainer? supplierFacts = null;
        awardPanel.AddChild(DeskUi.Heading("Arrange support"));
        var targets = AwardTargets(view);
        if (targets.Count == 0) { awardPanel.AddChild(DeskUi.Label("No site capabilities are available.")); return; }
        if (!targets.Any(t => t.Id == awardTarget))
            awardTarget = (targets.FirstOrDefault(t => SelectedCard is not null && t.Card == SelectedCard)
                ?? targets.FirstOrDefault(t => t.Card is not null) ?? targets[0]).Id;
        void ChooseTarget(string id)
        {
            if (formVersion != awardFormVersion || formHash != session.StateHash) return;
            SelectAwardTarget(id);
        }
        var target = targets.Single(t => t.Id == awardTarget);
        if (awardReceipts.TryGetValue(target.Id, out var recordId) && view.Decision(recordId) is { } receipt)
        {
            var acknowledgement = DeskUi.Label(receipt.Sentence + "\n" + receipt.Now); acknowledgement.SetMeta("arranged_award", target.Id);
            awardPanel.AddChild(acknowledgement); awardPanel.MoveChild(acknowledgement, 0);
        }
        SelectedCard = target.Card; RenderRequirementContext(view);
        var ownTargets = targets.Where(t => t.Card is null).ToArray();
        BuyRequestLabel = DeskUi.Label(target.Card is null ? target.Label : view.RequirementEditor(target.Card)!.Title);
        BuyRequestLabel.SetMeta("buy_request", target.Id); awardPanel.AddChild(BuyRequestLabel);
        if (target.Card is null)
            InitiativeTargetInput = AddPick(awardPanel, "What would you buy on your own initiative?",
                ownTargets.Select(t => new Pick(t.Id, t.Label)).ToArray(), target.Id, ChooseTarget, inline: true);
        if (target.Card is null && view.OwnInitiative.FirstOrDefault(t => t.SiteId == target.Site && t.CapabilityId == target.Capability) is { } initiative)
        {
            InitiativeDetails = DeskUi.Label(initiative.Why + "\nWho sells it: " + string.Join(", ", initiative.Vendors.Select(view.NameOf)) + "."
                + (initiative.Have ? view.Rules.Model.Capabilities.Single(c => c.Id == initiative.CapabilityId).Lift
                    ? "\nA boat to " + view.Scenario.Islands.Single(i => i.Id == view.Scenario.Sites.Single(s => s.Id == initiative.SiteId).Island).Name + " is running today."
                    : "\nThe office has one running today." : ""));
            awardPanel.AddChild(InitiativeDetails);
        }
        var editor = view.ActionEditor(target.Site, target.Capability, target.Card)!; CurrentActionEditor = editor;
        var choices = ReconcileAwardChoices(view, target);
        instrumentInput = AddAwardChoice("Arrangement", "Choose an arrangement…", choices.Instruments, awardInstrument,
            id => { awardInstrument = id; }, out var instrumentReason, AwardPickerNotice(view, target, "arrangement", choices), listReasonsOnly: true);
        InstrumentChoiceReason = instrumentReason;
        var requirement = target.Card is null ? null : view.RequirementEditor(target.Card);
        quantity ??= Math.Max(1, editor.PeakDemandUnits); start ??= Math.Max(view.Day, requirement?.RequestedStartDay ?? view.Day);
        duration ??= Math.Max(1, requirement?.DurationDays ?? view.LastDay);
        // SpinBox stores doubles: this is its exact-integer display limit, not a simulation capacity rule.
        var amounts = new HBoxContainer { Name = "BuyAmounts" }; amounts.AddThemeConstantOverride("separation", 12); awardPanel.AddChild(amounts);
        VBoxContainer AmountColumn(string name) { var column = new VBoxContainer { Name = name, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = name == "Amount" ? 1.5f : 1 }; column.AddThemeConstantOverride("separation", 4); amounts.AddChild(column); return column; }
        quantityInput = AddNumber(AmountColumn("Amount"), "Quantity", $"Quantity ({editor.Unit})" + (editor.CardId is null ? "" : $" · Asked: {editor.RequestedQuantity ?? "No quantity stated"}"), quantity.Value, 9007199254740991, value => { quantity = checked((long)value); UpdateAwardPreview(); });
        startInput = AddNumber(AmountColumn("Start"), "StartDay", "Start day" + (editor.RequestedStartDay is { } askedStart ? $" · Asked: day {askedStart}" : ""), start.Value, int.MaxValue, value => { start = checked((int)value); UpdateAwardPreview(); });
        durationInput = AddNumber(AmountColumn("Days"), "Duration", "Duration (days)" + (editor.RequestedDurationDays is { } askedDays ? $" · Asked: {askedDays} days" : ""), duration.Value, int.MaxValue, value => { duration = checked((int)value); UpdateAwardPreview(); });
        vendorInput = AddAwardChoice("Supplier", "Choose a supplier…", choices.Vendors, awardVendor,
            id => { awardVendor = id; }, out var vendorReason, AwardPickerNotice(view, target, "supplier", choices, awardChoiceNotice), listReasonsOnly: true);
        VendorChoiceReason = vendorReason;
        if (!string.IsNullOrEmpty(awardVendor) && editor.Vendors.FirstOrDefault(v => v.Id == awardVendor) is { } supplier)
        {
            var notice = view.VendorCards().FirstOrDefault(c => c.Id == supplier.Id)?.Sells
                .FirstOrDefault(s => s.CapabilityId == editor.CapabilityId)?.NoticeSays;
            supplierFacts = new VBoxContainer(); supplierFacts.AddThemeConstantOverride("separation", 10);
            supplierFacts.AddChild(DeskUi.Label($"{supplier.Name} is based on {supplier.IslandName}.\nCash: "
                + Payment(supplier.PaymentKnown ? supplier.AcceptsCash : null) + " · Card: " + Payment(supplier.PaymentKnown ? supplier.AcceptsGpc : null)));
            SupplierKnowledge = DeskUi.Label("Usual capacity per day: " + (supplier.CapacityKnown ? $"{DeskUi.Quantity(supplier.CapacityPerDay)} {editor.Unit}" : "—")
                + " · Uncommitted: " + (supplier.CapacityKnown ? DeskUi.Quantity(supplier.CapacityFreeToday) : "—")
                + (supplier.Heard is { } heard ? "\n" + heard : ""));
            supplierFacts.AddChild(SupplierKnowledge);
            SupplierNotice = DeskUi.Label((supplier.PaymentKnown ? "Starts " + DeskUi.Count(supplier.LeadTimeDays, "day") + " after a signed order" : "Time after a signed order: —")
                + (notice is null ? "" : "\n" + notice));
            supplierFacts.AddChild(SupplierNotice);

        }
        var needsVehicle = awardInstrument is "bpaCall" or "idiqOrder";
        vehicleInput = AddAwardChoice("Contract vehicle", needsVehicle ? "Choose a contract vehicle…" : "No contract vehicle needed", choices.Vehicles, awardVehicle,
            id => awardVehicle = id, out var vehicleReason, needsVehicle ? AwardPickerNotice(view, target, "vehicle", choices) : null);
        VehicleChoiceReason = vehicleReason;
        vehicleInput.Disabled = !needsVehicle;
        vehicleInput.GetParent<Control>().Visible = needsVehicle;
        if (!needsVehicle) { vehicleReason.Text = ""; vehicleReason.Hide(); }
        var immediate = awardInstrument is "sf44" or "gpc";
        if (awardFunding is null && !immediate) awardFunding = choices.Funding.FirstOrDefault(f => f.Available)?.Id ?? "";
        fundingInput = AddAwardChoice("Funding line", immediate ? "No funding line used" : "Choose a funding line…", choices.Funding, immediate ? "" : awardFunding,
            id => awardFunding = id, out var fundingReason, !immediate ? AwardPickerNotice(view, target, "funding", choices) : null);
        FundingChoiceReason = fundingReason;
        fundingInput.Disabled = immediate;
        fundingInput.GetParent<Control>().Visible = !immediate;
        if (view.CorModeled && view.Rules.Instruments.TryGetValue(awardInstrument ?? "", out var instrument) && instrument.WrittenTerms)
        {
            var cors = view.CorChoices(editor.CapabilityId);
            var options = new[] { new Pick("", "None") }.Concat(cors.Select(c => new Pick(c.Id,
                c.Why.Split(" and ", 2, StringSplitOptions.None)[0]
                + $" · catches about {c.ReportPermille / 10m:0.#}%, reports "
                + (c.LagDays == 0 ? "the same day" : DeskUi.Count(c.LagDays, "day") + " late")))).ToArray();
            CorInput = AddPick(awardPanel, "Contract monitor (COR)", options, awardCor, id =>
            {
                if (formVersion != awardFormVersion || formHash != session.StateHash || AtFrontDoor || ReadingTurnover || PlayingDay
                    || session.View is not { Finished: false }) return;
                awardCor = id; UpdateAwardPreview();
            }, inline: true);
            CorInput.Disabled = view.Finished;
            awardCor = Picked(CorInput);
            for (var i = 0; i < cors.Count; i++)
            {
                CorInput.SetItemTooltip(i + 1, cors[i].Why);
                var reason = DeskUi.Label("Possible COR: " + cors[i].Why);
                CorReasonLabels.Add(reason);
            }
        }
        BuildAwardPriorities(view); GateAward(view); // DESK-26
        foreach (var reason in awardChoiceReasons) awardPanel.AddChild(reason);
        if (priorityHelp is not null) awardPanel.AddChild(priorityHelp);
        foreach (var reason in CorReasonLabels) awardPanel.AddChild(reason);
        if (supplierFacts is not null) awardPanel.AddChild(supplierFacts);
        if (string.IsNullOrEmpty(awardVendor) || string.IsNullOrEmpty(awardInstrument))
        {
            previewPanel.AddChild(DeskUi.Label("Choose an arrangement and an available supplier, and any vehicle and funding line they need. The quote will then show the cost, timing and anything the terms leave uncovered."));
            return;
        }
        if (!AwardTermsComplete)
        {
            previewPanel.AddChild(DeskUi.Label("Choose a contract vehicle and funding line for these terms before asking for the quote."));
            return;
        }
        if (AwardTrade is null) { AwardTrade = DeskUi.Label(); previewPanel.AddChild(AwardTrade); }
        previewTiming = DeskUi.Label(); previewPanel.AddChild(previewTiming);
        previewCost = DeskUi.Label(); previewPanel.AddChild(previewCost);
        previewStops = DeskUi.Label(); previewStops.AddThemeColorOverride("font_color", TabletopTheme.Alert); previewPanel.AddChild(previewStops);
        previewGaps = DeskUi.Label(); previewGaps.AddThemeColorOverride("font_color", TabletopTheme.Warning); previewPanel.AddChild(previewGaps);
        previewLearned = new VBoxContainer(); previewLearned.AddThemeConstantOverride("separation", 8); previewPanel.AddChild(previewLearned);
        if (session.IsReplay) previewPanel.AddChild(DeskUi.Label("Awarding takes control and stops future replay actions."));
        UpdateAwardPreview();
    }

    private void UpdateAwardPreview()
    {
        var view = session.View; var editor = CurrentActionEditor;
        if (view is null || editor is null) return;
        if (!AwardTermsComplete) { if (SupplierNotice is not null) SupplierNotice.Show(); PendingAward = null; AwardPreview = null; ClearAwardWriter(); RefreshRequestBuyWarnings(); GateAward(view); return; }
        while (view.AwardIdTaken($"player-award-{awardSequence}")) awardSequence++;
        awardAssignee ??= view.Team.Single(m => m.Role == "you").Id;
        PendingAward = new AwardAction { Day = view.Day, Id = $"player-award-{awardSequence}", Card = editor.CardId,
            Site = editor.SiteId, Capability = editor.CapabilityId, Instrument = awardInstrument ?? "", Vendor = awardVendor ?? "",
            Vehicle = string.IsNullOrEmpty(awardVehicle) ? null : awardVehicle, FundingLine = awardInstrument is "sf44" or "gpc" || string.IsNullOrEmpty(awardFunding) ? null : awardFunding,
            Cor = view.Rules.Instruments[awardInstrument!].WrittenTerms && !string.IsNullOrEmpty(awardCor) ? awardCor : null,
            Quantity = quantity ?? 1, StartDay = start, DurationDays = duration ?? 1,
            Assignee = awardInstrument is "sf44" or "gpc" ? null : awardAssignee,
            Priorities = WrittenAward ? [.. PriorityOrder.Where(awardPriorities.Contains)] : null }; // DESK-26
        try
        {
            AwardPreview = view.Preview(PendingAward);
            SupplierNotice?.Hide();
            RefreshAwardWriter(view);
            AwardTrade!.Text = AwardPreview.Trade ?? ""; AwardTrade.Visible = AwardPreview.Trade is not null; // DESK-26
            static string Range(int low, int high) => low == high ? low.ToString() : $"{low}–{high}";
            previewTiming.Text = $"It would take {PreviewHours(AwardPreview, shop: true)}. The earliest start is day {Range(AwardPreview.EarliestStartMin, AwardPreview.EarliestStartMax)}{DeskUi.QueueClause(AwardPreview.QueueAheadHours)}.";
            previewCost.Text = $"This would cost {DeskUi.Money(AwardPreview.DailyCents)} a day and {DeskUi.Money(AwardPreview.MobilizationCents)} to mobilize, for {DeskUi.Money(AwardPreview.TotalCents)} in total.";
            previewStops.Text = string.Join("\n\n", AwardPreview.HardStops);
            previewGaps.Text = string.Join("\n\n", AwardPreview.SoftGaps);
            var summary = view.AwardSummary(PendingAward);
            AwardSummaryLabel.Text = summary?.Sentence ?? ""; AwardSummaryLabel.Visible = summary is not null;
            AwardSummaryLabel.AddThemeColorOverride("font_color", summary?.Attention == true ? TabletopTheme.Warning : TabletopTheme.Ink);
            if (previewLearned is not null)
            {
                DeskUi.Clear(previewLearned); DeskUi.Clear(awardLearnedActions!); AwardAddButtons.Clear();
                foreach (var line in (editor.CardId is null ? null : view.RequirementEditor(editor.CardId))?.Lines.Where(l => l.Known && !l.On && l.CanAdd) ?? [])
                {
                    var text = (line.LearnedFrom is { } person ? person.WithUnit + " told you: " : "") + (line.Learned ?? line.GapIfOff ?? line.Label);
                    var warning = DeskUi.Label(text); warning.AddThemeColorOverride("font_color", TabletopTheme.Warning); previewLearned.AddChild(warning);
                    var add = AddLearnedLineButton(awardLearnedActions!, view, editor.CardId!, line);
                    AwardAddButtons[line.Id] = add;
                }
            }
            previewStops.Visible = AwardPreview.HardStops.Count > 0;
            previewGaps.Visible = AwardPreview.SoftGaps.Count > 0;
            RefreshRequestBuyWarnings(); GateAward(view);
        }
        catch (OverflowException)
        {
            AwardSummaryLabel.Text = ""; AwardSummaryLabel.Hide(); ClearAwardWriter();
            SupplierNotice?.Show();
            AwardPreview = null; AwardTrade!.Text = ""; AwardTrade.Hide(); previewCost.Text = ""; previewTiming.Text = ""; previewStops.Text = "These values are too large to preview."; previewStops.Visible = true;
            previewGaps.Text = ""; RefreshRequestBuyWarnings(); SetAwardGate("These values are too large to preview.", null);
        }
    }

    internal void SelectAwardTarget(string id)
    {
        if (session.View is not { } view || !AwardTargets(view).Any(t => t.Id == id)) return;
        SwitchAwardTarget(id); SelectedCard = AwardTargets(view).Single(t => t.Id == id).Card;
        RefreshActionDesk(view);
    }

    internal void GoToOwnInitiative()
    {
        if (session.View is not { Finished: false } view || AtFrontDoor || ReadingTurnover || PlayingDay) return;
        SwitchAwardTarget(AwardTargets(view).FirstOrDefault(t => t.Card is null)?.Id);
        RefreshActionDesk(view); ShowMarket();
    }

    internal void GoToMarket(string card)
    {
        if (session.View is not { } view || AtFrontDoor || ReadingTurnover || PlayingDay
            || !view.Cards.Any(c => c.Id == card)) return;
        SwitchAwardTarget(card); SelectedCard = card;
        RefreshActionDesk(session.View); ShowMarket();
    }
}
