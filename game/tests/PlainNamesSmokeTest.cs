using Godot;
using LinesOfSupport.Sim;
using System.Text.Json;
using System.Text.RegularExpressions;
using static LinesOfSupport.Game.Tests.DeskSmokeSupport;

namespace LinesOfSupport.Game.Tests;

public partial class PlainNamesSmokeTest : Node
{
    private int checkedPanels, checkedStrings;

    public override async void _Ready()
    {
        GameShell? shell = null;
        try
        {
            shell = Shell(this);
            foreach (var folder in Directory.EnumerateDirectories(GamePaths.ScenarioRoot).Where(p => Path.GetFileName(p) != "_stub" && File.Exists(Path.Combine(p, "scenario.json"))))
            {
                var scenario = Path.GetFileName(folder);
                Scenario(shell, scenario);
                var forbidden = ScenarioIds(Path.Combine(folder, "scenario.json"));
                InspectForms(shell, forbidden);
                var replay = scenario switch
                {
                    "spoke-b" => "approach1 as written",
                    "spoke-b-90" => "approach3b uca never definitized",
                    "sustainment" => "approach3 cure bfuel",
                    "tutorial" => "approach1 taught",
                    _ => null
                };
                if (replay is not null) Replay(shell, replay);
                shell.DeveloperToggle.ButtonPressed = false;
                while (true)
                {
                    InspectPanels(shell, forbidden);
                    if (shell.Session.View!.Finished) break;
                    Step(shell);
                }
            }
            // Show the original reported problem through a real customer card, with developer tools off.
            Scenario(shell, "spoke-b"); shell.DeveloperToggle.ButtonPressed = false;
            shell.RequestOpenButtons["req0007"].EmitSignal(Button.SignalName.Pressed);
            var card = shell.Session.View!.RequirementEditor("req0007")!;
            var panel = shell.GetNode<TabContainer>("%DeskTabs").GetNode("Requirements");
            Require(PlayerText(panel).Any(t => t.Contains(card.Title) && t.Contains(card.Person!.DisplayName)), "The requirement shows what and who.");
            var args = OS.GetCmdlineUserArgs(); var shot = Array.IndexOf(args, "--screenshot");
            if (shot >= 0)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Require(GetViewport().GetTexture().GetImage().SavePng(args[shot + 1]) == Error.Ok, "Plain-name screenshot saved.");
            }
            shell.QueueFree(); shell = null;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print($"PLAIN_NAMES_SMOKE_PASS: {checkedPanels} panel visits and {checkedStrings} strings; all scenarios, forms, live days, debrief causes and internal action keys.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            shell?.QueueFree();
            GD.PushError($"PLAIN_NAMES_SMOKE_FAIL: {error}"); GetTree().Quit(1);
        }
    }

    private static HashSet<string> ScenarioIds(string path)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        using var data = JsonDocument.Parse(File.ReadAllText(path));
        void Visit(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) Visit(item);
            if (element.ValueKind != JsonValueKind.Object) return;
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name == "id" && property.Value.ValueKind == JsonValueKind.String) result.Add(property.Value.GetString()!);
                Visit(property.Value);
            }
        }
        Visit(data.RootElement); return result;
    }

    private void InspectForms(GameShell shell, HashSet<string> forbidden)
    {
        var contacts = Enumerable.Range(0, shell.PersonContactInput.ItemCount).Select(i => shell.PersonContactInput.GetItemMetadata(i).AsString()).ToArray();
        foreach (var contact in contacts) { Pick(shell.PersonContactInput, contact); InspectPanels(shell, forbidden); }
        foreach (var vendor in shell.EstablishVendorButtons.Keys.ToArray())
        {
            shell.EstablishVendorButtons[vendor].EmitSignal(Button.SignalName.Pressed);
            var capabilities = Enumerable.Range(0, shell.BpaCapabilityInput.ItemCount).Select(i => shell.BpaCapabilityInput.GetItemMetadata(i).AsString()).ToArray();
            foreach (var capability in capabilities)
            {
                Pick(shell.BpaCapabilityInput, capability);
                foreach (var input in shell.BpaScopeInputs.Values) input.ButtonPressed = true;
                InspectPanels(shell, forbidden);
            }
        }
        var vendors = Enumerable.Range(0, shell.VendorInput.ItemCount).Select(i => shell.VendorInput.GetItemMetadata(i).AsString()).Where(id => id.Length > 0).ToArray();
        foreach (var vendor in vendors)
        {
            Pick(shell.VendorInput, vendor);
            var instruments = Enumerable.Range(0, shell.InstrumentInput.ItemCount).Where(i => !shell.InstrumentInput.IsItemDisabled(i)).Select(i => shell.InstrumentInput.GetItemMetadata(i).AsString()).Where(id => id.Length > 0).ToArray();
            foreach (var instrument in instruments)
            {
                var before = shell.Session.StateHash;
                Pick(shell.InstrumentInput, instrument); InspectPanels(shell, forbidden);
                Require(before == shell.Session.StateHash, "Naming and previewing never change the authoritative state.");
            }
        }
    }

    private void InspectPanels(GameShell shell, HashSet<string> scenarioIds)
    {
        var view = shell.Session.View!;
        var ids = scenarioIds.Concat(view.Records.Select(r => r.Id)).Concat(view.Work.Select(w => w.Id))
            .Concat(view.Arrangements.Select(a => a.Id)).Concat(view.Vehicles.Select(v => v.Id))
            .Concat(view.Rules.Model.Capabilities.SelectMany(c => c.Lines).Select(l => l.Id))
            .Where(id => id.Any(char.IsDigit) || Regex.IsMatch(id, "[a-z][A-Z]")).Distinct().OrderByDescending(id => id.Length).ToArray();
        var pattern = ids.Length == 0 ? null : new Regex(@"(?<![\w-])(?:" + string.Join("|", ids.Select(Regex.Escape)) + @")(?![\w-])");
        var tabs = shell.GetNode<TabContainer>("%DeskTabs");
        foreach (var gap in shell.DebriefPanel.GetChildren().OfType<Button>().Where(b => b.HasMeta("gap")))
        {
            // Disclose every cause chain, including the ones below the initial viewport.
            gap.EmitSignal(Button.SignalName.Pressed);
        }
        for (var i = 0; i < tabs.GetTabCount(); i++)
        {
            tabs.CurrentTab = i; checkedPanels++;
            Check(PlayerText(tabs.GetTabControl(i)), pattern, $"{view.Scenario.Title}, day {view.Day}, {tabs.GetTabTitle(i)}");
        }
        Check(PlayerText(shell.GetNode("Margin/Layout/TopStrip")), pattern, "top strip");
        Check(new[] { shell.GetNode<Label>("%BuildStatus").Text }, pattern, "action result");
    }

    private void Check(IEnumerable<string> strings, Regex? forbidden, string context)
    {
        foreach (var text in strings)
        {
            checkedStrings++;
            Require(forbidden is null || !forbidden.IsMatch(text), $"Internal id leaked in {context}: {forbidden?.Match(text).Value}: {text}");
        }
    }

    private static IEnumerable<string> PlayerText(Node node)
    {
        if (node is Control control) yield return control.TooltipText;
        if (node is Label label) yield return label.Text;
        if (node is RichTextLabel rich) yield return rich.Text;
        if (node is Button button) yield return button.Text;
        if (node is OptionButton options)
            for (var i = 0; i < options.ItemCount; i++)
            {
                yield return options.GetItemText(i); yield return options.GetItemTooltip(i);
            }
        foreach (var child in node.GetChildren()) foreach (var text in PlayerText(child)) yield return text;
    }
}
