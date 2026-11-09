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
    private OptionButton instrumentInput = null!, vendorInput = null!, vehicleInput = null!, fundingInput = null!;
    private SpinBox quantityInput = null!, startInput = null!, durationInput = null!;
    private Label previewCost = null!, previewStops = null!, previewGaps = null!;
    private Button awardButton = null!;
    internal AwardAction? PendingAward { get; private set; }
    internal ActionPreview? AwardPreview { get; private set; }
    internal ActionEditorView? CurrentActionEditor { get; private set; }
    internal OptionButton InstrumentInput => instrumentInput;
    internal OptionButton FundingInput => fundingInput;
    internal OptionButton? CorInput { get; private set; }
    internal OptionButton VendorInput => vendorInput;
    internal OptionButton VehicleInput => vehicleInput;
    internal SpinBox QuantityInput => quantityInput;
    internal SpinBox DurationInput => durationInput;
    internal Button AwardButton => awardButton;

    private void BuildActionDesk()
    {
        var split = new VSplitContainer { Name = "AwardEditor" };
        GetNode<TabContainer>("%DeskTabs").GetNode<Control>("Actions").AddChild(split);
        split.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var form = new Control { CustomMinimumSize = new Vector2(0, 150) }; split.AddChild(form);
        awardPanel = DeskUi.Panel(form, "Inputs");
        var lower = new VBoxContainer { CustomMinimumSize = new Vector2(0, 180) }; split.AddChild(lower);
        var holder = new Control { SizeFlagsVertical = SizeFlags.ExpandFill }; lower.AddChild(holder);
        previewPanel = DeskUi.Panel(holder, "Preview");
        awardFooter = new VBoxContainer(); lower.AddChild(awardFooter);
        split.SplitOffsets = [310];
    }
    private void ResetAwardForm()
    {
        awardTarget = awardInstrument = awardVendor = awardVehicle = awardFunding = awardCor = null;
        quantity = null; start = duration = null; awardSequence = 1;
    }

    private static List<AwardTarget> AwardTargets(SessionView view)
    {
        var targets = view.Cards.Select(c => view.RequirementEditor(c.Id)).Where(c => c is not null)
            .Select(c => new AwardTarget(c!.CardId, $"{c.CardId} · {c.SiteName} · {c.CapabilityLabel}", c.SiteId, c.CapabilityId, c.CardId)).ToList();
        targets.AddRange(view.MorningBoard().Tiles.Select(t => new AwardTarget(t.SiteId + "." + t.CapabilityId,
            $"{view.Scenario.Sites.Single(s => s.Id == t.SiteId).Name} · {t.Label} (no card)", t.SiteId, t.CapabilityId, null)));
        return targets;
    }

    private static OptionButton AddPick(VBoxContainer panel, string label, IReadOnlyList<Pick> options, string? selected, Action<string> changed)
    {
        panel.AddChild(DeskUi.Label(label));
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, FitToLongestItem = false };
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

    private static string? FundingBlock(FundingOption funding, int day)
    {
        var reasons = new List<string>();
        if (!funding.PaysForThis) reasons.Add($"Certified for {string.Join(", ", funding.Capabilities ?? [])} only");
        if (!funding.UsableToday) reasons.Add(day < funding.AvailableDay
            ? $"Available day {funding.AvailableDay}" : $"Expired after day {funding.ExpiresDay}");
        return reasons.Count == 0 ? null : string.Join("; ", reasons);
    }

    private static string? Picked(OptionButton picker) => picker.ItemCount == 0 ? null : picker.GetItemMetadata(picker.Selected).AsString();
    private SpinBox AddNumber(string label, double value, double max, Action<double> changed)
    {
        awardPanel.AddChild(DeskUi.Label(label));
        var input = new SpinBox { MinValue = 1, MaxValue = max, Step = 1, Value = value, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        input.ValueChanged += value => changed(value); awardPanel.AddChild(input); return input;
    }

    private void RefreshActionDesk(SessionView? view)
    {
        DeskUi.Clear(awardPanel); DeskUi.Clear(previewPanel); DeskUi.Clear(awardFooter);
        PendingAward = null; AwardPreview = null; CurrentActionEditor = null; CorInput = null;
        if (view is null) { awardPanel.AddChild(DeskUi.Label("Load a scenario to assemble an action.")); return; }
        var targets = AwardTargets(view);
        if (targets.Count == 0) { awardPanel.AddChild(DeskUi.Label("No site capabilities are available.")); return; }
        if (!targets.Any(t => t.Id == awardTarget)) awardTarget = targets.FirstOrDefault(t => t.Card == SelectedCard)?.Id ?? targets[0].Id;
        AddPick(awardPanel, "Requirement or site capability", targets.Select(t => new Pick(t.Id, t.Label)).ToArray(), awardTarget, id =>
        { awardTarget = id; quantity = null; start = duration = null; RefreshActionDesk(session.View); });
        var target = targets.Single(t => t.Id == awardTarget);
        var editor = view.ActionEditor(target.Site, target.Capability, target.Card)!; CurrentActionEditor = editor;
        instrumentInput = AddPick(awardPanel, "Instrument", editor.Instruments.Select(i => new Pick(i.Id, i.Label, i.UnavailableReason)).ToArray(), awardInstrument, id => { awardInstrument = id; RefreshActionDesk(session.View); });
        awardInstrument = Picked(instrumentInput);
        vendorInput = AddPick(awardPanel, "Vendor", editor.Vendors.Select(v => new Pick(v.Id, v.Name + (v.Standing is null ? "" : $" · {v.Standing}"))).ToArray(), awardVendor, id => { awardVendor = id; awardVehicle = null; RefreshActionDesk(session.View); });
        awardVendor = Picked(vendorInput);
        var vehicles = new[] { new Pick("", "No vehicle") }.Concat(editor.Vehicles.Where(v => v.VendorId == awardVendor)
            .Select(v => new Pick(v.Id, v.Id + " · " + v.Kind, v.OpenToday ? null : $"Ordering ended day {v.OrderingPeriodEndDay}"))).ToArray();
        vehicleInput = AddPick(awardPanel, "Vehicle", vehicles, awardVehicle, id => { awardVehicle = id; RefreshActionDesk(session.View); }); awardVehicle = Picked(vehicleInput);
        fundingInput = AddPick(awardPanel, "Funding line", editor.Funding.Select(f => new Pick(f.Id, $"{f.Id} · {DeskUi.Money(f.RemainingCents)} · expires {f.ExpiresDay}", FundingBlock(f, view.Day))).ToArray(), awardFunding, id => { awardFunding = id; RefreshActionDesk(session.View); });
        awardFunding = Picked(fundingInput);
        if (view.CorModeled && view.Rules.Instruments.TryGetValue(awardInstrument ?? "", out var instrument) && instrument.WrittenTerms)
        {
            var cors = new[] { new Pick("", "None") }.Concat(view.Cors.Select(c => new Pick(c.Id, CorText(view, c)))).ToArray();
            CorInput = AddPick(awardPanel, "COR", cors, awardCor, id => { awardCor = id; UpdateAwardPreview(); });
            awardCor = Picked(CorInput);
        }
        else awardCor = null;
        var requirement = target.Card is null ? null : view.RequirementEditor(target.Card);
        quantity ??= Math.Max(1, editor.PeakDemandUnits); start ??= Math.Max(view.Day, requirement?.RequestedStartDay ?? view.Day);
        duration ??= Math.Max(1, requirement?.DurationDays ?? view.LastDay);
        // SpinBox stores doubles: this is its exact-integer display limit, not a simulation capacity rule.
        quantityInput = AddNumber($"Quantity ({editor.Unit})", quantity.Value, 9007199254740991, value => { quantity = checked((long)value); UpdateAwardPreview(); });
        startInput = AddNumber("Requested start day", start.Value, int.MaxValue, value => { start = checked((int)value); UpdateAwardPreview(); });
        durationInput = AddNumber("Duration (days)", duration.Value, int.MaxValue, value => { duration = checked((int)value); UpdateAwardPreview(); });
        previewPanel.AddChild(DeskUi.Label("Live preview"));
        previewCost = DeskUi.Label(); previewPanel.AddChild(previewCost);
        previewStops = DeskUi.Label(); previewStops.AddThemeColorOverride("font_color", new Color(1, .4f, .4f)); previewPanel.AddChild(previewStops);
        previewGaps = DeskUi.Label(); previewGaps.AddThemeColorOverride("font_color", new Color(1, .75f, .25f)); previewPanel.AddChild(previewGaps);
        if (session.IsReplay) awardFooter.AddChild(DeskUi.Label("Awarding takes control and stops future replay actions."));
        awardButton = DeskUi.Button("Award", () =>
        {
            if (PendingAward is null || AwardPreview?.Accepted != true) return;
            SendAction(PendingAward);
        });
        awardFooter.AddChild(awardButton); UpdateAwardPreview();
    }

    private void UpdateAwardPreview()
    {
        var view = session.View; var editor = CurrentActionEditor;
        if (view is null || editor is null) return;
        while (view.Arrangements.Any(a => a.Id == $"player-award-{awardSequence}") || view.InWork($"player-award-{awardSequence}")) awardSequence++;
        PendingAward = new AwardAction { Day = view.Day, Id = $"player-award-{awardSequence}", Card = editor.CardId,
            Site = editor.SiteId, Capability = editor.CapabilityId, Instrument = awardInstrument ?? "", Vendor = awardVendor ?? "",
            Vehicle = string.IsNullOrEmpty(awardVehicle) ? null : awardVehicle, FundingLine = awardFunding,
            Cor = string.IsNullOrEmpty(awardCor) ? null : awardCor,
            Quantity = quantity ?? 1, StartDay = start, DurationDays = duration ?? 1 };
        try
        {
            AwardPreview = view.Preview(PendingAward);
            previewCost.Text = $"Daily {DeskUi.Money(AwardPreview.DailyCents)} · mobilization {DeskUi.Money(AwardPreview.MobilizationCents)}\nTotal {DeskUi.Money(AwardPreview.TotalCents)}\nShop {AwardPreview.ShopHoursMin}–{AwardPreview.ShopHoursMax} h; {AwardPreview.QueueAheadHours} h queued\nEarliest start day {AwardPreview.EarliestStartMin}–{AwardPreview.EarliestStartMax} (ignores queue ahead)";
            previewStops.Text = AwardPreview.HardStops.Count == 0 ? "No hard stops." : "Hard stops\n" + string.Join("\n", AwardPreview.HardStops);
            previewGaps.Text = AwardPreview.SoftGaps.Count == 0 ? "No soft gaps reported." : "Soft gaps\n" + string.Join("\n", AwardPreview.SoftGaps);
            previewStops.Visible = AwardPreview.HardStops.Count > 0;
            previewGaps.Visible = AwardPreview.SoftGaps.Count > 0;
            awardButton.Disabled = !AwardPreview.Accepted || view.Finished;
        }
        catch (OverflowException)
        {
            AwardPreview = null; previewCost.Text = ""; previewStops.Text = "These values are too large to preview."; previewStops.Visible = true;
            previewGaps.Text = ""; awardButton.Disabled = true;
        }
    }
}
