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
        if (awardInstrument is not ("sf44" or "gpc")) ClearUnavailable(ref awardFunding, choices.Funding);
        if (awardInstrument is not ("bpaCall" or "idiqOrder")) awardVehicle = "";
        if (reasons.Count > 0) awardChoiceNotice = string.Join(" ", reasons.Distinct());
        CurrentAwardChoices = choices;
        return choices;
    }

    private OptionButton AddAwardChoice(string label, string prompt, IReadOnlyList<ChoiceOption> options,
        string? selected, Action<string> changed, out Label explanation, string? notice = null, bool listReasonsOnly = false)
    {
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 12); awardPanel.AddChild(row);
        var caption = DeskUi.Label(label); caption.AutowrapMode = TextServer.AutowrapMode.Off;
        caption.CustomMinimumSize = new Vector2(150, 0); caption.SizeFlagsVertical = SizeFlags.ShrinkCenter; row.AddChild(caption);
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, FitToLongestItem = false, ClipText = true };
        picker.GetPopup().AddThemeFontSizeOverride("font_size", 18);
        picker.AddItem(prompt); picker.SetItemMetadata(0, "");
        static string ListReason(ChoiceOption option)
        {
            var reason = option.Reason ?? "Unavailable today.";
            if (reason.StartsWith(option.Label + " ", StringComparison.OrdinalIgnoreCase))
                reason = reason[(option.Label.Length + 1)..];
            return DeskUi.ListLabel(reason);
        }
        foreach (var option in options)
        {
            var i = picker.ItemCount;
            picker.AddItem(DeskUi.ListLabel(option.Label) + (option.Available ? "" : " — " + ListReason(option)));
            picker.SetItemMetadata(i, option.Id);
            picker.SetItemDisabled(i, !option.Available);
            picker.SetItemTooltip(i, option.Reason ?? "");
        }
        var index = options.Select((o, i) => (o, i)).Where(x => x.o.Id == selected && x.o.Available).Select(x => x.i + 1).DefaultIfEmpty(0).First();
        picker.Select(index);
        var unavailable = options.Where(o => !o.Available).ToArray();
        // Arrangement and supplier refusals live on their disabled picker entries. Repeating them below a
        // chosen buy makes those alternatives read like faults in the buy itself.
        var reasonText = listReasonsOnly ? "" : string.Join("\n\n", unavailable.Select(o => o.Reason ?? $"{o.Label} is unavailable today.").Distinct());
        if (!string.IsNullOrEmpty(notice) && !reasonText.Contains(notice, StringComparison.Ordinal))
            reasonText += (reasonText.Length > 0 ? "\n\n" : "") + notice;
        var reasonLabel = DeskUi.Label(reasonText);
        reasonLabel.AddThemeColorOverride("font_color", TabletopTheme.Ink);
        reasonLabel.Visible = reasonLabel.Text.Length > 0;
        picker.ItemSelected += i =>
        {
            if (i < 0 || i >= picker.ItemCount || picker.IsItemDisabled((int)i))
            {
                picker.Select(index);
                return;
            }
            awardChoiceNotice = null; awardFormTouched = true;
            changed(picker.GetItemMetadata((int)i).AsString());
            RefreshActionDesk(session.View);
        };
        row.AddChild(picker);
        if (listReasonsOnly) awardPanel.AddChild(reasonLabel); else awardChoiceReasons.Add(reasonLabel);
        explanation = reasonLabel;
        return picker;
    }
    // Ask the same read projection about other structural selections. This is navigation guidance, never Game-side
    // eligibility: the Sim still owns every unavailable reason and the amount-dependent preview.
    private static string? AwardPickerNotice(SessionView view, AwardTarget target, string kind, AwardChoicesView choices, string? notice = null)
    {
        var options = kind switch { "supplier" => choices.Vendors, "arrangement" => choices.Instruments,
            "vehicle" => choices.Vehicles, _ => choices.Funding };
        if (options.Any(o => o.Available)) return notice;
        var alternatives = new List<string>();
        foreach (var instrument in view.AwardChoices(target.Site, target.Capability, target.Card)!.Instruments.Where(i => i.Available))
        {
            var other = view.AwardChoices(target.Site, target.Capability, target.Card, instrumentId: instrument.Id)!;
            foreach (var vendor in other.Vendors.Where(v => v.Available))
            {
                var pair = view.AwardChoices(target.Site, target.Capability, target.Card, vendor.Id, instrument.Id)!;
                if (!pair.Instruments.Any(i => i.Id == instrument.Id && i.Available)) continue;
                if (kind == "vehicle" && !pair.Vehicles.Any(v => v.Available)) continue;
                if (kind == "funding" && instrument.Id is not ("sf44" or "gpc") && !pair.Funding.Any(f => f.Available)) continue;
                alternatives.Add(instrument.Label + " with " + vendor.Label);
            }
        }
        var hint = alternatives.Count > 0 ? "To open available choices, choose " + string.Join("; or ", alternatives.Distinct().Take(3)) + "."
            : "No available combination today. Read the reasons above before choosing another request or waiting.";
        return string.IsNullOrEmpty(notice) ? hint : notice + " " + hint;
    }

}
