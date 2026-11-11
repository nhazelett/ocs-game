using Godot;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Game;

public partial class GameShell
{
    internal AwardChoicesView? CurrentAwardChoices { get; private set; }
    internal Label VendorChoiceReason { get; private set; } = null!;
    internal Label InstrumentChoiceReason { get; private set; } = null!;
    internal Label FundingChoiceReason { get; private set; } = null!;
    internal string? AwardChoiceNotice => awardChoiceNotice;
    private string? awardChoiceNotice;
    private bool AwardTermsComplete => CurrentAwardChoices is { } choices
        && choices.Vendors.Any(v => v.Id == awardVendor && v.Available)
        && choices.Instruments.Any(i => i.Id == awardInstrument && i.Available)
        && (awardInstrument is not ("bpaCall" or "idiqOrder") || choices.Vehicles.Any(v => v.Id == awardVehicle && v.Available))
        && (awardInstrument is "sf44" or "gpc" || choices.Funding.Any(f => f.Id == awardFunding && f.Available));

    private AwardChoicesView ReconcileAwardChoices(SessionView view, AwardTarget target)
    {
        AwardChoicesView Read() => view.AwardChoices(target.Site, target.Capability, target.Card,
            string.IsNullOrEmpty(awardVendor) ? null : awardVendor,
            string.IsNullOrEmpty(awardInstrument) ? null : awardInstrument)!;
        var choices = Read();
        var reasons = new List<string>();
        void ClearUnavailable(ref string? selected, IReadOnlyList<ChoiceOption> options)
        {
            if (string.IsNullOrEmpty(selected)) return;
            var id = selected;
            var option = options.FirstOrDefault(o => o.Id == id);
            if (option?.Available == true) return;
            selected = "";
            if (option?.Reason is { } reason) reasons.Add(reason);
        }
        ClearUnavailable(ref awardVendor, choices.Vendors);
        ClearUnavailable(ref awardInstrument, choices.Instruments);
        choices = Read();
        ClearUnavailable(ref awardVehicle, choices.Vehicles);
        ClearUnavailable(ref awardFunding, choices.Funding);
        if (awardInstrument is not ("bpaCall" or "idiqOrder")) awardVehicle = "";
        if (awardInstrument is "sf44" or "gpc") awardFunding = "";
        if (reasons.Count > 0) awardChoiceNotice = string.Join(" ", reasons.Distinct());
        CurrentAwardChoices = choices;
        return choices;
    }

    private OptionButton AddAwardChoice(string label, string prompt, IReadOnlyList<ChoiceOption> options,
        string? selected, Action<string> changed, out Label explanation, string? notice = null)
    {
        awardPanel.AddChild(DeskUi.Label(label));
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, FitToLongestItem = false, ClipText = true };
        picker.AddItem(prompt); picker.SetItemMetadata(0, "");
        foreach (var option in options)
        {
            var i = picker.ItemCount;
            picker.AddItem(DeskUi.ListLabel(option.Label) + (option.Available ? "" : " (unavailable)"));
            picker.SetItemMetadata(i, option.Id);
            picker.SetItemDisabled(i, !option.Available);
            picker.SetItemTooltip(i, option.Reason ?? "");
        }
        var index = options.Select((o, i) => (o, i)).Where(x => x.o.Id == selected && x.o.Available).Select(x => x.i + 1).DefaultIfEmpty(0).First();
        picker.Select(index);
        var unavailable = options.Where(o => !o.Available).ToArray();
        var reasonText = index > 0 ? "" : unavailable.Length switch
        {
            1 => unavailable[0].Reason ?? "",
            > 1 => $"{unavailable.Length} choices are unavailable; point at one to see why.",
            _ => ""
        };
        reasonText = notice ?? reasonText;
        var reasonLabel = DeskUi.Label(reasonText);
        reasonLabel.Visible = reasonLabel.Text.Length > 0;
        picker.ItemSelected += i =>
        {
            if (i < 0 || i >= picker.ItemCount || picker.IsItemDisabled((int)i))
            {
                picker.Select(index);
                return;
            }
            awardChoiceNotice = null;
            changed(picker.GetItemMetadata((int)i).AsString());
            RefreshActionDesk(session.View);
        };
        picker.GetPopup().IdFocused += id =>
        {
            if (id < 0 || id >= picker.ItemCount) return;
            var option = options.FirstOrDefault(o => o.Id == picker.GetItemMetadata((int)id).AsString());
            reasonLabel.Text = option is { Available: false, Reason: { } reason } ? reason : reasonText;
            reasonLabel.Visible = reasonLabel.Text.Length > 0;
        };
        picker.GetPopup().PopupHide += () =>
        {
            reasonLabel.Text = reasonText;
            reasonLabel.Visible = reasonLabel.Text.Length > 0;
        };
        awardPanel.AddChild(picker); awardPanel.AddChild(reasonLabel); explanation = reasonLabel;
        return picker;
    }
}
