using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    private sealed record BpaOffer(string Capability, string Label, VendorOption Vendor);
    internal OptionButton VendorCardPicker { get; private set; } = null!;
    internal VBoxContainer VendorCardsBox { get; private set; } = null!;
    private VBoxContainer bpaForm = null!, vendorSetupFooter = null!;
    private Control vendorSetupPane = null!;
    private Label establishReason = null!;
    private ScrollContainer vendorCardsScroll = null!, vendorSetupScroll = null!;
    private Label bpaPreviewText = null!;
    private string? bpaVendor, bpaCapability, idiqFunding, setupHash, selectedVendor;
    private bool settingIdiq;
    private readonly HashSet<string> bpaScope = new(StringComparer.Ordinal);
    private long? bpaCallLimit, idiqGuarantee;
    private long idiqCeiling;
    private int? idiqPeriod;
    private int bpaSequence = 1, vendorSetupVersion;
    internal EstablishBpaAction? PendingEstablish { get; private set; }
    internal EstablishIdiqAction? PendingIdiq { get; private set; }
    internal ActionPreview? EstablishPreview { get; private set; }
    internal Button EstablishButton { get; private set; } = null!;
    internal Button VendorSetupBack { get; private set; } = null!;
    internal Dictionary<string, Button> EstablishVendorButtons { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, Button> IdiqVendorButtons { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, Button> VendorMeetButtons { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, VBoxContainer> VendorCardContents { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, CheckBox> BpaScopeInputs { get; } = new(StringComparer.Ordinal);
    internal OptionButton BpaCapabilityInput { get; private set; } = null!;
    internal CheckBox CustomBpaLimit { get; private set; } = null!;
    internal SpinBox BpaLimitInput { get; private set; } = null!;
    internal SpinBox IdiqCeilingInput { get; private set; } = null!;
    internal SpinBox IdiqGuaranteeInput { get; private set; } = null!;
    internal SpinBox IdiqPeriodInput { get; private set; } = null!;
    internal CheckBox CustomIdiqGuarantee { get; private set; } = null!;
    internal CheckBox CustomIdiqPeriod { get; private set; } = null!;
    internal OptionButton IdiqFundingInput { get; private set; } = null!;

    private void BuildBpaDesk()
    {
        var tabs = GetNode<TabContainer>("%DeskTabs");
        var page = tabs.GetNode<Control>("Directory");
        tabs.SetTabTitle(page.GetIndex(), "Vendors");
        // Keep stable internal tab indices for navigation; this obsolete surface is unreachable.
        tabs.SetTabHidden(tabs.GetNode<Control>("Vehicles").GetIndex(), true);
        VendorCardsBox = DeskUi.Panel(page, "VendorCards");
        vendorCardsScroll = (ScrollContainer)VendorCardsBox.GetParent();
        bpaForm = DeskUi.Pane(page, "VendorSetup", out vendorSetupFooter);
        vendorSetupScroll = (ScrollContainer)bpaForm.GetParent();
        vendorSetupPane = vendorSetupFooter.GetParent<Control>(); vendorSetupPane.Hide();
        vendorSetupScroll.FollowFocus = true;
    }

    private static List<BpaOffer> DirectoryOffers(SessionView view) => AwardTargets(view)
        .Select(t => view.ActionEditor(t.Site, t.Capability, t.Card)!)
        .SelectMany(e => e.Vendors.Select(v => new BpaOffer(e.CapabilityId, e.CapabilityLabel, v)))
        .DistinctBy(o => (o.Capability, o.Vendor.Id)).ToList();

    private void CloseVendorSetup()
    {
        vendorSetupVersion++; vendorSetupPane.Hide(); vendorCardsScroll.Show();
        PendingEstablish = null; PendingIdiq = null; EstablishPreview = null; setupHash = null;
    }

    private void ResetBpaForm()
    {
        bpaVendor = bpaCapability = idiqFunding = selectedVendor = null; bpaScope.Clear(); bpaCallLimit = idiqGuarantee = null;
        idiqCeiling = 0; idiqPeriod = null; bpaSequence = 1;
        if (vendorSetupScroll is not null) CloseVendorSetup();
    }

    private void OpenVendorSetup(string vendor, bool idiq)
    {
        if (session.View is not { Finished: false } view || AtFrontDoor || ReadingTurnover || PlayingDay
            || !view.VendorCards().Any(c => c.Id == vendor)) return;
        bpaVendor = vendor; bpaCapability = null; bpaScope.Clear(); bpaCallLimit = idiqGuarantee = null;
        idiqFunding = null; idiqCeiling = 0; idiqPeriod = null; settingIdiq = idiq;
        vendorCardsScroll.Hide(); vendorSetupPane.Show(); vendorSetupScroll.ScrollVertical = 0;
        RefreshBpaForm(view, DirectoryOffers(view)); VendorSetupBack.GrabFocus();
    }

    private void MeetFromVendorCard(string vendor, string? observedHash)
    {
        if (observedHash != session.StateHash || session.View is not { Finished: false } view || AtFrontDoor || PlayingDay || ReadingTurnover) return;
        var card = view.VendorCards().FirstOrDefault(c => c.Id == vendor);
        if (card is null || card.MetDay is not null || card.MeetingSetUp) return;
        var action = new MeetVendorAction { Day = view.Day, Vendor = vendor };
        if (view.PreviewVisit(action).Accepted) SendAction(action);
    }

    private void RefreshBpaDesk(SessionView? view)
    {
        DeskUi.Clear(VendorCardsBox); EstablishVendorButtons.Clear(); IdiqVendorButtons.Clear();
        VendorMeetButtons.Clear(); VendorCardContents.Clear();
        VendorCardsBox.AddChild(DeskUi.Label("Vendors"));
        VendorCardsBox.AddChild(DeskUi.Label("Blank facts fill in when you meet a vendor, do business with them or survey their island."));
        if (view is null) { VendorCardsBox.AddChild(DeskUi.Label("Start a deployment to see the vendors.")); CloseVendorSetup(); return; }
        var offers = DirectoryOffers(view);
        var cards = view.VendorCards();
        if (!cards.Any(c => c.Id == selectedVendor)) selectedVendor = cards.FirstOrDefault()?.Id;
        VendorCardPicker = AddPick(VendorCardsBox, "Whose card?", cards.Select(c => new Pick(c.Id, c.Name)).ToArray(), selectedVendor,
            id => { selectedVendor = id; vendorCardsScroll.ScrollVertical = 0; RefreshBpaDesk(session.View); });
        foreach (var card in cards)
        {
            var paper = new PanelContainer { Visible = card.Id == selectedVendor }; VendorCardsBox.AddChild(paper);
            var margin = new MarginContainer(); paper.AddChild(margin);
            foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 12);
            var box = new VBoxContainer(); margin.AddChild(box); VendorCardContents[card.Id] = box;
            var name = DeskUi.Label(card.Name); name.ThemeTypeVariation = "TabletopHeading"; box.AddChild(name);
            box.AddChild(DeskUi.Label("Based on " + card.IslandName));
            box.AddChild(DeskUi.Label("Cash: " + Payment(card.AcceptsCash) + " · Card: " + Payment(card.AcceptsGpc)));
            box.AddChild(DeskUi.Label("Islands reached: " + (card.IslandsReached is null ? "—" : string.Join(", ", card.IslandsReached.Select(view.NameOf)))));
            foreach (var sell in card.Sells)
                box.AddChild(DeskUi.Label(sell.Label + "\nSupply per day: " + (sell.CapacityPerDay is { } capacity ? $"{capacity.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} {sell.Unit}" : "—")
                    + $"\nPrice per {sell.Unit}{(view.Rules.Model.Capabilities.Single(c => c.Id == sell.CapabilityId).Delivery == "consumable" ? "" : " per day")}: " + (sell.UnitPriceCents is { } price ? DeskUi.Money(price) : "—")
                    + " · Notice: " + (sell.LeadTimeDays is { } days ? (days == 1 ? "1 day" : $"{days} days") : "—")));
            if (card.Standing is not null) box.AddChild(DeskUi.Label("Standing: " + card.Standing));
            if (card.KnownFrom is not null) box.AddChild(DeskUi.Label(card.KnownFrom));
            if (card.StillToLearn is not null) box.AddChild(DeskUi.Label(card.StillToLearn));
            box.AddChild(DeskUi.Label("What we hold with them"));
            foreach (var id in card.ArrangementIds)
            {
                var arrangement = view.Arrangements.Single(a => a.Id == id);
                box.AddChild(DeskUi.Button(view.ArrangementName(id) + " · " + arrangement.Status, () => OpenContractView(id)));
            }
            foreach (var id in card.VehicleIds)
            {
                var vehicle = view.Vehicles.Single(v => v.Id == id);
                var label = DeskUi.Label(view.VehicleName(id) + $" · ordering through day {vehicle.OrderingPeriodEndDay}"
                    + (vehicle.CallLimitCents is { } limit ? " · call limit " + DeskUi.Money(limit) : ""));
                label.SetMeta("vehicle", id); box.AddChild(label);
            }
            var pending = view.Work.Where(w => w.Kind == WorkKind.EstablishVehicle && w.CompletedDay is null
                && (w.Payload is EstablishBpaAction bpa && bpa.Vendor == card.Id || w.Payload is EstablishIdiqAction idiq && idiq.Vendor == card.Id)).ToArray();
            foreach (var work in pending) box.AddChild(DeskUi.Label("Setup in work: " + work.Label));
            if (card.ArrangementIds.Count + card.VehicleIds.Count + pending.Length == 0) box.AddChild(DeskUi.Label("Nothing yet."));
            if (card.Rep is { } rep)
            {
                var personRow = new HBoxContainer(); box.AddChild(personRow);
                var portrait = new PersonPortrait(); portrait.SetPerson(rep); personRow.AddChild(portrait);
                var details = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; personRow.AddChild(details);
                details.AddChild(DeskUi.Label(rep.WithUnit));
                var visit = view.PreviewVisit(new MeetVendorAction { Day = view.Day, Vendor = card.Id });
                var observedHash = session.StateHash;
                var meet = DeskUi.Button(card.MeetingSetUp ? "Meeting in work" : card.MetDay is { } met ? $"Met on day {met}" : "Meet " + rep.DisplayName,
                    () => MeetFromVendorCard(card.Id, observedHash));
                meet.Disabled = view.Finished || card.MetDay is not null || card.MeetingSetUp || !visit.Accepted;
                var meetReason = DeskUi.ActionReason(details, meet);
                DeskUi.Gate(meet, meetReason, view.Finished ? DeskUi.FinishedReason : card.MeetingSetUp ? "The meeting is already in work."
                    : card.MetDay is not null ? "You have already met this vendor." : visit.HardStops.FirstOrDefault()); VendorMeetButtons[card.Id] = meet;
                if (card.MetDay is null && !card.MeetingSetUp)
                    details.AddChild(DeskUi.Label(visit.Accepted ? $"About {VendorRange(visit.ShopHoursMin, visit.ShopHoursMax)} shop hours; back day {VendorRange(visit.EarliestStartMin, visit.EarliestStartMax)}{DeskUi.QueueClause(visit.QueueAheadHours)}." : string.Join("\n", visit.HardStops)));
            }
            if (offers.Any(o => o.Vendor.Id == card.Id))
            {
                var bpa = DeskUi.Button("Set up a BPA", () => OpenVendorSetup(card.Id, false)); bpa.Disabled = view.Finished;
                var idiq = DeskUi.Button("Set up an IDIQ", () => OpenVendorSetup(card.Id, true)); idiq.Disabled = view.Finished;
                var bpaReason = DeskUi.ActionReason(box, bpa); var idiqReason = DeskUi.ActionReason(box, idiq);
                DeskUi.Gate(bpa, bpaReason, view.Finished ? DeskUi.FinishedReason : null);
                DeskUi.Gate(idiq, idiqReason, view.Finished ? DeskUi.FinishedReason : null); EstablishVendorButtons[card.Id] = bpa; IdiqVendorButtons[card.Id] = idiq;
            }
        }
        if (vendorSetupPane.Visible) RefreshBpaForm(view, offers);
    }

    private static string VendorRange(int min, int max) => min == max ? min.ToString() : $"{min}–{max}";

    private static string Payment(bool? takes) => takes is null ? "—" : takes.Value ? "yes" : "no";

    private void RefreshBpaForm(SessionView view, IReadOnlyList<BpaOffer> offers)
    {
        var version = ++vendorSetupVersion;
        DeskUi.Clear(bpaForm); DeskUi.Clear(vendorSetupFooter); BpaScopeInputs.Clear(); PendingEstablish = null; PendingIdiq = null; EstablishPreview = null;
        var available = offers.Where(o => o.Vendor.Id == bpaVendor).ToArray();
        VendorSetupBack = DeskUi.Button("Back to vendors", CloseVendorSetup); bpaForm.AddChild(VendorSetupBack);
        if (available.Length == 0) { CloseVendorSetup(); return; }
        var title = DeskUi.Label($"Set up {(settingIdiq ? "an IDIQ" : "a BPA")} with {available[0].Vendor.Name}");
        title.ThemeTypeVariation = "TabletopHeading"; bpaForm.AddChild(title);
        bpaForm.AddChild(DeskUi.Label(settingIdiq ? "A binding contract. The minimum guarantee is owed even if you place no orders."
            : "An agreement for repeat buys. Setting it up does not buy anything; each call is a separate purchase."));
        BpaCapabilityInput = AddPick(bpaForm, "What will it cover?", available.Select(o => new Pick(o.Capability, o.Label)).ToArray(), bpaCapability, id =>
        { bpaCapability = id; bpaScope.Clear(); RefreshBpaForm(session.View!, DirectoryOffers(session.View!)); });
        bpaCapability = Picked(BpaCapabilityInput);
        var selected = available.Single(o => o.Capability == bpaCapability);
        bpaForm.AddChild(DeskUi.Label("Scope offered by this vendor"));
        foreach (var line in selected.Vendor.Lines)
        {
            var input = new CheckBox { Text = ScopeLineName(view, line), ButtonPressed = bpaScope.Contains(line) };
            input.Toggled += on => { if (on) bpaScope.Add(line); else bpaScope.Remove(line); UpdateEstablishPreview(); };
            BpaScopeInputs[line] = input; bpaForm.AddChild(input);
        }
        if (settingIdiq)
        {
            IdiqCeilingInput = CurrencyInput("Contract ceiling ($)", idiqCeiling, value => { idiqCeiling = value; UpdateEstablishPreview(); });
            CustomIdiqGuarantee = new CheckBox { Text = "Set a minimum guarantee", ButtonPressed = idiqGuarantee is not null };
            bpaForm.AddChild(CustomIdiqGuarantee);
            IdiqGuaranteeInput = CurrencyInput("Minimum guarantee ($); unchecked uses the standard minimum", idiqGuarantee ?? 0, value =>
            { if (CustomIdiqGuarantee.ButtonPressed) { idiqGuarantee = value; UpdateEstablishPreview(); } });
            IdiqGuaranteeInput.Editable = idiqGuarantee is not null;
            CustomIdiqGuarantee.Toggled += on => { IdiqGuaranteeInput.Editable = on; idiqGuarantee = on ? checked((long)((decimal)IdiqGuaranteeInput.Value * 100)) : null; UpdateEstablishPreview(); };
            CustomIdiqPeriod = new CheckBox { Text = "Set an ordering period", ButtonPressed = idiqPeriod is not null }; bpaForm.AddChild(CustomIdiqPeriod);
            bpaForm.AddChild(DeskUi.Label("Ordering days; unchecked uses the rest of the deployment"));
            IdiqPeriodInput = new SpinBox { MinValue = 1, MaxValue = int.MaxValue, Step = 1, Value = idiqPeriod ?? 1, Editable = idiqPeriod is not null }; bpaForm.AddChild(IdiqPeriodInput);
            IdiqPeriodInput.ValueChanged += value => { if (CustomIdiqPeriod.ButtonPressed) { idiqPeriod = checked((int)value); UpdateEstablishPreview(); } };
            CustomIdiqPeriod.Toggled += on => { IdiqPeriodInput.Editable = on; idiqPeriod = on ? checked((int)IdiqPeriodInput.Value) : null; UpdateEstablishPreview(); };
            IdiqFundingInput = AddPick(bpaForm, "Funding for the guarantee", view.Funding.Select(f => new Pick(f.Id, view.FundingName(f.Id))).ToArray(), idiqFunding,
                id => { idiqFunding = id; UpdateEstablishPreview(); });
            idiqFunding = Picked(IdiqFundingInput);
        }
        else
        {
            CustomBpaLimit = new CheckBox { Text = "Set a custom call limit", ButtonPressed = bpaCallLimit is not null }; bpaForm.AddChild(CustomBpaLimit);
            BpaLimitInput = CurrencyInput("Call limit ($); unchecked uses the standard limit", bpaCallLimit ?? 100, value =>
            { if (CustomBpaLimit.ButtonPressed) { bpaCallLimit = value; UpdateEstablishPreview(); } });
            BpaLimitInput.Editable = bpaCallLimit is not null;
            CustomBpaLimit.Toggled += on => { BpaLimitInput.Editable = on; bpaCallLimit = on ? checked((long)((decimal)BpaLimitInput.Value * 100)) : null; UpdateEstablishPreview(); };
        }
        if (session.IsReplay) bpaForm.AddChild(DeskUi.Label("Setting this up takes control and stops future replay actions."));
        bpaPreviewText = DeskUi.Label(); bpaForm.AddChild(bpaPreviewText);
        EstablishButton = DeskUi.Button(settingIdiq ? "Establish IDIQ" : "Establish BPA", () => SubmitVendorSetup(version)); establishReason = DeskUi.ActionReason(vendorSetupFooter, EstablishButton);
        UpdateEstablishPreview();
    }

    private SpinBox CurrencyInput(string label, long cents, Action<long> change)
    {
        bpaForm.AddChild(DeskUi.Label(label));
        var input = new SpinBox { MinValue = 0, MaxValue = 90071992547409, Step = 1, Value = (double)(cents / 100m) };
        input.ValueChanged += value => change(checked((long)((decimal)value * 100))); bpaForm.AddChild(input); return input;
    }

    private void UpdateEstablishPreview()
    {
        if (session.View is not { } view || bpaVendor is null || bpaCapability is null || !vendorSetupPane.Visible) return;
        var prefix = settingIdiq ? "player-idiq-" : "player-bpa-";
        bool InUse(string id) => view.Vehicles.Any(v => v.Id == id) || view.Work.Any(w => w.Payload is EstablishBpaAction b && b.Id == id || w.Payload is EstablishIdiqAction i && i.Id == id);
        while (InUse(prefix + bpaSequence)) bpaSequence++;
        var offer = DirectoryOffers(view).Single(o => o.Vendor.Id == bpaVendor && o.Capability == bpaCapability);
        var scope = offer.Vendor.Lines.Where(bpaScope.Contains).ToList();
        if (settingIdiq)
        {
            PendingIdiq = new EstablishIdiqAction { Day = view.Day, Id = prefix + bpaSequence, Vendor = bpaVendor, Capability = bpaCapability,
                ScopeLines = scope, CeilingCents = idiqCeiling, MinimumGuaranteeCents = idiqGuarantee, OrderingPeriodDays = idiqPeriod, FundingLine = idiqFunding ?? "" };
            EstablishPreview = view.PreviewEstablish(PendingIdiq); PendingEstablish = null;
        }
        else
        {
            PendingEstablish = new EstablishBpaAction { Day = view.Day, Id = prefix + bpaSequence, Vendor = bpaVendor, Capability = bpaCapability, ScopeLines = scope, CallLimitCents = bpaCallLimit };
            EstablishPreview = view.PreviewEstablish(PendingEstablish); PendingIdiq = null;
        }
        setupHash = session.StateHash;
        bpaPreviewText.Text = $"About {VendorRange(EstablishPreview.ShopHoursMin, EstablishPreview.ShopHoursMax)} shop hours; ready day {VendorRange(EstablishPreview.EarliestStartMin, EstablishPreview.EarliestStartMax)}{DeskUi.QueueClause(EstablishPreview.QueueAheadHours)}."
            + (settingIdiq ? "\nMinimum guarantee to obligate: " + DeskUi.Money(EstablishPreview.TotalCents) : "")
            + (EstablishPreview.HardStops.Count == 0 ? "\nNo hard stops." : "\n" + string.Join("\n", EstablishPreview.HardStops))
            + (EstablishPreview.SoftGaps.Count == 0 ? "" : "\n" + string.Join("\n", EstablishPreview.SoftGaps));
        bpaPreviewText.AddThemeColorOverride("font_color", EstablishPreview.Accepted ? TabletopTheme.Ink : TabletopTheme.Alert);
        DeskUi.Gate(EstablishButton, establishReason, view.Finished ? DeskUi.FinishedReason : EstablishPreview.HardStops.FirstOrDefault());
    }

    private void SubmitVendorSetup(int version)
    {
        if (version != vendorSetupVersion || !vendorSetupPane.Visible || AtFrontDoor || PlayingDay || ReadingTurnover || session.View is not { Finished: false }
            || setupHash != session.StateHash || EstablishPreview?.Accepted != true) return;
        PlayerAction? action = settingIdiq ? PendingIdiq : PendingEstablish;
        if (action is null) return;
        if (SendAction(action).Accepted) CloseVendorSetup();
    }
}
