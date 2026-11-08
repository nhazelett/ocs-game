using Godot;
using System.Text.Json;

namespace LinesOfSupport.Game;

public partial class GameShell : Control
{
    private readonly GameSessionBridge session = new();
    private IReadOnlyList<ScenarioFolder> scenarios = [];
    internal string? ScenarioRootOverride { get; set; }

    public override void _Ready()
    {
        GetNode<Label>("%GameTitle").Text = ProjectSettings.GetSetting("application/config/name").AsString();
        GetNode<Button>("%NextDay").Pressed += OnNextDay;
        RefreshStrip();
        PopulateScenarios();
    }

    private void PopulateScenarios()
    {
        var picker = GetNode<OptionButton>("%ScenarioPicker");
        try
        {
            var root = ScenarioRootOverride ?? Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "scenarios"));
            scenarios = ScenarioCatalog.Discover(root);
            foreach (var scenario in scenarios)
                picker.AddItem(scenario.Name);
            picker.ItemSelected += LoadScenario;
            picker.Disabled = scenarios.Count == 0;
            if (scenarios.Count > 0)
            {
                var initial = scenarios.Select(x => x.Name).ToList().IndexOf("_stub");
                picker.Select(initial >= 0 ? initial : 0);
                LoadScenario(picker.Selected);
            }
            else
                GetNode<Label>("%BuildStatus").Text = "No scenario folders found.";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            picker.Disabled = true;
            GetNode<Label>("%BuildStatus").Text = $"Cannot list scenarios: {error.Message}";
        }
    }

    private void LoadScenario(long index)
    {
        var map = GetNode<SubViewport>("%BoardViewport").GetNode<SchematicMap>("SchematicMap");
        map.ShowScenario(null);
        try
        {
            var data = ScenarioBoardData.Load(scenarios[(int)index].ScenarioPath);
            map.ShowScenario(data);
            GetNode<Label>("%BoardCaption").Text = data.Title + " · schematic layout, not geographic positions";
            GetNode<Label>("%BuildStatus").Text = $"Viewing {data.Id}. Day advance is not connected.";
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or
            KeyNotFoundException or InvalidOperationException or FormatException)
        {
            GetNode<Label>("%BoardCaption").Text = "Scenario could not be displayed.";
            GetNode<Label>("%BuildStatus").Text = $"Cannot load scenario: {error.Message}";
        }
    }

    private void OnNextDay()
    {
        if (!session.RequestNextDay())
            GetNode<Label>("%BuildStatus").Text = "Day advance is unavailable until a scenario is connected.";
        RefreshStrip();
    }

    private void RefreshStrip()
    {
        GetNode<Label>("%DayCounter").Text = $"Day {session.Day}";
        GetNode<Label>("%ShopHoursLabel").Text = session.ShopHours is int hours ? $"Shop hours: {hours}" : "Shop hours: —";
        GetNode<Label>("%CashLabel").Text = session.CashCents is long cents ? $"Cash: {cents / 100m:C}" : "Cash: —";
    }
}
