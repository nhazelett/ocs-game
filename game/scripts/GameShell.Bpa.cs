using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private sealed record BpaOffer(string Capability, string Label, VendorOption Vendor);
    private VBoxContainer directoryList = null!, bpaForm = null!, knownVehicles = null!;
    private Label bpaPreviewText = null!;
    private string? bpaVendor, bpaCapability;
    private readonly HashSet<string> bpaScope = new(StringComparer.Ordinal);
    private long? bpaCallLimit;
    private int bpaSequence = 1;
    internal EstablishBpaAction? PendingEstablish { get; private set; }
    internal ActionPreview? EstablishPreview { get; private set; }
    internal Button EstablishButton { get; private set; } = null!;
    internal Dictionary<string, Button> EstablishVendorButtons { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, CheckBox> BpaScopeInputs { get; } = new(StringComparer.Ordinal);
    internal OptionButton BpaCapabilityInput { get; private set; } = null!;
    internal CheckBox CustomBpaLimit { get; private set; } = null!;
    internal SpinBox BpaLimitInput { get; private set; } = null!;

    private void BuildBpaDesk()
    {
        var tabs = GetNode<TabContainer>("%DeskTabs");
        var split = new VSplitContainer { Name = "BpaDirectory" };
        tabs.GetNode<Control>("Directory").AddChild(split); split.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var top = new Control { CustomMinimumSize = new Vector2(0, 120) }; split.AddChild(top);
        directoryList = DeskUi.Panel(top, "Vendors");
        var bottom = new VBoxContainer { CustomMinimumSize = new Vector2(0, 260) }; split.AddChild(bottom);
        var holder = new Control { SizeFlagsVertical = SizeFlags.ExpandFill }; bottom.AddChild(holder);
        bpaForm = DeskUi.Panel(holder, "EstablishBpa");
        bpaPreviewText = DeskUi.Label(); bottom.AddChild(bpaPreviewText);
        EstablishButton = DeskUi.Button("Establish BPA", () =>
        {
            if (PendingEstablish is not null && EstablishPreview?.Accepted == true) SendAction(PendingEstablish);
        });
        bottom.AddChild(EstablishButton); split.SplitOffsets = [180];
        knownVehicles = DeskUi.Panel(tabs.GetNode<Control>("Vehicles"), "KnownVehicles");
    }

    private static List<BpaOffer> DirectoryOffers(SessionView view) => AwardTargets(view)
        .Select(t => view.ActionEditor(t.Site, t.Capability, t.Card)!)
        .SelectMany(e => e.Vendors.Select(v => new BpaOffer(e.CapabilityId, e.CapabilityLabel, v)))
        .DistinctBy(o => (o.Capability, o.Vendor.Id)).ToList();

    private void ResetBpaForm()
    {
        bpaVendor = bpaCapability = null; bpaScope.Clear(); bpaCallLimit = null; bpaSequence = 1;
    }

    private void RefreshBpaDesk(SessionView? view)
    {
        DeskUi.Clear(directoryList); DeskUi.Clear(knownVehicles); EstablishVendorButtons.Clear();
        if (view is null)
        {
            directoryList.AddChild(DeskUi.Label("Deploy or load a replay to see the market directory."));
            knownVehicles.AddChild(DeskUi.Label("No active session."));
            RefreshBpaForm(view, []); return;
        }
        var offers = DirectoryOffers(view);
        foreach (var group in offers.GroupBy(o => o.Vendor.Id))
        {
            var vendor = group.First().Vendor;
            var row = DeskUi.Button($"{DeskUi.ListLabel(vendor.Name)} · {vendor.IslandName}\n{string.Join("\n", group.Select(o => $"{o.Label}: {o.Vendor.CapacityFreeToday}/{o.Vendor.CapacityPerDay} free · lead {o.Vendor.LeadTimeDays} days · {DeskUi.Money(o.Vendor.UnitPriceCents)}/unit/day"))}" + (vendor.Standing is null ? "" : $"\nStanding: {vendor.Standing}") + "\nEstablish BPA…", () =>
            {
                bpaVendor = vendor.Id; bpaCapability = null; bpaScope.Clear(); RefreshBpaDesk(session.View);
            });
            row.ToggleMode = true; row.ButtonPressed = bpaVendor == vendor.Id;
            directoryList.AddChild(row); EstablishVendorButtons.Add(vendor.Id, row);
        }
        foreach (var vehicle in AwardTargets(view).Select(t => view.ActionEditor(t.Site, t.Capability, t.Card)!).SelectMany(e => e.Vehicles).DistinctBy(v => v.Id))
        {
            var label = DeskUi.Label($"{view.VehicleName(vehicle.Id)}\nScope: {string.Join(", ", vehicle.ScopeLines.Select(id => ScopeLineName(view, id)))}\nCall limit: {(vehicle.CallLimitCents is { } limit ? DeskUi.Money(limit) : "not specified")}\nOrdering through day {vehicle.OrderingPeriodEndDay} · {(vehicle.OpenToday ? "open today" : "closed")}\nPlace of performance: {string.Join(", ", vehicle.PlaceOfPerformance.Select(view.NameOf))}");
            label.SetMeta("vehicle", vehicle.Id); knownVehicles.AddChild(label);
        }
        foreach (var work in view.Work.Where(w => w.Kind == WorkKind.EstablishVehicle && w.CompletedDay is null))
            knownVehicles.AddChild(DeskUi.Label($"Setup in work: {work.Label}"));
        if (knownVehicles.GetChildCount() == 0) knownVehicles.AddChild(DeskUi.Label("No known vehicles."));
        RefreshBpaForm(view, offers);
    }

    private void RefreshBpaForm(SessionView? view, IReadOnlyList<BpaOffer> offers)
    {
        DeskUi.Clear(bpaForm); BpaScopeInputs.Clear(); PendingEstablish = null; EstablishPreview = null;
        bpaPreviewText.Text = ""; EstablishButton.Disabled = true;
        var available = offers.Where(o => o.Vendor.Id == bpaVendor).ToArray();
        if (view is null || available.Length == 0) { bpaForm.AddChild(DeskUi.Label("Choose a vendor above to assemble a BPA.")); return; }
        bpaForm.AddChild(DeskUi.Label($"Establish BPA with {available[0].Vendor.Name}"));
        BpaCapabilityInput = AddPick(bpaForm, "Capability", available.Select(o => new Pick(o.Capability, o.Label)).ToArray(), bpaCapability, id =>
        {
            bpaCapability = id; bpaScope.Clear(); RefreshBpaForm(session.View, DirectoryOffers(session.View!));
        });
        bpaCapability = Picked(BpaCapabilityInput);
        var selected = available.Single(o => o.Capability == bpaCapability);
        bpaForm.AddChild(DeskUi.Label("Scope lines offered by this vendor"));
        foreach (var line in selected.Vendor.Lines)
        {
            var input = new CheckBox { Text = ScopeLineName(view, line), ButtonPressed = bpaScope.Contains(line) };
            input.Toggled += on => { if (on) bpaScope.Add(line); else bpaScope.Remove(line); UpdateEstablishPreview(); };
            BpaScopeInputs.Add(line, input); bpaForm.AddChild(input);
        }
        CustomBpaLimit = new CheckBox { Text = "Set a custom call limit", ButtonPressed = bpaCallLimit is not null };
        bpaForm.AddChild(CustomBpaLimit); bpaForm.AddChild(DeskUi.Label("Call limit ($); unchecked uses the simulation default"));
        // Bound only by SpinBox's exact cent representation, not a copied threshold rule.
        BpaLimitInput = new SpinBox { MinValue = 0, MaxValue = 90071992547409, Step = 1, Value = (double)(bpaCallLimit / 100m ?? 1), Editable = bpaCallLimit is not null };
        bpaForm.AddChild(BpaLimitInput);
        CustomBpaLimit.Toggled += on => { BpaLimitInput.Editable = on; bpaCallLimit = on ? checked((long)((decimal)BpaLimitInput.Value * 100)) : null; UpdateEstablishPreview(); };
        BpaLimitInput.ValueChanged += value => { if (CustomBpaLimit.ButtonPressed) { bpaCallLimit = checked((long)((decimal)value * 100)); UpdateEstablishPreview(); } };
        if (session.IsReplay) bpaForm.AddChild(DeskUi.Label("Establishing takes control and stops future replay actions."));
        UpdateEstablishPreview();
    }

    private void UpdateEstablishPreview()
    {
        if (session.View is not { } view || bpaVendor is null || bpaCapability is null) return;
        while (view.Vehicles.Any(v => v.Id == $"player-bpa-{bpaSequence}") || view.Work.Any(w => w.Payload is EstablishBpaAction e && e.Id == $"player-bpa-{bpaSequence}")) bpaSequence++;
        var offer = DirectoryOffers(view).Single(o => o.Vendor.Id == bpaVendor && o.Capability == bpaCapability);
        PendingEstablish = new EstablishBpaAction { Day = view.Day, Id = $"player-bpa-{bpaSequence}", Vendor = bpaVendor, Capability = bpaCapability,
            ScopeLines = offer.Vendor.Lines.Where(bpaScope.Contains).ToList(), CallLimitCents = bpaCallLimit };
        EstablishPreview = view.PreviewEstablish(PendingEstablish);
        bpaPreviewText.Text = $"Shop {EstablishPreview.ShopHoursMin}–{EstablishPreview.ShopHoursMax} h · {EstablishPreview.QueueAheadHours} h queued\nReady for calls day {EstablishPreview.EarliestStartMin}–{EstablishPreview.EarliestStartMax} (ignores queue ahead)\n" +
            (EstablishPreview.HardStops.Count == 0 ? "No hard stops." : string.Join("\n", EstablishPreview.HardStops)) +
            (EstablishPreview.SoftGaps.Count == 0 ? "" : "\n" + string.Join("\n", EstablishPreview.SoftGaps));
        bpaPreviewText.AddThemeColorOverride("font_color", EstablishPreview.Accepted ? TabletopTheme.Ink : TabletopTheme.Alert);
        EstablishButton.Disabled = !EstablishPreview.Accepted || view.Finished;
    }
}
